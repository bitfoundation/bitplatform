using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Bit.BevTools.Tests;

[TestClass]
public class JsInteropTrackingTests
{
    [TestMethod]
    public async Task Calls_are_recorded_with_identifier_duration_and_result_type()
    {
        var (session, scope) = TestHelpers.CreateSession();
        using (scope)
        {
            var fake = new FakeJSRuntime { Handler = (id, _) => id == "add" ? 3 : null };
            var tracked = fake.WithBevToolsTracking(session);
            Assert.IsExactInstanceOfType<BevTrackingJSRuntime>(tracked);

            var result = await tracked.InvokeAsync<int>("add", 1, 2);
            Assert.AreEqual(3, result);

            var record = Assert.ContainsSingle(session.Interop.Snapshot());
            Assert.AreEqual("add", record.Identifier);
            Assert.AreEqual(BevJsInteropDirection.DotNetToJs, record.Direction);
            Assert.AreEqual(2, record.ArgumentCount);
            Assert.AreEqual("Int32, Int32", record.ArgumentTypes);
            Assert.AreEqual("Int32", record.ResultType);
            Assert.IsTrue(record.Succeeded);
            Assert.IsNotNull(record.DurationMs);
            Assert.AreEqual(BevToolsEventKind.JsInterop, session.Timeline.Find(record.TimelineEventId)!.Kind);
            Assert.ContainsSingle(session.Interop.Aggregates());
        }
    }

    [TestMethod]
    public async Task Failures_are_recorded_and_rethrown()
    {
        var (session, scope) = TestHelpers.CreateSession();
        using (scope)
        {
            var fake = new FakeJSRuntime { Handler = (_, _) => throw new JSException("nope") };
            var tracked = fake.WithBevToolsTracking(session);
            await Assert.ThrowsExactlyAsync<JSException>(async () => await tracked.InvokeVoidAsync("explode"));

            var record = Assert.ContainsSingle(session.Interop.Snapshot());
            Assert.IsFalse(record.Succeeded);
            Assert.Contains("nope", record.Error!);
            Assert.AreEqual("void", record.ResultType);
            var error = Assert.ContainsSingle(session.Errors.Snapshot());
            Assert.AreEqual("js-interop", error.Source);
        }
    }

    [TestMethod]
    public async Task Module_references_are_wrapped_so_module_calls_are_tracked_too()
    {
        var (session, scope) = TestHelpers.CreateSession();
        using (scope)
        {
            var module = new FakeJSObjectReference();
            var fake = new FakeJSRuntime { Handler = (id, _) => id == "import" ? module : null };
            var tracked = fake.WithBevToolsTracking(session);

            var imported = await tracked.InvokeAsync<IJSObjectReference>("import", "./x.js");
            Assert.IsExactInstanceOfType<BevTrackingJSObjectReference>(imported);
            await imported.InvokeVoidAsync("doWork", 1);

            var calls = session.Interop.Snapshot();
            Assert.AreEqual(2, calls.Length);
            Assert.AreEqual("module.doWork", calls[1].Identifier);
            Assert.IsTrue(calls[1].IsModuleCall);
            Assert.ContainsSingle(module.Calls, "doWork");
        }
    }

#if NET10_0_OR_GREATER
    [TestMethod]
    public async Task Value_and_constructor_calls_added_in_net10_reach_the_runtime_and_are_recorded()
    {
        // .NET 10 declares these on IJSRuntime and IJSObjectReference as default members that throw
        // NotImplementedException; a wrapper that left them to the defaults broke the code it was patched into.
        var (session, scope) = TestHelpers.CreateSession();
        using (scope)
        {
            var fake = new Net10JSRuntime();
            var tracked = (BevTrackingInProcessJSRuntime)fake.WithBevToolsTracking(session);

            Assert.AreEqual("Home", await tracked.GetValueAsync<string>("document.title"));
            await tracked.SetValueAsync("document.title", "Orders");
            var map = await tracked.InvokeConstructorAsync("Map");
            Assert.AreEqual("Orders", tracked.GetValue<string>("document.title"));
            tracked.SetValue("app.ready", true);
            Assert.IsExactInstanceOfType<BevTrackingInProcessJSObjectReference>(tracked.InvokeConstructor("Set"));

            Assert.IsExactInstanceOfType<BevTrackingInProcessJSObjectReference>(map);
            Assert.AreEqual(1, await map.GetValueAsync<int>("size"));
            await map.SetValueAsync("label", "m");

            Assert.AreEqual(true, fake.Values["app.ready"]);
            CollectionAssert.AreEqual(
                new[] { "get document.title", "set document.title", "new Map", "get document.title", "set app.ready", "new Set", "get module.size", "set module.label" },
                session.Interop.Snapshot().Select(c => c.Identifier).ToArray());
            Assert.IsTrue(session.Interop.Snapshot().All(c => c.Succeeded));
        }
    }

    /// <summary>A runtime that implements the .NET 10 members, as the framework's own runtimes do.</summary>
    private sealed class Net10JSRuntime : IJSInProcessRuntime, IJSInProcessObjectReference
    {
        public Dictionary<string, object?> Values { get; } = new() { ["document.title"] = "Home", ["size"] = 1 };

        public TValue Invoke<TValue>(string identifier, params object?[]? args) => default!;

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => ValueTask.FromResult(default(TValue)!);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => ValueTask.FromResult(default(TValue)!);

        public TValue GetValue<TValue>(string identifier) => (TValue)Values[identifier]!;

        public void SetValue<TValue>(string identifier, TValue value) => Values[identifier] = value;

        public IJSInProcessObjectReference InvokeConstructor(string identifier, params object?[]? args) => this;

        public ValueTask<TValue> GetValueAsync<TValue>(string identifier) => ValueTask.FromResult(GetValue<TValue>(identifier));

        public ValueTask<TValue> GetValueAsync<TValue>(string identifier, CancellationToken cancellationToken) => GetValueAsync<TValue>(identifier);

        public ValueTask SetValueAsync<TValue>(string identifier, TValue value)
        {
            SetValue(identifier, value);
            return ValueTask.CompletedTask;
        }

        public ValueTask SetValueAsync<TValue>(string identifier, TValue value, CancellationToken cancellationToken) => SetValueAsync(identifier, value);

        public ValueTask<IJSObjectReference> InvokeConstructorAsync(string identifier, object?[]? args) => ValueTask.FromResult<IJSObjectReference>(this);

        public ValueTask<IJSObjectReference> InvokeConstructorAsync(string identifier, CancellationToken cancellationToken, object?[]? args) => InvokeConstructorAsync(identifier, args);

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
#endif

    [TestMethod]
    public void Wrapping_is_idempotent_and_disabled_sessions_return_the_original()
    {
        var (session, scope) = TestHelpers.CreateSession();
        using (scope)
        {
            var fake = new FakeJSRuntime();
            var once = fake.WithBevToolsTracking(session);
            var twice = once.WithBevToolsTracking(session);
            Assert.AreSame(fake, ((BevTrackingJSRuntime)twice).Inner);
        }

        var collection = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        collection.AddBitBevToolsServices(o => o.Enabled = false);
        using var provider = Microsoft.Extensions.DependencyInjection.ServiceCollectionContainerBuilderExtensions.BuildServiceProvider(collection);
        var disabled = (BevToolsSession)provider.GetService(typeof(BevToolsSession))!;
        var raw = new FakeJSRuntime();
        Assert.AreSame(raw, raw.WithBevToolsTracking(disabled));
    }
}
