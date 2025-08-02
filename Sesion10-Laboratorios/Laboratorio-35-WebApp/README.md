# 🚀 LABORATORIO 35: IMPLEMENTACIÓN DE LA BASE DE LA APLICACIÓN WEB .NET CORE

## 🎯 Objetivo
Construir los fundamentos seguros de la aplicación SecureShop implementando principios de **Secure-by-Design** desde la primera línea de código.

## ⏱️ Duración: 20 minutos

## 🎭 Conceptos Fundamentales del Desarrollo Seguro

### 🏗️ Filosofía de Construcción Segura
> *"No vamos a usar un template por defecto y esperar que sea seguro. Configuraremos meticulosamente cada aspecto del proyecto desde middleware de seguridad hasta validación de entrada, desde configuración HTTPS hasta protección contra ataques comunes."*

La configuración del proyecto .NET Core desde cero nos permite implementar seguridad desde los cimientos, no como una idea tardía. Es la diferencia entre construir una fortaleza desde los planos arquitectónicos versus intentar fortificar una casa ya construida.

### 🔐 Principios de Arquitectura Limpia Segura

#### **Separación de Responsabilidades con Seguridad**
Cada proyecto tiene implicaciones de seguridad específicas:

- **SecureShop.Web**: Proyecto web con configuraciones de seguridad habilitadas por defecto en .NET 9
- **SecureShop.Core**: Biblioteca de clases sin superficie de ataque web
- **SecureShop.Data**: Persistencia con auditoría y cifrado integrados
- **SecureShop.Security**: Servicios especializados de seguridad

Esta separación permite que cada proyecto tenga dependencias específicas sin crear referencias circulares que podrían introducir vulnerabilidades.

#### **Stack Tecnológico de Seguridad Empresarial**

**ASP.NET Core 9**: No es solo una actualización - incluye características de seguridad que no existían en versiones anteriores:
- Mejor gestión de secretos integrada
- Protección mejorada contra ataques de temporización  
- Validación automática más robusta
- Headers de seguridad optimizados

**Entity Framework Core**: Proporciona protección automática cuando se usa correctamente:
- Consultas parametrizadas automáticas (inmunes a SQL injection)
- Cifrado de conexión por defecto
- Capacidades de auditoría integradas
- Características como Always Encrypted para cifrado a nivel de columna

## 🛡️ Middleware de Seguridad Empresarial

### Headers de Seguridad - Primera Línea de Defensa
Los headers que configuraremos forman la primera línea de defensa, como los sistemas de seguridad perimetral de un edificio corporativo:

#### **X-Frame-Options: DENY**
- **Propósito**: Previene ataques de clickjacking
- **Caso Real**: Atacantes podrían crear un sitio que parece un juego simple, pero cada click realmente está haciendo transferencias bancarias en nuestra aplicación incrustada invisiblemente
- **Protección**: Impide que nuestra aplicación sea incrustada en iframes maliciosos

#### **Content-Security-Policy (CSP)**
- **Propósito**: Sistema inmunológico que rechaza código extraño
- **Funcionalidad**: Incluso si un atacante logra inyectar código malicioso, CSP previene que se ejecute
- **Analogía**: Como tener un sistema inmunológico que rechaza código que no reconoce

#### **X-Content-Type-Options: nosniff** 
- **Propósito**: Previene que navegadores "adivinen" tipos de contenido
- **Protección**: Sin esta protección, un atacante podría subir un archivo .txt que contiene JavaScript malicioso, y algunos navegadores lo ejecutarían como script

### Configuración HTTPS Forzada
> *"No hay tráfico no encriptado, nunca. Incluso en desarrollo local, forzamos HTTPS para construir buenos hábitos desde el principio."*

## 🗄️ Entity Framework Seguro - Más Allá de CRUD

### Contexto de Base de Datos con Seguridad Integrada
El contexto que implementaremos va mucho más allá de simples operaciones CRUD:

#### **Auditoría Automática**
```csharp
public override int SaveChanges()
{
    // Registro automático de cada INSERT, UPDATE, DELETE
    var auditEntries = ChangeTracker.Entries()
        .Where(e => e.State == EntityState.Added || 
                   e.State == EntityState.Modified || 
                   e.State == EntityState.Deleted)
        .Select(e => new AuditLog { ... });
    
    AuditLogs.AddRange(auditEntries);
    return base.SaveChanges();
}
```

#### **Soft Delete con Preservación de Auditoría**
- **Concepto**: Nunca eliminamos realmente datos - solo los marcamos como eliminados
- **Importancia**: Crítico para auditoría, cumplimiento regulatorio, e investigaciones de seguridad
- **Implementación**: Automática en el contexto de base de datos

#### **Cifrado Transparente de Campos**
```csharp
// El costo se cifra automáticamente usando Key Vault
[EncryptedProperty]
public decimal CostPrice { get; set; }
```

## 🔍 Framework de Validación de Entrada Robusto

### Validación en Múltiples Capas
> *"La validación de entrada robusta es nuestra primera línea de defensa contra ataques maliciosos. No es solo verificar que los campos requeridos estén presentes - es asegurar que cada byte de datos que entra cumple expectativas específicas."*

#### **Capa 1 - Cliente (JavaScript)**: Validación inmediata para UX, pero no para seguridad
#### **Capa 2 - Modelo (Data Annotations)**: Validaciones declarativas con múltiples reglas  
#### **Capa 3 - Servicio de Negocio**: Validación específica de reglas de negocio
#### **Capa 4 - Base de Datos**: Protege contra ataques que bypasean la aplicación web

### Ejemplo de Validación Múltiple
```csharp
[Required(ErrorMessage = "Name is required")]
[StringLength(100, MinimumLength = 3)]  // Control de longitud bidireccional
[RegularExpression(@"^[a-zA-Z0-9\s\-\.]+$")]  // Whitelist approach
public string Name { get; set; }
```

**¿Por qué múltiples validaciones?** Porque los atacantes buscan la validación más débil y la explotan. Es como tener múltiples cerraduras en una puerta - todas deben funcionar para que la seguridad sea efectiva.

## 🎯 Pasos de Implementación

### Paso 1: Crear Solución y Proyectos (8 minutos)

**Concepto**: Arquitectura limpia con separación de responsabilidades de seguridad

```powershell
# Navegar al directorio src
cd src

# Crear la solución con naming conventions empresariales
dotnet new sln -n SecureShop

# Crear proyectos con propósitos específicos de seguridad
dotnet new web -n SecureShop.Web           # Frontend con seguridad integrada
dotnet new classlib -n SecureShop.Core     # Lógica de negocio sin superficie de ataque
dotnet new classlib -n SecureShop.Data     # Persistencia con auditoría
dotnet new classlib -n SecureShop.Security # Servicios especializados de seguridad

# Agregar proyectos a la solución para gestión centralizada
dotnet sln add SecureShop.Web/SecureShop.Web.csproj
dotnet sln add SecureShop.Core/SecureShop.Core.csproj  
dotnet sln add SecureShop.Data/SecureShop.Data.csproj
dotnet sln add SecureShop.Security/SecureShop.Security.csproj
```

### Paso 2: Configurar Middleware de Seguridad (7 minutos)

**Concepto**: Pipeline de seguridad con orden específico para máxima protección

El Program.cs que implementaremos incluye:
- Headers de seguridad automáticos
- Políticas de autorización granulares  
- CORS configurado de forma segura
- HTTPS forzado en todos los ambientes
- Middleware de manejo de errores que no expone información sensible

### Paso 3: Implementar Modelos de Datos Seguros (5 minutos)

**Concepto**: Modelos con validación robusta y características de auditoría integradas

Los modelos incluirán:
- Validación declarativa con múltiples reglas
- Campos de auditoría automáticos
- Cifrado transparente para datos sensibles
- Relaciones que preservan integridad referencial

## 📋 Entregables del Laboratorio

Al completar este laboratorio tendrás:

- [ ] Solución multi-proyecto con arquitectura limpia
- [ ] Middleware de seguridad implementado y configurado
- [ ] Entity Framework con auditoría y cifrado
- [ ] Modelos de datos con validación robusta  
- [ ] Headers de seguridad aplicados automáticamente
- [ ] Configuración HTTPS forzada
- [ ] Base sólida para características avanzadas de seguridad

## 🔍 Casos de Estudio Preventivos

### **British Airways (2019)**
- **Problema**: Sistema de pagos comprometido por inyección de script con solo validación cliente
- **Nuestro Sistema**: Validación en múltiples capas habría bloqueado el ataque en tres puntos diferentes

### **Target (2013)**  
- **Problema**: Implementaron cifrado después de su brecha, pero lo hicieron mal - cifraron todo con la misma clave almacenada en el mismo servidor
- **Nuestro Enfoque**: Usamos diferentes claves para diferentes tipos de datos, almacenadas en Key Vault separado

## 💡 Valor Profesional Generado

**Portfolio Evidence**: Base de aplicación empresarial con seguridad desde el diseño  
**Skills Advancement**: Competencias en desarrollo seguro .NET Core 9  
**Security Mindset**: Implementación práctica de Secure-by-Design  
**Enterprise Architecture**: Patrones usados en organizaciones Fortune 500

## 🔗 Preparación para Laboratorios Siguientes

Esta base será fundamental para:
- **Lab 36**: Integración Azure AD con estructura preparada
- **Lab 37**: Key Vault con servicios de cifrado implementados
- **Testing**: Validación de controles de seguridad implementados

---

> **💡 Mindset Clave**: Cada línea de código que escribimos, cada configuración que establecemos, y cada decisión de diseño que tomamos será evaluada contra nuestro modelo de seguridad. No es perfección teórica - es seguridad práctica que funciona en el mundo real.