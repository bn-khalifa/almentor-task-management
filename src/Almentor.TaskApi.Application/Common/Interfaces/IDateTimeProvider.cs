namespace Almentor.TaskApi.Application.Common.Interfaces;

// Abstraction over the system clock so time-dependent rules (e.g. the
// "due date cannot be in the past" check) can be unit-tested with a fixed
// "now" instead of the real wall clock. Implemented by Infrastructure.
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
    DateOnly Today { get; }
}
