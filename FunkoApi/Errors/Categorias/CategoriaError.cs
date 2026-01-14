namespace FunkoApi.Errors.Categorias;

public static class CategoriaError
{

    public static NotFoundError NotFound(long id) =>
        NotFoundError.FromId(id, "Categoria");
    
    public static ConflictError NombreDuplicado(string nombre) =>
        ConflictError.Duplicate("categoria", nombre);
    
    public static BusinessRuleError TieneProductos(long id, int productosCount) =>
        new($"No se puede eliminar la categoría con ID {id} porque tiene {productosCount} productos asociados");
    
    public static ValidationError Validacion(string mensaje) =>
        new(mensaje, new Dictionary<string, string[]>()); // new Dictionary<string, string[]>() = diccionario vacío
    
    public static ValidationError ValidacionConCampos(Dictionary<string, string[]> errores) =>
        ValidationError.WithFieldErrors(errores);
}