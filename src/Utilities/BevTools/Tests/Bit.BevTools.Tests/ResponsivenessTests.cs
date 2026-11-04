using System.Diagnostics;
using System.Text.Json;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.BevTools.Tests;

/// <summary>
/// The page's responsiveness as the browser measured it: interactions to the next paint and frames it could not paint
/// on time, linked on WebAssembly - where .NET runs on the browser's thread - to the work that ran inside them.
/// </summary>
[TestClass]
public class ResponsivenessTests
{
    private static BevInteractionPayload Interaction(double ageMs, double durationMs, double inputDelayMs, double processingMs, string name = "click", string? target = "<button#save>") => new()
    {
        AgeMs = ageMs,
        Name = name,
        DurationMs = durationMs,
        InputDelayMs = inputDelayMs,
        ProcessingMs = processingMs,
        PresentationMs = durationMs - inputDelayMs - processingMs,
        ProcessingStartAgeMs = ageMs - inputDelayMs,
        ProcessingEndAgeMs = ageMs - inputDelayMs - processingMs,
        Target = target,
    };

    private static long TicksAgo(double ms) => Stopwatch.GetTimestamp() - (long)(ms / 1000 * Stopwatch.Frequency);

    [TestMethod]
    public void Interactions_and_long_frames_are_recorded_with_their_parts_and_the_session_INP()
    {
        var (session, scope) = TestHelpers.CreateSession();
        using (scope)
        {
            session.Platform = "Server";
            var bridge = new BevJsBridge(session, () => { });
            bridge.ReportResponsiveness(
                [new BevLongFramePayload { AgeMs = 900, DurationMs = 240, BlockingMs = 190, Scripts = [new BevFrameScript { Invoker = "BUTTON#save.onclick", Type = "event-listener", Source = "app.js", Fn = "save", DurationMs = 200 }] }],
                [Interaction(800, 30, 2, 20), Interaction(500, 320, 10, 280)]);

            var r = session.Browser.Responsiveness;
            Assert.AreEqual(2, r.InteractionCount);
            Assert.AreEqual(320, r.SlowestInteraction!.DurationMs);
            Assert.AreEqual(320, r.InteractionToNextPaintMs);
            Assert.AreEqual(1, r.LongFrameCount);
            Assert.AreEqual(190, r.TotalBlockingMs);
            Assert.IsNull(r.Interactions[1].HandlerTitle, "on a circuit nothing is matched: its handlers run on the server");

            var events = session.Timeline.Snapshot();
            var frame = Assert.ContainsSingle(e => e.Category == "long-frame", events);
            Assert.AreEqual(240, frame.DurationMs);
            StringAssert.Contains(frame.Detail, "BUTTON#save.onclick (app.js save)");
            Assert.AreEqual(BevToolsSeverity.Warning, frame.Severity, "190 ms blocking is over the 150 ms threshold");
            var interaction = Assert.ContainsSingle(e => e.Category == "interaction", events, "a 30 ms interaction is counted, not listed");
            Assert.AreEqual("click on <button#save>: next paint after 320.0 ms", interaction.Title);
            Assert.AreEqual("input delay 10.0 ms · processing 280.0 ms · presentation 30.0 ms", interaction.Detail);
            Assert.IsTrue(interaction.Timestamp < DateTimeOffset.UtcNow.AddMilliseconds(-400), "dated by its age");
        }
    }

    [TestMethod]
    public void On_webassembly_the_handler_that_ran_inside_an_interaction_and_the_work_inside_a_frame_are_named()
    {
        var (session, scope) = TestHelpers.CreateSession();
        using (scope)
        {
            session.Platform = "WebAssembly";
            var click = session.Timeline.Record(new BevToolsEvent { Kind = BevToolsEventKind.UiEvent, Category = "ui-event", Title = "onclick → Orders.Load", StartTicks = TicksAgo(490) });
            session.Timeline.Record(new BevToolsEvent { Kind = BevToolsEventKind.RenderBatch, Category = "batch", Title = "Batch #7: 120 components", StartTicks = TicksAgo(300) });
            session.Timeline.Record(new BevToolsEvent { Kind = BevToolsEventKind.UiEvent, Category = "ui-event", Title = "oninput → Search.OnQuery", StartTicks = TicksAgo(5000) });

            var bridge = new BevJsBridge(session, () => { });
            bridge.ReportResponsiveness([new BevLongFramePayload { AgeMs = 500, DurationMs = 260, BlockingMs = 210 }], [Interaction(500, 320, 5, 300)]);

            var interaction = session.Browser.Responsiveness.Interactions.Single();
            Assert.AreEqual(click, interaction.HandlerEventId);
            Assert.AreEqual("onclick → Orders.Load", interaction.HandlerTitle);
            Assert.AreEqual(click, session.Timeline.Find(interaction.TimelineEventId)!.ParentEventId);

            var frame = session.Browser.Responsiveness.Frames.Single();
            CollectionAssert.AreEqual(new[] { "onclick → Orders.Load", "Batch #7: 120 components" }, frame.During.ToArray());
            Assert.AreEqual(click, frame.TriggerEventId);
            StringAssert.StartsWith(BevResponsiveness.Describe(frame), "during onclick → Orders.Load, Batch #7: 120 components");
        }
    }

    [TestMethod]
    public void A_long_frame_mostly_spent_rendering_bevtools_own_panel_is_not_the_applications()
    {
        var (session, scope) = TestHelpers.CreateSession();
        using (scope)
        {
            session.Platform = "WebAssembly";
            // The panel rendered for 200 ms of a 260 ms frame, and for 20 ms of another.
            session.RenderTracker.NoteOwnRender(TicksAgo(1000), TicksAgo(800));
            session.RenderTracker.NoteOwnRender(TicksAgo(400), TicksAgo(380));
            new BevJsBridge(session, () => { }).ReportResponsiveness(
                [new BevLongFramePayload { AgeMs = 1010, DurationMs = 260, BlockingMs = 210 }, new BevLongFramePayload { AgeMs = 410, DurationMs = 300, BlockingMs = 250 }],
                []);

            var responsiveness = session.Browser.Responsiveness;
            var frame = Assert.ContainsSingle(responsiveness.Frames, "the panel's own frame is not recorded");
            Assert.IsTrue(frame.OwnMs is > 10 and < 35, "about 20 ms: " + frame.OwnMs);
            Assert.IsFalse(frame.IsMostlyBevTools);
            StringAssert.StartsWith(BevResponsiveness.Describe(frame), BevFmt.Ms(frame.OwnMs) + " of it BevTools' own panel rendering");
            Assert.AreEqual(1, responsiveness.LongFrameCount, "nor counted");
            Assert.AreEqual(250, responsiveness.TotalBlockingMs);
            Assert.AreEqual(300, responsiveness.LongestFrameMs);
            Assert.IsFalse(session.Timeline.Snapshot().Any(e => e.Category == "long-frame" && e.DurationMs == 260), "nor in the timeline");

            session.Diagnostics.Evaluate(force: true);
            var blocked = Assert.ContainsSingle(f => f.Diagnostic.RuleId == "browser.long-frame", session.Diagnostics.Findings).Diagnostic;
            Assert.AreEqual("The main thread was blocked for 250 ms in 1 frame over the last minute", blocked.Title, "the panel's own frame left out");
        }
    }

    [TestMethod]
    public void An_interaction_two_reports_carried_is_one_interaction()
    {
        var (session, scope) = TestHelpers.CreateSession();
        using (scope)
        {
            session.Platform = "Server";
            var bridge = new BevJsBridge(session, () => { });
            // The pointerdown reported before its click was: the same interaction id, the same first input.
            var down = Interaction(500, 60, 2, 8, name: "pointerdown");
            down.InteractionId = 7;
            bridge.ReportResponsiveness([], [down]);
            var click = Interaction(500, 320, 5, 250, name: "click");
            click.InteractionId = 7;
            bridge.ReportResponsiveness([], [click]);

            var r = session.Browser.Responsiveness;
            Assert.AreEqual(1, r.InteractionCount, "counted once");
            var whole = Assert.ContainsSingle(r.Interactions);
            Assert.AreEqual("click", whole.Name, "named after the part whose handlers took longest");
            Assert.AreEqual(320, whole.DurationMs, 2);
            Assert.AreEqual(2, whole.InputDelayMs, 2);
            Assert.AreEqual(253, whole.ProcessingMs, 2);
            Assert.AreSame(whole, r.SlowestInteraction);
            Assert.AreEqual(320, r.InteractionToNextPaintMs!.Value, 2);
            var events = session.Timeline.Snapshot().Where(e => e.Category == "interaction").ToArray();
            var evt = Assert.ContainsSingle(events, "one event for the interaction, the first part's replaced");
            Assert.AreEqual(whole.TimelineEventId, evt.Id);
            StringAssert.StartsWith(evt.Title, "click on <button#save>: next paint after 32");

            // Without an id nothing says two entries are one.
            bridge.ReportResponsiveness([], [Interaction(300, 40, 1, 10), Interaction(200, 40, 1, 10)]);
            Assert.AreEqual(3, r.InteractionCount);
        }
    }

    [TestMethod]
    public void A_frame_around_more_panel_renders_than_are_kept_says_its_own_time_is_the_least_it_spent()
    {
        var (session, scope) = TestHelpers.CreateSession();
        using (scope)
        {
            session.Platform = "WebAssembly";
            // 300 renders of 0.5 ms inside the frame - 150 ms of its 260 - of which the newest 256 are kept: 128 ms known.
            for (var i = 0; i < 300; i++)
            {
                session.RenderTracker.NoteOwnRender(TicksAgo(1000 - i * 0.6), TicksAgo(1000 - i * 0.6 - 0.5));
            }

            new BevJsBridge(session, () => { }).ReportResponsiveness([new BevLongFramePayload { AgeMs = 1010, DurationMs = 260, BlockingMs = 210 }], []);

            var frame = Assert.ContainsSingle(session.Browser.Responsiveness.Frames, "not shown to be the panel's, so recorded");
            Assert.IsTrue(frame.OwnMsIsLowerBound);
            Assert.IsFalse(frame.IsMostlyBevTools);
            StringAssert.StartsWith(BevResponsiveness.Describe(frame), "at least " + BevFmt.Ms(frame.OwnMs) + " of it BevTools' own panel rendering");
        }
    }

    [TestMethod]
    public void A_frame_without_scripts_says_so_and_nothing_about_the_browser()
    {
        var frame = new BevLongFrame(DateTimeOffset.UtcNow, 120, 70, []);
        Assert.AreEqual("no script attribution", BevResponsiveness.Describe(frame));
    }

    [TestMethod]
    public void Two_handlers_inside_one_interaction_name_neither()
    {
        var (session, scope) = TestHelpers.CreateSession();
        using (scope)
        {
            session.Platform = "WebAssembly";
            session.Timeline.Record(new BevToolsEvent { Kind = BevToolsEventKind.UiEvent, Title = "onpointerdown → A.Down", StartTicks = TicksAgo(480) });
            session.Timeline.Record(new BevToolsEvent { Kind = BevToolsEventKind.UiEvent, Title = "onclick → A.Click", StartTicks = TicksAgo(400) });
            new BevJsBridge(session, () => { }).ReportResponsiveness([], [Interaction(500, 320, 5, 300)]);

            Assert.IsNull(session.Browser.Responsiveness.Interactions.Single().HandlerEventId);
        }
    }

    [TestMethod]
    public void Slow_interactions_and_blocked_frames_are_findings_that_say_which_part_was_slow()
    {
        var (session, scope) = TestHelpers.CreateSession();
        using (scope)
        {
            session.Platform = "WebAssembly";
            var bridge = new BevJsBridge(session, () => { });
            bridge.ReportResponsiveness(
                [new BevLongFramePayload { AgeMs = 900, DurationMs = 120, BlockingMs = 70 }, new BevLongFramePayload { AgeMs = 700, DurationMs = 640, BlockingMs = 590 }],
                [Interaction(800, 150, 5, 100), Interaction(600, 420, 380, 20), Interaction(400, 260, 10, 30, "keydown", "<input.demo-search>")]);
            session.Diagnostics.Evaluate(force: true);

            var findings = session.Diagnostics.Findings.Select(f => f.Diagnostic).ToList();
            var click = Assert.ContainsSingle(f => f.RuleId == "browser.slow-interaction" && f.Title.StartsWith("click", StringComparison.Ordinal), findings);
            Assert.AreEqual("click on <button#save>: next paint after 420 ms", click.Title, "the 150 ms click is under the threshold");
            StringAssert.Contains(click.Evidence, "Mostly input delay.");
            var key = Assert.ContainsSingle(f => f.RuleId == "browser.slow-interaction" && f.Title.StartsWith("keydown", StringComparison.Ordinal), findings);
            StringAssert.Contains(key.Evidence, "Mostly presentation.");

            var blocked = Assert.ContainsSingle(f => f.RuleId == "browser.long-frame", findings);
            Assert.AreEqual("The main thread was blocked for 590 ms in 1 frame over the last minute", blocked.Title);
            Assert.AreEqual(BevDiagnosticSeverity.Error, blocked.Severity);
            StringAssert.Contains(blocked.Suggestion, "Task.Yield");
        }
    }

    [TestMethod]
    public void Nothing_is_recorded_while_paused_or_when_turned_off_and_bad_payloads_are_dropped()
    {
        var (session, scope) = TestHelpers.CreateSession();
        using (scope)
        {
            var bridge = new BevJsBridge(session, () => { });
            session.PauseCapture();
            bridge.ReportResponsiveness([new BevLongFramePayload { AgeMs = 10, DurationMs = 300, BlockingMs = 250 }], [Interaction(10, 300, 1, 200)]);
            Assert.AreEqual(0, session.Browser.Responsiveness.InteractionCount);
            session.ResumeCapture();

            bridge.ReportResponsiveness(
                [new BevLongFramePayload { AgeMs = double.NaN, DurationMs = 300 }, new BevLongFramePayload { AgeMs = 10, DurationMs = double.PositiveInfinity }],
                [new BevInteractionPayload { AgeMs = 10, DurationMs = -5 }, Interaction(10, 300, 1, 200, name: new string('x', 500), target: "<div token=eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxMjMifQ.c2lnbmF0dXJlLXZhbHVl>")]);
            var interaction = Assert.ContainsSingle(session.Browser.Responsiveness.Interactions);
            Assert.AreEqual(40, interaction.Name.Length);
            Assert.DoesNotContain("eyJ", interaction.Target!, StringComparison.Ordinal);
            Assert.AreEqual(0, session.Browser.Responsiveness.LongFrameCount);
        }

        (session, scope) = TestHelpers.CreateSession(o => o.TrackResponsiveness = false);
        using (scope)
        {
            new BevJsBridge(session, () => { }).ReportResponsiveness([], [Interaction(10, 300, 1, 200)]);
            Assert.AreEqual(0, session.Browser.Responsiveness.InteractionCount);
        }
    }

    [TestMethod]
    public void INP_ignores_one_outlier_in_every_fifty_interactions()
    {
        var r = new BevResponsiveness();
        for (var i = 1; i <= 100; i++)
        {
            r.Add(new BevInteraction(DateTimeOffset.UtcNow, "click", i, 0, i, 0, null));
        }

        Assert.AreEqual(98, r.InteractionToNextPaintMs);
        Assert.AreEqual(100, r.SlowestInteraction!.DurationMs);
    }

    [TestMethod]
    public void Exports_carry_the_summary_and_the_recent_entries()
    {
        var (session, scope) = TestHelpers.CreateSession();
        using (scope)
        {
            new BevJsBridge(session, () => { }).ReportResponsiveness([new BevLongFramePayload { AgeMs = 100, DurationMs = 90, BlockingMs = 40 }], [Interaction(50, 210, 5, 180)]);
            using var json = JsonDocument.Parse(BevSessionExporter.ToJson(session));
            var r = json.RootElement.GetProperty("browser").GetProperty("responsiveness");
            Assert.AreEqual(1, r.GetProperty("interactions").GetInt32());
            Assert.AreEqual(210, r.GetProperty("interactionToNextPaintMs").GetDouble());
            Assert.AreEqual(40, r.GetProperty("totalBlockingMs").GetDouble());
            Assert.AreEqual("<button#save>", r.GetProperty("recentInteractions")[0].GetProperty("target").GetString());
            Assert.IsTrue(json.RootElement.GetProperty("options").GetProperty("trackResponsiveness").GetBoolean());
        }
    }
}

[TestClass]
public class ResponsivenessPanelTests : BunitContext
{
    public ResponsivenessPanelTests()
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
    public void The_browser_panel_shows_the_slow_interactions_and_long_frames()
    {
        var module = JSInterop.SetupModule("./_content/Bit.BevTools/bit-bevtools.js");
        module.Setup<BevBrowserSnapshot>("attach", _ => true).SetResult(new BevBrowserSnapshot
        {
            Online = true,
            Responsiveness = new BevResponsivenessSupport { EventTiming = true, LongTask = true },
        });
        var panel = Render<BevToolsPanel>();
        var session = Services.GetRequiredService<BevToolsSession>();
        new BevJsBridge(session, () => { }).ReportResponsiveness(
            [new BevLongFramePayload { AgeMs = 100, DurationMs = 180, BlockingMs = 130 }],
            [new BevInteractionPayload { AgeMs = 50, Name = "click", DurationMs = 260, InputDelayMs = 4, ProcessingMs = 200, PresentationMs = 56, Target = "<button.buy>" }]);

        panel.FindAll(".bev-tab").First(t => t.TextContent.Trim().StartsWith("Browser", StringComparison.Ordinal)).Click();
        var text = panel.Find(".bev-body").TextContent;
        StringAssert.Contains(text, "Responsiveness");
        StringAssert.Contains(text, "260.0 ms");
        StringAssert.Contains(text, "click on <button.buy>");
        StringAssert.Contains(text, "reports long tasks, without the scripts that ran in them");
        StringAssert.Contains(text, "the browser's part");
        Assert.AreEqual(BevCapabilityStatus.Available, session.Capabilities.Single(c => c.Name.StartsWith("Interaction latency", StringComparison.Ordinal)).Status);
        Assert.AreEqual(BevCapabilityStatus.Partial, session.Capabilities.Single(c => c.Name.StartsWith("Long frames", StringComparison.Ordinal)).Status);
    }

    [TestMethod]
    public void A_browser_that_reports_no_frames_shows_no_frame_totals()
    {
        var module = JSInterop.SetupModule("./_content/Bit.BevTools/bit-bevtools.js");
        module.Setup<BevBrowserSnapshot>("attach", _ => true).SetResult(new BevBrowserSnapshot
        {
            Online = true,
            Responsiveness = new BevResponsivenessSupport { EventTiming = true },
        });
        var panel = Render<BevToolsPanel>();
        panel.FindAll(".bev-tab").First(t => t.TextContent.Trim().StartsWith("Browser", StringComparison.Ordinal)).Click();

        var stats = panel.FindAll(".bev-body .bev-stat").Select(s => s.TextContent).ToList();
        Assert.Contains(s => s.StartsWith("–long frames · longest –", StringComparison.Ordinal), stats, string.Join(" | ", stats));
        Assert.Contains(s => s.StartsWith("–blocking time", StringComparison.Ordinal), stats, "not measured, not zero");
    }
}
