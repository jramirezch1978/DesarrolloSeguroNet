using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Azure.Identity;

Console.WriteLine("🎯 LABORATORIO 0: VERIFICACIÓN DEL ENTORNO");
Console.WriteLine("==========================================");
Console.WriteLine();

// Verificar .NET 9
var dotnetVersion = Environment.Version;
Console.WriteLine($"✅ .NET Version: {dotnetVersion}");

// Verificar que estamos en .NET 9
if (dotnetVersion.Major >= 9)
{
    Console.WriteLine("✅ .NET 9 está funcionando correctamente");
}
else
{
    Console.WriteLine("❌ Se requiere .NET 9 o superior");
    return -1;
}

// Configurar logging
using var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddLogging(builder =>
        {
            builder.AddConsole();
        });
    })
    .Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("✅ Logging configurado correctamente");

// Verificar Azure Identity
try
{
    var credential = new DefaultAzureCredential();
    Console.WriteLine("✅ Azure Identity SDK cargado correctamente");
    logger.LogInformation("Azure SDK funcionando");
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️  Azure Identity: {ex.Message}");
    logger.LogWarning("Azure Identity no está completamente configurado (normal en setup inicial)");
}

// Verificar variables de entorno básicas
Console.WriteLine();
Console.WriteLine("🔍 Variables de entorno:");
Console.WriteLine($"   Usuario: {Environment.UserName}");
Console.WriteLine($"   Máquina: {Environment.MachineName}");
Console.WriteLine($"   OS: {Environment.OSVersion}");
Console.WriteLine($"   Directorio actual: {Environment.CurrentDirectory}");

Console.WriteLine();
Console.WriteLine("🎉 VERIFICACIÓN DEL ENTORNO COMPLETADA");
Console.WriteLine("✅ El entorno está listo para los laboratorios de infraestructura Azure");
Console.WriteLine();
Console.WriteLine("📋 Próximo paso: Ejecutar Laboratorio 1 - Virtual Network");

return 0; 