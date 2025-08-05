# 🔒 Laboratorio 40: Encriptación de Datos de Aplicación

**Duración:** 15 minutos  
**Objetivo:** Implementar encriptación automática en Entity Framework Core

## 📋 Funcionalidades Implementadas

### 1. Servicio de Encriptación
- Interfaz `IEncryptionService` para encriptación/desencriptación
- Implementación simulada con Azure Key Vault
- Gestión automática de claves de cifrado
- Logs detallados de operaciones

### 2. Value Converters para EF Core
- Converter para strings cifrados
- Converter para decimales cifrados
- Factory pattern para gestión de dependencias
- Manejo de errores robusto

### 3. Modelos con Campos Cifrados
- Customer: Email, Teléfono, Dirección, Tarjeta de Crédito
- Product: Costo (información comercial sensible)
- Configuración automática en DbContext

## 🔧 Implementación Clave

### Servicio de Encriptación

```csharp
public interface IEncryptionService
{
    Task<string> EncryptAsync(string plainText);
    Task<string> DecryptAsync(string encryptedText);
    Task<string> GetKeyVersionAsync();
}

public class KeyVaultEncryptionService : IEncryptionService
{
    public async Task<string> EncryptAsync(string plainText)
    {
        // Simulación - en producción usar Azure Key Vault
        var encrypted = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes($"ENC:{plainText}"));
        return encrypted;
    }

    public async Task<string> DecryptAsync(string encryptedText)
    {
        var decoded = System.Text.Encoding.UTF8.GetString(
            Convert.FromBase64String(encryptedText));
        return decoded.StartsWith("ENC:") ? decoded.Substring(4) : decoded;
    }
}
```

### Value Converters

```csharp
public class EncryptedStringConverter : ValueConverter<string?, string?>
{
    public EncryptedStringConverter(IEncryptionService encryptionService, ILogger logger)
        : base(
            // Cifrar al guardar en DB
            plainText => plainText == null ? null : EncryptValue(encryptionService, plainText),
            // Descifrar al leer de DB  
            encryptedText => encryptedText == null ? null : DecryptValue(encryptionService, encryptedText))
    {
    }
}
```

### Configuración en DbContext

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Customer con campos cifrados
    modelBuilder.Entity<Customer>(entity =>
    {
        entity.Property(e => e.Email)
            .HasConversion(_converterFactory.CreateStringConverter());
            
        entity.Property(e => e.PhoneNumber)
            .HasConversion(_converterFactory.CreateStringConverter());
            
        entity.Property(e => e.CreditCardNumber)
            .HasConversion(_converterFactory.CreateStringConverter());
    });

    // Product con costo cifrado
    modelBuilder.Entity<Product>(entity =>
    {
        entity.Property(e => e.Cost)
            .HasConversion(_converterFactory.CreateDecimalConverter());
    });
}
```

## 🔐 Campos Protegidos

| Entidad | Campo | Tipo | Razón de Protección |
|---------|-------|------|---------------------|
| **Customer** | Email | String | PII - GDPR |
| **Customer** | PhoneNumber | String | PII - GDPR |
| **Customer** | Address | String | PII - GDPR |
| **Customer** | CreditCardNumber | String | PCI DSS |
| **Product** | Cost | Decimal | Información comercial sensible |

## 🚀 Testing

```powershell
# Compilar
cd Lab40-Encriptacion-Datos
dotnet build

# Ejecutar
dotnet run --project SecureShop.Web
```

## ✅ Criterios de Completitud

- [ ] Servicio de encriptación implementado
- [ ] Value converters funcionando
- [ ] Campos sensibles cifrados automáticamente
- [ ] DbContext configurado correctamente
- [ ] Aplicación compila sin errores
- [ ] Testing de encriptación/desencriptación exitoso

---

## 🎯 Próximo Laboratorio

**Lab 41: Pruebas Integrales de Seguridad** - Testing automatizado de vulnerabilidades OWASP y compliance.