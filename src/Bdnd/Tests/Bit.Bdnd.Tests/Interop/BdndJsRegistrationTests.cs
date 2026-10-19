using Bit.Bdnd.Tests.Fakes;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Bdnd.Tests.Interop;

/// <summary>
/// The registration keeps one JS object per rendered host element in sync across renders: it registers the
/// element, pushes changed options, re-registers a re-created element and releases the JS object on disposal.
/// </summary>
[TestClass]
public class BdndJsRegistrationTests
{
    private sealed record Options(int Value);

    private static readonly ElementReference Element = new("element-1");

    /// <summary>A register delegate that records its calls and answers from <paramref name="answer"/>.</summary>
    private sealed class Registrar(Func<int, ValueTask<IJSObjectReference?>> answer)
    {
        public List<(ElementReference Element, Options Options)> Calls { get; } = [];

        public Registrar(params IJSObjectReference?[] references)
            : this(call => ValueTask.FromResult(references[Math.Min(call, references.Length - 1)]))
        {
        }

        public ValueTask<IJSObjectReference?> Register(ElementReference element, Options options)
        {
            Calls.Add((element, options));
            return answer(Calls.Count - 1);
        }
    }

    [TestMethod]
    public async Task TheFirstSyncRegistersTheElementWithItsOptions()
    {
        var reference = new FakeJSObjectReference();
        var registrar = new Registrar(reference);
        var registration = new BdndJsRegistration<Options>(registrar.Register);

        Assert.IsNull(registration.Reference);
        await registration.SyncAsync(Element, new Options(1));

        var call = Assert.ContainsSingle(registrar.Calls);
        Assert.AreEqual(Element, call.Element);
        Assert.AreEqual(new Options(1), call.Options);
        Assert.AreSame(reference, registration.Reference);
        Assert.IsEmpty(reference.Calls);
    }

    [TestMethod]
    public async Task EqualOptionsAreNotPushedAgain()
    {
        var reference = new FakeJSObjectReference();
        var registrar = new Registrar(reference);
        var registration = new BdndJsRegistration<Options>(registrar.Register);

        await registration.SyncAsync(Element, new Options(1));
        await registration.SyncAsync(Element, new Options(1));

        Assert.ContainsSingle(registrar.Calls);
        Assert.IsEmpty(reference.Calls);
    }

    [TestMethod]
    public async Task ChangedOptionsArePushedThroughUpdate()
    {
        var reference = new FakeJSObjectReference();
        var registrar = new Registrar(reference);
        var registration = new BdndJsRegistration<Options>(registrar.Register);

        await registration.SyncAsync(Element, new Options(1));
        await registration.SyncAsync(Element, new Options(2));
        await registration.SyncAsync(Element, new Options(2));

        Assert.ContainsSingle(registrar.Calls);
        var update = Assert.ContainsSingle(reference.Calls);
        Assert.AreEqual("update", update.Identifier);
        Assert.AreEqual(new Options(2), update.Args![0]);
    }

    [TestMethod]
    public async Task OptionsAFailedUpdateDidNotDeliverArePushedAgain()
    {
        var reference = new FakeJSObjectReference();
        var registration = new BdndJsRegistration<Options>(new Registrar(reference).Register);
        await registration.SyncAsync(Element, new Options(1));
        reference.Fails("update", new JSException("update failed"));

        await Assert.ThrowsExactlyAsync<JSException>(() => registration.SyncAsync(Element, new Options(2)));
        reference.Succeeds("update");
        await registration.SyncAsync(Element, new Options(2));

        Assert.AreSequenceEqual(new object?[] { new Options(2), new Options(2) },
            reference.CallsTo("update").Select(args => args![0]));
    }

    [TestMethod]
    public async Task ARecreatedElementIsRegisteredAgainAndTheOldRegistrationReleased()
    {
        var first = new FakeJSObjectReference();
        var second = new FakeJSObjectReference();
        var registrar = new Registrar(first, second);
        var registration = new BdndJsRegistration<Options>(registrar.Register);

        await registration.SyncAsync(Element, new Options(1));
        await registration.SyncAsync(new ElementReference("element-2"), new Options(1));

        Assert.HasCount(2, registrar.Calls);
        Assert.AreEqual("element-2", registrar.Calls[1].Element.Id);
        Assert.AreSequenceEqual(new[] { "dispose" }, first.Calls.Select(c => c.Identifier));
        Assert.AreEqual(1, first.DisposeCount);
        Assert.AreSame(second, registration.Reference);
        Assert.IsEmpty(second.Calls);
    }

    [TestMethod]
    public async Task ARegistrationAnsweredWithNullIsRetriedOnTheNextSync()
    {
        // registerDragSource answers null when the element was gone by the time JS ran
        var reference = new FakeJSObjectReference();
        var registrar = new Registrar(null, reference);
        var registration = new BdndJsRegistration<Options>(registrar.Register);

        await registration.SyncAsync(Element, new Options(1));
        Assert.IsNull(registration.Reference);

        await registration.SyncAsync(Element, new Options(1));

        Assert.HasCount(2, registrar.Calls);
        Assert.AreSame(reference, registration.Reference);
    }

    [TestMethod]
    public async Task SyncsWhileARegistrationIsInFlightAreCoalescedIntoTheLatest()
    {
        var gate = new TaskCompletionSource<IJSObjectReference?>();
        var registrar = new Registrar(_ => new ValueTask<IJSObjectReference?>(gate.Task));
        var registration = new BdndJsRegistration<Options>(registrar.Register);

        var first = registration.SyncAsync(Element, new Options(1));
        var second = registration.SyncAsync(Element, new Options(2));
        var third = registration.SyncAsync(Element, new Options(3));

        Assert.IsFalse(first.IsCompleted);
        Assert.IsTrue(second.IsCompleted, "a render must not wait for JS");
        Assert.IsTrue(third.IsCompleted);

        var reference = new FakeJSObjectReference();
        gate.SetResult(reference);
        await first;

        Assert.ContainsSingle(registrar.Calls);
        var update = Assert.ContainsSingle(reference.Calls);
        Assert.AreEqual("update", update.Identifier);
        Assert.AreEqual(new Options(3), update.Args![0]);
    }

    [TestMethod]
    public async Task SyncsWhileAnUpdateIsInFlightAreCoalescedToo()
    {
        var gate = new TaskCompletionSource();
        var reference = new FakeJSObjectReference();
        var registration = new BdndJsRegistration<Options>(new Registrar(reference).Register);
        await registration.SyncAsync(Element, new Options(1));
        reference.WaitsFor("update", gate.Task);

        var first = registration.SyncAsync(Element, new Options(2));
        await registration.SyncAsync(Element, new Options(3));
        await registration.SyncAsync(Element, new Options(4));
        gate.SetResult();
        await first;

        Assert.AreSequenceEqual(new object?[] { new Options(2), new Options(4) },
            reference.CallsTo("update").Select(args => args![0]));
    }

    [TestMethod]
    public async Task AFailedSyncDoesNotBlockTheNextOne()
    {
        var reference = new FakeJSObjectReference();
        var registrar = new Registrar(call => call == 0
            ? ValueTask.FromException<IJSObjectReference?>(new JSException("boom"))
            : ValueTask.FromResult<IJSObjectReference?>(reference));
        var registration = new BdndJsRegistration<Options>(registrar.Register);

        await Assert.ThrowsExactlyAsync<JSException>(() => registration.SyncAsync(Element, new Options(1)));
        await registration.SyncAsync(Element, new Options(1));

        Assert.AreSame(reference, registration.Reference);
    }

    [TestMethod]
    public async Task ALostCircuitDuringSyncIsSwallowed()
    {
        var registrar = new Registrar(_ => ValueTask.FromException<IJSObjectReference?>(new JSDisconnectedException("gone")));
        var registration = new BdndJsRegistration<Options>(registrar.Register);

        await registration.SyncAsync(Element, new Options(1));

        Assert.IsNull(registration.Reference);
    }

    [TestMethod]
    public async Task DisposingReleasesTheJsObject()
    {
        var reference = new FakeJSObjectReference();
        var registration = new BdndJsRegistration<Options>(new Registrar(reference).Register);
        await registration.SyncAsync(Element, new Options(1));

        await registration.DisposeAsync();
        await registration.DisposeAsync();

        Assert.AreSequenceEqual(new[] { "dispose" }, reference.Calls.Select(c => c.Identifier));
        Assert.AreEqual(1, reference.DisposeCount);
        Assert.IsNull(registration.Reference);
    }

    [TestMethod]
    public async Task DisposingBeforeRegisteringDoesNothing()
    {
        var registrar = new Registrar(new FakeJSObjectReference());
        var registration = new BdndJsRegistration<Options>(registrar.Register);

        await registration.DisposeAsync();
        await registration.SyncAsync(Element, new Options(1));

        Assert.IsEmpty(registrar.Calls, "a disposed registration never registers");
    }

    [TestMethod]
    public async Task ARegistrationCompletingAfterDisposalIsReleasedAtOnce()
    {
        var gate = new TaskCompletionSource<IJSObjectReference?>();
        var registration = new BdndJsRegistration<Options>(new Registrar(_ => new ValueTask<IJSObjectReference?>(gate.Task)).Register);
        var sync = registration.SyncAsync(Element, new Options(1));

        await registration.DisposeAsync();
        var reference = new FakeJSObjectReference();
        gate.SetResult(reference);
        await sync;

        Assert.AreSequenceEqual(new[] { "dispose" }, reference.Calls.Select(c => c.Identifier));
        Assert.AreEqual(1, reference.DisposeCount);
        Assert.IsNull(registration.Reference);
    }

    [TestMethod]
    public async Task AnUpdateFailingBecauseOfDisposalIsSwallowed()
    {
        var gate = new TaskCompletionSource();
        var reference = new FakeJSObjectReference();
        var registration = new BdndJsRegistration<Options>(new Registrar(reference).Register);
        await registration.SyncAsync(Element, new Options(1));
        reference.WaitsFor("update", gate.Task).Fails("update", new ObjectDisposedException("DotNetObjectReference"));

        var sync = registration.SyncAsync(Element, new Options(2));
        await registration.DisposeAsync();
        gate.SetResult();

        await sync;
    }

    [TestMethod]
    [DataRow(typeof(JSDisconnectedException))]
    [DataRow(typeof(JSException))]
    [DataRow(typeof(OperationCanceledException))]
    [DataRow(typeof(ObjectDisposedException))]
    public async Task DisposalIsBestEffort(Type exceptionType)
    {
        var exception = (Exception)Activator.CreateInstance(exceptionType, "gone")!;
        var reference = new FakeJSObjectReference { DisposeException = exception }.Fails("dispose", exception);
        var registration = new BdndJsRegistration<Options>(new Registrar(reference).Register);
        await registration.SyncAsync(Element, new Options(1));

        await registration.DisposeAsync();

        Assert.ContainsSingle(reference.CallsTo("dispose"));
        Assert.AreEqual(1, reference.DisposeCount, "the .NET side is released even when the JS call failed");
    }
}
