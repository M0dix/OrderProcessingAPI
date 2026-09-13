using System.ComponentModel.DataAnnotations;

namespace OrderProcessingAPI.Contracts.Dtos;

public sealed class CreateOrderRequest
{
    [Required, EmailAddress]
    public string CustomerEmail { get; set; } = string.Empty;

    [Required]
    public string ProductSku { get; set; } = "SKU-BOOK";

    [Range(1, 100)]
    public int Quantity { get; set; } = 1;

    // Учебный флаг
    public bool FailPayment { get; set; }

    // Учебный флаг
    public bool FailShipment { get; set; }
}
