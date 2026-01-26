using FluentValidation;
using FunkoApi.Dtos;

namespace FunkoApi.Validators.Auth;

public class LoginRequestValidator : AbstractValidator<LoginDto>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("El usuario no puede estar vacío");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña no puede estar vacía");
    }
}