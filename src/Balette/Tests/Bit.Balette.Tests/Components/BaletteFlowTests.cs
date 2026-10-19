using Microsoft.Extensions.DependencyInjection;

namespace Bit.Balette.Tests;

/// <summary>
/// Flows that start outside the open palette - a global shortcut, <see cref="IBaletteService.ExecuteAsync"/>,
/// <see cref="IBaletteService.OpenAsync"/> with a query or a page - and commands that live only in the palette's
/// markup or its <see cref="Balette.Commands"/> parameter rather than in the service's registry.
/// </summary>
[TestClass]
public class BaletteFlowTests : BunitContext
{
    private readonly IBaletteService _service;
    private readonly BunitJSModuleInterop _jsHandle;

    public BaletteFlowTests()
    {
        Services.AddBitBaletteServices();
        Services.AddBitBaletteInMemoryHistoryStore();
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

    private bool? LastOpenSelectsAll()
        => _jsHandle.Invocations.LastOrDefault(i => i.Identifier == "open").Arguments is { Count: 2 } args ? (bool?)args[1] : null;

    [TestMethod]
    public async Task Opening_with_a_query_keeps_it_for_typing_after()
    {
        _service.Register(BaletteCmd.New("Anything"));
        var cut = Render<Balette>();

        await cut.InvokeAsync(() => _service.OpenAsync(">"));
        cut.WaitForAssertion(() => Assert.IsFalse(LastOpenSelectsAll()));
        Assert.AreEqual(">", cut.Find("[data-balette=input]").GetAttribute("value"));

        await cut.InvokeAsync(() => _service.CloseAsync());
        await cut.InvokeAsync(() => _service.OpenAsync());
        cut.WaitForAssertion(() => Assert.IsTrue(LastOpenSelectsAll()));
    }

    [TestMethod]
    public async Task A_shortcut_whose_handler_prompts_opens_the_palette_on_the_prompt()
    {
        _service.Register(BaletteCmd.New("Rename…").Shortcut("F2")
            .Handler(_ => Task.FromResult(BaletteCommandResult.Prompt("New name", _ => Task.FromResult(BaletteCommandResult.Close)))));
        var cut = Render<Balette>();

        await cut.InvokeAsync(() => cut.Instance.OnGlobalShortcut("f2"));

        cut.WaitForAssertion(() =>
        {
            Assert.AreEqual("true", cut.Find("dialog").GetAttribute("data-open"));
            Assert.Contains("New name", cut.Find("[data-balette=prompt]").TextContent);
        });
        Assert.IsTrue(_service.IsOpen);
    }

    [TestMethod]
    public async Task Executing_a_page_command_from_code_opens_the_palette_on_its_page()
    {
        _service.Register(BaletteCmd.Page("Theme", BaletteCmd.New("Light"), BaletteCmd.New("Dark")));
        var cut = Render<Balette>();

        await cut.InvokeAsync(() => _service.ExecuteAsync("theme"));

        cut.WaitForAssertion(() =>
        {
            Assert.AreEqual("true", cut.Find("dialog").GetAttribute("data-open"));
            Assert.Contains("Theme", cut.Find("[data-balette=breadcrumbs]").TextContent);
            Assert.HasCount(2, cut.FindAll("[data-balette=item]"));
        });
    }

    [TestMethod]
    public async Task A_confirmation_started_from_a_shortcut_confirms_that_command_and_no_other()
    {
        var ran = new List<string>();
        _service.Register(
            BaletteCmd.Action("Harmless", () => ran.Add("harmless")),
            BaletteCmd.New("Delete project").Shortcut("Mod+Shift+D").Destructive("This cannot be undone").Handler(() => ran.Add("delete")));
        var cut = Render<Balette>();

        await cut.InvokeAsync(() => cut.Instance.OnGlobalShortcut("mod+shift+d"));
        cut.WaitForAssertion(() =>
        {
            var confirming = Assert.ContainsSingle(cut.FindAll("[data-confirming=true]"));
            Assert.AreEqual("true", confirming.GetAttribute("aria-selected"));
            Assert.Contains("Delete project", confirming.TextContent);
        });

        // The second Enter runs the active row, which has to be the command being confirmed.
        await cut.InvokeAsync(() => cut.Instance.OnKeyDown("Enter", false, false, false, false));

        AssertSequence.AreEqual(["delete"], ran);
    }

    [TestMethod]
    public async Task Commands_passed_to_the_palette_can_be_executed_and_opened_by_id()
    {
        var ran = false;
        var cut = Render<Balette>(p => p.Add(x => x.Commands, new[]
        {
            BaletteCmd.Action("Instance action", () => ran = true).Id("instance-action").Build(),
            BaletteCmd.Page("Instance page", BaletteCmd.New("Child one"), BaletteCmd.New("Child two")).Id("instance-page").Build(),
        }));

        await cut.InvokeAsync(() => _service.ExecuteAsync("instance-action"));
        Assert.IsTrue(ran);

        await cut.InvokeAsync(() => _service.OpenAsync(pageCommandId: "instance-page"));
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Instance page", cut.Find("[data-balette=breadcrumbs]").TextContent);
            Assert.HasCount(2, cut.FindAll("[data-balette=item]"));
        });
    }

    [TestMethod]
    public async Task Declarative_pages_can_be_opened_by_id()
    {
        var cut = Render<Balette>(p => p.AddChildContent<BaletteItem>(parent => parent
            .Add(x => x.Title, "New…")
            .Add(x => x.Id, "new")
            .AddChildContent<BaletteItem>(child => child.Add(x => x.Title, "Project"))));

        await cut.InvokeAsync(() => _service.OpenAsync(pageCommandId: "new"));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("New…", cut.Find("[data-balette=breadcrumbs]").TextContent);
            Assert.Contains("Project", Assert.ContainsSingle(cut.FindAll("[data-balette=item]")).TextContent);
        });
    }

    [TestMethod]
    public async Task Executing_an_unknown_id_throws()
    {
        var cut = Render<Balette>();

        await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() => cut.InvokeAsync(() => _service.ExecuteAsync("missing")));
    }

    [TestMethod]
    public async Task The_title_and_its_marks_render_as_one_inline_box()
    {
        _service.Register(BaletteCmd.New("Go to Settings"));
        var cut = Render<Balette>();
        await cut.InvokeAsync(() => _service.OpenAsync("gs"));

        cut.WaitForAssertion(() =>
        {
            var label = cut.Find("[data-balette=item] [data-balette=label]");
            Assert.AreEqual("Go to Settings", label.TextContent);
            Assert.HasCount(2, label.QuerySelectorAll("mark"));
        });
    }

    [TestMethod]
    public async Task A_toast_before_closing_is_rendered_while_the_palette_is_still_open()
    {
        _service.Register(BaletteCmd.Action("Copy link", _ => Task.FromResult(BaletteCommandResult.Toast("Link copied"))));
        var cut = Render<Balette>();
        await cut.InvokeAsync(() => _service.OpenAsync());
        cut.WaitForAssertion(() => Assert.ContainsSingle(cut.FindAll("[data-balette=item]")));

        // From the keyboard nothing renders until the whole flow is over, the close included, so the toast has to
        // be rendered on its own before the palette waits to close.
        var run = cut.InvokeAsync(() => cut.Instance.OnKeyDown("Enter", false, false, false, false));

        cut.WaitForAssertion(() => Assert.Contains("Link copied", cut.Find("[data-balette=toast]").TextContent));
        await run;
        cut.WaitForAssertion(() => Assert.AreEqual("false", cut.Find("dialog").GetAttribute("data-open")));
    }

    [TestMethod]
    public async Task A_command_that_keeps_the_palette_open_asks_for_no_focus_while_it_is_closed()
    {
        var toggled = 0;
        _service.Register(BaletteCmd.New("Word wrap").Id("toggle:wordwrap").Shortcut("Alt+Z")
            .Handler(_ => { toggled++; return Task.FromResult(BaletteCommandResult.KeepOpen); }));
        var cut = Render<Balette>();

        await cut.InvokeAsync(() => cut.Instance.OnGlobalShortcut("alt+z"));
        await cut.InvokeAsync(() => _service.ExecuteAsync("toggle:wordwrap"));

        Assert.AreEqual(2, toggled);
        Assert.AreEqual("false", cut.Find("dialog").GetAttribute("data-open"));
        Assert.IsFalse(_jsHandle.Invocations.Any(i => i.Identifier == "focusInput"), "The input only exists while the palette is open.");
    }

    [TestMethod]
    public void The_service_reports_the_detected_platform()
    {
        var raised = 0;
        _service.PlatformDetected += () => raised++;

        var cut = Render<Balette>();

        cut.WaitForAssertion(() => Assert.AreEqual(1, raised));
        Assert.IsFalse(_service.IsMac);
    }
}
