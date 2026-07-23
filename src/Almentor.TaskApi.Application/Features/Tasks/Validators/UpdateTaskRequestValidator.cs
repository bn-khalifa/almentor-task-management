using Almentor.TaskApi.Application.Common.Interfaces;
using Almentor.TaskApi.Application.Features.Tasks.Dtos;
using FluentValidation;

namespace Almentor.TaskApi.Application.Features.Tasks.Validators;

public class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
{
    public UpdateTaskRequestValidator(IDateTimeProvider clock)
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.");

        RuleFor(x => x.DueDate)
            .Must(dueDate => dueDate is null || dueDate.Value >= clock.Today)
            .WithMessage("Due date cannot be in the past.");

        RuleFor(x => x.Status)
            .NotNull().WithMessage("Status is required.");

        RuleFor(x => x.Priority)
            .NotNull().WithMessage("Priority is required.");
    }
}
