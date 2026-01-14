namespace FunkoApi.Errors;

public abstract record AppError(string Mensaje, int StatusCode);

public record NotFoundError(string Mensaje) : AppError(Mensaje, 404)
{
    //Error para categoria con Id de tipo GUID
    public static NotFoundError FromId(Guid id, string entity) => 
        new($"{entity} con ID {id} no encontrado/a.");
    
    //Igual pero para Funko ya que usa long
    public static NotFoundError FromId(long id, string entity) => 
        new($"{entity} con ID {id} no encontrado/a.");
}

public record ConflictError(string Message) : AppError(Message, 409)
{
    public static ConflictError Duplicate(string entity, string value) => 
        new($"Ya existe un/a {entity} con el valor: {value}.");
}

public record BusinessRuleError(string Message) : AppError(Message, 400);


public record ValidationError(string Message, Dictionary<string, string[]> FieldErrors) 
    : AppError(Message, 400)
{
    public static ValidationError WithFieldErrors(Dictionary<string, string[]> errors) => 
        new("Han ocurrido errores de validación.", errors);
}