using CSharpFunctionalExtensions;
using FunkoApi.Dtos;
using FunkoApi.Errors;
using FunkoApi.Models;

namespace FunkoApi.Services.Categorias;

public interface ICategoriaService
{
    Task<IEnumerable<CategoriaResponseDto>> GetAllAsync();
    Task<Result<CategoriaResponseDto, AppError>> GetByIdAsync(Guid id);
    Task<Result<CategoriaResponseDto, AppError>> CreateAsync(CategoriaRequestDto dto);
    Task<Result<CategoriaResponseDto, AppError>> UpdateAsync(Guid id, CategoriaRequestDto dto);
    Task<Result<CategoriaResponseDto, AppError>> DeleteAsync(Guid id);
}