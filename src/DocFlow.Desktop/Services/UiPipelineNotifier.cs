using System.Windows;
using DocFlow.Core.Interfaces;
using DocFlow.Core.Models;

namespace DocFlow.Desktop.Services;

/// <summary>
/// Marshals job update events from background worker threads back onto the WPF UI thread.
/// </summary>
public sealed class UiPipelineNotifier : IPipelineNotifier
{
    public event Action<DocumentJob>? JobUpdated;

    public Task NotifyJobUpdatedAsync(DocumentJob job, CancellationToken ct = default)
    {
        // Dispatcher.InvokeAsync prevents cross-thread UI exceptions
        Application.Current?.Dispatcher.InvokeAsync(() => JobUpdated?.Invoke(job));
        return Task.CompletedTask;
    }
}
