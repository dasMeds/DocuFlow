using System.Collections.Concurrent;
using DocFlow.Core.Interfaces;
using DocFlow.Core.Models;

namespace DocFlow.Infrastructure.Persistence;

/// <summary>
/// Thread-safe in-memory repository for fast local execution and prototyping.
/// Can be replaced with an EF Core / SQLite, SQL Server, or PostgreSQL implementation.
/// </summary>
public sealed class InMemoryJobRepository : IJobRepository
{
    private readonly ConcurrentDictionary<Guid, DocumentJob> _store = new();

    /// <inheritdoc />
    public Task AddAsync(DocumentJob job, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        _store[job.Id] = Copy(job);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<DocumentJob?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_store.TryGetValue(id, out var job) ? Copy(job) : null);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<DocumentJob>> GetAllAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        // Return newest submissions first for UI display
        IReadOnlyList<DocumentJob> list = _store.Values
            .OrderByDescending(j => j.CreatedAt)
            .Select(Copy)
            .ToList();
        return Task.FromResult(list);
    }

    /// <inheritdoc />
    public Task UpdateAsync(DocumentJob job, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        _store[job.Id] = Copy(job);
        return Task.CompletedTask;
    }

    private static DocumentJob Copy(DocumentJob job)
        => new()
        {
            Id = job.Id,
            FileName = job.FileName,
            FileType = job.FileType,
            StoragePath = job.StoragePath,
            FileSizeBytes = job.FileSizeBytes,
            RawTextSnippet = job.RawTextSnippet,
            Status = job.Status,
            Stage = job.Stage,
            RetryCount = job.RetryCount,
            MaxRetries = job.MaxRetries,
            ErrorMessage = job.ErrorMessage,
            ReviewNote = job.ReviewNote,
            ProcessingDiagnostic = job.ProcessingDiagnostic,
            ExtractedData = Copy(job.ExtractedData),
            CreatedAt = job.CreatedAt,
            CompletedAt = job.CompletedAt
        };

    private static DocFlow.Core.Models.ExtractedInvoice? Copy(DocFlow.Core.Models.ExtractedInvoice? invoice)
        => invoice is null
            ? null
            : new DocFlow.Core.Models.ExtractedInvoice
            {
                InvoiceNumber = invoice.InvoiceNumber,
                VendorName = invoice.VendorName,
                IssueDate = invoice.IssueDate,
                Subtotal = invoice.Subtotal,
                TaxAmount = invoice.TaxAmount,
                TotalAmount = invoice.TotalAmount
            };
}
