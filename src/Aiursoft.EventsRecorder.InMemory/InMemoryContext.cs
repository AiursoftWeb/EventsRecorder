using Aiursoft.EventsRecorder.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.EventsRecorder.InMemory;

public class InMemoryContext(DbContextOptions<InMemoryContext> options) : EventsRecorderDbContext(options)
{
    public override Task MigrateAsync(CancellationToken cancellationToken)
    {
        return Database.EnsureCreatedAsync(cancellationToken);
    }

    public override Task<bool> CanConnectAsync()
    {
        return Task.FromResult(true);
    }
}
