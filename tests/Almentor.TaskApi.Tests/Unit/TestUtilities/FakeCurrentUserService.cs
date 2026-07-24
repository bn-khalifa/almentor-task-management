using Almentor.TaskApi.Application.Common.Interfaces;

namespace Almentor.TaskApi.Tests.Unit.TestUtilities;

/// <summary>Fixed-identity current-user for service unit tests.</summary>
public class FakeCurrentUserService : ICurrentUserService
{
    public Guid UserId { get; set; } = Guid.NewGuid();
}
