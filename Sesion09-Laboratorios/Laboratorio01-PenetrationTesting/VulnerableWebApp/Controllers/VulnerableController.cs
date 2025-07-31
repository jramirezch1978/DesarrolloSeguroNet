using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data.SqlClient;
using System.Text.Json;

namespace VulnerableWebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VulnerableController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<VulnerableController> _logger;

    public VulnerableController(AppDbContext context, ILogger<VulnerableController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ❌ VULNERABLE: SQL Injection
    [HttpGet("user/{id}")]
    public async Task<IActionResult> GetUser(string id)
    {
        try
        {
            // Vulnerable SQL query - directly concatenating user input
            var users = await _context.Users
                .FromSqlRaw($"SELECT * FROM Users WHERE Id = {id}")
                .ToListAsync();

            if (!users.Any())
            {
                return NotFound("User not found");
            }

            return Ok(users.First());
        }
        catch (Exception ex)
        {
            // ❌ VULNERABLE: Information disclosure in error messages
            return BadRequest($"Database error: {ex.Message}");
        }
    }

    // ❌ VULNERABLE: Authentication bypass and user enumeration
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrEmpty(request.Username))
        {
            return BadRequest("Username is required");
        }

        // Check if user exists first (vulnerable to user enumeration)
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username);

        if (user == null)
        {
            return Unauthorized("User does not exist"); // ❌ Reveals user existence
        }

        // Simple password check (no hashing, timing attack vulnerable)
        if (user.Password == request.Password)
        {
            // ❌ VULNERABLE: Weak session management
            var token = GenerateSimpleToken(user);
            return Ok(new { Token = token, IsAdmin = user.IsAdmin });
        }

        return Unauthorized("Invalid password"); // ❌ Different message reveals valid username
    }

    // ❌ VULNERABLE: XSS via unescaped output
    [HttpGet("search")]
    public IActionResult Search(string query)
    {
        if (string.IsNullOrEmpty(query))
        {
            return BadRequest("Query parameter is required");
        }

        // Simulate search results with reflected XSS vulnerability
        var htmlResponse = $@"
        <html>
        <body>
            <h1>Search Results</h1>
            <p>You searched for: {query}</p>
            <div>No results found for '{query}'</div>
        </body>
        </html>";

        return Content(htmlResponse, "text/html");
    }

    // ❌ VULNERABLE: Broken access control
    [HttpGet("admin/secrets")]
    public async Task<IActionResult> GetSecrets(string token)
    {
        // ❌ Weak token validation
        if (string.IsNullOrEmpty(token))
        {
            return Unauthorized("Token required");
        }

        // ❌ Predictable token structure
        if (!token.StartsWith("user_") && !token.StartsWith("admin_"))
        {
            return Unauthorized("Invalid token format");
        }

        // ❌ No actual verification of admin status
        if (token.StartsWith("admin_"))
        {
            var secrets = await _context.SecretData.ToListAsync();
            return Ok(secrets);
        }

        return Forbid("Admin access required");
    }

    // ❌ VULNERABLE: Information disclosure
    [HttpGet("debug/config")]
    public IActionResult GetDebugConfig()
    {
        var config = new
        {
            DatabaseConnection = "Server=localhost;Database=Production;User=sa;Password=SuperSecret123!",
            ApiKeys = new
            {
                PaymentGateway = "pk_live_1234567890",
                EmailService = "sg.1234567890",
                CloudStorage = "AKIA1234567890"
            },
            InternalUrls = new
            {
                AdminPanel = "https://admin.internal.company.com",
                Database = "https://db.internal.company.com",
                Monitoring = "https://monitor.internal.company.com"
            }
        };

        return Ok(config);
    }

    // ❌ VULNERABLE: Command injection
    [HttpPost("ping")]
    public IActionResult PingHost([FromBody] PingRequest request)
    {
        if (string.IsNullOrEmpty(request.Host))
        {
            return BadRequest("Host is required");
        }

        try
        {
            // ❌ Direct command execution with user input
            var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = "ping";
            process.StartInfo.Arguments = $"-n 4 {request.Host}"; // Vulnerable to command injection
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            
            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return Ok(new { Output = output });
        }
        catch (Exception ex)
        {
            return BadRequest($"Ping failed: {ex.Message}");
        }
    }

    private string GenerateSimpleToken(User user)
    {
        // ❌ VULNERABLE: Predictable token generation
        var prefix = user.IsAdmin ? "admin_" : "user_";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return $"{prefix}{user.Id}_{timestamp}";
    }
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class PingRequest
{
    public string Host { get; set; } = string.Empty;
}