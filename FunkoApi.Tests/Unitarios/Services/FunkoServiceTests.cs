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

using Microsoft.AspNetCore.SignalR;
using FunkoApi.WebSockets;
using Microsoft.AspNetCore.Http;

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
    private Mock<IHubContext<FunkoHub>> _hubContextMock;
    private Mock<IHubClients> _hubClientsMock;
    private Mock<IClientProxy> _clientProxyMock;
    

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
        _hubContextMock = new Mock<IHubContext<FunkoHub>>();
        _hubClientsMock = new Mock<IHubClients>();
        _clientProxyMock = new Mock<IClientProxy>();

        // Configuración de SignalR Mock
        _hubContextMock.Setup(h => h.Clients).Returns(_hubClientsMock.Object);
        _hubClientsMock.Setup(c => c.All).Returns(_clientProxyMock.Object);

        // 2. Inyectamos los Mocks en el constructor del servicio real
        _service = new FunkoService(
            _repositoryMock.Object,
            _categoriaRepositoryMock.Object,
            _cacheMock.Object,
            _validatorMock.Object,
            _storageMock.Object,
            _loggerMock.Object,
            _hubContextMock.Object
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

        _clientProxyMock.Verify(
            c => c.SendCoreAsync(
                "FunkoCreated",
                It.Is<object[]>(o => o.Length == 1 && ((FunkoResponseDto)o[0]).Nombre == "Batman"),
                It.IsAny<CancellationToken>()),
            Times.Once);
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

        _clientProxyMock.Verify(
            c => c.SendCoreAsync(
                "FunkoDeleted",
                It.Is<object[]>(o => o.Length == 1 && (long)o[0] == idExistente),
                It.IsAny<CancellationToken>()),
            Times.Once);
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

    [Test]
    public async Task UpdateAsync_CuandoDatosSonValidos_RetornaSuccess()
    {
        // ARRANGE
        long idExistente = 1;
        var categoriaId = Guid.NewGuid();
        var dto = new FunkoRequestDto("Batman Updated", categoriaId, 25.0m);
        var categoria = new Categoria { Id = categoriaId, Nombre = "DC" };
        var funkoExistente = new Funko
        {
            Id = idExistente,
            Nombre = "Batman",
            Precio = 20.0m,
            CategoriaId = categoriaId,
            Categoria = categoria,
            Imagen = "batman.png",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var funkoActualizado = new Funko
        {
            Id = idExistente,
            Nombre = "Batman Updated",
            Precio = 25.0m,
            CategoriaId = categoriaId,
            Categoria = categoria,
            Imagen = "batman.png",
            CreatedAt = funkoExistente.CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };

        _validatorMock.Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _repositoryMock.Setup(r => r.GetByIdAsync(idExistente))
            .ReturnsAsync(funkoExistente);

        _categoriaRepositoryMock.Setup(r => r.GetByIdAsync(categoriaId))
            .ReturnsAsync(categoria);

        _repositoryMock.Setup(r => r.UpdateAsync(idExistente, It.IsAny<Funko>()))
            .ReturnsAsync(funkoActualizado);

        _cacheMock.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // ACT
        var result = await _service.UpdateAsync(idExistente, dto);

        // ASSERT
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Nombre, Is.EqualTo("Batman Updated"));
        Assert.That(result.Value.Precio, Is.EqualTo(25.0m));
        _repositoryMock.Verify(r => r.UpdateAsync(idExistente, It.IsAny<Funko>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        _clientProxyMock.Verify(
            c => c.SendCoreAsync(
                "FunkoUpdated",
                It.Is<object[]>(o => o.Length == 1),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task UpdateAsync_CuandoNoExiste_RetornaNotFound()
    {
        // ARRANGE
        long idNoExistente = 99;
        var dto = new FunkoRequestDto("Batman", Guid.NewGuid(), 20.0m);

        _validatorMock.Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _repositoryMock.Setup(r => r.GetByIdAsync(idNoExistente))
            .ReturnsAsync((Funko?)null);

        // ACT
        var result = await _service.UpdateAsync(idNoExistente, dto);

        // ASSERT
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Is.InstanceOf<NotFoundError>());
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<long>(), It.IsAny<Funko>()), Times.Never);
    }

    [Test]
    public async Task UpdateAsync_CuandoValidacionFalla_RetornaFailure()
    {
        // ARRANGE
        long idExistente = 1;
        var dto = new FunkoRequestDto("", Guid.Empty, -5);

        var validationFailures = new List<ValidationFailure>
        {
            new ValidationFailure("Nombre", "El nombre es requerido"),
            new ValidationFailure("Precio", "El precio debe ser positivo")
        };

        _validatorMock.Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // ACT
        var result = await _service.UpdateAsync(idExistente, dto);

        // ASSERT
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Is.InstanceOf<BusinessRuleError>());
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<long>(), It.IsAny<Funko>()), Times.Never);
    }

    [Test]
    public async Task UpdateAsync_CuandoCategoriaNoExiste_RetornaBusinessRuleError()
    {
        // ARRANGE
        long idExistente = 1;
        var categoriaIdInvalido = Guid.NewGuid();
        var dto = new FunkoRequestDto("Batman", categoriaIdInvalido, 20.0m);
        var funkoExistente = new Funko
        {
            Id = idExistente,
            Nombre = "Batman",
            Precio = 20.0m,
            CategoriaId = Guid.NewGuid(),
            Imagen = "batman.png",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _validatorMock.Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _repositoryMock.Setup(r => r.GetByIdAsync(idExistente))
            .ReturnsAsync(funkoExistente);

        _categoriaRepositoryMock.Setup(r => r.GetByIdAsync(categoriaIdInvalido))
            .ReturnsAsync((Categoria?)null);

        // ACT
        var result = await _service.UpdateAsync(idExistente, dto);

        // ASSERT
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Is.InstanceOf<BusinessRuleError>());
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<long>(), It.IsAny<Funko>()), Times.Never);
    }

    [Test]
    public async Task UpdateImageAsync_CuandoDatosSonValidos_RetornaSuccess()
    {
        // ARRANGE
        long idExistente = 1;
        var categoriaId = Guid.NewGuid();
        var categoria = new Categoria { Id = categoriaId, Nombre = "Marvel" };
        var funkoExistente = new Funko
        {
            Id = idExistente,
            Nombre = "Iron Man",
            Precio = 30.0m,
            CategoriaId = categoriaId,
            Categoria = categoria,
            Imagen = "old-image.png",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var funkoActualizado = new Funko
        {
            Id = idExistente,
            Nombre = "Iron Man",
            Precio = 30.0m,
            CategoriaId = categoriaId,
            Categoria = categoria,
            Imagen = "new-image.png",
            CreatedAt = funkoExistente.CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };

        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("test.jpg");
        fileMock.Setup(f => f.Length).Returns(1024);

        _repositoryMock.Setup(r => r.GetByIdAsync(idExistente))
            .ReturnsAsync(funkoExistente);

        _storageMock.Setup(s => s.SaveFileAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync("new-image.png");

        _storageMock.Setup(s => s.DeleteFile(It.IsAny<string>()));

        _repositoryMock.Setup(r => r.UpdateAsync(idExistente, It.IsAny<Funko>()))
            .ReturnsAsync(funkoActualizado);

        _cacheMock.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // ACT
        var result = await _service.UpdateImageAsync(idExistente, fileMock.Object);

        // ASSERT
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Imagen, Is.EqualTo("new-image.png"));
        _storageMock.Verify(s => s.SaveFileAsync(It.IsAny<IFormFile>()), Times.Once);
        _storageMock.Verify(s => s.DeleteFile("old-image.png"), Times.Once);
        _repositoryMock.Verify(r => r.UpdateAsync(idExistente, It.IsAny<Funko>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Test]
    public async Task UpdateImageAsync_CuandoFunkoNoExiste_RetornaNotFound()
    {
        // ARRANGE
        long idNoExistente = 99;
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("test.jpg");
        fileMock.Setup(f => f.Length).Returns(1024);

        _repositoryMock.Setup(r => r.GetByIdAsync(idNoExistente))
            .ReturnsAsync((Funko?)null);

        // ACT
        var result = await _service.UpdateImageAsync(idNoExistente, fileMock.Object);

        // ASSERT
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Is.InstanceOf<NotFoundError>());
        _storageMock.Verify(s => s.SaveFileAsync(It.IsAny<IFormFile>()), Times.Never);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<long>(), It.IsAny<Funko>()), Times.Never);
    }

    [Test]
    public async Task UpdateImageAsync_CuandoErrorAlSubirImagen_RetornaBusinessRuleError()
    {
        // ARRANGE
        long idExistente = 1;
        var categoriaId = Guid.NewGuid();
        var categoria = new Categoria { Id = categoriaId, Nombre = "Marvel" };
        var funkoExistente = new Funko
        {
            Id = idExistente,
            Nombre = "Iron Man",
            Precio = 30.0m,
            CategoriaId = categoriaId,
            Categoria = categoria,
            Imagen = "old-image.png",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("test.jpg");
        fileMock.Setup(f => f.Length).Returns(1024);

        _repositoryMock.Setup(r => r.GetByIdAsync(idExistente))
            .ReturnsAsync(funkoExistente);

        _storageMock.Setup(s => s.SaveFileAsync(It.IsAny<IFormFile>()))
            .ThrowsAsync(new Exception("Error de almacenamiento"));

        // ACT
        var result = await _service.UpdateImageAsync(idExistente, fileMock.Object);

        // ASSERT
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Is.InstanceOf<BusinessRuleError>());
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<long>(), It.IsAny<Funko>()), Times.Never);
    }

    [Test]
    public async Task GetByIdAsync_CuandoEstaCacheado_NoConsultaRepositorio()
    {
        // ARRANGE
        long idExistente = 1;
        var categoriaDto = new CategoriaResponseDto(Guid.NewGuid(), "Marvel");
        var funkoEnCache = new FunkoResponseDto(idExistente, "Iron Man", categoriaDto, 30.0m, "ironman.png", DateTime.UtcNow, DateTime.UtcNow);
        
        var jsonCache = System.Text.Json.JsonSerializer.Serialize(funkoEnCache);
        var bytesCache = System.Text.Encoding.UTF8.GetBytes(jsonCache);
        
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(bytesCache);

        // ACT
        var result = await _service.GetByIdAsync(idExistente);

        // ASSERT
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Nombre, Is.EqualTo("Iron Man"));
        _repositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<long>()), Times.Never);
    }

    [Test]
    public async Task CreateAsync_CuandoCategoriaNoExiste_RetornaBusinessRuleError()
    {
        // ARRANGE
        var categoriaIdInvalido = Guid.NewGuid();
        var dto = new FunkoRequestDto("Batman", categoriaIdInvalido, 20.5m);
        
        _validatorMock.Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
             .ReturnsAsync(new ValidationResult());
        
        _categoriaRepositoryMock.Setup(r => r.GetByIdAsync(categoriaIdInvalido))
            .ReturnsAsync((Categoria?)null);

        // ACT
        var result = await _service.CreateAsync(dto);

        // ASSERT
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Is.InstanceOf<BusinessRuleError>());
        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<Funko>()), Times.Never);
    }

    [Test]
    public async Task UpdateImageAsync_CuandoImagenNoEsPorDefecto_EliminaImagenAntigua()
    {
        // ARRANGE
        long idExistente = 1;
        var categoriaId = Guid.NewGuid();
        var categoria = new Categoria { Id = categoriaId, Nombre = "Marvel" };
        var funkoExistente = new Funko
        {
            Id = idExistente,
            Nombre = "Iron Man",
            Precio = 30.0m,
            CategoriaId = categoriaId,
            Categoria = categoria,
            Imagen = "old-local-image.png", // Imagen local, no URL
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var funkoActualizado = new Funko
        {
            Id = idExistente,
            Nombre = "Iron Man",
            Precio = 30.0m,
            CategoriaId = categoriaId,
            Categoria = categoria,
            Imagen = "new-image.png",
            CreatedAt = funkoExistente.CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };

        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("test.jpg");
        fileMock.Setup(f => f.Length).Returns(1024);

        _repositoryMock.Setup(r => r.GetByIdAsync(idExistente))
            .ReturnsAsync(funkoExistente);

        _storageMock.Setup(s => s.SaveFileAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync("new-image.png");

        _storageMock.Setup(s => s.DeleteFile(It.IsAny<string>()));

        _repositoryMock.Setup(r => r.UpdateAsync(idExistente, It.IsAny<Funko>()))
            .ReturnsAsync(funkoActualizado);

        _cacheMock.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // ACT
        var result = await _service.UpdateImageAsync(idExistente, fileMock.Object);

        // ASSERT
        Assert.That(result.IsSuccess, Is.True);
        _storageMock.Verify(s => s.DeleteFile("old-local-image.png"), Times.Once);
    }

    [Test]
    public async Task UpdateAsync_CuandoRepositorioRetornaNull_RetornaNotFound()
    {
        // ARRANGE
        long idExistente = 1;
        var categoriaId = Guid.NewGuid();
        var categoria = new Categoria { Id = categoriaId, Nombre = "Marvel" };
        var funkoExistente = new Funko
        {
            Id = idExistente,
            Nombre = "Iron Man",
            Precio = 30.0m,
            CategoriaId = categoriaId,
            Categoria = categoria,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var dto = new FunkoRequestDto("Iron Man Actualizado", categoriaId, 35.0m);

        _validatorMock.Setup(v => v.ValidateAsync(It.IsAny<FunkoRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _repositoryMock.Setup(r => r.GetByIdAsync(idExistente))
            .ReturnsAsync(funkoExistente);

        _categoriaRepositoryMock.Setup(r => r.GetByIdAsync(categoriaId))
            .ReturnsAsync(categoria);

        // Simular que UpdateAsync del repositorio retorna null
        _repositoryMock.Setup(r => r.UpdateAsync(idExistente, It.IsAny<Funko>()))
            .ReturnsAsync((Funko?)null);

        // ACT
        var result = await _service.UpdateAsync(idExistente, dto);

        // ASSERT
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Is.InstanceOf<NotFoundError>());
    }

    [Test]
    public async Task UpdateImageAsync_CuandoImagenEsHttps_NoEliminaImagenAntigua()
    {
        // ARRANGE
        long idExistente = 1;
        var categoriaId = Guid.NewGuid();
        var categoria = new Categoria { Id = categoriaId, Nombre = "Marvel" };
        var funkoExistente = new Funko
        {
            Id = idExistente,
            Nombre = "Iron Man",
            Precio = 30.0m,
            CategoriaId = categoriaId,
            Categoria = categoria,
            Imagen = "https://example.com/image.png", // Imagen externa (URL)
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var funkoActualizado = new Funko
        {
            Id = idExistente,
            Nombre = "Iron Man",
            Precio = 30.0m,
            CategoriaId = categoriaId,
            Categoria = categoria,
            Imagen = "new-image.png",
            CreatedAt = funkoExistente.CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };

        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("test.jpg");
        fileMock.Setup(f => f.Length).Returns(1024);

        _repositoryMock.Setup(r => r.GetByIdAsync(idExistente))
            .ReturnsAsync(funkoExistente);

        _storageMock.Setup(s => s.SaveFileAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync("new-image.png");

        _repositoryMock.Setup(r => r.UpdateAsync(idExistente, It.IsAny<Funko>()))
            .ReturnsAsync(funkoActualizado);

        _cacheMock.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // ACT
        var result = await _service.UpdateImageAsync(idExistente, fileMock.Object);

        // ASSERT
        Assert.That(result.IsSuccess, Is.True);
        // Verificar que NO se llamó a DeleteFile porque la imagen era una URL
        _storageMock.Verify(s => s.DeleteFile(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task UpdateImageAsync_CuandoImagenEsNulaOVacia_NoEliminaImagenAntigua()
    {
        // ARRANGE
        long idExistente = 1;
        var categoriaId = Guid.NewGuid();
        var categoria = new Categoria { Id = categoriaId, Nombre = "Marvel" };
        var funkoExistente = new Funko
        {
            Id = idExistente,
            Nombre = "Iron Man",
            Precio = 30.0m,
            CategoriaId = categoriaId,
            Categoria = categoria,
            Imagen = null!, // Sin imagen anterior
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var funkoActualizado = new Funko
        {
            Id = idExistente,
            Nombre = "Iron Man",
            Precio = 30.0m,
            CategoriaId = categoriaId,
            Categoria = categoria,
            Imagen = "new-image.png",
            CreatedAt = funkoExistente.CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };

        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("test.jpg");
        fileMock.Setup(f => f.Length).Returns(1024);

        _repositoryMock.Setup(r => r.GetByIdAsync(idExistente))
            .ReturnsAsync(funkoExistente);

        _storageMock.Setup(s => s.SaveFileAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync("new-image.png");

        _repositoryMock.Setup(r => r.UpdateAsync(idExistente, It.IsAny<Funko>()))
            .ReturnsAsync(funkoActualizado);

        _cacheMock.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // ACT
        var result = await _service.UpdateImageAsync(idExistente, fileMock.Object);

        // ASSERT
        Assert.That(result.IsSuccess, Is.True);
        // Verificar que NO se llamó a DeleteFile porque no había imagen anterior
        _storageMock.Verify(s => s.DeleteFile(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task UpdateImageAsync_CuandoImagenEsVacia_NoEliminaImagenAntigua()
    {
        // ARRANGE
        long idExistente = 1;
        var categoriaId = Guid.NewGuid();
        var categoria = new Categoria { Id = categoriaId, Nombre = "Marvel" };
        var funkoExistente = new Funko
        {
            Id = idExistente,
            Nombre = "Iron Man",
            Precio = 30.0m,
            CategoriaId = categoriaId,
            Categoria = categoria,
            Imagen = "", // Imagen vacía
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var funkoActualizado = new Funko
        {
            Id = idExistente,
            Nombre = "Iron Man",
            Precio = 30.0m,
            CategoriaId = categoriaId,
            Categoria = categoria,
            Imagen = "new-image.png",
            CreatedAt = funkoExistente.CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };

        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("test.jpg");
        fileMock.Setup(f => f.Length).Returns(1024);

        _repositoryMock.Setup(r => r.GetByIdAsync(idExistente))
            .ReturnsAsync(funkoExistente);

        _storageMock.Setup(s => s.SaveFileAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync("new-image.png");

        _repositoryMock.Setup(r => r.UpdateAsync(idExistente, It.IsAny<Funko>()))
            .ReturnsAsync(funkoActualizado);

        _cacheMock.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // ACT
        var result = await _service.UpdateImageAsync(idExistente, fileMock.Object);

        // ASSERT
        Assert.That(result.IsSuccess, Is.True);
        // Verificar que NO se llamó a DeleteFile porque la imagen estaba vacía
        _storageMock.Verify(s => s.DeleteFile(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task GetAllAsync_CuandoCacheEstaVacio_ConsultaBaseDeDatos()
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
            },
            new Funko 
            { 
                Id = 2, 
                Nombre = "Superman", 
                Precio = 25.0m, 
                CategoriaId = categoriaId,
                Categoria = categoria,
                Imagen = "superman.png",
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
        Assert.That(result.Count(), Is.EqualTo(2));
        _repositoryMock.Verify(r => r.GetAllAsync(), Times.Once);
        _cacheMock.Verify(c => c.SetAsync(
            It.IsAny<string>(), 
            It.IsAny<byte[]>(), 
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Test]
    public async Task DeleteAsync_InvalidaCacheDespuesDeEliminar()
    {
        // ARRANGE
        long idExistente = 1;
        var categoriaId = Guid.NewGuid();
        var categoria = new Categoria { Id = categoriaId, Nombre = "DC" };
        var funko = new Funko
        {
            Id = idExistente,
            Nombre = "Superman",
            Precio = 30.0m,
            CategoriaId = categoriaId,
            Categoria = categoria,
            Imagen = "superman.png",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        _repositoryMock.Setup(r => r.DeleteAsync(idExistente))
            .ReturnsAsync(funko);
        
        _cacheMock.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // ACT
        var result = await _service.DeleteAsync(idExistente);

        // ASSERT
        Assert.That(result.IsSuccess, Is.True);
        _cacheMock.Verify(
            c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), 
            Times.Exactly(2)); // Una para CacheKeyAll y otra para GetKey(id)
    }

    [Test]
    public async Task UpdateAsync_InvalidaCacheDespuesDeActualizar()
    {
        // ARRANGE
        long idExistente = 1;
        var categoriaId = Guid.NewGuid();
        var dto = new FunkoRequestDto("Batman Updated", categoriaId, 25.0m);
        var categoria = new Categoria { Id = categoriaId, Nombre = "DC" };
        var funkoExistente = new Funko
        {
            Id = idExistente,
            Nombre = "Batman",
            Precio = 20.0m,
            CategoriaId = categoriaId,
            Categoria = categoria,
            Imagen = "batman.png",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var funkoActualizado = new Funko
        {
            Id = idExistente,
            Nombre = "Batman Updated",
            Precio = 25.0m,
            CategoriaId = categoriaId,
            Categoria = categoria,
            Imagen = "batman.png",
            CreatedAt = funkoExistente.CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };

        _validatorMock.Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _repositoryMock.Setup(r => r.GetByIdAsync(idExistente))
            .ReturnsAsync(funkoExistente);

        _categoriaRepositoryMock.Setup(r => r.GetByIdAsync(categoriaId))
            .ReturnsAsync(categoria);

        _repositoryMock.Setup(r => r.UpdateAsync(idExistente, It.IsAny<Funko>()))
            .ReturnsAsync(funkoActualizado);

        _cacheMock.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // ACT
        var result = await _service.UpdateAsync(idExistente, dto);

        // ASSERT
        Assert.That(result.IsSuccess, Is.True);
        _cacheMock.Verify(
            c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), 
            Times.Exactly(2)); // Una para CacheKeyAll y otra para GetKey(id)
    }
}
