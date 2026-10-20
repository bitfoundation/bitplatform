using Bit.Bmotion.Tests.TestInfra;

namespace Bit.Bmotion.Tests.Engine;

/// <summary>Tests for the engine diagnostics snapshot behind &lt;BmotionInspector&gt; (plan item 3.5).</summary>
[TestClass]
public class DiagnosticsTests
{
    private static BmotionAnimationEngine NewEngine(bool inProcess = true)
        => new(new FakeBmotionInterop { IsInProcess = inProcess });

    [TestMethod]
    public async Task GetDiagnostics_ReportsRegisteredElementsAndSeededValues()
    {
        await using var engine = NewEngine();
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
        await using var engine = NewEngine(inProcess: true);
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
    public async Task GetDiagnostics_ReturnsSnapshotCopies_NotLiveDictionaries()
    {
        await using var engine = NewEngine();
        engine.RegisterElement("el", new Dictionary<string, object?>
        {
            ["x"] = 1.0,
            ["opacity"] = 0.25,
            ["backgroundColor"] = "#000000",
        });

        var first = engine.GetDiagnostics().Single();

        // Mutate the SAME element's live state after the snapshot was taken: changed values and a
        // brand-new key in each store. A snapshot aliasing the live dictionaries would see all of it.
        engine.SetInstant("el", new Dictionary<string, object?>
        {
            ["x"] = 99.0,
            ["rotate"] = 45.0,
            ["opacity"] = 0.75,
            ["pathLength"] = 0.5,
            ["backgroundColor"] = "#ffffff",
            ["width"] = "10px",
        });

        Assert.AreEqual(1.0, first.Transforms["x"]);
        Assert.IsFalse(first.Transforms.ContainsKey("rotate"));
        Assert.AreEqual(0.25, first.NumericValues["opacity"]);
        Assert.IsFalse(first.NumericValues.ContainsKey("pathLength"));
        Assert.AreEqual("#000000", first.StringValues["backgroundColor"]);
        Assert.IsFalse(first.StringValues.ContainsKey("width"));

        // And the live state really did change, so the assertions above are not vacuous.
        var second = engine.GetDiagnostics().Single();
        Assert.AreEqual(99.0, second.Transforms["x"]);
        Assert.AreEqual("#ffffff", second.StringValues["backgroundColor"]);
    }

    [TestMethod]
    public async Task GetDiagnostics_Empty_WhenNothingRegistered()
    {
        await using var engine = NewEngine();
        Assert.AreEqual(0, engine.GetDiagnostics().Count);
    }
}
