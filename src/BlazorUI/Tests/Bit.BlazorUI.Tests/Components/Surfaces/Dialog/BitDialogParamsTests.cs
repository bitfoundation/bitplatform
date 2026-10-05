using System;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Dialog;

/// <summary>
/// Covers the BitParams cascade of the Dialog: what a BitDialogParams fills in, and what it leaves alone because
/// the dialog wrote it for itself.
/// </summary>
[TestClass]
public class BitDialogParamsTests : BunitTestContext
{
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
            Position = BitDialogPosition.TopEnd,
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
            Position = BitDialogPosition.TopEnd,
        };

        var component = RenderWithParams(@params, RenderDialog(builder =>
        {
            builder.AddAttribute(10, nameof(BitDialog.Color), BitColor.Success);
            builder.AddAttribute(11, nameof(BitDialog.OkText), "Own");
            builder.AddAttribute(12, nameof(BitDialog.ShowCancelButton), true);
            builder.AddAttribute(13, nameof(BitDialog.ShowCloseButton), true);
            builder.AddAttribute(14, nameof(BitDialog.Position), BitDialogPosition.Center);
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
}
