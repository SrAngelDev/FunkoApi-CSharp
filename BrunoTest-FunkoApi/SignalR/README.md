# 📡 Tests de SignalR con Bruno

Esta carpeta contiene tests de Bruno diseñados para **disparar eventos SignalR** que puedes capturar con tu cliente SignalR HTML.

## 🎯 ¿Cómo funciona?

SignalR funciona con WebSockets, y Bruno no puede conectarse directamente como cliente WebSocket. Por lo tanto, estos tests funcionan así:

1. **Abre tu cliente HTML SignalR** (`cliente.html`) en el navegador
2. **Ejecuta los tests de Bruno** para disparar operaciones CRUD
3. **Observa en el cliente HTML** cómo recibe los eventos en tiempo real

## 🚀 Eventos SignalR Disponibles

Tu API envía estos eventos SignalR:

| Evento | Disparado por | Datos enviados |
|--------|---------------|----------------|
| `FunkoCreated` | POST /api/funkos | Objeto FunkoResponseDto completo |
| `FunkoUpdated` | PUT /api/funkos/{id} | Objeto FunkoResponseDto actualizado |
| `FunkoDeleted` | DELETE /api/funkos/{id} | ID del funko eliminado (long) |

## 📋 Tests Disponibles

### 1. **Trigger-FunkoCreated.bru**
- Crea un nuevo funko via REST API
- Dispara evento `FunkoCreated`
- Guarda el ID en `{{signalrFunkoId}}`

### 2. **Trigger-FunkoUpdated.bru**
- Actualiza el funko creado previamente
- Dispara evento `FunkoUpdated`
- Usa `{{signalrFunkoId}}` del test anterior

### 3. **Trigger-FunkoDeleted.bru**
- Elimina el funko creado
- Dispara evento `FunkoDeleted`
- Usa `{{signalrFunkoId}}` del test anterior

### 4. **Trigger-CicloCompleto.bru** ⭐
- Test automatizado que ejecuta todo el ciclo
- Crea → Actualiza (2s) → Elimina (4s)
- Dispara los 3 eventos en secuencia
- Perfecto para demostración

## 🔧 Configuración del Cliente SignalR

Tu cliente HTML debe conectarse a:
```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:5000/ws/funkos")
    .build();

// Escuchar eventos
connection.on("FunkoCreated", (funko) => {
    console.log("✅ Funko creado:", funko);
});

connection.on("FunkoUpdated", (funko) => {
    console.log("🔄 Funko actualizado:", funko);
});

connection.on("FunkoDeleted", (funkoId) => {
    console.log("❌ Funko eliminado ID:", funkoId);
});

await connection.start();
```

## 📝 Orden de Ejecución Recomendado

1. Ejecuta primero los tests prerequisitos:
   - `Categorias/Obtener Funko para Categoria` → Captura `categoriaId`
   - `Auth/Login` → Captura `authToken`

2. Abre tu cliente HTML SignalR en el navegador

3. Ejecuta los tests de SignalR:
   - **Opción Manual**: Ejecuta `Trigger-FunkoCreated`, luego `Trigger-FunkoUpdated`, luego `Trigger-FunkoDeleted`
   - **Opción Automática**: Solo ejecuta `Trigger-CicloCompleto` y observa la secuencia

## 💡 Tips

- **Console.log en Bruno**: Los tests imprimen mensajes útiles en la consola de Bruno
- **Timing**: El test de ciclo completo espera 2 segundos entre operaciones para que veas claramente cada evento
- **Debugging**: Si no recibes eventos, verifica que:
  - El cliente SignalR esté conectado
  - La API esté corriendo
  - El token de autorización sea válido

## 🎬 Demo Rápida

```bash
# 1. Inicia la API
dotnet run --project FunkoApi

# 2. Abre cliente.html en el navegador

# 3. En Bruno, ejecuta en orden:
# - Auth/Login
# - Categorias/Obtener Funko para Categoria
# - SignalR/Trigger-CicloCompleto

# 4. Observa en el navegador cómo recibe los 3 eventos
```

---

**Nota**: Bruno actúa como el "disparador" de eventos, mientras que tu cliente HTML actúa como el "receptor". Esta es la forma estándar de probar SignalR cuando la herramienta de testing no soporta WebSockets nativamente.
