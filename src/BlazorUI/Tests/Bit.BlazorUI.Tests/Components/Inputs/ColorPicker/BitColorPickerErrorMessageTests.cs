using System.Linq;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.ColorPicker;

/// <summary>
/// Covers the rejection the app asserts on the color of a picker - ErrorMessage, ErrorMessageTemplate,
/// ErrorMessageAriaLabel and Invalid - as it is shown at the foot of the panel, marked and described on every control
/// that holds the color, and announced.
/// </summary>
[TestClass]
public class BitColorPickerErrorMessageTests : BunitTestContext
{
    private const string ErrorRegion = ".bit-clp > [role=status].bit-clp-vhd";
    private const string HueInput = ".bit-clp-hsd .bit-clp-inp";
    private const string AlphaInput = ".bit-clp-asd .bit-clp-inp";

    [TestInitialize]
    public void SetupJsInteropMode()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [TestMethod]
    public void BitColorPickerErrorMessageShouldRenderAtTheFootOfThePanelAndMarkEveryControlThatHoldsTheColor()
    {
        var component = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.ShowInputs, true);
            parameters.Add(p => p.ShowAlphaSlider, true);
            parameters.Add(p => p.ShowContrast, true);
            parameters.Add(p => p.Presets, new[] { "#FF0000", "#00FF00" });
            parameters.Add(p => p.ErrorMessage, "Not in the brand palette.");
        });

        var root = component.Find(".bit-clp");
        var error = component.Find(".bit-clp-erm");
        var contrast = component.Find(".bit-clp-cnr");

        Assert.AreEqual("Not in the brand palette.", error.TextContent.Trim());
        Assert.IsTrue(root.ClassList.Contains("bit-inv"));

        // The last row of the panel, under the palette too.
        Assert.AreEqual(1, component.FindAll(".bit-clp-prs + .bit-clp-erm").Count);

        // A group carries no invalid state of its own: every control that holds the color does, and each of them is
        // described by the line - the first one by the contrast verdict after it.
        Assert.IsNull(root.GetAttribute("aria-invalid"));
        Assert.AreEqual("true", component.Find(".bit-clp-rec").GetAttribute("aria-invalid"));
        Assert.AreEqual($"{error.Id} {contrast.Id}", component.Find(".bit-clp-rec").GetAttribute("aria-describedby"));

        var holders = component.FindAll(".bit-clp-inp").Concat(component.FindAll(".bit-clp-fin")).ToList();

        Assert.AreEqual(7, holders.Count);
        Assert.IsTrue(holders.All(i => i.GetAttribute("aria-invalid") == "true"));
        Assert.IsTrue(holders.All(i => i.GetAttribute("aria-describedby") == error.Id));

        // The swatches pick a color rather than hold one, and the carrier only says what the EditContext says.
        Assert.IsTrue(component.FindAll(".bit-clp-prt").All(b => b.HasAttribute("aria-invalid") is false && b.HasAttribute("aria-describedby") is false));
        Assert.IsNull(component.Find(".bit-input-hidden").GetAttribute("aria-invalid"));

        component.Render(parameters => parameters.Add(p => p.ErrorMessage, (string?)null));

        Assert.IsFalse(component.Find(".bit-clp").ClassList.Contains("bit-inv"));
        Assert.IsNull(component.Find(".bit-clp-rec").GetAttribute("aria-invalid"));
        Assert.AreEqual(contrast.Id, component.Find(".bit-clp-rec").GetAttribute("aria-describedby"));
        Assert.IsTrue(component.FindAll(".bit-clp-fin").All(i => i.HasAttribute("aria-invalid") is false && i.HasAttribute("aria-describedby") is false));
        Assert.AreEqual(0, component.FindAll(".bit-clp-erm").Count);
    }

    [TestMethod]
    public void BitColorPickerWithoutAnErrorMessageShouldRenderNoneOfIt()
    {
        var component = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.ShowInputs, true);
        });

        Assert.IsFalse(component.Find(".bit-clp").ClassList.Contains("bit-inv"));
        Assert.IsNull(component.Find(".bit-clp-rec").GetAttribute("aria-invalid"));
        Assert.IsNull(component.Find(".bit-clp-rec").GetAttribute("aria-describedby"));
        Assert.IsTrue(component.FindAll(".bit-clp-fin").All(i => i.HasAttribute("aria-describedby") is false));
        Assert.AreEqual(0, component.FindAll(".bit-clp-erm").Count);

        // A picker that is never wired for an error message carries no live region at all.
        Assert.AreEqual(0, component.FindAll(".bit-clp-vhd").Count);
        Assert.AreEqual(0, component.FindAll("[role=status]").Count);
    }

    [TestMethod]
    public void BitColorPickerInvalidShouldMarkEveryControlWithoutRenderingAMessage()
    {
        var component = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.ShowInputs, true);
            parameters.Add(p => p.ShowAlphaSlider, true);
            parameters.Add(p => p.Invalid, true);
        });

        Assert.IsTrue(component.Find(".bit-clp").ClassList.Contains("bit-inv"));
        Assert.AreEqual("true", component.Find(".bit-clp-rec").GetAttribute("aria-invalid"));
        Assert.IsTrue(component.FindAll(".bit-clp-inp").All(i => i.GetAttribute("aria-invalid") == "true"));
        Assert.IsTrue(component.FindAll(".bit-clp-fin").All(i => i.GetAttribute("aria-invalid") == "true"));
        Assert.AreEqual(0, component.FindAll(".bit-clp-erm").Count);
        Assert.IsNull(component.Find(".bit-clp-rec").GetAttribute("aria-describedby"));

        component.Render(parameters => parameters.Add(p => p.Invalid, false));

        Assert.IsFalse(component.Find(".bit-clp").ClassList.Contains("bit-inv"));
        Assert.IsNull(component.Find(".bit-clp-rec").GetAttribute("aria-invalid"));
        Assert.IsTrue(component.FindAll(".bit-clp-fin").All(i => i.HasAttribute("aria-invalid") is false));
    }

    [TestMethod]
    public void BitColorPickerShouldKeepShowingTheErrorMessageWhileDisabled()
    {
        var component = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.Disabled, true);
            parameters.Add(p => p.ShowInputs, true);
            parameters.Add(p => p.ErrorMessage, "Not in the brand palette.");
        });

        var root = component.Find(".bit-clp");
        var error = component.Find(".bit-clp-erm");

        // The line still explains the color, and the controls still point at it.
        Assert.IsTrue(root.ClassList.Contains("bit-dis"));
        Assert.IsTrue(root.ClassList.Contains("bit-inv"));
        Assert.AreEqual("Not in the brand palette.", error.TextContent.Trim());
        Assert.AreEqual(error.Id, component.Find(".bit-clp-rec").GetAttribute("aria-describedby"));
        Assert.IsTrue(component.FindAll(".bit-clp-fin").All(i => i.GetAttribute("aria-describedby") == error.Id));
    }

    // Without the gradient, the contrast verdict moves to whichever slider comes first, and the error line stays ahead
    // of it there.
    [TestMethod, DataRow(true), DataRow(false)]
    public void BitColorPickerShouldDescribeTheFirstControlWithTheErrorAheadOfTheContrast(bool showHueSlider)
    {
        var component = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.ShowSaturationArea, false);
            parameters.Add(p => p.ShowHueSlider, showHueSlider);
            parameters.Add(p => p.ShowAlphaSlider, true);
            parameters.Add(p => p.ShowContrast, true);
            parameters.Add(p => p.ErrorMessage, "Not in the brand palette.");
        });

        var error = component.Find(".bit-clp-erm");
        var contrast = component.Find(".bit-clp-cnr");

        Assert.AreEqual($"{error.Id} {contrast.Id}", component.Find(showHueSlider ? HueInput : AlphaInput).GetAttribute("aria-describedby"));

        if (showHueSlider)
        {
            Assert.AreEqual(error.Id, component.Find(AlphaInput).GetAttribute("aria-describedby"));
        }
    }

    [TestMethod]
    public void BitColorPickerErrorMessageTemplateShouldReplaceThePlainMessage()
    {
        var component = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.ShowInputs, true);
            parameters.Add(p => p.ErrorMessage, "the plain text");
            parameters.Add(p => p.ErrorMessageTemplate, "<b class=\"custom-error\">Too close to the background</b>");
        });

        var error = component.Find(".bit-clp-erm");

        Assert.AreEqual("Too close to the background", error.QuerySelector(".custom-error")!.TextContent);
        Assert.IsFalse(error.TextContent.Contains("the plain text"));
        Assert.IsTrue(component.Find(".bit-clp").ClassList.Contains("bit-inv"));
        Assert.AreEqual("true", component.Find(".bit-clp-rec").GetAttribute("aria-invalid"));

        // A template is free to render anything at all, so the region says the color was rejected rather than
        // staying silent and leaving the picker sounding accepted.
        Assert.AreEqual("Invalid input", component.Find(ErrorRegion).TextContent.Trim());
        Assert.AreEqual(error.Id, component.Find(".bit-clp-rec").GetAttribute("aria-describedby"));
        Assert.IsTrue(component.FindAll(".bit-clp-fin").All(i => i.GetAttribute("aria-describedby") == error.Id));
    }

    [TestMethod]
    public void BitColorPickerErrorMessageAriaLabelShouldSpeakForTheTemplate()
    {
        var component = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.ShowInputs, true);
            parameters.Add(p => p.ErrorMessageAriaLabel, "این رنگ در پالت برند نیست");
            parameters.Add(p => p.ErrorMessage, "the plain text");
            parameters.Add(p => p.ErrorMessageTemplate, "<i class=\"warning-icon\"></i>");
        });

        var labelId = component.Find(".bit-clp-rec").GetAttribute("aria-describedby");
        var label = component.Find($"#{labelId}");

        Assert.AreEqual("این رنگ در پالت برند نیست", component.Find(ErrorRegion).TextContent.Trim());
        Assert.AreEqual("این رنگ در پالت برند نیست", label.TextContent);
        Assert.IsTrue(label.ClassList.Contains("bit-clp-vhd"));
        Assert.IsTrue(component.FindAll(".bit-clp-fin").All(i => i.GetAttribute("aria-describedby") == labelId));

        // Once the template goes, the plain text is on screen, and it is its own accessible text.
        component.Render(parameters => parameters.Add(p => p.ErrorMessageTemplate, (RenderFragment?)null));

        Assert.AreEqual("the plain text", component.Find(ErrorRegion).TextContent.Trim());
        Assert.AreEqual(component.Find(".bit-clp-erm").Id, component.Find(".bit-clp-rec").GetAttribute("aria-describedby"));
        Assert.AreEqual(0, component.FindAll($"#{labelId}").Count);
    }

    [TestMethod, DataRow(""), DataRow("  ")]
    public void BitColorPickerBlankErrorMessageAriaLabelShouldFallBackToTheDefault(string label)
    {
        var component = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.ErrorMessageAriaLabel, label);
            parameters.Add(p => p.ErrorMessageTemplate, "<i class=\"warning-icon\"></i>");
        });

        Assert.AreEqual("Invalid input", component.Find(ErrorRegion).TextContent.Trim());
        Assert.AreEqual(component.Find(".bit-clp-erm").Id, component.Find(".bit-clp-rec").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitColorPickerInvalidShouldOnlyBeAnnouncedWithAnErrorMessageAriaLabel()
    {
        var component = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.Invalid, true);
            parameters.Add(p => p.ErrorMessageAriaLabel, (string?)null);
        });

        Assert.AreEqual(string.Empty, component.Find(ErrorRegion).TextContent.Trim());
        Assert.IsFalse(component.Find(".bit-clp-rec").HasAttribute("aria-describedby"));

        component.Render(parameters => parameters.Add(p => p.ErrorMessageAriaLabel, "رنگ پذیرفته نشد"));

        Assert.AreEqual("رنگ پذیرفته نشد", component.Find(ErrorRegion).TextContent.Trim());
        Assert.AreEqual("رنگ پذیرفته نشد", component.Find($"#{component.Find(".bit-clp-rec").GetAttribute("aria-describedby")}").TextContent);
    }

    [TestMethod]
    public void BitColorPickerLiveRegionShouldBeRenderedEmptyUntilThereIsAMessage()
    {
        // A region that arrives with its text already in it is the one screen readers are known to miss, so a picker
        // wired for a message - even one that is null for now - carries the region from its first render.
        var component = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.ErrorMessage, (string?)null);
        });

        var region = component.Find(ErrorRegion);

        Assert.AreEqual("polite", region.GetAttribute("aria-live"));
        Assert.AreEqual("true", region.GetAttribute("aria-atomic"));
        Assert.AreEqual(string.Empty, region.TextContent.Trim());

        component.Render(parameters => parameters.Add(p => p.ErrorMessage, "Not in the brand palette."));

        Assert.AreEqual("Not in the brand palette.", component.Find(ErrorRegion).TextContent.Trim());

        // Once there it stays, ready for the next message.
        component.Render(parameters => parameters.Add(p => p.ErrorMessage, (string?)null));

        Assert.AreEqual(string.Empty, component.Find(ErrorRegion).TextContent.Trim());
    }

    [TestMethod]
    public void BitColorPickerErrorMessageShouldStayWhileTheUserMovesTheColor()
    {
        var component = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.ErrorMessage, "Not in the brand palette.");
        });

        component.Find(".bit-clp-rec").KeyDown("ArrowDown");

        // The message is the app's to take back: a move of the color leaves it, and its announcement, as they were -
        // a drag would otherwise have it read out at every frame.
        Assert.AreNotEqual("#FFFFFF", component.Instance.Hex);
        Assert.AreEqual("Not in the brand palette.", component.Find(".bit-clp-erm").TextContent.Trim());
        Assert.AreEqual("Not in the brand palette.", component.Find(ErrorRegion).TextContent.Trim());
        Assert.AreEqual("true", component.Find(".bit-clp-rec").GetAttribute("aria-invalid"));
    }

    [TestMethod]
    public void BitColorPickerShouldApplyTheClassesAndStylesOfTheErrorMessage()
    {
        var component = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.ErrorMessage, "Not in the brand palette.");
            parameters.Add(p => p.Classes, new BitColorPickerClassStyles
            {
                ErrorMessageContainer = "erm-container",
                ErrorMessage = "erm-text",
            });
            parameters.Add(p => p.Styles, new BitColorPickerClassStyles
            {
                ErrorMessageContainer = "margin: 1px;",
                ErrorMessage = "padding: 2px;",
            });
        });

        var error = component.Find(".bit-clp-erm");
        var text = error.QuerySelector("span")!;

        Assert.IsTrue(error.ClassList.Contains("erm-container"));
        Assert.AreEqual("margin: 1px;", error.GetAttribute("style"));
        Assert.IsTrue(text.ClassList.Contains("erm-text"));
        Assert.AreEqual("padding: 2px;", text.GetAttribute("style"));
        Assert.AreEqual("Not in the brand palette.", text.TextContent);
    }

    [TestMethod]
    public void BitColorPickerColorTheEditContextRejectsShouldNotBeMarkedInvalidTwice()
    {
        var component = RenderComponent<BitColorPickerValidationTest>(parameters =>
        {
            parameters.Add(p => p.ErrorMessage, "Not in the brand palette.");
        });

        // The required color is missing, so the form rejects it as well as the app.
        component.Find("form").Submit();

        var error = component.Find(".bit-clp-erm");

        Assert.AreEqual(1, component.Find(".bit-clp").GetAttribute("class")!.Split(' ').Count(c => c == "bit-inv"));
        Assert.AreEqual("true", component.Find(".bit-clp-rec").GetAttribute("aria-invalid"));
        Assert.AreEqual("true", component.Find(".bit-input-hidden").GetAttribute("aria-invalid"));
        Assert.AreEqual(error.Id, component.Find(".bit-clp-rec").GetAttribute("aria-describedby"));
        Assert.IsTrue(component.FindAll(".bit-clp-fin").All(i => i.GetAttribute("aria-describedby") == error.Id));

        // Taking the app's message back leaves the form's rejection standing on its own.
        component.Render(parameters => parameters.Add(p => p.ErrorMessage, (string?)null));

        Assert.IsTrue(component.Find(".bit-clp").ClassList.Contains("bit-inv"));
        Assert.AreEqual("true", component.Find(".bit-clp-rec").GetAttribute("aria-invalid"));
        Assert.IsTrue(component.FindAll(".bit-clp-fin").All(i => i.GetAttribute("aria-invalid") == "true"));
        Assert.AreEqual(0, component.FindAll(".bit-clp-erm").Count);
        Assert.IsNull(component.Find(".bit-clp-rec").GetAttribute("aria-describedby"));
    }
}
