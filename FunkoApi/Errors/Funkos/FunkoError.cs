namespace FunkoApi.Errors.Funkos;

public static class FunkoError
{
    public static NotFoundError NotFound(long id) =>
        NotFoundError.FromId(id, "Funko");

    public static ConflictError NombreDuplicado(string nombre) =>
        ConflictError.Duplicate("Funko", nombre);

    // Ejemplo de regla de negocio específica para Funkos
    public static BusinessRuleError PrecioInvalido(decimal precio) =>
        new($"El precio {precio} no es válido para un Funko de esta categoría");

    public static ValidationError Validacion(string mensaje) =>
        new(mensaje, new Dictionary<string, string[]>());

    public static ValidationError ValidacionConCampos(Dictionary<string, string[]> errores) =>
        ValidationError.WithFieldErrors(errores);
}