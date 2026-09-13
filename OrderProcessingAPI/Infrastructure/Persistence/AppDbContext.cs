using Microsoft.EntityFrameworkCore;
using OrderProcessingAPI.Domain.Entities;

namespace OrderProcessingAPI.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();
    public DbSet<ProductStock> ProductStocks => Set<ProductStock>();
    public DbSet<InventoryReservation> InventoryReservations => Set<InventoryReservation>();
    public DbSet<PaymentRecord> Payments => Set<PaymentRecord>();
    public DbSet<Shipment> Shipments => Set<Shipment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CustomerEmail).HasMaxLength(256).IsRequired();
            entity.Property(x => x.ProductSku).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.HasIndex(x => x.Status);
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("outbox");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Type).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Payload).IsRequired();
            entity.HasIndex(x => x.PublishedAtUtc);
        });

        modelBuilder.Entity<ProcessedMessage>(entity =>
        {
            entity.HasKey(x => new { x.MessageId, x.Handler });
            entity.Property(x => x.Handler).HasMaxLength(128).IsRequired();
        });

        modelBuilder.Entity<ProductStock>(entity =>
        {
            entity.HasKey(x => x.Sku);
            entity.Property(x => x.Sku).HasMaxLength(64);
            entity.Property(x => x.Name).HasMaxLength(256).IsRequired();
            entity.Property(x => x.Price).HasPrecision(18, 2);
        });

        modelBuilder.Entity<InventoryReservation>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ProductSku).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.HasIndex(x => x.OrderId).IsUnique();
        });

        modelBuilder.Entity<PaymentRecord>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.HasIndex(x => x.OrderId).IsUnique();
        });

        modelBuilder.Entity<Shipment>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TrackingNumber).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.HasIndex(x => x.OrderId).IsUnique();
        });
    }
}
