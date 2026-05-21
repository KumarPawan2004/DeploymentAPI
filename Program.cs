using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using DeploymentAPI.Data;
using DeploymentAPI.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure PostgreSQL Database
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Configure JWT Authentication
var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
    ?? builder.Configuration["JwtSettings:SecretKey"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };
    });

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact",
        builder => builder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

var app = builder.Build();

// SEED ADMIN USER - ADD THIS SECTION
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        // Self-healing: Ensure Categories table exists (MUST be done first before EnsureCreated/model mapping checks)
        dbContext.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS ""Categories"" (
                ""Id"" SERIAL PRIMARY KEY,
                ""Name"" VARCHAR(255) NOT NULL,
                ""Description"" TEXT NOT NULL,
                ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT timezone('utc', now())
            );
        ");

        dbContext.Database.EnsureCreated();

        // Seed default categories if none exist in the database table
        if (!dbContext.Categories.Any())
        {
            dbContext.Categories.AddRange(
                new Category { Name = "Computer Science & Engineering", Description = "Core academic research documents, programming resources, and computer hardware systems.", CreatedAt = DateTime.UtcNow },
                new Category { Name = "Data Structures & Algorithms", Description = "Coding puzzle guides, tree traversals, search/sort complexities, and graph concepts.", CreatedAt = DateTime.UtcNow },
                new Category { Name = "Operating Systems", Description = "CPU scheduling notes, disk optimization techniques, page allocations, and process forks.", CreatedAt = DateTime.UtcNow },
                new Category { Name = "Database Management System (DBMS)", Description = "Relational database structures, SQL query optimization guides, transaction isolation properties.", CreatedAt = DateTime.UtcNow },
                new Category { Name = "Discrete Mathematics", Description = "Set theories, logical calculations, probability graphs, and discrete mathematics guides.", CreatedAt = DateTime.UtcNow }
            );
            dbContext.SaveChanges();
            Console.WriteLine("✅ Default categories seeded successfully!");
        }

        // Check if any admin exists
        var adminExists = dbContext.Users.Any(u => u.Role == "Admin");

        if (!adminExists)
        {
            // Self-healing: Check if admin@noteshub.com exists. If so, update role to Admin!
            var existingAdminUser = dbContext.Users.FirstOrDefault(u => u.Email == "admin@noteshub.com");
            if (existingAdminUser != null)
            {
                existingAdminUser.Role = "Admin";
                dbContext.SaveChanges();
                Console.WriteLine("✅ Existing seed admin user updated to Admin role successfully!");
            }
            else
            {
                var adminUser = new User
                {
                    FullName = "Super Admin",
                    Email = "admin@noteshub.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                    Role = "Admin",
                    IsBlocked = false,
                    CreatedAt = DateTime.UtcNow
                };

                dbContext.Users.Add(adminUser);
                dbContext.SaveChanges();

                Console.WriteLine("✅ Admin user created successfully!");
                Console.WriteLine("   Email: admin@noteshub.com");
                Console.WriteLine("   Password: Admin@123");
            }
        }
        else
        {
            Console.WriteLine("✅ Admin user already exists");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Database initialization error: {ex.Message}");
    }
}

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "DeploymentAPI V1");
});

app.UseCors("AllowReact");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Run($"http://0.0.0.0:{port}");