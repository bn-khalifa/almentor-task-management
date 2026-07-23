using Almentor.TaskApi.Application.Common.Interfaces;

namespace Almentor.TaskApi.Infrastructure.Time;

// Real-clock implementation of IDateTimeProvider. Registered as a singleton.
public class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;

    public DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);
}
