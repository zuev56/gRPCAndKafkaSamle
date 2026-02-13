namespace Producer.Shared.Models;

public sealed class PlaceOrderRequest
{
    public string UserId { get; set; } = null!;
    public int Total { get; set; }
    public List<OrderItemDto> Items { get; set; } = [];
}