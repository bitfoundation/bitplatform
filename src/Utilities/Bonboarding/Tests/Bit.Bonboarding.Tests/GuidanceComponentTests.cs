using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Bit.Bonboarding.Tests.Infrastructure;

namespace Bit.Bonboarding.Tests;

/// <summary>
/// The guidance that lives on the page rather than over it: beacons that open hints or tours, onboarding checklists
/// and the list of tours a help menu offers.
/// </summary>
[TestClass]
public class GuidanceComponentTests : BunitContext
{
    private readonly FakeInterop _interop = new();

    public GuidanceComponentTests()
    {
        Services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.None));
        Services.AddBitBonboardingServices();
        Services.AddSingleton<IBonboardingInterop>(_interop);
        Services.AddBitBonboardingInMemoryStore();

        _interop.ExistingSelectors.Add("[data-bonboarding-anchor=\"export\"]");
    }

    private IBonboardingService BonboardingService => Services.GetRequiredService<IBonboardingService>();

    private static BonboardingTourDefinition Tour(string id, string? title = null) => new()
    {
        Id = id,
        Title = title ?? id,
        Steps = [new BonboardingStepDefinition { Id = $"{id}-1", Title = "One" }, new BonboardingStepDefinition { Id = $"{id}-2", Title = "Two" }],
    };

    // ---- Beacons ----------------------------------------------------------------

    [TestMethod]
    public void A_Beacon_Is_A_Named_Button_The_Browser_Positions_On_Its_Target()
    {
        var cut = Render<BonboardingBeacon>(parameters => parameters
            .Add(beacon => beacon.Anchor, "export")
            .Add(beacon => beacon.Title, "Exports moved")
            .Add(beacon => beacon.Position, BonboardingBeaconPosition.BottomStart));

        var button = cut.Find("button.bit-bonboarding-beacon");
        Assert.AreEqual("Exports moved", button.GetAttribute("aria-label"));
        Assert.AreEqual("dialog", button.GetAttribute("aria-haspopup"));
        Assert.AreEqual("false", button.GetAttribute("aria-expanded"));
        Assert.Contains("bit-bonboarding-themed", button.ClassName!);

        var activation = _interop.Beacons.Values.Single();
        Assert.AreEqual("[data-bonboarding-anchor=\"export\"]", activation.Target.Selector);
        Assert.AreEqual("bottom-start", activation.Position);
    }

    [TestMethod]
    public void A_Beacon_Without_A_Target_Fails_Loudly()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => Render<BonboardingBeacon>(parameters => parameters.Add(beacon => beacon.Title, "Lost")));
    }

    [TestMethod]
    public async Task Pressing_A_Beacon_Opens_A_Hint_That_Leaves_The_Page_Usable()
    {
        var cut = Render<BonboardingBeacon>(parameters => parameters
            .Add(beacon => beacon.Anchor, "export")
            .Add(beacon => beacon.Title, "Exports moved")
            .Add(beacon => beacon.Description, "They live in the share menu now."));

        await cut.Find("button.bit-bonboarding-beacon").ClickAsync(new());

        var hint = BonboardingService.Active!;
        Assert.AreEqual(BonboardingTourStatus.Running, hint.Status);
        Assert.AreEqual("Exports moved", hint.CurrentStep!.Title);
        Assert.AreEqual(BonboardingInteractionMode.Free, hint.CurrentOptions!.Interaction);
        Assert.IsFalse(hint.CurrentOptions.ShowOverlay);
        Assert.IsFalse(hint.CurrentOptions.TrapFocus, "A hint is not modal: the page stays the user's.");
        Assert.AreEqual("Got it", BonboardingLabels.Default.HintDone);

        cut.WaitForAssertion(() => Assert.AreEqual("true", cut.Find("button.bit-bonboarding-beacon").GetAttribute("aria-expanded")));
    }

    [TestMethod]
    public async Task A_Beacon_With_An_Id_Goes_Away_For_Good_Once_Its_Hint_Is_Read()
    {
        var cut = Render<BonboardingBeacon>(parameters => parameters
            .Add(beacon => beacon.Id, "exports-moved")
            .Add(beacon => beacon.Anchor, "export")
            .Add(beacon => beacon.Title, "Exports moved"));

        await cut.Find("button.bit-bonboarding-beacon").ClickAsync(new());
        await cut.InvokeAsync(() => BonboardingService.Active!.NextAsync());

        cut.WaitForAssertion(() => Assert.IsEmpty(cut.FindAll("button.bit-bonboarding-beacon")));
        Assert.IsEmpty(_interop.Beacons, "The browser stopped positioning it.");
        Assert.AreEqual("[data-bonboarding-anchor=\"export\"]", _interop.FocusedTargets.Single().Selector, "Focus went to the element the hint was about.");
        Assert.IsTrue(await BonboardingService.IsClosedAsync("exports-moved"));

        // A fresh render on another visit remembers.
        var again = Render<BonboardingBeacon>(parameters => parameters
            .Add(beacon => beacon.Id, "exports-moved")
            .Add(beacon => beacon.Anchor, "export"));
        again.WaitForAssertion(() => Assert.IsEmpty(again.FindAll("button.bit-bonboarding-beacon")));
    }

    [TestMethod]
    public async Task A_Beacon_Whose_Hint_Is_Forgotten_Comes_Back()
    {
        var cut = Render<BonboardingBeacon>(parameters => parameters
            .Add(beacon => beacon.Id, "exports-moved")
            .Add(beacon => beacon.Anchor, "export"));

        await cut.Find("button.bit-bonboarding-beacon").ClickAsync(new());
        await cut.InvokeAsync(() => BonboardingService.Active!.NextAsync());
        cut.WaitForAssertion(() => Assert.IsEmpty(cut.FindAll("button.bit-bonboarding-beacon")));

        await cut.InvokeAsync(() => BonboardingService.ResetAsync("exports-moved"));

        cut.WaitForAssertion(() => Assert.HasCount(1, cut.FindAll("button.bit-bonboarding-beacon")));
        Assert.ContainsSingle(_interop.Beacons, "The browser positions it again.");
    }

    [TestMethod]
    public async Task A_Beacon_Without_An_Id_Stays_After_Its_Hint()
    {
        var cut = Render<BonboardingBeacon>(parameters => parameters
            .Add(beacon => beacon.Anchor, "export")
            .Add(beacon => beacon.Title, "Exports moved"));

        await cut.Find("button.bit-bonboarding-beacon").ClickAsync(new());
        await cut.InvokeAsync(() => BonboardingService.Active!.DismissAsync());

        cut.WaitForAssertion(() => Assert.AreEqual("false", cut.Find("button.bit-bonboarding-beacon").GetAttribute("aria-expanded")));
        Assert.IsEmpty(_interop.FocusedTargets);
    }

    [TestMethod]
    public async Task A_Beacon_Steps_Aside_While_A_Tour_Runs()
    {
        var cut = Render<BonboardingBeacon>(parameters => parameters.Add(beacon => beacon.Anchor, "export"));
        Assert.HasCount(1, cut.FindAll("button.bit-bonboarding-beacon"));

        var tour = await cut.InvokeAsync(() => BonboardingService.StartAsync(Tour("busy")));
        cut.WaitForAssertion(() => Assert.IsEmpty(cut.FindAll("button.bit-bonboarding-beacon")));

        await cut.InvokeAsync(() => tour.DismissAsync());
        cut.WaitForAssertion(() => Assert.HasCount(1, cut.FindAll("button.bit-bonboarding-beacon")));
        Assert.HasCount(1, _interop.Beacons, "Attached again with its new element.");
    }

    private static BonboardingTourDefinition MultiPageTour() => new()
    {
        Id = "multi-page",
        Title = "Settings tour",
        Steps =
        [
            new BonboardingStepDefinition { Id = "here", Title = "Here" },
            new BonboardingStepDefinition { Id = "there", Title = "There", Route = "settings" },
        ],
    };

    [TestMethod]
    public async Task A_Beacon_Comes_Back_While_The_User_Is_Away_From_A_Tour()
    {
        var cut = Render<BonboardingBeacon>(parameters => parameters.Add(beacon => beacon.Anchor, "export"));
        var navigation = Services.GetRequiredService<NavigationManager>();

        var tour = await cut.InvokeAsync(() => BonboardingService.StartAsync(MultiPageTour()));
        await cut.InvokeAsync(() => tour.NextAsync());
        cut.WaitForAssertion(() => Assert.IsEmpty(cut.FindAll("button.bit-bonboarding-beacon")));

        // The user followed a link elsewhere: nothing of the tour is on screen to give way to.
        await cut.InvokeAsync(() => navigation.NavigateTo("dashboard"));
        cut.WaitForAssertion(() => Assert.HasCount(1, cut.FindAll("button.bit-bonboarding-beacon")));

        // Back on its page, the tour takes the screen again.
        await cut.InvokeAsync(() => navigation.NavigateTo("settings"));
        cut.WaitForAssertion(() => Assert.IsEmpty(cut.FindAll("button.bit-bonboarding-beacon")));
        Assert.AreEqual(BonboardingTourStatus.Running, tour.Status);
    }

    [TestMethod]
    public async Task A_Beacon_Can_Start_A_Tour_And_Leaves_Once_It_Is_Done()
    {
        var tour = Tour("billing");
        var cut = Render<BonboardingBeacon>(parameters => parameters
            .Add(beacon => beacon.Anchor, "export")
            .Add(beacon => beacon.Tour, tour));

        await cut.Find("button.bit-bonboarding-beacon").ClickAsync(new());
        var session = BonboardingService.Active!;
        Assert.AreEqual("billing", session.Tour.Id);

        await cut.InvokeAsync(() => session.CompleteAsync());
        cut.WaitForAssertion(() => Assert.IsEmpty(cut.FindAll("button.bit-bonboarding-beacon")));
    }

    [TestMethod]
    public async Task A_Beacon_Can_Run_A_Handler_Instead()
    {
        var pressed = 0;
        var cut = Render<BonboardingBeacon>(parameters => parameters
            .Add(beacon => beacon.Anchor, "export")
            .Add(beacon => beacon.OnClick, () => pressed++));

        var button = cut.Find("button.bit-bonboarding-beacon");
        Assert.IsNull(button.GetAttribute("aria-haspopup"), "It opens nothing the library knows about.");

        await button.ClickAsync(new());

        Assert.AreEqual(1, pressed);
        Assert.IsNull(BonboardingService.Active);
    }

    // ---- Checklists -----------------------------------------------------------------

    private static BonboardingChecklistDefinition Checklist(params BonboardingChecklistItem[] items) => new()
    {
        Id = "getting-started",
        Title = "Get started",
        Items = [.. items],
    };

    [TestMethod]
    public async Task A_Checklist_Shows_What_Is_Done_And_How_Far_Along_It_Is()
    {
        await BonboardingService.SetChecklistItemAsync("getting-started", "profile");

        var cut = Render<BonboardingChecklist>(parameters => parameters.Add(list => list.Checklist, Checklist(
            new BonboardingChecklistItem { Id = "profile", Title = "Fill in your profile" },
            new BonboardingChecklistItem { Id = "invite", Title = "Invite a teammate", Description = "Work goes faster together." },
            new BonboardingChecklistItem { Id = "avatar", Title = "Add a picture", IsComplete = _ => ValueTask.FromResult(true) })));

        cut.WaitForAssertion(() => Assert.IsNotNull(cut.Find(".bit-bonboarding-checklist")));

        Assert.AreEqual("2 of 3 done", cut.Find(".bit-bonboarding-checklist-count").TextContent);
        var bar = cut.Find("[role='progressbar']");
        Assert.AreEqual("2", bar.GetAttribute("aria-valuenow"));
        Assert.AreEqual("3", bar.GetAttribute("aria-valuemax"));

        var states = cut.FindAll(".bit-bonboarding-checklist-item").Select(item => item.GetAttribute("data-bonboarding-state")).ToArray();
        CollectionAssert.AreEqual(new[] { "done", "todo", "done" }, states);
        Assert.Contains("Not done", cut.FindAll(".bit-bonboarding-checklist-item")[1].TextContent, "The state is spoken, not only drawn.");

        var heading = cut.Find(".bit-bonboarding-checklist-title");
        Assert.AreEqual("heading", heading.GetAttribute("role"));
        Assert.AreEqual(heading.Id, cut.Find(".bit-bonboarding-checklist").GetAttribute("aria-labelledby"));
    }

    [TestMethod]
    public async Task A_Checklist_Item_Is_Done_When_Its_Tour_Is_Completed()
    {
        var tour = Tour("first-project");
        var cut = Render<BonboardingChecklist>(parameters => parameters.Add(list => list.Checklist, Checklist(
            new BonboardingChecklistItem { Id = "project", Title = "Create a project", Tour = tour })));

        cut.WaitForAssertion(() => Assert.AreEqual("todo", cut.Find(".bit-bonboarding-checklist-item").GetAttribute("data-bonboarding-state")));

        // Pressing the item starts its tour.
        await cut.Find(".bit-bonboarding-checklist-action").ClickAsync(new());
        Assert.AreEqual("first-project", BonboardingService.Active?.Tour.Id);

        await cut.InvokeAsync(() => BonboardingService.Active!.CompleteAsync());

        cut.WaitForAssertion(() => Assert.AreEqual("done", cut.Find(".bit-bonboarding-checklist-item").GetAttribute("data-bonboarding-state")));
    }

    [TestMethod]
    public async Task A_Signal_Ticks_An_Item_Off_For_Good()
    {
        var checklist = Checklist(new BonboardingChecklistItem { Id = "invite", Title = "Invite a teammate", CompleteOnSignal = "member-invited" });
        var completed = 0;

        var cut = Render<BonboardingChecklist>(parameters => parameters
            .Add(list => list.Checklist, checklist)
            .Add(list => list.OnCompleted, () => completed++));

        cut.WaitForAssertion(() => Assert.AreEqual("todo", cut.Find(".bit-bonboarding-checklist-item").GetAttribute("data-bonboarding-state")));

        Assert.IsTrue(await cut.InvokeAsync(() => BonboardingService.SignalAsync("member-invited")));

        cut.WaitForAssertion(() => Assert.AreEqual("done", cut.Find(".bit-bonboarding-checklist-item").GetAttribute("data-bonboarding-state")));
        Assert.AreEqual(1, completed, "The last item getting done completes the checklist.");
        CollectionAssert.AreEqual(new[] { "invite" }, (await BonboardingService.GetCompletedChecklistItemsAsync("getting-started")).ToArray());
    }

    [TestMethod]
    public async Task A_Checklist_Given_Another_Definition_Shows_That_One()
    {
        await BonboardingService.SetChecklistItemAsync("members", "invitation");

        var cut = Render<BonboardingChecklist>(parameters => parameters.Add(list => list.Checklist, Checklist(
            new BonboardingChecklistItem { Id = "project", Title = "Create a project" })));
        cut.WaitForAssertion(() => Assert.AreEqual("0 of 1 done", cut.Find(".bit-bonboarding-checklist-count").TextContent));

        // The user's role changed, and the checklist with it.
        cut.Render(parameters => parameters.Add(list => list.Checklist, new BonboardingChecklistDefinition
        {
            Id = "members",
            Title = "Join the team",
            Items =
            [
                new BonboardingChecklistItem { Id = "invitation", Title = "Accept your invitation" },
                new BonboardingChecklistItem { Id = "profile", Title = "Fill in your profile" },
            ],
        }));

        cut.WaitForAssertion(() => Assert.AreEqual("1 of 2 done", cut.Find(".bit-bonboarding-checklist-count").TextContent));
        var states = cut.FindAll(".bit-bonboarding-checklist-item").Select(item => item.GetAttribute("data-bonboarding-state")).ToArray();
        CollectionAssert.AreEqual(new[] { "done", "todo" }, states);
    }

    [TestMethod]
    public async Task Resetting_A_Tour_Refreshes_The_Checklists_And_Lists_On_Screen()
    {
        var tour = Tour("first-project", "First project");
        BonboardingService.Register(tour);
        await BonboardingService.StartAsync(tour);
        await BonboardingService.Active!.CompleteAsync();

        var checklist = Render<BonboardingChecklist>(parameters => parameters.Add(list => list.Checklist, Checklist(
            new BonboardingChecklistItem { Id = "project", Title = "Create a project", Tour = tour })));
        var list = Render<BonboardingTourList>();

        checklist.WaitForAssertion(() => Assert.AreEqual("done", checklist.Find(".bit-bonboarding-checklist-item").GetAttribute("data-bonboarding-state")));
        list.WaitForAssertion(() => Assert.AreEqual("completed", list.Find("[data-bonboarding-tour='first-project']").GetAttribute("data-bonboarding-progress")));

        // A settings page's "Reset onboarding" button.
        await list.InvokeAsync(() => BonboardingService.ResetAsync("first-project"));

        checklist.WaitForAssertion(() => Assert.AreEqual("todo", checklist.Find(".bit-bonboarding-checklist-item").GetAttribute("data-bonboarding-state")));
        list.WaitForAssertion(() => Assert.AreEqual("new", list.Find("[data-bonboarding-tour='first-project']").GetAttribute("data-bonboarding-progress")));
    }

    [TestMethod]
    public async Task Resetting_Everything_Refreshes_The_Checklists_On_Screen()
    {
        await BonboardingService.SetChecklistItemAsync("getting-started", "profile");

        var cut = Render<BonboardingChecklist>(parameters => parameters.Add(list => list.Checklist, Checklist(
            new BonboardingChecklistItem { Id = "profile", Title = "Fill in your profile" })));
        cut.WaitForAssertion(() => Assert.AreEqual("done", cut.Find(".bit-bonboarding-checklist-item").GetAttribute("data-bonboarding-state")));

        await cut.InvokeAsync(() => BonboardingService.ResetAllAsync());

        cut.WaitForAssertion(() => Assert.AreEqual("todo", cut.Find(".bit-bonboarding-checklist-item").GetAttribute("data-bonboarding-state")));
    }

    [TestMethod]
    public async Task Checklists_Keep_Working_Behind_A_Decorated_Service()
    {
        // An application wrapping the service, for logging say, and forwarding to the one underneath.
        Services.AddScoped<BonboardingService>();
        Services.AddScoped<IBonboardingService>(services => new ForwardingBonboardingService(services.GetRequiredService<BonboardingService>()));

        var cut = Render<BonboardingChecklist>(parameters => parameters.Add(list => list.Checklist, Checklist(
            new BonboardingChecklistItem { Id = "invite", Title = "Invite a teammate", CompleteOnSignal = "member-invited" })));
        cut.WaitForAssertion(() => Assert.AreEqual("todo", cut.Find(".bit-bonboarding-checklist-item").GetAttribute("data-bonboarding-state")));

        Assert.IsInstanceOfType<ForwardingBonboardingService>(BonboardingService);
        Assert.IsTrue(await cut.InvokeAsync(() => BonboardingService.SignalAsync("member-invited")), "The checklist on screen was known to the service.");

        cut.WaitForAssertion(() => Assert.AreEqual("done", cut.Find(".bit-bonboarding-checklist-item").GetAttribute("data-bonboarding-state")));
    }

    [TestMethod]
    public async Task A_Checklist_Item_Counts_Only_A_Completion_Of_The_Tour_Version_It_Names()
    {
        var v1 = Tour("versioned");
        await BonboardingService.StartAsync(v1);
        await BonboardingService.Active!.CompleteAsync();

        // The tour was rewritten; the item names the new version, which nobody has completed yet.
        var v2 = Tour("versioned");
        v2.Version = 2;

        var cut = Render<BonboardingChecklist>(parameters => parameters.Add(list => list.Checklist, Checklist(
            new BonboardingChecklistItem { Id = "tour", Title = "Take the tour", Tour = v2 })));

        cut.WaitForAssertion(() => Assert.AreEqual("todo", cut.Find(".bit-bonboarding-checklist-item").GetAttribute("data-bonboarding-state")));
    }

    [TestMethod]
    public async Task A_Beacon_Hint_Follows_The_Applications_Persist_Setting()
    {
        var options = Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<BonboardingOptions>>().Value;
        options.Persist = false;

        var cut = Render<BonboardingBeacon>(parameters => parameters
            .Add(beacon => beacon.Id, "unpersisted-hint")
            .Add(beacon => beacon.Anchor, "export"));

        await cut.Find("button.bit-bonboarding-beacon").ClickAsync(new());
        await cut.InvokeAsync(() => BonboardingService.Active!.NextAsync());

        Assert.IsNull(await BonboardingService.GetRecordAsync("unpersisted-hint"), "Nothing is written when the application turned persistence off.");
    }

    [TestMethod]
    public void A_Checklist_Item_Id_With_A_Space_Still_Describes_Its_Button()
    {
        var cut = Render<BonboardingChecklist>(parameters => parameters.Add(list => list.Checklist, Checklist(
            new BonboardingChecklistItem { Id = "set up team", Title = "Set up your team", Description = "Invite three people.", Route = "team" })));

        cut.WaitForAssertion(() => Assert.IsNotNull(cut.Find("button.bit-bonboarding-checklist-action")));

        var describedBy = cut.Find("button.bit-bonboarding-checklist-action").GetAttribute("aria-describedby")!;
        Assert.DoesNotContain(" ", describedBy);
        Assert.AreEqual("Invite three people.", cut.Find($"#{describedBy}").TextContent);
    }

    [TestMethod]
    public void A_Checklist_Lists_Only_The_Items_That_Apply()
    {
        var cut = Render<BonboardingChecklist>(parameters => parameters.Add(list => list.Checklist, Checklist(
            new BonboardingChecklistItem { Id = "everyone", Title = "For everyone" },
            new BonboardingChecklistItem { Id = "admins", Title = "For admins", When = _ => ValueTask.FromResult(false) })));

        cut.WaitForAssertion(() => Assert.HasCount(1, cut.FindAll(".bit-bonboarding-checklist-item")));
        Assert.AreEqual("0 of 1 done", cut.Find(".bit-bonboarding-checklist-count").TextContent);
    }

    [TestMethod]
    public async Task A_Dismissed_Checklist_Stays_Dismissed()
    {
        var checklist = Checklist(new BonboardingChecklistItem { Id = "a", Title = "A" });

        var cut = Render<BonboardingChecklist>(parameters => parameters
            .Add(list => list.Checklist, checklist)
            .Add(list => list.Dismissible, true));

        cut.WaitForAssertion(() => Assert.IsNotNull(cut.Find("[data-bonboarding-action='dismiss-checklist']")));
        Assert.AreEqual("Dismiss checklist", cut.Find("[data-bonboarding-action='dismiss-checklist']").GetAttribute("aria-label"));

        await cut.Find("[data-bonboarding-action='dismiss-checklist']").ClickAsync(new());

        cut.WaitForAssertion(() => Assert.IsEmpty(cut.FindAll(".bit-bonboarding-checklist")));
        Assert.IsTrue(await BonboardingService.IsChecklistDismissedAsync("getting-started"));

        await BonboardingService.ResetChecklistAsync("getting-started");
        Assert.IsFalse(await BonboardingService.IsChecklistDismissedAsync("getting-started"));
    }

    [TestMethod]
    public async Task A_Route_Item_Navigates_There()
    {
        var cut = Render<BonboardingChecklist>(parameters => parameters.Add(list => list.Checklist, Checklist(
            new BonboardingChecklistItem { Id = "team", Title = "Set up your team", Route = "settings/team" })));

        cut.WaitForAssertion(() => Assert.IsNotNull(cut.Find("button.bit-bonboarding-checklist-action")));
        await cut.Find("button.bit-bonboarding-checklist-action").ClickAsync(new());

        StringAssert.EndsWith(Services.GetRequiredService<NavigationManager>().Uri, "settings/team");
    }

    // ---- Tour list --------------------------------------------------------------------

    [TestMethod]
    public async Task The_Tour_List_Offers_Each_Tour_As_Its_Progress_Suggests()
    {
        BonboardingService.Register(Tour("welcome", "Welcome tour"));
        BonboardingService.Register(Tour("billing", "Billing"));
        BonboardingService.Register(Tour("reports", "Reports"));

        await BonboardingService.StartAsync("billing");
        await BonboardingService.Active!.CompleteAsync();

        var reports = await BonboardingService.StartAsync("reports");
        await reports.CancelAsync();

        var cut = Render<BonboardingTourList>(parameters => parameters.Add(list => list.Title, "Guided tours"));

        cut.WaitForAssertion(() => Assert.HasCount(3, cut.FindAll(".bit-bonboarding-tour-list-item")));

        string ProgressOf(string id) => cut.Find($"[data-bonboarding-tour='{id}']").GetAttribute("data-bonboarding-progress")!;
        Assert.AreEqual("completed", ProgressOf("billing"));
        Assert.AreEqual("in-progress", ProgressOf("reports"));
        Assert.AreEqual("new", ProgressOf("welcome"));

        var start = cut.Find("[data-bonboarding-tour='welcome'] button");
        Assert.AreEqual("Start", start.TextContent);
        Assert.AreEqual("Replay", cut.Find("[data-bonboarding-tour='billing'] button").TextContent);
        Assert.AreEqual("Resume", cut.Find("[data-bonboarding-tour='reports'] button").TextContent);

        // The accessible name says which tour: the button's text, then the tour's title.
        var labelledBy = start.GetAttribute("aria-labelledby")!.Split(' ');
        Assert.AreEqual("Welcome tour", cut.Find($"#{labelledBy[1]}").TextContent);
    }

    [TestMethod]
    public async Task Starting_From_The_List_Runs_The_Tour()
    {
        BonboardingService.Register(Tour("welcome", "Welcome tour"));

        var cut = Render<BonboardingTourList>();
        cut.WaitForAssertion(() => Assert.IsNotNull(cut.Find("[data-bonboarding-tour='welcome'] button")));

        await cut.Find("[data-bonboarding-tour='welcome'] button").ClickAsync(new());

        Assert.AreEqual("welcome", BonboardingService.Active?.Tour.Id);
        cut.WaitForAssertion(() => Assert.IsTrue(cut.Find("[data-bonboarding-tour='welcome'] button").HasAttribute("disabled")));
    }

    [TestMethod]
    public async Task The_Tour_List_Can_Leave_Out_What_Is_Done()
    {
        var done = Tour("done", "Done");
        await BonboardingService.StartAsync(done);
        await BonboardingService.Active!.CompleteAsync();

        var cut = Render<BonboardingTourList>(parameters => parameters
            .Add(list => list.Tours, new[] { Tour("todo", "To do"), done })
            .Add(list => list.HideCompleted, true));

        cut.WaitForAssertion(() => Assert.HasCount(1, cut.FindAll(".bit-bonboarding-tour-list-item")));
        Assert.IsNotNull(cut.Find("[data-bonboarding-tour='todo']"));
    }

    [TestMethod]
    public async Task The_Tour_List_Follows_What_It_Is_Asked_To_List()
    {
        var done = Tour("done", "Done");
        await BonboardingService.StartAsync(done);
        await BonboardingService.Active!.CompleteAsync();

        var cut = Render<BonboardingTourList>(parameters => parameters.Add(list => list.Tours, new[] { done }));
        cut.WaitForAssertion(() => Assert.HasCount(1, cut.FindAll(".bit-bonboarding-tour-list-item")));

        // Another role, other tours.
        cut.Render(parameters => parameters.Add(list => list.Tours, new[] { Tour("todo", "To do"), done }));
        cut.WaitForAssertion(() => Assert.HasCount(2, cut.FindAll(".bit-bonboarding-tour-list-item")));

        cut.Render(parameters => parameters.Add(list => list.HideCompleted, true));
        cut.WaitForAssertion(() => Assert.HasCount(1, cut.FindAll(".bit-bonboarding-tour-list-item")));
        Assert.IsNotNull(cut.Find("[data-bonboarding-tour='todo']"));
    }

    [TestMethod]
    public async Task A_Tour_The_User_Walked_Away_From_Can_Be_Resumed_From_The_List()
    {
        var tour = MultiPageTour();
        BonboardingService.Register(tour);

        var cut = Render<BonboardingTourList>();
        var session = await cut.InvokeAsync(() => BonboardingService.StartAsync(tour));
        await cut.InvokeAsync(() => session.NextAsync());

        const string Button = "[data-bonboarding-tour='multi-page'] button";
        cut.WaitForAssertion(() => Assert.IsTrue(cut.Find(Button).HasAttribute("disabled")));

        // The user followed a link elsewhere and left the tour behind.
        await cut.InvokeAsync(() => Services.GetRequiredService<NavigationManager>().NavigateTo("dashboard"));

        cut.WaitForAssertion(() => Assert.IsFalse(cut.Find(Button).HasAttribute("disabled")));
        Assert.AreEqual("Resume", cut.Find(Button).TextContent);

        await cut.Find(Button).ClickAsync(new());

        Assert.AreEqual("there", BonboardingService.Active?.CurrentStep?.Id, "Picked up where the user left it.");
        Assert.AreEqual(BonboardingTourStatus.Running, BonboardingService.Active!.Status);
    }
}
