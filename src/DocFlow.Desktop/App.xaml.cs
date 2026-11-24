using System.Windows;
using DocFlow.Core.Interfaces;
using DocFlow.Desktop.Services;
using DocFlow.Desktop.ViewModels;
using DocFlow.Infrastructure.Persistence;
using DocFlow.Infrastructure.Processors;
using DocFlow.Infrastructure.Queue;
using DocFlow.Infrastructure.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DocFlow.Desktop;

/// <summary>
/// Configures .NET Generic Host inside the WPF desktop app so background workers
/// and dependency injection run cleanly alongside the UI thread.
/// </summary>
public partial class App : Application
{
    private readonly IHost _host;

    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                // Register shared pipeline queue, repository, processor, and background worker
                services.AddSingleton<IDocumentQueue, ChannelDocumentQueue>();
                services.AddSingleton<IJobRepository, InMemoryJobRepository>();
                services.AddSingleton<IDocumentProcessor, TextRuleDocumentProcessor>();
                services.AddSingleton<UiPipelineNotifier>();
                services.AddSingleton<IPipelineNotifier>(sp => sp.GetRequiredService<UiPipelineNotifier>());
                services.AddHostedService<DocumentProcessingWorker>();

                // Register MVVM ViewModel and main window
                services.AddSingleton<MainViewModel>();
                services.AddSingleton<MainWindow>();
            })
            .Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        // Start background workers before displaying the main window
        await _host.StartAsync();
        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        // Gracefully stop background workers when the desktop app closes
        await _host.StopAsync();
        _host.Dispose();
        base.OnExit(e);
    }
}
