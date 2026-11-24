using System.Threading.Channels;
using DocFlow.Core.Interfaces;

namespace DocFlow.Infrastructure.Queue;

/// <summary>
/// Thread-safe, bounded producer/consumer queue backed by System.Threading.Channels.
/// </summary>
public sealed class ChannelDocumentQueue : IDocumentQueue
{
    private readonly Channel<Guid> _channel;

    public ChannelDocumentQueue(int capacity = 500)
    {
        // Bounded capacity applies backpressure if intake spikes beyond buffer limits
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _channel = Channel.CreateBounded<Guid>(options);
    }

    public ValueTask EnqueueAsync(Guid jobId, CancellationToken ct = default)
        => _channel.Writer.WriteAsync(jobId, ct);

    public ValueTask<Guid> DequeueAsync(CancellationToken ct = default)
        => _channel.Reader.ReadAsync(ct);
}
