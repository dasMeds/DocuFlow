using DocFlow.Core.Models;

namespace DocFlow.Core.Interfaces;

/// <summary>
/// Abstraction over the async job queue (in-memory Channel, RabbitMQ, Service Bus, etc.).
/// </summary>
public interface IDocumentQueue
{
    ValueTask EnqueueAsync(Guid jobId, CancellationToken ct = default);
    ValueTask<Guid> DequeueAsync(CancellationToken ct = default);
}

/// <summary>
/// Pluggable parser contract so rule-based parsers, OCR, or AI extractors can be swapped cleanly.
/// </summary>
public interface IDocumentProcessor
{
    bool CanHandle(string fileExtension);
    Task<ExtractedInvoice> ExtractAsync(string filePath, CancellationToken ct = default);
}

/// <summary>
/// Persistence abstraction for storing and querying job state and extracted payloads.
/// </summary>
public interface IJobRepository
{
    Task AddAsync(DocumentJob job, CancellationToken ct = default);
    Task<DocumentJob?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<DocumentJob>> GetAllAsync(CancellationToken ct = default);
    Task UpdateAsync(DocumentJob job, CancellationToken ct = default);
}

/// <summary>
/// Pushes live job state transitions to connected clients (WPF Dispatcher or SignalR).
/// </summary>
public interface IPipelineNotifier
{
    Task NotifyJobUpdatedAsync(DocumentJob job, CancellationToken ct = default);
}
