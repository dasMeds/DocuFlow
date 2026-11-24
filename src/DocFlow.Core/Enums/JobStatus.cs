namespace DocFlow.Core.Enums;

/// <summary>
/// High-level lifecycle states for a document job in the pipeline.
/// </summary>
public enum JobStatus
{
    Queued,       // Accepted by intake and waiting in the background queue
    Processing,   // Actively being handled by a background worker
    NeedsReview,  // Extracted successfully, but flagged for human verification
    Completed,    // Passed validation and saved cleanly
    Failed        // Exhausted retries or encountered an unrecoverable error
}

/// <summary>
/// Fine-grained processing steps inside a worker execution cycle.
/// </summary>
public enum PipelineStage
{
    Pending,      // Not yet picked up by a worker
    Extracting,   // Parsing text, fields, or line items from the raw file
    Validating,   // Running business and financial accuracy checks
    Saving,       // Persisting validated output to storage/database
    Done          // Terminal stage reached (Completed, NeedsReview, or Failed)
}
