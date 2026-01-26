using FunkoApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FunkoApi.Data;

public static class AppSeeder
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        // Contexto y managers
        using var context = serviceProvider.GetRequiredService<FunkoDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<long>>>();

        // Aseguramos que la BD existe
        await context.Database.MigrateAsync();
        
        // Creamos roles si no existen
        string[] roles = { Roles.Admin, Roles.User };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<long>(role));
            }
        }

        // Seed inicial de categorias y funkos
        if (!await context.Categorias.AnyAsync())
        {
            var catDisney = new Categoria { Id = Guid.NewGuid(), Nombre = "Disney", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            var catMarvel = new Categoria { Id = Guid.NewGuid(), Nombre = "Marvel", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            var catAnime = new Categoria { Id = Guid.NewGuid(), Nombre = "Anime", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

            await context.Categorias.AddRangeAsync(catDisney, catMarvel, catAnime);
            
            var funkos = new List<Funko>
            {
                new Funko { Nombre = "Mickey Mouse", Precio = 15.99m, CategoriaId = catDisney.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Funko { Nombre = "Iron Man", Precio = 19.50m, CategoriaId = catMarvel.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Funko { Nombre = "Stitch", Precio = 18.99m, CategoriaId = catDisney.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            };
            await context.Funkos.AddRangeAsync(funkos);
        }

        // Creamos un usuario administrador si no existe
        if (await userManager.FindByNameAsync("admin") == null)
        {
            var admin = new User
            {
                UserName = "admin",
                Email = "admin@funko.com",
                Nombre = "Administrador",
                EmailConfirmed = true
            };
            
            var result = await userManager.CreateAsync(admin, "Admin123!"); 
            
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, Roles.Admin);
            }
        }

        await context.SaveChangesAsync();
        Console.WriteLine("✅ Seed Data realizado correctamente: Roles, Usuarios y Funkos procesados.");
    }
}