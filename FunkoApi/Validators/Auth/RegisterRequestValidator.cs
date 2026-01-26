using FluentValidation;
using FunkoApi.Dtos;

namespace FunkoApi.Validators.Auth;

public class RegisterRequestValidator : AbstractValidator<RegisterDto>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("El nombre de usuario es obligatorio")
            .MinimumLength(3).WithMessage("El usuario debe tener al menos 3 caracteres");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es obligatorio")
            .EmailAddress().WithMessage("El formato del email no es válido");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria")
            .MinimumLength(6).WithMessage("La contraseña debe tener al menos 6 caracteres")
            // Coincidiendo con la config de Identity en Program.cs:
            .Matches(@"[A-Z]").WithMessage("La contraseña debe contener al menos una letra mayúscula")
            .Matches(@"[a-z]").WithMessage("La contraseña debe contener al menos una letra minúscula")
            .Matches(@"[4-12]").WithMessage("La contraseña debe contener al menos un número");
    }
}
