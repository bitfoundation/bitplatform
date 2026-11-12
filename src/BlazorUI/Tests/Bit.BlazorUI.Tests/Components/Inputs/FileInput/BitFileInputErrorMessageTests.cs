using System.Linq;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.FileInput;

/// <summary>
/// Covers the rejection the app asserts on a file input's selection as a whole - ErrorMessage, ErrorMessageTemplate,
/// ErrorMessageAriaLabel and Invalid - as it is shown, marked on the browse button, described and announced, apart
/// from the files the file input rejects on its own.
/// </summary>
[TestClass]
public class BitFileInputErrorMessageTests : BunitTestContext
{
    // The live region of the error, told apart from the one the selection is announced out of.
    private const string ErrorRegion = ".bit-fin > [role=status].bit-fin-vhd";

    [TestMethod]
    public void BitFileInputErrorMessageShouldRenderUnderTheBrowseButtonAndMarkTheSelectionInvalid()
    {
        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.Description, "PDF only, up to 5 MB.");
            parameters.Add(p => p.ErrorMessage, "Uploads are paused.");
        });

        var button = component.Find(".bit-fin-lbl");
        var error = component.Find(".bit-fin-erm");

        Assert.AreEqual("Uploads are paused.", error.TextContent.Trim());
        Assert.IsTrue(component.Find(".bit-fin").ClassList.Contains("bit-inv"));
        Assert.AreEqual("true", button.GetAttribute("aria-invalid"));

        // Right under the button and ahead of the hint, the order the button reads the two out in.
        Assert.ContainsSingle(component.FindAll(".bit-fin-lbl + .bit-fin-erm + .bit-fin-dsc"));
        Assert.AreEqual($"{error.Id} {component.Find(".bit-fin-dsc").Id}", button.GetAttribute("aria-describedby"));

        component.Render(parameters => parameters.Add(p => p.ErrorMessage, (string?)null));

        Assert.IsFalse(component.Find(".bit-fin").ClassList.Contains("bit-inv"));
        Assert.IsNull(component.Find(".bit-fin-lbl").GetAttribute("aria-invalid"));
        Assert.AreEqual(component.Find(".bit-fin-dsc").Id, component.Find(".bit-fin-lbl").GetAttribute("aria-describedby"));
        Assert.IsEmpty(component.FindAll(".bit-fin-erm"));
    }

    [TestMethod]
    public void BitFileInputWithoutAnErrorMessageShouldRenderNoneOfIt()
    {
        var component = RenderComponent<BitFileInput>();

        var button = component.Find(".bit-fin-lbl");

        Assert.IsFalse(component.Find(".bit-fin").ClassList.Contains("bit-inv"));
        Assert.IsNull(button.GetAttribute("aria-invalid"));
        Assert.IsNull(button.GetAttribute("aria-describedby"));
        Assert.IsEmpty(component.FindAll(".bit-fin-erm"));

        // A file input that is never wired for an error message carries the selection region alone.
        Assert.IsEmpty(component.FindAll(".bit-fin-vhd"));
        Assert.ContainsSingle(component.FindAll("[role=status]"));
    }

    [TestMethod]
    public void BitFileInputInvalidShouldMarkTheSelectionWithoutRenderingAMessage()
    {
        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.Invalid, true);
        });

        var button = component.Find(".bit-fin-lbl");

        Assert.IsTrue(component.Find(".bit-fin").ClassList.Contains("bit-inv"));
        Assert.AreEqual("true", button.GetAttribute("aria-invalid"));
        Assert.IsEmpty(component.FindAll(".bit-fin-erm"));
        Assert.IsNull(button.GetAttribute("aria-describedby"));

        component.Render(parameters => parameters.Add(p => p.Invalid, false));

        Assert.IsFalse(component.Find(".bit-fin").ClassList.Contains("bit-inv"));
        Assert.IsNull(component.Find(".bit-fin-lbl").GetAttribute("aria-invalid"));
    }

    [TestMethod]
    public void BitFileInputShouldKeepShowingTheErrorMessageWhileDisabled()
    {
        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.Disabled, true);
            parameters.Add(p => p.ErrorMessage, "Uploads are paused.");
        });

        var button = component.Find(".bit-fin-lbl");

        // The line still explains the selection, while a disabled button declares no invalid state, as it already
        // does for a failed validation.
        Assert.AreEqual("Uploads are paused.", component.Find(".bit-fin-erm").TextContent.Trim());
        Assert.AreEqual(component.Find(".bit-fin-erm").Id, button.GetAttribute("aria-describedby"));
        Assert.IsNull(button.GetAttribute("aria-invalid"));
    }

    [TestMethod]
    public void BitFileInputShouldShowTheErrorMessageWithoutABrowseButton()
    {
        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.HideLabel, true);
            parameters.Add(p => p.ErrorMessage, "Uploads are paused.");
        });

        Assert.IsEmpty(component.FindAll(".bit-fin-lbl"));
        Assert.AreEqual("Uploads are paused.", component.Find(".bit-fin-erm").TextContent.Trim());
        Assert.IsTrue(component.Find(".bit-fin").ClassList.Contains("bit-inv"));
        Assert.AreEqual("Uploads are paused.", component.Find(ErrorRegion).TextContent.Trim());
    }

    [TestMethod]
    public void BitFileInputErrorMessageTemplateShouldReplaceThePlainMessage()
    {
        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.ErrorMessage, "the plain text");
            parameters.Add(p => p.ErrorMessageTemplate, "<b class=\"custom-error\">Quota reached</b>");
        });

        var error = component.Find(".bit-fin-erm");

        Assert.AreEqual("Quota reached", error.QuerySelector(".custom-error")!.TextContent);
        Assert.DoesNotContain("the plain text", error.TextContent);
        Assert.IsTrue(component.Find(".bit-fin").ClassList.Contains("bit-inv"));
        Assert.AreEqual("true", component.Find(".bit-fin-lbl").GetAttribute("aria-invalid"));

        // A template is free to render anything at all, so the region says the selection was rejected rather than
        // staying silent and leaving the field sounding accepted.
        Assert.AreEqual("Invalid input", component.Find(ErrorRegion).TextContent.Trim());
        Assert.AreEqual(error.Id, component.Find(".bit-fin-lbl").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitFileInputErrorMessageAriaLabelShouldSpeakForTheTemplate()
    {
        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.ErrorMessageAriaLabel, "سهمیهٔ بارگذاری پر شده است");
            parameters.Add(p => p.ErrorMessage, "the plain text");
            parameters.Add(p => p.ErrorMessageTemplate, "<i class=\"warning-icon\"></i>");
        });

        var label = component.Find($"#{component.Find(".bit-fin-lbl").GetAttribute("aria-describedby")}");

        Assert.AreEqual("سهمیهٔ بارگذاری پر شده است", component.Find(ErrorRegion).TextContent.Trim());
        Assert.AreEqual("سهمیهٔ بارگذاری پر شده است", label.TextContent);
        Assert.IsTrue(label.ClassList.Contains("bit-fin-vhd"));

        // Once the template goes, the plain text is on screen, and it is its own accessible text.
        component.Render(parameters => parameters.Add(p => p.ErrorMessageTemplate, (RenderFragment?)null));

        Assert.AreEqual("the plain text", component.Find(ErrorRegion).TextContent.Trim());
        Assert.AreEqual(component.Find(".bit-fin-erm").Id, component.Find(".bit-fin-lbl").GetAttribute("aria-describedby"));
        Assert.IsEmpty(component.FindAll("span.bit-fin-vhd"));
    }

    [TestMethod, DataRow(""), DataRow("  ")]
    public void BitFileInputBlankErrorMessageAriaLabelShouldFallBackToTheDefault(string label)
    {
        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.ErrorMessageAriaLabel, label);
            parameters.Add(p => p.ErrorMessageTemplate, "<i class=\"warning-icon\"></i>");
        });

        Assert.AreEqual("Invalid input", component.Find(ErrorRegion).TextContent.Trim());
        Assert.AreEqual(component.Find(".bit-fin-erm").Id, component.Find(".bit-fin-lbl").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitFileInputInvalidShouldOnlyBeAnnouncedWithAnErrorMessageAriaLabel()
    {
        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.Invalid, true);
            parameters.Add(p => p.ErrorMessageAriaLabel, (string?)null);
        });

        Assert.AreEqual(string.Empty, component.Find(ErrorRegion).TextContent.Trim());
        Assert.IsFalse(component.Find(".bit-fin-lbl").HasAttribute("aria-describedby"));

        component.Render(parameters => parameters.Add(p => p.ErrorMessageAriaLabel, "فایل‌ها پذیرفته نشدند"));

        Assert.AreEqual("فایل‌ها پذیرفته نشدند", component.Find(ErrorRegion).TextContent.Trim());
        Assert.AreEqual("فایل‌ها پذیرفته نشدند", component.Find($"#{component.Find(".bit-fin-lbl").GetAttribute("aria-describedby")}").TextContent);
    }

    [TestMethod]
    public void BitFileInputLiveRegionShouldBeRenderedEmptyUntilThereIsAMessage()
    {
        // A region that arrives with its text already in it is the one screen readers are known to miss, so a file
        // input wired for a message - even one that is null for now - carries the region from its first render.
        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.ErrorMessage, (string?)null);
        });

        var region = component.Find(ErrorRegion);

        Assert.AreEqual("polite", region.GetAttribute("aria-live"));
        Assert.AreEqual("true", region.GetAttribute("aria-atomic"));
        Assert.AreEqual(string.Empty, region.TextContent.Trim());

        component.Render(parameters => parameters.Add(p => p.ErrorMessage, "Uploads are paused."));

        Assert.AreEqual("Uploads are paused.", component.Find(ErrorRegion).TextContent.Trim());

        // Once there it stays, ready for the next message.
        component.Render(parameters => parameters.Add(p => p.ErrorMessage, (string?)null));

        Assert.AreEqual(string.Empty, component.Find(ErrorRegion).TextContent.Trim());
    }

    [TestMethod]
    public void BitFileInputShouldAnnounceTheErrorApartFromTheSelection()
    {
        Context.JSInterop.Setup<BitFileInputInfo[]>("BitBlazorUI.FileInput.setup", _ => true)
                         .SetResult([new() { Name = "a.txt", Size = 10, FileId = "1" }]);

        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.ErrorMessage, "Uploads are paused.");
        });

        component.Find(".bit-fin-fi").Change(string.Empty);

        // Each region keeps its own text, so a selection never cuts the error short, nor the error the selection.
        Assert.AreEqual("1 file selected.", component.Find(".bit-fin-lvr").TextContent.TrimEnd('​'));
        Assert.AreEqual("Uploads are paused.", component.Find(ErrorRegion).TextContent.Trim());
    }

    [TestMethod]
    public void BitFileInputShouldStyleItsErrorMessageApartFromTheMessageOfAnInvalidFile()
    {
        Context.JSInterop.Setup<BitFileInputInfo[]>("BitBlazorUI.FileInput.setup", _ => true)
                         .SetResult([new() { Name = "big.txt", Size = 100, FileId = "1" }]);

        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.MaxSize, 50);
            parameters.Add(p => p.ErrorMessage, "Uploads are paused.");
            parameters.Add(p => p.Classes, new BitFileInputClassStyles
            {
                ErrorMessageContainer = "erm-container",
                ErrorMessage = "file-message",
            });
            parameters.Add(p => p.Styles, new BitFileInputClassStyles
            {
                ErrorMessageContainer = "margin: 1px;",
                ErrorMessage = "padding: 2px;",
            });
        });

        component.Find(".bit-fin-fi").Change(string.Empty);

        var error = component.Find(".bit-fin-erm");
        var fileMessage = component.Find(".bit-fin-msg");

        Assert.IsTrue(error.ClassList.Contains("erm-container"));
        Assert.IsFalse(error.ClassList.Contains("file-message"));
        Assert.AreEqual("margin: 1px;", error.GetAttribute("style"));

        Assert.IsTrue(fileMessage.ClassList.Contains("file-message"));
        Assert.IsFalse(fileMessage.ClassList.Contains("erm-container"));
        Assert.AreEqual("padding: 2px;", fileMessage.GetAttribute("style"));
    }

    [TestMethod]
    public void BitFileInputSelectionTheEditContextRejectsShouldNotBeMarkedInvalidTwice()
    {
        var component = RenderComponent<BitFileInputValidationTest>(parameters =>
        {
            parameters.Add(p => p.ErrorMessage, "Uploads are paused.");
        });

        // The required selection is missing, so the form rejects it as well as the app.
        component.Find("form").Submit();

        var button = component.Find(".bit-fin-lbl");

        Assert.ContainsSingle(c => c == "bit-inv", component.Find(".bit-fin").GetAttribute("class")!.Split(' '));
        Assert.AreEqual("true", button.GetAttribute("aria-invalid"));
        Assert.AreEqual(component.Find(".bit-fin-erm").Id, button.GetAttribute("aria-describedby"));

        // Taking the app's message back leaves the form's rejection standing on its own.
        component.Render(parameters => parameters.Add(p => p.ErrorMessage, (string?)null));

        Assert.IsTrue(component.Find(".bit-fin").ClassList.Contains("bit-inv"));
        Assert.AreEqual("true", component.Find(".bit-fin-lbl").GetAttribute("aria-invalid"));
        Assert.IsEmpty(component.FindAll(".bit-fin-erm"));
        Assert.IsNull(component.Find(".bit-fin-lbl").GetAttribute("aria-describedby"));
    }
}
