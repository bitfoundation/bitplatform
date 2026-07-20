using Bit.Bmotion.Tests.TestInfra;

namespace Bit.Bmotion.Tests.Engine;

/// <summary>Tests for the engine diagnostics snapshot behind &lt;BmotionInspector&gt; (plan item 3.5).</summary>
[TestClass]
public class DiagnosticsTests
{
    private static BmotionAnimationEngine NewEngine(bool inProcess = true)
        => new(new FakeBmotionInterop { IsInProcess = inProcess });

    [TestMethod]
    public void GetDiagnostics_ReportsRegisteredElementsAndSeededValues()
    {
        var engine = NewEngine();
        engine.RegisterElement("el", new Dictionary<string, object?> { ["x"] = 10.0, ["opacity"] = 0.5 });

        var diag = engine.GetDiagnostics();
        Assert.AreEqual(1, diag.Count);
        var el = diag[0];
        Assert.AreEqual("el", el.Id);
        Assert.AreEqual(10.0, el.Transforms["x"]);
        Assert.AreEqual(0.5, el.NumericValues["opacity"]);
        Assert.IsFalse(el.HasActiveAnimations);
        Assert.AreEqual(0, el.ActiveDriverCount);
    }

    [TestMethod]
    public async Task GetDiagnostics_ShowsActiveDriversWhileAnimating()
    {
        // In-process/WASM engine: color properties never offload to the compositor, so a rAF
        // driver stays active on the engine and the diagnostics report it mid-animation.
        var engine = NewEngine(inProcess: true);
        engine.RegisterElement("el", null);

        await engine.AnimateToAsync("el",
            new Dictionary<string, object?> { ["backgroundColor"] = "#ff0000" },
            Bm.Tween(1.0).ToConfig());

        var el = engine.GetDiagnostics().Single(d => d.Id == "el");
        Assert.IsTrue(el.HasActiveAnimations);
        Assert.IsTrue(el.ActiveProperties.Contains("backgroundColor"));
        Assert.IsTrue(el.ActiveDriverCount >= 1);
    }

    [TestMethod]
    public void GetDiagnostics_ReturnsSnapshotCopies_NotLiveDictionaries()
    {
        var engine = NewEngine();
        engine.RegisterElement("el", new Dictionary<string, object?> { ["x"] = 1.0 });

        var first = engine.GetDiagnostics()[0];
        // A later registration must not mutate an earlier snapshot: it captured its own copy.
        engine.RegisterElement("el2", null);
        Assert.AreEqual(1, first.Transforms.Count, "earlier snapshot must be unaffected by later registrations");
    }

    [TestMethod]
    public void GetDiagnostics_Empty_WhenNothingRegistered()
        => Assert.AreEqual(0, NewEngine().GetDiagnostics().Count);
}
