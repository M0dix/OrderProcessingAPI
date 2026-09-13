using OrderProcessingAPI.Domain.Entities;

namespace OrderProcessingAPI.Repositories;

public interface IInventoryRepository
{
    Task<ProductStock?> GetStockAsync(string sku, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductStock>> GetAllStockAsync(CancellationToken cancellationToken);
    Task<InventoryReservation?> GetReservationByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);
    Task AddReservationAsync(InventoryReservation reservation, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
