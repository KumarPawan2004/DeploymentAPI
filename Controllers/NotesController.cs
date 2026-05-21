using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using DeploymentAPI.Data;
using DeploymentAPI.Models;
using DeploymentAPI.Models.DTOs;

namespace DeploymentAPI.Controllers;

[ApiController]
[Route("api/notes")]
[Authorize]
public class NotesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public NotesController(AppDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    // Get all approved notes (for browsing)
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<NoteResponseDTO>>> GetNotes(
        [FromQuery] string? category,
        [FromQuery] string? subject,
        [FromQuery] string? search,
        [FromQuery] bool? free)
    {
        var query = _context.Notes
            .Include(n => n.User)
            .Where(n => n.Status == "Approved");

        if (!string.IsNullOrEmpty(category))
            query = query.Where(n => n.Category == category);

        if (!string.IsNullOrEmpty(subject))
            query = query.Where(n => n.Subject == subject);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(n => n.Title.Contains(search) || n.Description.Contains(search));

        if (free == true)
            query = query.Where(n => n.IsFree);
        else if (free == false)
            query = query.Where(n => !n.IsFree);

        var notes = await query
            .OrderByDescending(n => n.UploadedAt)
            .Select(n => new NoteResponseDTO
            {
                Id = n.Id,
                Title = n.Title,
                Description = n.Description,
                Subject = n.Subject,
                Category = n.Category,
                Price = n.Price,
                IsFree = n.IsFree,
                Status = n.Status,
                Downloads = n.Downloads,
                Views = n.Views,
                UploaderName = n.User.FullName,
                UploadedAt = n.UploadedAt
            })
            .ToListAsync();

        return Ok(notes);
    }

    // Get note details
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<NoteResponseDTO>> GetNote(int id)
    {
        var note = await _context.Notes
            .Include(n => n.User)
            .FirstOrDefaultAsync(n => n.Id == id);

        if (note == null)
            return NotFound("Note not found");

        // Increment views
        note.Views++;
        await _context.SaveChangesAsync();

        return Ok(new NoteResponseDTO
        {
            Id = note.Id,
            Title = note.Title,
            Description = note.Description,
            Subject = note.Subject,
            Category = note.Category,
            Price = note.Price,
            IsFree = note.IsFree,
            Status = note.Status,
            Downloads = note.Downloads,
            Views = note.Views,
            UploaderName = note.User.FullName,
            UploadedAt = note.UploadedAt
        });
    }

    // Upload note
    [HttpPost("upload")]
    public async Task<ActionResult> UploadNote([FromForm] UploadNoteDTO noteData, IFormFile file)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        if (userId == 0)
            return Unauthorized();

        if (file == null || file.Length == 0)
            return BadRequest("File is required");

        if (!file.FileName.EndsWith(".pdf"))
            return BadRequest("Only PDF files are allowed");

        if (file.Length > 10 * 1024 * 1024) // 10MB limit
            return BadRequest("File size must be less than 10MB");

        // Create uploads directory if not exists
        var uploadsDir = Path.Combine(_environment.WebRootPath ?? "wwwroot", "uploads");
        if (!Directory.Exists(uploadsDir))
            Directory.CreateDirectory(uploadsDir);

        // Generate unique filename
        var fileName = $"{Guid.NewGuid()}_{file.FileName}";
        var filePath = Path.Combine(uploadsDir, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var note = new Note
        {
            Title = noteData.Title,
            Description = noteData.Description,
            Subject = noteData.Subject,
            Category = noteData.Category,
            Price = noteData.Price,
            FilePath = $"/uploads/{fileName}",
            FileName = file.FileName,
            UserId = userId,
            Status = "Pending"
        };

        _context.Notes.Add(note);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Note uploaded successfully. Awaiting admin review.", noteId = note.Id });
    }

    // Download note (for free or purchased notes)
    [HttpGet("download/{id}")]
    public async Task<IActionResult> DownloadNote(int id)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        var note = await _context.Notes
            .Include(n => n.Purchases)
            .FirstOrDefaultAsync(n => n.Id == id);

        if (note == null)
            return NotFound("Note not found");

        // Fetch user to check if they are an admin
        var user = await _context.Users.FindAsync(userId);
        var isAdmin = user?.Role == "Admin";

        if (note.Status != "Approved" && !isAdmin)
            return BadRequest("Note is not available for download");

        // Check if user has access (free note, purchased, owner, or admin)
        var hasAccess = isAdmin ||
                        note.IsFree ||
                        note.UserId == userId ||
                        await _context.Purchases.AnyAsync(p => p.NoteId == id && p.UserId == userId);

        if (!hasAccess)
            return BadRequest("You haven't purchased this note");

        // Increment download count (only for non-admins to avoid polluting download stats)
        if (!isAdmin)
        {
            note.Downloads++;
            await _context.SaveChangesAsync();
        }

        var filePath = Path.Combine(_environment.WebRootPath ?? "wwwroot", note.FilePath.TrimStart('/'));
        if (!System.IO.File.Exists(filePath))
            return NotFound("File not found");

        var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
        return File(fileBytes, "application/pdf", note.FileName);
    }

    // Get user's uploaded notes
    [HttpGet("my-uploads")]
    public async Task<ActionResult<IEnumerable<NoteResponseDTO>>> GetMyUploads()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        var notes = await _context.Notes
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.UploadedAt)
            .Select(n => new NoteResponseDTO
            {
                Id = n.Id,
                Title = n.Title,
                Description = n.Description,
                Subject = n.Subject,
                Category = n.Category,
                Price = n.Price,
                IsFree = n.IsFree,
                Status = n.Status,
                Downloads = n.Downloads,
                Views = n.Views,
                UploaderName = n.User.FullName,
                UploadedAt = n.UploadedAt
            })
            .ToListAsync();

        return Ok(notes);
    }

    // Get user's purchased notes
    [HttpGet("purchased")]
    public async Task<ActionResult<IEnumerable<NoteResponseDTO>>> GetPurchasedNotes()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        var purchasedNotes = await _context.Purchases
            .Include(p => p.Note)
            .ThenInclude(n => n.User)
            .Where(p => p.UserId == userId && p.Status == "Completed")
            .Select(p => new NoteResponseDTO
            {
                Id = p.Note.Id,
                Title = p.Note.Title,
                Description = p.Note.Description,
                Subject = p.Note.Subject,
                Category = p.Note.Category,
                Price = p.Note.Price,
                IsFree = p.Note.IsFree,
                Status = p.Note.Status,
                Downloads = p.Note.Downloads,
                Views = p.Note.Views,
                UploaderName = p.Note.User.FullName,
                UploadedAt = p.Note.UploadedAt
            })
            .ToListAsync();

        return Ok(purchasedNotes);
    }

    // Add to wishlist
    [HttpPost("wishlist/{noteId}")]
    public async Task<ActionResult> AddToWishlist(int noteId)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        var exists = await _context.Wishlists
            .AnyAsync(w => w.UserId == userId && w.NoteId == noteId);

        if (exists)
            return BadRequest("Note already in wishlist");

        var wishlist = new Wishlist
        {
            UserId = userId,
            NoteId = noteId
        };

        _context.Wishlists.Add(wishlist);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Added to wishlist" });
    }

    // Remove from wishlist
    [HttpDelete("wishlist/{noteId}")]
    public async Task<ActionResult> RemoveFromWishlist(int noteId)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        var wishlist = await _context.Wishlists
            .FirstOrDefaultAsync(w => w.UserId == userId && w.NoteId == noteId);

        if (wishlist == null)
            return NotFound("Note not found in wishlist");

        _context.Wishlists.Remove(wishlist);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Removed from wishlist" });
    }

    // Get wishlist
    [HttpGet("wishlist")]
    public async Task<ActionResult<IEnumerable<NoteResponseDTO>>> GetWishlist()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        var wishlist = await _context.Wishlists
            .Include(w => w.Note)
            .ThenInclude(n => n.User)
            .Where(w => w.UserId == userId)
            .Select(w => new NoteResponseDTO
            {
                Id = w.Note.Id,
                Title = w.Note.Title,
                Description = w.Note.Description,
                Subject = w.Note.Subject,
                Category = w.Note.Category,
                Price = w.Note.Price,
                IsFree = w.Note.IsFree,
                Status = w.Note.Status,
                Downloads = w.Note.Downloads,
                Views = w.Note.Views,
                UploaderName = w.Note.User.FullName,
                UploadedAt = w.Note.UploadedAt
            })
            .ToListAsync();

        return Ok(wishlist);
    }

    // DELETE: api/notes/{id}
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteNote(int id)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
        {
            return Unauthorized();
        }

        var note = await _context.Notes.FindAsync(id);
        if (note == null)
        {
            return NotFound("Note not found");
        }

        // Check if user is Admin
        var user = await _context.Users.FindAsync(userId);
        var isAdmin = user?.Role == "Admin";

        // Check permission (Owner or Admin)
        if (note.UserId != userId && !isAdmin)
        {
            return Forbid();
        }

        try
        {
            // Safeguarded deletion logic:
            // Check if this note has been purchased by anyone
            var hasPurchases = await _context.Purchases.AnyAsync(p => p.NoteId == id && p.Status == "Completed");
            
            if (hasPurchases)
            {
                // Soft delete: Change status to "Deleted" so sales ledger records remain intact
                note.Status = "Deleted";
                await _context.SaveChangesAsync();
            }
            else
            {
                // Hard delete: Remove record completely
                _context.Notes.Remove(note);
                await _context.SaveChangesAsync();

                // Attempt to delete physical PDF file from disk
                var filePath = Path.Combine(_environment.WebRootPath ?? "wwwroot", note.FilePath.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            return Ok(new { message = "Note deleted successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // POST: api/notes/purchase/{id}
    [HttpPost("purchase/{id}")]
    public async Task<ActionResult> PurchaseNote(int id)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
        {
            return Unauthorized();
        }

        var note = await _context.Notes.FindAsync(id);
        if (note == null)
        {
            return NotFound("Note not found");
        }

        if (note.Status != "Approved")
        {
            return BadRequest("Only approved notes can be purchased");
        }

        // Verify uploader cannot purchase their own note
        if (note.UserId == userId)
        {
            return BadRequest("You are the uploader of this note and already have full access");
        }

        // Verify free note cannot be purchased
        if (note.IsFree)
        {
            return BadRequest("This note is free. You can download it directly");
        }

        // Check if already purchased
        var alreadyPurchased = await _context.Purchases
            .AnyAsync(p => p.NoteId == id && p.UserId == userId && p.Status == "Completed");

        if (alreadyPurchased)
        {
            return BadRequest("You have already purchased this note");
        }

        var purchase = new Purchase
        {
            UserId = userId,
            NoteId = id,
            Amount = note.Price,
            Status = "Completed",
            PurchasedAt = DateTime.UtcNow,
            TransactionId = $"TXN{new Random().Next(100000, 999999)}"
        };

        _context.Purchases.Add(purchase);
        await _context.SaveChangesAsync();

        return Ok(new 
        { 
            message = "Purchase completed successfully", 
            transactionId = purchase.TransactionId,
            amount = purchase.Amount
        });
    }
}