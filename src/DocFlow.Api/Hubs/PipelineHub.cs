using DocFlow.Core.Interfaces;
using DocFlow.Core.Models;
using Microsoft.AspNetCore.SignalR;

namespace DocFlow.Api.Hubs;

/// <summary>
/// SignalR hub enabling real-time push notifications to connected browser clients.
/// </summary>
public sealed class PipelineHub : Hub { }

/// <summary>
/// Bridges core worker events to SignalR WebSocket broadcasts.
/// </summary>
public sealed class SignalRPipelineNotifier : IPipelineNotifier
{
    private readonly IHubContext<PipelineHub> _hubContext;

    public SignalRPipelineNotifier(IHubContext<PipelineHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotifyJobUpdatedAsync(DocumentJob job, CancellationToken ct = default)
        => _hubContext.Clients.All.SendAsync("JobUpdated", job, ct);
}
