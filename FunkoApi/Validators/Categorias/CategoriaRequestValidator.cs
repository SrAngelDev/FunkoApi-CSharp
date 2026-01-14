using FluentValidation;
using FunkoApi.Dtos;

namespace FunkoApi.Validators.Categorias;

public class CategoriaRequestValidator : AbstractValidator<CategoriaRequestDto>
{
    public CategoriaRequestValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre de la categoría es obligatorio")
            .MinimumLength(3).WithMessage("El nombre debe tener al menos 3 caracteres")
            .MaximumLength(50).WithMessage("El nombre no puede superar los 50 caracteres");
    }
}