namespace Bit.Belemetry.Tests.Diagnostics;

[TestClass]
public class BelemetryExceptionFingerprintTests
{
    private const string DotnetStack = """
           at MyApp.Pages.Orders.<LoadAsync>d__12.MoveNext() in C:\Users\jane\src\MyApp\Pages\Orders.razor:line 42
        --- End of stack trace from previous location ---
           at System.Threading.Tasks.Task.ThrowIfExceptional(Boolean includeTaskCanceledExceptions)
           at Microsoft.AspNetCore.Components.ComponentBase.RunInitAndSetParametersAsync()
           at MyApp.Services.OrderApi.<>c__DisplayClass5_0.<GetAsync>b__0() in /build/agent/src/OrderApi.cs:line 17
           at MyApp.Services.OrderApi.<Fetch>g__Send|3_0(String id)
        """;

    [TestMethod]
    public void DotnetFrames_AreNormalized_WithoutPathsArgumentsOrGeneratedNames()
    {
        var frames = BelemetryExceptionFingerprint.MeaningfulFrames(DotnetStack);

        CollectionAssert.AreEqual(new[] { "MyApp.Pages.Orders.LoadAsync", "MyApp.Services.OrderApi.GetAsync", "MyApp.Services.OrderApi.Fetch.Send" }, frames.ToArray(),
            "Application frames only, async state machines, lambdas and local functions named after their method.");
        Assert.AreEqual("MyApp.Pages.Orders.LoadAsync", BelemetryExceptionFingerprint.Anchor(DotnetStack));
    }

    [TestMethod]
    public void JavaScriptFrames_OfEveryEngine_AreNormalized()
    {
        Assert.AreEqual("loadOrders@app.js", BelemetryExceptionFingerprint.NormalizeFrame("at loadOrders (https://shop.test/js/app.js?v=3:12:34)"));
        Assert.AreEqual("<anonymous>@app.js", BelemetryExceptionFingerprint.NormalizeFrame("at https://shop.test/js/app.js:12:34"));
        Assert.AreEqual("loadOrders@app.js", BelemetryExceptionFingerprint.NormalizeFrame("loadOrders@https://shop.test/js/app.js:12:34"));
        Assert.AreEqual("render@main.js", BelemetryExceptionFingerprint.NormalizeFrame("at async render (https://shop.test/main.3f2a1b9c.js:1:2)"), "The content hash of a bundle name is not part of it.");
        Assert.IsNull(BelemetryExceptionFingerprint.NormalizeFrame("TypeError: x is undefined"), "The message line is not a frame.");
    }

    [TestMethod]
    public void InstanceValues_DoNotSplitAGroup()
    {
        var a = BelemetryExceptionFingerprint.Compute("System.InvalidOperationException", "Order 10423 not found (request 3f2a8c1e-0b6d-4f3a-9c1e-2b6d4f3a9c1e at 2026-11-04T10:00:00Z)", DotnetStack);
        var b = BelemetryExceptionFingerprint.Compute("System.InvalidOperationException", "Order 99871 not found (request 0b6d4f3a-3f2a-8c1e-9c1e-4f3a9c1e2b6d at 2026-11-05T08:12:13Z)", DotnetStack);

        Assert.AreEqual(a, b);
        Assert.AreEqual(24, a.Length);
        Assert.IsTrue(a.All(char.IsAsciiHexDigitLower));
        Assert.AreEqual("Order {n} not found (request {guid} at {time})", BelemetryExceptionFingerprint.Template("Order 10423 not found (request 3f2a8c1e-0b6d-4f3a-9c1e-2b6d4f3a9c1e at 2026-11-04T10:00:00Z)"));
    }

    [TestMethod]
    public void DifferentThrowSites_AndDifferentKinds_AreDifferentGroups()
    {
        var here = BelemetryExceptionFingerprint.Compute("System.InvalidOperationException", "Sequence contains no elements", DotnetStack);
        var elsewhere = BelemetryExceptionFingerprint.Compute("System.InvalidOperationException", "Sequence contains no elements", "   at MyApp.Pages.Cart.OnInitialized()");
        var otherType = BelemetryExceptionFingerprint.Compute("System.ArgumentException", "Sequence contains no elements", DotnetStack);
        var status404 = BelemetryExceptionFingerprint.Compute("System.Net.Http.HttpRequestException", "Response status code does not indicate success: 404 (Not Found).", DotnetStack);
        var status500 = BelemetryExceptionFingerprint.Compute("System.Net.Http.HttpRequestException", "Response status code does not indicate success: 500 (Internal Server Error).", DotnetStack);

        Assert.AreNotEqual(here, elsewhere);
        Assert.AreNotEqual(here, otherType);
        Assert.AreNotEqual(status404, status500, "Short numbers stay: a 404 and a 500 are different failures.");
    }

    [TestMethod]
    public void TheSameFailure_InTwoBuilds_IsOneGroup_UnlessReleasesAreKeptApart()
    {
        var build1 = BelemetryExceptionFingerprint.Compute("E", "boom", "   at App.A.B() in C:\\agent1\\A.cs:line 10");
        var build2 = BelemetryExceptionFingerprint.Compute("E", "boom", "   at App.A.B() in /home/agent2/A.cs:line 14");
        Assert.AreEqual(build1, build2, "Line numbers and paths change between builds; the method does not.");

        Assert.AreNotEqual(
            BelemetryExceptionFingerprint.Compute("E", "boom", "   at App.A.B()", release: "1.0.0"),
            BelemetryExceptionFingerprint.Compute("E", "boom", "   at App.A.B()", release: "1.1.0"));
    }

    [TestMethod]
    public void TheFingerprint_IsStable_AcrossRunsAndVersions()
    {
        // A change of this value is a change of the algorithm: bump BelemetryExceptionFingerprint.Version with it.
        Assert.AreEqual("1", BelemetryExceptionFingerprint.Version);
        Assert.AreEqual(BelemetryExceptionFingerprint.Compute("E", "boom", "   at App.A.B()"), BelemetryExceptionFingerprint.Compute("E", "boom", "   at App.A.B()"));
        Assert.AreNotEqual(BelemetryExceptionFingerprint.FromKey("orders"), BelemetryExceptionFingerprint.Compute("orders", null, null));
    }

    [TestMethod]
    public void StripPaths_KeepsFileNames_AndBoundsTheLength()
    {
        var stripped = BelemetryExceptionFingerprint.StripPaths(DotnetStack);
        Assert.DoesNotContain("jane", stripped);
        Assert.DoesNotContain("/build/agent", stripped);
        Assert.Contains("in Orders.razor:line 42", stripped);
        Assert.HasCount(100, BelemetryExceptionFingerprint.StripPaths(new string('x', 5000), maxLength: 100));
    }

    [TestMethod]
    public void WithoutAStack_QuotedNamesTellFailuresApart()
    {
        Assert.AreNotEqual(
            BelemetryExceptionFingerprint.Compute("System.InvalidOperationException", "Unable to resolve service for type 'IOrderApi'.", null),
            BelemetryExceptionFingerprint.Compute("System.InvalidOperationException", "Unable to resolve service for type 'ICartApi'.", null));
    }
}
