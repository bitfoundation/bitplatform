using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Bit.Bonboarding.Tests.Infrastructure;

namespace Bit.Bonboarding.Tests;

/// <summary>
/// Renders the shipped components against the fake browser layer, so the markup, the ARIA wiring
/// and the button behaviour are covered as well as the engine underneath them.
/// </summary>
[TestClass]
public class ComponentTests : BunitContext
{
    private readonly FakeInterop _interop = new();

    public ComponentTests()
    {
        Services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.None));
        Services.AddBitBonboardingServices();
        Services.AddSingleton<IBonboardingInterop>(_interop);
        Services.AddBitBonboardingInMemoryStore();

        _interop.ExistingSelectors.Add("#a");
        _interop.ExistingSelectors.Add("#b");
        _interop.ExistingSelectors.Add("#c");
    }

    private IBonboardingService BonboardingService => Services.GetRequiredService<IBonboardingService>();

    private static BonboardingTourDefinition Tour(params BonboardingStepDefinition[] steps)
        => new() { Id = "ui", Steps = [.. steps] };

    private static BonboardingStepDefinition Step(string id, string? title = null, string? selector = "#a") => new()
    {
        Id = id,
        Title = title ?? id,
        Description = $"Description of {id}.",
        Target = selector is null ? BonboardingStepTarget.None : BonboardingStepTarget.Css(selector),
    };

    // ---- Rendering ----------------------------------------------------------

    [TestMethod]
    public async Task Renders_Nothing_When_No_Tour_Is_Running()
    {
        var cut = Render<BonboardingRoot>();

        Assert.IsEmpty(cut.FindAll(".bit-bonboarding-root"));

        // The live region is always present so announcements are never missed.
        Assert.IsNotNull(cut.Find(".bit-bonboarding-live"));
        await Task.CompletedTask;
    }

    [TestMethod]
    public async Task Renders_The_Popover_For_The_Active_Step()
    {
        await BonboardingService.StartAsync(Tour(Step("a", "Create a project"), Step("b", "Second")));

        var cut = Render<BonboardingRoot>();

        Assert.AreEqual("Create a project", cut.Find(".bit-bonboarding-title").TextContent);
        Assert.Contains("Description of a.", cut.Find(".bit-bonboarding-description").TextContent);
        Assert.IsNotNull(cut.Find(".bit-bonboarding-spotlight"));
    }

    [TestMethod]
    public async Task Gives_The_Dialog_Correct_Accessible_Semantics()
    {
        await BonboardingService.StartAsync(Tour(Step("a", "Create a project")));

        var cut = Render<BonboardingRoot>();
        var dialog = cut.Find(".bit-bonboarding-popover");

        Assert.AreEqual("dialog", dialog.GetAttribute("role"));
        Assert.AreEqual("true", dialog.GetAttribute("aria-modal"));
        Assert.AreEqual("-1", dialog.GetAttribute("tabindex"));

        var labelledBy = dialog.GetAttribute("aria-labelledby");
        Assert.IsFalse(string.IsNullOrEmpty(labelledBy));
        Assert.AreEqual("Create a project", cut.Find($"#{labelledBy}").TextContent);

        var describedBy = dialog.GetAttribute("aria-describedby");
        Assert.IsFalse(string.IsNullOrEmpty(describedBy));
        Assert.Contains("Description of a.", cut.Find($"#{describedBy}").TextContent);
    }

    [TestMethod]
    public async Task Uses_A_Non_Modal_Dialog_When_The_Page_Stays_Interactive()
    {
        var step = Step("a");
        step.Interaction = BonboardingInteractionMode.TargetOnly;

        await BonboardingService.StartAsync(Tour(step));

        var cut = Render<BonboardingRoot>();

        Assert.AreEqual("false", cut.Find(".bit-bonboarding-popover").GetAttribute("aria-modal"));
    }

    [TestMethod]
    public async Task Falls_Back_To_A_Label_When_A_Step_Has_No_Title()
    {
        var step = Step("a", title: null);
        step.Title = null;

        await BonboardingService.StartAsync(Tour(step));

        var cut = Render<BonboardingRoot>();
        var dialog = cut.Find(".bit-bonboarding-popover");

        Assert.AreEqual("Product tour", dialog.GetAttribute("aria-label"));
        Assert.IsTrue(string.IsNullOrEmpty(dialog.GetAttribute("aria-labelledby")));
        Assert.AreEqual("true", dialog.GetAttribute("data-bonboarding-untitled"));
    }

    [TestMethod]
    public async Task Announces_Each_Step_In_A_Live_Region()
    {
        var session = await BonboardingService.StartAsync(Tour(Step("a", "First"), Step("b", "Second")));

        var cut = Render<BonboardingRoot>();
        var live = cut.Find(".bit-bonboarding-live");

        Assert.AreEqual("polite", live.GetAttribute("aria-live"));
        Assert.Contains("Step 1 of 2", live.TextContent);
        Assert.Contains("First", live.TextContent);

        await cut.InvokeAsync(() => session.NextAsync());

        Assert.Contains("Step 2 of 2", cut.Find(".bit-bonboarding-live").TextContent);
    }

    // ---- Buttons -------------------------------------------------------------

    [TestMethod]
    public async Task The_Next_Button_Advances_The_Tour()
    {
        var session = await BonboardingService.StartAsync(Tour(Step("a"), Step("b")));

        var cut = Render<BonboardingRoot>();
        await cut.Find("[data-bonboarding-action='next']").ClickAsync(new());

        Assert.AreEqual("b", session.CurrentStep!.Id);
        Assert.AreEqual("b", cut.Find(".bit-bonboarding-title").TextContent);
    }

    [TestMethod]
    public async Task The_Back_Button_Only_Appears_After_The_First_Step()
    {
        var session = await BonboardingService.StartAsync(Tour(Step("a"), Step("b")));

        var cut = Render<BonboardingRoot>();
        Assert.IsEmpty(cut.FindAll("[data-bonboarding-action='previous']"));

        await cut.InvokeAsync(() => session.NextAsync());
        await cut.Find("[data-bonboarding-action='previous']").ClickAsync(new());

        Assert.AreEqual("a", session.CurrentStep!.Id);
    }

    [TestMethod]
    public async Task The_Forward_Button_Reads_Done_On_The_Last_Step()
    {
        var session = await BonboardingService.StartAsync(Tour(Step("a"), Step("b")));

        var cut = Render<BonboardingRoot>();
        Assert.AreEqual("Next", cut.Find("[data-bonboarding-action='next']").TextContent);

        await cut.InvokeAsync(() => session.NextAsync());

        Assert.AreEqual("Done", cut.Find("[data-bonboarding-action='next']").TextContent);
        // Skipping is meaningless once there is nothing left to skip.
        Assert.IsEmpty(cut.FindAll("[data-bonboarding-action='skip']"));
    }

    [TestMethod]
    public async Task The_Close_Button_Dismisses_The_Tour()
    {
        var session = await BonboardingService.StartAsync(Tour(Step("a")));

        var cut = Render<BonboardingRoot>();
        await cut.Find("[data-bonboarding-action='close']").ClickAsync(new());

        Assert.AreEqual(BonboardingTourStatus.Dismissed, session.Status);
        Assert.IsEmpty(cut.FindAll(".bit-bonboarding-root"));
    }

    [TestMethod]
    public async Task The_Skip_Button_Ends_The_Tour_As_Skipped()
    {
        var session = await BonboardingService.StartAsync(Tour(Step("a"), Step("b")));

        var cut = Render<BonboardingRoot>();
        await cut.Find("[data-bonboarding-action='skip']").ClickAsync(new());

        Assert.AreEqual(BonboardingTourStatus.Skipped, session.Status);
    }

    [TestMethod]
    public async Task Custom_Actions_Render_And_Run()
    {
        var branched = false;
        var step = Step("a");
        step.Actions =
        [
            new BonboardingStepAction
            {
                Id = "designer",
                Label = "I design",
                Style = BonboardingStepActionStyle.Secondary,
                Effect = BonboardingStepActionEffect.GoTo,
                TargetStepId = "c",
                OnClick = _ => { branched = true; return ValueTask.CompletedTask; },
            },
        ];

        var session = await BonboardingService.StartAsync(Tour(step, Step("b"), Step("c")));

        var cut = Render<BonboardingRoot>();
        await cut.Find("[data-bonboarding-action='designer']").ClickAsync(new());

        Assert.IsTrue(branched);
        Assert.AreEqual("c", session.CurrentStep!.Id);
    }

    [TestMethod]
    public async Task A_Disabled_Action_Renders_As_Disabled()
    {
        var step = Step("a");
        step.Actions =
        [
            new BonboardingStepAction { Id = "locked", Label = "Locked", IsDisabled = _ => true },
        ];

        await BonboardingService.StartAsync(Tour(step));

        var cut = Render<BonboardingRoot>();

        Assert.IsTrue(cut.Find("[data-bonboarding-action='locked']").HasAttribute("disabled"));
    }

    // ---- Progress -------------------------------------------------------------

    [TestMethod]
    public async Task Renders_Progress_Dots_With_A_Readable_Fallback()
    {
        var session = await BonboardingService.StartAsync(Tour(Step("a"), Step("b"), Step("c")));

        var cut = Render<BonboardingRoot>();

        Assert.AreEqual(3, cut.FindAll(".bit-bonboarding-dot").Count);
        Assert.AreEqual("current", cut.FindAll(".bit-bonboarding-dot")[0].GetAttribute("data-bonboarding-state"));

        await cut.InvokeAsync(() => session.NextAsync());

        var dots = cut.FindAll(".bit-bonboarding-dot");
        Assert.AreEqual("done", dots[0].GetAttribute("data-bonboarding-state"));
        Assert.AreEqual("current", dots[1].GetAttribute("data-bonboarding-state"));
        Assert.AreEqual("todo", dots[2].GetAttribute("data-bonboarding-state"));
    }

    [TestMethod]
    public async Task Renders_A_Progress_Bar_With_Aria_Values()
    {
        var tour = Tour(Step("a"), Step("b"), Step("c"));
        tour.Progress = BonboardingProgressStyle.Bar;

        var session = await BonboardingService.StartAsync(tour);

        var cut = Render<BonboardingRoot>();
        await cut.InvokeAsync(() => session.NextAsync());

        var bar = cut.Find("[role='progressbar']");
        Assert.AreEqual("2", bar.GetAttribute("aria-valuenow"));
        Assert.AreEqual("3", bar.GetAttribute("aria-valuemax"));
        Assert.AreEqual("2 of 3", bar.GetAttribute("aria-valuetext"));
    }

    [TestMethod]
    public async Task Progress_Can_Be_Turned_Off()
    {
        var tour = Tour(Step("a"), Step("b"));
        tour.Progress = BonboardingProgressStyle.None;

        await BonboardingService.StartAsync(tour);

        var cut = Render<BonboardingRoot>();

        Assert.IsEmpty(cut.FindAll(".bit-bonboarding-dot"));
        Assert.IsEmpty(cut.FindAll("[role='progressbar']"));
    }

    // ---- Overlay and interaction -----------------------------------------------

    [TestMethod]
    public async Task A_Blocked_Step_Renders_One_Full_Screen_Blocker()
    {
        await BonboardingService.StartAsync(Tour(Step("a")));

        var cut = Render<BonboardingRoot>();

        Assert.ContainsSingle(cut.FindAll(".bit-bonboarding-blocker"));
    }

    [TestMethod]
    public async Task An_Interactive_Step_Renders_Blockers_Around_The_Hole()
    {
        var step = Step("a");
        step.Interaction = BonboardingInteractionMode.TargetOnly;

        await BonboardingService.StartAsync(Tour(step));

        var cut = Render<BonboardingRoot>();

        // Four rectangles surrounding the spotlight, leaving the target clickable.
        Assert.AreEqual(4, cut.FindAll(".bit-bonboarding-blocker").Count);
    }

    [TestMethod]
    public async Task A_Free_Step_Renders_No_Blockers_At_All()
    {
        var step = Step("a");
        step.Interaction = BonboardingInteractionMode.Free;

        await BonboardingService.StartAsync(Tour(step));

        var cut = Render<BonboardingRoot>();

        Assert.IsEmpty(cut.FindAll(".bit-bonboarding-blocker"));
    }

    [TestMethod]
    public async Task Clicking_The_Overlay_Closes_The_Tour_When_Configured()
    {
        var tour = Tour(Step("a"));
        tour.CloseOnOverlayClick = true;

        var session = await BonboardingService.StartAsync(tour);

        var cut = Render<BonboardingRoot>();
        await cut.Find(".bit-bonboarding-blocker").ClickAsync(new());

        Assert.AreEqual(BonboardingTourStatus.Dismissed, session.Status);
    }

    [TestMethod]
    public async Task A_Targetless_Step_Renders_A_Flat_Overlay_And_No_Spotlight()
    {
        await BonboardingService.StartAsync(Tour(Step("a", "Welcome", selector: null)));

        var cut = Render<BonboardingRoot>();

        Assert.IsEmpty(cut.FindAll(".bit-bonboarding-spotlight"));
        Assert.IsNotNull(cut.Find(".bit-bonboarding-overlay"));
    }

    // ---- Styling and theming ----------------------------------------------------

    [TestMethod]
    public async Task Publishes_Its_Options_As_Css_Variables()
    {
        var tour = Tour(Step("a"));
        tour.ZIndex = 12345;
        tour.OverlayOpacity = 0.4;

        await BonboardingService.StartAsync(tour);

        var cut = Render<BonboardingRoot>();
        var style = cut.Find(".bit-bonboarding-root").GetAttribute("style")!;

        Assert.Contains("--bit-bonboarding-z:12345", style);
        Assert.Contains("--bit-bonboarding-overlay-opacity:0.4", style);
        // Invariant formatting: a comma here would silently break the page in some locales.
        Assert.DoesNotContain(",", style.Replace("translate3d", string.Empty));
    }

    [TestMethod]
    public async Task Applies_The_Theme_And_Direction_To_The_Root()
    {
        var tour = Tour(Step("a"));
        tour.Theme = BonboardingTheme.Dark;
        tour.Direction = BonboardingDirection.RightToLeft;

        await BonboardingService.StartAsync(tour);

        var cut = Render<BonboardingRoot>();
        var root = cut.Find(".bit-bonboarding-root");

        Assert.AreEqual("dark", root.GetAttribute("data-bonboarding-theme"));
        Assert.AreEqual("rtl", root.GetAttribute("dir"));
    }

    [TestMethod]
    public async Task Extra_Classes_Reach_The_Root_And_The_Popover()
    {
        var tour = Tour(Step("a"));
        tour.PopoverClass = "tour-class";

        await BonboardingService.StartAsync(tour);

        var cut = Render<BonboardingRoot>(parameters => parameters
            .Add(p => p.Class, "app-class")
            .Add(p => p.PopoverClass, "host-class"));

        Assert.Contains("app-class", cut.Find(".bit-bonboarding-root").GetAttribute("class")!);

        var popoverClass = cut.Find("[role='dialog']").GetAttribute("class")!;
        Assert.Contains("tour-class", popoverClass);
        Assert.Contains("host-class", popoverClass);
    }

    // ---- Waiting ------------------------------------------------------------------

    [TestMethod]
    public async Task Shows_A_Waiting_State_While_A_Target_Is_Missing()
    {
        var step = Step("a", selector: "#late");
        step.MissingTarget = BonboardingMissingTargetBehavior.Wait;

        var session = await BonboardingService.StartAsync(Tour(step));

        var cut = Render<BonboardingRoot>();
        Assert.IsNotNull(cut.Find(".bit-bonboarding-waiting"));

        // There is no target to place against, but the waiting card must still be visible.
        Assert.AreEqual("true", cut.Find(".bit-bonboarding-popover").GetAttribute("data-bonboarding-placed"));

        await cut.InvokeAsync(() => _interop.AppearAsync(session.Id, "#late"));

        Assert.IsEmpty(cut.FindAll(".bit-bonboarding-waiting"));
        Assert.AreEqual("a", cut.Find(".bit-bonboarding-title").TextContent);
    }

    // ---- Headless -------------------------------------------------------------------

    [TestMethod]
    public async Task A_Template_Replaces_The_Popover_Contents()
    {
        await BonboardingService.StartAsync(Tour(Step("a", "Ignored")));

        RenderFragment<BonboardingStepRenderContext> template = context => builder =>
        {
            builder.OpenElement(0, "p");
            builder.AddAttribute(1, "class", "custom");
            builder.AddContent(2, $"Custom {context.Step.Id}");
            builder.CloseElement();
        };

        var cut = Render<BonboardingRoot>(parameters => parameters.Add(p => p.ChildContent, template));

        Assert.AreEqual("Custom a", cut.Find(".custom").TextContent);
        Assert.IsEmpty(cut.FindAll(".bit-bonboarding-title"));

        // The library still owns the dialog element, so accessibility survives the takeover.
        Assert.AreEqual("dialog", cut.Find("[role='dialog']").GetAttribute("role"));
    }

    [TestMethod]
    public async Task A_Template_Still_Leaves_The_Dialog_Named_After_The_Step()
    {
        await BonboardingService.StartAsync(Tour(Step("a", "Create a project")));

        RenderFragment<BonboardingStepRenderContext> template = context => builder =>
            builder.AddMarkupContent(0, "<p class=\"custom\">No heading here</p>");

        var cut = Render<BonboardingRoot>(parameters => parameters.Add(p => p.ChildContent, template));
        var dialog = cut.Find("[role='dialog']");

        // The template rendered no element with the title's id, so pointing aria-labelledby at it
        // would leave the dialog unnamed. The title travels as aria-label instead.
        Assert.AreEqual("Create a project", dialog.GetAttribute("aria-label"));
        Assert.IsTrue(string.IsNullOrEmpty(dialog.GetAttribute("aria-labelledby")));
        Assert.IsTrue(string.IsNullOrEmpty(dialog.GetAttribute("aria-describedby")));
    }

    [TestMethod]
    public async Task A_Step_Header_Fragment_Moves_The_Title_To_Aria_Label()
    {
        var step = Step("a", "Create a project");
        step.Header = _ => builder => builder.AddMarkupContent(0, "<div class=\"own-header\">Mine</div>");

        await BonboardingService.StartAsync(Tour(step));

        var cut = Render<BonboardingRoot>();
        var dialog = cut.Find("[role='dialog']");

        Assert.IsNotNull(cut.Find(".own-header"));
        Assert.AreEqual("Create a project", dialog.GetAttribute("aria-label"));
        Assert.IsTrue(string.IsNullOrEmpty(dialog.GetAttribute("aria-labelledby")));

        // The default body is still rendered, so the description reference is still valid.
        var describedBy = dialog.GetAttribute("aria-describedby");
        Assert.IsFalse(string.IsNullOrEmpty(describedBy));
        Assert.Contains("Description of a.", cut.Find($"#{describedBy}").TextContent);
    }

    [TestMethod]
    public async Task Unstyled_Drops_The_Built_In_Popover_Class()
    {
        await BonboardingService.StartAsync(Tour(Step("a")));

        var cut = Render<BonboardingRoot>(parameters => parameters.Add(p => p.Unstyled, true));

        Assert.IsEmpty(cut.FindAll(".bit-bonboarding-popover"));
        Assert.IsNotNull(cut.Find("[role='dialog']"));
    }

    [TestMethod]
    public async Task A_Step_Template_Wins_Over_A_Tour_Template()
    {
        var step = Step("a");
        step.Template = _ => builder => builder.AddMarkupContent(0, "<span class=\"from-step\">step</span>");

        var tour = Tour(step);
        tour.Template = _ => builder => builder.AddMarkupContent(0, "<span class=\"from-tour\">tour</span>");

        await BonboardingService.StartAsync(tour);

        var cut = Render<BonboardingRoot>();

        Assert.IsNotNull(cut.Find(".from-step"));
        Assert.IsEmpty(cut.FindAll(".from-tour"));
    }

    // ---- Declarative markup -----------------------------------------------------------

    [TestMethod]
    public void A_Declarative_Tour_Registers_Its_Steps_In_Order()
    {
        var cut = Render<DeclarativeTourHarness>();

        var tour = BonboardingService.GetTour("declared");

        Assert.IsNotNull(tour);
        CollectionAssert.AreEqual(new[] { "one", "two", "three" }, tour!.Steps.Select(step => step.Id).ToArray());
        Assert.AreEqual("Second", tour.Steps[1].Title);
        Assert.AreEqual(BonboardingPlacement.Right, tour.Steps[1].Placement);
        Assert.AreEqual("[data-bonboarding-anchor=\"create\"]", BonboardingTargetDescriptor.From(tour.Steps[0].Target).Selector);
        Assert.IsNotNull(cut.Instance);
    }

    [TestMethod]
    public async Task A_Declarative_Tour_Can_Be_Driven_Through_Its_Reference()
    {
        _interop.ExistingSelectors.Add("[data-bonboarding-anchor=\"create\"]");

        var cut = Render<DeclarativeTourHarness>();
        var tour = cut.FindComponent<BonboardingTour>().Instance;

        await cut.InvokeAsync(() => tour.StartAsync());

        Assert.IsTrue(tour.IsRunning);
        Assert.AreEqual("one", tour.Session!.CurrentStep!.Id);

        await cut.InvokeAsync(() => tour.StopAsync());

        Assert.IsFalse(tour.IsRunning);
    }

    [TestMethod]
    public async Task A_Running_Tour_Keeps_Its_Steps_When_Its_Markup_Is_Disposed()
    {
        _interop.ExistingSelectors.Add("[data-bonboarding-anchor=\"create\"]");

        var cut = Render<ToggleableTourHarness>();
        var tour = cut.FindComponent<BonboardingTour>().Instance;
        var session = await cut.InvokeAsync(() => tour.StartAsync());

        // Navigating to the next page of a multi-page tour tears the declaring markup down.
        cut.Render(parameters => parameters.Add(p => p.Show, false));

        Assert.IsTrue(session.IsActive);
        CollectionAssert.AreEqual(new[] { "one", "two", "three" }, session.Tour.Steps.Select(step => step.Id).ToArray());

        await session.NextAsync();
        Assert.AreEqual("two", session.CurrentStep!.Id);
    }

    [TestMethod]
    public void A_Step_Rendered_Again_Returns_To_Its_Declared_Position()
    {
        var cut = Render<ToggleableTourHarness>();

        cut.Render(parameters => parameters.Add(p => p.ShowSecond, false));
        cut.Render(parameters => parameters.Add(p => p.ShowSecond, true));

        var tour = BonboardingService.GetTour("toggleable");
        CollectionAssert.AreEqual(new[] { "one", "two", "three" }, tour!.Steps.Select(step => step.Id).ToArray());
    }

    [TestMethod]
    public void An_Auto_Started_Tour_That_Declines_To_Start_Does_Not_Throw()
    {
        var cut = Render<ToggleableTourHarness>(parameters => parameters
            .Add(p => p.AutoStart, BonboardingAutoStart.Always)
            .Add(p => p.Configure, tour => tour.CanStart = _ => ValueTask.FromResult(false)));

        Assert.IsNull(BonboardingService.Active);
        Assert.IsNotNull(cut.Instance);
    }

    [TestMethod]
    public void A_Step_Outside_A_Tour_Fails_Loudly()
    {
        var failure = Assert.ThrowsExactly<InvalidOperationException>(() => Render<BonboardingStep>());

        Assert.Contains("must be placed inside", failure.Message);
    }

    [TestMethod]
    public async Task A_Host_With_An_Explicit_Session_Claims_It_From_The_Ambient_Host()
    {
        var session = await BonboardingService.StartAsync(Tour(Step("a", "Claimed")));

        var ambient = Render<BonboardingRoot>();
        Assert.IsNotEmpty(ambient.FindAll(".bit-bonboarding-popover"));

        // A second host takes over rendering, so the tour is not drawn twice.
        var owner = Render<BonboardingRoot>(parameters => parameters
            .Add(p => p.Session, session)
            .Add(p => p.PopoverClass, "owned"));

        Assert.IsNotEmpty(owner.FindAll(".owned"));
        Assert.IsEmpty(ambient.FindAll(".bit-bonboarding-popover"));

        // Releasing the claim hands the session back.
        await owner.Instance.DisposeAsync();
        ambient.Render();

        Assert.IsNotEmpty(ambient.FindAll(".bit-bonboarding-popover"));
    }

    [TestMethod]
    public async Task The_Host_Theme_Overrides_The_Tour_Theme()
    {
        var tour = Tour(Step("a"));
        tour.Theme = BonboardingTheme.Light;

        await BonboardingService.StartAsync(tour);

        var cut = Render<BonboardingRoot>(parameters => parameters.Add(p => p.Theme, BonboardingTheme.Dark));

        Assert.AreEqual("dark", cut.Find(".bit-bonboarding-root").GetAttribute("data-bonboarding-theme"));
    }

    // ---- Cleanup ---------------------------------------------------------------------

    [TestMethod]
    public async Task Disposing_The_Host_Unsubscribes_From_The_Session()
    {
        var session = await BonboardingService.StartAsync(Tour(Step("a"), Step("b")));

        var cut = Render<BonboardingRoot>();
        await cut.Instance.DisposeAsync();

        // Nothing should throw or re-render after the host is gone.
        await session.NextAsync();

        Assert.AreEqual("b", session.CurrentStep!.Id);
    }

    [TestMethod]
    public async Task Registers_The_Popover_Element_With_The_Browser_Layer()
    {
        await BonboardingService.StartAsync(Tour(Step("a")));

        Render<BonboardingRoot>();

        Assert.IsNotNull(_interop.Popover);
        Assert.IsTrue(_interop.TrapFocus);
        Assert.IsTrue(_interop.FocusPopoverCount > 0);
    }

    [TestMethod]
    public async Task Focus_Moves_Into_The_Popover_Only_Once_It_Has_Been_Placed()
    {
        var session = await BonboardingService.StartAsync(Tour(Step("a"), Step("b", selector: "#b")));

        var cut = Render<BonboardingRoot>();
        var focusedOnFirstStep = _interop.FocusPopoverCount;
        Assert.AreEqual(1, focusedOnFirstStep);

        // Hold the browser's reply so the next step renders before it has been measured, as it
        // does in a real browser: the popover is hidden until then, and a hidden element cannot
        // take focus.
        var measured = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _interop.DuringActivation = _ => measured.Task;

        var next = session.NextAsync();

        cut.WaitForAssertion(() =>
        {
            var popover = cut.Find(".bit-bonboarding-popover");
            Assert.AreEqual("false", popover.GetAttribute("data-bonboarding-placed"));
            Assert.AreEqual("b", cut.Find(".bit-bonboarding-title").TextContent);
        });
        Assert.AreEqual(focusedOnFirstStep, _interop.FocusPopoverCount, "Focused a popover that was still hidden.");

        measured.SetResult();
        await next;

        cut.WaitForAssertion(() =>
        {
            Assert.AreEqual("true", cut.Find(".bit-bonboarding-popover").GetAttribute("data-bonboarding-placed"));
            Assert.AreEqual(focusedOnFirstStep + 1, _interop.FocusPopoverCount);
        });
    }

    [TestMethod]
    public async Task Tour_Callbacks_Run_On_The_Renderer_When_The_Tour_Is_Driven_From_Elsewhere()
    {
        var cut = Render<CallbackTourHarness>();
        var tour = cut.FindComponent<BonboardingTour>().Instance;
        var session = await cut.InvokeAsync(() => tour.StartAsync());

        // Off the renderer, as a timer or a background service would drive it. The callback
        // re-renders its owner, which the renderer only allows on its own dispatcher.
        await Task.Run(() => session.NextAsync());

        Assert.IsNull(session.Error);
        Assert.AreEqual("two", session.CurrentStep!.Id);
        Assert.AreEqual(2, cut.Instance.StepChanges);
    }

    [TestMethod]
    public void An_Explicit_Order_Places_A_Late_Step_Ahead_Of_The_Step_At_That_Position()
    {
        var cut = Render<OrderedTourHarness>();

        cut.Render(parameters => parameters.Add(p => p.ShowAdvanced, true));

        var tour = BonboardingService.GetTour("ordered");
        CollectionAssert.AreEqual(new[] { "intro", "advanced", "finish" }, tour!.Steps.Select(step => step.Id).ToArray());
    }

    [TestMethod]
    public async Task A_Malformed_Label_Format_Falls_Back_Instead_Of_Breaking_The_Render()
    {
        var tour = Tour(Step("a", "Broken labels"));
        tour.Progress = BonboardingProgressStyle.Text;
        tour.Labels = new BonboardingLabels { ProgressFormat = "{0 of {1}", StepAnnouncementFormat = "Step {0}. {2" };

        await BonboardingService.StartAsync(tour);
        var cut = Render<BonboardingRoot>();

        Assert.AreEqual("1 of 1", cut.Find(".bit-bonboarding-progress-text").TextContent);
        Assert.AreEqual("Step 1 of 1. Broken labels", cut.Find(".bit-bonboarding-live").TextContent);
    }

    [TestMethod]
    public async Task A_Target_Only_Step_With_No_Target_On_Screen_Blocks_The_Page()
    {
        var step = Step("a", "Gone", "#missing");
        step.Interaction = BonboardingInteractionMode.TargetOnly;
        step.MissingTarget = BonboardingMissingTargetBehavior.Center;

        await BonboardingService.StartAsync(Tour(step));
        var cut = Render<BonboardingRoot>();

        var blockers = cut.FindAll(".bit-bonboarding-blocker");
        Assert.HasCount(1, blockers);
        Assert.AreEqual("inset:0", blockers[0].GetAttribute("style"));
    }

    [TestMethod]
    public async Task The_Live_Region_Empties_Between_Tours_So_A_Restart_Is_Announced()
    {
        var cut = Render<BonboardingRoot>();
        var tour = Tour(Step("a", "Welcome"));

        var first = await cut.InvokeAsync(() => BonboardingService.StartAsync(tour));
        cut.WaitForAssertion(() => Assert.Contains("Welcome", cut.Find(".bit-bonboarding-live").TextContent));

        await cut.InvokeAsync(() => first.DismissAsync());
        cut.WaitForAssertion(() => Assert.AreEqual(string.Empty, cut.Find(".bit-bonboarding-live").TextContent.Trim()));

        await cut.InvokeAsync(() => BonboardingService.StartAsync(tour));
        cut.WaitForAssertion(() => Assert.Contains("Welcome", cut.Find(".bit-bonboarding-live").TextContent));
    }
}

/// <summary>Markup fixture for the declarative component tests.</summary>
public sealed class DeclarativeTourHarness : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<BonboardingTour>(0);
        builder.AddComponentParameter(1, nameof(BonboardingTour.Id), "declared");
        builder.AddComponentParameter(2, nameof(BonboardingTour.Title), "Declared tour");
        builder.AddComponentParameter(3, nameof(BonboardingTour.ChildContent), (RenderFragment)(child =>
        {
            child.OpenComponent<BonboardingStep>(0);
            child.AddComponentParameter(1, nameof(BonboardingStep.Id), "one");
            child.AddComponentParameter(2, nameof(BonboardingStep.Anchor), "create");
            child.AddComponentParameter(3, nameof(BonboardingStep.Title), "First");
            child.CloseComponent();

            child.OpenComponent<BonboardingStep>(4);
            child.AddComponentParameter(5, nameof(BonboardingStep.Id), "two");
            child.AddComponentParameter(6, nameof(BonboardingStep.Selector), "#b");
            child.AddComponentParameter(7, nameof(BonboardingStep.Title), "Second");
            child.AddComponentParameter(8, nameof(BonboardingStep.Placement), (BonboardingPlacement?)BonboardingPlacement.Right);
            child.CloseComponent();

            child.OpenComponent<BonboardingStep>(9);
            child.AddComponentParameter(10, nameof(BonboardingStep.Id), "three");
            child.AddComponentParameter(11, nameof(BonboardingStep.Title), "Third");
            child.CloseComponent();
        }));
        builder.CloseComponent();
    }
}

/// <summary>Markup fixture whose tour, or one of its steps, can be removed and rendered again.</summary>
public sealed class ToggleableTourHarness : ComponentBase
{
    [Parameter] public bool Show { get; set; } = true;

    [Parameter] public bool ShowSecond { get; set; } = true;

    [Parameter] public BonboardingAutoStart AutoStart { get; set; }

    [Parameter] public Action<BonboardingTourDefinition>? Configure { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (!Show) return;

        builder.OpenComponent<BonboardingTour>(0);
        builder.AddComponentParameter(1, nameof(BonboardingTour.Id), "toggleable");
        builder.AddComponentParameter(2, nameof(BonboardingTour.AutoStart), AutoStart);
        builder.AddComponentParameter(3, nameof(BonboardingTour.Configure), Configure);
        builder.AddComponentParameter(4, nameof(BonboardingTour.ChildContent), (RenderFragment)(child =>
        {
            child.OpenComponent<BonboardingStep>(0);
            child.AddComponentParameter(1, nameof(BonboardingStep.Id), "one");
            child.AddComponentParameter(2, nameof(BonboardingStep.Anchor), "create");
            child.CloseComponent();

            if (ShowSecond)
            {
                child.OpenComponent<BonboardingStep>(3);
                child.AddComponentParameter(4, nameof(BonboardingStep.Id), "two");
                child.AddComponentParameter(5, nameof(BonboardingStep.Selector), "#b");
                child.CloseComponent();
            }

            child.OpenComponent<BonboardingStep>(6);
            child.AddComponentParameter(7, nameof(BonboardingStep.Id), "three");
            child.AddComponentParameter(8, nameof(BonboardingStep.Selector), "#c");
            child.CloseComponent();
        }));
        builder.CloseComponent();
    }
}

/// <summary>Markup fixture whose tour reports step changes through an <see cref="EventCallback"/>.</summary>
public sealed class CallbackTourHarness : ComponentBase
{
    public int StepChanges { get; private set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<BonboardingTour>(0);
        builder.AddComponentParameter(1, nameof(BonboardingTour.Id), "callbacks");
        builder.AddComponentParameter(2, nameof(BonboardingTour.OnStepChanged),
            EventCallback.Factory.Create<BonboardingStepChangedEventArgs>(this, _ => StepChanges++));
        builder.AddComponentParameter(3, nameof(BonboardingTour.ChildContent), (RenderFragment)(child =>
        {
            child.OpenComponent<BonboardingStep>(0);
            child.AddComponentParameter(1, nameof(BonboardingStep.Id), "one");
            child.AddComponentParameter(2, nameof(BonboardingStep.Selector), "#a");
            child.CloseComponent();

            child.OpenComponent<BonboardingStep>(3);
            child.AddComponentParameter(4, nameof(BonboardingStep.Id), "two");
            child.AddComponentParameter(5, nameof(BonboardingStep.Selector), "#b");
            child.CloseComponent();
        }));
        builder.CloseComponent();
    }
}

/// <summary>Markup fixture with a step that only renders later and pins its position with Order.</summary>
public sealed class OrderedTourHarness : ComponentBase
{
    [Parameter] public bool ShowAdvanced { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<BonboardingTour>(0);
        builder.AddComponentParameter(1, nameof(BonboardingTour.Id), "ordered");
        builder.AddComponentParameter(2, nameof(BonboardingTour.ChildContent), (RenderFragment)(child =>
        {
            child.OpenComponent<BonboardingStep>(0);
            child.AddComponentParameter(1, nameof(BonboardingStep.Id), "intro");
            child.CloseComponent();

            if (ShowAdvanced)
            {
                child.OpenComponent<BonboardingStep>(2);
                child.AddComponentParameter(3, nameof(BonboardingStep.Id), "advanced");
                child.AddComponentParameter(4, nameof(BonboardingStep.Order), (int?)2);
                child.CloseComponent();
            }

            child.OpenComponent<BonboardingStep>(5);
            child.AddComponentParameter(6, nameof(BonboardingStep.Id), "finish");
            child.CloseComponent();
        }));
        builder.CloseComponent();
    }
}
