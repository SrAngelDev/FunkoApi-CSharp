using System.Text.Json;
using CSharpFunctionalExtensions;
using FluentValidation;
using FunkoApi.Dtos;
using FunkoApi.Errors;
using FunkoApi.Errors.Funkos;
using FunkoApi.Repositories.Categorias;
using FunkoApi.Repositories.Funkos;
using FunkoApi.Mappers;
using FunkoApi.Storage;
using FunkoApi.WebSockets;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Distributed;

namespace FunkoApi.Services.Funkos;

public class FunkoService(
    IFunkoRepository repository,
    ICategoriaRepository categoriaRepository,
    IDistributedCache cache,
    IValidator<FunkoRequestDto> validator,
    IStorageService storage,
    ILogger<FunkoService> logger,
    IHubContext<FunkoHub> hubContext) : IFunkoService
{
    // Configuración de expiración para Redis
    private readonly DistributedCacheEntryOptions _cacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
    };

    private const string CacheKeyAll = "funkos_all";
    private static string GetKey(long id) => $"funko_{id}";

    public async Task<IEnumerable<FunkoResponseDto>> GetAllAsync()
    {
        // Buscamos en caché
        var cachedData = await cache.GetStringAsync(CacheKeyAll);
        
        if (!string.IsNullOrEmpty(cachedData))
        {
            logger.LogInformation("--> Obteniendo Funkos desde Redis Cache");
            // Deserializamos el JSON de vuelta a objetos 
            return JsonSerializer.Deserialize<IEnumerable<FunkoResponseDto>>(cachedData)!;
        }

        // Si no está en caché, buscar en BD
        logger.LogInformation("--> Obteniendo Funkos desde Base de Datos");
        var funkos = await repository.GetAllAsync();
        var response = funkos.Select(f => f.ToResponseDto()).ToList();

        // Guardar en cache
        var jsonResponse = JsonSerializer.Serialize(response);
        await cache.SetStringAsync(CacheKeyAll, jsonResponse, _cacheOptions);

        return response;
    }

    public async Task<Result<FunkoResponseDto, AppError>> GetByIdAsync(long id)
    {
        string key = GetKey(id);
        var cachedData = await cache.GetStringAsync(key);
        
        if (!string.IsNullOrEmpty(cachedData))
        {
            logger.LogInformation($"--> Obteniendo Funko {id} desde Redis Cache");
            return Result.Success<FunkoResponseDto, AppError>(JsonSerializer.Deserialize<FunkoResponseDto>(cachedData)!);
        }

        var funko = await repository.GetByIdAsync(id);
        
        if (funko == null) 
            return Result.Failure<FunkoResponseDto, AppError>(FunkoError.NotFound(id));

        var response = funko.ToResponseDto();
        
        // Guardar en cache
        await cache.SetStringAsync(key, JsonSerializer.Serialize(response), _cacheOptions);
        
        return Result.Success<FunkoResponseDto, AppError>(response);
    }

    public async Task<Result<FunkoResponseDto, AppError>> CreateAsync(FunkoRequestDto dto)
    {
        // Primero validamos
        var valResult = await validator.ValidateAsync(dto);
        if (!valResult.IsValid) 
            return Result.Failure<FunkoResponseDto, AppError>(new BusinessRuleError(valResult.Errors.First().ErrorMessage));
    
        // Compruebo la categoria si existe
        var categoria = await categoriaRepository.GetByIdAsync(dto.CategoriaId);
        if (categoria == null) 
            return Result.Failure<FunkoResponseDto, AppError>(FunkoError.CategoriaNoEncontrada(dto.CategoriaId.ToString()));
    
        // Guardar en BBDD
        var nuevo = await repository.CreateAsync(dto.ToEntity(categoria.Id));
    
        // Gestionar la cache
        await cache.RemoveAsync("cache_key_all");

        var funkoResponse = nuevo!.ToResponseDto();
        
        await hubContext.Clients.All.SendAsync("FunkoCreated", funkoResponse);
        
        return Result.Success<FunkoResponseDto, AppError>(funkoResponse);
    }

    public async Task<Result<FunkoResponseDto, AppError>> UpdateAsync(long id, FunkoRequestDto dto)
    {
        // Lo primero valido
        var valResult = await validator.ValidateAsync(dto);
        if (!valResult.IsValid) 
            return Result.Failure<FunkoResponseDto, AppError>(new BusinessRuleError(valResult.Errors.First().ErrorMessage));
    
        var existente = await repository.GetByIdAsync(id);
        if (existente == null) 
            return Result.Failure<FunkoResponseDto, AppError>(FunkoError.NotFound(id));
    
        var categoria = await categoriaRepository.GetByIdAsync(dto.CategoriaId);
        if (categoria == null) 
            return Result.Failure<FunkoResponseDto, AppError>(FunkoError.CategoriaNoEncontrada(dto.CategoriaId.ToString()));
    
        // Actualizo los datos
        existente.Nombre = dto.Nombre;
        existente.Precio = dto.Precio;
        existente.CategoriaId = categoria.Id; 
        existente.UpdatedAt = DateTime.UtcNow; 
    
        // Actualizo en BBDD
        var actualizado = await repository.UpdateAsync(id, existente);

        if (actualizado == null) 
            return Result.Failure<FunkoResponseDto, AppError>(FunkoError.NotFound(id));
    
        // Gestiono la cache
        await cache.RemoveAsync(CacheKeyAll); 
        await cache.RemoveAsync(GetKey(id));   
        
        var responseDto = actualizado.ToResponseDto();
        
        await hubContext.Clients.All.SendAsync("FunkoUpdated", responseDto);

        return Result.Success<FunkoResponseDto, AppError>(responseDto);
    }

    public async Task<Result<FunkoResponseDto, AppError>> DeleteAsync(long id)
    {
        // Elimino de la BBDD
        var eliminado = await repository.DeleteAsync(id);
    
        // Si no existe, devolvemos error
        if (eliminado == null) 
            return Result.Failure<FunkoResponseDto, AppError>(FunkoError.NotFound(id));
    
        // Invalido la cache
        await cache.RemoveAsync(CacheKeyAll);
        await cache.RemoveAsync(GetKey(id));
    

        var responseDto = eliminado.ToResponseDto();
        
        await hubContext.Clients.All.SendAsync("FunkoDeleted", responseDto.Id);
        
        return Result.Success<FunkoResponseDto, AppError>(responseDto);
    }
    
    public async Task<Result<FunkoResponseDto, AppError>> UpdateImageAsync(long id, IFormFile file)
    {
        // Existe el Funko?
        var funko = await repository.GetByIdAsync(id);
        if (funko == null) return Result.Failure<FunkoResponseDto, AppError>(FunkoError.NotFound(id));

        // Guardar nueva imagen
        string nuevoNombreImagen;
        try 
        {
            nuevoNombreImagen = await storage.SaveFileAsync(file);
        }
        catch (Exception ex)
        {
            return Result.Failure<FunkoResponseDto, AppError>(new BusinessRuleError($"Error al subir imagen: {ex.Message}"));
        }

        // Borrar imagen antigua si no es la por defecto
        if (!string.IsNullOrEmpty(funko.Imagen) && !funko.Imagen.StartsWith("https"))
        {
            storage.DeleteFile(funko.Imagen);
        }

        // Actualizar entidad y BD
        funko.Imagen = nuevoNombreImagen; // Guardamos solo el nombre 
        funko.UpdatedAt = DateTime.UtcNow;
        
        var actualizado = await repository.UpdateAsync(id, funko);

        // Invalidar caché
        await cache.RemoveAsync(CacheKeyAll);
        await cache.RemoveAsync(GetKey(id));

        return Result.Success<FunkoResponseDto, AppError>(actualizado!.ToResponseDto());
    }
}