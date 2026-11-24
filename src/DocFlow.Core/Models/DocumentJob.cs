using DocFlow.Core.Enums;

namespace DocFlow.Core.Models;

/// <summary>
/// Tracks metadata, progress, retry history, and extracted payload for a single submitted file.
/// </summary>
public class DocumentJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }

    // Current pipeline state and stage
    public JobStatus Status { get; set; } = JobStatus.Queued;
    public PipelineStage Stage { get; set; } = PipelineStage.Pending;

    // Retry tracking for transient failures
    public int RetryCount { get; set; }
    public int MaxRetries { get; set; } = 3;

    // Diagnostic messages for failed jobs or human-in-the-loop review flags
    public string? ErrorMessage { get; set; }
    public string? ReviewNote { get; set; }

    // Structured output populated after the extraction stage
    public ExtractedInvoice? ExtractedData { get; set; }

    // Audit timestamps
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
}

/// <summary>
/// Normalized domain representation of an invoice extracted from a raw document.
/// Uses decimal for all monetary values to prevent floating-point rounding drift.
/// </summary>
public class ExtractedInvoice
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public DateOnly IssueDate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
}
