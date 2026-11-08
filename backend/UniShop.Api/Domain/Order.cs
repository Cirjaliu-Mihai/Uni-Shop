namespace UniShop.Api.Domain;

public class Order
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public OrderStatus Status { get; set; } = OrderStatus.Processing;
    public string ContactName { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string ShippingAddressLabel { get; set; } = string.Empty;
    public string ShippingFullName { get; set; } = string.Empty;
    public string ShippingPhone { get; set; } = string.Empty;
    public string ShippingStreet { get; set; } = string.Empty;
    public string ShippingCity { get; set; } = string.Empty;
    public string ShippingCounty { get; set; } = string.Empty;
    public string ShippingPostalCode { get; set; } = string.Empty;
    public string ShippingCountry { get; set; } = string.Empty;
    public int DeliveryMethodId { get; set; }
    public DeliveryMethod DeliveryMethod { get; set; } = null!;
    public string DeliveryMethodName { get; set; } = string.Empty;
    public decimal DeliveryMethodPrice { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DeliveryCost { get; set; }
    public decimal Total { get; set; }
    public string? Notes { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public Payment Payment { get; set; } = null!;
}