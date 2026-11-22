using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Bit.BevTools.Tests;

/// <summary>
/// <see cref="BevToolsAutoStart.Logs"/>: what the app logs from startup is captured by Bit.BevTools.Loader on its own,
/// while the rest of BevTools waits, and handed to the session attached later - read then at its value access and
/// redacted, never more than an entry logged after the attach would show, and never twice.
/// </summary>
[TestClass]
public class AutoStartLogsTests
{
    private const string Feature = "Entries written before BevTools started";

    [TestMethod]
    public async Task Entries_logged_before_the_attach_are_handed_to_the_session_that_attaches()
    {
        using var provider = Build(o => o.AutoStart = BevToolsAutoStart.Logs);
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("Shop.Startup");
        logger.LogInformation(new EventId(12, "Seeded"), "Loaded {Count} orders for {Customer}", 3, "Ada");
        logger.LogWarning(new InvalidOperationException("Cache cold", new TimeoutException("Redis did not answer")), "Warming the cache failed");

        using var scope = provider.CreateScope();
        var session = await OpenAsync(scope);

        var logs = session.Logs.Snapshot();
        Assert.HasCount(2, logs);
        Assert.IsTrue(logs.All(l => l.BeforeAttach));
        var loaded = logs[0];
        Assert.AreEqual("Loaded 3 orders for Ada", loaded.Message);
        Assert.AreEqual("Loaded {Count} orders for {Customer}", loaded.MessageTemplate);
        Assert.AreEqual("Shop.Startup", loaded.Category);
        Assert.AreEqual(12, loaded.EventId);
        Assert.AreEqual("Seeded", loaded.EventName);
        Assert.AreEqual("3", loaded.Properties[0].Value);
        Assert.AreEqual("\"Ada\"", loaded.Properties[1].Value);
        Assert.IsNull(loaded.CallerComponent, "no session read the stack");
        Assert.IsNull(loaded.TriggerEventId);
        Assert.AreEqual(0, loaded.TimelineEventId);
        Assert.IsTrue(loaded.At < session.StartedAt, "the time it was logged, not the time it was handed over");

        var warning = logs[1];
        Assert.AreEqual(typeof(InvalidOperationException).FullName, warning.ExceptionType);
        Assert.AreEqual("Cache cold\nSystem.TimeoutException: Redis did not answer", warning.ExceptionMessage);
        Assert.IsNull(warning.ErrorId, "an Errors entry would carry the attach's time, not the entry's");
        Assert.AreEqual(1, session.Logs.CountOf(LogLevel.Information));
        Assert.AreEqual(1, session.Logs.CountOf(LogLevel.Warning));

        var marker = session.Timeline.Snapshot().Single(e => e.Kind == BevToolsEventKind.Log);
        StringAssert.StartsWith(marker.Title, "2 log entries from before BevTools was attached");
        var capability = session.Capabilities.Single(c => c.Name == Feature);
        Assert.AreEqual(BevToolsCapabilityStatus.Partial, capability.Status);
        StringAssert.Contains(capability.Explanation, "handed 2 entries");

        // From now on the session's own provider records; the loader keeps nothing more, so nothing is there twice.
        using (session.UseAmbient())
        {
            logger.LogInformation("After the attach");
        }

        logs = session.Logs.Snapshot();
        Assert.HasCount(3, logs);
        Assert.IsFalse(logs[2].BeforeAttach);
        Assert.AreEqual("After the attach", logs[2].Message);
        Assert.IsTrue(logs[1].Id < logs[2].Id, "the entries handed over come first");
        Assert.IsFalse(Context(provider).AutoStartLogs!.IsCapturing);
    }

    [TestMethod]
    public async Task Without_AutoStart_the_loader_keeps_nothing_and_the_report_says_how_to_have_it()
    {
        using var provider = Build();
        Assert.IsFalse(provider.GetServices<ILoggerProvider>().OfType<BevToolsAutoStartLogProvider>().Any());
        provider.GetRequiredService<ILoggerFactory>().CreateLogger("Shop.Startup").LogInformation("Started");

        using var scope = provider.CreateScope();
        var session = await OpenAsync(scope);

        Assert.IsEmpty(session.Logs.Snapshot());
        var capability = session.Capabilities.Single(c => c.Name == Feature);
        Assert.AreEqual(BevToolsCapabilityStatus.NotAvailable, capability.Status);
        StringAssert.Contains(capability.Explanation, "BevToolsLoaderOptions.AutoStart");
    }

    [TestMethod]
    public void A_BevTools_registered_at_startup_starts_no_part_of_its_own()
    {
        var services = Services();
        services.AddBitBevToolsLoaderServices(o =>
        {
            o.RegisterAtStartup = true;
            o.AutoStart = BevToolsAutoStart.Logs;
        });

        Assert.IsNull(Context(services).AutoStartLogs);
        Assert.IsFalse(services.Any(d => d.ServiceType == typeof(ILoggerProvider) && d.ImplementationFactory?.Method.ReturnType == typeof(BevToolsAutoStartLogProvider)));
    }

    [TestMethod]
    public async Task Values_are_read_at_the_attached_session_value_access_and_redacted_when_handed_over()
    {
        using var provider = Build(o => o.AutoStart = BevToolsAutoStart.Logs);
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("Shop.Accounts");
        using (logger.BeginScope(new Dictionary<string, object?> { ["OrderId"] = 5, ["token"] = "abc" }))
        {
            logger.LogInformation("Signing in {User} with {Password}", "ada", "hunter2");
        }

        logger.LogInformation("At {Position}, granted {Grant}", new AutoStartPoint(1, 2), new AutoStartGrant("ada", "h-123"));

        using var scope = provider.CreateScope();
        var session = await OpenAsync(scope);

        var (signIn, position) = (session.Logs.Snapshot()[0], session.Logs.Snapshot()[1]);
        Assert.DoesNotContain("hunter2", signIn.Message!, StringComparison.Ordinal);
        Assert.AreEqual(BevToolsRedactor.RedactedValue, signIn.Properties.Single(p => p.Key == "Password").Value);
        Assert.AreEqual("OrderId=5, token=" + BevToolsRedactor.RedactedValue, Assert.ContainsSingle(signIn.Scopes));

        // An object is shown as the inspector shows it - its ToString when short and clean - unless a member is sensitive.
        Assert.AreEqual("AutoStartPoint { X = 1, Y = 2 }", position.Properties.Single(p => p.Key == "Position").Value);
        Assert.AreEqual(nameof(AutoStartGrant), position.Properties.Single(p => p.Key == "Grant").Value);
    }

    [TestMethod]
    public async Task Under_value_access_None_the_handed_over_entries_show_their_templates_only()
    {
        using var provider = Build(o => o.AutoStart = BevToolsAutoStart.Logs);
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("Shop.Orders");
        logger.LogInformation("Order {OrderId} placed by {Customer}", 42, "Ada");
        logger.LogInformation("Order 43 placed by Grace");

        using var scope = provider.CreateScope();
        var session = BitBevTools.Attach(scope.ServiceProvider, o => o.ValueAccess = BevToolsValueAccess.None);

        var (placed, interpolated) = (session.Logs.Snapshot()[0], session.Logs.Snapshot()[1]);
        Assert.IsTrue(placed.BeforeAttach);
        Assert.IsNull(placed.Message);
        Assert.AreEqual("Order {OrderId} placed by {Customer}", placed.MessageTemplate);
        Assert.IsTrue(placed.Properties.All(p => p.Value == BevToolsRedactor.HiddenValue));
        Assert.IsNull(interpolated.MessageTemplate, "a template without holes may be an interpolated string, values and all");
        Assert.AreEqual(BevToolsRedactor.HiddenValue, interpolated.Text);
    }

    [TestMethod]
    public void What_the_loader_keeps_references_no_object_the_app_logged()
    {
        using var provider = Build(o => o.AutoStart = BevToolsAutoStart.Logs);
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("Shop.Cart");
        var logged = LogAnObject(logger);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.IsFalse(logged.TryGetTarget(out _), "an attach may never come: a kept object would stay alive until then");
    }

    [TestMethod]
    public async Task Only_the_newest_entries_are_kept_and_the_others_are_counted()
    {
        using var provider = Build(o =>
        {
            o.AutoStart = BevToolsAutoStart.Logs;
            o.MaxLogs = 3;
        });
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("Shop.Startup");
        for (var i = 1; i <= 5; i++)
        {
            logger.LogInformation("Step {Step}", i);
        }

        using var scope = provider.CreateScope();
        var session = await OpenAsync(scope);

        CollectionAssert.AreEqual(new[] { "Step 3", "Step 4", "Step 5" }, session.Logs.Snapshot().Select(l => l.Message).ToArray());
        Assert.AreEqual(5, session.Logs.CountOf(LogLevel.Information), "the counts are of every entry, as the session's own are");
        Assert.AreEqual(3, session.Options.MaxLogs, "forwarded, so the session goes on with the same bound");
        StringAssert.Contains(session.Timeline.Snapshot().Single(e => e.Kind == BevToolsEventKind.Log).Title, "2 older not kept");
    }

    [TestMethod]
    public async Task Entries_below_the_minimum_level_BevTools_own_category_and_its_own_work_are_not_kept()
    {
        using var provider = Build(o =>
        {
            o.AutoStart = BevToolsAutoStart.Logs;
            o.MinimumLogLevel = LogLevel.Warning;
            o.Authorize = services =>
            {
                services.GetRequiredService<ILoggerFactory>().CreateLogger("Shop.Security").LogWarning("Checking who may open the dev tools");
                return ValueTask.FromResult(true);
            };
        });
        var factory = provider.GetRequiredService<ILoggerFactory>();
        factory.CreateLogger("Shop.Startup").LogInformation("Below the minimum");
        factory.CreateLogger("Shop.Startup").LogWarning("At the minimum");
        factory.CreateLogger("Bit.BevTools").LogWarning("BevTools' own line");

        using var scope = provider.CreateScope();
        var session = await OpenAsync(scope);

        Assert.AreEqual("At the minimum", Assert.ContainsSingle(session.Logs.Snapshot()).Message);
        Assert.AreEqual(LogLevel.Warning, session.Options.MinimumLogLevel);
    }

    [TestMethod]
    public void In_a_server_process_entries_are_kept_per_circuit_from_its_dispatcher_only()
    {
        using var provider = Build(o => o.AutoStart = BevToolsAutoStart.Logs, environment: "Development");
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("Shop.Circuit");
        using var first = provider.CreateScope();
        using var second = provider.CreateScope();
        var previous = SynchronizationContext.Current;
        try
        {
            RunOn(new SynchronizationContext(), first, () => logger.LogInformation("First circuit"));
            RunOn(new SynchronizationContext(), second, () => logger.LogInformation("Second circuit"));
            SynchronizationContext.SetSynchronizationContext(null);
            logger.LogInformation("The request pipeline, a hosted service: nobody's");
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }

        var logs = Context(provider).AutoStartLogs!;
        var entries = logs.Take(first.ServiceProvider, out _, out var counts, out _);
        Assert.AreEqual("First circuit", Assert.ContainsSingle(entries!).Formatted);
        Assert.AreEqual(1, counts[(int)LogLevel.Information]);
        Assert.IsNull(logs.Take(first.ServiceProvider, out _, out _, out var whyNone), "taken once");
        StringAssert.Contains(whyNone, "handed to the first session attached on this circuit's dispatcher");
        Assert.AreEqual("Second circuit", Assert.ContainsSingle(logs.Take(second.ServiceProvider, out _, out _, out _)!).Formatted);
    }

    [TestMethod]
    public void In_a_server_process_where_no_attach_is_allowed_nothing_is_kept()
    {
        using var provider = Build(o => o.AutoStart = BevToolsAutoStart.Logs, environment: "Production");
        provider.GetRequiredService<ILoggerFactory>().CreateLogger("Shop.Circuit").LogInformation("Nobody may ever read this");

        var logs = Context(provider).AutoStartLogs!;
        Assert.IsFalse(logs.IsCapturing);
        StringAssert.Contains(logs.Reason, "does not let BevTools be attached here");
    }

    [TestMethod]
    public void A_session_the_app_attaches_itself_where_the_loader_allows_none_is_told_it_records_from_its_attach_on()
    {
        using var provider = Build(o => o.AutoStart = BevToolsAutoStart.Logs, environment: "Production");
        provider.GetRequiredService<ILoggerFactory>().CreateLogger("Shop.Circuit").LogInformation("Kept for no one");
        using var scope = provider.CreateScope();

        var session = BitBevTools.Attach(scope.ServiceProvider, o => o.Enabled = true);

        var capability = session.Capabilities.Single(c => c.Name == Feature);
        Assert.AreEqual(BevToolsCapabilityStatus.NotAvailable, capability.Status);
        StringAssert.Contains(capability.Explanation, "a session the app attaches itself (BevToolsOptions.Enabled) records from its attach on");
        Assert.DoesNotContain("cannot happen", capability.Explanation, StringComparison.Ordinal);
    }

    [TestMethod]
    public void Outside_Development_a_server_keeps_names_and_types_only_and_runs_none_of_the_values_code()
    {
        using var provider = Build(o =>
        {
            o.AutoStart = BevToolsAutoStart.Logs;
            o.AllowInMultiUserHost = true;
            o.Authorize = _ => ValueTask.FromResult(true);
        }, environment: "Production");
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("Shop.Orders");
        using var scope = provider.CreateScope();
        var order = new AutoStartLazyOrder();
        var failure = new AutoStartCardException("Card 4111 1111 1111 1111 declined", new AutoStartCardException("Issuer said no"));
        var previous = SynchronizationContext.Current;
        try
        {
            RunOn(new SynchronizationContext(), scope, () =>
            {
                using (logger.BeginScope(order))
                {
                    logger.LogInformation("Placed {Order} for {Customer}", order, "Ada");
                }

                logger.LogWarning(failure, "Charging failed");
            });
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }

        Assert.AreEqual(0, order.ToStringCalls, "a session attached here reads no values, so the capture runs none of their code");
        Assert.AreEqual(0, failure.MessageCalls + ((AutoStartCardException)failure.InnerException!).MessageCalls, "an exception's message is the app's code too");
        var entries = Context(provider).AutoStartLogs!.Take(scope.ServiceProvider, out _, out _, out _)!;
        Assert.HasCount(2, entries);
        var charging = entries[1];
        Assert.AreEqual(typeof(AutoStartCardException).FullName, charging.ExceptionType);
        Assert.IsTrue(charging.ExceptionMessages!.All(m => m.Message is null));
        Assert.AreEqual(typeof(AutoStartCardException).FullName, charging.ExceptionMessages![1].Type, "the types are kept");
        var entry = entries[0];
        Assert.IsFalse(entry.ValuesRead);
        Assert.IsNull(entry.Formatted);
        Assert.AreEqual("Placed {Order} for {Customer}", entry.Template);
        Assert.AreEqual(typeof(AutoStartLazyOrder), entry.Values[0].ObjectType);
        Assert.IsNull(entry.Values[1].Value, "not even a string is kept");
        Assert.IsNull(Assert.ContainsSingle(entry.Scopes).Text);
    }

    [TestMethod]
    public void All_circuits_together_stay_within_one_budget_which_an_ended_circuit_gives_back()
    {
        const int MaxLogs = 4;
        using var provider = Build(o =>
        {
            o.AutoStart = BevToolsAutoStart.Logs;
            o.MaxLogs = MaxLogs;
        }, environment: "Development");
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("Shop.Circuit");
        var budget = BevToolsAutoStartLogs.CircuitsBudgetFactor * MaxLogs;
        var scopes = Enumerable.Range(0, BevToolsAutoStartLogs.CircuitsBudgetFactor).Select(_ => provider.CreateScope()).ToList();
        var previous = SynchronizationContext.Current;
        try
        {
            foreach (var scope in scopes)
            {
                RunOn(new SynchronizationContext(), scope, () => LogSteps(logger, MaxLogs));
            }

            var logs = Context(provider).AutoStartLogs!;
            Assert.AreEqual(budget, logs.Held, "the circuits that were never attached spent the budget");

            scopes[0].Dispose();
            Assert.AreEqual(budget - MaxLogs, logs.Held, "an ended circuit gives its share back");
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
            scopes.ForEach(s => s.Dispose());
        }
    }

    [TestMethod]
    public void A_new_circuit_keeps_its_entries_when_older_circuits_spent_the_budget()
    {
        const int MaxLogs = 4;
        using var provider = Build(o =>
        {
            o.AutoStart = BevToolsAutoStart.Logs;
            o.MaxLogs = MaxLogs;
        }, environment: "Development");
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("Shop.Circuit");
        var budget = BevToolsAutoStartLogs.CircuitsBudgetFactor * MaxLogs;
        // Reloads: circuits kept after a disconnect, never attached, holding their share until they end.
        var older = Enumerable.Range(0, BevToolsAutoStartLogs.CircuitsBudgetFactor).Select(_ => provider.CreateScope()).ToList();
        using var current = provider.CreateScope();
        var previous = SynchronizationContext.Current;
        try
        {
            foreach (var scope in older)
            {
                RunOn(new SynchronizationContext(), scope, () => LogSteps(logger, MaxLogs));
            }

            RunOn(new SynchronizationContext(), current, () => LogSteps(logger, MaxLogs));

            var logs = Context(provider).AutoStartLogs!;
            Assert.AreEqual(budget, logs.Held, "still within the budget");
            var entries = logs.Take(current.ServiceProvider, out _, out var counts, out _)!;
            CollectionAssert.AreEqual(new[] { "Step 1", "Step 2", "Step 3", "Step 4" }, entries.Select(e => e.Formatted).ToArray(),
                "the places came from the circuits holding the most");
            Assert.AreEqual(MaxLogs, counts[(int)LogLevel.Information]);

            var kept = older.Sum(s => logs.Take(s.ServiceProvider, out _, out _, out _)!.Length);
            Assert.AreEqual(budget - MaxLogs, kept, "the older circuits gave up their oldest entries, counted, not kept");
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
            older.ForEach(s => s.Dispose());
        }
    }

    [TestMethod]
    public void Scopes_whose_renderers_share_a_context_share_its_entries_until_the_last_of_them_ends()
    {
        using var provider = Build(o => o.AutoStart = BevToolsAutoStart.Logs, environment: "Development");
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("Shop.Circuit");
        var first = provider.CreateScope();
        using var second = provider.CreateScope();
        using var third = provider.CreateScope();
        var shared = new SynchronizationContext();
        var logs = Context(provider).AutoStartLogs!;
        var previous = SynchronizationContext.Current;
        try
        {
            RunOn(shared, first, () => logger.LogInformation("Before the second scope"));
            RunOn(shared, second, () => logger.LogInformation("Logged on the shared dispatcher"));
            first.Dispose();

            var entries = logs.Take(second.ServiceProvider, out _, out _, out _);
            CollectionAssert.AreEqual(new[] { "Before the second scope", "Logged on the shared dispatcher" }, entries!.Select(e => e.Formatted).ToArray(),
                "the first scope ending does not take what is still the second one's");

            // A context whose entries were handed over starts again for the next scope noted on it.
            RunOn(shared, third, () => logger.LogInformation("A new scope on the same dispatcher"));
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }

        Assert.AreEqual("A new scope on the same dispatcher", Assert.ContainsSingle(logs.Take(third.ServiceProvider, out _, out _, out _)!).Formatted);
    }

    [TestMethod]
    public async Task A_provider_the_app_removed_from_its_logger_factory_is_reported_instead_of_an_empty_handover()
    {
        var services = Services();
        services.AddBitBevToolsLoaderServices(o =>
        {
            o.RegisterAtStartup = false;
            o.AutoStart = BevToolsAutoStart.Logs;
        });
        services.AddLogging(b => b.ClearProviders());
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<ILoggerFactory>().CreateLogger("Shop.Startup").LogInformation("Nobody captured this");

        using var scope = provider.CreateScope();
        var session = await OpenAsync(scope);

        Assert.IsEmpty(session.Logs.Snapshot());
        var capability = session.Capabilities.Single(c => c.Name == Feature);
        Assert.AreEqual(BevToolsCapabilityStatus.NotAvailable, capability.Status);
        StringAssert.Contains(capability.Explanation, "ClearProviders");
        Assert.DoesNotContain("handed 0", capability.Explanation, StringComparison.Ordinal);
    }

    [TestMethod]
    public void A_BevTools_registered_at_startup_reports_entries_from_before_its_session_as_not_shown()
    {
        var services = Services();
        services.AddBitBevToolsServices(o => o.Enabled = true);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var session = scope.ServiceProvider.GetRequiredService<BevToolsSession>();

        var capability = session.Capabilities.Single(c => c.Name == Feature);
        Assert.AreEqual(BevToolsCapabilityStatus.Partial, capability.Status, "what was logged before the session existed is not shown in it");
        StringAssert.Contains(capability.Explanation, "not shown");
    }

    [TestMethod]
    public void A_circuit_whose_first_component_had_no_synchronization_context_is_reported_as_such()
    {
        using var provider = Build(o => o.AutoStart = BevToolsAutoStart.Logs, environment: "Development");
        using var scope = provider.CreateScope();
        var previous = SynchronizationContext.Current;
        try
        {
            SynchronizationContext.SetSynchronizationContext(null);
            scope.ServiceProvider.GetRequiredService<BevToolsActivatorHook>().CreateInstance(typeof(ChildComponent));
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }

        Assert.IsNull(Context(provider).AutoStartLogs!.Take(scope.ServiceProvider, out _, out _, out var whyNone));
        StringAssert.Contains(whyNone, "without a synchronization context");
    }

    [TestMethod]
    public async Task A_circuit_hands_its_entries_over_once_the_panel_attaches_the_session_to_its_renderer()
    {
        using var provider = Build(o => o.AutoStart = BevToolsAutoStart.Logs, environment: "Development");
        using var scope = provider.CreateScope();

        var session = await OpenAsync(scope);

        Assert.IsNotNull(session.Attachment!.PendingLogsBeforeAttach, "until then the circuit's entries are not the session's to record");
        Assert.AreEqual(BevToolsCapabilityStatus.RequiresInstrumentation, session.Capabilities.Single(c => c.Name == Feature).Status);
    }

    private static ServiceProvider Build(Action<BevToolsLoaderOptions>? configure = null, string? environment = null)
    {
        var services = Services(environment);
        services.AddBitBevToolsLoaderServices(o =>
        {
            o.RegisterAtStartup = false;
            configure?.Invoke(o);
        });
        return services.BuildServiceProvider();
    }

    private static ServiceCollection Services(string? environment = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IJSRuntime>(new FakeJSRuntime());
        services.AddLogging();
        if (environment is not null)
        {
            services.AddSingleton<IHostEnvironment>(new AutoStartEnvironment(environment));
            // Only its registration is consulted: it is what tells BevTools this process serves many users.
            services.AddSingleton<Microsoft.AspNetCore.Hosting.Server.IServer>(_ => throw new InvalidOperationException("not resolved"));
        }

        return services;
    }

    private static async Task<BevToolsSession> OpenAsync(IServiceScope scope)
    {
        var loader = scope.ServiceProvider.GetRequiredService<BevToolsLoader>();
        Assert.IsTrue(await loader.OpenAsync(), loader.Problem);
        return BevToolsRuntimeAttacher.Resolve(scope.ServiceProvider)!;
    }

    /// <summary>What a circuit's renderer does: creates its first component on its synchronization context, then runs on it.</summary>
    private static void RunOn(SynchronizationContext context, IServiceScope scope, Action log)
    {
        SynchronizationContext.SetSynchronizationContext(context);
        scope.ServiceProvider.GetRequiredService<BevToolsActivatorHook>().CreateInstance(typeof(ChildComponent));
        log();
    }

    private static void LogSteps(ILogger logger, int count)
    {
        for (var i = 1; i <= count; i++)
        {
            logger.LogInformation("Step {Step}", i);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<object> LogAnObject(ILogger logger)
    {
        var cart = new AutoStartCart([new AutoStartPoint(1, 2)]);
        using (logger.BeginScope(cart))
        {
            logger.LogInformation("Cart {Cart}", cart);
        }

        return new WeakReference<object>(cart);
    }

    private static BevToolsLoaderContext Context(IServiceProvider provider) => provider.GetRequiredService<BevToolsLoaderContext>();

    private static BevToolsLoaderContext Context(IServiceCollection services) =>
        (BevToolsLoaderContext)services.Single(d => d.ServiceType == typeof(BevToolsLoaderContext)).ImplementationInstance!;

    private sealed record AutoStartPoint(int X, int Y);

    private sealed record AutoStartGrant(string User, [property: BevToolsSensitive] string Handle);

    private sealed record AutoStartCart(List<AutoStartPoint> Items);

    /// <summary>An entity whose ToString would cost something real (a lazy load, a query), and says when it was asked.</summary>
    private sealed class AutoStartLazyOrder
    {
        public int ToStringCalls;

        public override string ToString()
        {
            ToStringCalls++;
            return "Order 42";
        }
    }

    /// <summary>An exception whose message carries what was logged, and says when it was read.</summary>
    private sealed class AutoStartCardException(string message, Exception? inner = null) : Exception(message, inner)
    {
        public int MessageCalls;

        public override string Message
        {
            get
            {
                MessageCalls++;
                return base.Message;
            }
        }
    }

    private sealed class AutoStartEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "Bit.BevTools.Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
