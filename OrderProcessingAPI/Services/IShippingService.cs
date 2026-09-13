using OrderProcessingAPI.Domain.Entities;

namespace OrderProcessingAPI.Services;

public interface IShippingService
{
    Task<Shipment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);
    Task<Shipment> CreateAsync(Order order, CancellationToken cancellationToken);
    Task CancelAsync(Guid orderId, CancellationToken cancellationToken);
}
