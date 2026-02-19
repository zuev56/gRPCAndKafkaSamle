namespace Producer.Shared.Models;

public sealed class OrderItemDto
{
    public string ProductId { get; set; } = null!;
    public int Quantity { get; set; }
}