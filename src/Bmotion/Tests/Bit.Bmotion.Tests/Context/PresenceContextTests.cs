namespace Bit.Bmotion.Tests.Context;

/// <summary>
/// Tests for <see cref="BmotionPresenceContext"/>'s exit bookkeeping: a completion only counts
/// towards the exit it finished, so an exit that ends after it was cancelled - or one recorded
/// during an earlier, cancelled exit - can never make a later exit complete early.
/// <para>
/// The children are bare <see cref="Bmotion"/> instances: the context only tracks their identity.
/// </para>
/// </summary>
[TestClass]
public class PresenceContextTests
{
    private static (BmotionPresenceContext Ctx, Func<int> Completions) NewContext()
    {
        var ctx = new BmotionPresenceContext();
        int completions = 0;
        ctx.AllExitsComplete += () => completions++;
        return (ctx, () => completions);
    }

    [TestMethod]
    public void NotifyExitComplete_WhileNotExiting_IsIgnored()
    {
        var (ctx, completions) = NewContext();
        var child = new Bmotion();
        ctx.Register(child);

        // An exit that finishes after the content re-entered: it completes nothing.
        ctx.NotifyExitComplete(child);
        Assert.AreEqual(0, completions());

        // ...and is not remembered for the next exit either.
        ctx.IsExiting = true;
        Assert.AreEqual(0, completions(), "starting an exit must not complete it on a stale notification");

        ctx.NotifyExitComplete(child);
        Assert.AreEqual(1, completions());
    }

    [TestMethod]
    public void ANewExit_StartsCountingFromZero()
    {
        var (ctx, completions) = NewContext();
        var a = new Bmotion();
        var b = new Bmotion();
        ctx.Register(a);
        ctx.Register(b);

        // First exit: a finishes, then the content re-enters before b does.
        ctx.IsExiting = true;
        ctx.NotifyExitComplete(a);
        ctx.IsExiting = false;

        // Second exit: b finishing alone must not complete it - a's completion belonged to the
        // cancelled exit.
        ctx.IsExiting = true;
        ctx.NotifyExitComplete(b);
        Assert.AreEqual(0, completions(), "a completion from a cancelled exit must not count towards the next one");

        ctx.NotifyExitComplete(a);
        Assert.AreEqual(1, completions());
    }

    [TestMethod]
    public void SettingIsExitingAgain_MidExit_KeepsTheCompletionsSoFar()
    {
        var (ctx, completions) = NewContext();
        var a = new Bmotion();
        var b = new Bmotion();
        ctx.Register(a);
        ctx.Register(b);

        ctx.IsExiting = true;
        ctx.NotifyExitComplete(a);
        // Re-asserting the same exit (a re-render while leaving) is not a new exit.
        ctx.IsExiting = true;
        ctx.NotifyExitComplete(b);

        Assert.AreEqual(1, completions());
    }
}
