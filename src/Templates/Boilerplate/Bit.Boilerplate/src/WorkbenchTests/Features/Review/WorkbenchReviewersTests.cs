namespace Boilerplate.WorkbenchTests.Features.Review;

/// <summary>
/// An invite's cookie counts only with the Workbench's own key, and only in dev mode, where an agent reads the comments.
/// </summary>
[TestClass, TestCategory("UnitTest")]
public class WorkbenchReviewersTests
{
    private static IConfiguration DevMode(string? dotnetWatch) => new ConfigurationBuilder().AddInMemoryCollection([new("DOTNET_WATCH", dotnetWatch)]).Build();

    private static DefaultHttpContext WithCookie(string? value)
    {
        var context = new DefaultHttpContext();

        if (value is not null)
        {
            context.Request.Headers.Cookie = $"{WorkbenchReviewers.CookieName}={value}";
        }

        return context;
    }

    [TestMethod]
    public void TheKey_Should_BeKeptForTheNextRunAndBeHardToGuess()
    {
        var key = WorkbenchReviewers.Key;

        Assert.AreEqual(key, File.ReadAllText(Path.Combine(WorkbenchFolders.Reviews, "reviewer-key.txt")).Trim());
        Assert.IsGreaterThanOrEqualTo(32, key.Length);
    }

    [TestMethod]
    [DataRow("1", true, true)]
    [DataRow(null, true, false)]
    [DataRow("0", true, false)]
    [DataRow("1", false, false)]
    public void TheCookie_Should_CountOnlyWithTheKeyInDevMode(string? dotnetWatch, bool hasTheKey, bool isInvited)
    {
        var context = WithCookie(hasTheKey ? WorkbenchReviewers.Key : $"{WorkbenchReviewers.Key[..^1]}x");

        Assert.AreEqual(isInvited, WorkbenchReviewers.IsInvited(context, DevMode(dotnetWatch)));
    }

    [TestMethod]
    public void NoCookie_Should_BeNoInvite()
    {
        Assert.IsFalse(WorkbenchReviewers.IsInvited(WithCookie(null), DevMode("1")));
        Assert.IsFalse(WorkbenchReviewers.IsInvited(WithCookie(""), DevMode("1")));
    }

    [TestMethod]
    public void AnInviteLink_Should_BeLeftAloneOutsideDevMode()
    {
        var context = new DefaultHttpContext();

        context.Request.QueryString = new QueryString($"?{WorkbenchReviewers.QueryName}={WorkbenchReviewers.Key}");

        Assert.IsFalse(WorkbenchReviewers.TryAccept(context, DevMode(null)));
        Assert.IsFalse(context.Response.Headers.ContainsKey("Set-Cookie"));
    }
}
