using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.MessageBox;

/// <summary>
/// Covers the BitParams cascade of the message box: what a BitMessageBoxParams fills in, what it leaves alone
/// because the message box set it for itself, and that it reaches the message boxes the service shows.
/// </summary>
[TestClass]
public class BitMessageBoxParamsTests : BunitTestContext
{
    // What belongs to a single message box rather than to a group of them: its words, its question, its severity,
    // its icon, its templates and its callbacks.
    private static readonly string[] _notCascaded =
    [
        nameof(BitMessageBox.CascadingParameters),
        nameof(BitMessageBox.Body),
        nameof(BitMessageBox.BodyTemplate),
        nameof(BitMessageBox.Buttons),
        nameof(BitMessageBox.ChildContent),
        nameof(BitMessageBox.Color),
        nameof(BitMessageBox.FooterTemplate),
        nameof(BitMessageBox.HeaderTemplate),
        nameof(BitMessageBox.Icon),
        nameof(BitMessageBox.IconAriaLabel),
        nameof(BitMessageBox.IconName),
        nameof(BitMessageBox.IconTemplate),
        nameof(BitMessageBox.OnBeforeResult),
        nameof(BitMessageBox.OnCancel),
        nameof(BitMessageBox.OnClose),
        nameof(BitMessageBox.OnNo),
        nameof(BitMessageBox.OnOk),
        nameof(BitMessageBox.OnResult),
        nameof(BitMessageBox.OnYes),
        nameof(BitMessageBox.Title),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitMessageBoxParams boxParams, System.Action<RenderTreeBuilder>? attributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { boxParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitMessageBox>(0);
                builder.AddAttribute(1, nameof(BitMessageBox.Title), "The title");
                builder.AddAttribute(2, nameof(BitMessageBox.Buttons), BitMessageBoxButtons.YesNoCancel);
                attributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitMessageBoxParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitMessageBox", BitMessageBoxParams.ParamName);
        Assert.AreEqual(BitMessageBoxParams.ParamName, new BitMessageBoxParams().Name);
        Assert.IsInstanceOfType<IBitComponentParams>(new BitMessageBoxParams());
    }

    [TestMethod]
    public void BitMessageBoxParamsShouldCarryEveryParameterThatBelongsToAGroupOfMessageBoxes()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitMessageBox).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                              .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                              .Select(p => p.Name)
                                              .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitMessageBoxParams).GetProperty(name), $"BitMessageBoxParams has no {name}.");
        }

        foreach (var name in _notCascaded.Where(n => n != nameof(BitMessageBox.CascadingParameters)))
        {
            Assert.IsNull(typeof(BitMessageBoxParams).GetProperty(name), $"BitMessageBoxParams cascades {name}, which belongs to a single message box.");
        }
    }

    [TestMethod]
    public void BitMessageBoxShouldTakeTheCascadedValues()
    {
        var component = RenderWithParams(new BitMessageBoxParams
        {
            Class = "cascaded",
            Classes = new() { Root = "cascaded-root", Title = "cascaded-title" },
            Styles = new() { Root = "margin:1px" },
            Size = BitSize.Small,
            YesText = "Ja",
            NoText = "Nein",
            CancelText = "Abbrechen",
            CloseButtonTitle = "Schließen",
            Reversed = true,
            TitleElement = "h2",
        });

        var root = component.Find(".bit-msb");

        Assert.IsTrue(root.ClassList.Contains("cascaded"));
        Assert.IsTrue(root.ClassList.Contains("cascaded-root"));
        Assert.IsTrue(root.ClassList.Contains("bit-msb-sm"));
        StringAssert.Contains(root.GetAttribute("style"), "margin:1px");

        var title = component.Find(".bit-msb-ttl");

        Assert.AreEqual("H2", title.TagName);
        Assert.IsTrue(title.ClassList.Contains("cascaded-title"));

        var texts = component.FindAll(".bit-msb-ftr .bit-btn").Select(b => b.TextContent.Trim()).ToArray();

        CollectionAssert.AreEqual(new[] { "Abbrechen", "Nein", "Ja" }, texts);
        Assert.AreEqual("Schließen", component.Find(".bit-msb-hdr .bit-btn").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitMessageBoxOwnValuesShouldWinOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitMessageBoxParams
        {
            Size = BitSize.Small,
            YesText = "Ja",
            ShowCloseButton = false,
        }, builder =>
        {
            builder.AddAttribute(3, nameof(BitMessageBox.Size), BitSize.Large);
            builder.AddAttribute(4, nameof(BitMessageBox.YesText), "Sure");
            builder.AddAttribute(5, nameof(BitMessageBox.ShowCloseButton), true);
        });

        var root = component.Find(".bit-msb");

        Assert.IsTrue(root.ClassList.Contains("bit-msb-lg"));
        Assert.IsFalse(root.ClassList.Contains("bit-msb-sm"));
        Assert.AreEqual("Sure", component.FindAll(".bit-msb-ftr .bit-btn")[0].TextContent.Trim());
        Assert.AreEqual(1, component.FindAll(".bit-msb-hdr .bit-btn").Count);
    }

    [TestMethod]
    public void BitMessageBoxShouldTakeTheCascadedButtonColorsAndHideIcon()
    {
        var component = RenderWithParams(new BitMessageBoxParams
        {
            ButtonColor = BitColor.Secondary,
            PrimaryButtonColor = BitColor.Error,
            HideIcon = true,
            ShowCloseButton = false,
        }, builder => builder.AddAttribute(3, nameof(BitMessageBox.Color), BitColor.Warning));

        var buttons = component.FindAll(".bit-msb-ftr .bit-btn");

        Assert.IsTrue(buttons[0].ClassList.Contains("bit-btn-err"), "The affirmative button does not take the cascaded PrimaryButtonColor.");
        Assert.IsTrue(buttons[1].ClassList.Contains("bit-btn-sec"), "The other buttons do not take the cascaded ButtonColor.");
        Assert.AreEqual(0, component.FindAll(".bit-msb-ico").Count, "The cascaded HideIcon is ignored.");
        Assert.AreEqual(0, component.FindAll(".bit-msb-hdr .bit-btn").Count, "The cascaded ShowCloseButton is ignored.");
    }

    [TestMethod]
    public void BitMessageBoxShouldFocusTheCascadedDefaultButton()
    {
        var component = RenderWithParams(new BitMessageBoxParams
        {
            AutoFocus = true,
            DefaultButton = BitMessageBoxResult.Cancel,
        });

        var cancel = component.FindAll(".bit-msb-ftr .bit-btn").Single(b => b.TextContent.Trim() == "Cancel");

        Assert.IsTrue(cancel.HasAttribute("autofocus"));
    }

    [TestMethod]
    public async Task BitMessageBoxServiceShouldTakeTheCascadeAroundTheModalContainer()
    {
        Services.AddSingleton<BitModalService>();
        Services.AddSingleton<BitMessageBoxService>();

        var host = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitMessageBoxParams { OkText = "D'accord", CancelText = "Annuler" } });
            parameters.AddChildContent<BitModalContainer>();
        });

        var showing = Services.GetRequiredService<BitMessageBoxService>().Show(new BitMessageBoxParameters
        {
            Title = "The title",
            Buttons = BitMessageBoxButtons.OkCancel,
            CancelText = "Non",
        });

        host.WaitForAssertion(() => Assert.AreEqual(1, host.FindAll(".bit-msb").Count));

        var texts = host.FindAll(".bit-msb-ftr .bit-btn").Select(b => b.TextContent.Trim()).ToArray();

        // The cascade fills in what the showing left unset, and the showing's own value wins over it.
        CollectionAssert.AreEqual(new[] { "D'accord", "Non" }, texts);

        host.FindAll(".bit-msb-ftr .bit-btn")[0].Click();

        Assert.AreEqual(BitMessageBoxResult.Ok, await showing);
    }
}
