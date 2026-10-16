using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components;

/// <summary>
/// Pins what every component inherits from BitComponentBase, through the component that adds the least of its own on
/// top of it (BitElement) and through a probe of the test's own: the root classes and their order, the direction
/// cascaded from an ancestor and the one the component names, the visibility styles, the id, the tab index, the
/// label, the attributes a pass leaves out, the strings the builders only rebuild on a change, the direction the
/// scripts and the key handlers resolve (ExplicitRtl, IsRtlAsync, IsRtlArrowAsync), the events a disabled component
/// drops (ActivationEvents), and the disposal flags and the cancellation the disposal swallows.
/// </summary>
[TestClass]
public class BitComponentBaseTests : BunitTestContext
{
    /// <summary>
    /// A component with nothing of its own: no parameters, no generated SetParametersAsync, so every parameter it is
    /// handed goes through the base class alone, and the flags the base keeps are read back through it.
    /// </summary>
    public sealed class Probe : BitComponentBase
    {
        public bool Rendered => IsRendered;

        public bool Disposed => IsDisposed;

        public bool WasDisposingWhenDisposed { get; private set; }

        public Exception? ThrowOnDispose { get; set; }

        protected override string RootElementClass => "probe";

        protected override ValueTask DisposeAsync(bool disposing)
        {
            WasDisposingWhenDisposed = IsDisposing;

            if (ThrowOnDispose is not null) throw ThrowOnDispose;

            return base.DisposeAsync(disposing);
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddMultipleAttributes(1, HtmlAttributes);
            builder.AddAttribute(2, "id", Id ?? UniqueId);
            builder.AddAttribute(3, "class", ClassBuilder.Value);
            builder.AddAttribute(4, "style", StyleBuilder.Value);
            builder.CloseElement();
        }
    }

    private static readonly PropertyInfo ExplicitRtlProperty = typeof(BitComponentBase).GetProperty("ExplicitRtl", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly MethodInfo IsRtlAsyncMethod = typeof(BitComponentBase).GetMethod("IsRtlAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly MethodInfo IsRtlArrowAsyncMethod = typeof(BitComponentBase).GetMethod("IsRtlArrowAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static bool? ExplicitRtl(BitComponentBase component) => (bool?)ExplicitRtlProperty.GetValue(component);

    private static ValueTask<bool> IsRtlAsync(BitComponentBase component, IJSRuntime js)
        => (ValueTask<bool>)IsRtlAsyncMethod.Invoke(component, [js, null])!;

    private static ValueTask<bool> IsRtlArrowAsync(BitComponentBase component, IJSRuntime js, string? key)
        => (ValueTask<bool>)IsRtlArrowAsyncMethod.Invoke(component, [js, key, null])!;



    // ---- the root classes -----------------------------------------------------------------------------

    [TestMethod]
    public void TheRootClassesShouldBeWrittenInTheirOrderWithTheClassParameterLast()
    {
        var component = RenderComponent<BitElement>(parameters =>
        {
            parameters.Add(p => p.Disabled, true);
            parameters.Add(p => p.Dir, BitDir.Rtl);
            parameters.Add(p => p.ForceAnimation, true);
            parameters.Add(p => p.Class, "custom");
        });

        // The order is a contract of its own: a stylesheet reading `.bit-elm.bit-dis` or an app's `.custom` does
        // not care, but a test matching the whole attribute does, and so does anything parsing it.
        Assert.AreEqual("bit-elm bit-dis bit-rtl bit-fam custom", component.Find(".bit-elm").GetAttribute("class"));
    }

    [TestMethod]
    public void TheRootClassesShouldFollowTheirParametersAfterTheFirstRender()
    {
        var component = RenderComponent<Probe>();

        var root = component.Find(".probe");

        Assert.AreEqual("probe", root.GetAttribute("class"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Disabled, true);
            parameters.Add(p => p.ForceAnimation, true);
        });

        Assert.AreEqual("probe bit-dis bit-fam", component.Find(".probe").GetAttribute("class"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Disabled, false);
            parameters.Add(p => p.ForceAnimation, false);
        });

        Assert.AreEqual("probe", component.Find(".probe").GetAttribute("class"));
    }

    [TestMethod]
    public void TheClassStringShouldOnlyBeRebuiltWhenAParameterItReadsChanges()
    {
        var component = RenderComponent<Probe>(parameters => parameters.Add(p => p.Class, "a"));

        var built = component.Instance.ClassBuilder.Value;

        // A pass that hands the same values in again rebuilds nothing: the very same string is handed out.
        component.Render(parameters => parameters.Add(p => p.Class, "a"));

        Assert.AreSame(built, component.Instance.ClassBuilder.Value);

        component.Render(parameters => parameters.Add(p => p.Class, "b"));

        Assert.AreEqual("probe b", component.Instance.ClassBuilder.Value);
    }

    [TestMethod]
    public void TheStyleStringShouldOnlyBeRebuiltWhenAParameterItReadsChanges()
    {
        var component = RenderComponent<Probe>(parameters => parameters.Add(p => p.Style, "color: red"));

        var built = component.Instance.StyleBuilder.Value;

        component.Render(parameters => parameters.Add(p => p.Style, "color: red"));

        Assert.AreSame(built, component.Instance.StyleBuilder.Value);

        component.Render(parameters => parameters.Add(p => p.Visibility, BitVisibility.Hidden));

        Assert.AreEqual("color: red;visibility:hidden", component.Instance.StyleBuilder.Value);
    }



    // ---- the direction --------------------------------------------------------------------------------

    [TestMethod]
    public void AnUnsetDirShouldWriteNeitherTheAttributeNorTheClass()
    {
        var component = RenderComponent<BitElement>();

        var root = component.Find(".bit-elm");

        Assert.IsFalse(root.HasAttribute("dir"));
        Assert.IsFalse(root.ClassList.Contains("bit-rtl"));
        Assert.IsNull(component.Instance.Dir);
    }

    [TestMethod,
        DataRow(BitDir.Ltr, "ltr"),
        DataRow(BitDir.Rtl, "rtl"),
        DataRow(BitDir.Auto, "auto")
    ]
    public void DirShouldWriteTheAttributeInLowerCaseAndTheClassForRtlAlone(BitDir dir, string expected)
    {
        var component = RenderComponent<BitElement>(parameters => parameters.Add(p => p.Dir, dir));

        var root = component.Find(".bit-elm");

        Assert.AreEqual(expected, root.GetAttribute("dir"));
        Assert.AreEqual(dir is BitDir.Rtl, root.ClassList.Contains("bit-rtl"));
    }

    [TestMethod]
    public void ACascadedDirShouldReachAComponentThatNamesNone()
    {
        var component = RenderComponent<CascadingValue<BitDir?>>(parameters =>
        {
            parameters.Add(p => p.Value, (BitDir?)BitDir.Rtl);
            parameters.AddChildContent<BitElement>();
        });

        var root = component.Find(".bit-elm");

        // The Dir the component reads is the cascaded one, so the attribute and the class both follow it.
        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        Assert.AreEqual(BitDir.Rtl, component.FindComponent<BitElement>().Instance.Dir);
    }

    [TestMethod]
    public void AnOwnDirShouldWinOverTheCascadedOne()
    {
        var component = RenderComponent<CascadingValue<BitDir?>>(parameters =>
        {
            parameters.Add(p => p.Value, (BitDir?)BitDir.Rtl);
            parameters.AddChildContent<BitElement>(element => element.Add(p => p.Dir, BitDir.Ltr));
        });

        var root = component.Find(".bit-elm");

        Assert.AreEqual("ltr", root.GetAttribute("dir"));
        Assert.IsFalse(root.ClassList.Contains("bit-rtl"));
    }

    [TestMethod]
    public void AChangeOfTheCascadedDirShouldRebuildTheRootClasses()
    {
        var component = RenderComponent<CascadingValue<BitDir?>>(parameters =>
        {
            parameters.Add(p => p.Value, (BitDir?)BitDir.Rtl);
            parameters.AddChildContent<BitElement>();
        });

        Assert.IsTrue(component.Find(".bit-elm").ClassList.Contains("bit-rtl"));

        // The class string is only rebuilt on a change, and a cascaded direction changing is one.
        // CascadingValue takes every parameter afresh on each pass, so the content is handed in again with the value.
        component.Render(parameters =>
        {
            parameters.Add(p => p.Value, (BitDir?)BitDir.Ltr);
            parameters.AddChildContent<BitElement>();
        });

        var root = component.Find(".bit-elm");

        Assert.AreEqual("ltr", root.GetAttribute("dir"));
        Assert.IsFalse(root.ClassList.Contains("bit-rtl"));
    }

    [TestMethod,
        DataRow(BitDir.Rtl, true),
        DataRow(BitDir.Ltr, false),
        DataRow(BitDir.Auto, null),
        DataRow(null, null)
    ]
    public void ExplicitRtlShouldOnlyAnswerForADirectionTheComponentNames(BitDir? dir, bool? expected)
    {
        var component = RenderComponent<BitElement>(parameters => parameters.Add(p => p.Dir, dir));

        // Unset and Auto are the browser's to resolve, so neither is taken for left to right.
        Assert.AreEqual(expected, ExplicitRtl(component.Instance));
    }

    [TestMethod]
    public void ExplicitRtlShouldFollowACascadedDir()
    {
        var component = RenderComponent<CascadingValue<BitDir?>>(parameters =>
        {
            parameters.Add(p => p.Value, (BitDir?)BitDir.Rtl);
            parameters.AddChildContent<BitElement>();
        });

        Assert.AreEqual(true, ExplicitRtl(component.FindComponent<BitElement>().Instance));
    }

    [TestMethod,
        DataRow(BitDir.Rtl, true),
        DataRow(BitDir.Ltr, false)
    ]
    public async Task IsRtlAsyncShouldAnswerANamedDirectionWithoutARoundTrip(BitDir dir, bool expected)
    {
        var component = RenderComponent<BitElement>(parameters => parameters.Add(p => p.Dir, dir));

        var isRtl = await IsRtlAsync(component.Instance, Context.JSInterop.JSRuntime);

        Assert.AreEqual(expected, isRtl);
        Context.JSInterop.VerifyNotInvoke("BitBlazorUI.Utils.isRtl");
    }

    [TestMethod,
        DataRow(BitDir.Auto, true),
        DataRow(BitDir.Auto, false),
        DataRow(null, true),
        DataRow(null, false)
    ]
    public async Task IsRtlAsyncShouldAskTheBrowserAboutTheRootForADirectionItDoesNotKnow(BitDir? dir, bool answer)
    {
        Context.JSInterop.Setup<bool>("BitBlazorUI.Utils.isRtl", _ => true).SetResult(answer);

        var component = RenderComponent<BitElement>(parameters => parameters.Add(p => p.Dir, dir));

        var isRtl = await IsRtlAsync(component.Instance, Context.JSInterop.JSRuntime);

        Assert.AreEqual(answer, isRtl);

        var invocation = Context.JSInterop.VerifyInvoke("BitBlazorUI.Utils.isRtl");
        Assert.AreEqual(component.Instance.RootElement, invocation.Arguments[0]);
    }

    [TestMethod]
    public async Task IsRtlAsyncShouldAnswerLeftToRightWhenTheBrowserCannotBeAsked()
    {
        Context.JSInterop.Setup<bool>("BitBlazorUI.Utils.isRtl", _ => true).SetException(new JSException("no script"));

        var component = RenderComponent<BitElement>();

        // A failed interop call is not a direction: it falls back to the default rather than throwing into the
        // handler that asked.
        Assert.IsFalse(await IsRtlAsync(component.Instance, Context.JSInterop.JSRuntime));
    }

    [TestMethod]
    public async Task IsRtlAsyncShouldAnswerLeftToRightBeforeTheRootIsRendered()
    {
        var component = new BitElement();

        Assert.IsFalse(await IsRtlAsync(component, Context.JSInterop.JSRuntime));
        Context.JSInterop.VerifyNotInvoke("BitBlazorUI.Utils.isRtl");
    }

    [TestMethod,
        DataRow("ArrowLeft"),
        DataRow("ArrowRight")
    ]
    public async Task IsRtlArrowAsyncShouldAskForTheHorizontalArrowsAlone(string key)
    {
        Context.JSInterop.Setup<bool>("BitBlazorUI.Utils.isRtl", _ => true).SetResult(true);

        var component = RenderComponent<BitElement>();

        Assert.IsTrue(await IsRtlArrowAsync(component.Instance, Context.JSInterop.JSRuntime, key));
        Context.JSInterop.VerifyInvoke("BitBlazorUI.Utils.isRtl");
    }

    [TestMethod,
        DataRow("ArrowUp"),
        DataRow("ArrowDown"),
        DataRow("Enter"),
        DataRow("a"),
        DataRow(null)
    ]
    public async Task IsRtlArrowAsyncShouldAnswerFalseForEveryOtherKeyWithoutAsking(string? key)
    {
        Context.JSInterop.Setup<bool>("BitBlazorUI.Utils.isRtl", _ => true).SetResult(true);

        var component = RenderComponent<BitElement>();

        // Only the two keys that move along the inline axis depend on the direction, so no other key pays the
        // round trip - even in a layout that is right to left.
        Assert.IsFalse(await IsRtlArrowAsync(component.Instance, Context.JSInterop.JSRuntime, key));
        Context.JSInterop.VerifyNotInvoke("BitBlazorUI.Utils.isRtl");
    }

    [TestMethod,
        DataRow(BitDir.Rtl, true),
        DataRow(BitDir.Ltr, false)
    ]
    public async Task IsRtlArrowAsyncShouldAnswerANamedDirectionWithoutARoundTrip(BitDir dir, bool expected)
    {
        var component = RenderComponent<BitElement>(parameters => parameters.Add(p => p.Dir, dir));

        Assert.AreEqual(expected, await IsRtlArrowAsync(component.Instance, Context.JSInterop.JSRuntime, "ArrowLeft"));
        Context.JSInterop.VerifyNotInvoke("BitBlazorUI.Utils.isRtl");
    }

    [TestMethod]
    public async Task IsRtlArrowAsyncShouldTellTheBrowserTheQuestionIsAnArrowKeys()
    {
        Context.JSInterop.Setup<bool>("BitBlazorUI.Utils.isRtl", _ => true).SetResult(false);

        var component = RenderComponent<BitElement>();

        Assert.IsFalse(await IsRtlArrowAsync(component.Instance, Context.JSInterop.JSRuntime, "ArrowRight"));

        // The flag is what lets the script refuse an arrow the user has moved on from; a plain IsRtlAsync (a pointer,
        // a measurement) is never refused.
        var invocation = Context.JSInterop.VerifyInvoke("BitBlazorUI.Utils.isRtl");
        Assert.AreEqual(component.Instance.RootElement, invocation.Arguments[0]);
        Assert.AreEqual(true, invocation.Arguments[1]);
    }

    [TestMethod]
    public async Task IsRtlArrowAsyncShouldCancelAnArrowTheBrowserReportsOvertaken()
    {
        // The script throws ARROW_KEY_OVERTAKEN for an arrow whose focus a Tab or a click has taken elsewhere while the
        // round trip was out, and the handler waiting on it is cancelled rather than stealing that focus back.
        Context.JSInterop.Setup<bool>("BitBlazorUI.Utils.isRtl", _ => true)
                         .SetException(new JSException($"Error: {ArrowKeyOvertaken}\n    at Utils.isRtl (bit.blazorui.js:1:1)"));

        var component = RenderComponent<BitElement>();

        await Assert.ThrowsAsync<OperationCanceledException>(async () => await IsRtlArrowAsync(component.Instance, Context.JSInterop.JSRuntime, "ArrowLeft"));
    }

    [TestMethod]
    public async Task IsRtlAsyncShouldNeverCancel()
    {
        // Only the arrow keys are ever refused: a direction read for anything else falls back to left to right on any
        // failure of the call, the refusal included.
        Context.JSInterop.Setup<bool>("BitBlazorUI.Utils.isRtl", _ => true).SetException(new JSException(ArrowKeyOvertaken));

        var component = RenderComponent<BitElement>();

        Assert.IsFalse(await IsRtlAsync(component.Instance, Context.JSInterop.JSRuntime));
    }

    [TestMethod]
    public void AKeyHandlerWhoseArrowIsOvertakenShouldStopWithoutAnError()
    {
        // What Blazor does with the cancellation: the handler stops where it awaited the direction - no focus moved, no
        // state changed - and the event is not reported as an unhandled error.
        Context.JSInterop.Setup<bool>("BitBlazorUI.Utils.isRtl", _ => true).SetException(new JSException(ArrowKeyOvertaken));

        var component = RenderComponent<ArrowProbe>();

        component.Find("div").KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });

        Assert.AreEqual(1, component.Instance.Started);
        Assert.AreEqual(0, component.Instance.Moved);

        // The next arrow, whose direction does come back, is acted on.
        Context.JSInterop.Setup<bool>("BitBlazorUI.Utils.isRtl", _ => true).SetResult(true);

        component.Find("div").KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });

        Assert.AreEqual(2, component.Instance.Started);
        Assert.AreEqual(1, component.Instance.Moved);
        Assert.IsTrue(component.Instance.LastRtl);
    }

    [TestMethod]
    public void TheRefusalOfAnOvertakenArrowShouldBeTheSameTextInTheScriptAndInDotNet()
    {
        var utils = SourceFiles.Read("Bit.BlazorUI", "Scripts", "Utils.ts");

        StringAssert.Contains(utils, $"ARROW_KEY_OVERTAKEN = '{ArrowKeyOvertaken}'");

        // The script follows the keys and the presses that take the focus away from the start of their dispatch.
        var general = SourceFiles.Read("Bit.BlazorUI", "Scripts", "general.ts");
        StringAssert.Contains(general, "document.documentElement.addEventListener('keydown', (e: KeyboardEvent) => BitBlazorUI.Utils.trackArrowKey(e), true);");
        StringAssert.Contains(general, "document.documentElement.addEventListener('pointerdown', (e: PointerEvent) => BitBlazorUI.Utils.trackArrowKey(e), true);");
    }

    private static readonly PropertyInfo RootElementProperty = typeof(BitComponentBase).GetProperty(nameof(BitComponentBase.RootElement))!;

    private static string ArrowKeyOvertaken => (string)typeof(BitComponentBase).Assembly
                                                                               .GetType("Bit.BlazorUI.UtilsJsRuntimeExtensions")!
                                                                               .GetField("ArrowKeyOvertaken", BindingFlags.NonPublic | BindingFlags.Static)!
                                                                               .GetValue(null)!;

    /// <summary>
    /// A component with one key handler that moves along the inline axis the way every arrow-key navigation of the
    /// library does: it asks for the direction, then acts on it.
    /// </summary>
    public sealed class ArrowProbe : BitComponentBase
    {
        [Inject] private IJSRuntime _js { get; set; } = default!;

        public int Started { get; private set; }

        public int Moved { get; private set; }

        public bool LastRtl { get; private set; }

        protected override string RootElementClass => "arrow-probe";

        private async Task HandleKeyDown(KeyboardEventArgs e)
        {
            Started++;

            LastRtl = await IsRtlArrowAsync(this, _js, e.Key);

            Moved++;
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "onkeydown", EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDown));
            builder.AddElementReferenceCapture(2, element => RootElementProperty.SetValue(this, element));
            builder.CloseElement();
        }
    }



    // ---- the visibility, the id, the tab index and the label ------------------------------------------

    [TestMethod,
        DataRow(BitVisibility.Visible, null),
        DataRow(BitVisibility.Hidden, "visibility:hidden"),
        DataRow(BitVisibility.Collapsed, "display:none")
    ]
    public void VisibilityShouldWriteItsStyleAfterTheStyleParameter(BitVisibility visibility, string? expected)
    {
        var component = RenderComponent<Probe>(parameters =>
        {
            parameters.Add(p => p.Style, "color: red");
            parameters.Add(p => p.Visibility, visibility);
        });

        var style = component.Find(".probe").GetAttribute("style");

        StringAssert.StartsWith(style, "color: red");

        if (expected is null)
        {
            Assert.AreEqual("color: red", style);
        }
        else
        {
            StringAssert.EndsWith(style, expected);
        }
    }

    [TestMethod]
    public void TheIdShouldFallBackToTheUniqueIdAndFollowTheParameter()
    {
        var component = RenderComponent<Probe>();

        var uniqueId = component.Instance.UniqueId;

        Assert.IsFalse(string.IsNullOrEmpty(uniqueId));
        Assert.AreEqual(uniqueId, component.Find(".probe").Id);

        component.Render(parameters => parameters.Add(p => p.Id, "named"));

        Assert.AreEqual("named", component.Find(".probe").Id);
        // The unique id is assigned once, at construction, so it survives every render and every Id written over it.
        Assert.AreEqual(uniqueId, component.Instance.UniqueId);

        component.Render(parameters => parameters.Add(p => p.Id, (string?)null));

        Assert.AreEqual(uniqueId, component.Find(".probe").Id);
    }

    [TestMethod]
    public void TwoInstancesShouldNeverShareAUniqueId()
    {
        var first = RenderComponent<Probe>();
        var second = RenderComponent<Probe>();

        Assert.AreNotEqual(first.Instance.UniqueId, second.Instance.UniqueId);
    }

    [TestMethod]
    public void TheRootElementShouldBeCapturedOnTheFirstRender()
    {
        var component = RenderComponent<BitElement>();

        Assert.IsFalse(string.IsNullOrEmpty(component.Instance.RootElement.Id));
        Assert.AreEqual(component.Find(".bit-elm").GetAttribute("blazor:elementreference"), component.Instance.RootElement.Id);
    }

    [TestMethod]
    public void IsRenderedShouldBeSetOnceTheFirstRenderHasRun()
    {
        var instance = new Probe();

        Assert.IsFalse(instance.Rendered);

        var component = RenderComponent<Probe>();

        Assert.IsTrue(component.Instance.Rendered);
    }

    [TestMethod]
    public void TabIndexAndAriaLabelShouldReachTheRoot()
    {
        var component = RenderComponent<BitElement>(parameters =>
        {
            parameters.Add(p => p.TabIndex, "-1");
            parameters.Add(p => p.AriaLabel, "Named");
        });

        var root = component.Find(".bit-elm");

        Assert.AreEqual("-1", root.GetAttribute("tabindex"));
        Assert.AreEqual("Named", root.GetAttribute("aria-label"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.TabIndex, (string?)null);
            parameters.Add(p => p.AriaLabel, (string?)null);
        });

        root = component.Find(".bit-elm");

        Assert.IsFalse(root.HasAttribute("tabindex"));
        Assert.IsFalse(root.HasAttribute("aria-label"));
    }



    // ---- the parameter passes -------------------------------------------------------------------------

    [TestMethod]
    public async Task AnAttributeWrittenOnTheComponentShouldGoWhenTheNextPassLeavesItOut()
    {
        var component = RenderComponent<Probe>();

        await component.InvokeAsync(() => component.Instance.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            { nameof(Probe.Class), "first" },
            { "data-pass", "first" },
        })));

        var root = component.Find(".probe");

        Assert.AreEqual("probe first", root.GetAttribute("class"));
        Assert.AreEqual("first", root.GetAttribute("data-pass"));

        // The splatted attributes are the pass's own: a pass that names none of them leaves the component with none,
        // while a parameter the pass leaves out keeps the value it had.
        await component.InvokeAsync(() => component.Instance.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            { nameof(Probe.Style), "color: red" },
        })));

        root = component.Find(".probe");

        Assert.AreEqual("probe first", root.GetAttribute("class"));
        Assert.AreEqual("color: red", root.GetAttribute("style"));
        Assert.IsFalse(root.HasAttribute("data-pass"));
        Assert.HasCount(0, component.Instance.HtmlAttributes);
    }

    [TestMethod]
    public async Task EveryBaseParameterShouldBeTakenByItsNameRatherThanSplatted()
    {
        var component = RenderComponent<Probe>();

        await component.InvokeAsync(() => component.Instance.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            { nameof(Probe.AriaLabel), "label" },
            { nameof(Probe.Class), "class" },
            { nameof(Probe.Dir), BitDir.Rtl },
            { nameof(Probe.Disabled), true },
            { nameof(Probe.ForceAnimation), true },
            { nameof(Probe.Id), "id" },
            { nameof(Probe.Style), "color: red" },
            { nameof(Probe.TabIndex), "3" },
            { nameof(Probe.Visibility), BitVisibility.Hidden },
        })));

        var instance = component.Instance;

        Assert.AreEqual("label", instance.AriaLabel);
        Assert.AreEqual("class", instance.Class);
        Assert.AreEqual(BitDir.Rtl, instance.Dir);
        Assert.IsTrue(instance.Disabled);
        Assert.IsTrue(instance.ForceAnimation);
        Assert.AreEqual("id", instance.Id);
        Assert.AreEqual("color: red", instance.Style);
        Assert.AreEqual("3", instance.TabIndex);
        Assert.AreEqual(BitVisibility.Hidden, instance.Visibility);

        // None of them is an attribute: a component with no parameters of its own splats nothing after such a pass.
        Assert.HasCount(0, instance.HtmlAttributes);

        foreach (var name in new[] { nameof(Probe.AriaLabel), nameof(Probe.Class), nameof(Probe.Dir), nameof(Probe.Disabled), nameof(Probe.ForceAnimation), nameof(Probe.Id), nameof(Probe.Style), nameof(Probe.TabIndex), nameof(Probe.Visibility) })
        {
            Assert.IsFalse(instance.HasNotBeenSet(name), $"{name} was set by the pass.");
        }
    }

    [TestMethod]
    public void HasNotBeenSetShouldAnswerForTheBaseParametersOfTheCurrentPassAlone()
    {
        var component = RenderComponent<Probe>(parameters => parameters.Add(p => p.Class, "a"));

        Assert.IsFalse(component.Instance.HasNotBeenSet(nameof(Probe.Class)));
        Assert.IsTrue(component.Instance.HasNotBeenSet(nameof(Probe.Style)));

        // Every pass starts from nothing: a parameter the markup set before and leaves out now is unset again.
        component.Render(parameters => parameters.Add(p => p.Style, "color: red"));

        Assert.IsTrue(component.Instance.HasNotBeenSet(nameof(Probe.Class)));
        Assert.IsFalse(component.Instance.HasNotBeenSet(nameof(Probe.Style)));
    }



    // ---- the activation events ------------------------------------------------------------------------

    [TestMethod]
    public void ActivationEventsShouldBeTheEventsAnElementIsActivatedThroughMatchedWithoutRegardToCase()
    {
        var field = typeof(BitComponentBase).GetField("ActivationEvents", BindingFlags.NonPublic | BindingFlags.Static);

        Assert.IsNotNull(field, "BitComponentBase.ActivationEvents is gone; the disabled contract reads it.");

        var events = (FrozenSet<string>)field.GetValue(null)!;

        CollectionAssert.AreEquivalent(new[]
        {
            "onclick", "ondblclick", "onauxclick", "oncontextmenu",
            "onmousedown", "onmouseup", "onpointerdown", "onpointerup",
            "ontouchstart", "ontouchend",
            "onkeydown", "onkeyup", "onkeypress",
        }, events.ToArray());

        // The renderer matches an attribute name without regard to case, so the set has to as well.
        Assert.IsTrue(events.Contains("OnClick"));
        Assert.IsTrue(events.Contains("ONKEYDOWN"));

        // What only follows the pointer or the focus around activates nothing, and stays on a disabled component.
        Assert.IsFalse(events.Contains("onmouseover"));
        Assert.IsFalse(events.Contains("onmouseenter"));
        Assert.IsFalse(events.Contains("onfocus"));
        Assert.IsFalse(events.Contains("onblur"));
    }



    // ---- the disposal ---------------------------------------------------------------------------------

    [TestMethod]
    public async Task DisposeAsyncShouldFlagTheComponentAsDisposingBeforeAndAsDisposedAfter()
    {
        var component = RenderComponent<Probe>();

        Assert.IsFalse(component.Instance.Disposed);

        await component.Instance.DisposeAsync();

        // IsDisposing is set the moment the disposal starts, ahead of whatever the override awaits before the base
        // class sets IsDisposed, so a child or a JS callback racing the disposal can read it.
        Assert.IsTrue(component.Instance.WasDisposingWhenDisposed);
        Assert.IsTrue(component.Instance.Disposed);
    }

    [TestMethod]
    public async Task DisposeAsyncShouldSwallowACancellationOfTheDisposal()
    {
        var component = RenderComponent<Probe>();

        // A circuit on its way down cancels the interop calls still in flight; the component is going anyway.
        component.Instance.ThrowOnDispose = new TaskCanceledException();

        await component.Instance.DisposeAsync();

        Assert.IsTrue(component.Instance.WasDisposingWhenDisposed);

        // The renderer disposes the component once more when the test ends, and that one is meant to succeed.
        component.Instance.ThrowOnDispose = null;
    }

    [TestMethod]
    public async Task DisposeAsyncShouldLetAnyOtherExceptionOfTheDisposalThrough()
    {
        var component = RenderComponent<Probe>();

        component.Instance.ThrowOnDispose = new InvalidOperationException("boom");

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await component.Instance.DisposeAsync());

        component.Instance.ThrowOnDispose = null;
    }
}
