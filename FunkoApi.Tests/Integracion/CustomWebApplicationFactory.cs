using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using FunkoApi.Data;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using FunkoApi.Models;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FunkoApi.Tests.Integration;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remover el descriptor del DbContext de PostgreSQL
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<FunkoDbContext>));
            
            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            // Remover también el DbContext registrado
            var dbContextService = services.SingleOrDefault(
                d => d.ServiceType == typeof(FunkoDbContext));
            
            if (dbContextService != null)
            {
                services.Remove(dbContextService);
            }

            // Remover Redis Cache
            services.RemoveAll<IDistributedCache>();
            services.AddDistributedMemoryCache();

            // Agregar DbContext con InMemory
            services.AddDbContext<FunkoDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDatabase");
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Este método se ejecuta DESPUÉS de ConfigureServices del Program.cs
            // Aquí sembramos los datos
            var sp = services.BuildServiceProvider();
            
            using (var scope = sp.CreateScope())
            {
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<FunkoDbContext>();
                var userManager = scopedServices.GetRequiredService<UserManager<User>>();
                var roleManager = scopedServices.GetRequiredService<RoleManager<IdentityRole<long>>>();

                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
                
                SeedTestData(db, userManager, roleManager).Wait();
            }
        });
    }

    private static async Task SeedTestData(
        FunkoDbContext context,
        UserManager<User> userManager,
        RoleManager<IdentityRole<long>> roleManager)
    {
        // Limpiar datos existentes
        context.Funkos.RemoveRange(context.Funkos);
        context.Categorias.RemoveRange(context.Categorias);
        await context.SaveChangesAsync();

        // Crear roles
        string[] roles = { Roles.Admin, Roles.User };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<long>(role));
            }
        }

        // Crear admin
        var adminUser = await userManager.FindByNameAsync("admin");
        if (adminUser == null)
        {
            adminUser = new User
            {
                UserName = "admin",
                Email = "admin@test.com",
                Nombre = "Admin",
                Apellidos = "Test"
            };
            await userManager.CreateAsync(adminUser, "Admin123");
            await userManager.AddToRoleAsync(adminUser, Roles.Admin);
        }

        // Crear user normal
        var normalUser = await userManager.FindByNameAsync("user");
        if (normalUser == null)
        {
            normalUser = new User
            {
                UserName = "user",
                Email = "user@test.com",
                Nombre = "User",
                Apellidos = "Test"
            };
            await userManager.CreateAsync(normalUser, "User123");
            await userManager.AddToRoleAsync(normalUser, Roles.User);
        }

        // Crear categorías
        var marvel = new Categoria
        {
            Id = Guid.NewGuid(),
            Nombre = "Marvel",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var dc = new Categoria
        {
            Id = Guid.NewGuid(),
            Nombre = "DC",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Categorias.AddRange(marvel, dc);
        await context.SaveChangesAsync();

        // Crear funkos
        var funkos = new[]
        {
            new Funko
            {
                Nombre = "Iron Man",
                Precio = 30.0m,
                CategoriaId = marvel.Id,
                Imagen = "ironman.png",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Funko
            {
                Nombre = "Batman",
                Precio = 25.0m,
                CategoriaId = dc.Id,
                Imagen = "batman.png",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        context.Funkos.AddRange(funkos);
        await context.SaveChangesAsync();
    }
}
