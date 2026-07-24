using Almentor.TaskApi.Application.Common.Parsing;
using Almentor.TaskApi.Domain.Enums;
using Shouldly;

namespace Almentor.TaskApi.Tests.Unit.Parsing;

public class EnumSnakeParserTests
{
    [Theory]
    [InlineData("todo", TaskItemStatus.Todo)]
    [InlineData("in_progress", TaskItemStatus.InProgress)]
    [InlineData("done", TaskItemStatus.Done)]
    public void Parses_snake_case_wire_values(string wireValue, TaskItemStatus expected)
    {
        var parsed = EnumSnakeParser.TryParse<TaskItemStatus>(wireValue, out var result);

        parsed.ShouldBeTrue();
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("IN_PROGRESS")]
    [InlineData("In_Progress")]
    public void Parsing_is_case_insensitive(string wireValue)
    {
        var parsed = EnumSnakeParser.TryParse<TaskItemStatus>(wireValue, out var result);

        parsed.ShouldBeTrue();
        result.ShouldBe(TaskItemStatus.InProgress);
    }

    [Fact]
    public void Unrecognized_value_fails_to_parse()
    {
        var parsed = EnumSnakeParser.TryParse<TaskItemStatus>("bogus", out _);

        parsed.ShouldBeFalse();
    }

    [Fact]
    public void Null_or_empty_value_fails_to_parse()
    {
        EnumSnakeParser.TryParse<TaskItemStatus>(null, out _).ShouldBeFalse();
        EnumSnakeParser.TryParse<TaskItemStatus>("", out _).ShouldBeFalse();
    }

    [Fact]
    public void ParseOrNull_returns_null_for_unrecognized_value()
    {
        EnumSnakeParser.ParseOrNull<TaskItemStatus>("bogus").ShouldBeNull();
    }

    [Fact]
    public void ParseOrNull_returns_parsed_value_when_recognized()
    {
        EnumSnakeParser.ParseOrNull<TaskItemPriority>("high").ShouldBe(TaskItemPriority.High);
    }

    [Fact]
    public void Rejects_the_raw_CLR_enum_name_not_just_snake_case()
    {
        // "InProgress" (the C# member name) is not the wire format; only
        // "in_progress" should be accepted, keeping query filters and JSON
        // bodies consistent.
        EnumSnakeParser.TryParse<TaskItemStatus>("InProgress", out _).ShouldBeFalse();
    }
}
