
using FluentValidation;
using Writepad.Model;

namespace Token2Core.Validators
{
    public class NoteValidator : AbstractValidator<Note>
    {
        public NoteValidator()
        {
            RuleFor(note => note.Title)
                .NotEmpty()
                .WithMessage("Title is required.")
                .MaximumLength(100)
                .WithMessage("Title cannot exceed 100 characters.");

            RuleFor(note => note.Content)
                .NotEmpty()
                .WithMessage("Content is required.")
                .MaximumLength(5000)
                .WithMessage("Content cannot exceed 5000 characters.");

            RuleFor(note => note.Topic)
                .MaximumLength(50)
                .WithMessage("Topic cannot exceed 50 characters.");
        }
    }
}

