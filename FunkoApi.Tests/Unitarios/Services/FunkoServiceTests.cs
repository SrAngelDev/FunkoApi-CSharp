using FunkoApi.Models;
using FunkoApi.Dtos;
using FunkoApi.Repositories.Funkos;
using FunkoApi.Repositories.Categorias;
using FunkoApi.Services.Funkos;
using FunkoApi.Storage;
using FunkoApi.Errors;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using FluentValidation;
using FluentValidation.Results;
using CSharpFunctionalExtensions;

namespace FunkoApi.Tests.Services;

[TestFixture]
public class FunkoServiceTests
{
    // Mocks de las dependencias
    private Mock<IFunkoRepository> _repositoryMock;
    private Mock<ICategoriaRepository> _categoriaRepositoryMock;
    private Mock<IDistributedCache> _cacheMock;
    private Mock<IStorageService> _storageMock;
    private Mock<IValidator<FunkoRequestDto>> _validatorMock;
    private Mock<ILogger<FunkoService>> _loggerMock;

    // El servicio real que vamos a probar
    private FunkoService _service;

    [SetUp]
    public void SetUp()
    {
        // 1. Inicializamos los Mocks
        _repositoryMock = new Mock<IFunkoRepository>();
        _categoriaRepositoryMock = new Mock<ICategoriaRepository>();
        _cacheMock = new Mock<IDistributedCache>();
        _storageMock = new Mock<IStorageService>();
        _validatorMock = new Mock<IValidator<FunkoRequestDto>>();
        _loggerMock = new Mock<ILogger<FunkoService>>();

        // 2. Inyectamos los Mocks en el constructor del servicio real
        _service = new FunkoService(
            _repositoryMock.Object,
            _categoriaRepositoryMock.Object,
            _cacheMock.Object,
            _validatorMock.Object,
            _storageMock.Object,
            _loggerMock.Object
        );
    }

    [Test]
    public async Task CreateAsync_CuandoDatosSonValidos_RetornaSuccess()
    {
        // ARRANGE
        var categoriaId = Guid.NewGuid();
        var dto = new FunkoRequestDto("Batman", categoriaId, 20.5m);
        var categoria = new Categoria { Id = categoriaId, Nombre = "DC" };
        var funkoGuardado = new Funko 
        { 
            Id = 1, 
            Nombre = "Batman", 
            Precio = 20.5m,
            CategoriaId = categoriaId,
            Categoria = categoria,
            Imagen = "https://placehold.co/600x400.png",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        _validatorMock.Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
             .ReturnsAsync(new ValidationResult());
        
        _categoriaRepositoryMock.Setup(r => r.GetByIdAsync(categoriaId))
            .ReturnsAsync(categoria);
        
        _repositoryMock.Setup(r => r.CreateAsync(It.IsAny<Funko>()))
             .ReturnsAsync(funkoGuardado);

        _cacheMock.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // ACT
        var result = await _service.CreateAsync(dto);

        // ASSERT
        Assert.That(result.IsSuccess, Is.True); // Verificamos ROP
        Assert.That(result.Value.Nombre, Is.EqualTo("Batman"));
        Assert.That(result.Value.Precio, Is.EqualTo(20.5m));
        
        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<Funko>()), Times.Once);
        
        _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CreateAsync_CuandoValidacionFalla_RetornaFailure()
    {
        // ARRANGE
        var dto = new FunkoRequestDto("", Guid.Empty, -5); // Datos inválidos
        
        var validationFailures = new List<ValidationFailure>
        {
            new ValidationFailure("Nombre", "El nombre es requerido"),
            new ValidationFailure("Precio", "El precio debe ser positivo")
        };
        _validatorMock.Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
             .ReturnsAsync(new ValidationResult(validationFailures));

        // ACT
        var result = await _service.CreateAsync(dto);

        // ASSERT
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Is.InstanceOf<BusinessRuleError>());
        
        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<Funko>()), Times.Never);
    }

    [Test]
    public async Task GetByIdAsync_CuandoExiste_RetornaFunko()
    {
        // ARRANGE
        long idExistente = 1;
        var categoriaId = Guid.NewGuid();
        var categoria = new Categoria { Id = categoriaId, Nombre = "Marvel" };
        var funko = new Funko
        {
            Id = idExistente,
            Nombre = "Spider-Man",
            Precio = 25.0m,
            CategoriaId = categoriaId,
            Categoria = categoria,
            Imagen = "spiderman.png",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);
        
        _repositoryMock.Setup(r => r.GetByIdAsync(idExistente))
             .ReturnsAsync(funko);
        
        _cacheMock.Setup(c => c.SetAsync(
            It.IsAny<string>(), 
            It.IsAny<byte[]>(), 
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // ACT
        var result = await _service.GetByIdAsync(idExistente);

        // ASSERT
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Nombre, Is.EqualTo("Spider-Man"));
        Assert.That(result.Value.Id, Is.EqualTo(idExistente));
        
        _repositoryMock.Verify(r => r.GetByIdAsync(idExistente), Times.Once);
        
        _cacheMock.Verify(c => c.SetAsync(
            It.IsAny<string>(), 
            It.IsAny<byte[]>(), 
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Test]
    public async Task GetByIdAsync_CuandoNoExiste_RetornaNotFound()
    {
        // ARRANGE
        long idNoExistente = 99;
        
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);
        
        _repositoryMock.Setup(r => r.GetByIdAsync(idNoExistente))
             .ReturnsAsync((Funko?)null);

        // ACT
        var result = await _service.GetByIdAsync(idNoExistente);

        // ASSERT
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Is.InstanceOf<NotFoundError>());
        
        _repositoryMock.Verify(r => r.GetByIdAsync(idNoExistente), Times.Once);
    }

    [Test]
    public async Task DeleteAsync_CuandoExiste_RetornaSuccess()
    {
        // ARRANGE
        long idExistente = 1;
        var categoriaId = Guid.NewGuid();
        var funko = new Funko
        {
            Id = idExistente,
            Nombre = "Superman",
            Precio = 30.0m,
            CategoriaId = categoriaId,
            Imagen = "superman.png",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        _repositoryMock.Setup(r => r.GetByIdAsync(idExistente))
            .ReturnsAsync(funko);
        
        _repositoryMock.Setup(r => r.DeleteAsync(idExistente))
            .ReturnsAsync(funko);
        
        _storageMock.Setup(s => s.DeleteFile(It.IsAny<string>()));
        
        _cacheMock.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // ACT
        var result = await _service.DeleteAsync(idExistente);

        // ASSERT
        Assert.That(result.IsSuccess, Is.True);
        
        _repositoryMock.Verify(r => r.DeleteAsync(idExistente), Times.Once);
        
        _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Test]
    public async Task DeleteAsync_CuandoNoExiste_RetornaNotFound()
    {
        // ARRANGE
        long idNoExistente = 99;
        
        _repositoryMock.Setup(r => r.DeleteAsync(idNoExistente))
            .ReturnsAsync((Funko?)null);

        // ACT
        var result = await _service.DeleteAsync(idNoExistente);

        // ASSERT
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Is.InstanceOf<NotFoundError>());
        
        _repositoryMock.Verify(r => r.DeleteAsync(idNoExistente), Times.Once);
    }

    [Test]
    public async Task GetAllAsync_CuandoCacheHit_NoConsultaRepositorio()
    {
        // ARRANGE
        var categoriaDto = new CategoriaResponseDto(Guid.NewGuid(), "Marvel");
        var funkosEnCache = new List<FunkoResponseDto>
        {
            new FunkoResponseDto(1, "Iron Man", categoriaDto, 30.0m, "ironman.png", DateTime.UtcNow, DateTime.UtcNow),
            new FunkoResponseDto(2, "Thor", categoriaDto, 35.0m, "thor.png", DateTime.UtcNow, DateTime.UtcNow)
        };
        
        var jsonCache = System.Text.Json.JsonSerializer.Serialize(funkosEnCache);
        var bytesCache = System.Text.Encoding.UTF8.GetBytes(jsonCache);
        
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(bytesCache);

        // ACT
        var result = await _service.GetAllAsync();

        // ASSERT
        Assert.That(result.Count(), Is.EqualTo(2));
        Assert.That(result.First().Nombre, Is.EqualTo("Iron Man"));
        
        _repositoryMock.Verify(r => r.GetAllAsync(), Times.Never);
    }

    [Test]
    public async Task GetAllAsync_CuandoCacheMiss_ConsultaRepositorioYGuardaEnCache()
    {
        // ARRANGE
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        var categoriaId = Guid.NewGuid();
        var categoria = new Categoria { Id = categoriaId, Nombre = "DC" };
        var funkos = new List<Funko>
        {
            new Funko 
            { 
                Id = 1, 
                Nombre = "Batman", 
                Precio = 20.0m, 
                CategoriaId = categoriaId,
                Categoria = categoria,
                Imagen = "batman.png",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };
        
        _repositoryMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(funkos);
        
        _cacheMock.Setup(c => c.SetAsync(
            It.IsAny<string>(), 
            It.IsAny<byte[]>(), 
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // ACT
        var result = await _service.GetAllAsync();

        // ASSERT
        Assert.That(result.Count(), Is.EqualTo(1));
        Assert.That(result.First().Nombre, Is.EqualTo("Batman"));
        
        _repositoryMock.Verify(r => r.GetAllAsync(), Times.Once);
        
        _cacheMock.Verify(c => c.SetAsync(
            It.IsAny<string>(), 
            It.IsAny<byte[]>(), 
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Test]
    public async Task CreateAsync_InvalidaCacheAlCrear()
    {
        // ARRANGE
        var categoriaId = Guid.NewGuid();
        var dto = new FunkoRequestDto("Wolverine", categoriaId, 28.0m);
        var categoria = new Categoria { Id = categoriaId, Nombre = "Marvel" };
        var funkoGuardado = new Funko 
        { 
            Id = 1, 
            Nombre = "Wolverine", 
            Precio = 28.0m,
            CategoriaId = categoriaId,
            Categoria = categoria,
            Imagen = "wolverine.png",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _validatorMock.Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
             .ReturnsAsync(new ValidationResult());

        _categoriaRepositoryMock.Setup(r => r.GetByIdAsync(categoriaId))
            .ReturnsAsync(categoria);

        _repositoryMock.Setup(r => r.CreateAsync(It.IsAny<Funko>()))
             .ReturnsAsync(funkoGuardado);

        _cacheMock.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // ACT
        var result = await _service.CreateAsync(dto);

        // ASSERT
        Assert.That(result.IsSuccess, Is.True);
        
        _cacheMock.Verify(c => c.RemoveAsync(
            It.IsAny<string>(), 
            It.IsAny<CancellationToken>()), 
            Times.AtLeastOnce);
    }
}
