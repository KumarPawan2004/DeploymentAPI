namespace DeploymentAPI.Models;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "User"; // "User" or "Admin"
    public bool IsBlocked { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    // Navigation properties
    public ICollection<Note> Notes { get; set; } = new List<Note>();
    public ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
    public ICollection<Wishlist> WishlistItems { get; set; } = new List<Wishlist>();
}