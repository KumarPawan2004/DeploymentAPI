using DeploymentAPI.Models;  // Changed from backend.Models
using Microsoft.EntityFrameworkCore;

namespace DeploymentAPI.Data;  // Changed from backend.Data

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
}