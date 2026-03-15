using FluentValidation;

namespace DATN.Application.Features.Images.Commands.UploadImage;

public class UploadImageCommandValidator : AbstractValidator<UploadImageCommand>
{
    public UploadImageCommandValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("FileName is required.");

        RuleFor(x => x.ContentType)
            .NotEmpty().WithMessage("ContentType is required.")
            .Must(ct => ct.StartsWith("image/")).WithMessage("Only image files are allowed.");

        RuleFor(x => x.FileStream)
            .NotNull().WithMessage("File stream cannot be null.")
            .Must(s => s == null || (s.Length > 0 && s.Length <= 5 * 1024 * 1024))
            .WithMessage("File must not be empty and must be less than or equal to 5MB.");
    }
}
