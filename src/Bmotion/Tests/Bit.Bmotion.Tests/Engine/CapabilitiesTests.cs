using Bit.Bmotion.Tests.TestInfra;
using Microsoft.Extensions.Logging;

namespace Bit.Bmotion.Tests.Engine;

/// <summary>Tests for the Blazor Server capability flags + degradation warning (plan item 1.1, stage 1).</summary>
[TestClass]
public class CapabilitiesTests
{
    [TestMethod]
    public void Capabilities_ReflectInteropMode()
    {
        var wasm = new BmotionCapabilities(new FakeBmotionInterop { IsInProcess = true });
        Assert.IsTrue(wasm.SupportsFrameLoop);
        Assert.IsTrue(wasm.SupportsCompositor);

        var server = new BmotionCapabilities(new FakeBmotionInterop { IsInProcess = false });
        Assert.IsFalse(server.SupportsFrameLoop);
        Assert.IsTrue(server.SupportsCompositor, "compositor works on Server via async interop");
    }

    [TestMethod]
    public async Task Server_NonOffloadableAnimation_LogsDegradationWarningOnce()
    {
        var interop = new FakeBmotionInterop { IsInProcess = false, SupportsLinearEasing = false };
        var logger = new ListLogger<BmotionAnimationEngine>();
        var engine = new BmotionAnimationEngine(interop, logger);
        engine.RegisterElement("el", null);

        // A color animation is never compositor-eligible, so on Server it degrades to instant.
        await engine.AnimateToAsync("el",
            new Dictionary<string, object?> { ["backgroundColor"] = "#ff0000" },
            Bm.Tween(0.5).ToConfig());
        await engine.AnimateToAsync("el",
            new Dictionary<string, object?> { ["backgroundColor"] = "#00ff00" },
            Bm.Tween(0.5).ToConfig());

        var warnings = logger.Warnings.Count(w => w.Contains("instant change on Blazor Server"));
        Assert.AreEqual(1, warnings, "degradation should warn exactly once");
    }

    [TestMethod]
    public async Task Wasm_DoesNotWarn()
    {
        var interop = new FakeBmotionInterop { IsInProcess = true };
        var logger = new ListLogger<BmotionAnimationEngine>();
        var engine = new BmotionAnimationEngine(interop, logger);
        engine.RegisterElement("el", null);

        await engine.AnimateToAsync("el",
            new Dictionary<string, object?> { ["backgroundColor"] = "#ff0000" },
            Bm.Tween(0.5).ToConfig());

        Assert.IsFalse(logger.Warnings.Any(w => w.Contains("Blazor Server")));
    }

    private sealed class ListLogger<T> : ILogger<T>
    {
        public List<string> Warnings { get; } = new();
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning) Warnings.Add(formatter(state, exception));
        }

        private sealed class NullScope : IDisposable { public static readonly NullScope Instance = new(); public void Dispose() { } }
    }
}
