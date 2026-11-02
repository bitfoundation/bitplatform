using System.Net;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.BevTools.Tests;

/// <summary>
/// Which component sent a request: the question a request per list row raises ("which list?") and the UI event cannot
/// answer, because a row loads its data in OnInitializedAsync, inside no event.
/// </summary>
[TestClass]
public class HttpCallerTests : BunitContext
{
    private sealed class OkHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") });
    }

    private void UseBevTools(bool captureCallers = true)
    {
        Services.AddBitBevToolsServices(o =>
        {
            o.Enabled = true;
            o.CaptureHttpCallers = captureCallers;
        });
        Services.AddScoped(sp => new CallerApi(new HttpClient(new BevToolsHttpMessageHandler(sp.GetRequiredService<BevToolsSession>()) { InnerHandler = new OkHandler() })
        {
            BaseAddress = new Uri("https://shop.test/"),
        }));
    }

    private BevToolsSession Session => Services.GetRequiredService<BevToolsSession>();

    [TestMethod]
    public void A_request_names_the_component_method_that_sent_it_and_the_service_in_between()
    {
        UseBevTools();
        var row = Render<CallerRow>(ps => ps.Add(p => p.Id, 1042));
        row.WaitForAssertion(() => Assert.IsTrue(row.Instance.Loaded));

        var request = Assert.ContainsSingle(Session.Http.Snapshot());
        Assert.AreEqual("CallerRow.OnInitializedAsync", request.CallerComponent, "the async method, named through its state machine, after an await");
        Assert.AreEqual("CallerApi.GetDetailsAsync", request.CallerVia);
        Assert.AreEqual("CallerRow.OnInitializedAsync via CallerApi.GetDetailsAsync", request.Caller);

        var endpoint = Assert.ContainsSingle(BevHttpEndpoints.Summarize(Session.Http.Snapshot()));
        Assert.AreEqual(("CallerRow.OnInitializedAsync", 1), Assert.ContainsSingle(endpoint.Callers));
        StringAssert.Contains(BevSessionExporter.ToJson(Session, new BevSessionExportOptions { Indented = false, Sections = new HashSet<string> { "http" } }),
            "\"caller\":\"CallerRow.OnInitializedAsync via CallerApi.GetDetailsAsync\"");
    }

    [TestMethod]
    public void A_local_function_is_named_by_itself_and_a_direct_call_has_no_via()
    {
        UseBevTools();
        var row = Render<LocalFunctionRow>();
        row.WaitForAssertion(() => Assert.IsTrue(row.Instance.Loaded));

        var request = Assert.ContainsSingle(Session.Http.Snapshot());
        Assert.AreEqual("LocalFunctionRow.Fetch", request.CallerComponent);
        Assert.IsNull(request.CallerVia);
    }

    [TestMethod]
    public void A_request_no_component_sent_names_none_and_the_option_turns_it_off()
    {
        UseBevTools(captureCallers: false);
        var row = Render<CallerRow>(ps => ps.Add(p => p.Id, 7));
        row.WaitForAssertion(() => Assert.IsTrue(row.Instance.Loaded));
        Services.GetRequiredService<CallerApi>().GetDetailsAsync(8).GetAwaiter().GetResult();

        Assert.IsTrue(Session.Http.Snapshot().All(r => r.Caller is null && r.CallerMissing is null), "not looked at, so no reason either");
    }

    [TestMethod]
    public void A_request_no_component_sent_says_why_it_names_none()
    {
        UseBevTools();
        Services.GetRequiredService<CallerApi>().GetDetailsAsync(8).GetAwaiter().GetResult();

        var request = Assert.ContainsSingle(Session.Http.Snapshot());
        Assert.IsNull(request.Caller);
        // Which reason depends on what ran the test: from a continuation (as MSTest runs it) it says so.
        StringAssert.StartsWith(request.CallerMissing, "no component on the stack");
        StringAssert.Contains(BevSessionExporter.ToJson(Session, new BevSessionExportOptions { Indented = false, Sections = new HashSet<string> { "http" } }),
            "\"callerMissing\":\"no component on the stack");
    }

    [TestMethod]
    public void A_continuation_that_another_components_completion_ran_is_not_that_components_request()
    {
        // WebAssembly has no synchronization context: completing a task runs what awaited it right there, so the code
        // below the send is whoever completed the task. Here a component opens a gate a service was waiting on.
        var previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(null);
        try
        {
            var gate = new TaskCompletionSource();
            var seen = GateWaiter.LookAfter(gate.Task);
            new GateOpener().Open(gate);

            Assert.IsTrue(seen.IsCompleted);
            Assert.IsNull(seen.Result.Component, seen.Result.Component);
            Assert.AreEqual(BevCallSites.AfterCompletion, seen.Result.Missing);

            // The control: called by the component directly, it is the component's.
            Assert.AreEqual("GateOpener.Look", new GateOpener().Look().Component);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }

    [TestMethod]
    public void An_endpoint_counts_every_sender_although_it_lists_the_first_five()
    {
        var records = Enumerable.Range(0, 7).Select(i => new BevHttpRecord
        {
            Id = i + 1, StartedAt = DateTimeOffset.UtcNow, Method = "GET", Url = "/api/orders/" + (1001 + i), StatusCode = 200, DurationMs = 5,
            CallerComponent = "Row" + i + ".OnInitializedAsync",
        }).ToArray();

        var endpoint = Assert.ContainsSingle(BevHttpEndpoints.Summarize(records));
        Assert.HasCount(5, endpoint.Callers);
        Assert.AreEqual(7, endpoint.CallerCount);
    }

    [TestMethod]
    public void Every_attempt_of_a_retried_request_names_its_sender_although_the_retry_handler_awaited_first()
    {
        // The recorder sits innermost, so it sees each attempt - on the thread pool once a resilience handler awaited,
        // where the component is no longer on the stack. The sender is read at the outer end, once per request.
        var attempts = 0;
        Services.AddBitBevToolsServices(o => o.Enabled = true);
        Services.AddHttpClient<FactoryCallerApi>(c => c.BaseAddress = new Uri("https://shop.test/"))
            .AddHttpMessageHandler(() => new AwaitingRetryHandler())
            .ConfigurePrimaryHttpMessageHandler(() => new StubHandler(() => Interlocked.Increment(ref attempts) == 1 ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK));

        var row = Render<FactoryCallerRow>();
        row.WaitForAssertion(() => Assert.IsTrue(row.Instance.Loaded));

        var records = Session.Http.Snapshot();
        CollectionAssert.AreEqual(new int?[] { 503, 200 }, records.Select(r => r.StatusCode).ToArray());
        Assert.IsTrue(records.All(r => r.Caller == "FactoryCallerRow.OnInitializedAsync via FactoryCallerApi.GetAsync"), string.Join(" | ", records.Select(r => r.Caller ?? "(none)")));
    }

    /// <summary>Behaves like a resilience handler: waits on the thread pool before each attempt and retries a 503 once.</summary>
    private sealed class AwaitingRetryHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(1, cancellationToken).ConfigureAwait(false);
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (response.StatusCode != HttpStatusCode.ServiceUnavailable)
            {
                return response;
            }

            await Task.Delay(1, cancellationToken).ConfigureAwait(false);
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }

    private sealed class StubHandler(Func<HttpStatusCode> status) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(status()) { Content = new StringContent("{}") });
    }

    [TestMethod]
    public void The_per_item_finding_says_which_component_sends_one_request_per_row()
    {
        UseBevTools();
        var start = DateTimeOffset.UtcNow.AddSeconds(-2);
        for (var i = 0; i < 6; i++)
        {
            Session.Http.Add(new BevHttpRecord
            {
                Id = Session.Http.NextId(), StartedAt = start.AddMilliseconds(i * 3), Method = "GET", Url = "/api/orders/" + (1001 + i), StatusCode = 200, DurationMs = 20,
                CallerComponent = "OrderRow.OnInitializedAsync", CallerVia = "ProductApi.GetOrderDetailsAsync",
            });
        }

        Session.Diagnostics.Evaluate(force: true);

        var finding = Assert.ContainsSingle(f => f.Diagnostic.RuleId == "http.per-item", Session.Diagnostics.Findings).Diagnostic;
        StringAssert.Contains(finding.Evidence, "sent from OrderRow.OnInitializedAsync via ProductApi.GetOrderDetailsAsync ×6");
    }
}

public sealed class CallerApi(HttpClient client)
{
    public async Task<string> GetDetailsAsync(int id) => await client.GetStringAsync("api/orders/" + id);
}

internal static class GateWaiter
{
    // A service method awaiting a gate, then reading its call site: what an HTTP send at that point would get.
    internal static async Task<BevCallSites.CallSite> LookAfter(Task gate)
    {
        await gate;
        return BevCallSites.Find(skipFrames: 0);
    }
}

public class GateOpener : ComponentBase
{
    public void Open(TaskCompletionSource gate) => gate.SetResult();

    internal BevCallSites.CallSite Look() => BevCallSites.Find(skipFrames: 0);
}

public sealed class FactoryCallerApi(HttpClient client)
{
    public async Task<HttpStatusCode> GetAsync() => (await client.GetAsync("api/orders/7")).StatusCode;
}

public class FactoryCallerRow : ComponentBase
{
    [Inject] public FactoryCallerApi Api { get; set; } = default!;

    public bool Loaded { get; private set; }

    protected override async Task OnInitializedAsync()
    {
        await Api.GetAsync();
        Loaded = true;
    }
}

public class CallerRow : ComponentBase
{
    [Inject] public CallerApi Api { get; set; } = default!;

    [Parameter] public int Id { get; set; }

    public bool Loaded { get; private set; }

    protected override async Task OnInitializedAsync()
    {
        // After an await, as real components load: the state machine's MoveNext is on the stack again.
        await Task.Yield();
        await Api.GetDetailsAsync(Id);
        Loaded = true;
    }
}

public class LocalFunctionRow : ComponentBase
{
    [Inject] public CallerApi Api { get; set; } = default!;

    [Inject] public IServiceProvider Services { get; set; } = default!;

    public bool Loaded { get; private set; }

    protected override async Task OnInitializedAsync()
    {
        await Fetch();
        Loaded = true;

        async Task Fetch()
        {
            var client = new HttpClient(new BevToolsHttpMessageHandler(Services.GetRequiredService<BevToolsSession>()) { InnerHandler = new OkLocalHandler() });
            await client.GetStringAsync("https://shop.test/api/orders/1");
        }
    }

    private sealed class OkLocalHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") });
    }
}
