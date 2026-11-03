using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.BevTools.Tests;

/// <summary>
/// What ties a moment in the app to something outside it: the trace id a request carried to the server, a task's
/// exception nobody awaited, a marker a developer put in the timeline.
/// </summary>
[TestClass]
public class CorrelationTests
{
    /// <summary>Writes traceparent below BevTools' handler, as HttpClient's diagnostics handler does in the primary handler.</summary>
    private sealed class PropagatingHandler(string? traceparent, HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (traceparent is not null)
            {
                request.Headers.TryAddWithoutValidation("traceparent", traceparent);
            }

            return Task.FromResult(new HttpResponseMessage(status));
        }
    }

    private static async Task<BevHttpRecord> Send(BevToolsSession session, string? traceparent)
    {
        using var client = new HttpClient(new BevToolsHttpMessageHandler(session) { InnerHandler = new PropagatingHandler(traceparent) });
        await client.GetAsync("https://api.test/orders/1");
        return session.Http.Snapshot().Last();
    }

    [TestMethod]
    public async Task A_request_records_the_trace_it_carried_to_the_server()
    {
        var (session, scope) = TestHelpers.CreateSession();
        using (scope)
        {
            var record = await Send(session, "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01");
            Assert.AreEqual("4bf92f3577b34da6a3ce929d0e0e4736", record.TraceId);
            Assert.AreEqual("00f067aa0ba902b7", record.SpanId);

            Assert.IsNull((await Send(session, null)).TraceId, "a client that propagates nothing sent no trace");
            Assert.IsNull((await Send(session, "00-00000000000000000000000000000000-00f067aa0ba902b7-01")).TraceId, "an all-zero trace id is invalid");
            Assert.IsNull((await Send(session, "garbage")).TraceId);

            using var json = JsonDocument.Parse(BevSessionExporter.ToJson(session, new BevSessionExportOptions { Sections = new HashSet<string> { "http" } }));
            Assert.AreEqual("4bf92f3577b34da6a3ce929d0e0e4736", json.RootElement.GetProperty("http")[0].GetProperty("traceId").GetString());
        }
    }

    [TestMethod]
    public void An_unobserved_task_exception_is_an_error_unless_bevtools_started_the_task()
    {
        var (session, scope) = TestHelpers.CreateSession();
        using (scope)
        {
            var appTask = Task.Run(() => throw new InvalidOperationException("Saving the draft failed"));
            // An async method of BevTools' own that faults: its task is BevTools'.
            var ownTask = BevRenderProfile.ParseAsync(new MemoryStream("not json"u8.ToArray()));
            try
            {
                Task.WaitAll(appTask, ownTask);
            }
            catch (AggregateException)
            {
            }

            BevUnobservedTasks.Record(session, appTask.Exception);
            BevUnobservedTasks.Record(session, ownTask.Exception);

            // An application's fire-and-forget call that went through BevTools' tracked IJSRuntime is still the application's.
            var js = new FakeJSRuntime { Handler = (_, _) => throw new InvalidOperationException("JS failed") }.WithBevToolsTracking(session);
            var viaWrapper = Microsoft.JSInterop.JSRuntimeExtensions.InvokeVoidAsync(js, "app.save").AsTask();
            try
            {
                viaWrapper.Wait();
            }
            catch (AggregateException)
            {
            }

            Assert.IsFalse(BevUnobservedTasks.IsBevToolsOwn(viaWrapper.Exception!.InnerException!), viaWrapper.Exception.InnerException!.StackTrace);
            Assert.IsTrue(BevUnobservedTasks.IsBevToolsOwn(ownTask.Exception!.InnerException!));

            // The tracked runtime recorded the failed JS call itself; the unobserved task is one error of its own.
            var error = Assert.ContainsSingle(e => e.Source == "task", session.Errors.Snapshot());
            Assert.AreEqual("A task's exception was never observed (fire-and-forget): Saving the draft failed", error.Message);
            Assert.AreEqual(typeof(InvalidOperationException).FullName, error.ExceptionType);
        }
    }

    [TestMethod]
    public void A_marker_is_a_timeline_event_from_the_api_the_palette_and_the_protocol()
    {
        var (session, scope) = TestHelpers.CreateSession();
        using (scope)
        {
            var id = session.AddMarker("  before the fix\n(second line) ");
            var marker = session.Timeline.Find(id)!;
            Assert.AreEqual("marker", marker.Category);
            Assert.AreEqual("⚑ before the fix (second line)", marker.Title);

            var answer = JsonDocument.Parse(BevToolsProtocol.Handle(session, "command", """{"name":"mark","argument":"after the fix"}""")).RootElement;
            Assert.AreEqual("Marker added", answer.GetProperty("done").GetString());
            Assert.AreEqual("⚑ after the fix", session.Timeline.Snapshot().Last().Title);

            Assert.AreEqual("⚑ Marker", session.Timeline.Find(session.AddMarker(""))!.Title);
            session.PauseCapture();
            Assert.AreEqual(-1, session.AddMarker("while paused"));
        }
    }
}

[TestClass]
public class MarkerPanelTests : BunitContext
{
    public MarkerPanelTests()
    {
        Services.AddBitBevToolsServices(o =>
        {
            o.Enabled = true;
            o.Ui.OpenByDefault = true;
        });
        JSInterop.Mode = JSRuntimeMode.Loose;
        this.UseRenderer("Server");
    }

    [TestMethod]
    public void The_timeline_toolbar_adds_numbered_markers()
    {
        var panel = Render<BevToolsPanel>();
        panel.FindAll(".bev-tab").First(t => t.TextContent.Trim().StartsWith("Timeline", StringComparison.Ordinal)).Click();
        panel.FindAll(".bev-toolbar button").First(b => b.TextContent.Contains("Mark", StringComparison.Ordinal)).Click();
        panel.FindAll(".bev-toolbar button").First(b => b.TextContent.Contains("Mark", StringComparison.Ordinal)).Click();

        var titles = Services.GetRequiredService<BevToolsSession>().Timeline.Snapshot().Where(e => e.Category == "marker").Select(e => e.Title).ToArray();
        CollectionAssert.AreEqual(new[] { "⚑ Marker 1", "⚑ Marker 2" }, titles);
        StringAssert.Contains(panel.Markup, "⚑ Marker 2");
    }
}
