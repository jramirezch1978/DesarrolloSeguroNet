namespace SecureBank.Application.Common.Interfaces;

/// <summary>
/// Interfaz para el servicio de tokens JWT
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Genera tokens de acceso y refresh para un usuario
    /// </summary>
    Task<AuthenticationTokens> GenerateTokensAsync(Guid userId, string email, string role, 
        string deviceFingerprint, string ipAddress);

    /// <summary>
    /// Refresca los tokens usando un refresh token válido
    /// </summary>
    Task<AuthenticationTokens?> RefreshTokensAsync(string refreshToken, string deviceFingerprint, string ipAddress);

    /// <summary>
    /// Valida un token de acceso
    /// </summary>
    Task<TokenValidationResult> ValidateAccessTokenAsync(string accessToken);

    /// <summary>
    /// Revoca un refresh token específico
    /// </summary>
    Task<bool> RevokeRefreshTokenAsync(string refreshToken);

    /// <summary>
    /// Revoca todos los tokens de un usuario
    /// </summary>
    Task<bool> RevokeAllUserTokensAsync(Guid userId);

    /// <summary>
    /// Limpia tokens expirados
    /// </summary>
    Task CleanupExpiredTokensAsync();
}

/// <summary>
/// Tokens de autenticación
/// </summary>
public class AuthenticationTokens
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiry { get; set; }
    public DateTime RefreshTokenExpiry { get; set; }
    public string TokenType { get; set; } = "Bearer";
}

/// <summary>
/// Resultado de validación de token
/// </summary>
public class TokenValidationResult
{
    public bool IsValid { get; set; }
    public Guid? UserId { get; set; }
    public string? Email { get; set; }
    public string? Role { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public List<string> Errors { get; set; } = new();
} 