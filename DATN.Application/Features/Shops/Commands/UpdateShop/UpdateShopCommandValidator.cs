using FluentValidation;

namespace DATN.Application.Features.Shops.Commands.UpdateShop;

public class UpdateShopCommandValidator : AbstractValidator<UpdateShopCommand>
{
    public UpdateShopCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Shop Id is required.");
            
        RuleFor(x => x.OwnerId)
            .NotEmpty().WithMessage("OwnerId must be provided to verify permissions.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Shop name is required.")
            .MaximumLength(255).WithMessage("Shop name must not exceed 255 characters.");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug is required.")
            .MaximumLength(255).WithMessage("Slug must not exceed 255 characters.");
    }
}
