using Fiap.Workshop.Application.Interfaces;
using Fiap.Workshop.Application.Interfaces.Services;
using Fiap.Workshop.Domain.Abstractions;
using Fiap.Workshop.Domain.Users;
using Fiap.Workshop.Infrastructure.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace Fiap.Workshop.Infrastructure.Repositories;

public sealed class AppDbContext(
    DbContextOptions<AppDbContext> options,
    IDomainEventDispatcher? dispatcher = null
) : DbContext(options), IUnitOfWork
{
    private readonly List<IDomainEvent> _pendingEvents = [];

    public DbSet<UserModel> Users => Set<UserModel>();

    internal void EnqueueDomainEvents(IEnumerable<IDomainEvent> events) =>
        _pendingEvents.AddRange(events);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserModel>(entity =>
        {
            entity.ToTable("Users");

            entity.HasKey(u => u.Id);

            entity.Property(u => u.Email)
                .HasMaxLength(Email.MaxLength)
                .IsRequired();

            entity.HasIndex(u => u.Email)
                .IsUnique();

            entity.Property(u => u.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(u => u.Role)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(u => u.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()")
                .IsRequired();
        });
    }

    public async Task<bool> CommitAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await base.SaveChangesAsync(cancellationToken) > 0;

            if (dispatcher is not null && _pendingEvents.Count > 0)
            {
                var events = _pendingEvents.ToList();
                _pendingEvents.Clear();
                await dispatcher.DispatchAsync(events, cancellationToken);
            }

            return result;
        }
        catch
        {
            return false;
        }
    }
}
