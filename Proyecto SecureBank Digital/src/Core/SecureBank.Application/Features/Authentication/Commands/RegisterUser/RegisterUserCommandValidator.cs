using FluentValidation;

namespace SecureBank.Application.Features.Authentication.Commands.RegisterUser;

/// <summary>
/// Validator para el comando de registro de usuario
/// Implementa validaciones robustas según los estándares de seguridad de SecureBank Digital
/// </summary>
public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        // Validación de Email
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es obligatorio")
            .EmailAddress().WithMessage("El formato del email no es válido")
            .MaximumLength(254).WithMessage("El email no puede tener más de 254 caracteres")
            .MustAsync(BeValidEmailDomain).WithMessage("El dominio del email no está permitido");

        // Validación de Documento
        RuleFor(x => x.DocumentNumber)
            .NotEmpty().WithMessage("El número de documento es obligatorio")
            .Length(8, 20).WithMessage("El número de documento debe tener entre 8 y 20 caracteres")
            .Matches(@"^[0-9A-Za-z]+$").WithMessage("El número de documento solo puede contener letras y números");

        RuleFor(x => x.DocumentType)
            .NotEmpty().WithMessage("El tipo de documento es obligatorio")
            .Must(BeValidDocumentType).WithMessage("Tipo de documento no válido");

        // Validación de Nombres
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("El nombre es obligatorio")
            .Length(2, 50).WithMessage("El nombre debe tener entre 2 y 50 caracteres")
            .Matches(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$").WithMessage("El nombre solo puede contener letras y espacios");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("El apellido es obligatorio")
            .Length(2, 50).WithMessage("El apellido debe tener entre 2 y 50 caracteres")
            .Matches(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$").WithMessage("El apellido solo puede contener letras y espacios");

        // Validación de Teléfono
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("El número de teléfono es obligatorio")
            .Matches(@"^\+?[1-9]\d{8,14}$").WithMessage("El formato del teléfono no es válido (debe tener entre 9 y 15 dígitos)");

        // Validación de PIN - Seguridad robusta
        RuleFor(x => x.Pin)
            .NotEmpty().WithMessage("El PIN es obligatorio")
            .Length(6, 6).WithMessage("El PIN debe tener exactamente 6 dígitos")
            .Matches(@"^\d{6}$").WithMessage("El PIN solo puede contener números")
            .Must(BeSecurePin).WithMessage("El PIN no es seguro (no puede ser secuencial, repetitivo o fechas comunes)");

        // Validación de confirmación de PIN
        RuleFor(x => x.ConfirmPin)
            .NotEmpty().WithMessage("La confirmación de PIN es obligatoria")
            .Equal(x => x.Pin).WithMessage("La confirmación de PIN no coincide");

        // Validación de Pregunta de Seguridad
        RuleFor(x => x.SecurityQuestion)
            .NotEmpty().WithMessage("La pregunta de seguridad es obligatoria")
            .Length(10, 200).WithMessage("La pregunta debe tener entre 10 y 200 caracteres")
            .Must(BeValidSecurityQuestion).WithMessage("La pregunta de seguridad debe ser más específica");

        // Validación de Respuesta de Seguridad
        RuleFor(x => x.SecurityAnswer)
            .NotEmpty().WithMessage("La respuesta de seguridad es obligatoria")
            .Length(3, 100).WithMessage("La respuesta debe tener entre 3 y 100 caracteres")
            .Must(BeSecureAnswer).WithMessage("La respuesta debe ser más compleja");

        // Validación de Dirección (opcional pero si se proporciona debe ser válida)
        When(x => x.Address != null, () =>
        {
            RuleFor(x => x.Address!.Street)
                .NotEmpty().WithMessage("La calle es obligatoria")
                .Length(3, 100).WithMessage("La calle debe tener entre 3 y 100 caracteres");

            RuleFor(x => x.Address!.Number)
                .NotEmpty().WithMessage("El número es obligatorio")
                .MaximumLength(10).WithMessage("El número no puede tener más de 10 caracteres");

            RuleFor(x => x.Address!.District)
                .NotEmpty().WithMessage("El distrito es obligatorio")
                .Length(2, 50).WithMessage("El distrito debe tener entre 2 y 50 caracteres");

            RuleFor(x => x.Address!.Province)
                .NotEmpty().WithMessage("La provincia es obligatoria")
                .Length(2, 50).WithMessage("La provincia debe tener entre 2 y 50 caracteres");

            RuleFor(x => x.Address!.Department)
                .NotEmpty().WithMessage("El departamento es obligatorio")
                .Must(BeValidPeruvianDepartment).WithMessage("Departamento no válido para Perú");

            RuleFor(x => x.Address!.PostalCode)
                .NotEmpty().WithMessage("El código postal es obligatorio")
                .Length(5, 5).WithMessage("El código postal debe tener 5 dígitos")
                .Matches(@"^\d{5}$").WithMessage("El código postal solo puede contener números");
        });

        // Validación de Términos y Condiciones
        RuleFor(x => x.AcceptTerms)
            .Equal(true).WithMessage("Debe aceptar los términos y condiciones");

        // Validación de información de red
        RuleFor(x => x.IpAddress)
            .NotEmpty().WithMessage("La dirección IP es obligatoria")
            .Must(BeValidIpAddress).WithMessage("Formato de dirección IP no válido");

        RuleFor(x => x.UserAgent)
            .NotEmpty().WithMessage("La información del navegador es obligatoria")
            .MaximumLength(500).WithMessage("La información del navegador es demasiado larga");

        // Validaciones complejas
        RuleFor(x => x)
            .Must(HaveValidDocumentForType).WithMessage("El número de documento no es válido para el tipo especificado")
            .Must(HaveConsistentPersonalData).WithMessage("Los datos personales no son consistentes");
    }

    private static bool BeValidDocumentType(string documentType)
    {
        var validTypes = new[] { "DNI", "CE", "PASSPORT", "RUC" };
        return validTypes.Contains(documentType?.ToUpperInvariant());
    }

    private static bool BeSecurePin(string pin)
    {
        if (string.IsNullOrWhiteSpace(pin) || pin.Length != 6)
            return false;

        // No debe ser secuencial (123456, 654321)
        if (IsSequential(pin))
            return false;

        // No debe ser repetitivo (111111, 222222)
        if (pin.Distinct().Count() <= 2)
            return false;

        // No debe ser fechas comunes (año actual, 123456, 000000)
        var commonPins = new[]
        {
            "123456", "654321", "111111", "222222", "333333", "444444", "555555",
            "666666", "777777", "888888", "999999", "000000", "012345", "543210",
            DateTime.Now.Year.ToString().PadLeft(6, '0').Substring(0, 6),
            (DateTime.Now.Year - 1).ToString().PadLeft(6, '0').Substring(0, 6)
        };

        return !commonPins.Contains(pin);
    }

    private static bool IsSequential(string pin)
    {
        // Verificar secuencia ascendente
        bool ascending = true;
        bool descending = true;

        for (int i = 1; i < pin.Length; i++)
        {
            if (int.Parse(pin[i].ToString()) != int.Parse(pin[i - 1].ToString()) + 1)
                ascending = false;
            if (int.Parse(pin[i].ToString()) != int.Parse(pin[i - 1].ToString()) - 1)
                descending = false;
        }

        return ascending || descending;
    }

    private static bool BeValidSecurityQuestion(string question)
    {
        if (string.IsNullOrWhiteSpace(question))
            return false;

        // La pregunta debe tener al menos una palabra interrogativa o ser específica
        var validStarters = new[] { "¿cuál", "¿dónde", "¿cómo", "¿quién", "¿qué", "¿cuándo", "¿por qué" };
        var lowerQuestion = question.ToLowerInvariant();

        return validStarters.Any(starter => lowerQuestion.Contains(starter)) ||
               lowerQuestion.Contains("nombre") ||
               lowerQuestion.Contains("lugar") ||
               lowerQuestion.Contains("fecha") ||
               lowerQuestion.Contains("color") ||
               lowerQuestion.Contains("animal");
    }

    private static bool BeSecureAnswer(string answer)
    {
        if (string.IsNullOrWhiteSpace(answer))
            return false;

        // La respuesta debe tener al menos 3 caracteres y no ser demasiado común
        var commonAnswers = new[] { "si", "no", "123", "abc", "aaa", "000", "ninguno", "nada" };
        return !commonAnswers.Contains(answer.ToLowerInvariant().Trim());
    }

    private static bool BeValidPeruvianDepartment(string department)
    {
        var validDepartments = new[]
        {
            "AMAZONAS", "ANCASH", "APURIMAC", "AREQUIPA", "AYACUCHO", "CAJAMARCA",
            "CALLAO", "CUSCO", "HUANCAVELICA", "HUANUCO", "ICA", "JUNIN",
            "LA LIBERTAD", "LAMBAYEQUE", "LIMA", "LORETO", "MADRE DE DIOS",
            "MOQUEGUA", "PASCO", "PIURA", "PUNO", "SAN MARTIN", "TACNA",
            "TUMBES", "UCAYALI"
        };

        return validDepartments.Contains(department?.ToUpperInvariant());
    }

    private static bool BeValidIpAddress(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return false;

        // Validación básica de IPv4 y IPv6
        return System.Net.IPAddress.TryParse(ipAddress, out _);
    }

    private static async Task<bool> BeValidEmailDomain(string email, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return false;

        var domain = email.Split('@')[1].ToLowerInvariant();
        
        // Lista de dominios bloqueados (temporales, sospechosos)
        var blockedDomains = new[]
        {
            "10minutemail.com", "tempmail.org", "mailinator.com", "guerrillamail.com",
            "temp-mail.org", "throwaway.email", "maildrop.cc"
        };

        return !blockedDomains.Contains(domain);
    }

    private static bool HaveValidDocumentForType(RegisterUserCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.DocumentType) || string.IsNullOrWhiteSpace(command.DocumentNumber))
            return true; // Se valida en otros lugares

        return command.DocumentType.ToUpperInvariant() switch
        {
            "DNI" => command.DocumentNumber.Length == 8 && command.DocumentNumber.All(char.IsDigit),
            "CE" => command.DocumentNumber.Length >= 9 && command.DocumentNumber.Length <= 12,
            "PASSPORT" => command.DocumentNumber.Length >= 6 && command.DocumentNumber.Length <= 15,
            "RUC" => command.DocumentNumber.Length == 11 && command.DocumentNumber.All(char.IsDigit),
            _ => false
        };
    }

    private static bool HaveConsistentPersonalData(RegisterUserCommand command)
    {
        // Verificar que el nombre no contenga números
        if (command.FirstName.Any(char.IsDigit) || command.LastName.Any(char.IsDigit))
            return false;

        // Verificar que el email no sea obviamente falso
        var fullName = $"{command.FirstName}{command.LastName}".ToLowerInvariant();
        var emailLocal = command.Email.Split('@')[0].ToLowerInvariant();
        
        // Si el email es muy diferente al nombre, podría ser sospechoso (pero no lo bloqueamos)
        // Esta es una validación informativa

        return true;
    }
} 