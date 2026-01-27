
using System.Text;
using System.Diagnostics.CodeAnalysis;
using FluentValidation;
using FunkoApi.Auth;
using FunkoApi.Configuration;
using FunkoApi.Data;
using FunkoApi.Dtos;
using FunkoApi.Errors;
using FunkoApi.GraphQL;
using FunkoApi.GraphQL.Types;
using FunkoApi.Models;
using FunkoApi.Repositories.Categorias;
using FunkoApi.Repositories.Funkos;
using FunkoApi.Services.Categorias;
using FunkoApi.Services.Email;
using FunkoApi.Services.Funkos;
using FunkoApi.Storage;
using FunkoApi.Validators.Funkos;
using FunkoApi.WebSockets;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

//Configuracion para el servicio de email
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminPolicy", policy => 
        policy.RequireRole("ADMIN"));
});

// Base de Datos - Solo configurar PostgreSQL si NO estamos en Testing
if (builder.Environment.EnvironmentName != "Testing")
{
    var connectionString = builder.Configuration.GetConnectionString("FunkoDb");
    builder.Services.AddDbContext<FunkoDbContext>(options =>
        options.UseNpgsql(connectionString));

    // Caché con Redis (IDistributedCache)
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = builder.Configuration.GetConnectionString("Redis");
        options.InstanceName = "FunkoApi_"; // Prefijo 
    });
}

// Repositories (Capa de Datos)
builder.Services.AddScoped<IFunkoRepository, FunkoRepository>();
builder.Services.AddScoped<ICategoriaRepository, CategoriaRepository>();

// Servicios (Capa de Negocio)
builder.Services.AddScoped<IFunkoService, FunkoService>();
builder.Services.AddScoped<ICategoriaService, CategoriaService>();
builder.Services.AddScoped<IStorageService, LocalStorageService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddTransient<IEmailService, EmailService>();

builder.Services.AddIdentity<User, IdentityRole<long>>(options =>
    {
        // Configuración de políticas de contraseña
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
    
        // Configuración de usuario
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<FunkoDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false, // En producción true
            ValidateAudience = false, // En producción true
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            // Debe coincidir con la clave del TokenService
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "ClaveSecretaSuperSeguraParaDesarrollo1234!"))
        };
    });

// REGISTRO DE GRAPHQL 
builder.Services.AddGraphQLConfig(); 

// REGISTRO DE SIGNALR
builder.Services.AddSignalR(); 

// Validadores (FluentValidation)
builder.Services.AddValidatorsFromAssemblyContaining<FunkoRequestValidator>();

// Configuración de Controladores y Rutas
builder.Services.AddControllers();

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Manejo Global de Errores (ProblemDetails)
builder.Services.AddProblemDetails();

// Política CORS (Permisiva para desarrollo)
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.AllowAnyHeader()
            .AllowAnyMethod()
            .SetIsOriginAllowed((host) => true) // Permitir cualquier origen (localhost, archivos locales)
            .AllowCredentials(); // Necesario para SignalR
    });
});

var app = builder.Build();

app.UseCors("CorsPolicy");

// Configuración para entorno de desarrollo
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles(); 

app.UseHttpsRedirection();

app.UseExceptionHandler(); 

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

// Creamos un scope temporal para obtener los servicios - Solo si NO estamos en Testing
if (app.Environment.EnvironmentName != "Testing")
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            // Llamamos al método estático del Seeder
            await AppSeeder.InitializeAsync(services);
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Ocurrió un error al ejecutar el Seed Data.");
        }
    }
}

// Mapeo de graphql
app.MapGraphQL("/graphql");

// Mapeo de signalr
app.MapHub<FunkoHub>("/ws/funkos");

app.Run();

// Hacer Program accesible para tests de integración
[ExcludeFromCodeCoverage]
public partial class Program { }

