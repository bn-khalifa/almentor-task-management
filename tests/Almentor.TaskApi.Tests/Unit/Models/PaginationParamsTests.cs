using Almentor.TaskApi.Application.Common.Models;
using Shouldly;

namespace Almentor.TaskApi.Tests.Unit.Models;

public class PaginationParamsTests
{
    [Fact]
    public void Defaults_are_offset_zero_and_limit_twenty()
    {
        var pagination = new PaginationParams();

        pagination.Offset.ShouldBe(0);
        pagination.Limit.ShouldBe(PaginationParams.DefaultLimit);
    }

    [Fact]
    public void Negative_offset_clamps_to_zero()
    {
        var pagination = new PaginationParams { Offset = -50 };

        pagination.Offset.ShouldBe(0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Non_positive_limit_falls_back_to_default(int requested)
    {
        var pagination = new PaginationParams { Limit = requested };

        pagination.Limit.ShouldBe(PaginationParams.DefaultLimit);
    }

    [Fact]
    public void Limit_above_max_clamps_to_max()
    {
        var pagination = new PaginationParams { Limit = 10_000 };

        pagination.Limit.ShouldBe(PaginationParams.MaxLimit);
    }

    [Fact]
    public void Limit_within_range_is_kept_as_is()
    {
        var pagination = new PaginationParams { Limit = 37 };

        pagination.Limit.ShouldBe(37);
    }
}
