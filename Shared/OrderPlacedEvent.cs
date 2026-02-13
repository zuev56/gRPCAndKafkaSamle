namespace Shared;

public class OrderPlacedEvent
{
    public string OrderId { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public decimal Total { get; set; }
    public List<OrderItem> Items { get; set; } = [];
    public DateTime Timestamp { get; set; }
    public string PaymentId { get; set; } = null!;
}