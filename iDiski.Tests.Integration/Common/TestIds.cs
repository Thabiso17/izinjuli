using System.Threading;

namespace iDiski.Tests.Integration.Common;

/// <summary>
/// Values for columns carrying a unique index, guaranteed not to repeat within a test run.
///
/// These used to be four random hex characters, which only makes a clash unlikely rather than
/// impossible — around one run in a few hundred, which is exactly often enough to show up as an
/// unexplained red build that passes when you run it again. A counter removes the question.
/// </summary>
public static class TestIds
{
    private static int _next;

    /// <summary>
    /// A short code beginning with <paramref name="prefix"/>. Five characters in total for a
    /// two-letter prefix, which fits the ten a team's short code allows.
    /// </summary>
    public static string Code(string prefix) =>
        prefix + Interlocked.Increment(ref _next).ToString("X4");
}
