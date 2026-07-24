using Almentor.TaskApi.Application.Features.Tasks.Dtos;
using Almentor.TaskApi.Application.Features.Tasks.Validators;
using Almentor.TaskApi.Tests.Unit.TestUtilities;
using Shouldly;

namespace Almentor.TaskApi.Tests.Unit.Validators;

public class CreateTaskRequestValidatorTests
{
    private static readonly DateOnly Today = new(2026, 7, 24);
    private readonly CreateTaskRequestValidator _validator = new(new FakeDateTimeProvider(Today));

    [Fact]
    public void Empty_title_is_invalid()
    {
        var request = new CreateTaskRequest { Title = "" };

        var result = _validator.Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateTaskRequest.Title));
    }

    [Fact]
    public void Due_date_in_the_past_is_invalid()
    {
        var request = new CreateTaskRequest { Title = "Ship it", DueDate = Today.AddDays(-1) };

        var result = _validator.Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateTaskRequest.DueDate));
    }

    [Fact]
    public void Due_date_of_today_is_valid()
    {
        // The rule compares by date, not time-of-day — today must not be rejected.
        var request = new CreateTaskRequest { Title = "Ship it", DueDate = Today };

        var result = _validator.Validate(request);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Due_date_in_the_future_is_valid()
    {
        var request = new CreateTaskRequest { Title = "Ship it", DueDate = Today.AddDays(30) };

        var result = _validator.Validate(request);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Missing_due_date_is_valid()
    {
        var request = new CreateTaskRequest { Title = "Ship it", DueDate = null };

        var result = _validator.Validate(request);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Title_over_200_characters_is_invalid()
    {
        var request = new CreateTaskRequest { Title = new string('a', 201) };

        var result = _validator.Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateTaskRequest.Title));
    }
}
