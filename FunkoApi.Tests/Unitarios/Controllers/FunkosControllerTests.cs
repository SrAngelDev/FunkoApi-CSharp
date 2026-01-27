using FunkoApi.Controllers;
using FunkoApi.Dtos;
using FunkoApi.Errors;
using FunkoApi.Services.Funkos;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using CSharpFunctionalExtensions;

namespace FunkoApi.Tests.Controllers;

[TestFixture]
public class FunkosControllerTests
{
    private Mock<IFunkoService> _serviceMock;
    private FunkosController _controller;

    [SetUp]
    public void SetUp()
    {
        _serviceMock = new Mock<IFunkoService>();
        _controller = new FunkosController(_serviceMock.Object);
    }

    [Test]
    public async Task GetAll_RetornaListaDeFunkos()
    {
        // ARRANGE
        var categoriaDto = new CategoriaResponseDto(Guid.NewGuid(), "DC");
        var funkos = new List<FunkoResponseDto>
        {
            new FunkoResponseDto(1, "Batman", categoriaDto, 20.5m, "batman.png", DateTime.UtcNow, DateTime.UtcNow),
            new FunkoResponseDto(2, "Superman", categoriaDto, 25.0m, "superman.png", DateTime.UtcNow, DateTime.UtcNow)
        };

        _serviceMock.Setup(s => s.GetAllAsync())
            .ReturnsAsync(funkos);

        // ACT
        var resultado = await _controller.GetAll();

        // ASSERT
        Assert.That(resultado.Result, Is.InstanceOf<OkObjectResult>());
        
        var okResult = (OkObjectResult)resultado.Result!;
        var model = (IEnumerable<FunkoResponseDto>)okResult.Value!;

        Assert.That(model.Count(), Is.EqualTo(2));
        Assert.That(model.First().Nombre, Is.EqualTo("Batman"));
    }

    [Test]
    public async Task GetById_CuandoExiste_RetornaOkConFunko()
    {
        // ARRANGE
        long idExistente = 1;
        var categoriaDto = new CategoriaResponseDto(Guid.NewGuid(), "Marvel");
        var funkoDto = new FunkoResponseDto(
            idExistente, 
            "Spider-Man", 
            categoriaDto, 
            30.0m, 
            "spiderman.png", 
            DateTime.UtcNow, 
            DateTime.UtcNow
        );

        _serviceMock.Setup(s => s.GetByIdAsync(idExistente))
            .ReturnsAsync(Result.Success<FunkoResponseDto, AppError>(funkoDto));

        // ACT
        var resultado = await _controller.GetById(idExistente);

        // ASSERT
        Assert.That(resultado.Result, Is.InstanceOf<OkObjectResult>());
        
        var okResult = (OkObjectResult)resultado.Result!;
        var model = (FunkoResponseDto)okResult.Value!;

        Assert.That(model.Nombre, Is.EqualTo("Spider-Man"));
        Assert.That(model.Id, Is.EqualTo(idExistente));
    }

    [Test]
    public async Task GetById_CuandoNoExiste_RetornaNotFound()
    {
        // ARRANGE
        long idNoExistente = 99;
        var error = new NotFoundError("Funko no encontrado");

        _serviceMock.Setup(s => s.GetByIdAsync(idNoExistente))
            .ReturnsAsync(Result.Failure<FunkoResponseDto, AppError>(error));

        // ACT
        var resultado = await _controller.GetById(idNoExistente);

        // ASSERT
        Assert.That(resultado.Result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task Create_CuandoServicioRetornaExito_DevuelveCreated()
    {
        // ARRANGE
        var categoriaId = Guid.NewGuid();
        var requestDto = new FunkoRequestDto("Superman", categoriaId, 15.0m);
        var categoriaDto = new CategoriaResponseDto(categoriaId, "DC");
        var responseDto = new FunkoResponseDto(
            1, 
            "Superman", 
            categoriaDto, 
            15.0m, 
            "superman.png", 
            DateTime.UtcNow, 
            DateTime.UtcNow
        );
        
        _serviceMock.Setup(s => s.CreateAsync(requestDto))
            .ReturnsAsync(Result.Success<FunkoResponseDto, AppError>(responseDto));

        // ACT
        var resultado = await _controller.Create(requestDto);

        // ASSERT
        Assert.That(resultado.Result, Is.InstanceOf<CreatedAtActionResult>());
        
        var createdResult = (CreatedAtActionResult)resultado.Result!;
        var model = (FunkoResponseDto)createdResult.Value!;

        Assert.That(model.Nombre, Is.EqualTo("Superman"));
        Assert.That(model.Id, Is.EqualTo(1));
        Assert.That(createdResult.ActionName, Is.EqualTo(nameof(FunkosController.GetById)));
    }

    [Test]
    public async Task Create_CuandoFallaValidacion_DevuelveBadRequest()
    {
        // ARRANGE
        var requestDto = new FunkoRequestDto("", Guid.Empty, -5); // Datos inválidos
        var error = new BusinessRuleError("El precio debe ser positivo");
        
        _serviceMock.Setup(s => s.CreateAsync(requestDto))
            .ReturnsAsync(Result.Failure<FunkoResponseDto, AppError>(error));

        // ACT
        var resultado = await _controller.Create(requestDto);

        // ASSERT
        Assert.That(resultado.Result, Is.InstanceOf<BadRequestObjectResult>());
        
        var badRequestResult = (BadRequestObjectResult)resultado.Result!;
        Assert.That(badRequestResult.StatusCode, Is.EqualTo(400));
    }

    [Test]
    public async Task Update_CuandoExiste_RetornaOkConFunkoActualizado()
    {
        // ARRANGE
        long idExistente = 1;
        var categoriaId = Guid.NewGuid();
        var requestDto = new FunkoRequestDto("Batman Actualizado", categoriaId, 22.5m);
        var categoriaDto = new CategoriaResponseDto(categoriaId, "DC");
        var responseDto = new FunkoResponseDto(
            idExistente, 
            "Batman Actualizado", 
            categoriaDto, 
            22.5m, 
            "batman.png", 
            DateTime.UtcNow, 
            DateTime.UtcNow
        );

        _serviceMock.Setup(s => s.UpdateAsync(idExistente, requestDto))
            .ReturnsAsync(Result.Success<FunkoResponseDto, AppError>(responseDto));

        // ACT
        var resultado = await _controller.Update(idExistente, requestDto);

        // ASSERT
        Assert.That(resultado.Result, Is.InstanceOf<OkObjectResult>());
        
        var okResult = (OkObjectResult)resultado.Result!;
        var model = (FunkoResponseDto)okResult.Value!;

        Assert.That(model.Nombre, Is.EqualTo("Batman Actualizado"));
        Assert.That(model.Precio, Is.EqualTo(22.5m));
    }

    [Test]
    public async Task Update_CuandoNoExiste_RetornaNotFound()
    {
        // ARRANGE
        long idNoExistente = 99;
        var requestDto = new FunkoRequestDto("Funko", Guid.NewGuid(), 15.0m);
        var error = new NotFoundError("Funko no encontrado");

        _serviceMock.Setup(s => s.UpdateAsync(idNoExistente, requestDto))
            .ReturnsAsync(Result.Failure<FunkoResponseDto, AppError>(error));

        // ACT
        var resultado = await _controller.Update(idNoExistente, requestDto);

        // ASSERT
        Assert.That(resultado.Result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task Delete_CuandoExiste_RetornaNoContent()
    {
        // ARRANGE
        long idExistente = 1;
        
        _serviceMock.Setup(s => s.DeleteAsync(idExistente))
            .ReturnsAsync(Result.Success<FunkoResponseDto, AppError>(null!));

        // ACT
        var resultado = await _controller.Delete(idExistente);

        // ASSERT
        Assert.That(resultado, Is.InstanceOf<NoContentResult>());
        
        var noContentResult = (NoContentResult)resultado;
        Assert.That(noContentResult.StatusCode, Is.EqualTo(204));
        
        _serviceMock.Verify(s => s.DeleteAsync(idExistente), Times.Once);
    }

    [Test]
    public async Task Delete_CuandoNoExiste_RetornaNotFound()
    {
        // ARRANGE
        long idNoExistente = 99;
        var error = new NotFoundError("Funko no encontrado");

        _serviceMock.Setup(s => s.DeleteAsync(idNoExistente))
            .ReturnsAsync(Result.Failure<FunkoResponseDto, AppError>(error));

        // ACT
        var resultado = await _controller.Delete(idNoExistente);

        // ASSERT
        Assert.That(resultado, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task UpdateImage_CuandoArchivoEsNull_RetornaBadRequest()
    {
        // ARRANGE
        long idExistente = 1;

        // ACT
        var resultado = await _controller.UpdateImage(idExistente, null!);

        // ASSERT
        Assert.That(resultado.Result, Is.InstanceOf<BadRequestObjectResult>());
        
        var badRequestResult = (BadRequestObjectResult)resultado.Result!;
        Assert.That(badRequestResult.StatusCode, Is.EqualTo(400));
    }

    [Test]
    public async Task UpdateImage_CuandoArchivoVacio_RetornaBadRequest()
    {
        // ARRANGE
        long idExistente = 1;
        var fileMock = new Mock<Microsoft.AspNetCore.Http.IFormFile>();
        fileMock.Setup(f => f.Length).Returns(0); // Archivo vacío

        // ACT
        var resultado = await _controller.UpdateImage(idExistente, fileMock.Object);

        // ASSERT
        Assert.That(resultado.Result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task UpdateImage_CuandoEsValido_RetornaOkConFunkoActualizado()
    {
        // ARRANGE
        long idExistente = 1;
        var categoriaDto = new CategoriaResponseDto(Guid.NewGuid(), "Marvel");
        var responseDto = new FunkoResponseDto(
            idExistente,
            "Iron Man",
            categoriaDto,
            30.0m,
            "nueva-imagen.png",
            DateTime.UtcNow,
            DateTime.UtcNow
        );

        var fileMock = new Mock<Microsoft.AspNetCore.Http.IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("test.jpg");
        fileMock.Setup(f => f.Length).Returns(1024);

        _serviceMock.Setup(s => s.UpdateImageAsync(idExistente, fileMock.Object))
            .ReturnsAsync(Result.Success<FunkoResponseDto, AppError>(responseDto));

        // ACT
        var resultado = await _controller.UpdateImage(idExistente, fileMock.Object);

        // ASSERT
        Assert.That(resultado.Result, Is.InstanceOf<OkObjectResult>());
        
        var okResult = (OkObjectResult)resultado.Result!;
        var model = (FunkoResponseDto)okResult.Value!;

        Assert.That(model.Imagen, Is.EqualTo("nueva-imagen.png"));
    }

    [Test]
    public async Task UpdateImage_CuandoFunkoNoExiste_RetornaNotFound()
    {
        // ARRANGE
        long idNoExistente = 99;
        var error = new NotFoundError("Funko no encontrado");

        var fileMock = new Mock<Microsoft.AspNetCore.Http.IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("test.jpg");
        fileMock.Setup(f => f.Length).Returns(1024);

        _serviceMock.Setup(s => s.UpdateImageAsync(idNoExistente, fileMock.Object))
            .ReturnsAsync(Result.Failure<FunkoResponseDto, AppError>(error));

        // ACT
        var resultado = await _controller.UpdateImage(idNoExistente, fileMock.Object);

        // ASSERT
        Assert.That(resultado.Result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task Create_CuandoCategoriaNoExiste_RetornaBadRequest()
    {
        // ARRANGE
        var categoriaIdInvalido = Guid.NewGuid();
        var requestDto = new FunkoRequestDto("Batman", categoriaIdInvalido, 20.0m);
        var error = new BusinessRuleError($"Categoría {categoriaIdInvalido} no encontrada");
        
        _serviceMock.Setup(s => s.CreateAsync(requestDto))
            .ReturnsAsync(Result.Failure<FunkoResponseDto, AppError>(error));

        // ACT
        var resultado = await _controller.Create(requestDto);

        // ASSERT
        Assert.That(resultado.Result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task Update_CuandoCategoriaNoExiste_RetornaBadRequest()
    {
        // ARRANGE
        long idExistente = 1;
        var categoriaIdInvalido = Guid.NewGuid();
        var requestDto = new FunkoRequestDto("Batman", categoriaIdInvalido, 20.0m);
        var error = new BusinessRuleError($"Categoría {categoriaIdInvalido} no encontrada");

        _serviceMock.Setup(s => s.UpdateAsync(idExistente, requestDto))
            .ReturnsAsync(Result.Failure<FunkoResponseDto, AppError>(error));

        // ACT
        var resultado = await _controller.Update(idExistente, requestDto);

        // ASSERT
        Assert.That(resultado.Result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task HandleError_CuandoConflictError_RetornaConflict()
    {
        // ARRANGE
        long idExistente = 1;
        var requestDto = new FunkoRequestDto("Batman", Guid.NewGuid(), 20.0m);
        var error = new ConflictError("Conflicto");

        _serviceMock.Setup(s => s.UpdateAsync(idExistente, requestDto))
            .ReturnsAsync(Result.Failure<FunkoResponseDto, AppError>(error));

        // ACT
        var resultado = await _controller.Update(idExistente, requestDto);

        // ASSERT
        Assert.That(resultado.Result, Is.InstanceOf<ConflictObjectResult>());
        
        var conflictResult = (ConflictObjectResult)resultado.Result!;
        Assert.That(conflictResult.StatusCode, Is.EqualTo(409));
    }
}
