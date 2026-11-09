using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.Slider;

/// <summary>
/// Covers the rejection the app reports on a Slider by itself - ErrorMessage, ErrorMessageTemplate,
/// ErrorMessageAriaLabel and Invalid - as opposed to the one the cascading EditContext reports: the line under the
/// slider, the invalid state on its thumbs, the aria-describedby wiring and the live region.
/// </summary>
[TestClass]
public class BitSliderErrorMessageTests : BunitTestContext
{
    [TestInitialize]
    public void SetupJsInteropMode()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [TestMethod]
    public void BitSliderErrorMessageShouldRenderUnderTheSliderAndMarkItInvalid()
    {
        var component = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Label, "Budget");
            parameters.Add(p => p.ErrorMessage, "Above what your plan allows.");
        });

        var root = component.Find(".bit-sld");

        Assert.AreEqual("Above what your plan allows.", component.Find(".bit-sld-erm").TextContent.Trim());

        // a message saying what is wrong with the value is a rejection of it, so it marks the slider the same way the
        // flag does - the error look on the track and aria-invalid on the thumb
        Assert.IsTrue(root.ClassList.Contains("bit-inv"));
        Assert.AreEqual("true", component.Find(".bit-sld-inp").GetAttribute("aria-invalid"));
        Assert.AreEqual(component.Find(".bit-sld-erm").Id, component.Find(".bit-sld-inp").GetAttribute("aria-describedby"));

        // the line sits under the whole slider, outside the label, so it is no part of the thumb's name
        var parts = root.Children.Select(c => c.ClassList.Contains("bit-sld-erm") ? "error"
                                            : c.ClassList.Contains("bit-sld-cnt") ? "slider"
                                            : c.ClassList.Contains("bit-sld-lbl") ? "label"
                                            : null)
                                 .Where(p => p is not null)
                                 .ToArray();

        CollectionAssert.AreEqual(new[] { "label", "slider", "error" }, parts);
    }

    [TestMethod]
    public void BitSliderWithoutAnErrorMessageShouldRenderNoneOfIt()
    {
        var component = RenderComponent<BitSlider>(parameters => parameters.Add(p => p.Label, "Budget"));

        var input = component.Find(".bit-sld-inp");

        Assert.AreEqual(0, component.FindAll(".bit-sld-erm").Count);
        Assert.IsFalse(component.Find(".bit-sld").ClassList.Contains("bit-inv"));
        Assert.IsFalse(input.HasAttribute("aria-invalid"));
        Assert.IsFalse(input.HasAttribute("aria-describedby"));

        // a form full of plain sliders does not carry an empty live region per slider
        Assert.AreEqual(0, component.FindAll("[role=status]").Count);
    }

    [TestMethod]
    public void BitSliderInvalidShouldMarkTheSliderWithoutRenderingAMessage()
    {
        var component = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Label, "Budget");
            parameters.Add(p => p.Invalid, true);
        });

        Assert.IsTrue(component.Find(".bit-sld").ClassList.Contains("bit-inv"));
        Assert.AreEqual("true", component.Find(".bit-sld-inp").GetAttribute("aria-invalid"));

        // the flag says the value was rejected; it does not invent a line saying why
        Assert.AreEqual(0, component.FindAll(".bit-sld-erm").Count);

        component.Render(parameters => parameters.Add(p => p.Invalid, false));

        Assert.IsFalse(component.Find(".bit-sld").ClassList.Contains("bit-inv"));
        Assert.IsFalse(component.Find(".bit-sld-inp").HasAttribute("aria-invalid"));
    }

    [TestMethod]
    public void BitSliderErrorMessageShouldMarkAndDescribeBothThumbsOfARange()
    {
        var component = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Label, "Price bracket");
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.DraggableTrack, true);
            parameters.Add(p => p.LowerValue, 2);
            parameters.Add(p => p.UpperValue, 8);
            parameters.Add(p => p.ErrorMessage, "No product is priced in this bracket.");
        });

        var errorId = component.Find(".bit-sld-erm").Id;

        // a range has one value for the pair, so there is one line for it, and both ends say they are rejected
        Assert.AreEqual(1, component.FindAll(".bit-sld-erm").Count);

        foreach (var thumb in new[] { component.Find(".bit-sld-inp-lwr"), component.Find(".bit-sld-inp-upr") })
        {
            Assert.AreEqual("true", thumb.GetAttribute("aria-invalid"));
            Assert.AreEqual(errorId, thumb.GetAttribute("aria-describedby"));
        }

        // the band the range is dragged by is no control of its own to a screen reader, and stays out of it
        var band = component.Find(".bit-sld-inp-bar");

        Assert.IsFalse(band.HasAttribute("aria-invalid"));
        Assert.IsFalse(band.HasAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitSliderErrorMessageTemplateShouldReplaceThePlainMessage()
    {
        var component = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.ErrorMessage, "Plain message");
            parameters.Add(p => p.ErrorMessageTemplate, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "custom-error");
                builder.AddContent(2, "Templated message");
                builder.CloseElement();
            }));
        });

        Assert.AreEqual("Templated message", component.Find(".bit-sld-erm .custom-error").TextContent.Trim());
        Assert.IsFalse(component.Find(".bit-sld-erm").TextContent.Contains("Plain message"));

        // the plain text is not on screen, so it is not what the live region announces either
        Assert.AreEqual("Invalid input", component.Find("[role=status]").TextContent.Trim());
        Assert.AreEqual(component.Find(".bit-sld-erm").Id, component.Find(".bit-sld-inp").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitSliderErrorMessageAriaLabelShouldSpeakForTheTemplate()
    {
        var component = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.ErrorMessageAriaLabel, "بیشتر از سقف طرح شما است");
            parameters.Add(p => p.ErrorMessage, "the plain text");
            parameters.Add(p => p.ErrorMessageTemplate, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "i");
                builder.AddAttribute(1, "class", "warning-icon");
                builder.CloseElement();
            }));
        });

        var label = component.Find($"#{component.Find(".bit-sld-inp").GetAttribute("aria-describedby")}");

        Assert.AreEqual("بیشتر از سقف طرح شما است", component.Find("[role=status]").TextContent.Trim());
        Assert.AreEqual("بیشتر از سقف طرح شما است", label.TextContent);
        Assert.IsTrue(label.ClassList.Contains("bit-sld-dsc"));

        // once the template goes, the plain text is on screen, and it is its own accessible text
        component.Render(parameters => parameters.Add(p => p.ErrorMessageTemplate, (RenderFragment?)null));

        Assert.AreEqual("the plain text", component.Find("[role=status]").TextContent.Trim());
        Assert.AreEqual(component.Find(".bit-sld-erm").Id, component.Find(".bit-sld-inp").GetAttribute("aria-describedby"));
    }

    [TestMethod, DataRow(""), DataRow("  ")]
    public void BitSliderBlankErrorMessageAriaLabelShouldFallBackToTheDefault(string label)
    {
        var component = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.ErrorMessageAriaLabel, label);
            parameters.Add(p => p.ErrorMessageTemplate, (RenderFragment)(builder => builder.AddContent(0, "!")));
        });

        Assert.AreEqual("Invalid input", component.Find("[role=status]").TextContent.Trim());
        Assert.AreEqual(component.Find(".bit-sld-erm").Id, component.Find(".bit-sld-inp").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitSliderInvalidShouldOnlyBeAnnouncedWithAnErrorMessageAriaLabel()
    {
        var component = RenderComponent<BitSlider>(parameters => parameters.Add(p => p.Invalid, true));

        // the flag on its own has nothing to say, and so no live region to say it in
        Assert.AreEqual(0, component.FindAll("[role=status]").Count);
        Assert.IsFalse(component.Find(".bit-sld-inp").HasAttribute("aria-describedby"));

        component.Render(parameters => parameters.Add(p => p.ErrorMessageAriaLabel, "مقدار نامعتبر"));

        Assert.AreEqual("مقدار نامعتبر", component.Find("[role=status]").TextContent.Trim());
        Assert.AreEqual("مقدار نامعتبر", component.Find($"#{component.Find(".bit-sld-inp").GetAttribute("aria-describedby")}").TextContent);
    }

    [TestMethod]
    public void BitSliderLiveRegionShouldBeRenderedEmptyUntilThereIsAMessage()
    {
        var component = RenderComponent<BitSlider>(parameters => parameters.Add(p => p.ErrorMessage, null));

        // a live region that arrives with its text already in it is the one thing screen readers are known to miss,
        // so a slider wired for a message has the region in the markup and only its text comes and goes
        Assert.AreEqual(string.Empty, component.Find("[role=status]").TextContent.Trim());
        Assert.AreEqual("polite", component.Find("[role=status]").GetAttribute("aria-live"));

        component.Render(parameters => parameters.Add(p => p.ErrorMessage, "Rejected by the server."));

        Assert.AreEqual("Rejected by the server.", component.Find("[role=status]").TextContent.Trim());

        component.Render(parameters => parameters.Add(p => p.ErrorMessage, null));

        Assert.AreEqual(string.Empty, component.Find("[role=status]").TextContent.Trim());
        Assert.IsFalse(component.Find(".bit-sld").ClassList.Contains("bit-inv"));
    }

    [TestMethod]
    public void BitSliderShouldAnnounceTheErrorMessageAMoveBrings()
    {
        IRenderedComponent<BitSlider> component = default!;

        // the app answers a move with a message of its own, the way a bound value with a rule on it does
        component = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Value, 2);
            parameters.Add(p => p.ErrorMessage, null);
            parameters.Add(p => p.ValueChanged, (double value) => component.Render(ps =>
            {
                ps.Add(p => p.Value, value);
                ps.Add(p => p.ErrorMessage, value > 8 ? "Above what your plan allows." : null);
            }));
        });

        Assert.AreEqual(string.Empty, component.Find("[role=status]").TextContent.Trim());

        component.Find(".bit-sld-inp").Input("9");

        Assert.AreEqual(9, component.Instance.Value);
        Assert.AreEqual("Above what your plan allows.", component.Find("[role=status]").TextContent.Trim());
        Assert.AreEqual("true", component.Find(".bit-sld-inp").GetAttribute("aria-invalid"));

        component.Find(".bit-sld-inp").Input("3");

        Assert.AreEqual(3, component.Instance.Value);
        Assert.AreEqual(string.Empty, component.Find("[role=status]").TextContent.Trim());
        Assert.IsFalse(component.Find(".bit-sld-inp").HasAttribute("aria-invalid"));
    }

    [TestMethod]
    public void BitSliderErrorMessageShouldBeDescribedBeforeTheAriaDescription()
    {
        var component = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Label, "Budget");
            parameters.Add(p => p.AriaDescription, "In euros a month.");
            parameters.Add(p => p.ErrorMessage, "Above what your plan allows.");
            parameters.Add(p => p.InputHtmlAttributes, new Dictionary<string, object> { { "aria-describedby", "budget-hint" } });
        });

        var describedBy = component.Find(".bit-sld-inp").GetAttribute("aria-describedby")!.Split(' ');

        var errorId = component.Find(".bit-sld-erm").Id;
        var descriptionId = component.Find("span.bit-sld-dsc:not([role])").Id;

        // the page's own reference is kept, and what is wrong with the value is read ahead of the description
        CollectionAssert.AreEqual(new[] { "budget-hint", errorId, descriptionId }, describedBy);
    }

    [TestMethod]
    public void BitSliderErrorMessageStyleAndClassTest()
    {
        var component = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.ErrorMessage, "Rejected.");
            parameters.Add(p => p.Styles, new BitSliderClassStyles
            {
                ErrorMessage = "font-style: italic",
                ErrorMessageContainer = "margin-top: 1rem"
            });
            parameters.Add(p => p.Classes, new BitSliderClassStyles
            {
                ErrorMessage = "custom-error",
                ErrorMessageContainer = "custom-error-container"
            });
        });

        var container = component.Find(".bit-sld-erm");

        Assert.IsTrue(container.ClassList.Contains("custom-error-container"));
        Assert.AreEqual("margin-top: 1rem", container.GetAttribute("style"));

        Assert.AreEqual("font-style: italic", component.Find(".bit-sld-erm .custom-error").GetAttribute("style"));
    }

    [TestMethod]
    public async Task BitSliderAValueTheEditContextRejectsShouldNotBeMarkedInvalidTwice()
    {
        // the model asks for 4 to 7 days and starts at 0, so validating it rejects the field
        var model = new BitSliderTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderComponent<BitSlider>(parameters =>
        {
            parameters.AddCascadingValue(editContext);
            parameters.Add(p => p.Value, model.Days);
            parameters.Add(p => p.ValueChanged, (double v) => model.Days = v);
            parameters.Add(p => p.ValueExpression, () => model.Days);
            parameters.Add(p => p.Invalid, true);
            parameters.Add(p => p.ErrorMessage, "Rejected by the server.");
        });

        await component.InvokeAsync(() => editContext.Validate());

        // the base class registers the class for any rejection, the form's and the app's alike, so the two together
        // still make a single one
        Assert.AreEqual(1, component.Find(".bit-sld").ClassList.Count(c => c == "bit-inv"));
        Assert.AreEqual("true", component.Find(".bit-sld-inp").GetAttribute("aria-invalid"));

        // and once the app drops its own rejection, the form's still stands
        component.Render(parameters =>
        {
            parameters.Add(p => p.Invalid, false);
            parameters.Add(p => p.ErrorMessage, null);
        });

        Assert.IsTrue(component.Find(".bit-sld").ClassList.Contains("bit-inv"));
        Assert.AreEqual("true", component.Find(".bit-sld-inp").GetAttribute("aria-invalid"));
    }
}
