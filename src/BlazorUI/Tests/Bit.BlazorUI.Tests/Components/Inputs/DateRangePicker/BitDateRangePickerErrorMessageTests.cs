using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.DateRangePicker;

/// <summary>
/// Covers the rejection the app asserts on a DateRangePicker itself - ErrorMessage, ErrorMessageTemplate,
/// ErrorMessageAriaLabel and Invalid - as it is shown, marked on the input, described and announced.
/// </summary>
[TestClass]
public class BitDateRangePickerErrorMessageTests : BunitTestContext
{
    [TestInitialize]
    public void SetupJsInterop()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [TestMethod]
    public void BitDateRangePickerErrorMessageShouldRenderUnderTheFieldAndMarkItInvalid()
    {
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.ErrorMessage, "Those nights are booked.");
        });

        var root = component.Find(".bit-dtrp");
        var input = component.Find(".bit-dtrp-inp");
        var error = component.Find(".bit-dtrp-erm");

        Assert.AreEqual("Those nights are booked.", error.TextContent.Trim());
        Assert.IsTrue(root.ClassList.Contains("bit-inv"));
        Assert.AreEqual("true", input.GetAttribute("aria-invalid"));
        Assert.AreEqual(error.Id, input.GetAttribute("aria-describedby"));

        // Under the field, inside the root, rather than between the label and the field.
        Assert.AreEqual(1, component.FindAll(".bit-dtrp > .bit-dtrp-wrp ~ .bit-dtrp-erm").Count);

        component.Render(parameters => parameters.Add(p => p.ErrorMessage, (string?)null));

        Assert.IsFalse(component.Find(".bit-dtrp").ClassList.Contains("bit-inv"));
        Assert.IsNull(component.Find(".bit-dtrp-inp").GetAttribute("aria-invalid"));
        Assert.IsNull(component.Find(".bit-dtrp-inp").GetAttribute("aria-describedby"));
        Assert.AreEqual(0, component.FindAll(".bit-dtrp-erm").Count);
    }

    [TestMethod]
    public void BitDateRangePickerWithoutAnErrorMessageShouldRenderNoneOfIt()
    {
        var component = RenderComponent<BitDateRangePicker>();

        var input = component.Find(".bit-dtrp-inp");

        Assert.IsFalse(component.Find(".bit-dtrp").ClassList.Contains("bit-inv"));
        Assert.IsNull(input.GetAttribute("aria-invalid"));
        Assert.IsNull(input.GetAttribute("aria-describedby"));
        Assert.AreEqual(0, component.FindAll(".bit-dtrp-erm").Count);

        // A picker that is never wired for an error message carries no empty live region for one.
        Assert.AreEqual(0, component.FindAll("[role=status]").Count);
    }

    [TestMethod]
    public void BitDateRangePickerInvalidShouldMarkItInvalidWithoutAMessage()
    {
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Invalid, true);
        });

        var input = component.Find(".bit-dtrp-inp");

        Assert.IsTrue(component.Find(".bit-dtrp").ClassList.Contains("bit-inv"));
        Assert.AreEqual("true", input.GetAttribute("aria-invalid"));
        Assert.AreEqual(0, component.FindAll(".bit-dtrp-erm").Count);
        Assert.IsNull(input.GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitDateRangePickerErrorMessageTemplateShouldReplaceThePlainMessage()
    {
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.ErrorMessage, "the plain text");
            parameters.Add(p => p.ErrorMessageTemplate, "<b class=\"custom-error\">Booked</b>");
        });

        var error = component.Find(".bit-dtrp-erm");

        Assert.AreEqual("Booked", error.QuerySelector(".custom-error")!.TextContent);
        Assert.IsFalse(error.TextContent.Contains("the plain text"));
        Assert.IsTrue(component.Find(".bit-dtrp").ClassList.Contains("bit-inv"));
        Assert.AreEqual("true", component.Find(".bit-dtrp-inp").GetAttribute("aria-invalid"));

        // A template is free to render anything at all, so the region says the range was rejected rather than
        // staying silent and leaving the field sounding accepted.
        Assert.AreEqual("Invalid input", component.Find(".bit-dtrp > [role=status]").TextContent.Trim());
        Assert.AreEqual(error.Id, component.Find(".bit-dtrp-inp").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitDateRangePickerErrorMessageAriaLabelShouldSpeakForTheTemplate()
    {
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.ErrorMessageAriaLabel, "این شب‌ها رزرو شده‌اند");
            parameters.Add(p => p.ErrorMessage, "the plain text");
            parameters.Add(p => p.ErrorMessageTemplate, "<i class=\"warning-icon\"></i>");
        });

        var label = component.Find($"#{component.Find(".bit-dtrp-inp").GetAttribute("aria-describedby")}");

        Assert.AreEqual("این شب‌ها رزرو شده‌اند", component.Find(".bit-dtrp > [role=status]").TextContent.Trim());
        Assert.AreEqual("این شب‌ها رزرو شده‌اند", label.TextContent);
        Assert.IsTrue(label.ClassList.Contains("bit-dtrp-sdt"));

        // Once the template goes, the plain text is on screen, and it is its own accessible text.
        component.Render(parameters => parameters.Add(p => p.ErrorMessageTemplate, (RenderFragment?)null));

        Assert.AreEqual("the plain text", component.Find(".bit-dtrp > [role=status]").TextContent.Trim());
        Assert.AreEqual(component.Find(".bit-dtrp-erm").Id, component.Find(".bit-dtrp-inp").GetAttribute("aria-describedby"));
        Assert.AreEqual(0, component.FindAll(".bit-dtrp-erm ~ span.bit-dtrp-sdt").Count);
    }

    [TestMethod, DataRow(""), DataRow("  ")]
    public void BitDateRangePickerBlankErrorMessageAriaLabelShouldFallBackToTheDefault(string label)
    {
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.ErrorMessageAriaLabel, label);
            parameters.Add(p => p.ErrorMessageTemplate, "<i class=\"warning-icon\"></i>");
        });

        Assert.AreEqual("Invalid input", component.Find(".bit-dtrp > [role=status]").TextContent.Trim());
        Assert.AreEqual(component.Find(".bit-dtrp-erm").Id, component.Find(".bit-dtrp-inp").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitDateRangePickerInvalidShouldOnlyBeAnnouncedWithAnErrorMessageAriaLabel()
    {
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Invalid, true);
            parameters.Add(p => p.ErrorMessageAriaLabel, (string?)null);
        });

        Assert.AreEqual(string.Empty, component.Find(".bit-dtrp > [role=status]").TextContent.Trim());
        Assert.IsFalse(component.Find(".bit-dtrp-inp").HasAttribute("aria-describedby"));

        component.Render(parameters => parameters.Add(p => p.ErrorMessageAriaLabel, "تاریخ‌ها نامعتبرند"));

        Assert.AreEqual("تاریخ‌ها نامعتبرند", component.Find(".bit-dtrp > [role=status]").TextContent.Trim());
        Assert.AreEqual("تاریخ‌ها نامعتبرند", component.Find($"#{component.Find(".bit-dtrp-inp").GetAttribute("aria-describedby")}").TextContent);
    }

    [TestMethod]
    public void BitDateRangePickerLiveRegionShouldBeRenderedEmptyUntilThereIsAMessage()
    {
        // A region that arrives with its text already in it is the one screen readers are known to miss, so a picker
        // wired for a message - even one that is null for now - carries the region from its first render.
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.ErrorMessage, (string?)null);
        });

        var region = component.Find(".bit-dtrp > [role=status]");

        Assert.AreEqual("polite", region.GetAttribute("aria-live"));
        Assert.AreEqual("true", region.GetAttribute("aria-atomic"));
        Assert.IsTrue(region.ClassList.Contains("bit-dtrp-sdt"));
        Assert.AreEqual(string.Empty, region.TextContent.Trim());

        component.Render(parameters => parameters.Add(p => p.ErrorMessage, "Those nights are booked."));

        Assert.AreEqual("Those nights are booked.", component.Find(".bit-dtrp > [role=status]").TextContent.Trim());

        // Once there it stays, ready for the next message.
        component.Render(parameters => parameters.Add(p => p.ErrorMessage, (string?)null));

        Assert.AreEqual(string.Empty, component.Find(".bit-dtrp > [role=status]").TextContent.Trim());
    }

    [TestMethod]
    public void BitDateRangePickerShouldDescribeTheInputWithTheErrorBeforeTheDescriptionTheAppGaveIt()
    {
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.InputHtmlAttributes, new Dictionary<string, object> { ["aria-describedby"] = "stay-hint" });
        });

        Assert.AreEqual("stay-hint", component.Find(".bit-dtrp-inp").GetAttribute("aria-describedby"));

        component.Render(parameters => parameters.Add(p => p.ErrorMessage, "Those nights are booked."));

        Assert.AreEqual($"{component.Find(".bit-dtrp-erm").Id} stay-hint", component.Find(".bit-dtrp-inp").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitDateRangePickerStandaloneShouldDescribeTheCalendarAndRenderTheMessageBelowIt()
    {
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Standalone, true);
            parameters.Add(p => p.Dir, BitDir.Rtl);
            parameters.Add(p => p.Style, "--bit-DateRangePicker-invalid-color: purple");
            parameters.Add(p => p.ErrorMessage, "Those nights are booked.");
        });

        var group = component.Find(".bit-dtrp-cac");
        var error = component.Find(".bit-dtrp-erm");

        Assert.AreEqual("group", group.GetAttribute("role"));
        Assert.AreEqual(error.Id, group.GetAttribute("aria-describedby"));

        // The message follows the calendar instead of sitting between its label and it, so it carries the direction
        // and the public variables of the root itself.
        Assert.AreEqual(0, component.FindAll(".bit-dtrp .bit-dtrp-erm").Count);
        Assert.AreEqual(1, component.FindAll(".bit-dtrp-cal ~ .bit-dtrp-erm").Count);
        Assert.AreEqual("rtl", error.GetAttribute("dir"));
        StringAssert.Contains(error.GetAttribute("style"), "--bit-DateRangePicker-invalid-color");

        // The announcement still comes out of the root.
        Assert.AreEqual("Those nights are booked.", component.Find(".bit-dtrp > [role=status]").TextContent.Trim());
    }

    [TestMethod]
    public void BitDateRangePickerStandaloneShouldNotDescribeTheCalendarWithoutAnError()
    {
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Standalone, true);
        });

        Assert.IsNull(component.Find(".bit-dtrp-cac").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitDateRangePickerShouldApplyTheStylesAndClassesOfTheErrorMessage()
    {
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.ErrorMessage, "Those nights are booked.");
            parameters.Add(p => p.Classes, new BitDateRangePickerClassStyles
            {
                ErrorMessageContainer = "erm-container",
                ErrorMessage = "erm-text",
            });
            parameters.Add(p => p.Styles, new BitDateRangePickerClassStyles
            {
                ErrorMessageContainer = "margin: 1px;",
                ErrorMessage = "padding: 2px;",
            });
        });

        var container = component.Find(".bit-dtrp-erm");
        var text = container.QuerySelector("span")!;

        Assert.IsTrue(container.ClassList.Contains("erm-container"));
        Assert.AreEqual("margin: 1px;", container.GetAttribute("style"));
        Assert.IsTrue(text.ClassList.Contains("erm-text"));
        Assert.AreEqual("padding: 2px;", text.GetAttribute("style"));
    }

    [TestMethod]
    public void BitDateRangePickerShouldKeepShowingTheErrorMessageWhileDisabled()
    {
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Disabled, true);
            parameters.Add(p => p.ErrorMessage, "Those nights are booked.");
        });

        Assert.AreEqual("Those nights are booked.", component.Find(".bit-dtrp-erm").TextContent.Trim());
        Assert.IsTrue(component.Find(".bit-dtrp").ClassList.Contains("bit-inv"));
    }

    [TestMethod]
    public void BitDateRangePickerRangeTheEditContextRejectsShouldNotBeMarkedInvalidTwice()
    {
        var component = RenderComponent<BitDateRangePickerValidationTest>(parameters =>
        {
            parameters.Add(p => p.ErrorMessage, "Those nights are booked.");
        });

        // The required range is missing, so the form rejects it as well as the app.
        component.Find("form").Submit();

        var input = component.Find(".bit-dtrp-inp");

        Assert.AreEqual(1, component.Find(".bit-dtrp").GetAttribute("class")!.Split(' ').Count(c => c == "bit-inv"));
        Assert.AreEqual("true", input.GetAttribute("aria-invalid"));
        Assert.AreEqual(component.Find(".bit-dtrp-erm").Id, input.GetAttribute("aria-describedby"));

        // Taking the app's message back leaves the form's rejection standing on its own.
        component.Render(parameters => parameters.Add(p => p.ErrorMessage, (string?)null));

        Assert.IsTrue(component.Find(".bit-dtrp").ClassList.Contains("bit-inv"));
        Assert.AreEqual("true", component.Find(".bit-dtrp-inp").GetAttribute("aria-invalid"));
        Assert.AreEqual(0, component.FindAll(".bit-dtrp-erm").Count);
        Assert.IsNull(component.Find(".bit-dtrp-inp").GetAttribute("aria-describedby"));
    }
}
