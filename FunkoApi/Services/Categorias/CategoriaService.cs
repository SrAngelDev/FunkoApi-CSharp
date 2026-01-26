using System.Text.Json;
using CSharpFunctionalExtensions;
using FluentValidation;
using FunkoApi.Dtos;
using FunkoApi.Errors;
using FunkoApi.Errors.Categorias;
using FunkoApi.Models;
using FunkoApi.Repositories.Categorias;
using Microsoft.Extensions.Caching.Distributed;
using FunkoApi.Mappers;

namespace FunkoApi.Services.Categorias;

public class CategoriaService(
    ICategoriaRepository repository,
    IDistributedCache cache,
    IValidator<CategoriaRequestDto> validator,
    ILogger<CategoriaService> logger) : ICategoriaService
{
    // Cache
    private readonly DistributedCacheEntryOptions _cacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
    };
    
    private const string CacheListKey = "categorias_all";
    private const string CacheBaseKey = "categoria_"; 
    private string GetKey(Guid id) => $"{CacheBaseKey}{id}";
    
    public async Task<IEnumerable<CategoriaResponseDto>> GetAllAsync()
    {
        var cachedData = await cache.GetStringAsync(CacheListKey);
        
        if (!string.IsNullOrEmpty(cachedData))
        {
            logger.LogInformation("--> Obteniendo Categorías desde Redis Cache");
            return JsonSerializer.Deserialize<IEnumerable<CategoriaResponseDto>>(cachedData)!;
        }

        logger.LogInformation("--> Obteniendo Categorías desde Base de Datos");
        var categorias = await repository.GetAllAsync();
        var dtos = categorias.Select(c => c.ToResponseDto()).ToList();
        
        await cache.SetStringAsync(CacheListKey, JsonSerializer.Serialize(dtos), _cacheOptions);
        
        return dtos;
    }

    public async Task<Result<CategoriaResponseDto, AppError>> GetByIdAsync(Guid id)
    {
        string key = GetKey(id); 
        var cachedData = await cache.GetStringAsync(key);
        
        if (!string.IsNullOrEmpty(cachedData))
        {
            logger.LogInformation($"--> Obteniendo Categoría {id} desde Redis Cache");
            return Result.Success<CategoriaResponseDto, AppError>(JsonSerializer.Deserialize<CategoriaResponseDto>(cachedData)!);
        }

        var categoria = await repository.GetByIdAsync(id);
        
        if (categoria == null)
        {
            return Result.Failure<CategoriaResponseDto, AppError>(CategoriaError.NotFound(id));
        }
        
        var dto = categoria.ToResponseDto();
        await cache.SetStringAsync(key, JsonSerializer.Serialize(dto), _cacheOptions);
        
        return Result.Success<CategoriaResponseDto, AppError>(dto);
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
        await cache.RemoveAsync(CacheListKey);
        
        return Result.Success<CategoriaResponseDto, AppError>(nuevaEntidad!.ToResponseDto());
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
        await cache.RemoveAsync(CacheListKey);
        await cache.RemoveAsync(GetKey(id));

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
        await cache.RemoveAsync(CacheListKey);
        await cache.RemoveAsync(GetKey(id));

        return Result.Success<CategoriaResponseDto, AppError>(new CategoriaResponseDto(eliminado.Id, eliminado.Nombre));
    }
}