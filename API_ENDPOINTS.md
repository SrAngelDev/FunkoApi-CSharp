# 📚 Documentación de Endpoints - FunkoApi

API REST para la gestión de Funkos con autenticación JWT y control de roles.

---

## 🔐 Autenticación

Base URL: `/api/auth`

### 1. Registro de Usuario

**POST** `/api/auth/register`

Registra un nuevo usuario en el sistema.

**Request Body:**
```json
{
  "username": "string",
  "email": "string",
  "password": "string"
}
```

**Validaciones:**
- Username: requerido, mínimo 3 caracteres
- Email: formato válido, único en el sistema
- Password: mínimo 6 caracteres

**Respuestas:**

✅ **200 OK**
```json
"Usuario registrado correctamente"
```

❌ **400 Bad Request** - Validación fallida
```json
{
  "error": "Mensaje descriptivo del error"
}
```

**Notas:**
- El usuario se crea con rol `User` por defecto
- Se envía un email de bienvenida automáticamente

---

### 2. Inicio de Sesión

**POST** `/api/auth/login`

Autentica un usuario y devuelve un token JWT.

**Request Body:**
```json
{
  "username": "string",
  "password": "string"
}
```

**Validaciones:**
- Username: requerido
- Password: requerido

**Respuestas:**

✅ **200 OK**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

❌ **401 Unauthorized** - Credenciales incorrectas
```json
{
  "error": "Usuario o contraseña incorrectos"
}
```

**Notas:**
- El token debe incluirse en el header `Authorization: Bearer {token}` para endpoints protegidos
- El token contiene información del usuario y sus roles

---

## 🎨 Gestión de Funkos

Base URL: `/api/funkos`

### 3. Obtener Todos los Funkos

**GET** `/api/funkos`

Retorna la lista completa de funkos disponibles.

**Headers:**
- *(Público - No requiere autenticación)*

**Respuestas:**

✅ **200 OK**
```json
[
  {
    "id": 1,
    "nombre": "Batman",
    "precio": 19.99,
    "stock": 15,
    "imagen": "https://example.com/images/batman.jpg",
    "categoria": {
      "id": 1,
      "nombre": "DC Comics"
    },
    "createdAt": "2026-01-20T10:30:00Z",
    "updatedAt": "2026-01-20T10:30:00Z"
  }
]
```

---

### 4. Obtener Funko por ID

**GET** `/api/funkos/{id}`

Retorna un funko específico por su ID.

**Parámetros:**
- `id` (path) - ID del funko (long)

**Headers:**
- *(Público - No requiere autenticación)*

**Respuestas:**

✅ **200 OK**
```json
{
  "id": 1,
  "nombre": "Batman",
  "precio": 19.99,
  "stock": 15,
  "imagen": "https://example.com/images/batman.jpg",
  "categoria": {
    "id": 1,
    "nombre": "DC Comics"
  },
  "createdAt": "2026-01-20T10:30:00Z",
  "updatedAt": "2026-01-20T10:30:00Z"
}
```

❌ **404 Not Found** - Funko no existe
```json
{
  "error": "Funko con ID 999 no encontrado"
}
```

---

### 5. Crear Funko

**POST** `/api/funkos`

Crea un nuevo funko en el sistema.

**Headers:**
- `Authorization: Bearer {token}` *(Rol: Admin)*

**Request Body:**
```json
{
  "nombre": "string",
  "precio": 19.99,
  "stock": 10,
  "categoriaId": 1,
  "imagen": "https://example.com/images/funko.jpg"
}
```

**Validaciones:**
- Nombre: requerido, máximo 100 caracteres
- Precio: mayor a 0
- Stock: mayor o igual a 0
- CategoriaId: debe existir en el sistema
- Imagen: URL válida (opcional)

**Respuestas:**

✅ **201 Created**
```json
{
  "id": 5,
  "nombre": "Spider-Man",
  "precio": 24.99,
  "stock": 20,
  "imagen": "https://example.com/images/spiderman.jpg",
  "categoria": {
    "id": 2,
    "nombre": "Marvel"
  },
  "createdAt": "2026-01-27T22:00:00Z",
  "updatedAt": "2026-01-27T22:00:00Z"
}
```

**Headers de Respuesta:**
- `Location: /api/funkos/5`

❌ **400 Bad Request** - Validación fallida
```json
{
  "error": "El precio debe ser mayor a 0"
}
```

❌ **404 Not Found** - Categoría no existe
```json
{
  "error": "Categoría con ID 99 no encontrada"
}
```

❌ **401 Unauthorized** - Sin autenticación

❌ **403 Forbidden** - Usuario sin rol Admin

---

### 6. Actualizar Funko

**PUT** `/api/funkos/{id}`

Actualiza completamente un funko existente.

**Parámetros:**
- `id` (path) - ID del funko (long)

**Headers:**
- `Authorization: Bearer {token}` *(Rol: Admin)*

**Request Body:**
```json
{
  "nombre": "string",
  "precio": 19.99,
  "stock": 10,
  "categoriaId": 1,
  "imagen": "https://example.com/images/funko.jpg"
}
```

**Validaciones:**
- Mismas que en la creación

**Respuestas:**

✅ **200 OK**
```json
{
  "id": 5,
  "nombre": "Spider-Man Actualizado",
  "precio": 29.99,
  "stock": 5,
  "imagen": "https://example.com/images/spiderman-v2.jpg",
  "categoria": {
    "id": 2,
    "nombre": "Marvel"
  },
  "createdAt": "2026-01-27T22:00:00Z",
  "updatedAt": "2026-01-27T23:00:00Z"
}
```

❌ **404 Not Found** - Funko no existe
```json
{
  "error": "Funko con ID 999 no encontrado"
}
```

❌ **400 Bad Request** - Validación fallida

❌ **401 Unauthorized** - Sin autenticación

❌ **403 Forbidden** - Usuario sin rol Admin

---

### 7. Actualizar Imagen de Funko

**PATCH** `/api/funkos/{id}/imagen`

Actualiza únicamente la imagen de un funko mediante upload de archivo.

**Parámetros:**
- `id` (path) - ID del funko (long)

**Headers:**
- `Authorization: Bearer {token}` *(Rol: Admin)*
- `Content-Type: multipart/form-data`

**Request Body (Form Data):**
- `file` (file) - Archivo de imagen

**Validaciones:**
- El archivo no puede estar vacío
- Formatos aceptados: JPG, PNG, GIF (dependiendo de la implementación de Storage)

**Respuestas:**

✅ **200 OK**
```json
{
  "id": 5,
  "nombre": "Spider-Man",
  "precio": 24.99,
  "stock": 20,
  "imagen": "https://example.com/images/spiderman-nueva.jpg",
  "categoria": {
    "id": 2,
    "nombre": "Marvel"
  },
  "createdAt": "2026-01-27T22:00:00Z",
  "updatedAt": "2026-01-27T23:30:00Z"
}
```

❌ **400 Bad Request** - Sin archivo
```json
{
  "error": "No se ha enviado ninguna imagen"
}
```

❌ **404 Not Found** - Funko no existe
```json
{
  "error": "Funko con ID 999 no encontrado"
}
```

❌ **401 Unauthorized** - Sin autenticación

❌ **403 Forbidden** - Usuario sin rol Admin

**Ejemplo cURL:**
```bash
curl -X PATCH "https://api.example.com/api/funkos/5/imagen" \
  -H "Authorization: Bearer {token}" \
  -F "file=@/path/to/image.jpg"
```

---

### 8. Eliminar Funko

**DELETE** `/api/funkos/{id}`

Elimina un funko del sistema (soft delete o hard delete según implementación).

**Parámetros:**
- `id` (path) - ID del funko (long)

**Headers:**
- `Authorization: Bearer {token}` *(Rol: Admin)*

**Respuestas:**

✅ **204 No Content**
- Sin cuerpo de respuesta

❌ **404 Not Found** - Funko no existe
```json
{
  "error": "Funko con ID 999 no encontrado"
}
```

❌ **401 Unauthorized** - Sin autenticación

❌ **403 Forbidden** - Usuario sin rol Admin

---

## 🔒 Sistema de Autorización

### Roles Disponibles

- **User**: Usuario registrado (por defecto)
- **Admin**: Administrador con permisos completos

### Endpoints Públicos

- `GET /api/funkos` - Listar todos
- `GET /api/funkos/{id}` - Ver detalle
- `POST /api/auth/register` - Registro
- `POST /api/auth/login` - Login

### Endpoints Protegidos (Admin)

- `POST /api/funkos` - Crear
- `PUT /api/funkos/{id}` - Actualizar
- `PATCH /api/funkos/{id}/imagen` - Actualizar imagen
- `DELETE /api/funkos/{id}` - Eliminar

---

## 📝 Códigos de Estado HTTP

| Código | Significado |
|--------|-------------|
| 200 | ✅ OK - Operación exitosa |
| 201 | ✅ Created - Recurso creado |
| 204 | ✅ No Content - Eliminado correctamente |
| 400 | ❌ Bad Request - Validación fallida |
| 401 | ❌ Unauthorized - Sin autenticación |
| 403 | ❌ Forbidden - Sin permisos |
| 404 | ❌ Not Found - Recurso no existe |
| 409 | ❌ Conflict - Conflicto de datos |
| 500 | ❌ Internal Server Error - Error del servidor |

---

## 🔧 Errores Comunes

### 1. Error de Autenticación
```json
{
  "error": "No autorizado"
}
```
**Solución**: Incluir header `Authorization: Bearer {token}`

### 2. Error de Permisos
```json
{
  "error": "Acceso denegado"
}
```
**Solución**: Usar cuenta con rol Admin

### 3. Error de Validación
```json
{
  "error": "El precio debe ser mayor a 0"
}
```
**Solución**: Revisar los datos enviados según las validaciones

---

## 🧪 Ejemplo de Flujo Completo

### 1. Registrarse
```bash
POST /api/auth/register
{
  "username": "john_doe",
  "email": "john@example.com",
  "password": "securepass123"
}
```

### 2. Iniciar Sesión
```bash
POST /api/auth/login
{
  "username": "john_doe",
  "password": "securepass123"
}

# Respuesta:
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

### 3. Consultar Funkos (Sin autenticación)
```bash
GET /api/funkos
```

### 4. Crear Funko (Con token Admin)
```bash
POST /api/funkos
Authorization: Bearer {token}

{
  "nombre": "Iron Man",
  "precio": 34.99,
  "stock": 8,
  "categoriaId": 2,
  "imagen": "https://example.com/ironman.jpg"
}
```

### 5. Actualizar Imagen
```bash
PATCH /api/funkos/1/imagen
Authorization: Bearer {token}
Content-Type: multipart/form-data

file: [archivo de imagen]
```

---

## 📌 Notas Técnicas

- **Formato de Fecha**: ISO 8601 (UTC) - `2026-01-27T22:00:00Z`
- **Content-Type Request**: `application/json` (excepto upload de archivos)
- **Content-Type Response**: `application/json`
- **Codificación**: UTF-8
- **Versionado**: Sin versión explícita en URL (v1 implícita)

---

## 🌐 WebSockets (Opcional)

Si la API incluye notificaciones en tiempo real:

**Endpoint**: `/hubs/funkos`

**Eventos:**
- `FunkoCreated` - Nuevo funko creado
- `FunkoUpdated` - Funko actualizado
- `FunkoDeleted` - Funko eliminado

---

## 📊 GraphQL (Opcional)

Si la API incluye soporte GraphQL:

**Endpoint**: `/graphql`

Ver documentación en `/graphql/playground` (entorno de desarrollo)

---

## 📧 Contacto y Soporte

- **Email**: support@funkoapi.com
- **Repositorio**: [GitHub](https://github.com/usuario/FunkoApi)
- **Documentación**: [Swagger UI](https://api.funkoapi.com/swagger)

---

**Última actualización**: 28 de enero de 2026
