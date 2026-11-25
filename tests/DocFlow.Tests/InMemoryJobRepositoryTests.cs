using DocFlow.Core.Interfaces;
using DocFlow.Core.Models;
using DocFlow.Infrastructure.Persistence;
using Xunit;

namespace DocFlow.Tests;

/// <summary>
/// Verifies that callers cannot mutate repository state through returned object references.
/// </summary>
public sealed class InMemoryJobRepositoryTests
{
    [Fact]
    public async Task GetByIdAsync_ReturnsAnIndependentCopy()
    {
        IJobRepository repository = new InMemoryJobRepository();
        var job = new DocumentJob { FileName = "original.txt" };
        await repository.AddAsync(job);

        var returned = await repository.GetByIdAsync(job.Id);
        returned!.FileName = "mutated.txt";

        var stored = await repository.GetByIdAsync(job.Id);
        Assert.Equal("original.txt", stored!.FileName);
    }
}
