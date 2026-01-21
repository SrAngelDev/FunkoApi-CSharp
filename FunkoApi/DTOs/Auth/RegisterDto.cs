namespace FunkoApi.Dtos;

public record RegisterDto(
    string Username, 
    string Password, 
    string Email
    );