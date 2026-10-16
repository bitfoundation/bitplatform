using System.Net;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Bit.BevTools.Tests;

/// <summary>
/// Pausing exists so a developer can read a buffer without the application overwriting it. Every capture path must
/// respect the flag, the application must keep working exactly as before, and errors must never be dropped.
/// </summary>
[TestClass]
public class CaptureControlTests : BunitContext
{
    private readonly FakeJSRuntime _js = new();

    public CaptureControlTests()
    {
        Services.AddSingleton<IJSRuntime>(_js);
        Services.AddBitBevToolsServices(o => o.Enabled = true);
    }

    private BevToolsSession Session => Services.GetRequiredService<BevToolsSession>();

    [TestMethod]
    public void Paused_renders_are_not_recorded_but_still_count_as_having_rendered()
    {
        var cut = Render<ParentComponent>();
        var parent = Session.Components.All.Single(r => r.Type == typeof(ParentComponent));
        var eventsBefore = Session.Timeline.Count;

        Session.PauseCapture();
        Assert.IsFalse(Session.IsCapturing);
        cut.InvokeAsync(() => cut.Instance.Refresh());
        cut.InvokeAsync(() => cut.Instance.Refresh());

        Assert.AreEqual(1, parent.RenderCount);
        Assert.AreEqual(2, parent.SkippedRenders);
        Assert.AreEqual(3, cut.Instance.RenderCalls);
        // Exactly one new row: the pause marker itself.
        Assert.AreEqual(eventsBefore + 1, Session.Timeline.Count);
        Assert.AreEqual("Capture paused", Session.Timeline.Snapshot()[^1].Title);

        Session.ResumeCapture();
        Assert.IsTrue(Session.IsCapturing);
        var resumed = Session.Timeline.Snapshot()[^1];
        Assert.AreEqual("Capture resumed", resumed.Title);
        Assert.Contains("2 component renders happened meanwhile", resumed.Detail!, StringComparison.Ordinal);

        cut.InvokeAsync(() => cut.Instance.Refresh());
        Assert.AreEqual(2, parent.RenderCount);
        // Not "Initial": the instance rendered while paused, and that must not be forgotten.
        Assert.AreEqual(BevRenderCause.StateHasChanged, parent.LastRender!.Cause);
    }

    [TestMethod]
    public void Pause_and_resume_are_idempotent()
    {
        Session.PauseCapture();
        Session.PauseCapture();
        Session.ResumeCapture();
        Session.ResumeCapture();

        var markers = Session.Timeline.Snapshot().Where(e => e.Category == "bevtools").Select(e => e.Title).ToList();
        Assert.AreSequenceEqual(["Capture paused", "Capture resumed"], markers);
        Assert.IsNull(Session.PausedAt);
    }

    [TestMethod]
    public async Task Http_requests_pass_through_untouched_while_paused()
    {
        var handler = new BevToolsHttpMessageHandler(Session) { InnerHandler = new StubHandler(new HttpResponseMessage(HttpStatusCode.OK)) };
        using var client = new HttpClient(handler);

        Session.PauseCapture();
        var response = await client.GetAsync("https://example.test/paused");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsEmpty(Session.Http.Snapshot());

        Session.ResumeCapture();
        await client.GetAsync("https://example.test/resumed");
        Assert.Contains("resumed", Assert.ContainsSingle(Session.Http.Snapshot()).Url, StringComparison.Ordinal);
    }

    [TestMethod]
    public void Js_interop_calls_go_through_but_are_not_recorded_while_paused()
    {
        Render<ParentComponent>();
        var child = Session.Components.All.Single(r => r.Type == typeof(ChildComponent));
        var runtime = ((ChildComponent)child.Component!).JS!;

        Session.PauseCapture();
        runtime.InvokeVoidAsync("whilePaused").GetAwaiter().GetResult();
        Assert.ContainsSingle(_js.Calls, "whilePaused");
        Assert.IsEmpty(Session.Interop.Snapshot());

        Session.ResumeCapture();
        runtime.InvokeVoidAsync("afterResume").GetAwaiter().GetResult();
        Assert.AreEqual("afterResume", Assert.ContainsSingle(Session.Interop.Snapshot()).Identifier);
    }

    [TestMethod]
    public void Errors_are_still_collected_while_paused_and_say_they_are_not_in_the_timeline()
    {
        Session.PauseCapture();
        var error = Session.Errors.Record(new InvalidOperationException("still important"), "test");

        Assert.ContainsSingle(Session.Errors.Snapshot());
        Assert.AreEqual(-1, error.TimelineEventId);
        Assert.DoesNotContain(e => e.Kind == BevToolsEventKind.Error, Session.Timeline.Snapshot());
    }

    [TestMethod]
    public void State_changes_while_paused_are_skipped_and_the_next_diff_spans_the_pause()
    {
        var state = new StateStoreTests.CounterState();
        Session.State.Attach(state, "test");

        state.Increment();
        Session.PauseCapture();
        state.Increment();
        state.Increment();
        Session.ResumeCapture();
        state.Increment();

        var entry = Assert.ContainsSingle(Session.State.Entries);
        Assert.AreEqual(2, entry.ChangeCount);
        var afterResume = entry.Changes[^1];
        Assert.Contains(c => c.Path == "Count" && c.Before == "1" && c.After == "4", afterResume.Changes);
    }

    [TestMethod]
    public async Task Failed_http_requests_still_reach_the_error_list_while_paused()
    {
        using var failing = new HttpMessageInvoker(new BevToolsHttpMessageHandler(Session) { InnerHandler = new StubHandler(new HttpResponseMessage(HttpStatusCode.InternalServerError)) });
        using var throwing = new HttpMessageInvoker(new BevToolsHttpMessageHandler(Session) { InnerHandler = new ThrowingHandler(new HttpRequestException("connection refused")) });
        using var cancelled = new HttpMessageInvoker(new BevToolsHttpMessageHandler(Session) { InnerHandler = new ThrowingHandler(new TaskCanceledException()) });

        Session.PauseCapture();
        var response = await failing.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://example.test/api/items?token=s3cr3t"), CancellationToken.None);
        await Assert.ThrowsExactlyAsync<HttpRequestException>(() => throwing.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://example.test/down"), CancellationToken.None));
        await Assert.ThrowsExactlyAsync<TaskCanceledException>(() => cancelled.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://example.test/slow"), CancellationToken.None));

        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        // The network log and the timeline stay frozen; the error list does not.
        Assert.IsEmpty(Session.Http.Snapshot());
        Assert.DoesNotContain(e => e.Kind is BevToolsEventKind.Http or BevToolsEventKind.Error, Session.Timeline.Snapshot());
        var errors = Session.Errors.Snapshot();
        Assert.AreEqual(2, errors.Length);
        var status = Assert.ContainsSingle(e => e.ExceptionType == "HttpStatus500", errors);
        Assert.StartsWith("GET /api/items?token=", status.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("s3cr3t", status.Message, StringComparison.Ordinal);
        Assert.ContainsSingle(e => e.ExceptionType == typeof(HttpRequestException).FullName, errors);
    }

#if NET10_0_OR_GREATER
    // UI events are observed through the framework's HandleEvent activity, which .NET 10 introduced.
    /// <summary>
    /// The click is not in the timeline while paused, so nothing may point at it. It used to be attached to the
    /// activity anyway, with id 0, and became the "preceding event" of every error raised inside the handler.
    /// </summary>
    [TestMethod]
    public void A_ui_event_that_starts_while_paused_is_never_used_as_a_trigger()
    {
        var cut = Render<ClickCallback>();
        var clicks = 0;
        BevErrorRecord? error = null;
        cut.Instance.OnClicked = () => error = Session.Errors.Record(new InvalidOperationException("click " + ++clicks), "test");

        Session.PauseCapture();
        cut.Find("button").Click();

        Assert.IsNotNull(error);
        Assert.IsNull(error!.PrecedingEventId);
        Assert.IsNull(Session.LastUiEventId);
        Assert.DoesNotContain(e => e.Kind == BevToolsEventKind.UiEvent, Session.Timeline.Snapshot());

        Session.ResumeCapture();
        cut.Find("button").Click();

        var click = Assert.ContainsSingle(e => e.Kind == BevToolsEventKind.UiEvent, Session.Timeline.Snapshot());
        Assert.AreEqual(click.Id, Session.LastUiEventId);
        Assert.AreEqual(click.Id, error!.PrecedingEventId);
    }
#endif

    /// <summary>Disposal detection must use "has rendered", like cause detection, or a paused-only component lingers for 5 s.</summary>
    [TestMethod]
    public void A_component_that_only_rendered_while_paused_is_marked_disposed_as_soon_as_it_is_removed()
    {
        var cut = Render<ParentComponent>(ps => ps.Add(p => p.ShowChild, false));

        Session.PauseCapture();
        cut.Render(ps => ps.Add(p => p.ShowChild, true));
        var child = Session.Components.All.Single(r => r.Type == typeof(ChildComponent));
        Assert.AreEqual(0, child.RenderCount);
        Assert.AreEqual(1, child.SkippedRenders);

        cut.Render(ps => ps.Add(p => p.ShowChild, false));
        Session.Components.Refresh(force: true);

        Assert.IsTrue(child.IsDisposed);
    }

    public sealed class ClickCallback : ComponentBase
    {
        public Action? OnClicked { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "button");
            builder.AddAttribute(1, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => OnClicked?.Invoke()));
            builder.CloseElement();
        }
    }

    private sealed class StubHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(response);
    }

    private sealed class ThrowingHandler(Exception exception) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromException<HttpResponseMessage>(exception);
    }
}
