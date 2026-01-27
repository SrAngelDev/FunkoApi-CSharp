using FunkoApi.Auth;
using FunkoApi.Dtos;
using FunkoApi.Errors;
using FunkoApi.Models;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;
using CSharpFunctionalExtensions;
using FunkoApi.Services.Email;

namespace FunkoApi.Tests.Services;

[TestFixture]
public class AuthServiceTests
{
    private Mock<UserManager<User>> _userManagerMock;
    private TokenService _tokenService; // Ya no es mock, usamos instancia real
    private Mock<IValidator<RegisterDto>> _registerValidatorMock;
    private Mock<IValidator<LoginDto>> _loginValidatorMock;
    private AuthService _authService;
    private Mock<IEmailService> _emailServiceMock;

    [SetUp]
    public void SetUp()
    {
        // Mock completo de UserManager
        var userStoreMock = new Mock<IUserStore<User>>();
        _userManagerMock = new Mock<UserManager<User>>(
            userStoreMock.Object,
            null!, null!, null!, null!, null!, null!, null!, null!);

        // Configuración mockeada para TokenService
        var configurationMock = new Mock<IConfiguration>();
        configurationMock.Setup(c => c["Jwt:Key"]).Returns("ClaveSecretaSuperSeguraParaTestsUnitarios123!");
        configurationMock.Setup(c => c["Jwt:Issuer"]).Returns("FunkoApiTest");
        configurationMock.Setup(c => c["Jwt:Audience"]).Returns("FunkoClientTest");

        // TokenService real (no mock)
        _tokenService = new TokenService(configurationMock.Object);

        // Validadores mockeados
        _registerValidatorMock = new Mock<IValidator<RegisterDto>>();
        _loginValidatorMock = new Mock<IValidator<LoginDto>>();
        
        //Mock de servicio de email
        _emailServiceMock = new Mock<IEmailService>();

        // Crear el servicio con los mocks
        _authService = new AuthService(
            _userManagerMock.Object,
            _tokenService,
            _registerValidatorMock.Object,
            _loginValidatorMock.Object,
            _emailServiceMock.Object
        );
    }
    
    // TESTS DE REGISTRO

    [Test]
    public async Task RegisterAsync_CuandoDatosSonValidos_RetornaSuccess()
    {
        // ARRANGE
        var registerDto = new RegisterDto("testuser", "Password123", "test@example.com");
        
        _registerValidatorMock.Setup(v => v.ValidateAsync(registerDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        
        _userManagerMock.Setup(um => um.CreateAsync(It.IsAny<User>(), registerDto.Password))
            .ReturnsAsync(IdentityResult.Success);
        
        _userManagerMock.Setup(um => um.AddToRoleAsync(It.IsAny<User>(), Roles.User))
            .ReturnsAsync(IdentityResult.Success);

        // ACT
        var result = await _authService.RegisterAsync(registerDto);

        // ASSERT
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Is.EqualTo("Usuario registrado correctamente"));
        
        _userManagerMock.Verify(um => um.CreateAsync(
            It.Is<User>(u => u.UserName == "testuser" && u.Email == "test@example.com"),
            registerDto.Password), 
            Times.Once);
        
        _userManagerMock.Verify(um => um.AddToRoleAsync(It.IsAny<User>(), Roles.User), Times.Once);
    }

    [Test]
    public async Task RegisterAsync_CuandoValidacionFalla_RetornaFailure()
    {
        // ARRANGE
        var registerDto = new RegisterDto("", "123", "invalid-email");
        
        var validationFailures = new List<ValidationFailure>
        {
            new ValidationFailure("Username", "El nombre de usuario es obligatorio")
        };
        _registerValidatorMock.Setup(v => v.ValidateAsync(registerDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // ACT
        var result = await _authService.RegisterAsync(registerDto);

        // ASSERT
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Is.InstanceOf<BusinessRuleError>());
        Assert.That(result.Error.Message, Does.Contain("usuario es obligatorio"));
        
        _userManagerMock.Verify(um => um.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task RegisterAsync_CuandoUsuarioDuplicado_RetornaFailure()
    {
        // ARRANGE
        var registerDto = new RegisterDto("duplicateuser", "Password123", "duplicate@example.com");

        _registerValidatorMock.Setup(v => v.ValidateAsync(registerDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        
        var identityError = new IdentityError { Description = "El nombre de usuario ya existe" };
        _userManagerMock.Setup(um => um.CreateAsync(It.IsAny<User>(), registerDto.Password))
            .ReturnsAsync(IdentityResult.Failed(identityError));

        // ACT
        var result = await _authService.RegisterAsync(registerDto);

        // ASSERT
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Is.InstanceOf<BusinessRuleError>());
        Assert.That(result.Error.Message, Does.Contain("ya existe"));
        
        _userManagerMock.Verify(um => um.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task RegisterAsync_CuandoFallaAsignacionRol_RetornaFailure()
    {
        // ARRANGE
        var registerDto = new RegisterDto("testuser", "Password123", "test@example.com");

        _registerValidatorMock.Setup(v => v.ValidateAsync(registerDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _userManagerMock.Setup(um => um.CreateAsync(It.IsAny<User>(), registerDto.Password))
            .ReturnsAsync(IdentityResult.Success);
        
        var identityError = new IdentityError { Description = "Error al asignar rol" };
        _userManagerMock.Setup(um => um.AddToRoleAsync(It.IsAny<User>(), Roles.User))
            .ReturnsAsync(IdentityResult.Failed(identityError));

        // ACT
        var result = await _authService.RegisterAsync(registerDto);

        // ASSERT
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Is.InstanceOf<BusinessRuleError>());
        Assert.That(result.Error.Message, Does.Contain("rol"));
    }
    
    // TESTS DE LOGIN

    [Test]
    public async Task LoginAsync_CuandoCredencialesSonValidas_RetornaTokenJWT()
    {
        // ARRANGE
        var loginDto = new LoginDto("testuser", "Password123");
        var user = new User 
        { 
            Id = 1, 
            UserName = "testuser", 
            Email = "test@example.com",
            Nombre = "Test User",
            CreatedAt = DateTime.UtcNow
        };
        var roles = new List<string> { Roles.User };
        
        _loginValidatorMock.Setup(v => v.ValidateAsync(loginDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        
        _userManagerMock.Setup(um => um.FindByNameAsync(loginDto.Username))
            .ReturnsAsync(user);
        
        _userManagerMock.Setup(um => um.CheckPasswordAsync(user, loginDto.Password))
            .ReturnsAsync(true);
        
        _userManagerMock.Setup(um => um.GetRolesAsync(user))
            .ReturnsAsync(roles);

        // ACT
        var result = await _authService.LoginAsync(loginDto);

        // ASSERT
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value, Is.Not.Empty);
        Assert.That(result.Value.Split('.').Length, Is.EqualTo(3)); 
    }

    [Test]
    public async Task LoginAsync_CuandoValidacionFalla_RetornaFailure()
    {
        // ARRANGE
        var loginDto = new LoginDto("", "");
        
        var validationFailures = new List<ValidationFailure>
        {
            new ValidationFailure("Username", "El usuario no puede estar vacío")
        };
        _loginValidatorMock.Setup(v => v.ValidateAsync(loginDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // ACT
        var result = await _authService.LoginAsync(loginDto);

        // ASSERT
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Is.InstanceOf<BusinessRuleError>());
        
        _userManagerMock.Verify(um => um.FindByNameAsync(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task LoginAsync_CuandoUsuarioNoExiste_RetornaFailure()
    {
        // ARRANGE
        var loginDto = new LoginDto("nonexistent", "Password123");

        _loginValidatorMock.Setup(v => v.ValidateAsync(loginDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        
        _userManagerMock.Setup(um => um.FindByNameAsync(loginDto.Username))
            .ReturnsAsync((User?)null);

        // ACT
        var result = await _authService.LoginAsync(loginDto);

        // ASSERT
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Is.InstanceOf<BusinessRuleError>());
        Assert.That(result.Error.Message, Does.Contain("Usuario o contraseña incorrectos"));
    }

    [Test]
    public async Task LoginAsync_CuandoContrasenaIncorrecta_RetornaFailure()
    {
        // ARRANGE
        var loginDto = new LoginDto("testuser", "WrongPassword");
        var user = new User 
        { 
            Id = 1, 
            UserName = "testuser", 
            Email = "test@example.com"
        };

        _loginValidatorMock.Setup(v => v.ValidateAsync(loginDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _userManagerMock.Setup(um => um.FindByNameAsync(loginDto.Username))
            .ReturnsAsync(user);
        
        _userManagerMock.Setup(um => um.CheckPasswordAsync(user, loginDto.Password))
            .ReturnsAsync(false);

        // ACT
        var result = await _authService.LoginAsync(loginDto);

        // ASSERT
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Is.InstanceOf<BusinessRuleError>());
        Assert.That(result.Error.Message, Does.Contain("Usuario o contraseña incorrectos"));
        
        _userManagerMock.Verify(um => um.GetRolesAsync(It.IsAny<User>()), Times.Never);
    }
}
