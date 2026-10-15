using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Surfaces;

/// <summary>
/// End-to-end tests of BitModalService and BitModalContainer in a real browser (/e2e/modal-service): Show&lt;T&gt;,
/// Show(Type), Show(RenderFragment) and Show(RenderFragment&lt;BitModalReference&gt;); the content closing its own modal
/// through the cascaded reference (Close, Dismiss, CloseWith, TryClose, Update); Result, GetResult&lt;T&gt; and IsDismissed
/// for every way a modal ends; GetContentAsync, Rendered and the parameters factory; the CanClose guard; Update and Refresh
/// of an open modal; persistent modals across a container remount; CloseOnNavigation; OpenModals, GetModal, CloseAll,
/// OnAddModal, OnCloseModal and IsContainerAvailable; a BitParams cascade and the container's ModalParameters; a custom
/// container; a service with no container; the close icon, styling and RTL through BitModalParameters; the exit
/// animation the container waits for; the focus handed back; and the page going away with modals open.
/// </summary>
[TestClass]
public class BitModalServiceE2ETests : E2ETestBase
{
    private const string PagePath = "/e2e/modal-service";

    private ILocator Modal(string name) => Page.Locator($"[data-modal='{name}']");

    private ILocator Content(string name) => Page.Locator($"[data-modal='{name}'] > .bit-mdl-ctn");

    private ILocator In(string name, string selector) => Page.Locator($"[data-modal='{name}'] {selector}");

    private async Task ShowAsync(string buttonId, string name, bool waitForFocus = true)
    {
        await PressAsync(Page.Locator($"#{buttonId}"));
        await Expect(Content(name)).ToBeVisibleAsync();
        await Modal(name).EvaluateAsync("el => Promise.all(el.getAnimations({ subtree: true }).map(a => a.finished.catch(() => null)))");

        if (waitForFocus)
        {
            await Page.WaitForFunctionAsync("name => document.querySelector(`[data-modal='${name}']`)?.contains(document.activeElement) ?? false", name);
        }
    }

    // Presses a button the way that leaves the focus on it in every engine: WebKit does not focus a button it clicks, so
    // there it is focused and activated with the keyboard - which is what a focus handed back to it is measured against.
    private async Task PressAsync(ILocator button)
    {
        if (IsWebKit)
        {
            await button.FocusAsync();
            await button.PressAsync("Enter");
        }
        else
        {
            await button.ClickAsync();
        }
    }

    private Task ClickOverlayAsync(string name)
    {
        return In(name, "> .bit-mdl-ovl").ClickAsync(new() { Position = new() { X = 4, Y = 4 } });
    }

    // ---------- Show<T> and the cascaded reference ----------

    [TestMethod]
    public async Task Show_ComponentContent_RendersItInAModalItCanCloseItself()
    {
        await OpenAsync(PagePath);
        await ShowAsync("btn-svc-basic", "basic");

        await Expect(Content("basic")).ToHaveAttributeAsync("role", "dialog");
        await Expect(Content("basic")).ToHaveAttributeAsync("aria-label", "From the service");
        await Expect(Content("basic")).ToHaveAttributeAsync("aria-modal", "true");
        await Expect(In("basic", ".svc-text")).ToHaveTextAsync("Hello from the service");
        await Expect(In("basic", ".svc-id")).ToHaveTextAsync((await Page.Locator("#svc-basic-id").TextContentAsync())!);
        await Expect(Page.Locator("#svc-added")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#svc-open-count")).ToHaveTextAsync("1");

        await In("basic", ".svc-close").ClickAsync();

        await Expect(Modal("basic")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#svc-basic-result")).ToHaveTextAsync("null");
        await Expect(Page.Locator("#svc-basic-dismissed")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#svc-closed")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#btn-svc-basic")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task CloseWith_AnswersTheAwaitedResult()
    {
        await OpenAsync(PagePath);
        await ShowAsync("btn-svc-basic", "basic");

        await In("basic", ".svc-closewith").ClickAsync();

        await Expect(Page.Locator("#svc-basic-result")).ToHaveTextAsync("yes");
        await Expect(Page.Locator("#svc-basic-dismissed")).ToHaveTextAsync("False");
    }

    [TestMethod]
    [DataRow("dismiss")]
    [DataRow("escape")]
    [DataRow("overlay")]
    public async Task ADismissal_CompletesTheResultWithNullAndMarksTheReferenceDismissed(string gesture)
    {
        await OpenAsync(PagePath);
        await ShowAsync("btn-svc-basic", "basic");

        switch (gesture)
        {
            case "dismiss": await In("basic", ".svc-dismiss").ClickAsync(); break;
            case "escape": await Page.Keyboard.PressAsync("Escape"); break;
            case "overlay": await ClickOverlayAsync("basic"); break;
        }

        await Expect(Modal("basic")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#svc-basic-result")).ToHaveTextAsync("null");
        await Expect(Page.Locator("#svc-basic-dismissed")).ToHaveTextAsync("True");
        if (gesture == "dismiss") await Expect(Page.Locator("#svc-log")).ToHaveTextAsync("Dismiss:True");
    }

    [TestMethod]
    public async Task Closing_PlaysTheExitAnimationBeforeTheContainerTakesTheModalOut()
    {
        await OpenAsync(PagePath);
        await ShowAsync("btn-svc-basic", "basic");
        await Modal("basic").EvaluateAsync(@"el => {
            window.__e2eClasses = [];
            new MutationObserver(records => records.forEach(r => window.__e2eClasses.push(r.target.className)))
                .observe(el, { attributes: true, attributeFilter: ['class'] });
        }");

        await In("basic", ".svc-close").ClickAsync();

        await Expect(Modal("basic")).ToHaveCountAsync(0);
        var classes = await Page.EvaluateAsync<string[]>("() => window.__e2eClasses");
        Assert.IsTrue(classes.Any(c => Regex.IsMatch(c, @"(^|\s)bit-mdl-lvg(\s|$)")), string.Join(" | ", classes));
    }

    [TestMethod]
    public async Task BitModalParameters_HeaderFooterAndCloseButton_BuildTheChrome()
    {
        await OpenAsync(PagePath);
        await ShowAsync("btn-svc-chrome", "chrome");

        await Expect(In("chrome", ".bit-mdl-hdr")).ToHaveTextAsync("Service header");
        await Expect(In("chrome", ".bit-mdl-fcn")).ToHaveTextAsync("Service footer");
        await Expect(Content("chrome")).ToHaveAttributeAsync("aria-labelledby", (await In("chrome", ".bit-mdl-hdr").GetAttributeAsync("id"))!);

        await In("chrome", ".bit-mdl-cls").ClickAsync();
        await Expect(Modal("chrome")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task ShowType_RendersAComponentKnownOnlyAtRunTime()
    {
        await OpenAsync(PagePath);
        await ShowAsync("btn-svc-type", "type");

        await Expect(In("type", ".svc-text")).ToHaveTextAsync("Shown by type");
        await Expect(Content("type")).ToHaveAttributeAsync("aria-label", "By type");
    }

    // ---------- Markup ----------

    [TestMethod]
    public async Task ShowMarkup_RendersTheFragmentAndLeavesContentNull()
    {
        await OpenAsync(PagePath);
        await ShowAsync("btn-svc-markup", "markup");

        await Expect(In("markup", ".svc-markup")).ToHaveTextAsync("Markup content");
        await Expect(Page.Locator("#svc-markup-content")).ToHaveTextAsync("null");
    }

    [TestMethod]
    public async Task ShowMarkupWithTheReference_LetsTheMarkupCloseItsOwnModal()
    {
        await OpenAsync(PagePath);
        await ShowAsync("btn-svc-markup-ref", "markup-ref");

        await Expect(In("markup-ref", ".svc-markup-id")).Not.ToBeEmptyAsync();
        await In("markup-ref", ".svc-markup-close").ClickAsync();

        await Expect(Modal("markup-ref")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#svc-closed")).ToHaveTextAsync("1");
    }

    // ---------- Awaiting a result ----------

    [TestMethod]
    public async Task GetResult_TypedAnswer_AndADismissalAnswersTheDefault()
    {
        await OpenAsync(PagePath);
        await ShowAsync("btn-svc-confirm", "confirm");

        await Expect(Content("confirm")).ToHaveAttributeAsync("role", "alertdialog");
        await Expect(In("confirm", ".svc-text")).ToHaveTextAsync("Delete the project?");
        await In("confirm", ".svc-closewith").ClickAsync();
        await Expect(Page.Locator("#svc-confirm")).ToHaveTextAsync("True");
        await Expect(Modal("confirm")).ToHaveCountAsync(0);

        await ShowAsync("btn-svc-confirm", "confirm");
        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#svc-confirm")).ToHaveTextAsync("dismissed:False");
    }

    // ---------- Reaching the content ----------

    [TestMethod]
    public async Task GetContentAsync_WaitsForTheRenderAndHandsTheInstanceBack()
    {
        await OpenAsync(PagePath);
        await ShowAsync("btn-svc-content", "content");

        await Expect(Page.Locator("#svc-rendered")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#svc-content-type")).ToHaveTextAsync("BitModalServiceE2EContent");

        await In("content", ".svc-count").ClickAsync();
        await In("content", ".svc-count").ClickAsync();
        await Expect(In("content", ".svc-count")).ToHaveTextAsync("Count 2");

        // The modal is open over the page, so the page's own button is pressed through the script.
        await Page.Locator("#btn-svc-read-content").DispatchEventAsync("click");
        await Expect(Page.Locator("#svc-content-clicks")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task ParametersFactory_HandsTheReferenceToTheContentBeforeItRenders()
    {
        await OpenAsync(PagePath);
        await ShowAsync("btn-svc-factory", "factory");

        var id = (await Page.Locator("#svc-factory-id").TextContentAsync())!;
        await Expect(In("factory", ".svc-text")).ToHaveTextAsync($"factory:{id}");
    }

    // ---------- Guarding the close ----------

    [TestMethod]
    public async Task CanClose_RefusesTheUsersClosingAndTryCloseUntilItAllows()
    {
        await OpenAsync(PagePath);
        await ShowAsync("btn-svc-guard", "guard");

        await In("guard", ".bit-mdl-cls").ClickAsync();
        await Expect(Page.Locator("#svc-log")).ToHaveTextAsync("CanClose:False");
        await Expect(Content("guard")).ToHaveClassAsync(HasClass("bit-mdl-bna"));

        await In("guard", ".svc-tryclose").ClickAsync();
        await Expect(Page.Locator("#svc-log")).ToHaveTextAsync("CanClose:False,CanClose:False,TryClose:False");

        await In("guard", ".svc-allow").FocusAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#svc-log")).ToHaveTextAsync("CanClose:False,CanClose:False,TryClose:False,CanClose:False");
        await Expect(Modal("guard")).ToBeVisibleAsync();

        await In("guard", ".svc-allow").ClickAsync();
        await In("guard", ".svc-tryclose").ClickAsync();

        await Expect(Modal("guard")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#svc-guard-result")).ToHaveTextAsync("tried:False");
        await Expect(Page.Locator("#svc-log")).ToContainTextAsync("CanClose:True,TryClose:True");
    }

    [TestMethod]
    public async Task CanClose_AllowedDismissal_ClosesAsADismissal()
    {
        await OpenAsync(PagePath);
        await ShowAsync("btn-svc-guard", "guard");

        await In("guard", ".svc-allow").ClickAsync();
        await In("guard", ".bit-mdl-cls").ClickAsync();

        await Expect(Modal("guard")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#svc-guard-result")).ToHaveTextAsync("null:True");
    }

    // ---------- Updating an open modal ----------

    [TestMethod]
    public async Task Update_ChangesWhatItNamesOrReplacesTheWholeSetAndRefreshPicksUpAMutation()
    {
        await OpenAsync(PagePath);
        await ShowAsync("btn-svc-update", "update");

        await Expect(In("update", ".bit-mdl-hdr")).ToHaveTextAsync("Step 1");
        await Expect(Modal("update")).ToHaveClassAsync(HasClass("bit-mdl-brg"));

        await In("update", ".svc-retitle").ClickAsync();
        await Expect(In("update", ".bit-mdl-hdr")).ToHaveTextAsync("Retitled");
        await Expect(Modal("update")).ToHaveClassAsync(HasClass("bit-mdl-brg"));

        await Page.Locator("#btn-svc-update-all").ClickAsync();
        await Expect(In("update", ".bit-mdl-hdr")).ToHaveTextAsync("Replaced");
        await Expect(In("update", ".bit-mdl-cls")).ToBeVisibleAsync();
        await Expect(Modal("update")).ToHaveClassAsync(HasClass("bit-mdl-blf"));

        await Page.Locator("#btn-svc-mutate").ClickAsync();
        await Expect(In("update", ".bit-mdl-hdr")).ToHaveTextAsync("Mutated");

        await Page.Locator("#btn-svc-mutate-all").ClickAsync();
        await Expect(In("update", ".bit-mdl-hdr")).ToHaveTextAsync("Mutated all");
    }

    // ---------- Persistent modals ----------

    [TestMethod]
    public async Task Persistent_SurvivesAContainerRemountWhileAnOrdinaryModalCloses()
    {
        await OpenAsync(PagePath);
        await ShowAsync("btn-svc-persistent", "persistent", waitForFocus: false);
        await ShowAsync("btn-svc-ordinary", "ordinary", waitForFocus: false);

        await Page.Locator("#btn-svc-toggle-container").ClickAsync();
        await Expect(Page.Locator("#svc-persist-mounted")).ToHaveTextAsync("False");

        await Expect(Modal("persistent")).ToHaveCountAsync(0);
        await Expect(Modal("ordinary")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#svc-ordinary-result")).ToHaveTextAsync("null");
        await Page.Locator("#btn-svc-persist-refresh").ClickAsync();
        await Expect(Page.Locator("#svc-ordinary-closed")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#svc-persistent-closed")).ToHaveTextAsync("False");

        await Page.Locator("#btn-svc-toggle-container").ClickAsync();

        await Expect(Content("persistent")).ToBeVisibleAsync();
        await Expect(In("persistent", ".svc-text")).ToHaveTextAsync("Persistent");
        await Expect(Modal("ordinary")).ToHaveCountAsync(0);
    }

    // ---------- Closing on navigation ----------

    [TestMethod]
    public async Task Navigation_ANewPathClosesTheModalsThatDidNotAskToStay()
    {
        await OpenAsync(PagePath);
        await ShowAsync("btn-svc-nav", "nav", waitForFocus: false);
        await ShowAsync("btn-svc-linger", "linger", waitForFocus: false);

        await Page.Locator("#btn-svc-query").ClickAsync();
        await Page.WaitForFunctionAsync("() => location.search === '?q=1'");
        await Page.Locator("#btn-svc-nav-refresh").ClickAsync();
        await Expect(Page.Locator("#svc-nav-closed")).ToHaveTextAsync("False");
        await Expect(Modal("nav")).ToBeVisibleAsync();

        await Page.Locator("#btn-svc-other").ClickAsync();
        await Expect(Page.Locator("#svc-section")).ToHaveTextAsync("other");

        await Expect(Modal("nav")).ToHaveCountAsync(0);
        await Page.Locator("#btn-svc-nav-refresh").ClickAsync();
        await Expect(Page.Locator("#svc-nav-closed")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#svc-linger-closed")).ToHaveTextAsync("False");
        await Expect(Content("linger")).ToBeVisibleAsync();
    }

    // ---------- Tracking open modals ----------

    [TestMethod]
    public async Task Tracking_OpenModalsGetModalCloseAllAndTheServiceEvents()
    {
        await OpenAsync(PagePath);
        await Expect(Page.Locator("#svc-available")).ToHaveTextAsync("True");

        for (var i = 1; i <= 3; i++)
        {
            await ShowAsync("btn-svc-stack", $"stack-{i}", waitForFocus: false);
        }

        await Expect(Page.Locator("#svc-open-count")).ToHaveTextAsync("3");
        await Expect(Page.Locator("#svc-added")).ToHaveTextAsync("3");

        await Page.Locator("#btn-svc-getmodal").ClickAsync();
        await Expect(Page.Locator("#svc-getmodal")).ToHaveTextAsync("found");

        await Page.Locator("#btn-svc-closeall").ClickAsync();

        await Expect(Page.Locator("#svc-closed")).ToHaveTextAsync("3");
        await Expect(Page.Locator("#svc-open-count")).ToHaveTextAsync("0");
        await Expect(Page.Locator("[data-modal^='stack-']")).ToHaveCountAsync(0);

        await Page.Locator("#btn-svc-getmodal").ClickAsync();
        await Expect(Page.Locator("#svc-getmodal")).ToHaveTextAsync("null");
    }

    // ---------- Cascading parameters and the container's defaults ----------

    [TestMethod]
    public async Task BitParams_AroundTheContainer_AreDefaultsAShowingCanOverride()
    {
        await OpenAsync(PagePath);
        await ShowAsync("btn-svc-cascaded", "cascaded");

        await Expect(Modal("cascaded")).ToHaveClassAsync(HasClass("bit-mdl-bcr"));
        await Expect(Modal("cascaded")).ToHaveClassAsync(HasClass("bit-mdl-mfl"));
        await Expect(In("cascaded", ".bit-mdl-cls")).ToBeVisibleAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Modal("cascaded")).ToHaveCountAsync(0);

        await ShowAsync("btn-svc-cascaded-own", "cascaded-own");
        await Expect(Modal("cascaded-own")).ToHaveClassAsync(HasClass("bit-mdl-tcr"));
        await Expect(Modal("cascaded-own")).Not.ToHaveClassAsync(HasClass("bit-mdl-bcr"));
    }

    [TestMethod]
    public async Task ContainerModalParameters_AreTheHouseDefaultsAShowingCanOverride()
    {
        await OpenAsync(PagePath);
        await ShowAsync("btn-svc-house", "house");

        await Expect(In("house", ".bit-mdl-hdr")).ToHaveTextAsync("House header");
        await Expect(In("house", ".bit-mdl-cls")).ToBeVisibleAsync();
        await Expect(Content("house")).ToHaveCSSAsync("max-width", "352px");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Modal("house")).ToHaveCountAsync(0);

        await ShowAsync("btn-svc-house-own", "house-own");
        await Expect(In("house-own", ".bit-mdl-hdr")).ToHaveTextAsync("Own header");
        await Expect(In("house-own", ".bit-mdl-cls")).ToBeVisibleAsync();
    }

    // ---------- Custom container ----------

    [TestMethod]
    public async Task CustomContainer_RendersEachModalInsideItsOwnMarkup()
    {
        await OpenAsync(PagePath);
        await ShowAsync("btn-svc-custom", "custom");

        var id = (await In("custom", ".svc-id").TextContentAsync())!;
        await Expect(Page.Locator($".svc-slot[data-modal-id='{id}'] [data-modal='custom']")).ToHaveCountAsync(1);
        await Expect(Page.Locator("#custom-container-count")).ToHaveTextAsync("1");

        await In("custom", ".svc-close").ClickAsync();
        await Expect(Modal("custom")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#custom-container-count")).ToHaveTextAsync("0");
    }

    // ---------- No container ----------

    [TestMethod]
    public async Task NoContainer_TheModalIsNeverRenderedAndRenderedSaysSo()
    {
        await OpenAsync(PagePath);
        await Expect(Page.Locator("#svc-orphan-available")).ToHaveTextAsync("False");

        await Page.Locator("#btn-svc-orphan").ClickAsync();

        await Expect(Page.Locator("#svc-orphan-rendered")).ToHaveTextAsync("False");
        await Expect(Page.Locator(".bit-mdl")).ToHaveCountAsync(0);
    }

    // ---------- Look ----------

    [TestMethod]
    public async Task BitModalParameters_CloseIcon_IsTheExternalGlyphWithItsTitle()
    {
        await OpenAsync(PagePath);
        await ShowAsync("btn-svc-icon", "icon");

        await Expect(In("icon", ".bit-mdl-cls")).ToHaveAttributeAsync("aria-label", "Dismiss");
        await Expect(In("icon", ".bit-mdl-cls i")).ToHaveClassAsync(HasClass("fa-xmark"));
        await Expect(In("icon", ".bit-mdl-cls i")).ToHaveClassAsync(HasClass("fa-solid"));
    }

    [TestMethod]
    public async Task BitModalParameters_StyleClassStylesAndClasses_ReachTheModal()
    {
        await OpenAsync(PagePath);
        await ShowAsync("btn-svc-styled", "styled");

        await Expect(Modal("styled")).ToHaveClassAsync(HasClass("svc-class"));
        await Expect(Content("styled")).ToHaveCSSAsync("background-color", "rgb(30, 27, 75)");
        await Expect(Content("styled")).ToHaveCSSAsync("color", "rgb(224, 231, 255)");
        await Expect(Content("styled")).ToHaveCSSAsync("outline-color", "rgb(1, 2, 3)");
        await Expect(In("styled", ".bit-mdl-hdr")).ToHaveCSSAsync("letter-spacing", "3px");
        await Expect(In("styled", ".bit-mdl-bdy")).ToHaveClassAsync(HasClass("svc-body"));
        await Expect(In("styled", ".bit-mdl-fcn")).ToHaveClassAsync(HasClass("svc-footer"));
    }

    [TestMethod]
    public async Task BitModalParameters_Dir_LaysTheModalOutRightToLeft()
    {
        await OpenAsync(PagePath);
        await ShowAsync("btn-svc-rtl", "rtl");

        await Expect(Modal("rtl")).ToHaveClassAsync(HasClass("bit-rtl"));
        await Expect(Modal("rtl")).ToHaveAttributeAsync("dir", "rtl");

        var root = (await Modal("rtl").BoundingBoxAsync())!;
        var box = (await Content("rtl").BoundingBoxAsync())!;
        Assert.AreEqual(root.X + root.Width, box.X + box.Width, 2, "TopStart lands on the right.");
        Assert.AreEqual(root.Y, box.Y, 2);
    }

    // ---------- Lifecycle ----------

    [TestMethod]
    public async Task LeavingThePage_WithModalsOpen_ClosesThemWithoutErrors()
    {
        await OpenAsync(PagePath);
        await ShowAsync("btn-svc-stack", "stack-1", waitForFocus: false);

        await Page.Locator("#btn-svc-home").ClickAsync();

        await Expect(Page.Locator("h1")).ToHaveTextAsync("Bit.BlazorUI E2E Test Host");
        await Expect(Page.Locator(".bit-mdl")).ToHaveCountAsync(0);
        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }
}
