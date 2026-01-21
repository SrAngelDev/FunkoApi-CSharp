using FluentValidation;
using FunkoApi.Dtos;

namespace FunkoApi.Validators.Funkos;

public class FunkoRequestValidator : AbstractValidator<FunkoRequestDto>
{
    public FunkoRequestValidator()
    {
        RuleFor(x=> x.Nombre)
            .NotEmpty().WithMessage("El nombre del Funko es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres.");
        
        RuleFor(x => x.Precio)
            .GreaterThan(0).WithMessage("El precio debe ser un valor positivo");

        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("La categoría asociada es obligatoria");
    }
}