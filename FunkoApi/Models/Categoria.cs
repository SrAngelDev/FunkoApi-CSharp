namespace FunkoApi.Models;

public class Categoria
{
    public Guid Id { get; set; }
    public string Nombre { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}