using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Dialog;

/// <summary>
/// Covers the BitParams cascade of the Dialog: that every parameter which is not about one dialog has a counterpart
/// on BitDialogParams, what a BitDialogParams fills in - the look, the size, the behavior of the showing and what the
/// dialog registers on the JS side - and what it leaves alone because the dialog wrote it for itself.
/// </summary>
[TestClass]
public class BitDialogParamsTests : BunitTestContext
{
    // What a dialog is about rather than how every dialog behaves - its content, its state, what points at elements
    // of one dialog or one page, and what it answers - is written per dialog, so it has no place on the params object
    // (see the remarks of BitDialogParams).
    private static readonly string[] _notCascaded =
    [
        nameof(BitDialog.CascadingParameters),
        nameof(BitDialog.AutoFocusSelector),
        nameof(BitDialog.Body),
        nameof(BitDialog.ChildContent),
        nameof(BitDialog.DefaultIsOpen),
        nameof(BitDialog.DragElementSelector),
        nameof(BitDialog.FooterTemplate),
        nameof(BitDialog.HeaderTemplate),
        nameof(BitDialog.IsAlert),
        nameof(BitDialog.IsCancelButtonEnabled),
        nameof(BitDialog.IsOkButtonEnabled),
        nameof(BitDialog.IsOpen),
        nameof(BitDialog.Message),
        nameof(BitDialog.OnCancel),
        nameof(BitDialog.OnClose),
        nameof(BitDialog.OnDismiss),
        nameof(BitDialog.OnDismissing),
        nameof(BitDialog.OnDismissPrevented),
        nameof(BitDialog.OnOk),
        nameof(BitDialog.OnOpen),
        nameof(BitDialog.OnOverlayClick),
        nameof(BitDialog.ScrollerElement),
        nameof(BitDialog.Subtitle),
        nameof(BitDialog.SubtitleAriaId),
        nameof(BitDialog.Title),
        nameof(BitDialog.TitleAriaId),
    ];

    private static RenderFragment RenderDialog(Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return builder =>
        {
            builder.OpenComponent<BitDialog>(0);
            builder.AddAttribute(1, nameof(BitDialog.IsOpen), true);
            builder.AddAttribute(2, nameof(BitDialog.Title), "Title");
            builder.AddAttribute(3, nameof(BitDialog.Message), "Message");
            extraAttributes?.Invoke(builder);
            builder.CloseComponent();
        };
    }

    private IRenderedComponent<BitParams> RenderWithParams(BitDialogParams @params, params RenderFragment[] dialogs)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, [@params]);
            parameters.AddChildContent(builder =>
            {
                foreach (var dialog in dialogs)
                {
                    builder.AddContent(0, dialog);
                }
            });
        });
    }

    [TestMethod]
    public void BitDialogParamsShouldHaveCorrectParamName()
    {
        var @params = new BitDialogParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual("BitParams.BitDialog", @params.Name);
        Assert.AreEqual(BitDialogParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitDialogShouldApplyCascadingParametersToEveryDialog()
    {
        var @params = new BitDialogParams
        {
            Color = BitColor.Error,
            AbsolutePosition = true,
            Modeless = true,
            FullWidth = true,
            FullHeight = true,
            Position = BitPosition.TopEnd,
            OkText = "Delete",
            CancelText = "Keep",
            CloseButtonTitle = "Dismiss",
            CloseIconName = "ChromeClose",
            Class = "cascaded",
        };

        var component = RenderWithParams(@params, RenderDialog(), RenderDialog());

        var roots = component.FindAll(".bit-dlg");

        Assert.HasCount(2, roots);

        foreach (var root in roots)
        {
            foreach (var cls in new[] { "bit-dlg-err", "bit-dlg-abs", "bit-dlg-mls", "bit-dlg-fwi", "bit-dlg-fhe", "cascaded" })
            {
                Assert.IsTrue(root.ClassList.Contains(cls), $"Missing {cls}.");
            }
        }

        foreach (var doc in component.FindAll(".bit-dlg-doc"))
        {
            Assert.IsTrue(doc.ClassList.Contains("bit-dlg-te"));
        }

        foreach (var ok in component.FindAll(".bit-dlg-okb"))
        {
            Assert.AreEqual("Delete", ok.TextContent.Trim());
        }

        foreach (var cancel in component.FindAll(".bit-dlg-cnb"))
        {
            Assert.AreEqual("Keep", cancel.TextContent.Trim());
        }

        foreach (var close in component.FindAll(".bit-dlg-cls"))
        {
            Assert.AreEqual("Dismiss", close.GetAttribute("aria-label"));
            Assert.IsTrue(close.QuerySelector("i")!.ClassList.Contains("bit-icon--ChromeClose"));
        }
    }

    [TestMethod]
    public void BitDialogDirectParametersShouldOverrideCascadingParameters()
    {
        var @params = new BitDialogParams
        {
            Color = BitColor.Error,
            OkText = "Cascaded",
            ShowCancelButton = false,
            ShowCloseButton = false,
            Position = BitPosition.TopEnd,
        };

        var component = RenderWithParams(@params, RenderDialog(builder =>
        {
            builder.AddAttribute(10, nameof(BitDialog.Color), BitColor.Success);
            builder.AddAttribute(11, nameof(BitDialog.OkText), "Own");
            builder.AddAttribute(12, nameof(BitDialog.ShowCancelButton), true);
            builder.AddAttribute(13, nameof(BitDialog.ShowCloseButton), true);
            builder.AddAttribute(14, nameof(BitDialog.Position), BitPosition.Center);
        }));

        var root = component.Find(".bit-dlg");

        Assert.IsTrue(root.ClassList.Contains("bit-dlg-suc"));
        Assert.IsFalse(root.ClassList.Contains("bit-dlg-err"));
        Assert.AreEqual("Own", component.Find(".bit-dlg-okb").TextContent.Trim());
        Assert.HasCount(1, component.FindAll(".bit-dlg-cnb"));
        Assert.HasCount(1, component.FindAll(".bit-dlg-cls"));
        Assert.IsTrue(component.Find(".bit-dlg-doc").ClassList.Contains("bit-dlg-ctr"));
    }

    [TestMethod]
    public void BitDialogCascadedDefaultTrueParametersShouldBeTurnedOffByTheCascade()
    {
        // Show*Button default to true on the dialog, so a cascade turning them off has to win over the default
        // rather than being mistaken for a value the dialog set for itself.
        var @params = new BitDialogParams
        {
            ShowOkButton = false,
            ShowCancelButton = false,
            ShowCloseButton = false,
        };

        var component = RenderWithParams(@params, RenderDialog());

        Assert.IsEmpty(component.FindAll(".bit-dlg-okb"));
        Assert.IsEmpty(component.FindAll(".bit-dlg-cnb"));
        Assert.IsEmpty(component.FindAll(".bit-dlg-cls"));
        Assert.IsEmpty(component.FindAll(".bit-dlg-bct"));
    }

    [TestMethod]
    public void BitDialogCascadedClassesAndStylesShouldReachTheParts()
    {
        var @params = new BitDialogParams
        {
            Classes = new() { Root = "cascaded-root", Container = "cascaded-container" },
            Styles = new() { Root = "color: red;", Title = "font-style: italic;" },
        };

        var component = RenderWithParams(@params, RenderDialog());

        Assert.IsTrue(component.Find(".bit-dlg").ClassList.Contains("cascaded-root"));
        Assert.IsTrue(component.Find(".bit-dlg-ctn").ClassList.Contains("cascaded-container"));
        StringAssert.Contains(component.Find(".bit-dlg").GetAttribute("style"), "color: red;");
        StringAssert.Contains(component.Find(".bit-dlg-ttl").GetAttribute("style"), "font-style: italic;");
    }

    [TestMethod]
    public void BitDialogCascadedSizeShouldReachTheSurface()
    {
        var @params = new BitDialogParams
        {
            Width = "30rem",
            MaxHeight = "20rem",
        };

        var component = RenderWithParams(@params, RenderDialog(), RenderDialog(builder =>
        {
            builder.AddAttribute(10, nameof(BitDialog.Width), "40rem");
        }));

        var containers = component.FindAll(".bit-dlg-ctn");

        StringAssert.Contains(containers[0].GetAttribute("style"), "--bit-dlg-wid:30rem;");
        StringAssert.Contains(containers[0].GetAttribute("style"), "--bit-dlg-mxh:20rem;");
        StringAssert.Contains(containers[1].GetAttribute("style"), "--bit-dlg-wid:40rem;");
        StringAssert.Contains(containers[1].GetAttribute("style"), "--bit-dlg-mxh:20rem;");
    }

    [TestMethod]
    public void BitDialogCascadedBlockingShouldRefuseTheOverlayAndPromoteTheRole()
    {
        var @params = new BitDialogParams { Blocking = true };

        var component = RenderWithParams(@params, RenderDialog());

        component.Find(".bit-dlg-ovl").Click();

        Assert.HasCount(1, component.FindAll(".bit-dlg"));
        Assert.AreEqual("alertdialog", component.Find(".bit-dlg-ctn").GetAttribute("role"));
    }

    [TestMethod]
    public void BitDialogCascadedCloseOnOverlayClickShouldRefuseTheOverlay()
    {
        var @params = new BitDialogParams { CloseOnOverlayClick = false };

        var component = RenderWithParams(@params, RenderDialog());

        component.Find(".bit-dlg-ovl").Click();

        Assert.HasCount(1, component.FindAll(".bit-dlg"));
        Assert.AreEqual("dialog", component.Find(".bit-dlg-ctn").GetAttribute("role"));
    }

    [TestMethod]
    public void BitDialogCascadedAutoFocusButtonShouldFocusThatButton()
    {
        var @params = new BitDialogParams { AutoFocusButton = BitDialogButton.Cancel };

        var component = RenderWithParams(@params, RenderDialog());

        var dialog = component.FindComponent<BitDialog>();

        Assert.AreEqual(BitDialogButton.Cancel, dialog.Instance.AutoFocusButton);
        Assert.IsEmpty(Context.JSInterop.Invocations["BitBlazorUI.Utils.focusFirstElement"]);
    }

    [TestMethod]
    public void BitDialogCascadedDisabledShouldDisableTheButtons()
    {
        var @params = new BitDialogParams { Disabled = true };

        var component = RenderWithParams(@params, RenderDialog());

        Assert.IsTrue(component.Find(".bit-dlg-okb").HasAttribute("disabled"));
        Assert.IsTrue(component.Find(".bit-dlg-cnb").HasAttribute("disabled"));
        Assert.IsTrue(component.Find(".bit-dlg-cls").HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitDialogOwnCloseIconNameShouldNotBeReplacedByACascadedCloseIcon()
    {
        // CloseIcon wins over CloseIconName, so the pair is one setting: a cascaded CloseIcon filled in beside the
        // dialog's own CloseIconName would replace the icon the dialog asked for.
        var @params = new BitDialogParams { CloseIcon = BitIconInfo.Css("cascaded-icon") };

        var component = RenderWithParams(@params, RenderDialog(builder =>
        {
            builder.AddAttribute(10, nameof(BitDialog.CloseIconName), "ChromeClose");
        }));

        var icon = component.Find(".bit-dlg-cli");

        Assert.IsTrue(icon.ClassList.Contains("bit-icon--ChromeClose"));
        Assert.IsFalse(icon.ClassList.Contains("cascaded-icon"));
    }

    [TestMethod]
    public void BitDialogOwnCloseIconShouldNotBeJoinedByACascadedCloseIconName()
    {
        var @params = new BitDialogParams { CloseIconName = "ChromeClose" };

        var component = RenderWithParams(@params, RenderDialog(builder =>
        {
            // Boxed, since BitIconInfo converts to a string and would otherwise pick that overload.
            builder.AddAttribute(10, nameof(BitDialog.CloseIcon), (object)BitIconInfo.Css("own-icon"));
        }));

        var dialog = component.FindComponent<BitDialog>();

        Assert.IsNull(dialog.Instance.CloseIconName);
        Assert.IsTrue(component.Find(".bit-dlg-cli").ClassList.Contains("own-icon"));
    }

    [TestMethod]
    public void BitDialogCascadedCloseIconShouldApplyToADialogThatSetNeither()
    {
        var @params = new BitDialogParams { CloseIcon = BitIconInfo.Css("cascaded-icon") };

        var component = RenderWithParams(@params, RenderDialog());

        Assert.IsTrue(component.Find(".bit-dlg-cli").ClassList.Contains("cascaded-icon"));
    }

    [TestMethod]
    public void BitDialogParamsShouldCarryEveryParameterThatIsNotPerDialog()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitDialog).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                          .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                          .Select(p => p.Name)
                                          .Where(n => baseParameters.Contains(n) is false
                                                   && _notCascaded.Contains(n) is false
                                                   && n.EndsWith("Changed", StringComparison.Ordinal) is false);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitDialogParams).GetProperty(name), $"BitDialogParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitDialogParamsShouldNotCascadeWhatBelongsToOneDialog()
    {
        foreach (var name in _notCascaded.Where(n => n != nameof(BitDialog.CascadingParameters)))
        {
            Assert.IsNull(typeof(BitDialogParams).GetProperty(name), $"BitDialogParams cascades {name}, which belongs to one dialog.");
        }
    }

    [TestMethod]
    public void BitDialogCascadedSizesShouldAllReachTheSurface()
    {
        var @params = new BitDialogParams
        {
            Height = "24rem",
            MinHeight = "10rem",
            MinWidth = "20rem",
            MaxWidth = "40rem",
        };

        var component = RenderWithParams(@params, RenderDialog());

        var style = component.Find(".bit-dlg-ctn").GetAttribute("style");

        StringAssert.Contains(style, "--bit-dlg-hei:24rem;");
        StringAssert.Contains(style, "--bit-dlg-mnh:10rem;");
        StringAssert.Contains(style, "--bit-dlg-mnw:20rem;");
        // A cascaded MaxWidth replaces the theme's ceiling the way one written on the dialog does.
        StringAssert.Contains(style, "--bit-dlg-mxw:40rem;");
        Assert.IsFalse(style!.Contains("--bit-Dialog-max-width"));
    }

    [TestMethod]
    public void BitDialogCascadedFocusOptionsShouldReachTheOpening()
    {
        // AutoFocus, RestoreFocus and TrapFocus default to true, so the cascade turning them off has to win over the
        // default rather than being mistaken for a value the dialog set for itself.
        var @params = new BitDialogParams
        {
            AutoFocus = false,
            RestoreFocus = false,
            TrapFocus = false,
        };

        RenderWithParams(@params, RenderDialog());

        Assert.IsEmpty(Context.JSInterop.Invocations["BitBlazorUI.Utils.focusFirstElement"]);
        Assert.IsEmpty(Context.JSInterop.Invocations["BitBlazorUI.Utils.storeFocus"]);
        Assert.IsEmpty(Context.JSInterop.Invocations["BitBlazorUI.Utils.setupFocusTrap"]);
    }

    [TestMethod]
    public void BitDialogCascadedScrollOptionsShouldHoldTheNamedScroller()
    {
        var @params = new BitDialogParams
        {
            AutoToggleScroll = true,
            ScrollerSelector = ".my-scroller",
        };

        RenderWithParams(@params, RenderDialog());

        var invocation = Context.JSInterop.Invocations["BitBlazorUI.Utils.toggleOverflow"][0];

        Assert.AreEqual(".my-scroller", invocation.Arguments[1]);
        Assert.AreEqual(true, invocation.Arguments[2]);
    }

    [TestMethod]
    public void BitDialogCascadedIsDraggableShouldRegisterTheDragHandlers()
    {
        var component = RenderWithParams(new BitDialogParams { IsDraggable = true }, RenderDialog());

        var containerId = component.Find(".bit-dlg-ctn").GetAttribute("id");
        var setup = Context.JSInterop.Invocations["BitBlazorUI.DragDrop.setup"];

        Assert.HasCount(1, setup);
        Assert.AreEqual($"[id=\"{containerId}\"] > .bit-dlg-hdr", setup[0].Arguments[2]);
    }

    [TestMethod]
    public async Task BitDialogCascadedCloseOnEscapeShouldRefuseTheKey()
    {
        BitDialogDismissReason? prevented = null;

        var component = RenderWithParams(new BitDialogParams { CloseOnEscape = false, NoDismissPreventedAnimation = true }, RenderDialog(builder =>
        {
            builder.AddAttribute(10, nameof(BitDialog.OnDismissPrevented), EventCallback.Factory.Create<BitDialogDismissReason>(this, r => prevented = r));
        }));

        var dialog = component.FindComponent<BitDialog>();

        await dialog.InvokeAsync(dialog.Instance._OnEscape);

        Assert.IsTrue(dialog.Instance.IsOpen);
        Assert.AreEqual(BitDialogDismissReason.Escape, prevented);
        // The cascaded NoDismissPreventedAnimation leaves the surface still.
        Assert.IsFalse(component.Find(".bit-dlg-ctn").ClassList.Contains("bit-dlg-prv"));
    }

    [TestMethod]
    public void BitDialogCascadedKeepMountedShouldKeepTheClosedDialogInThePage()
    {
        var isOpen = true;

        var component = RenderWithParams(new BitDialogParams { KeepMounted = true }, builder =>
        {
            builder.OpenComponent<BitDialog>(0);
            builder.AddAttribute(1, nameof(BitDialog.IsOpen), isOpen);
            builder.AddAttribute(2, nameof(BitDialog.IsOpenChanged), EventCallback.Factory.Create<bool>(this, v => isOpen = v));
            builder.AddAttribute(3, nameof(BitDialog.Title), "Title");
            builder.CloseComponent();
        });

        component.Find(".bit-dlg-cls").Click();

        component.WaitForAssertion(() =>
        {
            Assert.IsFalse(isOpen);
            Assert.IsTrue(component.Find(".bit-dlg").ClassList.Contains("bit-dlg-hdn"));
            Assert.AreEqual("Title", component.Find(".bit-dlg-ttl").TextContent);
        }, TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public void BitDialogCascadedBaseParametersShouldReachTheRoot()
    {
        var @params = new BitDialogParams
        {
            Dir = BitDir.Rtl,
            Style = "z-index: 7;",
            AriaLabel = "Cascaded",
        };

        var component = RenderWithParams(@params, RenderDialog());

        var root = component.Find(".bit-dlg");

        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        StringAssert.Contains(root.GetAttribute("style"), "z-index: 7;");
        Assert.AreEqual("Cascaded", component.Find(".bit-dlg-ctn").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitDialogShouldGoBackToItsDefaultsOnceTheCascadeStopsCarryingAValue()
    {
        var component = RenderWithParams(new BitDialogParams { Color = BitColor.Error, Modeless = true, OkText = "Delete" }, RenderDialog());

        Assert.IsTrue(component.Find(".bit-dlg").ClassList.Contains("bit-dlg-err"));
        Assert.IsTrue(component.Find(".bit-dlg").ClassList.Contains("bit-dlg-mls"));
        Assert.AreEqual("Delete", component.Find(".bit-dlg-okb").TextContent.Trim());

        component.Render(parameters => parameters.Add(p => p.Parameters, [new BitDialogParams()]));

        var dialog = component.FindComponent<BitDialog>().Instance;

        Assert.IsNull(dialog.Color);
        Assert.IsFalse(dialog.Modeless);
        Assert.AreEqual("Ok", dialog.OkText);
        Assert.IsFalse(component.Find(".bit-dlg").ClassList.Contains("bit-dlg-err"));
        Assert.IsFalse(component.Find(".bit-dlg").ClassList.Contains("bit-dlg-mls"));
        Assert.AreEqual("Ok", component.Find(".bit-dlg-okb").TextContent.Trim());
    }

    [TestMethod]
    public void BitDialogShouldNotBeAffectedByAnEmptyParamsObject()
    {
        var component = RenderWithParams(new BitDialogParams(), RenderDialog());

        var dialog = component.FindComponent<BitDialog>().Instance;

        Assert.IsNull(dialog.Color);
        Assert.IsNull(dialog.Width);
        Assert.IsNull(dialog.MaxWidth);
        Assert.IsNull(dialog.TrapFocus);
        Assert.IsNull(dialog.AutoFocusButton);
        Assert.IsNull(dialog.CloseButtonTitle);
        Assert.IsTrue(dialog.AutoFocus);
        Assert.IsTrue(dialog.CloseOnEscape);
        Assert.IsTrue(dialog.CloseOnOverlayClick);
        Assert.IsTrue(dialog.RestoreFocus);
        Assert.IsTrue(dialog.ShowOkButton);
        Assert.IsTrue(dialog.ShowCancelButton);
        Assert.IsTrue(dialog.ShowCloseButton);
        Assert.IsFalse(dialog.Blocking);
        Assert.IsFalse(dialog.Modeless);
        Assert.IsFalse(dialog.KeepMounted);
        Assert.AreEqual(BitPosition.Center, dialog.Position);
        Assert.AreEqual("Ok", dialog.OkText);
        Assert.AreEqual("Cancel", dialog.CancelText);

        Assert.AreEqual(1, component.Find(".bit-dlg").ClassList.Length);
        Assert.IsTrue(component.Find(".bit-dlg-doc").ClassList.Contains("bit-dlg-ctr"));
        Assert.AreEqual("dialog", component.Find(".bit-dlg-ctn").GetAttribute("role"));
    }
}
