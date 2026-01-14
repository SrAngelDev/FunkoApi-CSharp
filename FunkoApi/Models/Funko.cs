namespace FunkoApi.Models;

public class Funko
{
    public long Id { get; set; }
    public required string Nombre { get; set; }
    public required Categoria Categoria { get; set; }
    public decimal Precio { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}