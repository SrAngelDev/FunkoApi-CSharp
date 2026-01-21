using FunkoApi.Dtos;
using FunkoApi.Errors;
using FunkoApi.Models;
using Microsoft.AspNetCore.Mvc;
using FunkoApi.Services.Funkos;
using Microsoft.AspNetCore.Authorization;

namespace FunkoApi.Controllers;

[ApiController]
[Route("api/funkos")] 
public class FunkosController(IFunkoService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<FunkoResponseDto>>> GetAll()
    {
        var result = await service.GetAllAsync();
        return Ok(result);
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<FunkoResponseDto>> GetById(long id)
    {
        var result = await service.GetByIdAsync(id);
        
        if (result.IsFailure)
        {
            return HandleError(result.Error);
        }

        return Ok(result.Value);
    }
    
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<FunkoResponseDto>> Create([FromBody] FunkoRequestDto dto)
    {
        var result = await service.CreateAsync(dto);

        if (result.IsFailure)
        {
            return HandleError(result.Error);
        }
        
        return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
    }
    
    [HttpPut("{id}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<FunkoResponseDto>> Update(long id, [FromBody] FunkoRequestDto dto)
    {
        var result = await service.UpdateAsync(id, dto);

        if (result.IsFailure)
        {
            return HandleError(result.Error);
        }

        return Ok(result.Value); 
    }
    
    // PATCH: api/funkos/5/imagen
    [HttpPatch("{id}/imagen")]
    [Consumes("multipart/form-data")] // Importante para ficheros
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<FunkoResponseDto>> UpdateImage(long id, IFormFile file)
    {
        // Validar que mandan algo
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "No se ha enviado ninguna imagen" });
        }

        var result = await service.UpdateImageAsync(id, file);

        if (result.IsFailure)
        {
            return HandleError(result.Error);
        }

        return Ok(result.Value);
    }
    
    [HttpDelete("{id}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult> Delete(long id)
    {
        var result = await service.DeleteAsync(id);

        if (result.IsFailure)
        {
            return HandleError(result.Error);
        }
        
        return NoContent(); 
    }
    
    private ActionResult HandleError(AppError error)
    {
        return error switch
        {
            NotFoundError => NotFound(new { error = error.Message }),
            ConflictError => Conflict(new { error = error.Message }), // 409
            BusinessRuleError => BadRequest(new { error = error.Message }), // 400
            _ => StatusCode(500, new { error = "Error interno del servidor" })
        };
    }
}