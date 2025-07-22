using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SecureBank.Application.Common.DTOs;
using SecureBank.Domain.Enums;

namespace SecureBank.Security.Services;

/// <summary>
/// Servicio JWT para SecureBank Digital
/// Implementa JWT con refresh tokens y rotación automática según el prompt inicial
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _jwtOptions;
    private readonly ILogger<JwtTokenService> _logger;
    private readonly SymmetricSecurityKey _signingKey;
    private readonly TokenValidationParameters _tokenValidationParameters;

    // Store para refresh tokens en memoria (en producción sería Redis/DB)
    private static readonly Dictionary<string, RefreshTokenInfo> _refreshTokens = new();
    private static readonly object _lockObject = new();

    public JwtTokenService(IOptions<JwtOptions> jwtOptions, ILogger<JwtTokenService> logger)
    {
        _jwtOptions = jwtOptions.Value;
        _logger = logger;
        
        ValidateConfiguration();
        
        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));
        _tokenValidationParameters = CreateTokenValidationParameters();
    }

    /// <summary>
    /// Genera tokens de acceso y refresh para un usuario
    /// </summary>
    public AuthenticationTokens GenerateTokens(UserDto user, string deviceFingerprint, string ipAddress)
    {
        try
        {
            var accessToken = GenerateAccessToken(user, deviceFingerprint, ipAddress);
            var refreshToken = GenerateRefreshToken(user.Id, deviceFingerprint, ipAddress);

            var tokens = new AuthenticationTokens
            {
                AccessToken = accessToken.Token,
                RefreshToken = refreshToken.Token,
                AccessTokenExpiresAt = accessToken.ExpiresAt,
                RefreshTokenExpiresAt = refreshToken.ExpiresAt,
                TokenType = "Bearer",
                Scopes = GetUserScopes(user.Role)
            };

            _logger.LogInformation("Tokens generados para usuario {UserId} desde {IpAddress}", 
                user.Id, ipAddress);

            return tokens;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generando tokens para usuario {UserId}", user.Id);
            throw new SecurityException("Error durante la generación de tokens", ex);
        }
    }

    /// <summary>
    /// Refresca los tokens usando el refresh token
    /// </summary>
    public AuthenticationTokens RefreshTokens(string refreshToken, string deviceFingerprint, string ipAddress)
    {
        try
        {
            // Validar refresh token
            var refreshTokenInfo = ValidateRefreshToken(refreshToken, deviceFingerprint, ipAddress);
            
            // Generar nuevos tokens
            var userClaims = ExtractUserClaimsFromRefreshToken(refreshTokenInfo);
            var user = CreateUserDtoFromClaims(userClaims);
            
            // Revocar el refresh token usado
            RevokeRefreshToken(refreshToken);
            
            // Generar nuevos tokens
            var newTokens = GenerateTokens(user, deviceFingerprint, ipAddress);

            _logger.LogInformation("Tokens refrescados para usuario {UserId} desde {IpAddress}", 
                user.Id, ipAddress);

            return newTokens;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refrescando tokens desde {IpAddress}", ipAddress);
            throw new SecurityException("Error durante el refresh de tokens", ex);
        }
    }

    /// <summary>
    /// Valida un access token
    /// </summary>
    public ClaimsPrincipal ValidateAccessToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, _tokenValidationParameters, out var validatedToken);

            // Validaciones adicionales de seguridad
            ValidateTokenSecurity(validatedToken as JwtSecurityToken);

            return principal;
        }
        catch (SecurityTokenExpiredException)
        {
            _logger.LogWarning("Token de acceso expirado");
            throw new SecurityException("Token expirado");
        }
        catch (SecurityTokenInvalidSignatureException)
        {
            _logger.LogError("Token con firma inválida detectado");
            throw new SecurityException("Token inválido");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validando token de acceso");
            throw new SecurityException("Token inválido", ex);
        }
    }

    /// <summary>
    /// Revoca un refresh token específico
    /// </summary>
    public void RevokeRefreshToken(string refreshToken)
    {
        lock (_lockObject)
        {
            if (_refreshTokens.ContainsKey(refreshToken))
            {
                _refreshTokens[refreshToken].IsRevoked = true;
                _refreshTokens[refreshToken].RevokedAt = DateTime.UtcNow;
                
                _logger.LogInformation("Refresh token revocado: {TokenId}", 
                    _refreshTokens[refreshToken].TokenId);
            }
        }
    }

    /// <summary>
    /// Revoca todos los refresh tokens de un usuario
    /// </summary>
    public void RevokeAllUserTokens(Guid userId)
    {
        lock (_lockObject)
        {
            var userTokens = _refreshTokens.Values.Where(rt => rt.UserId == userId && !rt.IsRevoked);
            
            foreach (var token in userTokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
            }

            _logger.LogInformation("Todos los tokens revocados para usuario {UserId}", userId);
        }
    }

    /// <summary>
    /// Limpia tokens expirados del store
    /// </summary>
    public void CleanupExpiredTokens()
    {
        lock (_lockObject)
        {
            var expiredTokens = _refreshTokens
                .Where(kvp => kvp.Value.ExpiresAt <= DateTime.UtcNow || kvp.Value.IsRevoked)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var tokenKey in expiredTokens)
            {
                _refreshTokens.Remove(tokenKey);
            }

            _logger.LogInformation("Limpiados {Count} tokens expirados", expiredTokens.Count);
        }
    }

    // Métodos privados
    private TokenInfo GenerateAccessToken(UserDto user, string deviceFingerprint, string ipAddress)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenId = Guid.NewGuid().ToString();
        var issuedAt = DateTime.UtcNow;
        var expiresAt = issuedAt.AddMinutes(_jwtOptions.AccessTokenExpirationMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.GivenName, user.FirstName),
            new Claim(JwtRegisteredClaimNames.FamilyName, user.LastName),
            new Claim(JwtRegisteredClaimNames.Jti, tokenId),
            new Claim(JwtRegisteredClaimNames.Iat, ((DateTimeOffset)issuedAt).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("user_status", user.Status.ToString()),
            new Claim("device_fingerprint", deviceFingerprint),
            new Claim("ip_address", ipAddress),
            new Claim("is_verified", user.IsVerified.ToString()),
            new Claim("is_2fa_enabled", user.IsTwoFactorEnabled.ToString()),
            new Claim("token_type", "access")
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            NotBefore = issuedAt,
            IssuedAt = issuedAt,
            Issuer = _jwtOptions.Issuer,
            Audience = _jwtOptions.Audience,
            SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        return new TokenInfo
        {
            Token = tokenString,
            TokenId = tokenId,
            ExpiresAt = expiresAt
        };
    }

    private TokenInfo GenerateRefreshToken(Guid userId, string deviceFingerprint, string ipAddress)
    {
        var tokenId = Guid.NewGuid().ToString();
        var token = GenerateSecureRandomToken();
        var issuedAt = DateTime.UtcNow;
        var expiresAt = issuedAt.AddDays(_jwtOptions.RefreshTokenExpirationDays);

        var refreshTokenInfo = new RefreshTokenInfo
        {
            TokenId = tokenId,
            Token = token,
            UserId = userId,
            DeviceFingerprint = deviceFingerprint,
            IpAddress = ipAddress,
            IssuedAt = issuedAt,
            ExpiresAt = expiresAt,
            IsRevoked = false
        };

        lock (_lockObject)
        {
            _refreshTokens[token] = refreshTokenInfo;
        }

        return new TokenInfo
        {
            Token = token,
            TokenId = tokenId,
            ExpiresAt = expiresAt
        };
    }

    private RefreshTokenInfo ValidateRefreshToken(string refreshToken, string deviceFingerprint, string ipAddress)
    {
        lock (_lockObject)
        {
            if (!_refreshTokens.TryGetValue(refreshToken, out var tokenInfo))
            {
                throw new SecurityException("Refresh token no encontrado");
            }

            if (tokenInfo.IsRevoked)
            {
                _logger.LogWarning("Intento de uso de refresh token revocado desde {IpAddress}", ipAddress);
                throw new SecurityException("Refresh token revocado");
            }

            if (tokenInfo.ExpiresAt <= DateTime.UtcNow)
            {
                throw new SecurityException("Refresh token expirado");
            }

            // Validar device fingerprint (seguridad adicional)
            if (tokenInfo.DeviceFingerprint != deviceFingerprint)
            {
                _logger.LogError("Device fingerprint no coincide para refresh token desde {IpAddress}", ipAddress);
                throw new SecurityException("Token inválido para este dispositivo");
            }

            // Opcional: validar IP (puede ser muy estricto para usuarios móviles)
            if (_jwtOptions.ValidateIpAddress && tokenInfo.IpAddress != ipAddress)
            {
                _logger.LogWarning("IP address cambió para refresh token: {OldIp} -> {NewIp}", 
                    tokenInfo.IpAddress, ipAddress);
                // No lanzar excepción, solo loggear por ahora
            }

            return tokenInfo;
        }
    }

    private void ValidateTokenSecurity(JwtSecurityToken? jwtToken)
    {
        if (jwtToken == null)
            throw new SecurityException("Token JWT inválido");

        // Validar algoritmo de firma
        if (jwtToken.Header.Alg != SecurityAlgorithms.HmacSha256Signature)
        {
            throw new SecurityException("Algoritmo de firma no permitido");
        }

        // Validar que es un access token
        var tokenTypeClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "token_type")?.Value;
        if (tokenTypeClaim != "access")
        {
            throw new SecurityException("Tipo de token inválido");
        }
    }

    private TokenValidationParameters CreateTokenValidationParameters()
    {
        return new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _signingKey,
            ValidateIssuer = true,
            ValidIssuer = _jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = _jwtOptions.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1), // Tolerancia mínima para sincronización de reloj
            RequireExpirationTime = true,
            RequireSignedTokens = true
        };
    }

    private List<string> GetUserScopes(UserRole role)
    {
        return role switch
        {
            UserRole.Customer => new List<string> { "read:accounts", "write:transactions", "read:profile" },
            UserRole.CustomerPremium => new List<string> { "read:accounts", "write:transactions", "read:profile", "read:investments" },
            UserRole.CustomerBusiness => new List<string> { "read:accounts", "write:transactions", "read:profile", "read:reports", "manage:users" },
            UserRole.SupportOperator => new List<string> { "read:accounts", "read:users" },
            UserRole.Manager => new List<string> { "read:accounts", "write:approvals", "read:reports", "manage:limits" },
            UserRole.SecurityAuditor => new List<string> { "read:logs", "read:security", "read:reports" },
            UserRole.Administrator => new List<string> { "admin:all" },
            _ => new List<string>()
        };
    }

    private string GenerateSecureRandomToken()
    {
        var randomBytes = new byte[64]; // 512 bits
        RandomNumberGenerator.Fill(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private Dictionary<string, string> ExtractUserClaimsFromRefreshToken(RefreshTokenInfo refreshTokenInfo)
    {
        // En un escenario real, esto vendría de la base de datos
        // Por ahora, usamos los datos almacenados en el refresh token
        return new Dictionary<string, string>
        {
            ["user_id"] = refreshTokenInfo.UserId.ToString()
        };
    }

    private UserDto CreateUserDtoFromClaims(Dictionary<string, string> claims)
    {
        // En producción, esto haría una consulta a la base de datos
        // Por ahora, devolvemos un DTO básico
        return new UserDto
        {
            Id = Guid.Parse(claims["user_id"]),
            // Otros campos se llenarían desde DB
        };
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrEmpty(_jwtOptions.SecretKey))
            throw new InvalidOperationException("JWT SecretKey no configurada");

        if (_jwtOptions.SecretKey.Length < 32)
            throw new InvalidOperationException("JWT SecretKey debe tener al menos 32 caracteres");

        if (string.IsNullOrEmpty(_jwtOptions.Issuer))
            throw new InvalidOperationException("JWT Issuer no configurado");

        if (string.IsNullOrEmpty(_jwtOptions.Audience))
            throw new InvalidOperationException("JWT Audience no configurada");

        if (_jwtOptions.AccessTokenExpirationMinutes <= 0)
            throw new InvalidOperationException("AccessTokenExpirationMinutes inválido");

        if (_jwtOptions.RefreshTokenExpirationDays <= 0)
            throw new InvalidOperationException("RefreshTokenExpirationDays inválido");

        _logger.LogInformation("Servicio JWT inicializado correctamente");
    }
}

// Clases auxiliares
public interface IJwtTokenService
{
    AuthenticationTokens GenerateTokens(UserDto user, string deviceFingerprint, string ipAddress);
    AuthenticationTokens RefreshTokens(string refreshToken, string deviceFingerprint, string ipAddress);
    ClaimsPrincipal ValidateAccessToken(string token);
    void RevokeRefreshToken(string refreshToken);
    void RevokeAllUserTokens(Guid userId);
    void CleanupExpiredTokens();
}

public class JwtOptions
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int AccessTokenExpirationMinutes { get; set; } = 15;
    public int RefreshTokenExpirationDays { get; set; } = 7;
    public bool ValidateIpAddress { get; set; } = false;
}

public class TokenInfo
{
    public string Token { get; set; } = string.Empty;
    public string TokenId { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public class RefreshTokenInfo
{
    public string TokenId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string DeviceFingerprint { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime? RevokedAt { get; set; }
} 