using System.Diagnostics.CodeAnalysis;
using Aiursoft.EventsRecorder.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.EventsRecorder.Sqlite;

[ExcludeFromCodeCoverage]

public class SqliteContext(DbContextOptions<SqliteContext> options) : TemplateDbContext(options)
{
    public override Task<bool> CanConnectAsync()
    {
        return Task.FromResult(true);
    }
}
