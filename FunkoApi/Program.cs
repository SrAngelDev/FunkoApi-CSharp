using FunkoApi.Data;
using FunkoApi.Repositories.Categorias;
using FunkoApi.Repositories.Funkos;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

//Registramos el FunkoDbContext
builder.Services.AddDbContext<FunkoDbContext>(options =>
    options.UseInMemoryDatabase("FunkoDb"));
    
//Repositorios
builder.Services.AddScoped<IFunkoRepository, FunkoRepository>();
builder.Services.AddScoped<ICategoriaRepository, CategoriaRepository>();

//Servicios

//Añado los controladores
builder.Services.AddControllers();

var app = builder.Build();

app.Run();