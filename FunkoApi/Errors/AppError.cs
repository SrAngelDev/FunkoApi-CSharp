﻿using System.Diagnostics.CodeAnalysis;

namespace FunkoApi.Errors;

[ExcludeFromCodeCoverage]
public abstract record AppError(string Message, int Code);
[ExcludeFromCodeCoverage]
public record NotFoundError(string Message) : AppError(Message, 404);
[ExcludeFromCodeCoverage]
public record ConflictError(string Message) : AppError(Message, 409);
[ExcludeFromCodeCoverage]
public record BusinessRuleError(string Message) : AppError(Message, 400);