using OrderProcessingAPI.Domain.Entities;

namespace OrderProcessingAPI.Services;

public interface IInventoryService
{
    Task<ProductStock?> GetStockAsync(string sku, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductStock>> GetAllStockAsync(CancellationToken cancellationToken);
    Task<InventoryReservation?> GetReservationAsync(Guid orderId, CancellationToken cancellationToken);
    Task<InventoryReservation> ReserveAsync(Order order, CancellationToken cancellationToken);
    Task ReleaseAsync(Guid orderId, CancellationToken cancellationToken);
}
