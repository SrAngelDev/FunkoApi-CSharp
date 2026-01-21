using CSharpFunctionalExtensions;
using FunkoApi.Dtos;
using FunkoApi.Errors;

namespace FunkoApi.Services.Funkos;

public interface IFunkoService
{
    Task<IEnumerable<FunkoResponseDto>> GetAllAsync();
    Task<Result<FunkoResponseDto, AppError>> GetByIdAsync(long id);
    Task<Result<FunkoResponseDto, AppError>> CreateAsync(FunkoRequestDto dto);
    Task<Result<FunkoResponseDto, AppError>> UpdateAsync(long id, FunkoRequestDto dto);
    Task<Result<FunkoResponseDto, AppError>> UpdateImageAsync(long id, IFormFile file);
    Task<Result<FunkoResponseDto, AppError>> DeleteAsync(long id);
}