namespace UniShop.Api.Domain;

public class DeliveryMethod
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int EstimatedDays { get; set; }
    public ICollection<Product> Products { get; set; } = new List<Product>();
}