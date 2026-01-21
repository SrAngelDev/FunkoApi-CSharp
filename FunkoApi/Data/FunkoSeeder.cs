using FunkoApi.Models;

namespace FunkoApi.Data;

using Microsoft.EntityFrameworkCore;

public static class FunkoSeeder
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        // Obtenemos el contexto de la base de datos
        using var context = serviceProvider.GetRequiredService<FunkoDbContext>();

        // Aseguramos que la BD existe (importante para InMemory)
        await context.Database.EnsureCreatedAsync();

        // Si ya hay Funkos, no hacemos nada (evita duplicados al reiniciar si usaras BD real)
        if (await context.Funkos.AnyAsync()) return;
        
        var catDisney = new Categoria 
        { 
            Id = Guid.NewGuid(), 
            Nombre = "Disney", 
            CreatedAt = DateTime.UtcNow, 
            UpdatedAt = DateTime.UtcNow 
        };
        
        var catMarvel = new Categoria 
        { 
            Id = Guid.NewGuid(), 
            Nombre = "Marvel", 
            CreatedAt = DateTime.UtcNow, 
            UpdatedAt = DateTime.UtcNow 
        };
        
        var catAnime = new Categoria 
        { 
            Id = Guid.NewGuid(), 
            Nombre = "Anime", 
            CreatedAt = DateTime.UtcNow, 
            UpdatedAt = DateTime.UtcNow 
        };

        await context.Categorias.AddRangeAsync(catDisney, catMarvel, catAnime);

        var funkos = new List<Funko>
        {
            new Funko 
            { 
                Nombre = "Mickey Mouse", 
                Precio = 15.99m, 
                CategoriaId = catDisney.Id,
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow 
            },
            new Funko 
            { 
                Nombre = "Iron Man", 
                Precio = 19.50m, 
                CategoriaId = catMarvel.Id,
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow 
            },
            new Funko 
            { 
                Nombre = "Spider-Man No Way Home", 
                Precio = 22.00m, 
                CategoriaId = catMarvel.Id,
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow 
            },
            new Funko 
            { 
                Nombre = "Naruto Uzumaki", 
                Precio = 14.99m, 
                CategoriaId = catAnime.Id,
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow 
            },
             new Funko 
            { 
                Nombre = "Stitch", 
                Precio = 18.99m, 
                CategoriaId = catDisney.Id,
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow 
            }
        };

        await context.Funkos.AddRangeAsync(funkos);
        
        await context.SaveChangesAsync();
        
        Console.WriteLine("✅ Seed Data realizado correctamente: Categorías y Funkos creados.");
    }
}