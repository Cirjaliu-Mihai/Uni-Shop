using Microsoft.AspNetCore.Identity;

namespace UniShop.Api.Domain;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();
    public Cart? Cart { get; set; }
    public ICollection<Address> Addresses { get; set; } = new List<Address>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}