using CSharpFunctionalExtensions;
using FunkoApi.Dtos;
using FunkoApi.Models;

namespace FunkoApi.Services.Categorias;

public interface ICategoriaService
{
    Task<IEnumerable<CategoriaRespondeDto>> GetAllAsync();
    Task<Result<CategoriaRespondeDto, string>> GetByIdAsync(Guid id);
    Task<Result<CategoriaRespondeDto, string>> GetByNombreAsync(string nombre);
    Task<Result<CategoriaRespondeDto, string>> CreateAsync(CategoriaRequestDto categoria);
    Task<Result<CategoriaRespondeDto, string>> UpdateAsync(Guid id, CategoriaRequestDto categoria);
    Task<Result<CategoriaRespondeDto, string>> DeleteAsync(Guid id);
}