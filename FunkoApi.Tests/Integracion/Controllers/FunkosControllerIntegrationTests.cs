﻿using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FunkoApi.Dtos;
using FunkoApi.Tests.Integration;
using NUnit.Framework;

namespace FunkoApi.Tests.Integracion.Controllers;

[TestFixture]
public class FunkosControllerIntegrationTests
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
        var loginDto = new LoginDto("admin", "Admin123");
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return result?.Token ?? throw new Exception("No se pudo obtener el token");
    }

    [Test]
    public async Task GetAll_ReturnsOk_AndListOfFunkos()
    {
        // Arrange: (La BD ya se inicializó en el Factory con datos seed)

        // Act
        var response = await _client.GetAsync("/api/funkos");

        // Assert
        response.EnsureSuccessStatusCode(); // Verifica 200-299
        var result = await response.Content.ReadFromJsonAsync<List<FunkoResponseDto>>();
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Count, Is.GreaterThanOrEqualTo(2)); // Al menos Iron Man y Batman
        Assert.That(result.Any(f => f.Nombre == "Iron Man"), Is.True);
        Assert.That(result.Any(f => f.Nombre == "Batman"), Is.True);
    }

    [Test]
    public async Task GetById_CuandoExiste_ReturnsOkAndFunko()
    {
        // Arrange: Primero obtenemos todos para conseguir un ID válido
        var getAllResponse = await _client.GetAsync("/api/funkos");
        var allFunkos = await getAllResponse.Content.ReadFromJsonAsync<List<FunkoResponseDto>>();
        var funkoId = allFunkos!.First().Id;

        // Act
        var response = await _client.GetAsync($"/api/funkos/{funkoId}");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<FunkoResponseDto>();
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(funkoId));
    }

    [Test]
    public async Task GetById_CuandoNoExiste_ReturnsNotFound()
    {
        // Arrange
        long idInexistente = 99999;

        // Act
        var response = await _client.GetAsync($"/api/funkos/{idInexistente}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Create_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var newFunko = new FunkoRequestDto("Spider-Man", Guid.NewGuid(), 28.0m);

        // Act: Intentamos hacer POST sin configurar Header de Auth
        var response = await _client.PostAsJsonAsync("/api/funkos", newFunko);

        // Assert: Debe fallar porque el Controller tiene [Authorize(Roles = Roles.Admin)]
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Create_WithAdminToken_ReturnsCreated()
    {
        // Arrange
        var getAllResponse = await _client.GetAsync("/api/funkos");
        var allFunkos = await getAllResponse.Content.ReadFromJsonAsync<List<FunkoResponseDto>>();
        var categoriaId = allFunkos!.First().Categoria!.Id;

        var newFunko = new FunkoRequestDto("Thor", categoriaId, 32.0m);
        
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", _adminToken);

        // Act
        var response = await _client.PostAsJsonAsync("/api/funkos", newFunko);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        
        var result = await response.Content.ReadFromJsonAsync<FunkoResponseDto>();
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Nombre, Is.EqualTo("Thor"));
        Assert.That(result.Precio, Is.EqualTo(32.0m));
    }

    [Test]
    public async Task Create_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange
        var invalidFunko = new FunkoRequestDto("", Guid.Empty, -5.0m); // Datos inválidos
        
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", _adminToken);

        // Act
        var response = await _client.PostAsJsonAsync("/api/funkos", invalidFunko);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Update_WithAdminToken_ReturnsOk()
    {
        // Arrange
        var getAllResponse = await _client.GetAsync("/api/funkos");
        var allFunkos = await getAllResponse.Content.ReadFromJsonAsync<List<FunkoResponseDto>>();
        var funkoToUpdate = allFunkos!.First();
        
        var updatedFunko = new FunkoRequestDto(
            funkoToUpdate.Nombre + " Updated", 
            funkoToUpdate.Categoria!.Id, 
            funkoToUpdate.Precio + 5.0m);
        
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", _adminToken);

        // Act
        var response = await _client.PutAsJsonAsync($"/api/funkos/{funkoToUpdate.Id}", updatedFunko);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<FunkoResponseDto>();
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Nombre, Does.Contain("Updated"));
    }

    [Test]
    public async Task Delete_WithAdminToken_ReturnsNoContent()
    {
        // Arrange: Primero creamos un funko para eliminarlo
        var getAllResponse = await _client.GetAsync("/api/funkos");
        var allFunkos = await getAllResponse.Content.ReadFromJsonAsync<List<FunkoResponseDto>>();
        var categoriaId = allFunkos!.First().Categoria!.Id;

        var newFunko = new FunkoRequestDto("Funko to Delete", categoriaId, 15.0m);
        
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", _adminToken);
        
        var createResponse = await _client.PostAsJsonAsync("/api/funkos", newFunko);
        var createdFunko = await createResponse.Content.ReadFromJsonAsync<FunkoResponseDto>();

        // Act
        var response = await _client.DeleteAsync($"/api/funkos/{createdFunko!.Id}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        
        // Verificar que ya no existe
        var getResponse = await _client.GetAsync($"/api/funkos/{createdFunko.Id}");
        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Delete_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var getAllResponse = await _client.GetAsync("/api/funkos");
        var allFunkos = await getAllResponse.Content.ReadFromJsonAsync<List<FunkoResponseDto>>();
        var funkoId = allFunkos!.First().Id;

        // Act
        var response = await _client.DeleteAsync($"/api/funkos/{funkoId}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
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
