using Microsoft.EntityFrameworkCore;
using OrderProcessingAPI.Domain.Entities;
using OrderProcessingAPI.Infrastructure.Persistence;

namespace OrderProcessingAPI.Repositories;

public sealed class InventoryRepository(AppDbContext db) : IInventoryRepository
{
    public Task<ProductStock?> GetStockAsync(string sku, CancellationToken cancellationToken)
        => db.ProductStocks.FirstOrDefaultAsync(x => x.Sku == sku, cancellationToken);

    public async Task<IReadOnlyList<ProductStock>> GetAllStockAsync(CancellationToken cancellationToken)
        => await db.ProductStocks.OrderBy(x => x.Sku).ToListAsync(cancellationToken);

    public Task<InventoryReservation?> GetReservationByOrderIdAsync(Guid orderId, CancellationToken cancellationToken)
        => db.InventoryReservations.FirstOrDefaultAsync(x => x.OrderId == orderId, cancellationToken);

    public Task AddReservationAsync(InventoryReservation reservation, CancellationToken cancellationToken)
    {
        db.InventoryReservations.Add(reservation);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => db.SaveChangesAsync(cancellationToken);
}
