using System.Diagnostics.CodeAnalysis;
using Aiursoft.DbTools;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.EventsRecorder.Entities;

[ExcludeFromCodeCoverage]

public abstract class EventsRecorderDbContext(DbContextOptions options) : IdentityDbContext<User>(options), ICanMigrate
{
    public DbSet<GlobalSetting> GlobalSettings => Set<GlobalSetting>();

    public DbSet<EventType> EventTypes => Set<EventType>();

    public DbSet<EventField> EventFields => Set<EventField>();

    public DbSet<EventRecord> EventRecords => Set<EventRecord>();

    public DbSet<EventFieldValue> EventFieldValues => Set<EventFieldValue>();

    public DbSet<PluginConfig> PluginConfigs => Set<PluginConfig>();

    public virtual Task MigrateAsync(CancellationToken cancellationToken) =>
        Database.MigrateAsync(cancellationToken);

    public virtual Task<bool> CanConnectAsync() =>
        Database.CanConnectAsync();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<EventType>(entity =>
        {
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<EventField>(entity =>
        {
            entity.HasOne(e => e.EventType)
                .WithMany(t => t.Fields)
                .HasForeignKey(e => e.EventTypeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<EventRecord>(entity =>
        {
            entity.HasOne(e => e.EventType)
                .WithMany(t => t.Records)
                .HasForeignKey(e => e.EventTypeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<EventFieldValue>(entity =>
        {
            entity.HasOne(e => e.EventRecord)
                .WithMany(r => r.FieldValues)
                .HasForeignKey(e => e.EventRecordId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.EventField)
                .WithMany()
                .HasForeignKey(e => e.EventFieldId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PluginConfig>(entity =>
        {
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.UserId, e.PluginId }).IsUnique();
        });
    }
}
