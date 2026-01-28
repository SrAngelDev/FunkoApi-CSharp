# FunkoApi 🧸

**API REST desarrollada con .NET 10, ASP.NET Core y C# 14 para la gestión de Funkos Pop.**

## 📋 Descripción

FunkoApi es una API REST completa que permite gestionar una colección de Funkos Pop con sus categorías. Incluye autenticación JWT, autorización por roles, caché con Redis, GraphQL, SignalR para notificaciones en tiempo real, y un sistema completo de testing.

## ✨ Características

- 🔐 **Autenticación y Autorización**: Sistema completo con JWT y ASP.NET Identity
- 👥 **Gestión de Roles**: Control de acceso basado en roles (User/Admin)
- 🎨 **CRUD Completo**: Gestión de Funkos y Categorías
- 📸 **Subida de Imágenes**: Sistema de almacenamiento local de imágenes
- 🚀 **Caché Redis**: Optimización de consultas con caché distribuido
- 📊 **GraphQL**: Consultas flexibles con HotChocolate
- 🔔 **SignalR**: Notificaciones en tiempo real
- 📧 **Envío de Emails**: Notificaciones por correo electrónico
- ✅ **Validaciones**: FluentValidation para validación de datos
- 🗃️ **Base de Datos**: PostgreSQL con Entity Framework Core
- 🧪 **Testing**: Pruebas unitarias y de integración
- 📝 **Documentación**: Swagger/OpenAPI
- 🐳 **Docker**: Contenedores para PostgreSQL y Redis

## 🛠️ Tecnologías

- **.NET 10.0**
- **ASP.NET Core**
- **C# 14**
- **Entity Framework Core**
- **PostgreSQL**
- **Redis**
- **JWT Authentication**
- **ASP.NET Identity**
- **HotChocolate (GraphQL)**
- **SignalR**
- **FluentValidation**
- **Swagger/OpenAPI**
- **MailKit**
- **xUnit / NUnit** (Testing)

## 📦 Paquetes NuGet Principales

```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.2" />
<PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="10.0.2" />
<PackageReference Include="Microsoft.AspNetCore.SignalR.Common" Version="10.0.2" />
<PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="10.0.2" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.0" />
<PackageReference Include="HotChocolate.AspNetCore" Version="13.9.0" />
<PackageReference Include="FluentValidation" Version="12.1.1" />
<PackageReference Include="MailKit" Version="4.14.1" />
<PackageReference Include="Swashbuckle.AspNetCore" Version="10.1.0" />
```

## 🚀 Instalación y Configuración

### Requisitos Previos

- .NET 10.0 SDK
- Docker y Docker Compose
- PostgreSQL 15 (o usar Docker)
- Redis (o usar Docker)

### 1. Clonar el Repositorio

```bash
git clone https://github.com/SrAngelDev/FunkoApi-CSharp.git
cd FunkoApi
```

### 2. Levantar Servicios con Docker

```bash
docker-compose up -d
```

Esto iniciará:
- **PostgreSQL** en el puerto `5455`
- **Redis** en el puerto `6379`

### 3. Configurar appsettings.json

Ajusta la configuración en `FunkoApi/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "FunkoDb": "Host=localhost;Port=5455;Database=FunkoDb;Username=postgres;Password=postgres",
    "Redis": "localhost:6379"
  },
  "Jwt": {
    "Key": "TuClaveSecretaSuperSegura",
    "ExpireInMinutes": 120
  },
  "EmailSettings": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUser": "tu-email@gmail.com",
    "SmtpPass": "tu-contraseña-de-aplicación"
  }
}
```

### 4. Aplicar Migraciones

```bash
cd FunkoApi
dotnet ef database update
```

### 5. Ejecutar la API

```bash
dotnet run
```

La API estará disponible en:
- **HTTP**: `http://localhost:5000/api/funkos`
- **GraphQL**: `https://localhost:5000/graphql`
- **WebSocket SignalR**: `https://localhost:5000/ws/funkos`

## 📚 Endpoints Principales

### 🔐 Autenticación (`/api/auth`)

| Método | Endpoint | Descripción | Auth |
|--------|----------|-------------|------|
| POST | `/api/auth/register` | Registrar nuevo usuario | ❌ |
| POST | `/api/auth/login` | Iniciar sesión | ❌ |

### 🎨 Funkos (`/api/funkos`)

| Método | Endpoint | Descripción | Auth |
|--------|----------|-------------|------|
| GET | `/api/funkos` | Listar todos los funkos | ❌ |
| GET | `/api/funkos/{id}` | Obtener funko por ID | ❌ |
| POST | `/api/funkos` | Crear nuevo funko | ✅ Admin |
| PUT | `/api/funkos/{id}` | Actualizar funko | ✅ Admin |
| DELETE | `/api/funkos/{id}` | Eliminar funko | ✅ Admin |
| PATCH | `/api/funkos/{id}/image` | Actualizar imagen | ✅ Admin |

### 📂 Categorías (`/api/categorias`)

| Método | Endpoint | Descripción | Auth |
|--------|----------|-------------|------|
| GET | `/api/categorias` | Listar todas las categorías | ❌ |
| GET | `/api/categorias/{id}` | Obtener categoría por ID | ❌ |
| POST | `/api/categorias` | Crear nueva categoría | ✅ Admin |
| PUT | `/api/categorias/{id}` | Actualizar categoría | ✅ Admin |
| DELETE | `/api/categorias/{id}` | Eliminar categoría | ✅ Admin |

Ver documentación completa de endpoints en [API_ENDPOINTS.md](API_ENDPOINTS.md)

## 🔔 SignalR

La API incluye un Hub de SignalR para notificaciones en tiempo real:

- **Endpoint**: `/ws/funkos`
- **Eventos**: Notificaciones de creación, actualización y eliminación de funkos

### Cliente HTML de Ejemplo

Incluye un cliente HTML en `Cliente-SignalR/cliente.html` para probar las notificaciones en tiempo real.

## 📊 GraphQL

Consultas GraphQL disponibles en `/graphql`:

```graphql
query {
  funkos {
    id
    nombre
    precio
    stock
    imagen
    categoria {
      nombre
    }
  }
  
  categorias {
    id
    nombre
    funkos {
      nombre
    }
  }
}
```

## 🧪 Testing

El proyecto incluye pruebas unitarias y de integración:

```bash
# Ejecutar todos los tests
dotnet test

# Ejecutar con cobertura
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Generar reporte HTML de cobertura
reportgenerator -reports:coverage.opencover.xml -targetdir:coverage -reporttypes:Html
```
Ademas de pruebas automatizadas con **Bruno**.

Se incluye un script en PowerShell para ejecutar los tests y generar reportes de cobertura automáticamente: `test.ps1`

### Estructura de Tests

```
FunkoApi.Tests/
├── Unitarios/
│   ├── Controllers/
│   ├── Services/
│   └── Validators/
└── Integracion/
    ├── AuthTests/
    ├── FunkosTests/
    └── CategoriasTests/
```

## 🧰 Herramientas de Testing

El proyecto incluye colecciones de pruebas para **Bruno**:

```
BrunoTest-FunkoApi/
├── Auth/
├── Categorias/
├── Funkos/
├── GraphQL/
└── SignalR/
```

## 🗂️ Estructura del Proyecto

```
FunkoApi/
├── Auth/                 # Servicios de autenticación y tokens
├── Configuration/        # Configuraciones de la aplicación
├── Controllers/          # Controladores de la API
├── Data/                 # Contexto de base de datos
├── DTOs/                 # Data Transfer Objects
├── Errors/               # Manejo de errores
├── GraphQL/              # Configuración y tipos de GraphQL
├── Mappers/              # Mapeo entre entidades y DTOs
├── Migrations/           # Migraciones de Entity Framework
├── Models/               # Modelos de dominio
├── Repositories/         # Capa de acceso a datos
├── Services/             # Lógica de negocio
├── Storage/              # Servicios de almacenamiento
├── Validators/           # Validadores con FluentValidation
├── WebSockets/           # Hubs de SignalR
└── wwwroot/              # Archivos estáticos (imágenes)
```

## 🔒 Seguridad

- **ASP.NET Identity**: Gestión segura de usuarios y roles
- **JWT Authentication**: Tokens seguros con expiración configurable
- **Password Hashing**: Contraseñas hasheadas con ASP.NET Identity
- **Role-Based Authorization**: Control de acceso por roles
- **HTTPS**: Redirección automática a HTTPS
- **CORS**: Política CORS configurable
- **Input Validation**: Validación exhaustiva con FluentValidation

## 📧 Configuración de Email

Para habilitar el envío de emails (registro de usuarios):

1. Configura una cuenta de Gmail
2. Habilita la verificación en dos pasos
3. Genera una contraseña de aplicación
4. Actualiza `appsettings.json` con tus credenciales

## 🐳 Docker

### Servicios Disponibles

```bash
# Levantar servicios
docker-compose up -d

# Ver logs
docker-compose logs -f

# Detener servicios
docker-compose down

# Limpiar volúmenes
docker-compose down -v
```

## 📝 Documentación Adicional

- 📄 [Documentación de Endpoints](API_ENDPOINTS.md)
- 🔍 [GraphQL Playground](https://localhost:5000/graphql) (cuando la API esté corriendo)


## 🤝 Contribuciones

Este es un proyecto educativo. Si encuentras algún error o tienes sugerencias, no dudes en abrir un issue.

## 📝 Licencia

Este proyecto tiene fines educativos y está bajo la licencia **Creative Commons Reconocimiento-NoComercial-CompartirIgual 4.0 Internacional**.

## 👨‍💻 Autor

Codificado con 💖 por **Ángel Sánchez Gasanz**

---

⭐ Si este proyecto te resulta útil, considera darle una estrella en GitHub
