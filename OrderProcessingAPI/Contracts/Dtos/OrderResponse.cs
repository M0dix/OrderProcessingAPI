using OrderProcessingAPI.Domain.Enums;

namespace OrderProcessingAPI.Contracts.Dtos;

public sealed class OrderResponse
{
    public Guid Id { get; init; }
    public string CustomerEmail { get; init; } = string.Empty;
    public string ProductSku { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal Amount { get; init; }
    public OrderStatus Status { get; init; }
    public string? FailureReason { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
    public ReservationDto? Reservation { get; init; }
    public PaymentDto? Payment { get; init; }
    public ShipmentDto? Shipment { get; init; }
}

public sealed class ReservationDto
{
    public Guid Id { get; init; }
    public string ProductSku { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public ReservationStatus Status { get; init; }
}

public sealed class PaymentDto
{
    public Guid Id { get; init; }
    public decimal Amount { get; init; }
    public PaymentStatus Status { get; init; }
    public string? FailureReason { get; init; }
}

public sealed class ShipmentDto
{
    public Guid Id { get; init; }
    public string TrackingNumber { get; init; } = string.Empty;
    public ShipmentStatus Status { get; init; }
}

public sealed class StockResponse
{
    public string Sku { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int Available { get; init; }
    public decimal Price { get; init; }
}

public sealed class OutboxMessageResponse
{
    public Guid Id { get; init; }
    public string Type { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? PublishedAtUtc { get; init; }
    public bool Published => PublishedAtUtc is not null;
}

public sealed class ReplayResult
{
    public Guid MessageId { get; init; }
    public bool Duplicate { get; init; }
    public string Message { get; init; } = string.Empty;
}
