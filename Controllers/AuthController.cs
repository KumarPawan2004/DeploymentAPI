using DeploymentAPI.Data;
using DeploymentAPI.DTOs;
using DeploymentAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace DeploymentAPI.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDTO>> Register(RegisterDTO request)
    {
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
        {
            return BadRequest("Email already exists");
        }

        var assignedRole = "User";
        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            var normalizedRole = request.Role.Trim();
            if (normalizedRole.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                assignedRole = "Admin";
            }
            else if (normalizedRole.Equals("User", StringComparison.OrdinalIgnoreCase))
            {
                assignedRole = "User";
            }
        }

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            Role = assignedRole,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            IsBlocked = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var token = GenerateJwtToken(user);

        var response = new AuthResponseDTO
        {
            Token = token,
            User = new UserDto
            {
                Id = user.Id.ToString(),
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role,
                IsBlocked = user.IsBlocked
            }
        };

        return Ok(response);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDTO>> Login(LoginDTO request)
    {
        Console.WriteLine($"📝 Login attempt: {request.Email}");

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null)
        {
            Console.WriteLine("❌ User not found");
            return BadRequest("Invalid email or password");
        }

        var isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        Console.WriteLine($"🔐 Password valid: {isPasswordValid}");

        if (!isPasswordValid)
        {
            return BadRequest("Invalid email or password");
        }

        // Validate that the database user has the requested role, if provided
        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            if (!user.Role.Equals(request.Role.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"❌ Role mismatch: DB role={user.Role}, requested={request.Role}");
                return BadRequest("The selected role does not match this account.");
            }
        }

        var token = GenerateJwtToken(user);

        var response = new AuthResponseDTO
        {
            Token = token,
            User = new UserDto
            {
                Id = user.Id.ToString(),
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role,
                IsBlocked = user.IsBlocked
            }
        };

        return Ok(response);
    }

    [HttpGet("me")]
    public async Task<ActionResult> GetCurrentUser()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized();
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound("User not found");
        }

        return Ok(new
        {
            user.Id,
            user.FullName,
            user.Email,
            user.Role,
            user.IsBlocked,
            user.CreatedAt
        });
    }

    private string GenerateJwtToken(User user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"]
            ?? throw new InvalidOperationException("JWT SecretKey is not configured");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["ExpirationInMinutes"] ?? "60")),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [HttpPost("create-admin")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> CreateAdmin([FromBody] RegisterDTO request)
    {
        var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
        if (currentUserRole != "Admin")
        {
            return Forbid("Only admins can create new admin accounts");
        }

        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
        {
            return BadRequest("Email already exists");
        }

        var admin = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = "Admin",
            IsBlocked = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(admin);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Admin created successfully", email = admin.Email });
    }
}