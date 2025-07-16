# Laboratorio Práctico - Sesión 5: Protección de Datos y Azure Key Vault - Parte 2

## 📋 Información General
- **Curso**: Diseño Seguro de Aplicaciones (.NET en Azure)
- **Duración Total**: 75 minutos (4 laboratorios)
- **Modalidad**: Instructor-led con práctica individual
- **Herramientas**: Visual Studio Code + .NET 9 + C# + Azure Key Vault

## 🎯 Objetivos del Laboratorio
- Implementar Data Protection API con Azure Storage para protección enterprise-grade
- Integrar gestión completa de secretos con Azure Key Vault usando Managed Identity
- Crear interfaces avanzadas para gestión de datos protegidos
- Realizar testing end-to-end de todas las funcionalidades

## 📁 Estructura de Laboratorios

### 🛠️ [Laboratorio 0: Verificación y Configuración del Entorno](./Laboratorio0-Setup/)
- **Duración**: 10 minutos
- **Objetivo**: Verificar configuraciones previas y preparar entorno para laboratorios avanzados
- **Entregables**: Entorno configurado con .NET 9 y paquetes Azure

### 🧪 [Laboratorio 1: Implementación de Data Protection API Avanzada](./Laboratorio1-DataProtection/)
- **Duración**: 25 minutos
- **Objetivo**: Implementar Data Protection API con Azure Storage y Key Vault para protección enterprise-grade
- **Entregables**: Aplicación web con Data Protection API funcionando

### 🔑 [Laboratorio 2: Integración Completa con Azure Key Vault](./Laboratorio2-KeyVault/)
- **Duración**: 30 minutos
- **Objetivo**: Implementar gestión completa de secretos con Azure Key Vault usando Managed Identity
- **Entregables**: Integración completa con Azure Key Vault

### 🖥️ [Laboratorio 3: Implementación de Vistas Avanzadas y Testing](./Laboratorio3-Testing/)
- **Duración**: 10 minutos
- **Objetivo**: Crear interfaces avanzadas para gestión de datos protegidos y testing end-to-end
- **Entregables**: Interface web completa y testing funcional

## 🚀 Inicio Rápido

1. **Prerrequisitos**:
   - .NET 9 SDK instalado
   - Visual Studio Code
   - Azure CLI instalado
   - Cuenta de Azure con permisos de desarrollo

2. **Secuencia de Ejecución**:
   ```bash
   # Clonar o descargar el repositorio
   cd Sesion05-Laboratorios
   
   # Seguir cada laboratorio en orden:
   cd Laboratorio0-Setup          # Configuración inicial
   cd ../Laboratorio1-DataProtection  # Data Protection API
   cd ../Laboratorio2-KeyVault    # Azure Key Vault
   cd ../Laboratorio3-Testing     # Testing y UI
   ```

3. **Verificación Final**:
   - Data Protection API funcionando
   - Azure Key Vault integrado
   - Interface web interactiva
   - Testing end-to-end exitoso

## 📚 Tecnologías Utilizadas

- **.NET 9**: Framework principal
- **ASP.NET Core**: Aplicación web
- **Azure Key Vault**: Gestión de secretos
- **Azure Storage**: Persistencia de claves
- **Data Protection API**: Encriptación de datos
- **Azure Identity**: Autenticación
- **Bootstrap**: Interface de usuario

## 🎓 Resultados de Aprendizaje

Al completar este laboratorio, los estudiantes habrán logrado:

1. **🔐 Implementación Enterprise de Data Protection**:
   - Configuración avanzada con Azure Storage
   - Múltiples protectores para diferentes tipos de datos
   - Rotación automática de claves
   - Logging y auditoría integrados

2. **🔑 Gestión Completa de Secretos**:
   - Azure Key Vault con RBAC
   - Configuration Provider seamless
   - Managed Identity para autenticación
   - Operaciones CRUD de secrets

3. **🛡️ Seguridad End-to-End**:
   - Separación de responsabilidades entre tipos de datos
   - Protección en reposo y en tránsito
   - Auditoría completa de operaciones
   - Manejo seguro de errores

4. **⚙️ Patterns de Desarrollo Seguro**:
   - Dependency injection apropiada
   - Configuración centralizada
   - Logging estructurado
   - Testing automatizado

## 🚨 Troubleshooting Común

| Error | Solución |
|-------|----------|
| "Could not load Azure.Identity" | `dotnet clean && dotnet restore && dotnet build` |
| "Access denied to Key Vault" | Verificar `az login` y permisos RBAC |
| "DataProtection keys not found" | Verificar connection string y container |
| "Cannot connect to Key Vault" | Verificar URL y autenticación Azure CLI |

## 📞 Soporte

Para problemas durante el laboratorio:
1. Revisar logs en consola de desarrollo
2. Verificar configuración de Azure resources
3. Consultar sección de troubleshooting en cada laboratorio
4. Contactar al instructor

---
⚡ **¡Importante!**: Seguir los laboratorios en el orden indicado ya que cada uno depende del anterior. 