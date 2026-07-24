using Almentor.TaskApi.Application.Common.Interfaces;

namespace Almentor.TaskApi.Tests.Unit.TestUtilities;

/// <summary>
/// Fixed-clock test double for <see cref="IDateTimeProvider"/>. The due-date
/// rule ("cannot be in the past") is inherently relative to "now" — this makes
/// that comparison deterministic instead of depending on when the test runs.
/// </summary>
public class FakeDateTimeProvider : IDateTimeProvider
{
    public FakeDateTimeProvider(DateOnly today)
    {
        Today = today;
        UtcNow = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
    }

    public DateTime UtcNow { get; }
    public DateOnly Today { get; }
}
