using Microsoft.EntityFrameworkCore;
using DeploymentAPI.Models;

namespace DeploymentAPI.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Note> Notes { get; set; }
    public DbSet<Purchase> Purchases { get; set; }
    public DbSet<Wishlist> Wishlists { get; set; }
    public DbSet<Category> Categories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // Note configuration
        modelBuilder.Entity<Note>()
            .HasOne(n => n.User)
            .WithMany(u => u.Notes)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Purchase configuration
        modelBuilder.Entity<Purchase>()
            .HasOne(p => p.User)
            .WithMany(u => u.Purchases)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Purchase>()
            .HasOne(p => p.Note)
            .WithMany(n => n.Purchases)
            .HasForeignKey(p => p.NoteId)
            .OnDelete(DeleteBehavior.Restrict);

        // Wishlist configuration
        modelBuilder.Entity<Wishlist>()
            .HasOne(w => w.User)
            .WithMany(u => u.WishlistItems)
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Wishlist>()
            .HasOne(w => w.Note)
            .WithMany(n => n.Wishlists)
            .HasForeignKey(w => w.NoteId)
            .OnDelete(DeleteBehavior.Cascade);

        // Seed admin user (password: Admin@123)
        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = 1,
                FullName = "Super Admin",
                Email = "admin@noteshub.com",
                PasswordHash = "$2b$11$Km2MxlVBhCHANnzP7D8DO.IXD1yr48fGA0gHj7M2U5ZcC7Pi55HfW",
                Role = "Admin",
                IsBlocked = false,
                CreatedAt = DateTime.UtcNow
            }
        );

        // Seed default categories matching the frontend management console
        modelBuilder.Entity<Category>().HasData(
            new Category
            {
                Id = 1,
                Name = "Computer Science & Engineering",
                Description = "Core academic research documents, programming resources, and computer hardware systems.",
                CreatedAt = new DateTime(2026, 1, 12, 0, 0, 0, DateTimeKind.Utc)
            },
            new Category
            {
                Id = 2,
                Name = "Data Structures & Algorithms",
                Description = "Coding puzzle guides, tree traversals, search/sort complexities, and graph concepts.",
                CreatedAt = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc)
            },
            new Category
            {
                Id = 3,
                Name = "Operating Systems",
                Description = "CPU scheduling notes, disk optimization techniques, page allocations, and process forks.",
                CreatedAt = new DateTime(2026, 1, 8, 0, 0, 0, DateTimeKind.Utc)
            },
            new Category
            {
                Id = 4,
                Name = "Database Management System (DBMS)",
                Description = "Relational database structures, SQL query optimization guides, transaction isolation properties.",
                CreatedAt = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc)
            },
            new Category
            {
                Id = 5,
                Name = "Discrete Mathematics",
                Description = "Set theories, logical calculations, probability graphs, and discrete mathematics guides.",
                CreatedAt = new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}