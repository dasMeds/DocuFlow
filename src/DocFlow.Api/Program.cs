using DocFlow.Api.Hubs;
using DocFlow.Core.Enums;
using DocFlow.Core.Interfaces;
using DocFlow.Core.Models;
using DocFlow.Infrastructure.Persistence;
using DocFlow.Infrastructure.Processors;
using DocFlow.Infrastructure.Queue;
using DocFlow.Infrastructure.Workers;

var builder = WebApplication.CreateBuilder(args);

// Register SignalR and pipeline services in the DI container
builder.Services.AddSignalR();
builder.Services.AddSingleton<IDocumentQueue, ChannelDocumentQueue>();
builder.Services.AddSingleton<IJobRepository, InMemoryJobRepository>();
builder.Services.AddSingleton<IDocumentProcessor, TextRuleDocumentProcessor>();
builder.Services.AddSingleton<IPipelineNotifier, SignalRPipelineNotifier>();
builder.Services.AddHostedService<DocumentProcessingWorker>();

var app = builder.Build();

// Ensure local staging folder exists for uploaded raw documents
var uploadDir = Path.Combine(app.Environment.ContentRootPath, "uploads");
Directory.CreateDirectory(uploadDir);

// Serve the static web workbench from wwwroot/index.html
app.UseDefaultFiles();
app.UseStaticFiles();

// Retrieve all jobs and their current statuses
app.MapGet("/api/jobs", async (IJobRepository repo) => Results.Ok(await repo.GetAllAsync()));

// Intake endpoint: saves raw files, creates Queued records, and returns 202 Accepted immediately
app.MapPost("/api/documents", async (HttpRequest request, IJobRepository repo, IDocumentQueue queue) =>
{
    if (!request.HasFormContentType)
        return Results.BadRequest("Expected multipart/form-data.");

    var form = await request.ReadFormAsync();
    var created = new List<DocumentJob>();

    foreach (var file in form.Files)
    {
        var job = new DocumentJob
        {
            FileName = file.FileName,
            FileSizeBytes = file.Length,
            StoragePath = Path.Combine(uploadDir, $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}")
        };

        await using var stream = File.Create(job.StoragePath);
        await file.CopyToAsync(stream);

        await repo.AddAsync(job);
        await queue.EnqueueAsync(job.Id);
        created.Add(job);
    }

    return Results.Accepted("/api/jobs", created);
}).DisableAntiforgery();

// Manual retry endpoint for Failed / dead-lettered jobs
app.MapPost("/api/jobs/{id:guid}/retry", async (Guid id, IJobRepository repo, IDocumentQueue queue) =>
{
    var job = await repo.GetByIdAsync(id);
    if (job is null) return Results.NotFound();

    job.Status = JobStatus.Queued;
    job.Stage = PipelineStage.Pending;
    job.RetryCount = 0;
    job.ErrorMessage = null;
    await repo.UpdateAsync(job);
    await queue.EnqueueAsync(job.Id);
    return Results.Ok(job);
});

// Human-in-the-loop approval endpoint for NeedsReview jobs
app.MapPost("/api/jobs/{id:guid}/approve", async (Guid id, ExtractedInvoice updated, IJobRepository repo) =>
{
    var job = await repo.GetByIdAsync(id);
    if (job is null) return Results.NotFound();

    job.ExtractedData = updated;
    job.Status = JobStatus.Completed;
    job.ReviewNote = null;
    job.CompletedAt = DateTimeOffset.UtcNow;
    await repo.UpdateAsync(job);
    return Results.Ok(job);
});

app.MapHub<PipelineHub>("/hubs/pipeline");
app.Run();
