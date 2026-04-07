namespace Daylifood.Models;

public class Order
{
    public int Id { get; set; }
    public string BuyerId { get; set; } = string.Empty;
    public ApplicationUser Buyer { get; set; } = null!;
    public string? ShipperId { get; set; }
    public ApplicationUser? Shipper { get; set; }
    public int DeliveryAreaId { get; set; }
    public DeliveryArea DeliveryArea { get; set; } = null!;
    public decimal TotalPrice { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public string DeliveryAddress { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cod;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;
    public string? PaymentReference { get; set; }
    public string? PaymentGatewayTransactionNo { get; set; }
    public string? PaymentGatewayResponseCode { get; set; }
    public DateTime? PaidAt { get; set; }
    public bool InventoryCommitted { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
