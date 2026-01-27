using System.Net;
using System.Net.Http.Json;
using FunkoApi.Dtos;
using NUnit.Framework;

namespace FunkoApi.Tests.Integration.Auth;

[TestFixture]
public class AuthControllerIntegrationTests
{
    private CustomWebApplicationFactory _factory;
    private HttpClient _client;

    [SetUp]
    public void Setup()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [Test]
    public async Task Login_WithValidCredentials_ReturnsOkAndToken()
    {
        // Arrange
        var loginDto = new LoginDto("admin", "Admin123");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Token, Is.Not.Null.And.Not.Empty);
        
        // Verificar que el token es un JWT válido (tiene 3 partes separadas por puntos)
        var tokenParts = result.Token.Split('.');
        Assert.That(tokenParts.Length, Is.EqualTo(3));
    }

    [Test]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        // Arrange
        var loginDto = new LoginDto("admin", "WrongPassword");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Login_WithNonExistentUser_ReturnsUnauthorized()
    {
        // Arrange
        var loginDto = new LoginDto("nonexistent", "Password123");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Login_WithEmptyCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var loginDto = new LoginDto("", "");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Register_WithValidData_ReturnsOk()
    {
        // Arrange
        var registerDto = new RegisterDto("newuser", "NewUser123", "newuser@test.com");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadAsStringAsync();
        Assert.That(result, Does.Contain("registrado correctamente"));
    }

    [Test]
    public async Task Register_WithDuplicateUsername_ReturnsBadRequest()
    {
        // Arrange: Intentamos registrar un usuario que ya existe (admin)
        var registerDto = new RegisterDto("admin", "Password123", "another@test.com");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Register_WithInvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        var registerDto = new RegisterDto("testuser", "Password123", "invalid-email");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Register_WithWeakPassword_ReturnsBadRequest()
    {
        // Arrange: Password sin mayúsculas ni números
        var registerDto = new RegisterDto("testuser", "weak", "test@test.com");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Register_ThenLogin_WorksCorrectly()
    {
        // Arrange: Registrar un nuevo usuario
        var registerDto = new RegisterDto("integrationuser", "Integration123", "integration@test.com");
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerDto);
        registerResponse.EnsureSuccessStatusCode();

        // Act: Intentar login con las credenciales recién creadas
        var loginDto = new LoginDto("integrationuser", "Integration123");
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        loginResponse.EnsureSuccessStatusCode();
        var result = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Token, Is.Not.Null.And.Not.Empty);
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    // DTO auxiliar para el login response
    private class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
    }
}
