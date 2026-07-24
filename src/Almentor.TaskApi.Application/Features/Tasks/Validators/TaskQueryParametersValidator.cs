using Almentor.TaskApi.Application.Common.Parsing;
using Almentor.TaskApi.Application.Features.Tasks.Querying;
using Almentor.TaskApi.Domain.Enums;
using FluentValidation;

namespace Almentor.TaskApi.Application.Features.Tasks.Validators;

// Validates the raw task-list query. Each enum-ish string is checked against the
// same snake_case parser the service will use, so "valid here" guarantees the
// service's parse cannot fail. Null means "not supplied" and is always allowed.
public class TaskQueryParametersValidator : AbstractValidator<TaskQueryParameters>
{
    public TaskQueryParametersValidator()
    {
        RuleFor(x => x.Status)
            .Must(v => v is null || EnumSnakeParser.TryParse<TaskItemStatus>(v, out _))
            .WithMessage("Status must be one of: todo, in_progress, done.");

        RuleFor(x => x.Priority)
            .Must(v => v is null || EnumSnakeParser.TryParse<TaskItemPriority>(v, out _))
            .WithMessage("Priority must be one of: low, medium, high.");

        RuleFor(x => x.Sort)
            .Must(v => v is null || EnumSnakeParser.TryParse<TaskSortField>(v, out _))
            .WithMessage("Sort must be one of: created_at, due_date, priority.");

        RuleFor(x => x.Direction)
            .Must(v => v is null || EnumSnakeParser.TryParse<SortDirection>(v, out _))
            .WithMessage("Direction must be one of: asc, desc.");

        RuleFor(x => x)
            .Must(x => x.DueDateFrom is null || x.DueDateTo is null || x.DueDateFrom <= x.DueDateTo)
            .WithMessage("dueDateFrom must be on or before dueDateTo.")
            .WithName("dueDateRange");
    }
}
