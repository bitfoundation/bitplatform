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
