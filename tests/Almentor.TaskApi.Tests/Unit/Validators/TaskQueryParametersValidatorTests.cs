using Almentor.TaskApi.Application.Features.Tasks.Querying;
using Almentor.TaskApi.Application.Features.Tasks.Validators;
using Shouldly;

namespace Almentor.TaskApi.Tests.Unit.Validators;

public class TaskQueryParametersValidatorTests
{
    private readonly TaskQueryParametersValidator _validator = new();

    [Fact]
    public void Empty_query_is_valid()
    {
        var result = _validator.Validate(new TaskQueryParameters());

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("todo")]
    [InlineData("in_progress")]
    [InlineData("done")]
    public void Recognized_status_values_are_valid(string status)
    {
        var result = _validator.Validate(new TaskQueryParameters { Status = status });

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Unrecognized_status_is_invalid()
    {
        var result = _validator.Validate(new TaskQueryParameters { Status = "pending" });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(TaskQueryParameters.Status));
    }

    [Fact]
    public void Unrecognized_sort_field_is_invalid()
    {
        var result = _validator.Validate(new TaskQueryParameters { Sort = "banana" });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(TaskQueryParameters.Sort));
    }

    [Fact]
    public void Reversed_due_date_range_is_invalid()
    {
        var query = new TaskQueryParameters
        {
            DueDateFrom = new DateOnly(2026, 12, 1),
            DueDateTo = new DateOnly(2026, 1, 1)
        };

        var result = _validator.Validate(query);

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Equal_due_date_range_bounds_are_valid()
    {
        var sameDay = new DateOnly(2026, 8, 1);
        var query = new TaskQueryParameters { DueDateFrom = sameDay, DueDateTo = sameDay };

        var result = _validator.Validate(query);

        result.IsValid.ShouldBeTrue();
    }
}
