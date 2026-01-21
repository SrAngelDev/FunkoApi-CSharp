using CSharpFunctionalExtensions;
using FluentValidation;
using FunkoApi.Dtos;
using FunkoApi.Errors;
using FunkoApi.Errors.Categorias;
using FunkoApi.Models;
using FunkoApi.Repositories.Categorias;
using Microsoft.Extensions.Caching.Memory;
using FunkoApi.Mappers;

namespace FunkoApi.Services.Categorias;

public class CategoriaService(
    ICategoriaRepository repository,
    IMemoryCache cache,
    IValidator<CategoriaRequestDto> validator) : ICategoriaService
{
    // Cache
    private const string CacheListKey = "categorias_all";
    private const string CacheBaseKey = "categoria_"; 
    private string GetKey(Guid id) => $"{CacheBaseKey}{id}";
    
    private readonly TimeSpan _cacheTime = TimeSpan.FromMinutes(30);

    public async Task<IEnumerable<CategoriaResponseDto>> GetAllAsync()
    {
        if (!cache.TryGetValue(CacheListKey, out IEnumerable<CategoriaResponseDto>? dtos))
        {
            var categorias = await repository.GetAllAsync();
            dtos = categorias.Select(c => c.ToResponseDto()).ToList();
            cache.Set(CacheListKey, dtos, _cacheTime);
        }
        return dtos!;
    }

    public async Task<Result<CategoriaResponseDto, AppError>> GetByIdAsync(Guid id)
    {
        string key = GetKey(id); 
        
        if (!cache.TryGetValue(key, out CategoriaResponseDto? dto))
        {
            var categoria = await repository.GetByIdAsync(id);
            
            if (categoria == null)
            {
                return Result.Failure<CategoriaResponseDto, AppError>(CategoriaError.NotFound(id));
            }
            
            dto = categoria.ToResponseDto();
            
            cache.Set(key, dto, _cacheTime);
        }
        
        return Result.Success<CategoriaResponseDto, AppError>(dto!);
    }

    public async Task<Result<CategoriaResponseDto, AppError>> CreateAsync(CategoriaRequestDto dto)
    {
        // Validamos
        var valResult = await validator.ValidateAsync(dto);
        if (!valResult.IsValid) 
        {
            return Result.Failure<CategoriaResponseDto, AppError>(
                new BusinessRuleError(valResult.Errors.First().ErrorMessage));
        }
        
        var existe = await repository.GetByNombreAsync(dto.Nombre);
        if (existe != null)
        {
            return Result.Failure<CategoriaResponseDto, AppError>(
                CategoriaError.NombreDuplicado(dto.Nombre));
        }
        
        // Creamos
        var nuevaEntidad = await repository.CreateAsync(dto.ToEntity());
        
        //Limpiamos la cache
        cache.Remove(CacheListKey);
        
        return Result.Success<CategoriaResponseDto, AppError>(nuevaEntidad.ToResponseDto());
    }
    
    public async Task<Result<CategoriaResponseDto, AppError>> UpdateAsync(Guid id, CategoriaRequestDto dto) 
    {
        // Validamos
        var valResult = await validator.ValidateAsync(dto);
        if (!valResult.IsValid) 
        {
            return Result.Failure<CategoriaResponseDto, AppError>(
                new BusinessRuleError(valResult.Errors.First().ErrorMessage));
        }
        
        var categoriaExistente = await repository.GetByNombreAsync(dto.Nombre);
        
        if (categoriaExistente != null && categoriaExistente.Id != id)
        {
            return Result.Failure<CategoriaResponseDto, AppError>(
                CategoriaError.NombreDuplicado(dto.Nombre));
        }
        
        var actualizado = await repository.UpdateAsync(id, new Categoria { Nombre = dto.Nombre });
        
        if (actualizado == null) 
        {
            return Result.Failure<CategoriaResponseDto, AppError>(CategoriaError.NotFound(id));
        }

        // 4. Invalidar cache
        cache.Remove(CacheListKey);
        cache.Remove(GetKey(id));

        return Result.Success<CategoriaResponseDto, AppError>(new CategoriaResponseDto(actualizado.Id, actualizado.Nombre));
    }

    public async Task<Result<CategoriaResponseDto, AppError>> DeleteAsync(Guid id) 
    {
        var eliminado = await repository.DeleteAsync(id);
        if (eliminado == null)
        {
            return Result.Failure<CategoriaResponseDto, AppError>(CategoriaError.NotFound(id));
        }

        // Invalidamos la cache vieja y la del objeto
        cache.Remove(CacheListKey);
        cache.Remove(GetKey(id));

        return Result.Success<CategoriaResponseDto, AppError>(new CategoriaResponseDto(eliminado.Id, eliminado.Nombre));
    }
}