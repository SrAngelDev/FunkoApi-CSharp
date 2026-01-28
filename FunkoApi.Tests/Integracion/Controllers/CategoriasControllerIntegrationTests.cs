using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FunkoApi.Dtos;
using FunkoApi.Tests.Integration;
using NUnit.Framework;

namespace FunkoApi.Tests.Integracion.Controllers;

[TestFixture]
public class CategoriasControllerIntegrationTests
{
    private CustomWebApplicationFactory _factory;
    private HttpClient _client;
    private string? _adminToken;

    [SetUp]
    public async Task Setup()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
        
        // Obtener token de admin para tests que requieren autenticación
        _adminToken = await GetAdminTokenAsync();
    }

    private async Task<string> GetAdminTokenAsync()
    {
        var loginDto = new { username = "admin", password = "Admin123" };
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return result?.Token ?? throw new Exception("No se pudo obtener el token");
    }

    // DTO auxiliar para el login response
    private class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
    }

    [Test]
    public async Task GetAll_ReturnsOk_AndListOfCategorias()
    {
        // Arrange: (La BD ya se inicializó en el Factory con datos seed)

        // Act
        var response = await _client.GetAsync("/api/categorias");

        // Assert
        response.EnsureSuccessStatusCode(); // Verifica 200-299
        var result = await response.Content.ReadFromJsonAsync<List<CategoriaResponseDto>>();
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Count, Is.GreaterThanOrEqualTo(2)); // Al menos Marvel y DC
        Assert.That(result.Any(c => c.Nombre == "Marvel"), Is.True);
        Assert.That(result.Any(c => c.Nombre == "DC"), Is.True);
    }

    [Test]
    public async Task GetById_CuandoExiste_ReturnsOkAndCategoria()
    {
        // Arrange: Primero obtenemos todas para conseguir un ID válido
        var getAllResponse = await _client.GetAsync("/api/categorias");
        var allCategorias = await getAllResponse.Content.ReadFromJsonAsync<List<CategoriaResponseDto>>();
        var categoriaId = allCategorias!.First().Id;

        // Act
        var response = await _client.GetAsync($"/api/categorias/{categoriaId}");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CategoriaResponseDto>();
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(categoriaId));
    }

    [Test]
    public async Task GetById_CuandoNoExiste_ReturnsNotFound()
    {
        // Arrange
        var idInexistente = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/categorias/{idInexistente}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Create_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var newCategoria = new CategoriaRequestDto("Disney");

        // Act: Intentamos hacer POST sin configurar Header de Auth
        var response = await _client.PostAsJsonAsync("/api/categorias", newCategoria);

        // Assert: Debe fallar porque el Controller tiene [Authorize(Roles = Roles.Admin)]
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Create_WithAdminToken_ReturnsCreated()
    {
        // Arrange
        var newCategoria = new CategoriaRequestDto("Star Wars");
        
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", _adminToken);

        // Act
        var response = await _client.PostAsJsonAsync("/api/categorias", newCategoria);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        
        var result = await response.Content.ReadFromJsonAsync<CategoriaResponseDto>();
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Nombre, Is.EqualTo("Star Wars"));
    }

    [Test]
    public async Task Create_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange
        var invalidCategoria = new CategoriaRequestDto(""); // Nombre vacío
        
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", _adminToken);

        // Act
        var response = await _client.PostAsJsonAsync("/api/categorias", invalidCategoria);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Create_WithDuplicateName_ReturnsConflict()
    {
        // Arrange
        var duplicateCategoria = new CategoriaRequestDto("Marvel"); // Ya existe en seed
        
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", _adminToken);

        // Act
        var response = await _client.PostAsJsonAsync("/api/categorias", duplicateCategoria);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
    }

    [Test]
    public async Task Update_WithAdminToken_ReturnsOk()
    {
        // Arrange: Primero creamos una categoría para actualizarla
        var newCategoria = new CategoriaRequestDto("Pixar");
        
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", _adminToken);
        
        var createResponse = await _client.PostAsJsonAsync("/api/categorias", newCategoria);
        var createdCategoria = await createResponse.Content.ReadFromJsonAsync<CategoriaResponseDto>();
        
        var updatedCategoria = new CategoriaRequestDto("Pixar Studios");

        // Act
        var response = await _client.PutAsJsonAsync($"/api/categorias/{createdCategoria!.Id}", updatedCategoria);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CategoriaResponseDto>();
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Nombre, Is.EqualTo("Pixar Studios"));
    }

    [Test]
    public async Task Update_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var getAllResponse = await _client.GetAsync("/api/categorias");
        var allCategorias = await getAllResponse.Content.ReadFromJsonAsync<List<CategoriaResponseDto>>();
        var categoriaId = allCategorias!.First().Id;
        
        var updatedCategoria = new CategoriaRequestDto("Nuevo Nombre");

        // Act
        var response = await _client.PutAsJsonAsync($"/api/categorias/{categoriaId}", updatedCategoria);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Update_CuandoNoExiste_ReturnsNotFound()
    {
        // Arrange
        var idInexistente = Guid.NewGuid();
        var updatedCategoria = new CategoriaRequestDto("Nuevo Nombre");
        
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", _adminToken);

        // Act
        var response = await _client.PutAsJsonAsync($"/api/categorias/{idInexistente}", updatedCategoria);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Delete_WithAdminToken_ReturnsNoContent()
    {
        // Arrange: Primero creamos una categoría para eliminarla
        var newCategoria = new CategoriaRequestDto("Categoria to Delete");
        
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", _adminToken);
        
        var createResponse = await _client.PostAsJsonAsync("/api/categorias", newCategoria);
        var createdCategoria = await createResponse.Content.ReadFromJsonAsync<CategoriaResponseDto>();

        // Act
        var response = await _client.DeleteAsync($"/api/categorias/{createdCategoria!.Id}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        
        // Verificar que ya no existe
        var getResponse = await _client.GetAsync($"/api/categorias/{createdCategoria.Id}");
        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Delete_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var getAllResponse = await _client.GetAsync("/api/categorias");
        var allCategorias = await getAllResponse.Content.ReadFromJsonAsync<List<CategoriaResponseDto>>();
        var categoriaId = allCategorias!.First().Id;

        // Act
        var response = await _client.DeleteAsync($"/api/categorias/{categoriaId}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Delete_CuandoNoExiste_ReturnsNotFound()
    {
        // Arrange
        var idInexistente = Guid.NewGuid();
        
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", _adminToken);

        // Act
        var response = await _client.DeleteAsync($"/api/categorias/{idInexistente}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task GetAll_ReturnsAllCategoriasPublicly()
    {
        // Arrange: Sin autenticación

        // Act
        var response = await _client.GetAsync("/api/categorias");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<List<CategoriaResponseDto>>();
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Count, Is.GreaterThan(0));
    }

    [Test]
    public async Task GetById_ReturnsCategoria_Publicly()
    {
        // Arrange: Sin autenticación
        var getAllResponse = await _client.GetAsync("/api/categorias");
        var allCategorias = await getAllResponse.Content.ReadFromJsonAsync<List<CategoriaResponseDto>>();
        var categoriaId = allCategorias!.First().Id;

        // Act
        var response = await _client.GetAsync($"/api/categorias/{categoriaId}");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CategoriaResponseDto>();
        
        Assert.That(result, Is.Not.Null);
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
