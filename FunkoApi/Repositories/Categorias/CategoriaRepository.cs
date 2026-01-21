using FunkoApi.Data;
using FunkoApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FunkoApi.Repositories.Categorias;

public class CategoriaRepository(FunkoDbContext context) : ICategoriaRepository
{
    public async Task<IEnumerable<Categoria>> GetAllAsync()
    {
        return await context.Categorias.ToListAsync();
    }

    public async Task<Categoria?> GetByIdAsync(Guid id)
    {
        // El primero es ideal para buscar por algo concreto, como en el nombre más abajo
        // return await context.Categorias.FirstOrDefaultAsync(c => c.Id == id);
        // Este es el ideal para las claves ya que busca automaticamente
        return await context.Categorias.FindAsync(id);
    }

    public async Task<Categoria?> GetByNombreAsync(string nombre)
    {
        return await context.Categorias.FirstOrDefaultAsync(c => c.Nombre.ToLower() == nombre.ToLower());
    }

    public async Task<Categoria?> CreateAsync(Categoria categoria)
    {
        context.Categorias.Add(categoria);
        await context.SaveChangesAsync();
        return categoria;
    }

    public async Task<Categoria?> UpdateAsync(Guid id, Categoria categoria)
    {
        var categoriaToUpdate = await context.Categorias.FindAsync(id);
        if (categoriaToUpdate == null) return null;
        
        categoriaToUpdate.Nombre = categoria.Nombre;
        
        context.Categorias.Update(categoriaToUpdate);
        await context.SaveChangesAsync();
        return categoriaToUpdate;
    }

    public async Task<Categoria?> DeleteAsync(Guid id)
    {
        var categoria = await context.Categorias.FindAsync(id);
        if (categoria == null) return null;
        
        context.Categorias.Remove(categoria);
        await context.SaveChangesAsync();
        return categoria;
    }
}