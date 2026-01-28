using System.Diagnostics.CodeAnalysis;

namespace FunkoApi.Errors.Categorias;

[ExcludeFromCodeCoverage]
public static class CategoriaError {
    public static NotFoundError NotFound(Guid id) => new($"Categoría con ID {id} no encontrada");
    public static ConflictError NombreDuplicado(string nombre) => new($"La categoría '{nombre}' ya existe");
    public static BusinessRuleError TieneFunkos(Guid id) => new($"No se puede eliminar la categoría {id} porque tiene funkos asociados");
}