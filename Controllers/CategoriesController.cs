using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using DeploymentAPI.Data;
using DeploymentAPI.Models;

namespace DeploymentAPI.Controllers;

[ApiController]
[Route("api/categories")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _context;

    public CategoriesController(AppDbContext context)
    {
        _context = context;
    }

    // Helper to check if current user is an admin
    private async Task<bool> IsAdmin()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
        {
            return false;
        }
        var user = await _context.Users.FindAsync(userId);
        return user?.Role == "Admin";
    }

    // GET: api/Categories
    [HttpGet]
    [AllowAnonymous] // Anyone can see categories for navigation and browsing
    public async Task<ActionResult<IEnumerable<object>>> GetCategories()
    {
        var categories = await _context.Categories
            .OrderBy(c => c.Name)
            .Select(c => new
            {
                Id = c.Id.ToString(), // Match frontend key type string
                c.Name,
                c.Description,
                NoteCount = _context.Notes.Count(n => n.Category == c.Name && n.Status == "Approved"),
                CreatedAt = c.CreatedAt.ToString("dd MMM yyyy") // Match frontend date display
            })
            .ToListAsync();

        return Ok(categories);
    }

    // POST: api/Categories
    [HttpPost]
    public async Task<ActionResult> CreateCategory([FromBody] CategoryRequestDTO request)
    {
        if (!await IsAdmin())
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Category name is required.");
        }

        var exists = await _context.Categories.AnyAsync(c => c.Name.ToLower() == request.Name.Trim().ToLower());
        if (exists)
        {
            return BadRequest("A category with this name already exists.");
        }

        var category = new Category
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim() ?? "No description provided.",
            CreatedAt = DateTime.UtcNow
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Id = category.Id.ToString(),
            category.Name,
            category.Description,
            NoteCount = 0,
            CreatedAt = category.CreatedAt.ToString("dd MMM yyyy")
        });
    }

    // DELETE: api/Categories/{id}
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteCategory(int id)
    {
        if (!await IsAdmin())
        {
            return Forbid();
        }

        var category = await _context.Categories.FindAsync(id);
        if (category == null)
        {
            return NotFound("Category not found.");
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Category '{category.Name}' deleted successfully." });
    }
}

public class CategoryRequestDTO
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
