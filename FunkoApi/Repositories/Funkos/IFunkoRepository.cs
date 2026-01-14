using FunkoApi.Models;

namespace FunkoApi.Repositories.Funkos;

public interface IFunkoRepository
{
    Task<IEnumerable<Funko>> GetAllAsync();
    Task<Funko?> GetByIdAsync(long id);
    Task<Funko?> CreateAsync(Funko newFunko);
    Task<Funko?> UpdateAsync(long id, Funko updatedFunko);
    Task <Funko?>DeleteAsync(long id);
}