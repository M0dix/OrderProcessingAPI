namespace OrderProcessingAPI.Domain.Entities;

public class ProcessedMessage
{
    public Guid MessageId { get; set; }
    public string Handler { get; set; } = string.Empty;
    public DateTime ProcessedAtUtc { get; set; }
}
