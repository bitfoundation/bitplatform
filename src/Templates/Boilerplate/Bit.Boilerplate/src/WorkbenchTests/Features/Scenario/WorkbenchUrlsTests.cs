using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;

namespace Boilerplate.WorkbenchTests.Features.Scenario;

[TestClass, TestCategory("UnitTest")]
public class WorkbenchUrlsTests
{
    [TestMethod]
    public void APreview_Should_CarryTheScenarioThenThePaneThenTheSnapshot()
    {
        var scenario = new WorkbenchScenario { Theme = WorkbenchThemeMode.Dark };

        Assert.AreEqual("/preview/buildings", WorkbenchUrls.Preview("buildings", new()));
        Assert.AreEqual("/preview/buildings?theme=dark", WorkbenchUrls.Preview("buildings", scenario));
        Assert.AreEqual("/preview/buildings?theme=dark&pane=2&snapshot=abc", WorkbenchUrls.Preview("buildings", scenario, "2", "abc"));
        Assert.AreEqual("/preview/buildings?snapshot=abc", WorkbenchUrls.Preview("buildings", new(), snapshotId: "abc"));
    }

    [TestMethod]
    public void AScreenshotAddress_Should_TakeTheSameQueryKeysAsAPreview()
    {
        var scenario = new WorkbenchScenario { Device = "ipad-mini", Data = WorkbenchDataState.Empty };

        Assert.AreEqual($"/api/screenshots/theme{scenario.ToQueryString()}", WorkbenchUrls.Screenshot("theme", scenario));
        Assert.AreEqual($"/api/screenshots/theme{scenario.ToQueryString()}&snapshot=s1", WorkbenchUrls.Screenshot("theme", scenario, "s1"));
    }

    [TestMethod]
    [TestCategory("Localization")]
    public void Panes_Should_RoundTripThroughOneQueryValue()
    {
        WorkbenchScenario[] scenarios =
        [
            new(),
            new() { Device = "ipad-pro-11", Theme = WorkbenchThemeMode.Dark },
            new WorkbenchScenario { Culture = "fa-IR", Variant = "cards" }.WithSwitch("amount", "few")
        ];

        var value = WorkbenchUrls.EncodePanes(scenarios);

        Assert.DoesNotMatchRegex(new Regex("[+/=?&]"), value, "The value must be safe in a query string without escaping.");
        Assert.AreSequenceEqual(scenarios, WorkbenchUrls.DecodePanes(value));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("not base64 at all!")]
    public void AnInvalidPanesValue_Should_DecodeToNoViews(string? value)
    {
        Assert.IsEmpty(WorkbenchUrls.DecodePanes(value));
        Assert.IsEmpty(WorkbenchUrls.DecodeShots(value));
    }

    [TestMethod]
    public void AScreenshotArchive_Should_ListEveryViewWithItsSnapshot()
    {
        var url = WorkbenchUrls.ScreenshotArchive(
        [
            ("theme", new WorkbenchScenario(), null),
            ("buildings", new WorkbenchScenario { Theme = WorkbenchThemeMode.Dark }, "snap1")
        ]);

        Assert.StartsWith("/api/screenshots/zip?shots=", url);

        var shots = WorkbenchUrls.DecodeShots(WorkbenchUrls.ReadQuery($"http://localhost{url}", "shots"));

        Assert.HasCount(2, shots);
        Assert.AreEqual(("theme", string.Empty), shots[0]);
        Assert.AreEqual(("buildings", "?theme=dark&snapshot=snap1"), shots[1]);
    }

    [TestMethod]
    public void AReviewSheet_Should_CarryTheShotsAndTheFormat()
    {
        var url = WorkbenchUrls.ReviewSheet("theme", [("theme", new WorkbenchScenario(), null)], "pdf");

        Assert.StartsWith("/api/review-sheet/theme?shots=", url);
        Assert.AreEqual("pdf", WorkbenchUrls.ReadQuery($"http://localhost{url}", "format"));
    }

    [TestMethod]
    public void ReadQuery_Should_TreatABlankValueAsMissing()
    {
        Assert.AreEqual("2", WorkbenchUrls.ReadQuery("http://localhost/preview/theme?pane=2", WorkbenchUrls.PaneQueryKey));
        Assert.IsNull(WorkbenchUrls.ReadQuery("http://localhost/preview/theme?pane=", WorkbenchUrls.PaneQueryKey));
        Assert.IsNull(WorkbenchUrls.ReadQuery("http://localhost/preview/theme", WorkbenchUrls.PaneQueryKey));
    }

    [TestMethod]
    public void TheShellAndFlowAddresses_Should_BeTheOnesTheSkillNames()
    {
        Assert.AreEqual("/workbench/pos-dashboard", WorkbenchUrls.Workbench("pos-dashboard"));
        Assert.AreEqual("/flows/abc", WorkbenchUrls.Flow("abc"));
    }

    [TestMethod]
    [DataRow(new[] { "http://+:6000" }, "http://localhost:6000/")]
    [DataRow(new[] { "http://0.0.0.0:6000" }, "http://localhost:6000/")]
    [DataRow(new[] { "http://[::]:6000" }, "http://localhost:6000/")]
    [DataRow(new[] { "https://localhost:7000", "http://localhost:6000" }, "http://localhost:6000/")]
    [DataRow(new[] { "http://192.168.1.20:6000", "http://127.0.0.1:6001" }, "http://127.0.0.1:6001/")]
    [DataRow(new string[0], "http://localhost:5000/")]
    public void TheLocalBaseAddress_Should_PreferLoopbackThenHttp(string[] addresses, string expected)
    {
        var server = A.Fake<IServer>();
        var features = new FeatureCollection();
        var addressesFeature = new ServerAddressesFeature();

        foreach (var address in addresses)
        {
            addressesFeature.Addresses.Add(address);
        }

        features.Set<IServerAddressesFeature>(addressesFeature);
        A.CallTo(() => server.Features).Returns(features);

        Assert.AreEqual(expected, WorkbenchUrls.LocalBaseAddress(server).ToString());
    }
}
