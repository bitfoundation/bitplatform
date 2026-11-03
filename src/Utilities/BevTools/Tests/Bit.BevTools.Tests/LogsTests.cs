using System.Diagnostics;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Bit.BevTools.Tests;

/// <summary>
/// What the application's loggers write while a session's code runs: structured, redacted at capture, read at the
/// session's value access, linked to the component, UI event and trace it came from, and never someone else's.
/// </summary>
[TestClass]
public class LogsTests
{
    private static (BevToolsSession Session, IServiceScope Scope, ILoggerFactory Factory) Create(Action<BitBevToolsOptions>? configure = null, LogLevel factoryMinimum = LogLevel.Information)
    {
        var (session, scope) = TestHelpers.CreateSession(configure, services => services.AddLogging(logging => logging.SetMinimumLevel(factoryMinimum)));
        return (session, scope, scope.ServiceProvider.GetRequiredService<ILoggerFactory>());
    }

    [TestMethod]
    public void A_structured_entry_is_recorded_with_its_template_values_category_and_event()
    {
        var (session, scope, factory) = Create();
        using (scope)
        {
            var logger = factory.CreateLogger("Shop.Services.OrderService");
            using (session.UseAmbient())
            {
                logger.LogInformation(new EventId(1201, "OrdersLoaded"), "Loaded {Count} orders for {Customer} in {ElapsedMs:0.0} ms", 3, "Ada", 12.345);
            }

            var log = Assert.ContainsSingle(session.Logs.Snapshot());
            Assert.AreEqual(LogLevel.Information, log.Level);
            Assert.AreEqual("Shop.Services.OrderService", log.Category);
            Assert.AreEqual("OrderService", log.ShortCategory);
            Assert.AreEqual(1201, log.EventId);
            Assert.AreEqual("OrdersLoaded", log.EventName);
            Assert.AreEqual("Loaded 3 orders for Ada in 12.3 ms", log.Message);
            Assert.AreEqual("Loaded {Count} orders for {Customer} in {ElapsedMs:0.0} ms", log.MessageTemplate);
            CollectionAssert.AreEqual(new[] { "Count", "Customer", "ElapsedMs" }, log.Properties.Select(p => p.Key).ToArray());
            Assert.AreEqual("3", log.Properties[0].Value);
            Assert.AreEqual("\"Ada\"", log.Properties[1].Value);

            var evt = session.Timeline.Find(log.TimelineEventId);
            Assert.IsNotNull(evt);
            Assert.AreEqual(BevToolsEventKind.Log, evt.Kind);
            Assert.AreEqual("OrderService: Loaded 3 orders for Ada in 12.3 ms", evt.Title);
            Assert.AreEqual(1, session.Logs.CountOf(LogLevel.Information));
            Assert.IsEmpty(session.Errors.Snapshot(), "an information entry is not an error");
        }
    }

    [TestMethod]
    public void A_framework_category_is_the_namespace_or_one_below_it_and_names_are_redacted_by_shape()
    {
        Assert.IsTrue(BevToolsLoggerProvider.IsFrameworkCategory("Microsoft.JSInterop"));
        Assert.IsTrue(BevToolsLoggerProvider.IsFrameworkCategory("Microsoft.AspNetCore.Components.Server.Circuits.CircuitHost"));
        Assert.IsFalse(BevToolsLoggerProvider.IsFrameworkCategory("Microsoft.JSInteropExtras.Bridge"), "someone else's namespace");
        Assert.IsFalse(BevToolsLoggerProvider.IsFrameworkCategory("Microsoft.AspNetCore.ComponentsShop"));

        var (session, scope, factory) = Create();
        using (scope)
        {
            const string token = "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxMjMifQ.c2lnbmF0dXJlLXZhbHVl";
            using (session.UseAmbient())
            {
                factory.CreateLogger("Shop.Session." + token).LogInformation(new EventId(7, "Issued " + token), "Issued");
            }

            var log = Assert.ContainsSingle(session.Logs.Snapshot());
            Assert.DoesNotContain("eyJ", log.Category, StringComparison.Ordinal);
            Assert.DoesNotContain("eyJ", log.EventName!, StringComparison.Ordinal);
            Assert.DoesNotContain("eyJ", session.Timeline.Find(log.TimelineEventId)!.Detail!, StringComparison.Ordinal);
        }
    }

    [TestMethod]
    public void Entries_below_the_minimum_level_are_left_out_and_the_minimum_can_be_lowered()
    {
        var (session, scope, factory) = Create(factoryMinimum: LogLevel.Trace);
        using (scope)
        {
            var logger = factory.CreateLogger("Shop.Cache");
            using (session.UseAmbient())
            {
                logger.LogDebug("Cache miss for {Key}", "orders");
                logger.LogTrace("Probe");
            }

            Assert.AreEqual(0, session.Logs.Count, "Information is the default minimum");
        }

        (session, scope, factory) = Create(o => o.MinimumLogLevel = LogLevel.Debug, LogLevel.Trace);
        using (scope)
        {
            var logger = factory.CreateLogger("Shop.Cache");
            using (session.UseAmbient())
            {
                logger.LogDebug("Cache miss for {Key}", "orders");
                logger.LogTrace("Probe");
            }

            Assert.AreEqual("Cache miss for orders", Assert.ContainsSingle(session.Logs.Snapshot()).Message);
        }
    }

    [TestMethod]
    public void The_host_logging_configuration_filters_first()
    {
        var (session, scope, factory) = Create(o => o.MinimumLogLevel = LogLevel.Trace, LogLevel.Warning);
        using (scope)
        {
            using (session.UseAmbient())
            {
                factory.CreateLogger("Shop").LogInformation("Filtered by the host");
                factory.CreateLogger("Shop").LogWarning("Kept");
            }

            Assert.AreEqual("Kept", Assert.ContainsSingle(session.Logs.Snapshot()).Message);
        }
    }

    [TestMethod]
    public void A_value_with_a_sensitive_name_is_redacted_in_its_values_and_in_the_message()
    {
        var (session, scope, factory) = Create();
        using (scope)
        {
            using (session.UseAmbient())
            {
                factory.CreateLogger("Shop.Auth").LogWarning("Sign-in for {User} failed with {Password} and token {Token}", "ada", "hunter2", "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxMjMifQ.c2lnbmF0dXJlLXZhbHVl");
                factory.CreateLogger("Shop.Auth").LogInformation("Header was Bearer eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxMjMifQ.c2lnbmF0dXJlLXZhbHVl");
            }

            var logs = session.Logs.Snapshot();
            var structured = logs[0];
            Assert.AreEqual(BevRedactor.RedactedValue, structured.Properties.Single(p => p.Key == "Password").Value);
            Assert.AreEqual(BevRedactor.RedactedValue, structured.Properties.Single(p => p.Key == "Token").Value);
            Assert.DoesNotContain("hunter2", structured.Message!, StringComparison.Ordinal);
            Assert.DoesNotContain("eyJ", structured.Message!, StringComparison.Ordinal);
            Assert.AreEqual("Sign-in for ada failed with «redacted» and token «redacted»", structured.Message);

            Assert.DoesNotContain("eyJ", logs[1].Message!, StringComparison.Ordinal, "credential shapes in free text are redacted too");
            var json = BevSessionExporter.ToJson(session, new BevSessionExportOptions { Sections = new HashSet<string> { "logs", "timeline" } });
            Assert.DoesNotContain("hunter2", json, StringComparison.Ordinal);
            Assert.DoesNotContain("eyJhbGciOiJIUzI1NiJ9", json, StringComparison.Ordinal);
        }
    }

    [TestMethod]
    public void Value_access_none_keeps_values_unread_and_shows_the_template()
    {
        var (session, scope, factory) = Create(o => o.ValueAccess = BevValueAccess.None);
        using (scope)
        {
            using (session.UseAmbient())
            using (factory.CreateLogger("Shop").BeginScope(new Dictionary<string, object?> { ["CustomerId"] = 42 }))
            {
                factory.CreateLogger("Shop").LogInformation("Loaded {Count} orders for {Customer}", 3, "Ada");
            }

            var log = Assert.ContainsSingle(session.Logs.Snapshot());
            Assert.IsNull(log.Message);
            Assert.AreEqual("Loaded {Count} orders for {Customer}", log.Text);
            Assert.IsTrue(log.Properties.All(p => p.Value == BevRedactor.HiddenValue));
            Assert.AreEqual("CustomerId", Assert.ContainsSingle(log.Scopes), "the keys of a scope, not its values");
            Assert.DoesNotContain("Ada", BevSessionExporter.ToJson(session), StringComparison.Ordinal);
        }
    }

    [TestMethod]
    public void Value_access_none_shows_no_message_logged_as_one_string_since_it_may_be_interpolated()
    {
        var (session, scope, factory) = Create(o => o.ValueAccess = BevValueAccess.None);
        using (scope)
        {
            var email = "bob@x.test";
            using (session.UseAmbient())
            {
#pragma warning disable CA2254 // The point: an interpolated string is its own template.
                factory.CreateLogger("Shop.Auth").LogError($"Login failed for {email}");
#pragma warning restore CA2254
            }

            var log = Assert.ContainsSingle(session.Logs.Snapshot());
            Assert.IsNull(log.Message);
            Assert.IsNull(log.MessageTemplate);
            Assert.AreEqual(BevRedactor.HiddenValue, log.Text);
            StringAssert.Contains(Assert.ContainsSingle(session.Errors.Snapshot()).Message, BevRedactor.HiddenValue);
            Assert.DoesNotContain(email, BevSessionExporter.ToJson(session), StringComparison.Ordinal);
            Assert.DoesNotContain(email, BevSessionExporter.ToMarkdownReport(session), StringComparison.Ordinal);
        }
    }

    [TestMethod]
    public void A_sensitive_value_past_the_listed_properties_is_still_kept_out_of_the_message()
    {
        var (session, scope, factory) = Create();
        using (scope)
        {
            var names = Enumerable.Range(1, 21).Select(i => "{P" + i + "}").Append("{Password}");
            object?[] values = [.. Enumerable.Range(1, 21).Select(i => (object?)i), "hunter2"];
            using (session.UseAmbient())
            {
#pragma warning disable CA2254 // A template of 22 holes, built for the test.
                factory.CreateLogger("Shop").LogInformation(string.Join(" ", names), values);
#pragma warning restore CA2254
            }

            var log = Assert.ContainsSingle(session.Logs.Snapshot());
            Assert.HasCount(20, log.Properties, "only the first are listed");
            Assert.AreEqual(string.Join(" ", names), log.MessageTemplate, "the template is read past them");
            Assert.DoesNotContain("hunter2", log.Message!, StringComparison.Ordinal);
            StringAssert.EndsWith(log.Message, "21 «redacted»");
        }
    }

    [TestMethod]
    public void A_hole_named_twice_takes_the_value_in_its_place_when_the_message_is_rebuilt()
    {
        var (session, scope, factory) = Create(o => o.ValueAccess = BevValueAccess.Shallow);
        using (scope)
        {
            using (session.UseAmbient())
            {
                factory.CreateLogger("Shop").LogInformation("Moved {Item} from {Place} to {Place}", "box", "A", "B");
            }

            var log = Assert.ContainsSingle(session.Logs.Snapshot());
            Assert.AreEqual("Moved box from A to B", log.Message);
            var json = BevSessionExporter.ToJson(session, new BevSessionExportOptions { Sections = new HashSet<string> { "logs" } });
            Assert.AreEqual(1, json.Split("\"Place\"").Length - 1, "a name once in the export's properties");
            Assert.IsNotNull(System.Text.Json.Nodes.JsonNode.Parse(json)!["logs"]!.AsArray()[0]!["properties"]!.AsObject()["Place"], "and an export a JSON reader takes");
        }
    }

    [TestMethod]
    public void Value_access_shallow_never_prints_an_object_through_its_ToString()
    {
        var (session, scope, factory) = Create(o => o.ValueAccess = BevValueAccess.Shallow);
        using (scope)
        {
            using (session.UseAmbient())
            {
                factory.CreateLogger("Shop").LogInformation("Saved {Order} for {Customer}", new SecretOrder(), "Ada");
            }

            var log = Assert.ContainsSingle(session.Logs.Snapshot());
            Assert.DoesNotContain("internal-margin", log.Message!, StringComparison.Ordinal);
            StringAssert.Contains(log.Message, "for Ada");
        }
    }

    [TestMethod]
    public void Scopes_are_recorded_outermost_first_with_sensitive_keys_redacted()
    {
        var (session, scope, factory) = Create();
        using (scope)
        {
            var logger = factory.CreateLogger("Shop");
            using (session.UseAmbient())
            using (logger.BeginScope(new Dictionary<string, object?> { ["OrderId"] = 42, ["ApiKey"] = "abc" }))
            using (logger.BeginScope("Checkout {Step}", "payment"))
            {
                logger.LogInformation("Charging");
            }

            var log = Assert.ContainsSingle(session.Logs.Snapshot());
            Assert.HasCount(2, log.Scopes);
            Assert.AreEqual("OrderId=42, ApiKey=" + BevRedactor.RedactedValue, log.Scopes[0]);
            Assert.AreEqual("Step=payment", log.Scopes[1]);
        }
    }

    [TestMethod]
    public void An_application_error_entry_is_an_error_and_points_at_it()
    {
        var (session, scope, factory) = Create();
        using (scope)
        {
            using (session.UseAmbient())
            {
                factory.CreateLogger("Shop.Services.OrderService").LogError(new InvalidOperationException("Order 1042 was not found", new TimeoutException("db timeout")), "Could not load order {OrderId}", 1042);
            }

            var log = Assert.ContainsSingle(session.Logs.Snapshot());
            var error = Assert.ContainsSingle(session.Errors.Snapshot());
            Assert.AreEqual("log", error.Source);
            Assert.AreEqual(typeof(InvalidOperationException).FullName, error.ExceptionType);
            Assert.AreEqual("OrderService: Could not load order 1042", error.Message);
            Assert.AreEqual(error.Id, log.ErrorId);
            Assert.AreEqual(error.TimelineEventId, log.TimelineEventId, "one timeline row, the error's");
            Assert.AreEqual(typeof(InvalidOperationException).FullName, log.ExceptionType);
            StringAssert.Contains(log.ExceptionMessage, "System.TimeoutException: db timeout");
        }
    }

    [TestMethod]
    public void Blazor_warnings_still_reach_errors_and_are_entries_once()
    {
        var (session, scope, factory) = Create();
        using (scope)
        {
            using (session.UseAmbient())
            {
                factory.CreateLogger("Microsoft.AspNetCore.Components.Server.Circuits.CircuitHost").LogWarning("Circuit is slow");
                factory.CreateLogger("Microsoft.AspNetCore.Components.Web.ErrorBoundary").LogError(new InvalidOperationException("boom"), "Caught");
            }

            var errors = session.Errors.Snapshot();
            Assert.AreEqual("error-boundary", Assert.ContainsSingle(errors).Source);
            var logs = session.Logs.Snapshot();
            Assert.HasCount(2, logs);
            var warning = session.Timeline.Find(logs[0].TimelineEventId)!;
            Assert.AreEqual("log:circuit", warning.Category, "the warning's timeline row is the one Blazor's warnings always had");
            Assert.AreEqual(errors[0].Id, logs[1].ErrorId);
            Assert.AreEqual(2, session.Timeline.Snapshot().Length, "no second row per entry");
        }
    }

    [TestMethod]
    public void Paused_capture_records_no_entry_but_still_collects_an_error()
    {
        var (session, scope, factory) = Create();
        using (scope)
        {
            session.PauseCapture();
            using (session.UseAmbient())
            {
                factory.CreateLogger("Shop").LogInformation("While paused");
                factory.CreateLogger("Shop").LogError("Failed while paused");
            }

            Assert.AreEqual(0, session.Logs.Count);
            Assert.AreEqual("Shop: Failed while paused", Assert.ContainsSingle(session.Errors.Snapshot()).Message);
        }
    }

    [TestMethod]
    public void Track_logs_off_leaves_application_entries_alone_but_not_blazor_errors()
    {
        var (session, scope, factory) = Create(o => o.TrackLogs = false);
        using (scope)
        {
            using (session.UseAmbient())
            {
                factory.CreateLogger("Shop").LogError("App failure");
                factory.CreateLogger("Microsoft.AspNetCore.Components.Web.ErrorBoundary").LogError(new InvalidOperationException("boom"), "Caught");
            }

            Assert.AreEqual(0, session.Logs.Count);
            Assert.AreEqual("error-boundary", Assert.ContainsSingle(session.Errors.Snapshot()).Source);
        }
    }

    [TestMethod]
    public void What_bevtools_does_through_application_code_is_not_the_applications()
    {
        var (session, scope, factory) = Create();
        using (scope)
        {
            using (session.UseAmbient())
            {
                using (SessionResolver.BeginBevToolsWork())
                {
                    factory.CreateLogger("Shop").LogWarning("Asked by BevTools");
                }

                factory.CreateLogger("Bit.BevTools").LogWarning("BevTools' own");
                // A value whose ToString logs: the capture does not record what it causes.
                factory.CreateLogger("Shop").LogInformation("Value {Value}", new LoggingValue(factory.CreateLogger("Shop.Inner")));
            }

            Assert.AreEqual("Shop", Assert.ContainsSingle(session.Logs.Snapshot()).Category);
        }
    }

    [TestMethod]
    public void An_entry_written_outside_every_session_of_a_server_is_nobodys()
    {
        var (session, scope, factory) = Create();
        using (scope)
        {
            session.Platform = "Server";
            factory.CreateLogger("Shop").LogWarning("From the request pipeline");
            Assert.AreEqual(0, session.Logs.Count);
        }
    }

    [TestMethod]
    public void Work_that_left_the_dispatcher_belongs_to_the_session_whose_event_it_descends_from()
    {
        var (session, scope, factory) = Create();
        using (scope)
        {
            session.Platform = "Server";
            ActivityObserver.EnsureStarted();
            using var source = new ActivitySource(ActivityObserver.SourceName);
            Activity? handler;
            using (session.UseAmbient())
            {
                // The framework starts this activity on the circuit's dispatcher when it dispatches a click.
                handler = source.StartActivity(ActivityObserver.HandleEventName);
            }

            Assert.IsNotNull(handler, "BevTools listens to the framework's source");
            try
            {
                // After an await with ConfigureAwait(false) inside a library: no dispatcher, the activity flowed along.
                Task.Run(() => factory.CreateLogger("Microsoft.EntityFrameworkCore.Database.Command").LogInformation("Executed DbCommand ({Elapsed}ms)", 3)).Wait();
            }
            finally
            {
                handler.Stop();
            }

            var log = Assert.ContainsSingle(session.Logs.Snapshot());
            Assert.AreEqual(handler.TraceId.ToHexString(), log.TraceId);
            Assert.IsNotNull(log.TriggerEventId, "the click it ran in");
        }
    }

    [TestMethod]
    public void Exports_and_the_report_carry_the_entries_and_the_counts()
    {
        var (session, scope, factory) = Create();
        using (scope)
        {
            using (session.UseAmbient())
            {
                factory.CreateLogger("Shop.Payments").LogWarning(new EventId(7), "Retrying {Attempt}", 2);
                factory.CreateLogger("Shop.Payments").LogInformation("Done");
            }

            var json = System.Text.Json.JsonDocument.Parse(BevSessionExporter.ToJson(session)).RootElement;
            Assert.AreEqual(2, json.GetProperty("logs").GetArrayLength());
            var first = json.GetProperty("logs")[0];
            Assert.AreEqual("Warning", first.GetProperty("level").GetString());
            Assert.AreEqual("Retrying {Attempt}", first.GetProperty("messageTemplate").GetString());
            Assert.AreEqual("2", first.GetProperty("properties").GetProperty("Attempt").GetString());
            Assert.AreEqual(1, json.GetProperty("logCounts").GetProperty("Warning").GetInt32());
            Assert.IsTrue(json.GetProperty("options").GetProperty("trackLogs").GetBoolean());

            var report = BevSessionExporter.ToMarkdownReport(session);
            StringAssert.Contains(report, "## Logged warnings and errors (1 of 2 entries)");
            StringAssert.Contains(report, "`warn` Shop.Payments: Retrying 2");
        }
    }

    [TestMethod]
    public void One_template_logged_over_and_over_is_a_flood_whatever_its_values()
    {
        var (session, scope, factory) = Create(o => o.Diagnostics.LogFloodCount = 30);
        using (scope)
        {
            var logger = factory.CreateLogger("Shop.Products");
            using (session.UseAmbient())
            {
                for (var i = 0; i < 29; i++)
                {
                    logger.LogInformation("Rendering product {Id}", i);
                }

                logger.LogInformation("Something else");
            }

            session.Diagnostics.Evaluate(force: true);
            Assert.DoesNotContain(f => f.Diagnostic.RuleId == "log.flood", session.Diagnostics.Findings, "29 is below the threshold, and another template is not the same entry");

            using (session.UseAmbient())
            {
                logger.LogInformation("Rendering product {Id}", 29);
            }

            session.Diagnostics.Evaluate(force: true);
            var finding = Assert.ContainsSingle(f => f.Diagnostic.RuleId == "log.flood", session.Diagnostics.Findings).Diagnostic;
            StringAssert.Contains(finding.Title, "Shop.Products: \"Rendering product {Id}\" logged 30 times");
            Assert.AreEqual(BevDiagnosticSeverity.Warning, finding.Severity);
            Assert.IsNotEmpty(finding.RelatedEventIds!);
        }
    }

    [TestMethod]
    public void The_logs_filter_reads_levels_categories_event_ids_and_text()
    {
        var log = new BevLogRecord
        {
            Id = 1,
            At = DateTimeOffset.UtcNow,
            Level = LogLevel.Warning,
            Category = "Shop.Payments",
            EventId = 7,
            Message = "Retrying 2",
            MessageTemplate = "Retrying {Attempt}",
            Properties = [new("Attempt", "2")],
            TraceId = "4bf92f3577b34da6a3ce929d0e0e4736",
        };

        Assert.IsTrue(BevLogsPanel.Matches(log, BevQueryFilter.Parse("level:warn")));
        Assert.IsTrue(BevLogsPanel.Matches(log, BevQueryFilter.Parse("level:info")));
        Assert.IsFalse(BevLogsPanel.Matches(log, BevQueryFilter.Parse("level:error")));
        Assert.IsFalse(BevLogsPanel.Matches(log, BevQueryFilter.Parse("level:nonsense")), "an unknown level matches nothing rather than everything");
        Assert.IsTrue(BevLogsPanel.Matches(log, BevQueryFilter.Parse("category:payments event:7")));
        Assert.IsFalse(BevLogsPanel.Matches(log, BevQueryFilter.Parse("event:8")));
        Assert.IsTrue(BevLogsPanel.Matches(log, BevQueryFilter.Parse("trace:4bf9 attempt=2")));
        Assert.IsTrue(BevLogsPanel.Matches(log, BevQueryFilter.Parse("{Attempt}")));
        Assert.AreEqual(LogLevel.Critical, BevLogLevels.Parse("crit"));
        Assert.AreEqual(LogLevel.Error, BevLogLevels.Parse("4"));
        Assert.IsNull(BevLogLevels.Parse("9"));
    }

    private sealed class SecretOrder
    {
        public override string ToString() => "Order with internal-margin 42%";
    }

    private sealed class LoggingValue(ILogger logger)
    {
        public override string ToString()
        {
            logger.LogWarning("Logged from ToString");
            return "value";
        }
    }
}

/// <summary>Components that log, through a real renderer.</summary>
[TestClass]
public class LogsPanelTests : BunitContext
{
    public LogsPanelTests()
    {
        Services.AddBitBevToolsServices(o =>
        {
            o.Enabled = true;
            o.Ui.OpenByDefault = true;
        });
        // bUnit registers a logger factory that drops everything; the app's is a real one, with its providers.
        Services.AddLogging();
        Services.Replace(ServiceDescriptor.Singleton<ILoggerFactory, LoggerFactory>());
        Services.Replace(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(Logger<>)));
        Services.AddScoped<LoggingRowService>();
        JSInterop.Mode = JSRuntimeMode.Loose;
        this.UseRenderer("Server");
    }

    private BevToolsSession Session => Services.GetRequiredService<BevToolsSession>();

    [TestMethod]
    public async Task A_component_that_logs_is_named_as_the_sender_and_the_panel_shows_the_entry()
    {
        var module = JSInterop.SetupModule("./_content/Bit.BevTools/bit-bevtools.js");
        module.Setup<BevBrowserSnapshot>("attach", _ => true).SetResult(new BevBrowserSnapshot { Online = true, Visibility = "visible" });
        module.Setup<string?>("loadPrefs", _ => true).SetResult(null);
        var row = Render<LoggingRow>(ps => ps.Add(p => p.Id, 1042));
        row.WaitForAssertion(() => Assert.AreEqual(1, Session.Logs.Count));

        var log = Session.Logs.Snapshot()[0];
        Assert.AreEqual("LoggingRow.OnInitializedAsync", log.CallerComponent);
        Assert.AreEqual("LoggingRowService.Load", log.CallerVia);
        Assert.AreEqual(typeof(LoggingRowService).FullName, log.Category);

        var panel = Render<BevToolsPanel>();
        await panel.InvokeAsync(() => panel.FindAll(".bev-tab").First(t => t.TextContent.Trim().StartsWith("Logs", StringComparison.Ordinal)).Click());
        StringAssert.Contains(panel.Markup, "Loading row 1042");
        await panel.InvokeAsync(() => panel.FindAll(".bev-body tbody tr.bev-row").First().Click());
        StringAssert.Contains(panel.Markup, "Loading row {Id}");
        StringAssert.Contains(panel.Markup, "LoggingRow.OnInitializedAsync via LoggingRowService.Load");

        await panel.InvokeAsync(() => panel.FindAll(".bev-segmented button").First(b => b.TextContent == "Categories").Click());
        StringAssert.Contains(panel.Markup, typeof(LoggingRowService).FullName!);
    }

    [TestMethod]
    public async Task Leaving_the_entries_for_another_view_drops_the_command_they_were_filtered_by()
    {
        var logger = Services.GetRequiredService<ILoggerFactory>().CreateLogger(BevDbCommands.Category);
        var executed = LoggerMessage.Define<string, string>(LogLevel.Information, new EventId(BevDbCommands.ExecutedEventId), "Executed DbCommand ({elapsed}ms){commandText}");
        using (Session.UseAmbient())
        {
            executed(logger, "4", "SELECT 1", null);
            Services.GetRequiredService<ILoggerFactory>().CreateLogger("Shop.Other").LogInformation("Unrelated");
        }

        var panel = Render<BevToolsPanel>();
        await panel.InvokeAsync(() => panel.FindAll(".bev-tab").First(t => t.TextContent.Trim().StartsWith("Logs", StringComparison.Ordinal)).Click());
        await panel.InvokeAsync(() => panel.FindAll(".bev-segmented button").First(b => b.TextContent == "Database").Click());
        await panel.InvokeAsync(() => panel.FindAll(".bev-body tbody tr.bev-row").First().QuerySelector("button")!.Click());
        StringAssert.Contains(panel.Markup, "Show every entry");

        await panel.InvokeAsync(() => panel.FindAll(".bev-segmented button").First(b => b.TextContent == "Categories").Click());
        StringAssert.Contains(panel.Markup, "Shop.Other", "every category, not only the command's");
        await panel.InvokeAsync(() => panel.FindAll(".bev-segmented button").First(b => b.TextContent == "Entries").Click());
        Assert.DoesNotContain("Show every entry", panel.Markup, StringComparison.Ordinal);
    }
}

public sealed class LoggingRowService(ILogger<LoggingRowService> logger)
{
    public void Load(int id) => logger.LogInformation("Loading row {Id}", id);
}

public sealed class LoggingRow : ComponentBase
{
    [Parameter] public int Id { get; set; }

    [Inject] public LoggingRowService Service { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        await Task.Yield();
        Service.Load(Id);
    }
}
