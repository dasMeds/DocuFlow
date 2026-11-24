using System.Collections.Concurrent;
using DocFlow.Core.Interfaces;
using DocFlow.Core.Models;

namespace DocFlow.Infrastructure.Persistence;

/// <summary>
/// Thread-safe in-memory repository for fast local execution and prototyping.
/// Can be replaced with an EF Core / SQLite or PostgreSQL implementation without changing workers.
/// </summary>
public sealed class InMemoryJobRepository : IJobRepository
{
    private readonly ConcurrentDictionary<Guid, DocumentJob> _store = new();

    public Task AddAsync(DocumentJob job, CancellationToken ct = default)
    {
        _store[job.Id] = job;
        return Task.CompletedTask;
    }

    public Task<DocumentJob?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _store.TryGetValue(id, out var job);
        return Task.FromResult(job);
    }

    public Task<IReadOnlyList<DocumentJob>> GetAllAsync(CancellationToken ct = default)
    {
        // Return newest submissions first for UI display
        IReadOnlyList<DocumentJob> list = _store.Values
            .OrderByDescending(j => j.CreatedAt)
            .ToList();
        return Task.FromResult(list);
    }

    public Task UpdateAsync(DocumentJob job, CancellationToken ct = default)
    {
        _store[job.Id] = job;
        return Task.CompletedTask;
    }
}
