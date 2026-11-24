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

    // ObservableCollection automatically notifies the DataGrid when items are added or replaced
    public ObservableCollection<DocumentJob> Jobs { get; } = new();

    public MainViewModel(IDocumentQueue queue, IJobRepository repository, UiPipelineNotifier notifier)
    {
        _queue = queue;
        _repository = repository;
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
                StoragePath = info.FullName,
                FileSizeBytes = info.Exists ? info.Length : 0
            };

            await _repository.AddAsync(job);
            Jobs.Insert(0, job);
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
