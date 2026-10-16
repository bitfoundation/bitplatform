using Bit.Bonboarding.Tests.Infrastructure;

namespace Bit.Bonboarding.Tests;

[TestClass]
public class RouteMatcherTests
{
    [TestMethod]
    [DataRow(null, "anything", true)]
    [DataRow("", "anything", true)]
    [DataRow("settings", "settings", true)]
    [DataRow("/settings", "settings", true)]
    [DataRow("settings", "/settings/", true)]
    [DataRow("Settings", "settings", true)]
    [DataRow("settings", "settings?tab=profile", true)]
    [DataRow("settings", "settings#anchor", true)]
    [DataRow("settings", "billing", false)]
    [DataRow("settings", "settings/profile", false)]
    [DataRow("settings/*", "settings/profile", true)]
    [DataRow("settings/*", "settings", true)]
    [DataRow("settings/*", "settingsx", false)]
    [DataRow("*", "anything/at/all", true)]
    public void Matches_Paths_The_Way_Authors_Expect(string? pattern, string path, bool expected)
        => Assert.AreEqual(expected, BonboardingRouteMatcher.Matches(pattern, path));

    [TestMethod]
    [DataRow("/a/b/", "a/b")]
    [DataRow("a/b?x=1", "a/b")]
    [DataRow("a/b#c", "a/b")]
    [DataRow("   ", "")]
    [DataRow(null, "")]
    public void Normalises_Away_Slashes_Queries_And_Fragments(string? input, string expected)
        => Assert.AreEqual(expected, BonboardingRouteMatcher.Normalize(input));
}

[TestClass]
public class RoutingTests
{
    private static BonboardingTourDefinition Tour() => new()
    {
        Id = "multi-page",
        Steps =
        [
            new BonboardingStepDefinition { Id = "a", Target = BonboardingStepTarget.Css("#a") },
            new BonboardingStepDefinition { Id = "b", Target = BonboardingStepTarget.Css("#b"), Route = "settings" },
            new BonboardingStepDefinition { Id = "c", Target = BonboardingStepTarget.Css("#c"), Route = "settings" },
        ],
    };

    [TestMethod]
    public async Task Navigates_To_A_Step_Route_And_Waits_For_The_Page()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a", "#b", "#c");

        var session = await harness.Service.StartAsync(Tour());
        await session.NextAsync();

        // Navigation is synchronous in the test navigation manager, so the step is live again by
        // the time NextAsync returns.
        Assert.Contains("settings", harness.Navigation!.Navigations);
        Assert.AreEqual("b", session.CurrentStep!.Id);
        Assert.AreEqual(BonboardingTourStatus.Running, session.Status);
    }

    [TestMethod]
    public async Task Holds_The_Tour_When_The_User_Navigates_Away_And_Restores_It_On_Return()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a", "#b", "#c");

        var session = await harness.Service.StartAsync(Tour());
        await session.NextAsync();
        Assert.AreEqual(BonboardingTourStatus.Running, session.Status);

        harness.Navigation!.SimulateUserNavigation("dashboard");
        Assert.AreEqual(BonboardingTourStatus.Waiting, session.Status);

        harness.Navigation.SimulateUserNavigation("settings");
        Assert.AreEqual(BonboardingTourStatus.Running, session.Status);
        Assert.AreEqual("b", session.CurrentStep!.Id);
    }

    [TestMethod]
    public async Task A_Step_Entered_Through_A_Route_Reports_The_Step_It_Came_From()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a", "#b", "#c");

        var changes = new List<BonboardingStepChangedEventArgs>();
        var tour = Tour();
        tour.OnStepChanged = args => { changes.Add(args); return ValueTask.CompletedTask; };

        var session = await harness.Service.StartAsync(tour);
        await session.NextAsync();

        Assert.AreEqual("b", session.CurrentStep!.Id);
        Assert.AreEqual("b", changes[^1].Step.Id);
        Assert.AreEqual("a", changes[^1].PreviousStep?.Id);
        Assert.AreEqual(BonboardingStepChangeReason.Next, changes[^1].Reason);
    }

    [TestMethod]
    public async Task A_Paused_Tour_Ignores_Navigation_Until_It_Is_Resumed()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a", "#b", "#c");

        var session = await harness.Service.StartAsync(Tour());
        await session.NextAsync();
        await session.PauseAsync();

        harness.Navigation!.SimulateUserNavigation("dashboard");
        Assert.AreEqual(BonboardingTourStatus.Paused, session.Status);

        harness.Navigation.SimulateUserNavigation("settings");
        Assert.AreEqual(BonboardingTourStatus.Paused, session.Status);

        await session.ResumeAsync();

        Assert.AreEqual(BonboardingTourStatus.Running, session.Status);
        Assert.AreEqual("b", session.CurrentStep!.Id);
    }

    [TestMethod]
    public async Task Does_Not_Navigate_When_Auto_Navigation_Is_Disabled()
    {
        await using var harness = new BonboardingHarness(options => options.AutoNavigateToStepRoute = false);
        harness.WithElements("#a", "#b", "#c");

        var session = await harness.Service.StartAsync(Tour());
        await session.NextAsync();

        Assert.IsEmpty(harness.Navigation!.Navigations);
        Assert.AreEqual(BonboardingTourStatus.Waiting, session.Status);

        harness.Navigation.SimulateUserNavigation("settings");

        Assert.AreEqual(BonboardingTourStatus.Running, session.Status);
    }

    [TestMethod]
    public async Task A_Wildcard_Route_Constrains_Without_Navigating()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a");

        var tour = new BonboardingTourDefinition
        {
            Id = "wildcard",
            Steps = [new BonboardingStepDefinition { Id = "a", Target = BonboardingStepTarget.Css("#a"), Route = "settings/*" }],
        };

        var session = await harness.Service.StartAsync(tour);

        Assert.IsEmpty(harness.Navigation!.Navigations);
        Assert.AreEqual(BonboardingTourStatus.Waiting, session.Status);

        harness.Navigation.SimulateUserNavigation("settings/profile");

        Assert.AreEqual(BonboardingTourStatus.Running, session.Status);
    }

    [TestMethod]
    public async Task Same_Page_Navigation_Refreshes_The_Measurement()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a");

        var tour = new BonboardingTourDefinition
        {
            Id = "same-page",
            Steps = [new BonboardingStepDefinition { Id = "a", Target = BonboardingStepTarget.Css("#a") }],
        };

        await harness.Service.StartAsync(tour);
        var before = harness.Interop.MeasureCount;

        harness.Navigation!.SimulateUserNavigation("?tab=two");

        Assert.IsTrue(harness.Interop.MeasureCount > before);
    }

    [TestMethod]
    public async Task Ending_A_Tour_Unhooks_The_Navigation_Handler()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a");

        var tour = new BonboardingTourDefinition
        {
            Id = "cleanup",
            Steps = [new BonboardingStepDefinition { Id = "a", Target = BonboardingStepTarget.Css("#a") }],
        };

        var session = await harness.Service.StartAsync(tour);
        await session.CompleteAsync();

        var measurements = harness.Interop.MeasureCount;
        harness.Navigation!.SimulateUserNavigation("elsewhere");

        Assert.AreEqual(measurements, harness.Interop.MeasureCount);
        Assert.AreEqual(BonboardingTourStatus.Completed, session.Status);
    }

    [TestMethod]
    public async Task Works_Without_A_NavigationManager()
    {
        await using var harness = new BonboardingHarness(withNavigation: false);
        harness.WithElements("#a", "#b", "#c");

        var session = await harness.Service.StartAsync(Tour());
        await session.NextAsync();

        // With no router available the route requirement is simply ignored rather than hanging.
        Assert.AreEqual("b", session.CurrentStep!.Id);
        Assert.AreEqual(BonboardingTourStatus.Running, session.Status);
    }
}
