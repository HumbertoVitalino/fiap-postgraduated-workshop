using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.Services;
using Fiap.Workshop.Domain.Abstractions;
using Fiap.Workshop.Infrastructure.Repositories.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Fiap.Workshop.Infrastructure.Repositories;

public sealed class AppDbContext(
    DbContextOptions<AppDbContext> options,
    IDomainEventDispatcher dispatcher,
    ILogger<AppDbContext> logger
) : DbContext(options), IUnitOfWork
{
    private readonly List<IDomainEvent> _pendingEvents = [];

    public DbSet<UserModel> Users => Set<UserModel>();

    internal void EnqueueDomainEvents(IEnumerable<IDomainEvent> events) =>
        _pendingEvents.AddRange(events);

    public async Task<bool> CommitAsync(CancellationToken cancellationToken = default)
    {
        bool result;

        try
        {
            result = await base.SaveChangesAsync(cancellationToken) > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to commit changes to the database.");
            return false;
        }

        if (_pendingEvents.Count > 0)
        {
            var events = _pendingEvents.ToList();
            _pendingEvents.Clear();

            try
            {
                await dispatcher.DispatchAsync(events, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to dispatch domain events after commit.");
            }
        }

        return result;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserModel>(entity =>
        {
            entity.ToTable("Users");

            entity.HasKey(u => u.Id);

            entity.Property(u => u.Email)
                .HasMaxLength(256)
                .IsRequired();

            entity.HasIndex(u => u.Email)
                .IsUnique();

            entity.Property(u => u.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(u => u.Password)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(u => u.Role)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(u => u.CreatedAt)
                .IsRequired();

            entity.Property(u => u.UpdatedAt);
        });
    }
}
