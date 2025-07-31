using Microsoft.EntityFrameworkCore;
using System.Data.SqlClient;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Entity Framework with In-Memory database for demo
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("VulnerableDB"));

// Configure CORS (insecurely for demo)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ❌ VULNERABLE: Missing HTTPS redirection
// app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

// Seed vulnerable data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    SeedData(context);
}

app.Run();

static void SeedData(AppDbContext context)
{
    if (!context.Users.Any())
    {
        context.Users.AddRange(
            new User { Id = 1, Username = "admin", Password = "admin123", IsAdmin = true },
            new User { Id = 2, Username = "user", Password = "password", IsAdmin = false },
            new User { Id = 3, Username = "guest", Password = "guest", IsAdmin = false }
        );
        
        context.SecretData.AddRange(
            new SecretData { Id = 1, Title = "Database Password", Content = "SuperSecret123!" },
            new SecretData { Id = 2, Title = "API Key", Content = "sk-1234567890abcdef" },
            new SecretData { Id = 3, Title = "Admin Panel", Content = "/admin/secret-panel" }
        );
        
        context.SaveChanges();
    }
}

// Data Models
public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
}

public class SecretData
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    
    public DbSet<User> Users { get; set; }
    public DbSet<SecretData> SecretData { get; set; }
}