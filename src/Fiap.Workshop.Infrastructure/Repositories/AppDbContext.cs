using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.Services;
using Fiap.Workshop.Domain.Abstractions;
using Fiap.Workshop.Infrastructure.Repositories.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace Fiap.Workshop.Infrastructure.Repositories;

[ExcludeFromCodeCoverage]
public sealed class AppDbContext(
    DbContextOptions<AppDbContext> options,
    IDomainEventDispatcher dispatcher,
    ILogger<AppDbContext> logger
) : DbContext(options), IUnitOfWork
{
    private readonly List<IDomainEvent> _pendingEvents = [];

    public DbSet<UserModel> Users => Set<UserModel>();
    public DbSet<CustomerModel> Customers => Set<CustomerModel>();
    public DbSet<VehicleModel> Vehicles => Set<VehicleModel>();
    public DbSet<InventoryItemModel> InventoryItems => Set<InventoryItemModel>();
    public DbSet<ServiceModel> Services => Set<ServiceModel>();
    public DbSet<ServiceOrderModel> ServiceOrders => Set<ServiceOrderModel>();

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
                .IsRequired();

            entity.Property(u => u.Role)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(u => u.CreatedAt)
                .IsRequired();

            entity.Property(u => u.UpdatedAt);
        });

        modelBuilder.Entity<CustomerModel>(entity =>
        {
            entity.ToTable("Customers");

            entity.HasKey(c => c.Id);

            entity.Property(c => c.Name)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(c => c.Document)
                .HasMaxLength(14)
                .IsRequired();

            entity.HasIndex(c => c.Document)
                .IsUnique();

            entity.Property(c => c.Email)
                .HasMaxLength(256)
                .IsRequired();

            entity.HasIndex(c => c.Email)
                .IsUnique();

            entity.Property(c => c.Phone)
                .HasMaxLength(20)
                .IsRequired();

            entity.HasIndex(c => c.Phone)
                .IsUnique();

            entity.Property(c => c.CreatedAt)
                .IsRequired();

            entity.Property(c => c.UpdatedAt);
        });

        modelBuilder.Entity<VehicleModel>(entity =>
        {
            entity.ToTable("Vehicles");

            entity.HasKey(v => v.Id);

            entity.Property(v => v.CustomerId)
                .IsRequired();

            entity.HasIndex(v => v.CustomerId);

            entity.HasOne<CustomerModel>()
                .WithMany()
                .HasForeignKey(v => v.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(v => v.LicensePlate)
                .HasMaxLength(10)
                .IsRequired();

            entity.HasIndex(v => v.LicensePlate)
                .IsUnique();

            entity.Property(v => v.Brand)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(v => v.Model)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(v => v.ManufactureYear)
                .IsRequired();

            entity.Property(v => v.ModelYear)
                .IsRequired();

            entity.Property(v => v.Color)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(v => v.CreatedAt)
                .IsRequired();

            entity.Property(v => v.UpdatedAt);
        });

        modelBuilder.Entity<InventoryItemModel>(entity =>
        {
            entity.ToTable("InventoryItems");

            entity.HasKey(i => i.Id);

            entity.Property(i => i.Code)
                .HasMaxLength(50)
                .IsRequired();

            entity.HasIndex(i => i.Code)
                .IsUnique();

            entity.Property(i => i.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(i => i.Description)
                .HasMaxLength(1000)
                .IsRequired();

            entity.Property(i => i.QuantityOnHand)
                .IsRequired();

            entity.Property(i => i.ReservedQuantity)
                .IsRequired();

            entity.Property(i => i.MinimumStock)
                .IsRequired();

            entity.Property(i => i.UnitPrice)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            entity.Property(i => i.UnitOfMeasure)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(i => i.IsActive)
                .IsRequired();

            entity.Property(i => i.CreatedAt)
                .IsRequired();

            entity.Property(i => i.UpdatedAt);
        });

        modelBuilder.Entity<ServiceModel>(entity =>
        {
            entity.ToTable("Services");

            entity.HasKey(s => s.Id);

            entity.Property(s => s.Code)
                .HasMaxLength(50)
                .IsRequired();

            entity.HasIndex(s => s.Code)
                .IsUnique();

            entity.Property(s => s.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(s => s.Description)
                .HasMaxLength(1000)
                .IsRequired();

            entity.Property(s => s.BasePrice)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            entity.Property(s => s.EstimatedDuration)
                .IsRequired();

            entity.Property(s => s.IsActive)
                .IsRequired();

            entity.Property(s => s.CreatedAt)
                .IsRequired();

            entity.Property(s => s.UpdatedAt);
        });

        modelBuilder.Entity<ServiceOrderModel>(entity =>
        {
            entity.ToTable("ServiceOrders");

            entity.HasKey(so => so.Id);

            entity.Property(so => so.Number)
                .IsRequired();

            entity.HasIndex(so => so.Number)
                .IsUnique();

            entity.Property(so => so.CustomerId)
                .IsRequired();

            entity.HasIndex(so => so.CustomerId);

            entity.HasOne<CustomerModel>()
                .WithMany()
                .HasForeignKey(so => so.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(so => so.VehicleId)
                .IsRequired();

            entity.HasIndex(so => so.VehicleId);

            entity.HasOne<VehicleModel>()
                .WithMany()
                .HasForeignKey(so => so.VehicleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(so => so.CreatedBy)
                .IsRequired();

            entity.HasIndex(so => so.CreatedBy);

            entity.HasOne<UserModel>()
                .WithMany()
                .HasForeignKey(so => so.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(so => so.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(so => so.ProblemDescription)
                .HasMaxLength(2000)
                .IsRequired();

            entity.Property(so => so.DiagnoseDescription)
                .HasMaxLength(2000);

            entity.Property(so => so.OdometerReading)
                .IsRequired();

            entity.Property(so => so.Discount)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            entity.Property(so => so.Subtotal)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            entity.Property(so => so.Total)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            entity.Property(so => so.OpenedAt)
                .IsRequired();

            entity.Property(so => so.ClosedAt);

            entity.Property(so => so.CreatedAt)
                .IsRequired();

            entity.Property(so => so.UpdatedAt);

            entity.HasMany(so => so.Parts)
                .WithOne()
                .HasForeignKey(p => p.ServiceOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(so => so.Services)
                .WithOne()
                .HasForeignKey(s => s.ServiceOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(so => so.StatusHistory)
                .WithOne()
                .HasForeignKey(h => h.ServiceOrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ServiceOrderPartModel>(entity =>
        {
            entity.ToTable("ServiceOrderParts");

            entity.HasKey(p => p.Id);

            entity.Property(p => p.InventoryItemId)
                .IsRequired();

            entity.HasIndex(p => p.InventoryItemId);

            entity.HasOne<InventoryItemModel>()
                .WithMany()
                .HasForeignKey(p => p.InventoryItemId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(p => p.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(p => p.Description)
                .HasMaxLength(1000)
                .IsRequired();

            entity.Property(p => p.UnitPrice)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            entity.Property(p => p.Quantity)
                .IsRequired();

            entity.Property(p => p.CreatedAt)
                .IsRequired();

            entity.Property(p => p.UpdatedAt);
        });

        modelBuilder.Entity<ServiceOrderServiceModel>(entity =>
        {
            entity.ToTable("ServiceOrderServices");

            entity.HasKey(s => s.Id);

            entity.Property(s => s.ServiceId)
                .IsRequired();

            entity.HasIndex(s => s.ServiceId);

            entity.HasOne<ServiceModel>()
                .WithMany()
                .HasForeignKey(s => s.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(s => s.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(s => s.Description)
                .HasMaxLength(1000)
                .IsRequired();

            entity.Property(s => s.UnitPrice)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            entity.Property(s => s.Quantity)
                .IsRequired();

            entity.Property(s => s.EstimatedDuration)
                .IsRequired();

            entity.Property(s => s.CreatedAt)
                .IsRequired();

            entity.Property(s => s.UpdatedAt);
        });

        modelBuilder.Entity<ServiceOrderStatusHistoryModel>(entity =>
        {
            entity.ToTable("ServiceOrderStatusHistories");

            entity.HasKey(h => h.Id);

            entity.Property(h => h.PreviousStatus)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(h => h.CurrentStatus)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(h => h.ChangedBy)
                .IsRequired();

            entity.HasIndex(h => h.ChangedBy);

            entity.HasOne<UserModel>()
                .WithMany()
                .HasForeignKey(h => h.ChangedBy)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(h => h.ChangedAt)
                .IsRequired();
        });
    }
}
