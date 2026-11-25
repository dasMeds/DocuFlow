using System.Text;
using System.Text.Json.Serialization;
using DocFlow.Api.Hubs;
using DocFlow.Core.Enums;
using DocFlow.Core.Interfaces;
using DocFlow.Core.Models;
using DocFlow.Core.Validation;
using DocFlow.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
var uploadDirectory = Path.Combine(builder.Environment.ContentRootPath, "uploads");
var tessdataDirectory = Path.Combine(builder.Environment.ContentRootPath, "tessdata");

// API and desktop hosts share the same queue, repository, parser factory, and worker.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSignalR().AddJsonProtocol(options =>
    options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddDocFlowProcessing(
    tessdataDirectory,
    builder.Configuration["Tesseract:ExecutablePath"] ?? "tesseract");
builder.Services.AddSingleton<IPipelineNotifier, SignalRPipelineNotifier>();

var app = builder.Build();
Directory.CreateDirectory(uploadDirectory);
app.UseDefaultFiles();
app.UseStaticFiles();

// Job reads and CSV export are read-only views over the repository.
app.MapGet("/api/jobs", async (IJobRepository repository, CancellationToken ct) =>
    Results.Ok(await repository.GetAllAsync(ct)));

app.MapGet("/api/jobs/{id:guid}", async (Guid id, IJobRepository repository, CancellationToken ct) =>
{
    var job = await repository.GetByIdAsync(id, ct);
    return job is null ? Results.NotFound() : Results.Ok(job);
});

app.MapGet("/api/jobs/export", async (IJobRepository repository, CancellationToken ct) =>
{
    var jobs = await repository.GetAllAsync(ct);
    var lines = new List<string>
    {
        "Id,FileName,FileType,Status,InvoiceNumber,VendorName,IssueDate,Subtotal,TaxAmount,TotalAmount"
    };

    lines.AddRange(jobs.Select(job => string.Join(",", new[]
    {
        job.Id.ToString(),
        EscapeCsv(job.FileName),
        EscapeCsv(job.FileType),
        job.Status.ToString(),
        EscapeCsv(job.ExtractedData?.InvoiceNumber ?? string.Empty),
        EscapeCsv(job.ExtractedData?.VendorName ?? string.Empty),
        job.ExtractedData?.IssueDate?.ToString("yyyy-MM-dd") ?? string.Empty,
        job.ExtractedData?.Subtotal.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
        job.ExtractedData?.TaxAmount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
        job.ExtractedData?.TotalAmount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty
    })));

    return Results.File(
        Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, lines)),
        "text/csv; charset=utf-8",
        "docflow-jobs.csv");
});

app.MapPost("/api/documents", async (
    HttpRequest request,
    IJobRepository repository,
    IDocumentQueue queue,
    IDocumentProcessorFactory processorFactory,
    IPipelineNotifier notifier,
    CancellationToken ct) =>
{
    // Validate all extensions before writing any file from a multi-file request.
    if (!request.HasFormContentType)
        return Results.BadRequest("Expected multipart/form-data.");

    var form = await request.ReadFormAsync(ct);
    if (form.Files.Count == 0)
        return Results.BadRequest("Select at least one document to upload.");

    var files = form.Files.Select(file =>
    {
        var fileName = Path.GetFileName(file.FileName);
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return (File: file, FileName: fileName, Extension: extension);
    }).ToArray();

    var unsupported = files.FirstOrDefault(file => !processorFactory.Supports(file.Extension));
    if (unsupported.File is not null)
        return Results.BadRequest($"Unsupported file format: {unsupported.Extension}");

    var created = new List<DocumentJob>(files.Length);
    foreach (var upload in files)
    {
        var job = new DocumentJob
        {
            FileName = upload.FileName,
            FileType = upload.Extension,
            FileSizeBytes = upload.File.Length,
            StoragePath = Path.Combine(uploadDirectory, $"{Guid.NewGuid():N}_{upload.FileName}")
        };

        try
        {
            await using var stream = new FileStream(
                job.StoragePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true);
            await upload.File.CopyToAsync(stream, ct);
        }
        catch
        {
            if (System.IO.File.Exists(job.StoragePath))
                System.IO.File.Delete(job.StoragePath);
            throw;
        }

        await repository.AddAsync(job, ct);
        await queue.EnqueueAsync(job.Id, ct);
        await notifier.NotifyJobUpdatedAsync(job, ct);
        created.Add(job);
    }

    return Results.Accepted("/api/jobs", created);
}).DisableAntiforgery();

app.MapPost("/api/jobs/{id:guid}/retry", async (
    Guid id,
    IJobRepository repository,
    IDocumentQueue queue,
    IPipelineNotifier notifier,
    CancellationToken ct) =>
{
    var job = await repository.GetByIdAsync(id, ct);
    if (job is null)
        return Results.NotFound();
    if (job.Status != JobStatus.Failed)
        return Results.Conflict("Only failed jobs can be retried.");

    job.Status = JobStatus.Queued;
    job.Stage = PipelineStage.Pending;
    job.RetryCount = 0;
    job.ErrorMessage = null;
    job.ReviewNote = null;
    job.CompletedAt = null;
    await repository.UpdateAsync(job, ct);
    await queue.EnqueueAsync(job.Id, ct);
    await notifier.NotifyJobUpdatedAsync(job, ct);
    return Results.Accepted($"/api/jobs/{id}", job);
});

app.MapPost("/api/jobs/{id:guid}/approve", async (
    Guid id,
    ExtractedInvoice updated,
    IJobRepository repository,
    IPipelineNotifier notifier,
    CancellationToken ct) =>
{
    var job = await repository.GetByIdAsync(id, ct);
    if (job is null)
        return Results.NotFound();
    if (job.Status != JobStatus.NeedsReview)
        return Results.Conflict("Only jobs requiring review can be approved.");

    var (isValid, issue) = InvoiceValidator.Validate(updated);
    if (!isValid)
        return Results.BadRequest(issue);

    job.ExtractedData = updated;
    job.Status = JobStatus.Completed;
    job.Stage = PipelineStage.Done;
    job.ReviewNote = null;
    job.CompletedAt = DateTimeOffset.UtcNow;
    await repository.UpdateAsync(job, ct);
    await notifier.NotifyJobUpdatedAsync(job, ct);
    return Results.Ok(job);
});

app.MapHub<PipelineHub>("/hubs/pipeline");
app.Run();

static string EscapeCsv(string value)
{
    var firstNonWhitespace = value.FirstOrDefault(character => !char.IsWhiteSpace(character));
    if (firstNonWhitespace is '=' or '+' or '-' or '@')
        value = $"'{value}";

    return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
}
