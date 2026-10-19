using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Balette.Tests;

[TestClass]
public class BaletteComponentTests : BunitContext
{
    private readonly IBaletteService _service;
    private readonly BunitJSModuleInterop _jsHandle;

    public BaletteComponentTests()
    {
        Services.AddBitBaletteServices();
        Services.AddBitBaletteHistoryStore<BaletteInMemoryHistoryStore>();
        JSInterop.Mode = JSRuntimeMode.Loose;

        var module = JSInterop.SetupModule("./_content/Bit.Balette/bit-balette.js");
        var handle = _jsHandle = module.SetupModule("init", _ => true);
        handle.Setup<bool>("isMac").SetResult(false);
        handle.SetupVoid("open", _ => true).SetVoidResult();
        handle.SetupVoid("close").SetVoidResult();
        handle.SetupVoid("focusInput", _ => true).SetVoidResult();
        handle.SetupVoid("scrollIntoView", _ => true).SetVoidResult();
        handle.SetupVoid("setCommandShortcuts", _ => true).SetVoidResult();
        handle.SetupVoid("dispose").SetVoidResult();

        _service = Services.GetRequiredService<IBaletteService>();
    }

    private IRenderedComponent<Balette> RenderPalette() => Render<Balette>();

    [TestMethod]
    public void Renders_closed_dialog_initially()
    {
        var cut = RenderPalette();
        var dialog = cut.Find("dialog");
        Assert.AreEqual("false", dialog.GetAttribute("data-open"));
        Assert.IsEmpty(cut.FindAll("[data-balette=item]"));
    }

    [TestMethod]
    public async Task Opening_lists_registered_commands_grouped()
    {
        _service.Register(
            BaletteCmd.Navigate("Home", "/").Group("Navigation"),
            BaletteCmd.Navigate("Settings", "/settings").Group("Navigation"),
            BaletteCmd.Action("Toggle theme", () => { }).Group("Preferences"));

        var cut = RenderPalette();
        await cut.InvokeAsync(() => _service.OpenAsync());

        cut.WaitForAssertion(() =>
        {
            Assert.AreEqual("true", cut.Find("dialog").GetAttribute("data-open"));
            Assert.AreEqual(3, cut.FindAll("[data-balette=item]").Count);
            var headings = cut.FindAll("[data-balette=group-heading]").Select(h => h.TextContent.Trim()).ToList();
            Assert.Contains("Navigation", headings);
            Assert.Contains("Preferences", headings);
        });
    }

    [TestMethod]
    public async Task Typing_filters_and_highlights()
    {
        _service.Register(BaletteCmd.New("Open file"), BaletteCmd.New("Close window"), BaletteCmd.New("Save all"));
        var cut = RenderPalette();
        await cut.InvokeAsync(() => _service.OpenAsync());

        var input = cut.Find("[data-balette=input]");
        await cut.InvokeAsync(() => input.Input("clo"));

        cut.WaitForAssertion(() =>
        {
            var items = cut.FindAll("[data-balette=item]");
            Assert.ContainsSingle(items);
            Assert.Contains("Close window", items[0].TextContent);
            Assert.IsNotEmpty(cut.FindAll("[data-balette=mark]"));
        });
    }

    [TestMethod]
    public async Task Shows_empty_state_for_no_matches()
    {
        _service.Register(BaletteCmd.New("Alpha"));
        var cut = RenderPalette();
        await cut.InvokeAsync(() => _service.OpenAsync("zzz"));

        cut.WaitForAssertion(() =>
        {
            var empty = cut.Find("[data-balette=empty]");
            Assert.Contains("zzz", empty.TextContent);
        });
    }

    [TestMethod]
    public async Task Keyboard_navigation_moves_active_item_and_enter_executes()
    {
        var executed = new List<string>();
        _service.Register(
            BaletteCmd.Action("First", () => executed.Add("first")).Priority(BaletteCommandPriority.High),
            BaletteCmd.Action("Second", () => executed.Add("second")));

        var cut = RenderPalette();
        await cut.InvokeAsync(() => _service.OpenAsync());
        cut.WaitForAssertion(() => Assert.AreEqual(2, cut.FindAll("[data-balette=item]").Count));

        Assert.AreEqual("First", cut.Find("[data-active=true] .bit-balette-item-title").TextContent.Trim());

        await cut.InvokeAsync(() => cut.Instance.OnKeyDown("ArrowDown", false, false, false, false));
        Assert.AreEqual("Second", cut.Find("[data-active=true] .bit-balette-item-title").TextContent.Trim());

        await cut.InvokeAsync(() => cut.Instance.OnKeyDown("Enter", false, false, false, false));
        AssertSequence.AreEqual(["second"], executed);
        cut.WaitForAssertion(() => Assert.AreEqual("false", cut.Find("dialog").GetAttribute("data-open")));
    }

    [TestMethod]
    public async Task Loop_wraps_from_first_to_last()
    {
        _service.Register(BaletteCmd.New("A").Priority(10), BaletteCmd.New("B"), BaletteCmd.New("C").Priority(-10));
        var cut = RenderPalette();
        await cut.InvokeAsync(() => _service.OpenAsync());
        cut.WaitForAssertion(() => Assert.AreEqual(3, cut.FindAll("[data-balette=item]").Count));

        await cut.InvokeAsync(() => cut.Instance.OnKeyDown("ArrowUp", false, false, false, false));
        Assert.AreEqual("C", cut.Find("[data-active=true] .bit-balette-item-title").TextContent.Trim());
    }

    [TestMethod]
    public async Task Nested_pages_push_breadcrumb_and_backspace_pops()
    {
        _service.Register(BaletteCmd.Page("Theme", BaletteCmd.New("Light"), BaletteCmd.New("Dark")), BaletteCmd.New("Other"));
        var cut = RenderPalette();
        await cut.InvokeAsync(() => _service.OpenAsync());
        cut.WaitForAssertion(() => Assert.AreEqual(2, cut.FindAll("[data-balette=item]").Count));

        // Find "Theme" wherever it is listed rather than relying on the order.
        var items = cut.FindAll("[data-balette=item]");
        var themeIndex = items.ToList().FindIndex(i => i.TextContent.Contains("Theme"));
        for (var i = 0; i < themeIndex; i++)
        {
            await cut.InvokeAsync(() => cut.Instance.OnKeyDown("ArrowDown", false, false, false, false));
        }

        await cut.InvokeAsync(() => cut.Instance.OnKeyDown("Enter", false, false, false, false));

        cut.WaitForAssertion(() =>
        {
            Assert.AreEqual("true", cut.Find("dialog").GetAttribute("data-open"));
            Assert.Contains("Theme", cut.Find("[data-balette=breadcrumbs]").TextContent);
            var titles = cut.FindAll("[data-balette=item] .bit-balette-item-title").Select(t => t.TextContent.Trim()).ToList();
            AssertSequence.AreEqual(["Light", "Dark"], titles); // the declared order: nothing was typed to rank by
        });

        await cut.InvokeAsync(() => cut.Instance.OnKeyDown("Backspace", false, false, false, false));
        cut.WaitForAssertion(() =>
        {
            Assert.IsEmpty(cut.FindAll("[data-balette=breadcrumbs]"));
            Assert.AreEqual(2, cut.FindAll("[data-balette=item]").Count);
        });
    }

    [TestMethod]
    public async Task Escape_goes_back_then_closes()
    {
        _service.Register(BaletteCmd.Page("Theme", BaletteCmd.New("Light")));
        var cut = RenderPalette();
        await cut.InvokeAsync(() => _service.OpenAsync(pageCommandId: "theme"));
        cut.WaitForAssertion(() => Assert.Contains("Theme", cut.Find("[data-balette=breadcrumbs]").TextContent));

        await cut.InvokeAsync(() => cut.Instance.OnKeyDown("Escape", false, false, false, false));
        cut.WaitForAssertion(() => Assert.IsEmpty(cut.FindAll("[data-balette=breadcrumbs]")));
        Assert.AreEqual("true", cut.Find("dialog").GetAttribute("data-open"));

        await cut.InvokeAsync(() => cut.Instance.OnKeyDown("Escape", false, false, false, false));
        cut.WaitForAssertion(() => Assert.AreEqual("false", cut.Find("dialog").GetAttribute("data-open")));
    }

    [TestMethod]
    public async Task Mode_prefix_scopes_commands_and_shows_chip()
    {
        _service.RegisterMode(new BaletteCommandMode { Prefix = ">", Name = "Commands", Tags = ["command"], Description = "Run a command" });
        _service.Register(BaletteCmd.New("Run build").Tags("command"), BaletteCmd.New("Readme page"));
        var cut = RenderPalette();
        await cut.InvokeAsync(() => _service.OpenAsync(">"));

        cut.WaitForAssertion(() =>
        {
            Assert.AreEqual("Commands", cut.Find("[data-balette=mode]").TextContent.Trim());
            var titles = cut.FindAll("[data-balette=item] .bit-balette-item-title").Select(t => t.TextContent.Trim()).ToList();
            AssertSequence.AreEqual(["Run build"], titles);
        });
    }

    [TestMethod]
    public async Task Mode_hints_appear_on_empty_root_query()
    {
        _service.RegisterMode(new BaletteCommandMode { Prefix = ">", Name = "Commands", Tags = ["command"], Description = "Run a command" });
        _service.Register(BaletteCmd.New("Anything"));
        var cut = RenderPalette();
        await cut.InvokeAsync(() => _service.OpenAsync());
        cut.WaitForAssertion(() =>
        {
            var headings = cut.FindAll("[data-balette=group-heading]").Select(h => h.TextContent.Trim());
            Assert.Contains("Modes", headings);
        });
    }

    [TestMethod]
    public async Task Disabled_commands_render_disabled_and_do_not_execute()
    {
        var ran = false;
        _service.Register(BaletteCmd.Action("Deploy", () => ran = true).Disabled(true, "No permission"));
        var cut = RenderPalette();
        await cut.InvokeAsync(() => _service.OpenAsync());
        cut.WaitForAssertion(() => Assert.ContainsSingle(cut.FindAll("[data-balette=item][data-disabled=true]")));

        await cut.InvokeAsync(() => cut.Instance.OnKeyDown("Enter", false, false, false, false));
        Assert.IsFalse(ran);
        cut.WaitForAssertion(() => Assert.Contains("No permission", cut.Find("[data-balette=toast]").TextContent));
    }

    [TestMethod]
    public async Task Confirm_requires_second_enter()
    {
        var ran = false;
        _service.Register(BaletteCmd.Action("Delete everything", () => ran = true).Destructive("Really delete?"));
        var cut = RenderPalette();
        await cut.InvokeAsync(() => _service.OpenAsync());
        cut.WaitForAssertion(() => Assert.ContainsSingle(cut.FindAll("[data-balette=item]")));

        await cut.InvokeAsync(() => cut.Instance.OnKeyDown("Enter", false, false, false, false));
        Assert.IsFalse(ran);
        cut.WaitForAssertion(() => Assert.ContainsSingle(cut.FindAll("[data-confirming=true]")));

        await cut.InvokeAsync(() => cut.Instance.OnKeyDown("Enter", false, false, false, false));
        Assert.IsTrue(ran);
    }

    [TestMethod]
    public async Task Prompt_result_collects_text()
    {
        string? received = null;
        _service.Register(BaletteCmd.Action("Rename", _ => Task.FromResult(BaletteCommandResult.Prompt("New name", ctx =>
        {
            received = ctx.PromptValue;
            return Task.FromResult(BaletteCommandResult.Close);
        }))));

        var cut = RenderPalette();
        await cut.InvokeAsync(() => _service.OpenAsync());
        cut.WaitForAssertion(() => Assert.ContainsSingle(cut.FindAll("[data-balette=item]")));

        await cut.InvokeAsync(() => cut.Instance.OnKeyDown("Enter", false, false, false, false));
        cut.WaitForAssertion(() => Assert.IsNotNull(cut.Find("[data-balette=prompt]")));

        await cut.InvokeAsync(() => cut.Find("[data-balette=input]").Input("report.pdf"));
        await cut.InvokeAsync(() => cut.Instance.OnKeyDown("Enter", false, false, false, false));
        Assert.AreEqual("report.pdf", received);
    }

    [TestMethod]
    public async Task Page_result_pushes_ad_hoc_items()
    {
        _service.Register(BaletteCmd.Action("Pick", _ => Task.FromResult(BaletteCommandResult.Page("Pick one", [BaletteCmd.New("X"), BaletteCmd.New("Y")]))));
        var cut = RenderPalette();
        await cut.InvokeAsync(() => _service.OpenAsync());
        cut.WaitForAssertion(() => Assert.ContainsSingle(cut.FindAll("[data-balette=item]")));

        await cut.InvokeAsync(() => cut.Instance.OnKeyDown("Enter", false, false, false, false));
        cut.WaitForAssertion(() =>
        {
            var titles = cut.FindAll("[data-balette=item] .bit-balette-item-title").Select(t => t.TextContent.Trim()).ToList();
            AssertSequence.AreEqual(["X", "Y"], titles);
        });
    }

    [TestMethod]
    public async Task Multi_select_toggles_and_submits_selection()
    {
        IReadOnlyList<BaletteCommand>? submitted = null;
        _service.Register(BaletteCmd.New("Labels").MultiSelect().Children(BaletteCmd.New("Bug"), BaletteCmd.New("Feature"), BaletteCmd.New("Docs"))
            .Handler(ctx =>
            {
                submitted = ctx.SelectedCommands;
                return Task.FromResult(BaletteCommandResult.Close);
            }));

        var cut = RenderPalette();
        await cut.InvokeAsync(() => _service.OpenAsync(pageCommandId: "labels"));
        cut.WaitForAssertion(() => Assert.AreEqual(3, cut.FindAll("[data-balette=item]").Count));

        await cut.InvokeAsync(() => cut.Instance.OnKeyDown("Enter", false, false, false, false)); // toggle Bug
        await cut.InvokeAsync(() => cut.Instance.OnKeyDown("ArrowDown", false, false, false, false));
        await cut.InvokeAsync(() => cut.Instance.OnKeyDown("Enter", false, false, false, false)); // toggle Feature
        cut.WaitForAssertion(() => Assert.AreEqual(2, cut.FindAll("[data-checked=true]").Count));

        await cut.InvokeAsync(() => cut.Instance.OnKeyDown("Enter", true, false, false, false)); // Ctrl+Enter
        Assert.IsNotNull(submitted);
        AssertSequence.AreEqual(["Bug", "Feature"], submitted!.Select(c => c.Title).OrderBy(x => x));
    }

    [TestMethod]
    public async Task Declarative_items_register_and_unregister()
    {
        var cut = Render<Balette>(p => p.AddChildContent<BaletteGroup>(g => g
            .Add(x => x.Heading, "Declared")
            .AddChildContent<BaletteItem>(i => i.Add(x => x.Title, "From markup").Add(x => x.Href, "/markup"))));

        await cut.InvokeAsync(() => _service.OpenAsync());
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("From markup", cut.Find("[data-balette=list]").TextContent);
            Assert.Contains("Declared", cut.Find("[data-balette=group-heading]").TextContent);
        });
    }

    [TestMethod]
    public async Task Executed_event_fires_with_timing()
    {
        BaletteCommandExecutedEventArgs? args = null;
        _service.CommandExecuted += (_, e) => args = e;
        _service.Register(BaletteCmd.Action("Ping", () => { }));

        var cut = RenderPalette();
        await cut.InvokeAsync(() => _service.OpenAsync());
        cut.WaitForAssertion(() => Assert.ContainsSingle(cut.FindAll("[data-balette=item]")));
        await cut.InvokeAsync(() => cut.Instance.OnKeyDown("Enter", false, false, false, false));

        Assert.IsNotNull(args);
        Assert.AreEqual("Ping", args!.Command.Title);
        Assert.IsInstanceOfType<BaletteCommandResult.CloseResult>(args.Result);
    }

    [TestMethod]
    public async Task Recents_section_appears_after_execution()
    {
        _service.Register(BaletteCmd.Action("Ping", _ => Task.FromResult(BaletteCommandResult.KeepOpen)).Group("Tools"), BaletteCmd.New("Other").Group("Tools"));
        var cut = RenderPalette();
        await cut.InvokeAsync(() => _service.OpenAsync());
        cut.WaitForAssertion(() => Assert.AreEqual(2, cut.FindAll("[data-balette=item]").Count));

        var items = cut.FindAll("[data-balette=item]");
        var pingIndex = items.ToList().FindIndex(i => i.TextContent.Contains("Ping"));
        for (var i = 0; i < pingIndex; i++)
        {
            await cut.InvokeAsync(() => cut.Instance.OnKeyDown("ArrowDown", false, false, false, false));
        }

        await cut.InvokeAsync(() => cut.Instance.OnKeyDown("Enter", false, false, false, false));

        cut.WaitForAssertion(() =>
        {
            var headings = cut.FindAll("[data-balette=group-heading]").Select(h => h.TextContent.Trim()).ToList();
            Assert.Contains("Recent", headings);
        });
    }

    [TestMethod]
    public async Task Nested_declarative_items_open_a_page()
    {
        var created = new List<string>();
        var cut = Render<Balette>(p => p.AddChildContent<BaletteItem>(parent => parent
            .Add(x => x.Title, "New…")
            .Add(x => x.Id, "new")
            .AddChildContent<BaletteItem>(child => child
                .Add(x => x.Title, "Project")
                .Add(x => x.OnExecute, EventCallback.Factory.Create<BaletteCommandContext>(this, () => created.Add("project"))))));

        await cut.InvokeAsync(() => _service.OpenAsync());
        cut.WaitForAssertion(() =>
        {
            var titles = cut.FindAll("[data-balette=item] .bit-balette-item-title").Select(t => t.TextContent.Trim()).ToList();
            AssertSequence.AreEqual(["New…"], titles); // the child lives on the nested page, not the root
            Assert.ContainsSingle(cut.FindAll("[data-balette=item] .bit-balette-chevron"));
        });

        await cut.InvokeAsync(() => cut.Instance.OnKeyDown("Enter", false, false, false, false));
        cut.WaitForAssertion(() => AssertSequence.AreEqual(["Project"], cut.FindAll("[data-balette=item] .bit-balette-item-title").Select(t => t.TextContent.Trim())));

        await cut.InvokeAsync(() => cut.Instance.OnKeyDown("Enter", false, false, false, false));
        AssertSequence.AreEqual(["project"], created);
    }

    [TestMethod]
    public async Task Async_source_results_are_debounced_and_rendered_under_their_group()
    {
        var calls = new List<string>();
        _service.RegisterSource(new BaletteDelegateSource("Remote", async (q, ct) =>
        {
            calls.Add(q);
            await Task.Delay(10, ct);
            return [BaletteCmd.New($"Remote hit for {q}").Build()];
        }));

        var cut = RenderPalette();
        await cut.InvokeAsync(() => _service.OpenAsync());
        var input = cut.Find("[data-balette=input]");
        await cut.InvokeAsync(() => input.Input("a"));
        await cut.InvokeAsync(() => cut.Find("[data-balette=input]").Input("ab"));
        await cut.InvokeAsync(() => cut.Find("[data-balette=input]").Input("abc"));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Remote", cut.FindAll("[data-balette=group-heading]").Select(h => h.TextContent.Trim()));
            Assert.Contains("Remote hit for abc", cut.Find("[data-balette=list]").TextContent);
        }, TimeSpan.FromSeconds(3));

        AssertSequence.AreEqual(["abc"], calls); // keystrokes inside the debounce window never reach the source
    }

    [TestMethod]
    public async Task Slow_searches_wait_for_the_keystroke_to_paint_and_coalesce()
    {
        var queries = new List<string>();
        _service.Register(BaletteCmd.New("Alpha"), BaletteCmd.New("Beta"), BaletteCmd.New("Gamma"));
        var paint = _jsHandle.SetupVoid("afterPaint");
        var cut = Render<Balette>(p => p.Add(x => x.OnQueryChanged, q => queries.Add(q)));
        cut.Instance.SlowSearchThreshold = TimeSpan.Zero;
        await cut.InvokeAsync(() => _service.OpenAsync());
        cut.WaitForAssertion(() => Assert.AreEqual(3, cut.FindAll("[data-balette=item]").Count));

        await cut.InvokeAsync(() => cut.Find("[data-balette=input]").Input("g"));
        await cut.InvokeAsync(() => cut.Find("[data-balette=input]").Input("ga"));
        Assert.AreEqual(3, cut.FindAll("[data-balette=item]").Count); // not searched (nor re-rendered) before the paint

        await cut.InvokeAsync(paint.SetVoidResult);
        cut.WaitForAssertion(() => AssertSequence.AreEqual(["Gamma"], cut.FindAll("[data-balette=item] .bit-balette-item-title").Select(t => t.TextContent.Trim())));
        AssertSequence.AreEqual(["ga"], queries);
    }

    [TestMethod]
    public async Task Enter_during_a_deferred_search_runs_the_fresh_top_result()
    {
        var executed = new List<string>();
        _service.Register(
            BaletteCmd.Action("Alpha", () => executed.Add("alpha")).Priority(BaletteCommandPriority.High),
            BaletteCmd.Action("Beta", () => executed.Add("beta")));
        _jsHandle.SetupVoid("afterPaint"); // never completes: the keystroke stays deferred
        var cut = RenderPalette();
        cut.Instance.SlowSearchThreshold = TimeSpan.Zero;
        await cut.InvokeAsync(() => _service.OpenAsync());
        cut.WaitForAssertion(() => Assert.AreEqual(2, cut.FindAll("[data-balette=item]").Count));

        await cut.InvokeAsync(() => cut.Find("[data-balette=input]").Input("bet"));
        await cut.InvokeAsync(() => cut.Instance.OnKeyDown("Enter", false, false, false, false));

        AssertSequence.AreEqual(["beta"], executed);
    }

    [TestMethod]
    public async Task Closing_during_a_deferred_search_keeps_rendering()
    {
        _service.Register(BaletteCmd.New("Alpha"), BaletteCmd.New("Beta"));
        _jsHandle.SetupVoid("afterPaint");
        var cut = RenderPalette();
        cut.Instance.SlowSearchThreshold = TimeSpan.Zero;
        await cut.InvokeAsync(() => _service.OpenAsync());
        cut.WaitForAssertion(() => Assert.AreEqual(2, cut.FindAll("[data-balette=item]").Count));

        await cut.InvokeAsync(() => cut.Find("[data-balette=input]").Input("bet"));
        await cut.InvokeAsync(() => _service.CloseAsync());
        cut.WaitForAssertion(() => Assert.AreEqual("false", cut.Find("dialog").GetAttribute("data-open")));

        await cut.InvokeAsync(() => _service.OpenAsync());
        cut.WaitForAssertion(() => Assert.AreEqual(2, cut.FindAll("[data-balette=item]").Count));
    }

    [TestMethod]
    public async Task Commands_parameter_adds_instance_commands()
    {
        var cut = Render<Balette>(p => p.Add(x => x.Commands, new[] { BaletteCmd.New("Instance only").Build() }));
        await cut.InvokeAsync(() => _service.OpenAsync());
        cut.WaitForAssertion(() => Assert.Contains("Instance only", cut.Find("[data-balette=list]").TextContent));

        cut.Render(p => p.Add(x => x.Commands, new[] { BaletteCmd.New("Replaced").Build() }));
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Replaced", cut.Find("[data-balette=list]").TextContent);
            Assert.DoesNotContain("Instance only", cut.Find("[data-balette=list]").TextContent);
        });
    }

    [TestMethod]
    public async Task ResetOnClose_false_restores_query_and_page()
    {
        Services.GetRequiredService<BaletteOptions>().ResetOnClose = false;
        _service.Register(BaletteCmd.Page("Theme", BaletteCmd.New("Light"), BaletteCmd.New("Dark")));
        var cut = RenderPalette();

        await cut.InvokeAsync(() => _service.OpenAsync(pageCommandId: "theme"));
        await cut.InvokeAsync(() => cut.Find("[data-balette=input]").Input("da"));
        await cut.InvokeAsync(() => _service.CloseAsync());
        await cut.InvokeAsync(() => _service.OpenAsync());

        cut.WaitForAssertion(() =>
        {
            Assert.AreEqual("da", cut.Find("[data-balette=input]").GetAttribute("value"));
            Assert.Contains("Theme", cut.Find("[data-balette=breadcrumbs]").TextContent);
        });
    }

    [TestMethod]
    public async Task Vim_bindings_move_only_when_enabled()
    {
        _service.Register(BaletteCmd.New("A").Priority(10), BaletteCmd.New("B"));
        var cut = Render<Balette>(p => p.Add(x => x.VimBindings, false));
        await cut.InvokeAsync(() => _service.OpenAsync());
        cut.WaitForAssertion(() => Assert.AreEqual(2, cut.FindAll("[data-balette=item]").Count));

        await cut.InvokeAsync(() => cut.Instance.OnKeyDown("j", true, false, false, false));
        Assert.AreEqual("A", cut.Find("[data-active=true] .bit-balette-item-title").TextContent.Trim());

        cut.Render(p => p.Add(x => x.VimBindings, true));
        await cut.InvokeAsync(() => cut.Instance.OnKeyDown("j", true, false, false, false));
        Assert.AreEqual("B", cut.Find("[data-active=true] .bit-balette-item-title").TextContent.Trim());

        await cut.InvokeAsync(() => cut.Instance.OnKeyDown("n", true, false, false, false)); // Ctrl+N always on, loops
        Assert.AreEqual("A", cut.Find("[data-active=true] .bit-balette-item-title").TextContent.Trim());
    }

    [TestMethod]
    public async Task Trigger_opens_palette_with_query_and_shows_shortcut()
    {
        _service.Register(BaletteCmd.New("Run build").Tags("command"));
        _service.RegisterMode(new BaletteCommandMode { Prefix = ">", Name = "Commands", Tags = ["command"] });

        var palette = RenderPalette();
        var trigger = Render<BaletteTrigger>(p => p.Add(x => x.Query, ">"));

        Assert.AreEqual("Control+K", trigger.Find("button").GetAttribute("aria-keyshortcuts"));
        AssertSequence.AreEqual(["Ctrl", "K"], trigger.FindAll(".bit-balette-kbd").Select(k => k.TextContent));

        await trigger.InvokeAsync(() => trigger.Find("button").Click());
        palette.WaitForAssertion(() => Assert.AreEqual("Commands", palette.Find("[data-balette=mode]").TextContent.Trim()));
    }

    [TestMethod]
    public void Trigger_can_force_mac_symbols()
    {
        var trigger = Render<BaletteTrigger>(p => p.Add(x => x.IsMac, true));
        AssertSequence.AreEqual(["⌘", "K"], trigger.FindAll(".bit-balette-kbd").Select(k => k.TextContent));
        Assert.AreEqual("Meta+K", trigger.Find("button").GetAttribute("aria-keyshortcuts"));
    }

    [TestMethod]
    public void Unstyled_omits_the_styling_hook_class()
    {
        var cut = Render<Balette>(p => p.Add(x => x.Unstyled, true).Add(x => x.Class, "mine"));
        var dialog = cut.Find("dialog");
        Assert.DoesNotContain("bit-balette-dialog", dialog.ClassList);
        Assert.Contains("mine", dialog.ClassList);
        Assert.AreEqual("dialog", dialog.GetAttribute("data-balette"));
    }

    [TestMethod]
    public async Task Pin_shortcut_matches_shifted_uppercase_key()
    {
        _service.Register(BaletteCmd.New("Alpha").Group("G"), BaletteCmd.New("Beta").Group("G"));
        var cut = RenderPalette();
        await cut.InvokeAsync(() => _service.OpenAsync());
        cut.WaitForAssertion(() => Assert.AreEqual(2, cut.FindAll("[data-balette=item]").Count));

        // Browsers send key "P" (upper-case) when Shift is held.
        await cut.InvokeAsync(() => cut.Instance.OnKeyDown("P", true, true, false, false));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Pinned", cut.FindAll("[data-balette=group-heading]").Select(h => h.TextContent.Trim()));
            Assert.ContainsSingle(cut.FindAll("[data-balette=item] .bit-balette-pin"));
        });
    }

    [TestMethod]
    public async Task A_command_both_registered_and_passed_in_is_listed_once()
    {
        _service.Register(BaletteCmd.Navigate("Settings", "/settings"));
        var cut = Render<Balette>(p => p.Add(x => x.Commands, new[] { BaletteCmd.Navigate("Settings", "/settings").Build() }));

        // Opened again, the palette finds the history loaded and builds the recents from the ranked commands by id.
        await cut.InvokeAsync(() => _service.OpenAsync());
        await cut.InvokeAsync(() => _service.CloseAsync());
        await cut.InvokeAsync(() => _service.OpenAsync());

        cut.WaitForAssertion(() => Assert.ContainsSingle(cut.FindAll("[data-balette=item]")));
    }

    [TestMethod]
    public async Task Opening_and_closing_from_another_thread_reach_the_renderer()
    {
        _service.Register(BaletteCmd.New("Anything"));
        var cut = RenderPalette();

        await Task.Run(() => _service.OpenAsync());
        cut.WaitForAssertion(() => Assert.AreEqual("true", cut.Find("dialog").GetAttribute("data-open")));

        await Task.Run(() => _service.CloseAsync());
        cut.WaitForAssertion(() => Assert.AreEqual("false", cut.Find("dialog").GetAttribute("data-open")));
    }

    [TestMethod]
    public async Task The_highlight_follows_its_command_when_the_rows_reorder()
    {
        _service.Register(
            BaletteCmd.New("Other").Group("Tools"),
            BaletteCmd.Action("Word wrap", _ => Task.FromResult(BaletteCommandResult.KeepOpen)).Group("Tools"));
        var cut = RenderPalette();
        await cut.InvokeAsync(() => _service.OpenAsync());
        cut.WaitForAssertion(() => Assert.AreEqual(2, cut.FindAll("[data-balette=item]").Count));

        await cut.InvokeAsync(() => cut.Instance.OnKeyDown("ArrowDown", false, false, false, false));
        await cut.InvokeAsync(() => cut.Instance.OnKeyDown("Enter", false, false, false, false));

        // Run, the command moved up into Recent; the highlight went with it.
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Recent", cut.FindAll("[data-balette=group-heading]").Select(h => h.TextContent.Trim()));
            Assert.AreEqual("Word wrap", cut.Find("[data-active=true] .bit-balette-item-title").TextContent.Trim());
        });
    }

    [TestMethod]
    public async Task Moving_away_from_an_armed_command_clears_its_confirmation_prompt()
    {
        var ran = false;
        _service.Register(
            BaletteCmd.Action("Delete project", () => ran = true).Destructive("This permanently deletes the project").Priority(10),
            BaletteCmd.New("Other"));
        var cut = RenderPalette();
        await cut.InvokeAsync(() => _service.OpenAsync());
        cut.WaitForAssertion(() => Assert.AreEqual(2, cut.FindAll("[data-balette=item]").Count));

        await cut.InvokeAsync(() => cut.Instance.OnKeyDown("Enter", false, false, false, false));
        cut.WaitForAssertion(() => Assert.Contains("permanently deletes", cut.Find("[data-balette=toast]").TextContent));

        await cut.InvokeAsync(() => cut.Instance.OnKeyDown("ArrowDown", false, false, false, false));
        cut.WaitForAssertion(() =>
        {
            Assert.IsEmpty(cut.FindAll("[data-balette=toast]"));
            Assert.IsEmpty(cut.FindAll("[data-confirming=true]"));
        });
        Assert.IsFalse(ran);
    }

    [TestMethod]
    public async Task A_second_Enter_does_not_submit_a_running_prompt_again()
    {
        var submits = 0;
        var release = new TaskCompletionSource();
        _service.Register(BaletteCmd.Action("Rename", _ => Task.FromResult(BaletteCommandResult.Prompt("New name", async _ =>
        {
            submits++;
            await release.Task;
            return BaletteCommandResult.Close;
        }))));
        var cut = RenderPalette();
        await cut.InvokeAsync(() => _service.OpenAsync());
        cut.WaitForAssertion(() => Assert.ContainsSingle(cut.FindAll("[data-balette=item]")));
        await cut.InvokeAsync(() => cut.Instance.OnKeyDown("Enter", false, false, false, false));
        cut.WaitForAssertion(() => Assert.IsNotNull(cut.Find("[data-balette=prompt]")));
        await cut.InvokeAsync(() => cut.Find("[data-balette=input]").Input("report.pdf"));

        var first = cut.InvokeAsync(() => cut.Instance.OnKeyDown("Enter", false, false, false, false));
        var second = cut.InvokeAsync(() => cut.Instance.OnKeyDown("Enter", false, false, false, false));
        release.SetResult();
        await Task.WhenAll(first, second);

        Assert.AreEqual(1, submits);
    }

    [TestMethod]
    public async Task Closing_while_a_handler_runs_cancels_it_and_drops_its_answer()
    {
        var release = new TaskCompletionSource();
        var token = CancellationToken.None;
        _service.Register(BaletteCmd.Action("Export…", async ctx =>
        {
            token = ctx.CancellationToken;
            await release.Task;
            return BaletteCommandResult.Page("Export as", [BaletteCmd.New("PDF")]);
        }));
        var cut = RenderPalette();
        await cut.InvokeAsync(() => _service.OpenAsync());
        cut.WaitForAssertion(() => Assert.ContainsSingle(cut.FindAll("[data-balette=item]")));

        var running = cut.InvokeAsync(() => cut.Instance.OnKeyDown("Enter", false, false, false, false));
        await cut.InvokeAsync(() => cut.Instance.OnKeyDown("Escape", false, false, false, false));
        Assert.IsTrue(token.IsCancellationRequested);

        release.SetResult();
        await running;
        Assert.AreEqual("false", cut.Find("dialog").GetAttribute("data-open"));
    }

    [TestMethod]
    public async Task A_reset_drops_the_source_results_of_the_old_query()
    {
        _service.RegisterSource(new BaletteDelegateSource("Remote", (q, _) => Task.FromResult<IReadOnlyList<BaletteCommand>>([BaletteCmd.New($"Remote hit for {q}").Build()])));
        _service.Register(BaletteCmd.Action("Adapt layout", _ => Task.FromResult(BaletteCommandResult.Reset)));
        var cut = RenderPalette();
        await cut.InvokeAsync(() => _service.OpenAsync());
        await cut.InvokeAsync(() => cut.Find("[data-balette=input]").Input("ada"));
        cut.WaitForAssertion(() => Assert.Contains("Remote hit for ada", cut.Find("[data-balette=list]").TextContent), TimeSpan.FromSeconds(3));

        await cut.InvokeAsync(() => cut.Instance.OnKeyDown("Enter", false, false, false, false));

        cut.WaitForAssertion(() =>
        {
            Assert.AreEqual(string.Empty, cut.Find("[data-balette=input]").GetAttribute("value"));
            Assert.DoesNotContain("Remote hit", cut.Find("[data-balette=list]").TextContent);
        });
    }

    [TestMethod]
    public void Changed_vim_bindings_and_toggle_shortcut_reach_the_browser_layer()
    {
        var cut = Render<Balette>(p => p.Add(x => x.VimBindings, false));
        cut.WaitForAssertion(() => Assert.IsTrue(_jsHandle.Invocations.Any(i => i.Identifier == "isMac")));

        cut.Render(p => p.Add(x => x.VimBindings, true).Add(x => x.ToggleShortcut, "Ctrl+Shift+P"));

        Assert.AreEqual(true, _jsHandle.Invocations.Single(i => i.Identifier == "setVimBindings").Arguments[0]);
        var toggles = (IEnumerable<string>)_jsHandle.Invocations.Single(i => i.Identifier == "setToggleShortcuts").Arguments[0]!;
        Assert.Contains("ctrl+shift+p", toggles);
    }

    [TestMethod]
    public async Task Each_hover_suspension_is_rendered_with_a_fresh_value()
    {
        _service.Register(BaletteCmd.New("Alpha"));
        var cut = RenderPalette();
        await cut.InvokeAsync(() => _service.OpenAsync());
        Assert.IsNull(cut.Find("dialog").GetAttribute("data-hover-suspended"));

        await cut.InvokeAsync(() => cut.Find("[data-balette=input]").Input("al"));
        var first = cut.Find("dialog").GetAttribute("data-hover-suspended");
        Assert.IsNotNull(first);

        await cut.InvokeAsync(cut.Instance.OnPointerMoved);
        await cut.InvokeAsync(() => cut.Find("[data-balette=input]").Input("alp"));
        Assert.AreNotEqual(first, cut.Find("dialog").GetAttribute("data-hover-suspended"));
    }

    [TestMethod]
    public async Task Scrolling_to_a_row_passes_its_position_among_all_rows()
    {
        _service.Register(BaletteCmd.New("A").Group("One"), BaletteCmd.New("B").Group("Two"));
        var cut = Render<Balette>(p => p.Add(x => x.Virtualize, true));
        await cut.InvokeAsync(() => _service.OpenAsync());
        cut.WaitForAssertion(() => Assert.AreEqual(2, cut.FindAll("[data-balette=item]").Count));

        await cut.InvokeAsync(() => cut.Instance.OnKeyDown("End", false, false, false, false));

        // Heading One, A, heading Two, B: the browser layer estimates where an unrendered row lies from these.
        cut.WaitForAssertion(() =>
        {
            var args = _jsHandle.Invocations.Last(i => i.Identifier == "scrollIntoView").Arguments;
            Assert.AreEqual(3, args[2]);
            Assert.AreEqual(4, args[3]);
        });
    }
}
