using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Blex.Tests;

/// <summary>
/// <see cref="BlexManager.ResetAll"/> is the application-wide counterpart to
/// <see cref="BlexStoreBase.ResetState"/>: every store returns to the state it had when it was
/// registered, each as its own normal action, so middleware, persistence, undo/redo and DevTools
/// all see it exactly like any other dispatch.
/// </summary>
[TestClass]
public class ResetAllTests
{
    [TestMethod]
    public void ResetAll_ReturnsEveryStoreToItsRegisteredState()
    {
        var counter = new CounterStore();
        var profile = new ProfileStore();
        var manager = new BlexManager();
        manager.Register(counter);
        manager.Register(profile);

        counter.Add(5);
        counter.Label = "busy";
        profile.SignIn("ada");

        manager.ResetAll();

        Assert.AreEqual(0, counter.Count);
        Assert.AreEqual("idle", counter.Label);
        Assert.AreEqual("anonymous", profile.UserName);
    }

    [TestMethod]
    public void ResetAll_RecordsOneResetStateActionPerStore()
    {
        var counter = new CounterStore();
        var profile = new ProfileStore();
        var manager = new BlexManager();
        manager.Register(counter);
        manager.Register(profile);
        counter.Increment();
        profile.SignIn("ada");

        var recorded = new List<string>();
        using var sub = manager.Subscribe(ctx => recorded.Add(ctx.QualifiedName));

        manager.ResetAll();

        Assert.AreSequenceEqual(["counter/ResetState", "profile/ResetState"], recorded);
    }

    [TestMethod]
    public void ResetAll_SkipsStoresThatAlreadyHoldTheirInitialState()
    {
        var counter = new CounterStore();
        var profile = new ProfileStore();
        var manager = new BlexManager();
        manager.Register(counter);
        manager.Register(profile);
        counter.Increment(); // only this one drifted

        var recorded = new List<string>();
        using var sub = manager.Subscribe(ctx => recorded.Add(ctx.QualifiedName));

        manager.ResetAll();

        // An unchanged store mutates nothing, so nothing is recorded for it -- the same rule that
        // makes a no-op adapter call invisible.
        Assert.AreSequenceEqual(["counter/ResetState"], recorded);
    }

    [TestMethod]
    public void ResetAll_IsVetoablePerStore()
    {
        var counter = new CounterStore();
        var profile = new ProfileStore();
        // Veto only the counter's reset, so both stores can still be moved off their defaults.
        var manager = new BlexManager(
            [new BlexFilterMiddleware(ctx => !(ctx.Store is CounterStore && ctx.ActionName == "ResetState"))]);
        manager.Register(counter);
        manager.Register(profile);
        counter.Add(5);
        profile.SignIn("ada");

        manager.ResetAll();

        Assert.AreEqual(5, counter.Count);            // vetoed
        Assert.AreEqual("anonymous", profile.UserName); // reset
    }

    [TestMethod]
    public void ResetAll_ContainsAThrowingStoreAndKeepsGoing()
    {
        var throwing = new CounterStore();
        var profile = new ProfileStore();
        var manager = new BlexManager();
        manager.Register(throwing);
        manager.Register(profile);
        throwing.Increment();
        profile.SignIn("ada");

        var errors = new List<BlexError>();
        manager.OnError = errors.Add;
        throwing.StateChanged += () => throw new System.InvalidOperationException("render boom");

        manager.ResetAll();

        // The throwing store's subscriber is isolated by the notification pipeline, so the reset
        // itself still applies and the stores behind it are untouched by the failure.
        Assert.AreEqual(0, throwing.Count);
        Assert.AreEqual("anonymous", profile.UserName);
        Assert.Contains(e => e.Source == "subscriber", errors);
    }

    [TestMethod]
    public void ResetAll_OnAnEmptyManager_IsANoOp()
    {
        var manager = new BlexManager();
        manager.ResetAll(); // must not throw
    }
}
