using System.Collections.ObjectModel;
using System.IO;
using DocFlow.Core.Interfaces;
using DocFlow.Core.Models;
using DocFlow.Desktop.Services;

namespace DocFlow.Desktop.ViewModels;

/// <summary>
/// Presentation state and commands for the WPF workbench, decoupled from XAML code-behind.
/// </summary>
public sealed class MainViewModel
{
    private readonly IDocumentQueue _queue;
    private readonly IJobRepository _repository;
    private readonly IDocumentProcessorFactory _processorFactory;

    // ObservableCollection automatically notifies the DataGrid when items are added or replaced
    public ObservableCollection<DocumentJob> Jobs { get; } = new();

    /// <summary>
    /// Creates the desktop view model and subscribes to worker status changes.
    /// </summary>
    public MainViewModel(
        IDocumentQueue queue,
        IJobRepository repository,
        IDocumentProcessorFactory processorFactory,
        UiPipelineNotifier notifier)
    {
        _queue = queue;
        _repository = repository;
        _processorFactory = processorFactory;
        notifier.JobUpdated += OnJobUpdated;
    }

    /// <summary>
    /// Creates Queued job records for selected/dropped files and pushes them to the background channel.
    /// </summary>
    public async Task EnqueueFilesAsync(IEnumerable<string> filePaths)
    {
        foreach (var path in filePaths)
        {
            var info = new FileInfo(path);
            var job = new DocumentJob
            {
                FileName = info.Name,
                FileType = Path.GetExtension(info.Name).ToLowerInvariant(),
                StoragePath = info.FullName,
                FileSizeBytes = info.Exists ? info.Length : 0
            };

            if (!_processorFactory.Supports(job.FileType))
            {
                job.Status = DocFlow.Core.Enums.JobStatus.Failed;
                job.Stage = DocFlow.Core.Enums.PipelineStage.Done;
                job.ErrorMessage = $"Unsupported file format: {job.FileType}";
                job.ReviewNote = job.ErrorMessage;
                job.CompletedAt = DateTimeOffset.UtcNow;
            }

            await _repository.AddAsync(job);
            Jobs.Insert(0, job);
            if (job.Status == DocFlow.Core.Enums.JobStatus.Queued)
                await _queue.EnqueueAsync(job.Id);
        }
    }

    private void OnJobUpdated(DocumentJob updated)
    {
        // Replace the updated job entry so DataGrid row bindings refresh cleanly
        var idx = -1;
        for (int i = 0; i < Jobs.Count; i++)
        {
            if (Jobs[i].Id == updated.Id) { idx = i; break; }
        }
        if (idx >= 0) Jobs[idx] = updated;
    }
}
