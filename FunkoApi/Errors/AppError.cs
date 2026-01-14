namespace FunkoApi.Errors;

public abstract record AppError(string Message, int Code);
public record NotFoundError(string Message) : AppError(Message, 404);
public record ConflictError(string Message) : AppError(Message, 409);
public record BusinessRuleError(string Message) : AppError(Message, 400);