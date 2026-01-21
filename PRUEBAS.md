### 🚀 Guía de Pruebas FunkoAPI

Bienvenido a la guía de pruebas para la API de gestión de Funkos. A continuación, encontrarás los endpoints disponibles, ejemplos de solicitudes y casos de prueba sugeridos para verificar el correcto funcionamiento de la API.

#### 🔐 1. Autenticación (Auth)

**Registro de Usuario**
- **Endpoint:** `POST /auth/register`
- **Cuerpo (JSON):**
```json
{
  "username": "nuevoUsuario",
  "password": "Password123!",
  "email": "usuario@ejemplo.com"
}
```

**Login de Usuario**
- **Endpoint:** `POST /auth/login`
- **Cuerpo (JSON):**
```json
{
  "username": "admin",
  "password": "admin123"
}
```
> **Nota:** El login te devolverá un `token`. Cópialo para las pruebas protegidas (Admin).

---

#### 🧸 2. Gestión de Funkos (Funkos)

**Obtener todos los Funkos**
- **Endpoint:** `GET /funkos`
- **Acceso:** Público

**Obtener un Funko por ID**
- **Endpoint:** `GET /funkos/{id}`
- **Ejemplo:** `GET /funkos/1`

**Crear un Funko (Requiere Admin)**
- **Endpoint:** `POST /funkos`
- **Auth:** `Bearer Token` (debes poner el token en la pestaña Auth -> Bearer Token)
- **Cuerpo (JSON):**
```json
{
  "nombre": "Baby Yoda",
  "categoriaId": "ID-DE-CATEGORIA-EXISTENTE",
  "precio": 25.50
}
```
> *Nota: Obtén un `categoriaId` válido del seeder (ej. de Disney o Marvel).*

**Actualizar un Funko (Requiere Admin)**
- **Endpoint:** `PUT /funkos/{id}`
- **Auth:** `Bearer Token`
- **Cuerpo (JSON):**
```json
{
  "nombre": "Mickey Mouse Classic",
  "categoriaId": "ID-DE-CATEGORIA-EXISTENTE",
  "precio": 18.00
}
```

**Actualizar Imagen del Funko (Requiere Admin)**
- **Endpoint:** `PATCH /funkos/{id}/imagen`
- **Auth:** `Bearer Token`
- **Body:** `form-data`
  - Clave: `file` (selecciona tipo 'File' y sube una imagen)

**Eliminar un Funko (Requiere Admin)**
- **Endpoint:** `DELETE /funkos/{id}`
- **Auth:** `Bearer Token`

---

#### 🧪 3. Casos de Prueba Sugeridos

| Caso de Prueba | Método | Endpoint | Resultado Esperado |
| :--- | :---: | :--- | :--- |
| **Obtener lista** | GET | `/funkos` | `200 OK` + Lista de Funkos |
| **Ver detalle existente** | GET | `/funkos/1` | `200 OK` + Objeto Funko |
| **Ver detalle no existente**| GET | `/funkos/999` | `404 Not Found` |
| **Crear sin token** | POST | `/funkos` | `401 Unauthorized` |
| **Crear con token Admin** | POST | `/funkos` | `201 Created` |
| **Login fallido** | POST | `/auth/login` | `401 Unauthorized` |

---

#### 💡 Tips para Bruno/Postman
1. **Variables de Entorno:** Crea una variable `baseUrl` y otra `token`. 
2. **Scripts:** En el login, puedes usar un script para guardar automáticamente el token:
   - **Postman (Tests):** `pm.environment.set("token", pm.response.json().token);`
   - **Bruno (Script -> Post-response):** `bru.setEnvVar("token", res.getBody().token);`