using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SecureShop.Security;

// Converter para campos de texto cifrados
public class EncryptedStringConverter : ValueConverter<string?, string?>
{
    public EncryptedStringConverter(IEncryptionService encryptionService, ILogger logger)
        : base(
            // Convertir de string a string cifrado (para guardar en DB)
            plainText => plainText == null ? null : EncryptValue(encryptionService, plainText, logger),
            // Convertir de string cifrado a string plano (para leer de DB)
            encryptedText => encryptedText == null ? null : DecryptValue(encryptionService, encryptedText, logger))
    {
    }

    private static string EncryptValue(IEncryptionService encryptionService, string plainText, ILogger logger)
    {
        try
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            // Usar método síncrono para compatibilidad con EF Core
            var task = encryptionService.EncryptAsync(plainText);
            task.Wait();
            return task.Result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error encriptando valor");
            throw new InvalidOperationException("Error en proceso de cifrado", ex);
        }
    }

    private static string DecryptValue(IEncryptionService encryptionService, string encryptedText, ILogger logger)
    {
        try
        {
            if (string.IsNullOrEmpty(encryptedText))
                return encryptedText;

            // Usar método síncrono para compatibilidad con EF Core
            var task = encryptionService.DecryptAsync(encryptedText);
            task.Wait();
            return task.Result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error desencriptando valor");
            // En caso de error, devolver valor indicativo en lugar de fallar
            return "[ENCRYPTED_DATA_ERROR]";
        }
    }
}

// Converter para números decimales cifrados
public class EncryptedDecimalConverter : ValueConverter<decimal?, string?>
{
    public EncryptedDecimalConverter(IEncryptionService encryptionService, ILogger logger)
        : base(
            // Convertir de decimal a string cifrado
            plainValue => plainValue == null ? null : EncryptDecimal(encryptionService, plainValue.Value, logger),
            // Convertir de string cifrado a decimal
            encryptedText => encryptedText == null ? null : DecryptDecimal(encryptionService, encryptedText, logger))
    {
    }

    private static string EncryptDecimal(IEncryptionService encryptionService, decimal plainValue, ILogger logger)
    {
        try
        {
            var plainText = plainValue.ToString("F4"); // 4 decimales de precisión
            var task = encryptionService.EncryptAsync(plainText);
            task.Wait();
            return task.Result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error encriptando valor decimal");
            throw new InvalidOperationException("Error en proceso de cifrado decimal", ex);
        }
    }

    private static decimal? DecryptDecimal(IEncryptionService encryptionService, string encryptedText, ILogger logger)
    {
        try
        {
            var task = encryptionService.DecryptAsync(encryptedText);
            task.Wait();
            var plainText = task.Result;
            
            if (decimal.TryParse(plainText, out var result))
            {
                return result;
            }
            
            logger.LogWarning("No se pudo convertir valor desencriptado a decimal: {Value}", plainText);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error desencriptando valor decimal");
            return null;
        }
    }
}

// Factory para crear converters con dependencias
public class EncryptionConverterFactory
{
    private readonly IServiceProvider _serviceProvider;

    public EncryptionConverterFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public EncryptedStringConverter CreateStringConverter()
    {
        var encryptionService = _serviceProvider.GetRequiredService<IEncryptionService>();
        var logger = _serviceProvider.GetRequiredService<ILogger<EncryptedStringConverter>>();
        return new EncryptedStringConverter(encryptionService, logger);
    }

    public EncryptedDecimalConverter CreateDecimalConverter()
    {
        var encryptionService = _serviceProvider.GetRequiredService<IEncryptionService>();
        var logger = _serviceProvider.GetRequiredService<ILogger<EncryptedDecimalConverter>>();
        return new EncryptedDecimalConverter(encryptionService, logger);
    }
}