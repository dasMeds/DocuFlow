using DocFlow.Core.Enums;

namespace DocFlow.Core.Models;

/// <summary>
/// Tracks metadata, progress, retry history, and extracted payload for a single submitted file.
/// </summary>
public class DocumentJob
{
    /// <summary>Stable identifier used by the API, queue, and repository.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Sanitized original filename shown to users.</summary>
    public string FileName { get; set; } = string.Empty;
    /// <summary>Lowercase filename extension, including the leading dot.</summary>
    public string FileType { get; set; } = string.Empty;
    /// <summary>Path to the source document available to the processor.</summary>
    public string StoragePath { get; set; } = string.Empty;
    /// <summary>Source document size in bytes.</summary>
    public long FileSizeBytes { get; set; }
    /// <summary>At most 500 characters of extracted source text for preview.</summary>
    public string RawTextSnippet { get; set; } = string.Empty;

    // Current pipeline state and stage
    /// <summary>Current lifecycle status.</summary>
    public JobStatus Status { get; set; } = JobStatus.Queued;
    /// <summary>Current processing stage.</summary>
    public PipelineStage Stage { get; set; } = PipelineStage.Pending;

    // Retry tracking for transient failures
    /// <summary>Number of processing attempts that have failed.</summary>
    public int RetryCount { get; set; }
    /// <summary>Maximum processing attempts before the job is marked failed.</summary>
    public int MaxRetries { get; set; } = 3;

    // Diagnostic messages for failed jobs or human-in-the-loop review flags
    /// <summary>Last unrecoverable or retryable processing error.</summary>
    public string? ErrorMessage { get; set; }
    /// <summary>Reason the job is waiting for human approval.</summary>
    public string? ReviewNote { get; set; }
    /// <summary>Non-fatal extraction diagnostic, such as missing OCR language data.</summary>
    public string? ProcessingDiagnostic { get; set; }

    // Structured output populated after the extraction stage
    /// <summary>Normalized invoice fields, when text extraction has completed.</summary>
    public ExtractedInvoice? ExtractedData { get; set; }

    // Audit timestamps
    /// <summary>Time the job was accepted by the pipeline.</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>Time the job reached a terminal state.</summary>
    public DateTimeOffset? CompletedAt { get; set; }
}

/// <summary>
/// Normalized domain representation of an invoice extracted from a raw document.
/// Uses decimal for all monetary values to prevent floating-point rounding drift.
/// </summary>
public class ExtractedInvoice
{
    /// <summary>Supplier-assigned invoice identifier.</summary>
    public string InvoiceNumber { get; set; } = string.Empty;
    /// <summary>Name of the company or vendor that issued the invoice.</summary>
    public string VendorName { get; set; } = string.Empty;
    /// <summary>Invoice issue date, when present in the source document.</summary>
    public DateOnly? IssueDate { get; set; }
    /// <summary>Pre-tax monetary amount.</summary>
    public decimal Subtotal { get; set; }
    /// <summary>Tax monetary amount.</summary>
    public decimal TaxAmount { get; set; }
    /// <summary>Invoice amount including tax.</summary>
    public decimal TotalAmount { get; set; }
}

/// <summary>
/// Text content and a non-fatal diagnostic produced while reading a document.
/// </summary>
public sealed record DocumentTextExtraction(string Text, string? Diagnostic = null);
