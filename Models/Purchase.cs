namespace DeploymentAPI.Models;

public class Purchase
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int NoteId { get; set; }
    public Note Note { get; set; } = null!;
    public decimal Amount { get; set; }
    public string TransactionId { get; set; } = Guid.NewGuid().ToString();
    public DateTime PurchasedAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Completed"; // Completed, Refunded
}