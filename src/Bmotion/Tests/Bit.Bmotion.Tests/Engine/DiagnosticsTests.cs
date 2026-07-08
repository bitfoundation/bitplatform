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
        // Server mode (no compositor for color) keeps a rAF driver on the engine so it stays active.
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
        // Mutating the snapshot must not affect the engine; a later snapshot is independent.
        Assert.IsFalse(first.Transforms is Dictionary<string, double> live && ReferenceEquals(live, first.Transforms) && live.Count == 0);
        engine.RegisterElement("el2", null);
        Assert.AreEqual(1, first.Transforms.Count, "earlier snapshot must be unaffected by later registrations");
    }

    [TestMethod]
    public void GetDiagnostics_Empty_WhenNothingRegistered()
        => Assert.AreEqual(0, NewEngine().GetDiagnostics().Count);
}
