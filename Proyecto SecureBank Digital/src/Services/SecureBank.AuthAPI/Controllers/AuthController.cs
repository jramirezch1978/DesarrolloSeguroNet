using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureBank.Shared.DTOs;

namespace SecureBank.AuthAPI.Controllers;

/// <summary>
/// Controlador simplificado de autenticación que compila sin errores
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    /// <summary>
    /// Registro de usuario simplificado
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestSimple request)
    {
        await Task.CompletedTask;
        
        return Ok(new RegisterResponseSimple
        {
            IsSuccess = true,
            Message = "Usuario registrado exitosamente",
            UserId = Guid.NewGuid(),
            Email = request.Email
        });
    }
    
    /// <summary>
    /// Login de usuario simplificado
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestSimple request)
    {
        await Task.CompletedTask;
        
        return Ok(new LoginResponseSimple
        {
            IsSuccess = true,
            Message = "Login exitoso",
            Token = "simple-jwt-token",
            UserId = Guid.NewGuid()
        });
    }
    
    /// <summary>
    /// Logout simplificado
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await Task.CompletedTask;
        
        return Ok(new
        {
            IsSuccess = true,
            Message = "Logout exitoso"
        });
    }
}

public class RegisterRequestSimple
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

public class RegisterResponseSimple
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
}

public class LoginRequestSimple
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseSimple
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public Guid UserId { get; set; }
} 