using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SecureBank.Application.Common.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace SecureBank.Infrastructure.Security.Services;

/// <summary>
/// Implementación del servicio de tokens JWT
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<JwtTokenService> _logger;
    private readonly Dictionary<string, RefreshTokenInfo> _refreshTokens = new();

    public JwtTokenService(IConfiguration configuration, ILogger<JwtTokenService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AuthenticationTokens> GenerateTokensAsync(Guid userId, string email, string role,
        string deviceFingerprint, string ipAddress)
    {
        try
        {
            var accessToken = GenerateAccessToken(userId, email, role, deviceFingerprint, ipAddress);
            var refreshToken = GenerateRefreshToken();

            var refreshTokenInfo = new RefreshTokenInfo
            {
                UserId = userId,
                Email = email,
                Role = role,
                DeviceFingerprint = deviceFingerprint,
                IpAddress = ipAddress,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                IsRevoked = false
            };

            _refreshTokens[refreshToken] = refreshTokenInfo;

            return new AuthenticationTokens
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiry = DateTime.UtcNow.AddMinutes(15),
                RefreshTokenExpiry = refreshTokenInfo.ExpiresAt,
                TokenType = "Bearer"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generando tokens para usuario {UserId}", userId);
            throw;
        }
    }

    public async Task<AuthenticationTokens?> RefreshTokensAsync(string refreshToken, string deviceFingerprint, string ipAddress)
    {
        try
        {
            if (!_refreshTokens.TryGetValue(refreshToken, out var tokenInfo))
            {
                _logger.LogWarning("Intento de refresh con token inválido desde {IpAddress}", ipAddress);
                return null;
            }

            if (tokenInfo.IsRevoked || tokenInfo.ExpiresAt < DateTime.UtcNow)
            {
                _refreshTokens.Remove(refreshToken);
                _logger.LogWarning("Intento de refresh con token expirado o revocado para usuario {UserId}", tokenInfo.UserId);
                return null;
            }

            if (tokenInfo.DeviceFingerprint != deviceFingerprint)
            {
                _logger.LogWarning("Device fingerprint no coincide para refresh token del usuario {UserId}", tokenInfo.UserId);
                return null;
            }

            // Generar nuevos tokens
            var newTokens = await GenerateTokensAsync(tokenInfo.UserId, tokenInfo.Email, tokenInfo.Role, deviceFingerprint, ipAddress);

            // Revocar el token anterior
            _refreshTokens.Remove(refreshToken);

            return newTokens;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en refresh de tokens");
            return null;
        }
    }

    public async Task<Application.Common.Interfaces.TokenValidationResult> ValidateAccessTokenAsync(string accessToken)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["JWT:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured"));

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _configuration["JWT:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["JWT:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(accessToken, validationParameters, out SecurityToken validatedToken);

            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var emailClaim = principal.FindFirst(ClaimTypes.Email)?.Value;
            var roleClaim = principal.FindFirst(ClaimTypes.Role)?.Value;

            return new Application.Common.Interfaces.TokenValidationResult
            {
                IsValid = true,
                UserId = Guid.TryParse(userIdClaim, out var userId) ? userId : null,
                Email = emailClaim,
                Role = roleClaim,
                ExpiresAt = validatedToken.ValidTo
            };
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Token de acceso inválido");
            return new Application.Common.Interfaces.TokenValidationResult
            {
                IsValid = false,
                Errors = new List<string> { "Token inválido" }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validando token de acceso");
            return new Application.Common.Interfaces.TokenValidationResult
            {
                IsValid = false,
                Errors = new List<string> { "Error interno de validación" }
            };
        }
    }

    public async Task<bool> RevokeRefreshTokenAsync(string refreshToken)
    {
        try
        {
            if (_refreshTokens.TryGetValue(refreshToken, out var tokenInfo))
            {
                tokenInfo.IsRevoked = true;
                _logger.LogInformation("Refresh token revocado para usuario {UserId}", tokenInfo.UserId);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revocando refresh token");
            return false;
        }
    }

    public async Task<bool> RevokeAllUserTokensAsync(Guid userId)
    {
        try
        {
            var userTokens = _refreshTokens.Where(kvp => kvp.Value.UserId == userId).ToList();
            
            foreach (var token in userTokens)
            {
                token.Value.IsRevoked = true;
            }

            _logger.LogInformation("Todos los tokens revocados para usuario {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revocando todos los tokens del usuario {UserId}", userId);
            return false;
        }
    }

    public async Task CleanupExpiredTokensAsync()
    {
        try
        {
            var expiredTokens = _refreshTokens
                .Where(kvp => kvp.Value.ExpiresAt < DateTime.UtcNow || kvp.Value.IsRevoked)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var token in expiredTokens)
            {
                _refreshTokens.Remove(token);
            }

            _logger.LogInformation("Limpieza de tokens: {Count} tokens expirados eliminados", expiredTokens.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en limpieza de tokens expirados");
        }
    }

    private string GenerateAccessToken(Guid userId, string email, string role, string deviceFingerprint, string ipAddress)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_configuration["JWT:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured"));

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Role, role),
            new("device_fingerprint", deviceFingerprint),
            new("ip_address", ipAddress),
            new("jti", Guid.NewGuid().ToString()),
            new("iat", new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(15),
            Issuer = _configuration["JWT:Issuer"],
            Audience = _configuration["JWT:Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}

/// <summary>
/// Información del refresh token
/// </summary>
internal class RefreshTokenInfo
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string DeviceFingerprint { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
} 