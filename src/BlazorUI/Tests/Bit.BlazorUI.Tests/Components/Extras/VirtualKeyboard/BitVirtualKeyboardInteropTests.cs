using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.VirtualKeyboard;

/// <summary>
/// What the keyboard tells its script (the options and their version) and what it makes of what the script tells it back.
/// </summary>
[TestClass]
public class BitVirtualKeyboardInteropTests : BunitTestContext
{
    private const string Setup = "BitBlazorUI.BitVirtualKeyboard.setup";

    private BunitJSModuleInterop SetupHandle()
    {
        var module = Context.JSInterop.SetupModule(invocation => invocation.Identifier == Setup);
        module.Mode = JSRuntimeMode.Loose;
        return module;
    }

    private BitVirtualKeyboardJsOptions SetupOptions() => (BitVirtualKeyboardJsOptions)Context.JSInterop.Invocations[Setup].Single().Arguments[2]!;

    private static List<BitVirtualKeyboardJsOptions> Updates(BunitJSModuleInterop handle) =>
        [.. handle.Invocations.Where(i => i.Identifier == "update").Select(i => (BitVirtualKeyboardJsOptions)i.Arguments[0]!)];

    [TestMethod]
    public void TheScriptShouldBeSetUpOnceWithTheLayoutAndTheOptions()
    {
        SetupHandle();

        var component = RenderComponent<BitVirtualKeyboard>(p => p.Add(c => c.Form, BitVirtualKeyboardForm.Full)
                                                                  .Add(c => c.RepeatDelay, 300)
                                                                  .Add(c => c.Value, "ab"));
        component.Render();

        Assert.AreEqual(1, Context.JSInterop.Invocations[Setup].Count);

        var options = SetupOptions();
        Assert.AreEqual("en", options.LayoutId);
        Assert.AreEqual("base", options.BaseLayer);
        Assert.AreEqual("shift", options.ShiftLayer);
        Assert.AreEqual("caps", options.CapsLayer);
        Assert.AreEqual(300, options.RepeatDelay);
        Assert.IsTrue(options.ValueMode);
        Assert.IsTrue(options.SetValue);
        Assert.AreEqual("ab", options.Value);
        Assert.AreEqual("Auto", options.EnterBehavior);
        Assert.AreEqual(component.FindAll("[data-bit-vkb-k]").Count + component.FindAll(".bit-vkb-gap,.bit-vkb-blk").Count, options.Keys.Length);
        Assert.AreEqual("Backspace", options.Keys.First(k => k.Id == "bksp").A);
        Assert.IsTrue(options.Keys.First(k => k.Id == "bksp").R);
        Assert.AreEqual(options.KeysVersion.ToString(), component.Find(".bit-vkb").GetAttribute("data-bit-vkb-v"));
    }

    [TestMethod]
    public void TheScriptShouldBeUpdatedOnlyWhenWhatItIsToldChanges()
    {
        var handle = SetupHandle();
        var component = RenderComponent<BitVirtualKeyboard>(p => p.Add(c => c.Form, BitVirtualKeyboardForm.Full));

        component.Render(p => p.Add(c => c.Layout, BitVirtualKeyboardLayouts.English));
        component.Render(p => p.Add(c => c.Layout, BitVirtualKeyboardLayouts.English));
        Assert.AreEqual(0, Updates(handle).Count, "a new instance of the same layout changes nothing");

        component.Render(p => p.Add(c => c.RepeatDelay, 200));
        var updates = Updates(handle);
        Assert.AreEqual(1, updates.Count);
        Assert.AreEqual(200, updates[0].RepeatDelay);
        Assert.IsTrue(updates[0].Version > SetupOptions().Version);
        Assert.AreEqual(SetupOptions().KeysVersion, updates[0].KeysVersion, "the keys did not change, so presses are never held back");
    }

    [TestMethod]
    public void AValueTheScriptReportsShouldBeAssignedAndNotSentBack()
    {
        var handle = SetupHandle();
        string? bound = null;
        var inputs = new List<string?>();
        var component = RenderComponent<BitVirtualKeyboard>(p => p.Add(c => c.Value, bound)
                                                                  .Add(c => c.ValueChanged, v => bound = v)
                                                                  .Add(c => c.OnInput, v => inputs.Add(v)));

        component.InvokeAsync(() => component.Instance._OnValue("abc", 3)).Wait();
        component.Render(p => p.Add(c => c.Value, bound));

        Assert.AreEqual("abc", bound);
        CollectionAssert.AreEqual(new[] { "abc" }, inputs);
        Assert.IsFalse(Updates(handle).Any(u => u.SetValue), "the value came from the script");
    }

    [TestMethod]
    public void AOneWayValueShouldBeGivenBackToTheScript()
    {
        var handle = SetupHandle();
        var component = RenderComponent<BitVirtualKeyboard>(p => p.Add(c => c.Value, "fixed"));

        component.InvokeAsync(() => component.Instance._OnValue("fixedx", 7)).Wait();
        component.Render();

        Assert.AreEqual("fixed", component.Instance.Value);
        var update = Updates(handle).Last();
        Assert.IsTrue(update.SetValue);
        Assert.AreEqual("fixed", update.Value);
        Assert.AreEqual(7, update.ValueSeq);
    }

    [TestMethod]
    public void AnOpenTheScriptReportsShouldBeAssignedOrGivenBack()
    {
        var handle = SetupHandle();
        var opens = new List<bool>();
        var bound = RenderComponent<BitVirtualKeyboard>(p => p.Add(c => c.Mode, BitVirtualKeyboardMode.Docked)
                                                             .Add(c => c.Open, false)
                                                             .Add(c => c.OpenChanged, v => opens.Add(v)));

        bound.InvokeAsync(() => bound.Instance._OnOpen(true)).Wait();
        CollectionAssert.AreEqual(new[] { true }, opens);

        var oneWay = RenderComponent<BitVirtualKeyboard>(p => p.Add(c => c.Mode, BitVirtualKeyboardMode.Docked).Add(c => c.Open, false));
        var before = Updates(handle).Count;
        oneWay.InvokeAsync(() => oneWay.Instance._OnOpen(true)).Wait();

        Assert.IsFalse(oneWay.Instance.Open);
        Assert.IsTrue(Updates(handle).Skip(before).Any(u => u.Open is false), "the script is told the Open the parent holds");
    }

    [TestMethod]
    public void AnInputAttachedAtRunTimeShouldStayAttachedThroughTheRenderItCauses()
    {
        var handle = SetupHandle();
        var component = RenderComponent<BitVirtualKeyboard>();

        component.InvokeAsync(() => component.Instance.AttachAsync(new ElementReference("input-1")).AsTask()).Wait();
        component.InvokeAsync(() => component.Instance._OnTarget("text", null, null, null, false, "input-1")).Wait();
        component.Render();

        var attaches = handle.Invocations.Where(i => i.Identifier == "attach").ToList();
        Assert.AreEqual(1, attaches.Count, "no render hands the script the unset Target");
        Assert.IsNotNull(attaches[0].Arguments[0]);
    }

    [TestMethod]
    public void AScrambledKeyboardShouldKeepItsKeysThroughARenderOfItsParent()
    {
        var handle = SetupHandle();
        var component = RenderComponent<BitVirtualKeyboard>(p => p.Add(c => c.Purpose, BitVirtualKeyboardPurpose.Pin).Add(c => c.Scramble, true));
        var version = component.Find(".bit-vkb").GetAttribute("data-bit-vkb-v");
        var digits = component.FindAll("[data-bit-vkb-k]").Select(k => k.TextContent).ToList();

        component.Render(p => p.Add(c => c.Purpose, BitVirtualKeyboardPurpose.Pin));

        Assert.AreEqual(version, component.Find(".bit-vkb").GetAttribute("data-bit-vkb-v"), "the presses are not held back");
        CollectionAssert.AreEqual(digits, component.FindAll("[data-bit-vkb-k]").Select(k => k.TextContent).ToList());

        component.InvokeAsync(() => component.Instance._OnTarget("text", null, null, "pin", false, null)).Wait();
        Assert.AreNotEqual(version, component.Find(".bit-vkb").GetAttribute("data-bit-vkb-v"), "attaching scrambles the keys again");
    }

    [TestMethod]
    public void AKeyPressHandlerShouldBeAskedAndItsAnswerReturned()
    {
        SetupHandle();
        var component = RenderComponent<BitVirtualKeyboard>(p => p.Add(c => c.OnKeyPress, args =>
        {
            if (args.Text == "q") args.Text = "Q";
            if (args.KeyId == "bksp") args.Cancel = true;
        }));

        Assert.IsTrue(SetupOptions().HasKeyPressHandler);

        var replaced = component.InvokeAsync(() => component.Instance._OnKeyPress("q", "Character", "q", "base")).Result;
        Assert.AreEqual("Q", replaced.Text);
        Assert.IsFalse(replaced.Cancel);
        Assert.AreEqual(BitVirtualKeyboardKeyAction.Character, replaced.Action);

        var cancelled = component.InvokeAsync(() => component.Instance._OnKeyPress("bksp", "Backspace", null, "base")).Result;
        Assert.IsTrue(cancelled.Cancel);
    }

    [TestMethod]
    public void TheInputTypedIntoShouldPickThePurposeAndLayout()
    {
        SetupHandle();
        var component = RenderComponent<BitVirtualKeyboard>(p => p.Add(c => c.FollowFocus, true).Add(c => c.Form, BitVirtualKeyboardForm.Compact));

        component.InvokeAsync(() => component.Instance._OnTarget("email", null, null, null, false, "mail")).Wait();
        Assert.IsTrue(component.FindAll("button").Any(b => b.TextContent.Contains("@")), "the email layout");

        component.InvokeAsync(() => component.Instance._OnTarget("text", "url", null, null, false, null)).Wait();
        Assert.IsTrue(component.FindAll("button").Any(b => b.TextContent.Contains(".com")));

        component.InvokeAsync(() => component.Instance._OnTarget(null, null, null, null, false, null)).Wait();
        Assert.IsFalse(component.FindAll("button").Any(b => b.TextContent.Contains(".com")));
    }

    [TestMethod]
    public void APasswordInputShouldPutTheKeyboardInSecureMode()
    {
        SetupHandle();
        var inputs = new List<string?>();
        var component = RenderComponent<BitVirtualKeyboard>(p => p.Add(c => c.FollowFocus, true).Add(c => c.OnInput, v => inputs.Add(v)));

        component.InvokeAsync(() => component.Instance._OnTarget("password", null, null, null, true, null)).Wait();
        component.InvokeAsync(() => component.Instance._OnTargetValue("secret")).Wait();

        Assert.AreEqual(0, inputs.Count, "no text of a password reaches .NET");
    }

    [TestMethod]
    public void TheLanguageKeyShouldMoveToTheNextLanguage()
    {
        SetupHandle();
        BitVirtualKeyboardLayout? changed = null;
        var component = RenderComponent<BitVirtualKeyboard>(p => p.Add(c => c.Languages, ["en", "en"])
                                                                  .Add(c => c.Form, BitVirtualKeyboardForm.Full)
                                                                  .Add(c => c.OnLayoutChange, l => changed = l));

        component.InvokeAsync(() => component.Instance._OnNextLayout()).Wait();

        Assert.AreEqual("en", component.Instance.ActiveLayout!.Id);
        Assert.IsNull(changed, "the same layout is no change");
    }

    [TestMethod]
    public void AutoFormShouldFollowWhatTheScriptMeasures()
    {
        SetupHandle();
        var component = RenderComponent<BitVirtualKeyboard>();

        Assert.AreEqual("en-compact", component.Instance.ActiveLayout!.Id);
        component.InvokeAsync(() => component.Instance._OnCompact(false)).Wait();
        Assert.AreEqual("en", component.Instance.ActiveLayout!.Id);
    }

    [TestMethod]
    public void TheCallbacksShouldDoNothingOnceTheKeyboardIsDisposed()
    {
        SetupHandle();
        var inputs = new List<string?>();
        var component = RenderComponent<BitVirtualKeyboard>(p => p.Add(c => c.OnInput, v => inputs.Add(v)));
        var instance = component.Instance;

        component.InvokeAsync(() => instance.DisposeAsync().AsTask()).Wait();
        component.InvokeAsync(() => instance._OnValue("x", 1)).Wait();

        Assert.AreEqual(0, inputs.Count);
    }

    [TestMethod]
    [DataRow("next", "Next")]
    [DataRow("done", "Done")]
    [DataRow("search", "Search")]
    [DataRow(null, "Enter")]
    public void TheEnterKeyShouldBeNamedByTheEnterKeyHintOfTheInput(string? hint, string name)
    {
        SetupHandle();
        var component = RenderComponent<BitVirtualKeyboard>(p => p.Add(c => c.FollowFocus, true).Add(c => c.Form, BitVirtualKeyboardForm.Compact));

        component.InvokeAsync(() => component.Instance._OnTarget("text", null, hint, null, false, null)).Wait();

        Assert.IsTrue(component.FindAll("[data-bit-vkb-layer=base] > button.bit-vkb-pri").All(b => b.GetAttribute("aria-label") == name));
    }
}