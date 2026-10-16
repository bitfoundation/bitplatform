using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bit.Blex;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Blex.Tests;

/// <summary>
/// An action body that throws is reported to middleware and to
/// <see cref="BlexManager.ActionFailed"/> without being swallowed -- the counterpart to Pinia's
/// <c>$onAction</c> <c>onError</c> hook.
/// </summary>
[TestClass]
public class ActionErrorTests
{
    private sealed class RecordingMiddleware : IBlexMiddleware
    {
        public List<BlexActionErrorContext> Errors { get; } = [];

        public void OnAction(BlexActionContext context)
        {
        }

        public void OnActionError(BlexActionErrorContext context) => Errors.Add(context);
    }

    [TestMethod]
    public void SyncActionThatThrows_IsReportedAndStillPropagates()
    {
        var middleware = new RecordingMiddleware();
        var manager = new BlexManager([middleware]);
        var store = new FailingStore();
        manager.Register(store);

        var seen = new List<BlexActionErrorContext>();
        manager.ActionFailed += seen.Add;

        Assert.ThrowsExactly<InvalidOperationException>(store.Boom);

        Assert.ContainsSingle(middleware.Errors);
        Assert.AreEqual("failing/Boom", middleware.Errors[0].QualifiedName);
        Assert.AreEqual("sync-boom", middleware.Errors[0].Exception.Message);
        Assert.ContainsSingle(seen);
    }

    [TestMethod]
    public async Task AsyncActionThatThrows_IsReportedAndStillPropagates()
    {
        var middleware = new RecordingMiddleware();
        var manager = new BlexManager([middleware]);
        var store = new FailingStore();
        manager.Register(store);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(store.BoomAsync);

        Assert.ContainsSingle(middleware.Errors);
        Assert.AreEqual("failing/BoomAsync", middleware.Errors[0].QualifiedName);
    }

    [TestMethod]
    public async Task EffectThatThrows_IsReportedWithItsArguments()
    {
        var middleware = new RecordingMiddleware();
        var manager = new BlexManager([middleware]);
        var store = new FailingStore();
        manager.Register(store);

        // The effect wrapper captures the failure into LoadError instead of propagating it, so the
        // hook is the only way an app-wide reporter learns about it.
        await store.Load("orders");

        Assert.IsNotNull(store.LoadError);
        Assert.ContainsSingle(middleware.Errors);
        var reported = middleware.Errors[0];
        Assert.AreEqual("failing/Load", reported.QualifiedName);
        Assert.AreEqual("key", reported.Args[0].Name);
        Assert.AreEqual("orders", reported.Args[0].Value);
    }

    [TestMethod]
    public async Task CancellationIsNotAFailure()
    {
        var middleware = new RecordingMiddleware();
        var manager = new BlexManager([middleware]);
        var store = new FailingStore();
        manager.Register(store);

        var watch = store.Watch();
        store.CancelWatch();
        await watch;

        Assert.IsEmpty(middleware.Errors);
        Assert.IsNull(store.WatchError);
    }

    [TestMethod]
    public void SuccessfulActions_ReportNothing()
    {
        var middleware = new RecordingMiddleware();
        var manager = new BlexManager([middleware]);
        var store = new FailingStore();
        manager.Register(store);

        store.Set(3);

        Assert.IsEmpty(middleware.Errors);
    }

    [TestMethod]
    public void ANestedFailure_IsReportedOnceByTheActionThatThrew()
    {
        var middleware = new RecordingMiddleware();
        var manager = new BlexManager([middleware]);
        var store = new FailingStore();
        manager.Register(store);

        // The same exception passes through Outer's frame on its way out; reporting per frame would
        // multiply every nested failure.
        Assert.ThrowsExactly<InvalidOperationException>(store.Outer);

        Assert.ContainsSingle(middleware.Errors);
        Assert.AreEqual("failing/Boom", middleware.Errors[0].QualifiedName);
    }

    [TestMethod]
    public async Task ANestedFailureInsideAnEffect_IsAlsoReportedOnce()
    {
        var middleware = new RecordingMiddleware();
        var manager = new BlexManager([middleware]);
        var store = new FailingStore();
        manager.Register(store);

        await store.OuterEffect();

        Assert.IsNotNull(store.OuterEffectError);
        Assert.ContainsSingle(middleware.Errors);
        Assert.AreEqual("failing/Boom", middleware.Errors[0].QualifiedName);
    }

    [TestMethod]
    public void DistinctFailures_AreEachReported()
    {
        var middleware = new RecordingMiddleware();
        var manager = new BlexManager([middleware]);
        var store = new FailingStore();
        manager.Register(store);

        Assert.ThrowsExactly<InvalidOperationException>(store.Boom);
        Assert.ThrowsExactly<InvalidOperationException>(store.Boom);

        Assert.AreEqual(2, middleware.Errors.Count);
    }

    [TestMethod]
    public void TheHarnessRecordsFailures()
    {
        using var harness = Bit.Blex.Testing.BlexTestHarness.For<FailingStore>();

        Assert.ThrowsExactly<InvalidOperationException>(harness.Store.Boom);

        Assert.ContainsSingle(harness.Failures);
        Assert.AreEqual("failing/Boom", harness.Failures[0].QualifiedName);
    }

    [TestMethod]
    public void AThrowingErrorHandler_IsIsolatedAndReported()
    {
        var manager = new BlexManager();
        var store = new FailingStore();
        manager.Register(store);

        var errors = new List<BlexError>();
        manager.OnError = errors.Add;
        manager.ActionFailed += _ => throw new InvalidOperationException("handler-boom");

        // The original failure still reaches the caller; the handler's own failure is contained.
        Assert.ThrowsExactly<InvalidOperationException>(store.Boom);
        Assert.ContainsSingle(errors);
        Assert.AreEqual("subscriber", errors[0].Source);
    }
}
