namespace SecureBank.Domain.ValueObjects;

/// <summary>
/// Value Object para direcciones en SecureBank Digital
/// Diseñado para el contexto peruano con validaciones específicas
/// </summary>
public class Address : IEquatable<Address>
{
    public string Street { get; private set; }
    public string Number { get; private set; }
    public string? Apartment { get; private set; }
    public string District { get; private set; }
    public string Province { get; private set; }
    public string Department { get; private set; }
    public string PostalCode { get; private set; }
    public string Country { get; private set; }

    // Constructor privado para EF Core
    private Address() 
    { 
        Street = string.Empty;
        Number = string.Empty;
        District = string.Empty;
        Province = string.Empty;
        Department = string.Empty;
        PostalCode = string.Empty;
        Country = string.Empty;
    }

    /// <summary>
    /// Constructor para crear una nueva dirección con validaciones
    /// </summary>
    public Address(
        string street,
        string number,
        string district,
        string province,
        string department,
        string postalCode,
        string? apartment = null,
        string country = "PERU")
    {
        Street = ValidateStreet(street);
        Number = ValidateNumber(number);
        District = ValidateDistrict(district);
        Province = ValidateProvince(province);
        Department = ValidateDepartment(department);
        PostalCode = ValidatePostalCode(postalCode);
        Apartment = ValidateApartment(apartment);
        Country = ValidateCountry(country);
    }

    /// <summary>
    /// Obtiene la dirección completa formateada
    /// </summary>
    public string GetFullAddress()
    {
        var apartment = !string.IsNullOrWhiteSpace(Apartment) ? $" {Apartment}" : "";
        return $"{Street} {Number}{apartment}, {District}, {Province}, {Department}, {Country} {PostalCode}";
    }

    /// <summary>
    /// Obtiene la dirección sin el país y código postal
    /// </summary>
    public string GetLocalAddress()
    {
        var apartment = !string.IsNullOrWhiteSpace(Apartment) ? $" {Apartment}" : "";
        return $"{Street} {Number}{apartment}, {District}, {Province}, {Department}";
    }

    /// <summary>
    /// Verifica si es una dirección en Lima Metropolitana
    /// </summary>
    public bool IsLimaMetropolitan()
    {
        return Department.ToUpperInvariant() == "LIMA" && 
               (Province.ToUpperInvariant() == "LIMA" || Province.ToUpperInvariant() == "CALLAO");
    }

    // Implementación de IEquatable<Address>
    public bool Equals(Address? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return Street == other.Street &&
               Number == other.Number &&
               Apartment == other.Apartment &&
               District == other.District &&
               Province == other.Province &&
               Department == other.Department &&
               PostalCode == other.PostalCode &&
               Country == other.Country;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Address);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            Street, Number, Apartment, District, 
            Province, Department, PostalCode, Country);
    }

    public static bool operator ==(Address? left, Address? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Address? left, Address? right)
    {
        return !Equals(left, right);
    }

    // Métodos de validación privados
    private static string ValidateStreet(string street)
    {
        if (string.IsNullOrWhiteSpace(street))
            throw new ArgumentException("La calle es obligatoria", nameof(street));

        if (street.Length < 3 || street.Length > 100)
            throw new ArgumentException("La calle debe tener entre 3 y 100 caracteres", nameof(street));

        return street.Trim();
    }

    private static string ValidateNumber(string number)
    {
        if (string.IsNullOrWhiteSpace(number))
            throw new ArgumentException("El número es obligatorio", nameof(number));

        if (number.Length > 10)
            throw new ArgumentException("El número no puede tener más de 10 caracteres", nameof(number));

        return number.Trim();
    }

    private static string? ValidateApartment(string? apartment)
    {
        if (string.IsNullOrWhiteSpace(apartment))
            return null;

        if (apartment.Length > 20)
            throw new ArgumentException("El apartamento no puede tener más de 20 caracteres", nameof(apartment));

        return apartment.Trim();
    }

    private static string ValidateDistrict(string district)
    {
        if (string.IsNullOrWhiteSpace(district))
            throw new ArgumentException("El distrito es obligatorio", nameof(district));

        if (district.Length < 2 || district.Length > 50)
            throw new ArgumentException("El distrito debe tener entre 2 y 50 caracteres", nameof(district));

        return district.Trim().ToUpperInvariant();
    }

    private static string ValidateProvince(string province)
    {
        if (string.IsNullOrWhiteSpace(province))
            throw new ArgumentException("La provincia es obligatoria", nameof(province));

        if (province.Length < 2 || province.Length > 50)
            throw new ArgumentException("La provincia debe tener entre 2 y 50 caracteres", nameof(province));

        return province.Trim().ToUpperInvariant();
    }

    private static string ValidateDepartment(string department)
    {
        if (string.IsNullOrWhiteSpace(department))
            throw new ArgumentException("El departamento es obligatorio", nameof(department));

        // Lista de departamentos válidos del Perú
        var validDepartments = new[]
        {
            "AMAZONAS", "ANCASH", "APURIMAC", "AREQUIPA", "AYACUCHO", "CAJAMARCA",
            "CALLAO", "CUSCO", "HUANCAVELICA", "HUANUCO", "ICA", "JUNIN",
            "LA LIBERTAD", "LAMBAYEQUE", "LIMA", "LORETO", "MADRE DE DIOS",
            "MOQUEGUA", "PASCO", "PIURA", "PUNO", "SAN MARTIN", "TACNA",
            "TUMBES", "UCAYALI"
        };

        var normalizedDepartment = department.Trim().ToUpperInvariant();
        
        if (!validDepartments.Contains(normalizedDepartment))
            throw new ArgumentException("Departamento no válido para Perú", nameof(department));

        return normalizedDepartment;
    }

    private static string ValidatePostalCode(string postalCode)
    {
        if (string.IsNullOrWhiteSpace(postalCode))
            throw new ArgumentException("El código postal es obligatorio", nameof(postalCode));

        // Validación básica para códigos postales peruanos (5 dígitos)
        if (postalCode.Length != 5 || !postalCode.All(char.IsDigit))
            throw new ArgumentException("El código postal debe tener 5 dígitos", nameof(postalCode));

        return postalCode.Trim();
    }

    private static string ValidateCountry(string country)
    {
        if (string.IsNullOrWhiteSpace(country))
            throw new ArgumentException("El país es obligatorio", nameof(country));

        var normalizedCountry = country.Trim().ToUpperInvariant();
        
        // Por defecto aceptamos solo Perú, pero se puede extender
        var validCountries = new[] { "PERU", "PERÚ" };
        
        if (!validCountries.Contains(normalizedCountry))
            throw new ArgumentException("País no soportado", nameof(country));

        return "PERU"; // Normalizamos a PERU sin tilde
    }
} 