namespace DeploymentAPI.Models;

public class Note
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty; // Path to PDF file
    public string FileName { get; set; } = string.Empty;
    public decimal Price { get; set; } = 0; // 0 = free
    public bool IsFree => Price == 0;
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
    public string? RejectionReason { get; set; }
    public int Downloads { get; set; } = 0;
    public int Views { get; set; } = 0;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }

    // Foreign keys
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int? ReviewedBy { get; set; }

    // Navigation properties
    public ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
    public ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
}