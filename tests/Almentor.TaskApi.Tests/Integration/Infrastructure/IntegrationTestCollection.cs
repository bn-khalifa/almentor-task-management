namespace Almentor.TaskApi.Tests.Integration.Infrastructure;

/// <summary>
/// Shares one SqlServerFixture (one container) across every integration test
/// class in this collection, so the container starts once per test run, not
/// once per class.
/// </summary>
[CollectionDefinition(nameof(IntegrationTestCollection))]
public class IntegrationTestCollection : ICollectionFixture<SqlServerFixture>;
