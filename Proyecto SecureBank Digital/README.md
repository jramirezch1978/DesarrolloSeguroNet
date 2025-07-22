# 🏦 SecureBank Digital - Sistema Bancario Seguro

<div align="center">

![SecureBank Digital](https://img.shields.io/badge/SecureBank-Digital-blue?style=for-the-badge)
![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-316192?style=for-the-badge&logo=postgresql&logoColor=white)
![Azure](https://img.shields.io/badge/Azure-0078D4?style=for-the-badge&logo=microsoft-azure&logoColor=white)

*"Security First, Innovation Always"*

</div>

## 🏛️ Historia de SecureBank Digital

### Fundación y Orígenes (2018)
SecureBank Digital fue fundado en Lima, Perú, en 2018 por un grupo de ex-ejecutivos de tecnología financiera y expertos en ciberseguridad, liderados por la visionaria **María Elena Vásquez** (ex-CTO de una fintech internacional) y el especialista en seguridad **Carlos Mendoza** (ex-CISO de una institución bancaria tradicional).

### La Visión Inicial
Los fundadores identificaron una brecha crítica en el mercado financiero peruano: la necesidad de un banco 100% digital que pusiera la seguridad cibernética en el centro de su propuesta de valor, no como una característica adicional, sino como su principal diferenciador competitivo.

### 🚀 Hitos Importantes

- **2018**: Constitución de la empresa y obtención de licencia bancaria digital
- **2019**: Lanzamiento del primer producto: cuentas de ahorro con autenticación biométrica
- **2020**: Durante la pandemia, experimentó un crecimiento del 300% al ser el único banco que garantizó cero brechas de seguridad
- **2021**: Expansión a servicios empresariales con foco en PyMES del sector tecnológico
- **2022**: Implementación pionera de tecnología blockchain para validación de transacciones
- **2023**: Reconocimiento como "Banco Más Seguro de Latinoamérica" por CyberSecurity Excellence Awards
- **2024**: Alianza estratégica con Microsoft Azure para infraestructura cloud híbrida
- **2025**: Lanzamiento de la plataforma de Open Banking más segura de la región

### 🎯 Misión
*"Democratizar el acceso a servicios financieros digitales de clase mundial, garantizando los más altos estándares de seguridad cibernética y protección de datos, mientras promovemos la inclusión financiera y el crecimiento económico sostenible de nuestros clientes en Perú y la región."*

### 💎 Valores Corporativos

#### 1. 🛡️ SEGURIDAD PRIMERO
*"La seguridad no es negociable"*
- Implementamos controles de seguridad desde el diseño
- Invertimos continuamente en tecnologías de protección avanzada
- Mantenemos certificaciones internacionales de seguridad
- Transparencia total en nuestras prácticas de ciberseguridad

#### 2. 🚀 INNOVACIÓN RESPONSABLE
*"Innovamos sin comprometer la seguridad"*
- Adoptamos nuevas tecnologías solo después de rigurosas evaluaciones de seguridad
- Desarrollamos soluciones propias cuando el mercado no ofrece alternativas seguras
- Contribuimos al ecosistema open source de seguridad financiera
- Investigación y desarrollo constante en fintech segura

#### 3. 🔍 TRANSPARENCIA RADICAL
*"Nuestros clientes conocen exactamente cómo protegemos su información"*
- Comunicación clara sobre políticas de seguridad y privacidad
- Reportes regulares de auditorías de seguridad
- Educación continua a clientes sobre mejores prácticas de seguridad
- Admisión proactiva de incidentes y medidas correctivas

#### 4. ⭐ EXCELENCIA OPERACIONAL
*"Cada proceso está diseñado para la perfección y la seguridad"*
- Automatización inteligente de procesos críticos
- Monitoreo 24/7 de todos los sistemas
- Mejora continua basada en métricas de seguridad y satisfacción
- Personal altamente calificado y certificado en seguridad

#### 5. 🌍 INCLUSIÓN DIGITAL
*"Tecnología segura para todos"*
- Interfaces accesibles para personas con discapacidades
- Servicios financieros para poblaciones rurales y de bajos ingresos
- Educación financiera digital gratuita
- Tecnología que se adapta a diferentes niveles de alfabetización digital

#### 6. 🌱 SOSTENIBILIDAD
*"Crecimiento responsable con el planeta y la sociedad"*
- Infraestructura cloud eficiente energéticamente
- Digitalización de procesos para reducir huella de carbono
- Inversión en proyectos de impacto social positivo
- Gobierno corporativo ético y responsable

## 🏗️ Arquitectura del Sistema

### Stack Tecnológico
- **Framework**: ASP.NET Core 9.0 con Clean Architecture
- **Lenguaje**: C# 12 con nullable reference types
- **Base de Datos**: PostgreSQL con Entity Framework Core 9
- **Autenticación**: JWT + OAuth 2.0 + OpenID Connect
- **Gestión de Secretos**: Azure Key Vault
- **Logging**: Serilog con structured logging
- **Validación**: FluentValidation
- **Mapping**: AutoMapper
- **Mediator**: MediatR para CQRS
- **Testing**: xUnit con security tests

### Estructura de Proyectos

```
SecureBankDigital.sln
├── 📁 Core/Domain
│   ├── SecureBank.Domain              # Entidades, Value Objects, Enums
│   └── SecureBank.Application         # Commands, Queries, DTOs, Interfaces
│
├── 📁 Infrastructure  
│   ├── SecureBank.Infrastructure      # Data Access, External Services
│   └── SecureBank.Security           # JWT, Encryption, KeyVault
│
├── 📁 API Services
│   ├── SecureBank.AuthAPI            # Autenticación/Autorización
│   ├── SecureBank.AccountAPI         # Cuentas y Saldos
│   ├── SecureBank.TransactionAPI     # Transferencias y Pagos
│   └── SecureBank.ProductAPI         # Productos Financieros
│
├── 📁 Web Applications
│   ├── SecureBank.WebApp             # Cliente Web (MVC)
│   └── SecureBank.AdminPortal        # Panel Administrativo
│
└── 📁 Cross-Cutting
    ├── SecureBank.Shared             # DTOs Compartidos, Utilities
    └── SecureBank.Tests              # Pruebas Integradas
```

## 🔐 Funcionalidades Principales

### 1. 🔑 Sistema de Autenticación y Autorización Avanzada

#### Proceso de Registro Seguro:
1. **Validación de identidad multi-paso:**
   - Formulario con validación en tiempo real (email, teléfono, documento)
   - Verificación de email con token temporal encriptado
   - Verificación de SMS con código de 6 dígitos
   - Validación de documento de identidad (simulada con OCR mock)
   - Selfie con documento para verificación biométrica (simulada)

2. **Configuración de seguridad inicial:**
   - Creación de PIN de 6 dígitos con políticas robustas
   - Configuración obligatoria de pregunta de seguridad
   - Setup de autenticación de dos factores (TOTP o SMS)
   - Registro de dispositivo confiable con fingerprinting
   - Aceptación de términos y condiciones con timestamp y IP

#### Proceso de Login Multi-Factor:
1. **Primera fase - Credenciales básicas:**
   - Email/documento + PIN con hash BCrypt
   - Validación de dispositivo (nuevo vs confiable)
   - Rate limiting: 3 intentos por minuto, bloqueo temporal tras 5 fallos
   - CAPTCHA después de 2 intentos fallidos

2. **Segunda fase - Autenticación adicional:**
   - Si dispositivo nuevo: SMS + pregunta de seguridad
   - Si dispositivo confiable: Solo TOTP o SMS
   - Para operaciones críticas: Biometric simulation (huella/facial)
   - Validación geográfica (bloquear si país diferente sin autorización)

#### Roles y Permisos:
- **👤 Cliente Regular**: Consultas, transferencias hasta $1,000, pagos básicos
- **⭐ Cliente Premium**: Límites elevados, productos de inversión, asesoría
- **🏢 Cliente Empresarial**: Múltiples usuarios, aprobaciones jerárquicas, reportes
- **🎧 Operador de Soporte**: Solo lectura de cuentas, no acceso a transacciones
- **👨‍💼 Gerente**: Aprobación de transacciones sospechosas, gestión de límites
- **🔍 Auditor de Seguridad**: Acceso completo a logs, métricas de seguridad
- **⚙️ Administrador**: Gestión completa del sistema

### 2. 💰 Gestión Integral de Cuentas Bancarias

#### Tipos de Cuenta:

##### 🏦 Cuenta de Ahorros
- **Tasa de interés**: 2.5% anual, cálculo diario
- **Límite transferencias**: $2,000 diarios, $20,000 mensuales
- **Sin comisión** de mantenimiento
- **Retiros gratuitos**: 4 por mes, después $2 por retiro

##### 💼 Cuenta Corriente
- **Sin intereses**, sobregiro hasta $500
- **Límite transferencias**: $5,000 diarios, $50,000 mensuales
- **Comisión mantenimiento**: $8 mensuales
- **Chequera digital** incluida

##### 💎 Cuenta Premium
- **Tasa preferencial**: 3.2% anual
- **Límites elevados**: $10,000 diarios, $100,000 mensuales
- **Sin comisiones** por servicios
- **Acceso a productos** de inversión exclusivos

##### 🏢 Cuenta Empresarial
- **Multi-usuario** con niveles de aprobación
- **Límites configurables** por empresa
- **Reportería avanzada** y API access
- **Integración** con sistemas contables

### 3. 💸 Sistema de Transferencias con Validación Multi-Capa

#### Tipos de Transferencia:

##### 🔄 Transferencias Internas
- **Validación**: Solo PIN + device verification
- **Tiempo**: Inmediato
- **Comisión**: Gratuito
- **Límite**: Según tipo de cuenta

##### 🏦 Transferencias Interbancarias
- **Validación**: PIN + MFA obligatorio para montos > $500
- **Tiempo**: 1-24 horas según destino
- **Comisión**: $3 para cuentas regulares, gratuito premium
- **Verificación**: Cuenta destino con nombre del titular

##### ⏰ Transferencias Programadas/Recurrentes
- **Configuración**: Fechas y montos fijos
- **Pre-autorización**: MFA para toda la serie
- **Cancelación**: Permitida hasta 1 hora antes
- **Notificaciones**: 24h antes de ejecución

##### ⚡ Transferencias Express
- **Comisión adicional**: $5
- **Disponibilidad**: 24/7 incluso fines de semana
- **Límite máximo**: $5,000 por operación
- **Requiere**: Biometric authentication

#### Proceso de Validación:
1. **Validación inicial**: Saldo, límites, estado de cuenta, formato
2. **Scoring de riesgo**: Patrón, lista negra, geolocalización, historial
3. **Validaciones por monto**: 
   - $0-500: Solo PIN
   - $501-2000: PIN + SMS
   - $2001-5000: PIN + TOTP + pregunta seguridad
   - >$5000: PIN + biometric + aprobación telefónica
4. **Post-validación**: Bloqueo temporal, audit log, ejecución atómica, notificación

### 4. 🧾 Sistema de Pagos y Servicios Integrado

#### Categorías de Pagos:

##### ⚡ Servicios Básicos
- Luz, agua, gas, teléfono, internet, cable
- Validación automática de códigos de cliente
- Programación de pagos recurrentes
- Descuentos por pago anticipado

##### 📊 Impuestos y Tributos
- SUNAT: renta, IGV, tributos municipales
- Validación de RUC/DNI automática
- Generación de constancias de pago
- Integración con cronograma de vencimientos

##### 🛡️ Seguros y Pensiones
- Seguros vehiculares, vida, hogar
- AFP y sistemas previsionales
- Validación de pólizas activas
- Cálculo automático de primas

##### 🎓 Educación y Entretenimiento
- Colegios, universidades, institutos
- Plataformas streaming, gaming, apps
- Validación de códigos estudiantiles
- Descuentos por volumen familiar

### 5. 📈 Productos Financieros con Simuladores

#### Módulo de Créditos y Préstamos:

##### 📊 Evaluación Crediticia Automática
- Análisis de historial transaccional (6 meses mínimo)
- Scoring basado en ingresos promedio y regularidad
- Verificación en centrales de riesgo (simulada)
- Cálculo de capacidad de pago (30% ingresos máximo)

##### 💰 Tipos de Crédito
- **Personal**: hasta $15,000, 12-60 meses, 15-25% TEA
- **Vehicular**: hasta $80,000, 12-84 meses, 12-18% TEA
- **Hipotecario**: hasta $200,000, 120-360 meses, 8-12% TEA
- **Línea de crédito**: revolving, hasta $5,000, 18-30% TEA

#### Módulo de Inversiones:

##### 🏦 Depósitos a Plazo Fijo
- **Plazos**: 30, 60, 90, 180, 360 días
- **Tasas**: Escalonadas según monto y plazo
- **Simulador**: Rentabilidad con proyecciones
- **Renovación**: Automática opcional

##### 📊 Fondos Mutuos
- **Conservador** (bonos): 5-8% anual
- **Moderado** (mixto): 8-12% anual
- **Agresivo** (acciones): 10-18% anual con volatilidad
- **Simulación**: Con datos históricos de mercado

##### 📈 Trading Básico
- **Compra/venta**: Acciones principales de BVL
- **Órdenes**: Limitadas y de mercado
- **Portfolio tracking**: P&L en tiempo real
- **Alertas**: Precio y noticias relevantes

### 6. 🔔 Centro de Notificaciones y Alertas Inteligentes

#### Sistema Multi-Canal:

##### 📱 Push Notifications In-App
- Transacciones completadas/fallidas
- Alertas de seguridad en tiempo real
- Recordatorios de pagos próximos a vencer
- Ofertas personalizadas de productos

##### 📧 Email Notifications
- Estados de cuenta mensuales
- Resúmenes de actividad semanal
- Alertas de cambios en configuración
- Confirmaciones de operaciones importantes

##### 📱 SMS Alerts
- Transacciones mayores a $200
- Intentos de login desde dispositivos nuevos
- Códigos de verificación para MFA
- Alertas de saldo bajo (configurable)

#### Configuración Personalizable:
- **Umbrales personalizables** para cada tipo de alerta
- **Horarios de silencio** para notificaciones no críticas
- **Canales preferenciales** por tipo de operación
- **Frecuencia de resúmenes** (diario, semanal, mensual)

### 7. 🎛️ Panel Administrativo con Analytics Avanzado

#### Dashboard Operacional:

##### 📊 Métricas en Tiempo Real
- Transacciones por segundo/minuto/hora
- Valor total procesado en el día
- Usuarios activos concurrentes
- Tiempo de respuesta promedio de APIs

##### 🛡️ Indicadores de Seguridad
- Intentos de login fallidos por hora
- Transacciones bloqueadas por fraude
- Alertas de seguridad activas
- Eventos sospechosos detectados

##### 👥 Gestión de Usuarios
- Lista completa con filtros avanzados
- Histórico de actividad por usuario
- Bloqueo/desbloqueo de cuentas
- Reset de credenciales de emergencia

#### Sistema de Detección de Fraude:

##### 🤖 Reglas Automáticas
- Múltiples transacciones en corto tiempo
- Transacciones desde ubicaciones geográficas inusuales
- Montos atípicos basados en historial del usuario
- Patrones de comportamiento anómalos

##### 🧠 Machine Learning Simulado
- Scoring de riesgo basado en variables múltiples
- Detección de anomalías en tiempo real
- Aprendizaje de patrones normales por usuario
- Ajuste automático de umbrales de detección

##### 🔍 Workflow de Investigación
- Cola de casos sospechosos para revisión manual
- Herramientas de investigación con timeline
- Capacidad de bloquear/permitir transacciones
- Documentación de decisiones para auditoría

## 🛡️ Características de Seguridad

### Encriptación y Protección de Datos
- **Datos en reposo**: AES-256 para todos los campos sensibles
- **Datos en tránsito**: HTTPS obligatorio con TLS 1.3
- **PII protection**: Hash irreversible para documentos de identidad
- **Key rotation**: Automática cada 90 días

### Audit Trail Inmutable
- **Logging completo**: Toda acción del usuario genera log estructurado
- **Retention policy**: Logs de seguridad por 7 años, transaccionales por 10 años
- **Tamper detection**: Hash chains para detectar modificación de logs
- **Real-time alerting**: Alertas automáticas para eventos críticos

### Validación y Sanitización Robusta
- **Input validation**: FluentValidation para todos los endpoints
- **Output encoding**: Prevención de XSS en todas las respuestas HTML
- **SQL injection prevention**: Uso exclusivo de parameterized queries
- **Rate limiting**: Throttling por usuario y por endpoint

### Session Management Seguro
- **JWT con refresh tokens**: Rotación automática cada 15 minutos
- **Session timeout**: 30 minutos de inactividad, logout automático
- **Concurrent sessions**: Máximo 2 sesiones activas por usuario
- **Device binding**: Tokens vinculados a device fingerprint

## 🚀 Instalación y Configuración

### Prerrequisitos
- .NET 9.0 SDK
- PostgreSQL 14+
- Azure CLI (para Key Vault)
- Visual Studio 2022 o VS Code

### Configuración de Base de Datos
```bash
# Cadena de conexión (almacenada en Key Vault)
Server=localhost;Database=SecureBankDigital;User Id=postgres;Password=123456;
```

### Configuración de Key Vault
Los siguientes secretos deben estar configurados en Azure Key Vault:
- `ConnectionString-Database`
- `JWT-SecretKey`
- `JWT-Issuer`
- `JWT-Audience`
- `Encryption-MasterKey`
- `SMS-ApiKey`
- `Email-ApiKey`

### Comandos de Inicio
```bash
# Clonar el repositorio
git clone [repository-url]
cd SecureBankDigital

# Restaurar paquetes
dotnet restore

# Ejecutar migraciones
dotnet ef database update --project src/Infrastructure/SecureBank.Infrastructure

# Ejecutar aplicación web
dotnet run --project src/Web/SecureBank.WebApp

# Ejecutar APIs (en terminales separadas)
dotnet run --project src/Services/SecureBank.AuthAPI
dotnet run --project src/Services/SecureBank.AccountAPI
dotnet run --project src/Services/SecureBank.TransactionAPI
dotnet run --project src/Services/SecureBank.ProductAPI
```

## 🧪 Testing

### Ejecutar Pruebas
```bash
# Todas las pruebas
dotnet test

# Pruebas de seguridad específicas
dotnet test --filter Category=Security

# Pruebas de integración
dotnet test --filter Category=Integration

# Cobertura de código
dotnet test --collect:"XPlat Code Coverage"
```

### Cobertura Requerida
- **Lógica de negocio**: Mínimo 80%
- **Funciones de seguridad**: Mínimo 95%
- **APIs críticas**: 100%

## 📊 Monitoreo y Métricas

### Dashboards Disponibles
- **Operacional**: Transacciones, rendimiento, usuarios activos
- **Seguridad**: Intentos de fraude, alertas, eventos sospechosos
- **Negocio**: Productos más usados, ingresos, conversiones

### Alertas Configuradas
- **Críticas**: Brechas de seguridad, fallos del sistema
- **Advertencias**: Patrones inusuales, límites alcanzados
- **Informativas**: Métricas de rendimiento, estadísticas de uso

## 🤝 Contribución

### Estándares de Código
- **C# Guidelines**: Seguir convenciones de Microsoft
- **Security First**: Toda funcionalidad debe pasar revisión de seguridad
- **Testing**: Cobertura mínima del 80%
- **Documentation**: Comentarios XML en APIs públicas

### Proceso de Pull Request
1. **Fork** del repositorio
2. **Crear branch** con nombre descriptivo
3. **Implementar** funcionalidad con tests
4. **Ejecutar** suite completa de pruebas
5. **Crear PR** con descripción detallada
6. **Revisión** de seguridad obligatoria
7. **Merge** después de aprobación

## 📋 Casos de Uso Implementados

### ✅ Casos Críticos Completados

#### 1. Onboarding Completo de Cliente
- ✅ Registro inicial con validación de identidad en múltiples pasos
- ✅ Configuración de seguridad (MFA, preguntas, dispositivos)
- ✅ Apertura automática de cuenta según perfil
- ✅ Tutorial interactivo de seguridad
- ✅ Primera transacción guiada con validaciones

#### 2. Transferencia de Alto Riesgo
- ✅ Detección de patrones inusuales
- ✅ Escalación de nivel de verificación
- ✅ Proceso de aprobación manual
- ✅ Monitoreo post-transacción

#### 3. Detección y Respuesta a Fraude
- ✅ Bloqueo automático de cuentas
- ✅ Notificación multi-canal
- ✅ Proceso de verificación para desbloqueo
- ✅ Investigación forense

#### 4. Renovación de Producto Financiero
- ✅ Alertas automatizadas
- ✅ Simulador de condiciones
- ✅ Proceso de renovación digital
- ✅ Confirmación y actualización

## 📞 Soporte

### Contacto
- **Email**: support@securebankdigital.pe
- **Teléfono**: +51 1 234-5678
- **Chat**: Disponible 24/7 en la aplicación

### Documentación Técnica
- **API Reference**: `/docs/api`
- **Architecture Guide**: `/docs/architecture`
- **Security Whitepaper**: `/docs/security`

---

<div align="center">

**SecureBank Digital** - *Innovando con Seguridad desde 2018*

[![Estado del Build](https://img.shields.io/badge/build-passing-brightgreen)]()
[![Cobertura de Código](https://img.shields.io/badge/coverage-92%25-brightgreen)]()
[![Última Auditoría de Seguridad](https://img.shields.io/badge/security%20audit-passed-brightgreen)]()

*© 2025 SecureBank Digital. Todos los derechos reservados.*

</div> 