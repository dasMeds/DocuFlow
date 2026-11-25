using DocFlow.Core.Models;

namespace DocFlow.Core.Interfaces;

/// <summary>
/// Abstraction over the async job queue (in-memory Channel, RabbitMQ, Service Bus, etc.).
/// </summary>
public interface IDocumentQueue
{
    /// <summary>Adds a job identifier to the queue, waiting when it is full.</summary>
    ValueTask EnqueueAsync(Guid jobId, CancellationToken ct = default);
    /// <summary>Waits asynchronously for the next job identifier.</summary>
    ValueTask<Guid> DequeueAsync(CancellationToken ct = default);
}

/// <summary>
/// Extracts readable text and optional diagnostics from a document.
/// </summary>
public interface IDocumentProcessor
{
    /// <summary>File extensions accepted by this processor, including their leading dots.</summary>
    IReadOnlyCollection<string> SupportedExtensions { get; }
    /// <summary>Returns whether the processor accepts the supplied extension.</summary>
    bool CanHandle(string fileExtension);
    /// <summary>Extracts text from a file and returns any non-fatal diagnostic.</summary>
    Task<DocumentTextExtraction> ExtractTextAsync(string filePath, CancellationToken ct = default);
}

/// <summary>
/// Resolves a document processor by its file extension.
/// </summary>
public interface IDocumentProcessorFactory
{
    /// <summary>Returns whether a processor is registered for the supplied extension.</summary>
    bool Supports(string fileExtension);
    /// <summary>Gets the matching processor or throws for an unsupported extension.</summary>
    IDocumentProcessor GetProcessor(string fileExtension);
}

/// <summary>
/// Extracts normalized invoice fields from text returned by a document processor.
/// </summary>
public interface IInvoiceFieldExtractor
{
    /// <summary>Extracts normalized invoice fields from raw document text.</summary>
    ExtractedInvoice Extract(string rawText);
}

/// <summary>
/// Persistence abstraction for storing and querying job state and extracted payloads.
/// </summary>
public interface IJobRepository
{
    /// <summary>Adds a new job.</summary>
    Task AddAsync(DocumentJob job, CancellationToken ct = default);
    /// <summary>Gets a job by identifier, or null when it does not exist.</summary>
    Task<DocumentJob?> GetByIdAsync(Guid id, CancellationToken ct = default);
    /// <summary>Gets all jobs ordered for display.</summary>
    Task<IReadOnlyList<DocumentJob>> GetAllAsync(CancellationToken ct = default);
    /// <summary>Persists the latest state of a job.</summary>
    Task UpdateAsync(DocumentJob job, CancellationToken ct = default);
}

/// <summary>
/// Pushes live job state transitions to connected clients (WPF Dispatcher or SignalR).
/// </summary>
public interface IPipelineNotifier
{
    /// <summary>Notifies connected consumers that a job changed.</summary>
    Task NotifyJobUpdatedAsync(DocumentJob job, CancellationToken ct = default);
}
