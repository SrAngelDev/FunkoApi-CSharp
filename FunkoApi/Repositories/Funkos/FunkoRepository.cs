using FunkoApi.Data;
using FunkoApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FunkoApi.Repositories.Funkos;

public class FunkoRepository(FunkoDbContext context) : IFunkoRepository
{
    
    public async Task<IEnumerable<Funko>> GetAllAsync()
    {
        return await context.Funkos
            .Include(f => f.Categoria) 
            .ToListAsync();
    }

    public async Task<Funko?> GetByIdAsync(long id)
    {
        return await context.Funkos
            .Include(f => f.Categoria)
            .FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task<Funko?> CreateAsync(Funko newFunko)
    {
        context.Funkos.Add(newFunko);
        await context.SaveChangesAsync();
        return newFunko;
    }

    public async Task<Funko?> UpdateAsync(long id, Funko updatedFunko)
    {
        var oldFunko = await context.Funkos.FindAsync(id);
        if (oldFunko == null) return null;
        
        oldFunko.Nombre = updatedFunko.Nombre;
        oldFunko.Categoria = updatedFunko.Categoria;
        oldFunko.Precio = updatedFunko.Precio;
        
        await context.SaveChangesAsync();
        return oldFunko;
    }

    public async Task<Funko?> DeleteAsync(long id)
    {
        var funko = await context.Funkos.FindAsync(id);
        if (funko == null) return null;
        
        context.Funkos.Remove(funko);
        await context.SaveChangesAsync();
        return funko;
    }
}