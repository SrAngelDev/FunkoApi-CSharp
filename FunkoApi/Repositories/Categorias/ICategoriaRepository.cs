using FunkoApi.Models;

namespace FunkoApi.Repositories.Categorias;

public interface ICategoriaRepository
{
    Task<IEnumerable<Categoria>> GetAllAsync();
    Task<Categoria?> GetByIdAsync(Guid id);
    Task<Categoria?> GetByNombreAsync(string nombre);
    Task<Categoria?> CreateAsync(Categoria categoria);
    Task<Categoria?> UpdateAsync(Guid id, Categoria categoria);
    Task<Categoria?> DeleteAsync(Guid id);
}