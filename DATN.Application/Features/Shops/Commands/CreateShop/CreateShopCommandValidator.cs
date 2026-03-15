using FluentValidation;

namespace DATN.Application.Features.Shops.Commands.CreateShop;

public class CreateShopCommandValidator : AbstractValidator<CreateShopCommand>
{
    public CreateShopCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Shop name is required.")
            .MaximumLength(255).WithMessage("Shop name must not exceed 255 characters.");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug is required.")
            .MaximumLength(255).WithMessage("Slug must not exceed 255 characters.");

        RuleFor(x => x.OwnerId)
            .NotNull().WithMessage("OwnerId is required for the shop.");
    }
}
