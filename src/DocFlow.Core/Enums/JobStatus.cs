namespace DocFlow.Core.Enums;

/// <summary>
/// High-level lifecycle states for a document job in the pipeline.
/// </summary>
public enum JobStatus
{
    /// <summary>Accepted by intake and waiting in the background queue.</summary>
    Queued,
    /// <summary>Actively being handled by a background worker.</summary>
    Processing,
    /// <summary>Extracted successfully but flagged for human verification.</summary>
    NeedsReview,
    /// <summary>Passed validation and saved cleanly.</summary>
    Completed,
    /// <summary>Exhausted retries or encountered an unrecoverable error.</summary>
    Failed
}

/// <summary>
/// Fine-grained processing steps inside a worker execution cycle.
/// </summary>
public enum PipelineStage
{
    /// <summary>Not yet picked up by a worker.</summary>
    Pending,
    /// <summary>Parsing text, fields, or line items from the raw file.</summary>
    Extracting,
    /// <summary>Running business and financial accuracy checks.</summary>
    Validating,
    /// <summary>Persisting validated output to storage.</summary>
    Saving,
    /// <summary>Terminal stage reached.</summary>
    Done
}
