using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using DeploymentAPI.Data;
using DeploymentAPI.Models;
using DeploymentAPI.Models.DTOs;

namespace DeploymentAPI.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _context;

    public AdminController(AppDbContext context)
    {
        _context = context;
    }

    // Check if user is admin
    private async Task<bool> IsAdmin()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var user = await _context.Users.FindAsync(userId);
        return user?.Role == "Admin";
    }

    // Get all users (Admin only)
    [HttpGet("users")]
    public async Task<ActionResult> GetAllUsers()
    {
        if (!await IsAdmin()) return Forbid();

        var users = await _context.Users
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.Email,
                u.Role,
                u.IsBlocked,
                u.CreatedAt,
                u.LastLoginAt,
                NotesCount = _context.Notes.Count(n => n.UserId == u.Id),
                PurchasesCount = _context.Purchases.Count(p => p.UserId == u.Id)
            })
            .ToListAsync();

        return Ok(users);
    }

    // Update user status (Block/Unblock) - Admin only
    [HttpPut("users/{userId}/status")]
    public async Task<ActionResult> UpdateUserStatus(int userId, [FromBody] UpdateUserStatusDTO request)
    {
        if (!await IsAdmin()) return Forbid();

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return NotFound("User not found");

        user.IsBlocked = request.IsBlocked;
        await _context.SaveChangesAsync();

        return Ok(new { message = $"User {(request.IsBlocked ? "blocked" : "unblocked")} successfully" });
    }

    // Get pending notes for review (Admin only)
    [HttpGet("pending-notes")]
    public async Task<ActionResult> GetPendingNotes()
    {
        if (!await IsAdmin()) return Forbid();

        var notes = await _context.Notes
            .Include(n => n.User)
            .Where(n => n.Status == "Pending")
            .OrderBy(n => n.UploadedAt)
            .Select(n => new
            {
                n.Id,
                n.Title,
                n.Description,
                n.Subject,
                n.Category,
                n.Price,
                n.IsFree,
                n.FileName,
                n.UploadedAt,
                Uploader = new { n.User.FullName, n.User.Email }
            })
            .ToListAsync();

        return Ok(notes);
    }

    // Review note (Approve/Reject) - Admin only
    [HttpPost("review-note/{noteId}")]
    public async Task<ActionResult> ReviewNote(int noteId, [FromBody] ReviewNoteDTO request)
    {
        if (!await IsAdmin()) return Forbid();

        var note = await _context.Notes.FindAsync(noteId);
        if (note == null)
            return NotFound("Note not found");

        if (request.Status != "Approved" && request.Status != "Rejected")
            return BadRequest("Status must be 'Approved' or 'Rejected'");

        note.Status = request.Status;
        note.RejectionReason = request.RejectionReason;
        note.ReviewedAt = DateTime.UtcNow;
        note.ReviewedBy = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        await _context.SaveChangesAsync();

        return Ok(new { message = $"Note {request.Status.ToLower()} successfully" });
    }

    // Get all notes (Admin only)
    [HttpGet("all-notes")]
    public async Task<ActionResult> GetAllNotes()
    {
        if (!await IsAdmin()) return Forbid();

        var notes = await _context.Notes
            .Include(n => n.User)
            .OrderByDescending(n => n.UploadedAt)
            .Select(n => new
            {
                n.Id,
                n.Title,
                n.Subject,
                n.Category,
                n.Price,
                n.Status,
                n.Downloads,
                n.Views,
                n.UploadedAt,
                Uploader = n.User.FullName
            })
            .ToListAsync();

        return Ok(notes);
    }

    // Get dashboard stats (Admin only)
    [HttpGet("dashboard-stats")]
    public async Task<ActionResult> GetDashboardStats()
    {
        if (!await IsAdmin()) return Forbid();

        var stats = new
        {
            TotalUsers = await _context.Users.CountAsync(),
            TotalNotes = await _context.Notes.CountAsync(),
            ApprovedNotes = await _context.Notes.CountAsync(n => n.Status == "Approved"),
            PendingNotes = await _context.Notes.CountAsync(n => n.Status == "Pending"),
            TotalDownloads = await _context.Notes.SumAsync(n => n.Downloads),
            TotalRevenue = await _context.Purchases.Where(p => p.Status == "Completed").SumAsync(p => p.Amount),
            TotalTransactions = await _context.Purchases.CountAsync(),
            Categories = await _context.Notes
                .Where(n => n.Status == "Approved")
                .GroupBy(n => n.Category)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .ToListAsync()
        };

        return Ok(stats);
    }

    // Get all transactions list (Admin only)
    [HttpGet("transactions")]
    public async Task<ActionResult> GetTransactions()
    {
        if (!await IsAdmin()) return Forbid();

        var transactions = await _context.Purchases
            .Include(p => p.User)
            .Include(p => p.Note)
            .OrderByDescending(p => p.PurchasedAt)
            .Select(p => new
            {
                Id = p.TransactionId,
                Buyer = p.User.FullName,
                Note = p.Note.Title,
                Price = p.Amount,
                Status = p.Status,
                Date = p.PurchasedAt.ToString("dd MMM yyyy, hh:mm tt")
            })
            .ToListAsync();

        return Ok(transactions);
    }
}