using System.Diagnostics;

namespace Bit.BevTools;

/// <summary>Contributes the Circuit panel, the circuit-instability rule and the "Show active circuits" command. Also serves as the reference extension.</summary>
public sealed class BevToolsServerExtension : IBevToolsExtension
{
    public string Name => "Blazor Server circuits";

    public void Configure(IBevToolsExtensionBuilder builder)
    {
        builder
            .AddPanel(new BevToolsPanelDescriptor("circuit", "Circuit", typeof(BevCircuitPanel), Order: 50) { RequiredPlatform = "Server" })
            .AddDiagnosticRule(new CircuitInstabilityRule())
            .AddCommand(new ShowCircuitsCommand());
    }

    private sealed class CircuitInstabilityRule : IBevDiagnosticRule
    {
        public string Id => "circuit.unstable";

        public string Title => "Circuit instability";

        public string Description => "The connection dropped repeatedly within a short time.";

        public IEnumerable<BevDiagnostic> Evaluate(IBevDiagnosticContext context)
        {
            var downs = context.EventsInWindow(BevToolsEventKind.Circuit, TimeSpan.FromMinutes(2)).Where(IsConnectionDown).ToList();
            if (downs.Count >= 2)
            {
                var browserSide = context.Events.Where(e => e.Category == "reconnect").Select(e => e.Title).Distinct().Take(4).ToList();
                yield return new BevDiagnostic(
                    Id,
                    BevDiagnosticSeverity.Warning,
                    $"Connection dropped {downs.Count} times in 2 minutes",
                    "Server-side connection-down events: " + string.Join(", ", downs.Select(d => d.Timestamp.ToLocalTime().ToString("HH:mm:ss"))) + (browserSide.Count > 0 ? ". Browser reported: " + string.Join(", ", browserSide) : "."),
                    "Check proxies/load balancers for WebSocket timeouts, long-running synchronous handlers (see Slow inbound message events) and client network conditions. Consider CircuitOptions.DisconnectedCircuitRetentionPeriod.",
                    Fingerprint: Id + ":" + EpisodeStart(context.Events, downs[^1], IsConnectionDown, TimeSpan.FromMinutes(2)),
                    RelatedEventIds: downs.Select(d => d.Id).ToList());
            }

            var slow = context.EventsInWindow(BevToolsEventKind.Circuit, TimeSpan.FromMinutes(1)).Where(IsSlowInbound).ToList();
            if (slow.Count >= 3)
            {
                yield return new BevDiagnostic(
                    "circuit.blocked",
                    BevDiagnosticSeverity.Warning,
                    $"{slow.Count} inbound messages blocked the circuit for ≥ {(context.GetOptions<BevDiagnosticsOptions>()?.SlowEventHandlerMs ?? 100):0} ms each",
                    "Longest: " + Ms(slow.Max(e => e.DurationMs ?? 0)) + ". While a message is processed, the circuit cannot handle other input or send renders, which the browser perceives as freezes and eventually as disconnects.",
                    "Move CPU-bound work to Task.Run and avoid blocking waits (.Result, .Wait()) in handlers.",
                    Fingerprint: "circuit.blocked:" + EpisodeStart(context.Events, slow[^1], IsSlowInbound, TimeSpan.FromMinutes(1)),
                    RelatedEventIds: slow.Select(e => e.Id).ToList());
            }
        }

        private static bool IsConnectionDown(BevToolsEvent e) => e.Kind == BevToolsEventKind.Circuit && e.Title == "Connection down";

        private static bool IsSlowInbound(BevToolsEvent e) => e.Kind == BevToolsEventKind.Circuit && e.Category == "circuit-inbound";

        /// <summary>
        /// The id of the first event of the episode <paramref name="oldest"/> (the oldest matching event in the window)
        /// belongs to: earlier matching events count as long as each one is within <paramref name="window"/> of the next.
        /// A connection that keeps failing stays one finding that updates, instead of a new finding for every new event
        /// (the newest one changes each time) or whenever the oldest one ages out of the window.
        /// </summary>
        private static long EpisodeStart(IReadOnlyList<BevToolsEvent> events, BevToolsEvent oldest, Func<BevToolsEvent, bool> matches, TimeSpan window)
        {
            var windowTicks = (long)(window.TotalSeconds * Stopwatch.Frequency);
            var anchor = oldest;
            for (var i = events.Count - 1; i >= 0; i--)
            {
                var evt = events[i];
                if (evt.Id >= anchor.Id)
                {
                    continue;
                }

                if (anchor.StartTicks - evt.StartTicks > windowTicks)
                {
                    break;
                }

                if (matches(evt))
                {
                    anchor = evt;
                }
            }

            return anchor.Id;
        }

        private static string Ms(double v) => v.ToString("0.#") + " ms";
    }

    private sealed class ShowCircuitsCommand : IBevToolsCommand
    {
        public string Id => "circuit.show";

        public string Title => "Show active circuits";

        public string Category => "Circuit";

        public IReadOnlyList<string> Keywords => ["signalr", "connection", "reconnect", "server"];

        public Task ExecuteAsync(IBevToolsCommandContext context, string? argument)
        {
            context.ShowPanel("circuit");
            return Task.CompletedTask;
        }
    }
}
