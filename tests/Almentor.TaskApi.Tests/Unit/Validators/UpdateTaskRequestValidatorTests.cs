using Almentor.TaskApi.Application.Features.Tasks.Dtos;
using Almentor.TaskApi.Application.Features.Tasks.Validators;
using Almentor.TaskApi.Domain.Enums;
using Almentor.TaskApi.Tests.Unit.TestUtilities;
using Shouldly;

namespace Almentor.TaskApi.Tests.Unit.Validators;

public class UpdateTaskRequestValidatorTests
{
    private static readonly DateOnly Today = new(2026, 7, 24);
    private readonly UpdateTaskRequestValidator _validator = new(new FakeDateTimeProvider(Today));

    private static UpdateTaskRequest ValidRequest() => new()
    {
        Title = "Ship it",
        Status = TaskItemStatus.InProgress,
        Priority = TaskItemPriority.Medium
    };

    [Fact]
    public void Fully_populated_request_is_valid()
    {
        var result = _validator.Validate(ValidRequest());

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Missing_status_is_invalid()
    {
        // PUT is a full replace — omitting Status must not silently default.
        var request = ValidRequest();
        request.Status = null;

        var result = _validator.Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateTaskRequest.Status));
    }

    [Fact]
    public void Missing_priority_is_invalid()
    {
        var request = ValidRequest();
        request.Priority = null;

        var result = _validator.Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateTaskRequest.Priority));
    }

    [Fact]
    public void Due_date_in_the_past_is_invalid()
    {
        var request = ValidRequest();
        request.DueDate = Today.AddDays(-7);

        var result = _validator.Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateTaskRequest.DueDate));
    }
}
