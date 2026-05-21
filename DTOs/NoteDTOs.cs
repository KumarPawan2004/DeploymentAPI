namespace DeploymentAPI.Models.DTOs;

public class UploadNoteDTO
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; } = 0;
}

public class NoteResponseDTO
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsFree { get; set; }
    public string Status { get; set; } = string.Empty;
    public int Downloads { get; set; }
    public int Views { get; set; }
    public string UploaderName { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
}

public class ReviewNoteDTO
{
    public string Status { get; set; } = string.Empty; // Approved or Rejected
    public string? RejectionReason { get; set; }
}

public class UpdateUserStatusDTO
{
    public bool IsBlocked { get; set; }
}