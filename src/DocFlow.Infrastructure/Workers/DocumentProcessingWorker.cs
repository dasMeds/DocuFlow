using DocFlow.Core.Enums;
using DocFlow.Core.Interfaces;
using DocFlow.Core.Validation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DocFlow.Infrastructure.Workers;

/// <summary>
/// Long-running background service that pulls queued JobIds, orchestrates extraction
/// and validation stages, broadcasts progress, and manages exponential retry backoff.
/// </summary>
public sealed class DocumentProcessingWorker : BackgroundService
{
    private readonly IDocumentQueue _queue;
    private readonly IJobRepository _repository;
    private readonly IDocumentProcessorFactory _processorFactory;
    private readonly IInvoiceFieldExtractor _fieldExtractor;
    private readonly IPipelineNotifier _notifier;
    private readonly ILogger<DocumentProcessingWorker> _logger;

    /// <summary>
    /// Creates a worker with queue, persistence, parsing, and notification services.
    /// </summary>
    public DocumentProcessingWorker(
        IDocumentQueue queue,
        IJobRepository repository,
        IDocumentProcessorFactory processorFactory,
        IInvoiceFieldExtractor fieldExtractor,
        IPipelineNotifier notifier,
        ILogger<DocumentProcessingWorker> logger)
    {
        _queue = queue;
        _repository = repository;
        _processorFactory = processorFactory;
        _fieldExtractor = fieldExtractor;
        _notifier = notifier;
        _logger = logger;
    }

    /// <summary>
    /// Dequeues jobs, extracts invoice fields, validates results, and retries transient failures.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DocFlow Background Worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            // Asynchronously wait for the next enqueued job ID
            var jobId = await _queue.DequeueAsync(stoppingToken);
            var job = await _repository.GetByIdAsync(jobId, stoppingToken);
            if (job is null) continue;

            try
            {
                // Stage 1: Transition to Processing -> Extracting
                job.Status = JobStatus.Processing;
                job.Stage = PipelineStage.Extracting;
                await _repository.UpdateAsync(job, stoppingToken);
                await _notifier.NotifyJobUpdatedAsync(job, stoppingToken);

                var extension = string.IsNullOrWhiteSpace(job.FileType)
                    ? Path.GetExtension(job.FileName)
                    : job.FileType;
                var processor = _processorFactory.GetProcessor(extension);
                var extraction = await processor.ExtractTextAsync(job.StoragePath, stoppingToken);
                job.RawTextSnippet = extraction.Text.Length <= 500
                    ? extraction.Text
                    : extraction.Text[..500];
                job.ProcessingDiagnostic = extraction.Diagnostic;
                var extracted = _fieldExtractor.Extract(extraction.Text);
                job.ExtractedData = extracted;

                // Stage 2: Validate extracted fields against business rules
                job.Stage = PipelineStage.Validating;
                await _repository.UpdateAsync(job, stoppingToken);
                await _notifier.NotifyJobUpdatedAsync(job, stoppingToken);

                var (isValid, issue) = InvoiceValidator.Validate(extracted);
                if (!isValid || extraction.Diagnostic is not null)
                {
                    // Route discrepancies to human-in-the-loop review rather than failing hard
                    job.Status = JobStatus.NeedsReview;
                    job.ReviewNote = extraction.Diagnostic ?? issue ?? "Document extraction requires manual review.";
                    job.Stage = PipelineStage.Done;
                }
                else
                {
                    // Stage 3: Finalize and mark job Completed
                    job.Stage = PipelineStage.Saving;
                    await _repository.UpdateAsync(job, stoppingToken);
                    job.Status = JobStatus.Completed;
                    job.Stage = PipelineStage.Done;
                    job.CompletedAt = DateTimeOffset.UtcNow;
                    job.ErrorMessage = null;
                    job.ReviewNote = null;
                }

                await _repository.UpdateAsync(job, stoppingToken);
                await _notifier.NotifyJobUpdatedAsync(job, stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Job {JobId} failed on attempt {Attempt}", job.Id, job.RetryCount + 1);
                job.RetryCount++;
                job.ErrorMessage = ex.Message;

                if (job.RetryCount < job.MaxRetries)
                {
                    // Re-queue with exponential backoff (2s, 4s, ...) for transient errors
                    job.Status = JobStatus.Queued;
                    job.Stage = PipelineStage.Pending;
                    await _repository.UpdateAsync(job, stoppingToken);
                    await _notifier.NotifyJobUpdatedAsync(job, stoppingToken);

                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, job.RetryCount)), stoppingToken);
                    await _queue.EnqueueAsync(job.Id, stoppingToken);
                }
                else
                {
                    // Exhausted retries: isolate job as Failed so other items keep flowing
                    job.Status = JobStatus.Failed;
                    job.Stage = PipelineStage.Done;
                    await _repository.UpdateAsync(job, stoppingToken);
                    await _notifier.NotifyJobUpdatedAsync(job, stoppingToken);
                }
            }
        }
    }
}
