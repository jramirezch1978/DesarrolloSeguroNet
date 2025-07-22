# SecureBank Digital - Proyecto de Banca Digital Segura

## Historia de SecureBank Digital

**SecureBank Digital** fue fundado en 2018 en Lima, Perú, con una visión revolucionaria: crear el primer banco 100% digital del país con un enfoque inquebrantable en la seguridad cibernética. 

### Nuestra Filosofía
> **"Security First, Innovation Always"** - La seguridad es nuestro fundamento, la innovación es nuestro motor.

### Misión
Democratizar el acceso a servicios financieros seguros y modernos, proporcionando a los peruanos una experiencia bancaria digital que combine:
- **Seguridad de nivel mundial**
- **Innovación tecnológica constante**
- **Accesibilidad universal**
- **Transparencia total**

### Evolución y Crecimiento

#### 2018 - Fundación
- Creación del concepto SecureBank Digital
- Desarrollo del marco de seguridad inicial
- Registro ante la SBS (Superintendencia de Banca y Seguros)

#### 2019 - Lanzamiento MVP
- Productos básicos: Cuentas de ahorro y corriente
- Transferencias interbancarias
- 10,000 primeros usuarios

#### 2020 - Expansión de Servicios
- Tarjetas de débito virtuales y físicas
- Pagos QR y contactless
- 50,000 usuarios activos

#### 2021 - Era Digital
- Implementación de inteligencia artificial para prevención de fraudes
- Lanzamiento de productos de inversión
- 150,000 usuarios activos

#### 2022 - Consolidación
- Introducción de servicios de préstamos personales
- Partnership con fintechs locales
- 300,000 usuarios activos

#### 2023 - Innovación Avanzada
- Implementación de blockchain para auditorías
- Servicios de criptomonedas reguladas
- 500,000 usuarios activos

#### 2024 - Expansión Regional (Proyectada)
- Lanzamiento en Colombia y Chile
- Productos empresariales avanzados
- Meta: 1,000,000 usuarios

#### 2025 - Open Banking (Proyectada)
- Plataforma abierta para terceros
- Servicios financieros como servicio (FaaS)
- Liderazgo regional en innovación

### Valores Corporativos

1. **Seguridad Primero** - Cada decisión se evalúa bajo la lente de la seguridad
2. **Innovación Responsable** - Tecnología de vanguardia con implementación cuidadosa
3. **Transparencia Radical** - Comunicación abierta y honesta con todos los stakeholders
4. **Excelencia Operacional** - Procesos optimizados y mejora continua
5. **Inclusión Digital** - Acceso financiero para todos los segmentos de la población
6. **Sostenibilidad** - Compromiso con el medio ambiente y la responsabilidad social

## Arquitectura Tecnológica

### Stack Tecnológico Principal
- **.NET 9** - Framework principal
- **PostgreSQL en Azure** - Base de datos principal
- **Azure Key Vault** - Gestión de secretos y claves
- **Azure Monitor** - Telemetría y Machine Learning
- **Docker** - Containerización
- **Azure Service Bus** - Mensajería asíncrona

### Arquitectura Modular Híbrida

El proyecto implementa una **arquitectura modular híbrida** que combina los principios de **Clean Architecture** con un enfoque de **microservicios modulares**:

#### Core/Domain Layer
- **SecureBank.Domain**: Entidades, value objects, enums
- **SecureBank.Application**: Commands, queries, DTOs, interfaces

#### Infrastructure Layer  
- **SecureBank.Infrastructure**: Data access, EF Core, PostgreSQL
- **SecureBank.Security**: JWT, encryption, Key Vault integration

#### API Services
- **SecureBank.AuthAPI**: Autenticación, registro, login, MFA
- **SecureBank.AccountAPI**: Gestión de cuentas, saldos
- **SecureBank.TransactionAPI**: Transferencias, pagos
- **SecureBank.ProductAPI**: Créditos, inversiones

#### Web Applications
- **SecureBank.WebApp**: Aplicación web cliente (MVC)
- **SecureBank.AdminPortal**: Panel administrativo

#### Cross-Cutting
- **SecureBank.Shared**: DTOs compartidos, utilidades
- **SecureBank.Tests**: Pruebas unitarias e integración

---

## Configuración de PostgreSQL en Azure

### 1. Creación de la Base de Datos PostgreSQL en Azure

#### Opción A: Portal de Azure (Interfaz Gráfica)

1. **Iniciar sesión en Azure Portal**
   - Navegar a https://portal.azure.com
   - Autenticarse con las credenciales de Azure

2. **Crear Azure Database for PostgreSQL**
   ```
   Servicios > Bases de datos > Azure Database for PostgreSQL
   ```

3. **Configuración del Servidor**
   ```
   Subscription: [Tu suscripción]
   Resource Group: rg-securebank-prod
   Server Name: securebank-postgresql-prod
   Region: East US 2
   PostgreSQL Version: 14
   Compute + Storage: General Purpose, 2 vCores, 100 GB SSD
   ```

4. **Configuración de Autenticación**
   ```
   Authentication Method: PostgreSQL authentication only
   Admin Username: securebank_admin
   Password: [Generar contraseña segura]
   ```

#### Opción B: Azure CLI (Recomendado para Automatización)

```bash
# Variables de configuración
RESOURCE_GROUP="rg-securebank-prod"
LOCATION="eastus2"
SERVER_NAME="securebank-postgresql-prod"
ADMIN_USER="securebank_admin"
ADMIN_PASSWORD="[GenerarPasswordSeguro123!]"
DATABASE_NAME="securebank_prod"

# Crear resource group
az group create --name $RESOURCE_GROUP --location $LOCATION

# Crear servidor PostgreSQL
az postgres server create \
  --resource-group $RESOURCE_GROUP \
  --name $SERVER_NAME \
  --location $LOCATION \
  --admin-user $ADMIN_USER \
  --admin-password $ADMIN_PASSWORD \
  --sku-name GP_Gen5_2 \
  --version 14 \
  --storage-size 102400 \
  --backup-retention 35 \
  --geo-redundant-backup Enabled \
  --ssl-enforcement Enabled

# Crear la base de datos
az postgres db create \
  --resource-group $RESOURCE_GROUP \
  --server-name $SERVER_NAME \
  --name $DATABASE_NAME

# Configurar firewall para Azure Services
az postgres server firewall-rule create \
  --resource-group $RESOURCE_GROUP \
  --server $SERVER_NAME \
  --name "AllowAzureServices" \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0
```

#### Opción C: Terraform (Infrastructure as Code)

```hcl
# main.tf
terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~>3.0"
    }
  }
}

provider "azurerm" {
  features {}
}

resource "azurerm_resource_group" "securebank" {
  name     = "rg-securebank-prod"
  location = "East US 2"
}

resource "azurerm_postgresql_server" "securebank_db" {
  name                = "securebank-postgresql-prod"
  location            = azurerm_resource_group.securebank.location
  resource_group_name = azurerm_resource_group.securebank.name

  administrator_login          = "securebank_admin"
  administrator_login_password = var.db_admin_password

  sku_name   = "GP_Gen5_2"
  version    = "14"
  storage_mb = 102400

  backup_retention_days        = 35
  geo_redundant_backup_enabled = true
  auto_grow_enabled           = true

  public_network_access_enabled = false
  ssl_enforcement_enabled       = true
  ssl_minimal_tls_version_enforced = "TLS1_2"

  threat_detection_policy {
    enabled = true
  }

  tags = {
    Environment = "Production"
    Project     = "SecureBank"
    Security    = "High"
  }
}

resource "azurerm_postgresql_database" "securebank_prod" {
  name                = "securebank_prod"
  resource_group_name = azurerm_resource_group.securebank.name
  server_name         = azurerm_postgresql_server.securebank_db.name
  charset             = "UTF8"
  collation           = "English_United States.1252"
}

resource "azurerm_postgresql_virtual_network_rule" "securebank_vnet_rule" {
  name                                 = "postgresql-vnet-rule"
  resource_group_name                  = azurerm_resource_group.securebank.name
  server_name                          = azurerm_postgresql_server.securebank_db.name
  subnet_id                           = azurerm_subnet.internal.id
  ignore_missing_vnet_service_endpoint = true
}

variable "db_admin_password" {
  description = "Password for PostgreSQL administrator"
  type        = string
  sensitive   = true
}
```

### 2. Configuración de Seguridad Avanzada

#### Configuración de Red Virtual (VNet)

```bash
# Crear VNet para aislamiento de red
az network vnet create \
  --resource-group $RESOURCE_GROUP \
  --name securebank-vnet \
  --address-prefix 10.0.0.0/16 \
  --subnet-name database-subnet \
  --subnet-prefix 10.0.1.0/24

# Configurar service endpoint para PostgreSQL
az network vnet subnet update \
  --resource-group $RESOURCE_GROUP \
  --vnet-name securebank-vnet \
  --name database-subnet \
  --service-endpoints Microsoft.Sql

# Configurar VNet rule para PostgreSQL
az postgres server vnet-rule create \
  --resource-group $RESOURCE_GROUP \
  --server $SERVER_NAME \
  --name securebank-vnet-rule \
  --subnet /subscriptions/[SUBSCRIPTION-ID]/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.Network/virtualNetworks/securebank-vnet/subnets/database-subnet
```

#### Configuración de Private Endpoint

```bash
# Crear Private Endpoint para mayor seguridad
az network private-endpoint create \
  --resource-group $RESOURCE_GROUP \
  --name securebank-postgresql-pe \
  --vnet-name securebank-vnet \
  --subnet database-subnet \
  --private-connection-resource-id /subscriptions/[SUBSCRIPTION-ID]/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.DBforPostgreSQL/servers/$SERVER_NAME \
  --group-id postgresqlServer \
  --connection-name securebank-postgresql-connection
```

### 3. Obtención de la Cadena de Conexión

#### Método 1: Azure Portal

1. Navegar a tu servidor PostgreSQL en Azure Portal
2. En el menú izquierdo, seleccionar "Connection strings"
3. Copiar la cadena de conexión para .NET:

```
Server=securebank-postgresql-prod.postgres.database.azure.com;Database=securebank_prod;Port=5432;User Id=securebank_admin@securebank-postgresql-prod;Password={your_password};Ssl Mode=Require;
```

#### Método 2: Azure CLI

```bash
# Obtener información del servidor
az postgres server show \
  --resource-group $RESOURCE_GROUP \
  --name $SERVER_NAME \
  --query "{FQDN:fullyQualifiedDomainName,Name:name}" \
  --output table

# La cadena de conexión será:
# Server={FQDN};Database=securebank_prod;Port=5432;User Id=securebank_admin@{ServerName};Password={Password};Ssl Mode=Require;Trust Server Certificate=true;
```

#### Ejemplo de Cadena de Conexión Completa

```
Server=securebank-postgresql-prod.postgres.database.azure.com;Database=securebank_prod;Port=5432;User Id=securebank_admin@securebank-postgresql-prod;Password=SuperSecurePassword123!;Ssl Mode=Require;Trust Server Certificate=true;Include Error Detail=true;
```

### 4. Configuración en Azure Key Vault

#### Crear Azure Key Vault

```bash
# Variables
KEY_VAULT_NAME="securebank-keyvault-prod"

# Crear Key Vault
az keyvault create \
  --resource-group $RESOURCE_GROUP \
  --name $KEY_VAULT_NAME \
  --location $LOCATION \
  --enabled-for-disk-encryption true \
  --enabled-for-deployment true \
  --enabled-for-template-deployment true \
  --sku premium

# Configurar políticas de acceso para la aplicación
az keyvault set-policy \
  --name $KEY_VAULT_NAME \
  --resource-group $RESOURCE_GROUP \
  --object-id [APP-OBJECT-ID] \
  --secret-permissions get list \
  --key-permissions get list decrypt encrypt \
  --certificate-permissions get list
```

#### Almacenar la Cadena de Conexión en Key Vault

```bash
# Almacenar la cadena de conexión completa
az keyvault secret set \
  --vault-name $KEY_VAULT_NAME \
  --name "ConnectionStrings--DefaultConnection" \
  --value "Server=securebank-postgresql-prod.postgres.database.azure.com;Database=securebank_prod;Port=5432;User Id=securebank_admin@securebank-postgresql-prod;Password=SuperSecurePassword123!;Ssl Mode=Require;Trust Server Certificate=true;Include Error Detail=true;"

# Almacenar componentes individuales para mayor flexibilidad
az keyvault secret set \
  --vault-name $KEY_VAULT_NAME \
  --name "Database--Server" \
  --value "securebank-postgresql-prod.postgres.database.azure.com"

az keyvault secret set \
  --vault-name $KEY_VAULT_NAME \
  --name "Database--Name" \
  --value "securebank_prod"

az keyvault secret set \
  --vault-name $KEY_VAULT_NAME \
  --name "Database--UserId" \
  --value "securebank_admin@securebank-postgresql-prod"

az keyvault secret set \
  --vault-name $KEY_VAULT_NAME \
  --name "Database--Password" \
  --value "SuperSecurePassword123!"

# Claves de encriptación para datos sensibles
az keyvault key create \
  --vault-name $KEY_VAULT_NAME \
  --name "data-encryption-key" \
  --protection software \
  --size 2048

az keyvault key create \
  --vault-name $KEY_VAULT_NAME \
  --name "pii-encryption-key" \
  --protection software \
  --size 2048
```

#### Configuración usando Managed Identity

```bash
# Crear Managed Identity para la aplicación
az identity create \
  --resource-group $RESOURCE_GROUP \
  --name securebank-app-identity

# Obtener el Object ID de la Managed Identity
MANAGED_IDENTITY_OBJECT_ID=$(az identity show \
  --resource-group $RESOURCE_GROUP \
  --name securebank-app-identity \
  --query principalId \
  --output tsv)

# Asignar permisos de Key Vault a la Managed Identity
az keyvault set-policy \
  --name $KEY_VAULT_NAME \
  --object-id $MANAGED_IDENTITY_OBJECT_ID \
  --secret-permissions get list \
  --key-permissions get list decrypt encrypt unwrapKey wrapKey
```

### 5. Configuración en el Código .NET

#### appsettings.json (para desarrollo local)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=securebank_dev;Port=5432;User Id=postgres;Password=devpassword;Ssl Mode=Disable;"
  },
  "KeyVault": {
    "VaultUrl": "https://securebank-keyvault-prod.vault.azure.net/"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

#### Program.cs - Configuración de Key Vault

```csharp
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configurar Key Vault
if (!builder.Environment.IsDevelopment())
{
    var keyVaultUrl = builder.Configuration["KeyVault:VaultUrl"];
    if (!string.IsNullOrEmpty(keyVaultUrl))
    {
        var secretClient = new SecretClient(
            new Uri(keyVaultUrl), 
            new DefaultAzureCredential());
        
        builder.Configuration.AddAzureKeyVault(
            new Uri(keyVaultUrl),
            new DefaultAzureCredential());
    }
}

// Configurar Entity Framework con PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorCodesToAdd: null);
        npgsqlOptions.CommandTimeout(30);
    });
    
    // Configuraciones adicionales para producción
    if (!builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging(false);
        options.EnableDetailedErrors(false);
    }
});

var app = builder.Build();

// Aplicar migraciones automáticamente en producción
if (!app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await context.Database.MigrateAsync();
}

app.Run();
```

#### Configuración de DbContext

```csharp
// ApplicationDbContext.cs
public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly IConfiguration _configuration;
    private readonly ICurrentUserService _currentUserService;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IConfiguration configuration,
        ICurrentUserService currentUserService) : base(options)
    {
        _configuration = configuration;
        _currentUserService = currentUserService;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            optionsBuilder.UseNpgsql(connectionString);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configurar esquema por defecto
        modelBuilder.HasDefaultSchema("securebank");
        
        // Configuraciones adicionales...
        base.OnModelCreating(modelBuilder);
    }
}
```

### 6. Configuración de Variables de Entorno para Producción

#### Azure App Service - Configuration

```bash
# Configurar variables de entorno en Azure App Service
az webapp config appsettings set \
  --resource-group $RESOURCE_GROUP \
  --name securebank-webapp-prod \
  --settings \
  "ASPNETCORE_ENVIRONMENT=Production" \
  "KeyVault__VaultUrl=https://securebank-keyvault-prod.vault.azure.net/" \
  "AZURE_CLIENT_ID=[MANAGED-IDENTITY-CLIENT-ID]"
```

#### Docker - docker-compose.yml para producción

```yaml
version: '3.8'
services:
  securebank-api:
    image: securebank/api:latest
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - KeyVault__VaultUrl=https://securebank-keyvault-prod.vault.azure.net/
      - AZURE_CLIENT_ID=${AZURE_CLIENT_ID}
    depends_on:
      - postgres
    networks:
      - securebank-network

  postgres:
    image: postgres:14
    environment:
      - POSTGRES_DB=securebank_prod
      - POSTGRES_USER=securebank_admin
      - POSTGRES_PASSWORD_FILE=/run/secrets/postgres_password
    secrets:
      - postgres_password
    volumes:
      - postgres_data:/var/lib/postgresql/data
    networks:
      - securebank-network

secrets:
  postgres_password:
    external: true

volumes:
  postgres_data:

networks:
  securebank-network:
    driver: bridge
```

### 7. Monitoreo y Backup

#### Configuración de Backup Automático

```bash
# Configurar backup automático
az postgres server configuration set \
  --resource-group $RESOURCE_GROUP \
  --server-name $SERVER_NAME \
  --name backup_retention_days \
  --value 35

# Configurar backup geo-redundante
az postgres server configuration set \
  --resource-group $RESOURCE_GROUP \
  --server-name $SERVER_NAME \
  --name geo_redundant_backup \
  --value on
```

#### Configuración de Alertas

```bash
# Crear alert rule para conexiones
az monitor metrics alert create \
  --name "PostgreSQL High Connections" \
  --resource-group $RESOURCE_GROUP \
  --scopes /subscriptions/[SUBSCRIPTION-ID]/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.DBforPostgreSQL/servers/$SERVER_NAME \
  --condition "count static average active_connections > 80" \
  --description "Alert when active connections exceed 80"
```

### 8. Mejores Prácticas de Seguridad

1. **Encriptación en Tránsito**: Siempre usar SSL/TLS
2. **Encriptación en Reposo**: Habilitar Azure Storage Service Encryption
3. **Acceso de Red**: Usar Private Endpoints cuando sea posible
4. **Autenticación**: Implementar Azure AD authentication
5. **Auditoría**: Habilitar audit logging
6. **Backup**: Configurar backups automáticos geo-redundantes
7. **Monitoreo**: Implementar alertas proactivas
8. **Rotación de Contraseñas**: Rotar credenciales regularmente

---

## Funcionalidades Principales

### 1. Autenticación y Seguridad
- Registro de usuarios con verificación multi-factor
- Login seguro con protección anti-fraude
- Gestión de dispositivos de confianza
- Encriptación end-to-end de datos sensibles

### 2. Gestión de Cuentas
- Apertura de cuentas digitales (ahorro, corriente, premium, empresarial)
- Consulta de saldos y movimientos en tiempo real
- Configuración de límites personalizados
- Estados de cuenta digitales

### 3. Transferencias y Pagos
- Transferencias entre cuentas propias
- Transferencias interbancarias (CCI)
- Pagos de servicios (luz, agua, teléfono, etc.)
- Programación de pagos recurrentes

### 4. Productos Financieros
- Solicitud de créditos personales con scoring automático
- Productos de inversión (depósitos a plazo, fondos mutuos)
- Simuladores financieros
- Recomendaciones personalizadas

### 5. Panel Administrativo
- Dashboard de métricas en tiempo real
- Gestión de usuarios y roles
- Monitoreo de transacciones
- Reportes de seguridad y auditoría

## Características de Seguridad

### Encriptación
- **AES-256-GCM** para datos sensibles
- **BCrypt** para contraseñas y PINs
- **SHA-256** para integridad de datos

### Autenticación
- **JWT** con rotación de refresh tokens
- **Multi-Factor Authentication (MFA)**
- **Device fingerprinting**

### Auditoría
- **Audit trail inmutable** con hash chaining
- **Logging estructurado** en Azure Monitor
- **Detección de fraude** en tiempo real

### Cumplimiento
- **Rate limiting** para prevenir ataques
- **Validación robusta** de inputs
- **Headers de seguridad** (CSP, HSTS, etc.)
- **Protección PII** con encriptación específica

## Integración con Azure Monitor para ML

El proyecto incluye una integración completa con Azure Monitor y Application Insights, diseñada específicamente para extraer datos relevantes para Machine Learning:

### Categorías de Datos Capturados
- **Eventos de Seguridad**: Intentos de login, accesos no autorizados
- **Eventos de Transacción**: Patrones de gasto, frecuencia de uso
- **Comportamiento de Usuario**: Navegación, preferencias, horarios de uso
- **Detección de Fraude**: Análisis de riesgo, alertas automáticas

### Modelos de ML Implementados
- **Isolation Forest**: Detección de anomalías
- **Gradient Boosting**: Scoring de crédito
- **LSTM**: Análisis de series temporales
- **K-Means**: Segmentación de usuarios
- **XGBoost**: Predicción de riesgo

Para más detalles, consultar la documentación en `docs/azure/azure-monitor-ml-integration.md`

## Configuración y Despliegue

### Prerrequisitos
- .NET 9 SDK
- PostgreSQL 14+
- Azure Subscription
- Docker (opcional)

### Variables de Entorno

```bash
# Desarrollo Local
export ASPNETCORE_ENVIRONMENT=Development
export ConnectionStrings__DefaultConnection="Server=localhost;Database=securebank_dev;Port=5432;User Id=postgres;Password=devpassword;"

# Producción (usando Key Vault)
export ASPNETCORE_ENVIRONMENT=Production
export KeyVault__VaultUrl="https://securebank-keyvault-prod.vault.azure.net/"
export AZURE_CLIENT_ID="[managed-identity-client-id]"
```

### Comandos de Instalación

```bash
# Clonar el repositorio
git clone https://github.com/securebank/SecureBank.Digital.git
cd SecureBank.Digital

# Restaurar dependencias
dotnet restore

# Ejecutar migraciones
dotnet ef database update --project src/Infrastructure/SecureBank.Infrastructure

# Compilar la solución
dotnet build --configuration Release

# Ejecutar tests
dotnet test

# Ejecutar la aplicación
dotnet run --project src/Services/SecureBank.AuthAPI
```

### Docker Compose

```bash
# Levantar el stack completo
docker-compose up -d

# Ver logs
docker-compose logs -f

# Parar el stack
docker-compose down
```

## Contribución

### Estructura de Commits
```
type(scope): description

- feat: nueva funcionalidad
- fix: corrección de bugs
- docs: documentación
- style: formato de código
- refactor: refactorización
- test: tests
- chore: tareas de mantenimiento
```

### Proceso de Desarrollo
1. Fork del repositorio
2. Crear branch feature/nombre-funcionalidad
3. Desarrollar con tests
4. Pull request con descripción detallada
5. Code review y aprobación
6. Merge a develop

## Licencia

Este proyecto es propiedad de SecureBank Digital S.A. Todos los derechos reservados.

## Contacto

- **Email**: developers@securebank.digital
- **Slack**: #securebank-dev
- **Wiki**: https://wiki.securebank.digital

---

**SecureBank Digital** - Construyendo el futuro de la banca digital en Latinoamérica 🏦🔒 