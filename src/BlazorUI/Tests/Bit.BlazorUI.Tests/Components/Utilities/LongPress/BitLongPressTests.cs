using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.JSInterop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Utilities.LongPress;

[TestClass]
public class BitLongPressTests : BunitTestContext
{
    private const string Setup = "BitBlazorUI.LongPress.setup";
    private const string Dispose = "BitBlazorUI.LongPress.dispose";

    private List<JSRuntimeInvocation> Setups() => Context.JSInterop.Invocations.Where(i => i.Identifier == Setup).ToList();

    [TestMethod]
    public void BitLongPressShouldRenderItsContentInAPlainContainer()
    {
        var component = RenderComponent<BitLongPress>(parameters => parameters.AddChildContent("<p class=\"content\">Hold me</p>"));

        // Nothing for assistive technologies to stop at until there is a name or a key to say.
        component.MarkupMatches(@"<div class=""bit-lpr"" id:ignore><p class=""content"">Hold me</p></div>");
    }

    [TestMethod]
    public void BitLongPressShouldBeANamedGroupWhenItHasAName()
    {
        var component = RenderComponent<BitLongPress>(parameters => parameters.Add(p => p.AriaLabel, "Photo"));

        var root = component.Find(".bit-lpr");
        Assert.AreEqual("group", root.GetAttribute("role"));
        Assert.AreEqual("Photo", root.GetAttribute("aria-label"));
        Assert.IsNull(root.GetAttribute("tabindex"), "a name alone puts nothing in the tab order");
    }

    [TestMethod]
    public void BitLongPressWithAKeyboardTriggerShouldBeATabStopThatNamesItsKeys()
    {
        var component = RenderComponent<BitLongPress>(parameters => parameters.Add(p => p.KeyboardTrigger, true));

        var root = component.Find(".bit-lpr");
        Assert.AreEqual("0", root.GetAttribute("tabindex"));
        Assert.AreEqual("group", root.GetAttribute("role"));
        Assert.AreEqual("Shift+F10 ContextMenu", root.GetAttribute("aria-keyshortcuts"));
    }

    [TestMethod]
    public void ADisabledBitLongPressShouldLeaveTheTabOrder()
    {
        var component = RenderComponent<BitLongPress>(parameters =>
        {
            parameters.Add(p => p.KeyboardTrigger, true);
            parameters.Add(p => p.Disabled, true);
        });

        var root = component.Find(".bit-lpr");
        Assert.IsTrue(root.ClassList.Contains("bit-dis"));
        Assert.IsNull(root.GetAttribute("tabindex"));
        Assert.IsNull(root.GetAttribute("aria-keyshortcuts"));
    }

    [TestMethod]
    public void BitLongPressShouldKeepTheRoleTabIndexAndKeysThePageGives()
    {
        // The splat only reaches the component through the render tree: HtmlAttributes is a plain parameter on
        // BitComponentBase rather than a CaptureUnmatchedValues one, so bUnit's AddUnmatched cannot feed it.
        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, []);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitLongPress>(0);
                builder.AddComponentParameter(1, nameof(BitLongPress.KeyboardTrigger), true);
                builder.AddComponentParameter(2, nameof(BitLongPress.TabIndex), "-1");
                builder.AddAttribute(3, "role", "listitem");
                builder.AddAttribute(4, "aria-keyshortcuts", "Shift+F10");
                builder.CloseComponent();
            });
        });

        var root = component.Find(".bit-lpr");
        Assert.AreEqual("-1", root.GetAttribute("tabindex"));
        Assert.AreEqual("listitem", root.GetAttribute("role"));
        Assert.AreEqual("Shift+F10", root.GetAttribute("aria-keyshortcuts"));
    }

    [TestMethod]
    public void BitLongPressShouldSetUpItsScriptWithTheDefaults()
    {
        Context.JSInterop.SetupVoid(Setup);

        RenderComponent<BitLongPress>();

        var invocation = Context.JSInterop.VerifyInvoke(Setup);
        Assert.AreEqual(500, invocation.Arguments[2]);    // delay
        Assert.AreEqual(10m, invocation.Arguments[3]);    // moveTolerance
        Assert.AreEqual(false, invocation.Arguments[4]);  // touchOnly
        Assert.AreEqual(false, invocation.Arguments[5]);  // keyboardTrigger
    }

    [TestMethod]
    public void BitLongPressShouldSetUpItsScriptWithItsOwnValues()
    {
        Context.JSInterop.SetupVoid(Setup);

        RenderComponent<BitLongPress>(parameters =>
        {
            parameters.Add(p => p.Delay, 1200);
            parameters.Add(p => p.MoveTolerance, 25m);
            parameters.Add(p => p.TouchOnly, true);
            parameters.Add(p => p.KeyboardTrigger, true);
        });

        var invocation = Context.JSInterop.VerifyInvoke(Setup);
        Assert.AreEqual(1200, invocation.Arguments[2]);
        Assert.AreEqual(25m, invocation.Arguments[3]);
        Assert.AreEqual(true, invocation.Arguments[4]);
        Assert.AreEqual(true, invocation.Arguments[5]);
    }

    [TestMethod]
    public void BitLongPressShouldNeverHandItsScriptANegativeDelayOrTolerance()
    {
        Context.JSInterop.SetupVoid(Setup);

        RenderComponent<BitLongPress>(parameters =>
        {
            parameters.Add(p => p.Delay, -5);
            parameters.Add(p => p.MoveTolerance, -1m);
        });

        var invocation = Context.JSInterop.VerifyInvoke(Setup);
        Assert.AreEqual(0, invocation.Arguments[2]);
        Assert.AreEqual(0m, invocation.Arguments[3]);
    }

    [TestMethod]
    public void BitLongPressShouldSetUpItsScriptAgainOnlyWhenWhatItIsSetUpWithChanges()
    {
        Context.JSInterop.SetupVoid(Setup);
        Context.JSInterop.SetupVoid(Dispose);

        var component = RenderComponent<BitLongPress>(parameters => parameters.Add(p => p.Delay, 300));

        component.Render(parameters => parameters.Add(p => p.Delay, 300));
        Assert.HasCount(1, Setups(), "an unchanged delay sets nothing up again");

        component.Render(parameters => parameters.Add(p => p.Delay, 700));

        Context.JSInterop.VerifyInvoke(Dispose);
        var setups = Setups();
        Assert.HasCount(2, setups);
        Assert.AreEqual(300, setups[0].Arguments[2]);
        Assert.AreEqual(700, setups[1].Arguments[2]);
    }

    [TestMethod]
    public void BitLongPressShouldSetUpItsScriptAgainAfterAFailedSetup()
    {
        Context.JSInterop.SetupVoid(Setup, _ => true).SetException(new JSDisconnectedException("Disconnected"));
        Context.JSInterop.SetupVoid(Dispose);

        var component = RenderComponent<BitLongPress>();

        component.Render();

        Assert.HasCount(2, Setups());
    }

    [TestMethod]
    public async Task BitLongPressShouldTearItsScriptDownWhenDisposed()
    {
        Context.JSInterop.SetupVoid(Setup);
        Context.JSInterop.SetupVoid(Dispose);

        RenderComponent<BitLongPress>();

        await Context.DisposeComponentsAsync();

        Context.JSInterop.VerifyInvoke(Dispose);
    }

    [TestMethod]
    public async Task BitLongPressShouldRaiseOnLongPressWithThePointAndThePointer()
    {
        BitLongPressEventArgs? raised = null;

        var component = RenderComponent<BitLongPress>(parameters => parameters.Add(p => p.OnLongPress, (BitLongPressEventArgs e) => raised = e));

        await component.InvokeAsync(() => component.Instance._OnLongPress(12.5, 40, "touch"));

        Assert.IsNotNull(raised);
        Assert.AreEqual(12.5, raised.ClientX);
        Assert.AreEqual(40, raised.ClientY);
        Assert.AreEqual("touch", raised.PointerType);
    }

    [TestMethod]
    public async Task ADisabledBitLongPressShouldRaiseNothing()
    {
        var count = 0;

        var component = RenderComponent<BitLongPress>(parameters =>
        {
            parameters.Add(p => p.Disabled, true);
            parameters.Add(p => p.KeyboardTrigger, true);
            parameters.Add(p => p.OnLongPress, () => count++);
        });

        await component.InvokeAsync(() => component.Instance._OnLongPress(1, 1, "mouse"));
        await component.InvokeAsync(() => component.Instance._OnLongPress(1, 1, "keyboard"));

        Assert.AreEqual(0, count);
    }

    [TestMethod]
    public async Task BitLongPressShouldTakeTheKeyboardOnlyWithAKeyboardTrigger()
    {
        var types = new List<string>();

        var component = RenderComponent<BitLongPress>(parameters => parameters.Add(p => p.OnLongPress, (BitLongPressEventArgs e) => types.Add(e.PointerType)));

        await component.InvokeAsync(() => component.Instance._OnLongPress(1, 1, "keyboard"));
        Assert.IsEmpty(types, "a long press the keyboard cannot reach takes no key");

        component.Render(parameters => parameters.Add(p => p.KeyboardTrigger, true));
        await component.InvokeAsync(() => component.Instance._OnLongPress(1, 1, "keyboard"));

        CollectionAssert.AreEqual(new[] { "keyboard" }, types);
    }
}
