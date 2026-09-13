using OrderProcessingAPI.Domain.Enums;

namespace OrderProcessingAPI.Domain.Entities;

public class Shipment
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public ShipmentStatus Status { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
