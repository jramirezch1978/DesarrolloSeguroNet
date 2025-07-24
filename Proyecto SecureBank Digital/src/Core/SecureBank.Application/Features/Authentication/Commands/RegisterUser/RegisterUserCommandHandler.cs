using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using SecureBank.Application.Common.Interfaces;
using SecureBank.Domain.Entities;
using SecureBank.Domain.ValueObjects;
using SecureBank.Domain.Enums;

namespace SecureBank.Application.Features.Authentication.Commands.RegisterUser;

/// <summary>
/// Handler para el comando de registro de usuario
/// Implementa proceso de registro seguro con validaciones multi-paso
/// </summary>
public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, RegisterUserResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<RegisterUserCommandHandler> _logger;

    public RegisterUserCommandHandler(
        IApplicationDbContext context,
        IEncryptionService encryptionService,
        ICurrentUserService currentUserService,
        ILogger<RegisterUserCommandHandler> logger)
    {
        _context = context;
        _encryptionService = encryptionService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<RegisterUserResponse> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var response = new RegisterUserResponse();

        try
        {
            // 1. Validar duplicados
            var validationResult = await ValidateUniqueConstraints(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                response.Success = false;
                response.Errors = validationResult.Errors;
                response.Message = "Error en la validación de datos";
                return response;
            }

            // 2. Crear address si se proporcionó
            Address? address = null;
            if (request.Address != null)
            {
                address = new Address(
                    request.Address.Street,
                    request.Address.Number,
                    request.Address.District,
                    request.Address.Province,
                    request.Address.Department,
                    request.Address.PostalCode,
                    request.Address.Apartment,
                    request.Address.Country
                );
            }

            // 3. Hashear PIN y respuesta de seguridad
            var hashedPin = _encryptionService.HashPassword(request.Pin);
            var hashedSecurityAnswer = _encryptionService.HashPassword(request.SecurityAnswer.ToLowerInvariant().Trim());

            // 4. Crear usuario
            var user = new User(
                request.Email,
                request.DocumentNumber,
                request.DocumentType,
                request.FirstName,
                request.LastName,
                request.PhoneNumber,
                hashedPin,
                request.SecurityQuestion,
                hashedSecurityAnswer,
                request.IpAddress,
                address
            );

            // 5. Aceptar términos y condiciones
            user.AcceptTerms(request.IpAddress);

            // 6. Agregar usuario al contexto
            _context.Users.Add(user);

            // 7. Crear entrada de auditoría
            var auditLog = new AuditLog(
                AuditAction.UserCreated,
                nameof(User),
                $"Usuario registrado: {request.Email}",
                request.IpAddress,
                request.UserAgent,
                AuditLevel.Information,
                userId: user.Id,
                additionalData: $"DocumentType: {request.DocumentType}, DocumentNumber: {request.DocumentNumber}"
            );
            _context.AuditLogs.Add(auditLog);

            // 8. Registrar dispositivo inicial
            if (!string.IsNullOrWhiteSpace(request.DeviceFingerprint))
            {
                var device = await CreateInitialDevice(user.Id, request, cancellationToken);
                _context.UserDevices.Add(device);
            }

            // 9. Generar tokens de verificación
            var emailToken = _encryptionService.GenerateSecureToken(32);
            var phoneCode = _encryptionService.GenerateNumericCode(6);

            // 10. Guardar cambios
            await _context.SaveChangesAsync(cancellationToken);

            // 11. Crear cuenta inicial automáticamente (Ahorros)
            await CreateInitialAccount(user.Id, request.IpAddress, cancellationToken);

            // 12. Preparar respuesta exitosa
            response.Success = true;
            response.UserId = user.Id;
            response.Message = "Usuario registrado exitosamente. Se han enviado códigos de verificación.";
            response.EmailVerificationToken = emailToken;
            response.PhoneVerificationCode = phoneCode;
            response.TokenExpiresAt = DateTime.UtcNow.AddHours(24);
            response.RequiredSteps = new RegisterUserSteps
            {
                RequiresEmailVerification = true,
                RequiresPhoneVerification = true,
                RequiresDocumentVerification = false, // Simulado como completado
                RequiresBiometricSetup = false,
                RequiresTwoFactorSetup = true,
                PendingVerifications = new List<string> { "Email", "Teléfono", "Configuración 2FA" }
            };

            _logger.LogInformation("Usuario registrado exitosamente. UserId: {UserId}, Email: {Email}", 
                user.Id, request.Email);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registrando usuario: {Email}", request.Email);
            
            response.Success = false;
            response.Message = "Error interno del servidor durante el registro";
            response.Errors.Add("Ha ocurrido un error inesperado. Por favor intente nuevamente.");
            
            return response;
        }
    }

    private async Task<ValidationResult> ValidateUniqueConstraints(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var result = new ValidationResult { IsValid = true };

        // Verificar email duplicado
        var emailExists = await _context.Users
            .AnyAsync(u => u.Email == request.Email.ToLowerInvariant(), cancellationToken);
        
        if (emailExists)
        {
            result.IsValid = false;
            result.Errors.Add("Ya existe un usuario registrado con este email");
        }

        // Verificar documento duplicado
        var documentExists = await _context.Users
            .AnyAsync(u => u.DocumentNumber == request.DocumentNumber && u.DocumentType == request.DocumentType, 
                     cancellationToken);
        
        if (documentExists)
        {
            result.IsValid = false;
            result.Errors.Add("Ya existe un usuario registrado con este documento");
        }

        // Verificar teléfono duplicado
        var phoneExists = await _context.Users
            .AnyAsync(u => u.PhoneNumber == request.PhoneNumber, cancellationToken);
        
        if (phoneExists)
        {
            result.IsValid = false;
            result.Errors.Add("Ya existe un usuario registrado con este número de teléfono");
        }

        return result;
    }

    private async Task<UserDevice> CreateInitialDevice(Guid userId, RegisterUserCommand request, CancellationToken cancellationToken)
    {
        // Parsear User Agent para extraer información del dispositivo
        var deviceInfo = ParseUserAgent(request.UserAgent);
        
        var device = new UserDevice(
            userId,
            request.DeviceFingerprint!,
            deviceInfo.DeviceName,
            deviceInfo.DeviceType,
            deviceInfo.OperatingSystem,
            deviceInfo.Browser,
            request.UserAgent,
            request.IpAddress,
            "Unknown", // País - se obtendría de un servicio de geolocalización
            "Unknown"  // Ciudad - se obtendría de un servicio de geolocalización
        );

        return device;
    }

    private async Task CreateInitialAccount(Guid userId, string ipAddress, CancellationToken cancellationToken)
    {
        // Crear cuenta de ahorros inicial automáticamente
        var account = new Account(userId, AccountType.Savings, ipAddress);
        
        _context.Accounts.Add(account);

        // Crear entrada de auditoría para la cuenta
        var auditLog = new AuditLog(
            AuditAction.AccountCreated,
            nameof(Account),
            $"Cuenta inicial creada automáticamente: {account.AccountNumber}",
            ipAddress,
            "System",
            AuditLevel.Information,
            userId: userId,
            accountId: account.Id
        );
        _context.AuditLogs.Add(auditLog);

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static DeviceInfo ParseUserAgent(string userAgent)
    {
        // Implementación simplificada del parsing de User Agent
        // En producción usaríamos una librería especializada como UAParser
        
        var deviceInfo = new DeviceInfo
        {
            DeviceName = "Dispositivo Desconocido",
            DeviceType = "Unknown",
            OperatingSystem = "Unknown",
            Browser = "Unknown"
        };

        if (string.IsNullOrWhiteSpace(userAgent))
            return deviceInfo;

        var ua = userAgent.ToLowerInvariant();

        // Detectar tipo de dispositivo
        if (ua.Contains("mobile") || ua.Contains("android") || ua.Contains("iphone"))
        {
            deviceInfo.DeviceType = "Mobile";
            deviceInfo.DeviceName = ua.Contains("iphone") ? "iPhone" : "Dispositivo Móvil";
        }
        else if (ua.Contains("tablet") || ua.Contains("ipad"))
        {
            deviceInfo.DeviceType = "Tablet";
            deviceInfo.DeviceName = ua.Contains("ipad") ? "iPad" : "Tablet";
        }
        else
        {
            deviceInfo.DeviceType = "Desktop";
            deviceInfo.DeviceName = "Computadora";
        }

        // Detectar sistema operativo
        if (ua.Contains("windows"))
            deviceInfo.OperatingSystem = "Windows";
        else if (ua.Contains("mac os") || ua.Contains("macos"))
            deviceInfo.OperatingSystem = "macOS";
        else if (ua.Contains("linux"))
            deviceInfo.OperatingSystem = "Linux";
        else if (ua.Contains("android"))
            deviceInfo.OperatingSystem = "Android";
        else if (ua.Contains("ios") || ua.Contains("iphone") || ua.Contains("ipad"))
            deviceInfo.OperatingSystem = "iOS";

        // Detectar navegador
        if (ua.Contains("chrome"))
            deviceInfo.Browser = "Chrome";
        else if (ua.Contains("firefox"))
            deviceInfo.Browser = "Firefox";
        else if (ua.Contains("safari"))
            deviceInfo.Browser = "Safari";
        else if (ua.Contains("edge"))
            deviceInfo.Browser = "Edge";

        return deviceInfo;
    }

    private class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    private class DeviceInfo
    {
        public string DeviceName { get; set; } = string.Empty;
        public string DeviceType { get; set; } = string.Empty;
        public string OperatingSystem { get; set; } = string.Empty;
        public string Browser { get; set; } = string.Empty;
    }
} 