using System.Threading.Channels;
using DocFlow.Core.Interfaces;

namespace DocFlow.Infrastructure.Queue;

/// <summary>
/// Thread-safe, bounded producer/consumer queue backed by System.Threading.Channels.
/// </summary>
public sealed class ChannelDocumentQueue : IDocumentQueue
{
    private readonly Channel<Guid> _channel;

    /// <summary>
    /// Creates a bounded queue with backpressure when its configured capacity is reached.
    /// </summary>
    public ChannelDocumentQueue(int capacity = 500)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);

        // Bounded capacity applies backpressure if intake spikes beyond buffer limits
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            AllowSynchronousContinuations = false
        };
        _channel = Channel.CreateBounded<Guid>(options);
    }

    /// <inheritdoc />
    public ValueTask EnqueueAsync(Guid jobId, CancellationToken ct = default)
        => _channel.Writer.WriteAsync(jobId, ct);

    /// <inheritdoc />
    public ValueTask<Guid> DequeueAsync(CancellationToken ct = default)
        => _channel.Reader.ReadAsync(ct);
}
