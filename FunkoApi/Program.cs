using FunkoApi.Data;
using FunkoApi.Repositories.Categorias;
using FunkoApi.Repositories.Funkos;
using FunkoApi.Services.Categorias;
using FunkoApi.Services.Funkos;
using FunkoApi.Validators.Funkos;
using Microsoft.EntityFrameworkCore;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

// 1. Base de Datos
builder.Services.AddDbContext<FunkoDbContext>(options =>
    options.UseInMemoryDatabase("FunkoDb"));

// 2. Caché en Memoria (IMemoryCache)
builder.Services.AddMemoryCache();

// 3. Repositories (Capa de Datos)
builder.Services.AddScoped<IFunkoRepository, FunkoRepository>();
builder.Services.AddScoped<ICategoriaRepository, CategoriaRepository>();

// 4. Servicios (Capa de Negocio)
builder.Services.AddScoped<IFunkoService, FunkoService>();
builder.Services.AddScoped<ICategoriaService, CategoriaService>();

// 5. Validadores (FluentValidation)
builder.Services.AddValidatorsFromAssemblyContaining<FunkoRequestValidator>();

// 6. Configuración de Controladores y Rutas
builder.Services.AddControllers();

// 7. Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 8. Manejo Global de Errores (ProblemDetails)
builder.Services.AddProblemDetails();

var app = builder.Build();

// --- PIPELINE DE PETICIONES HTTP ---

// Configuración para entorno de desarrollo
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseExceptionHandler(); 

app.UseAuthorization();

app.MapControllers();

// Creamos un scope temporal para obtener los servicios
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // Llamamos al método estático del Seeder
        await FunkoSeeder.InitializeAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ocurrió un error al ejecutar el Seed Data.");
    }
}

app.Run();