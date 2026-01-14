using CSharpFunctionalExtensions;
using FunkoApi.Dtos;
using FunkoApi.Errors;
using FunkoApi.Models;

namespace FunkoApi.Services.Categorias;

public interface ICategoriaService
{
    Task<IEnumerable<CategoriaRespondeDto>> GetAllAsync();
    Task<Result<CategoriaRespondeDto, AppError>> GetByIdAsync(Guid id);
    Task<Result<CategoriaRespondeDto, AppError>> CreateAsync(CategoriaRequestDto dto);
    Task<Result<CategoriaRespondeDto, AppError>> UpdateAsync(Guid id, CategoriaRequestDto dto);
    Task<Result<CategoriaRespondeDto, AppError>> DeleteAsync(Guid id);
}