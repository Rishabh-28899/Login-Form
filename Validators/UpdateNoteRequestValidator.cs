
using FluentValidation;
using WritePad.DTOs;

namespace Token2Core.Validators
{
    public class UpdateNoteRequestValidator
        : AbstractValidator<UpdateNoteRequestDto>
    {
        public UpdateNoteRequestValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty()
                .WithMessage("Title is required.")
                .MaximumLength(100)
                .WithMessage("Title cannot exceed 100 characters.");

            RuleFor(x => x.Content)
                .NotEmpty()
                .WithMessage("Content is required.")
                .MaximumLength(5000)
                .WithMessage("Content cannot exceed 5000 characters.");

            RuleFor(x => x.Topic)
                .MaximumLength(50)
                .WithMessage("Topic cannot exceed 50 characters.");
        }
    }
}