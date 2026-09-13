using iDiski.Tests.Integration.Common;
using Xunit;

namespace iDiski.Tests.Integration.Api;

/// <summary>
/// One application, shared by every test class in this folder.
///
/// It has to be shared rather than per-class. ApiTestFixture hands its configuration to the
/// application through environment variables, which belong to the process, so two fixtures
/// alive at once would each point the other's host at their own database. A collection also
/// stops xUnit running these classes in parallel, and saves booting the host and its container
/// once per class.
///
/// The database is therefore shared too: give anything carrying a unique index — an email, a
/// short code, a jersey number within a team — a value of its own.
/// </summary>
[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiTestFixture>
{
    public const string Name = "API";
}
