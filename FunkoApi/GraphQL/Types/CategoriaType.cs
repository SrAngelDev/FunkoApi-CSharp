using FunkoApi.Models;

namespace FunkoApi.GraphQL.Types;

public class CategoriaType : ObjectType<Categoria>
{
    protected override void Configure(IObjectTypeDescriptor<Categoria> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        descriptor.Field(c => c.Id).Type<IdType>();
        descriptor.Field(c => c.Nombre);
        
        // Relación inversa: Una categoría tiene una lista de Funkos
        descriptor.Field(c => c.Funkos)
            .Type<ListType<FunkoType>>();
    }
}