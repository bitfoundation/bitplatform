using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.Rating;

/// <summary>
/// Covers the rejection the app reports on a Rating by itself - ErrorMessage, ErrorMessageTemplate,
/// ErrorMessageAriaLabel and Invalid - as opposed to the one the cascading EditContext reports: the line under the
/// items, the invalid state on the radiogroup, the aria-describedby wiring and the live region.
/// </summary>
[TestClass]
public class BitRatingErrorMessageTests : BunitTestContext
{
    [TestMethod]
    public void BitRatingErrorMessageShouldRenderUnderTheItemsAndMarkTheRatingInvalid()
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.Label, "Quality");
            parameters.Add(p => p.Description, "Half stars are allowed.");
            parameters.Add(p => p.ErrorMessage, "Five stars need a written review.");
        });

        var root = component.Find(".bit-rtg");

        Assert.AreEqual("Five stars need a written review.", component.Find(".bit-rtg-erm").TextContent.Trim());

        // a message saying what is wrong with the value is a rejection of it, so it marks the rating the same way the
        // flag does - the error look on the items and aria-invalid on the radiogroup
        Assert.IsTrue(root.ClassList.Contains("bit-inv"));
        Assert.AreEqual("true", root.GetAttribute("aria-invalid"));

        // the line sits under the items and ahead of the description, outside every item, so it is no part of the
        // name of any of them
        var parts = root.Children.Select(c => c.ClassList.Contains("bit-rtg-erm") ? "error"
                                            : c.ClassList.Contains("bit-rtg-cnt") ? "items"
                                            : c.ClassList.Contains("bit-rtg-dsc") ? "description"
                                            : null)
                                 .Where(p => p is not null)
                                 .ToArray();

        Assert.AreSequenceEqual(new[] { "items", "error", "description" }, parts);
        Assert.IsEmpty(component.FindAll(".bit-rtg-btn .bit-rtg-erm"));
    }

    [TestMethod]
    public void BitRatingWithoutAnErrorMessageShouldRenderNoneOfIt()
    {
        var component = RenderComponent<BitRating>(parameters => parameters.Add(p => p.Label, "Quality"));

        var root = component.Find(".bit-rtg");

        Assert.IsEmpty(component.FindAll(".bit-rtg-erm"));
        Assert.IsFalse(root.ClassList.Contains("bit-inv"));
        Assert.IsFalse(root.HasAttribute("aria-invalid"));
        Assert.IsFalse(root.HasAttribute("aria-describedby"));

        // a form full of plain ratings does not carry an empty error region per rating
        Assert.IsEmpty(component.FindAll("[role=status]"));
    }

    [TestMethod]
    public void BitRatingInvalidShouldMarkTheRatingWithoutRenderingAMessage()
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.Label, "Quality");
            parameters.Add(p => p.Invalid, true);
        });

        Assert.IsTrue(component.Find(".bit-rtg").ClassList.Contains("bit-inv"));
        Assert.AreEqual("true", component.Find(".bit-rtg").GetAttribute("aria-invalid"));

        // the flag says the value was rejected; it does not invent a line saying why
        Assert.IsEmpty(component.FindAll(".bit-rtg-erm"));

        component.Render(parameters => parameters.Add(p => p.Invalid, false));

        Assert.IsFalse(component.Find(".bit-rtg").ClassList.Contains("bit-inv"));
        Assert.IsFalse(component.Find(".bit-rtg").HasAttribute("aria-invalid"));
    }

    [TestMethod]
    public void BitRatingReadOnlyShouldShowTheErrorWithoutAnInvalidStateItsRoleCannotCarry()
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.Label, "Quality");
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.Value, 4);
            parameters.Add(p => p.ErrorMessage, "This score was flagged for review.");
        });

        var root = component.Find(".bit-rtg");

        // a read-only rating is an image of its value, which supports no aria-invalid - but the look and the line
        // still show, and the image is still described by the line
        Assert.AreEqual("img", root.GetAttribute("role"));
        Assert.IsFalse(root.HasAttribute("aria-invalid"));
        Assert.IsTrue(root.ClassList.Contains("bit-inv"));
        Assert.AreEqual(component.Find(".bit-rtg-erm").Id, root.GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitRatingErrorMessageTemplateShouldReplaceThePlainMessage()
    {
        var component = RenderComponent<BitRating>(parameters =>
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

        Assert.AreEqual("Templated message", component.Find(".bit-rtg-erm .custom-error").TextContent.Trim());
        Assert.DoesNotContain("Plain message", component.Find(".bit-rtg-erm").TextContent);

        // the plain text is not on screen, so it is not what the live region announces either
        Assert.AreEqual("Invalid input", component.Find("[role=status]").TextContent.Trim());
        Assert.AreEqual(component.Find(".bit-rtg-erm").Id, component.Find(".bit-rtg").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitRatingErrorMessageAriaLabelShouldSpeakForTheTemplate()
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.ErrorMessageAriaLabel, "پنج ستاره نیاز به نوشتن نظر دارد");
            parameters.Add(p => p.ErrorMessage, "the plain text");
            parameters.Add(p => p.ErrorMessageTemplate, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "i");
                builder.AddAttribute(1, "class", "warning-icon");
                builder.CloseElement();
            }));
        });

        var label = component.Find($"#{component.Find(".bit-rtg").GetAttribute("aria-describedby")}");

        Assert.AreEqual("پنج ستاره نیاز به نوشتن نظر دارد", component.Find("[role=status]").TextContent.Trim());
        Assert.AreEqual("پنج ستاره نیاز به نوشتن نظر دارد", label.TextContent);
        Assert.IsTrue(label.ClassList.Contains("bit-rtg-alb"));

        // once the template goes, the plain text is on screen, and it is its own accessible text
        component.Render(parameters => parameters.Add(p => p.ErrorMessageTemplate, (RenderFragment?)null));

        Assert.AreEqual("the plain text", component.Find("[role=status]").TextContent.Trim());
        Assert.AreEqual(component.Find(".bit-rtg-erm").Id, component.Find(".bit-rtg").GetAttribute("aria-describedby"));
    }

    [TestMethod, DataRow(""), DataRow("  ")]
    public void BitRatingBlankErrorMessageAriaLabelShouldFallBackToTheDefault(string label)
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.ErrorMessageAriaLabel, label);
            parameters.Add(p => p.ErrorMessageTemplate, (RenderFragment)(builder => builder.AddContent(0, "!")));
        });

        Assert.AreEqual("Invalid input", component.Find("[role=status]").TextContent.Trim());
        Assert.AreEqual(component.Find(".bit-rtg-erm").Id, component.Find(".bit-rtg").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitRatingInvalidShouldOnlyBeAnnouncedWithAnErrorMessageAriaLabel()
    {
        var component = RenderComponent<BitRating>(parameters => parameters.Add(p => p.Invalid, true));

        // the flag on its own has nothing to say, and so no live region to say it in
        Assert.IsEmpty(component.FindAll("[role=status]"));
        Assert.IsFalse(component.Find(".bit-rtg").HasAttribute("aria-describedby"));

        component.Render(parameters => parameters.Add(p => p.ErrorMessageAriaLabel, "امتیاز نامعتبر"));

        Assert.AreEqual("امتیاز نامعتبر", component.Find("[role=status]").TextContent.Trim());
        Assert.AreEqual("امتیاز نامعتبر", component.Find($"#{component.Find(".bit-rtg").GetAttribute("aria-describedby")}").TextContent);
    }

    [TestMethod]
    public void BitRatingLiveRegionShouldBeRenderedEmptyUntilThereIsAMessage()
    {
        var component = RenderComponent<BitRating>(parameters => parameters.Add(p => p.ErrorMessage, null));

        // a live region that arrives with its text already in it is the one thing screen readers are known to miss,
        // so a rating wired for a message has the region in the markup and only its text comes and goes
        Assert.AreEqual(string.Empty, component.Find("[role=status]").TextContent.Trim());
        Assert.AreEqual("polite", component.Find("[role=status]").GetAttribute("aria-live"));

        component.Render(parameters => parameters.Add(p => p.ErrorMessage, "Rejected by the server."));

        Assert.AreEqual("Rejected by the server.", component.Find("[role=status]").TextContent.Trim());

        component.Render(parameters => parameters.Add(p => p.ErrorMessage, null));

        Assert.AreEqual(string.Empty, component.Find("[role=status]").TextContent.Trim());
        Assert.IsFalse(component.Find(".bit-rtg").ClassList.Contains("bit-inv"));
    }

    [TestMethod]
    public void BitRatingShouldAnnounceTheErrorMessageAPickBringsApartFromTheValue()
    {
        IRenderedComponent<BitRating> component = default!;

        // the app answers a pick with a message of its own, the way a bound value with a rule on it does
        component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.Value, 3);
            parameters.Add(p => p.ErrorMessage, null);
            parameters.Add(p => p.ValueChanged, (double value) => component.Render(ps =>
            {
                ps.Add(p => p.Value, value);
                ps.Add(p => p.ErrorMessage, value == 5 ? "Five stars need a written review." : null);
            }));
        });

        Assert.AreEqual(string.Empty, component.Find("[role=status]").TextContent.Trim());

        component.FindAll(".bit-rtg-btn")[4].Click();

        Assert.AreEqual(5, component.Instance.Value);
        Assert.AreEqual("Five stars need a written review.", component.Find("[role=status]").TextContent.Trim());
        Assert.AreEqual("true", component.Find(".bit-rtg").GetAttribute("aria-invalid"));

        // the value keeps the region of its own, which the error does not take over
        Assert.HasCount(2, component.FindAll("[aria-live]"));
        Assert.DoesNotContain("review", component.Find("[aria-live]:not([role])").TextContent);

        component.FindAll(".bit-rtg-btn")[1].Click();

        Assert.AreEqual(2, component.Instance.Value);
        Assert.AreEqual(string.Empty, component.Find("[role=status]").TextContent.Trim());
        Assert.IsFalse(component.Find(".bit-rtg").HasAttribute("aria-invalid"));
    }

    [TestMethod]
    public void BitRatingErrorMessageShouldBeDescribedBeforeTheDescription()
    {
        var component = Context.Render(builder =>
        {
            builder.OpenComponent<BitRating>(0);
            builder.AddAttribute(1, nameof(BitRating.Label), "Quality");
            builder.AddAttribute(2, nameof(BitRating.Description), "Half stars are allowed.");
            builder.AddAttribute(3, nameof(BitRating.ErrorMessage), "Five stars need a written review.");
            builder.AddAttribute(4, "aria-describedby", "quality-hint");
            builder.CloseComponent();
        });

        var describedBy = component.Find(".bit-rtg").GetAttribute("aria-describedby")!.Split(' ');

        var errorId = component.Find(".bit-rtg-erm").Id;
        var descriptionId = component.Find(".bit-rtg-dsc").Id;

        // the page's own reference is kept, and the two lines are read in the order they sit under the items
        Assert.AreSequenceEqual(new[] { "quality-hint", errorId, descriptionId }, describedBy);
    }

    [TestMethod]
    public void BitRatingErrorMessageStyleAndClassTest()
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.ErrorMessage, "Rejected.");
            parameters.Add(p => p.Styles, new BitRatingClassStyles
            {
                ErrorMessage = "font-style: italic",
                ErrorMessageContainer = "margin-top: 1rem"
            });
            parameters.Add(p => p.Classes, new BitRatingClassStyles
            {
                ErrorMessage = "custom-error",
                ErrorMessageContainer = "custom-error-container"
            });
        });

        var container = component.Find(".bit-rtg-erm");

        Assert.IsTrue(container.ClassList.Contains("custom-error-container"));
        Assert.AreEqual("margin-top: 1rem", container.GetAttribute("style"));

        Assert.AreEqual("font-style: italic", component.Find(".bit-rtg-erm .custom-error").GetAttribute("style"));
    }

    [TestMethod]
    public async Task BitRatingAValueTheEditContextRejectsShouldNotBeMarkedInvalidTwice()
    {
        // the model asks for 1 to 5 and starts at 0, so validating it rejects the field
        var model = new BitRatingTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.AddCascadingValue(editContext);
            parameters.Add(p => p.AllowZeroStars, true);
            parameters.Add(p => p.Value, model.Value);
            parameters.Add(p => p.ValueChanged, (double v) => model.Value = v);
            parameters.Add(p => p.ValueExpression, () => model.Value);
            parameters.Add(p => p.Invalid, true);
            parameters.Add(p => p.ErrorMessage, "Rejected by the server.");
        });

        await component.InvokeAsync(() => editContext.Validate());

        // the base class registers the class for any rejection, the form's and the app's alike, so the two together
        // still make a single one
        Assert.ContainsSingle(c => c == "bit-inv", component.Find(".bit-rtg").ClassList);
        Assert.AreEqual("true", component.Find(".bit-rtg").GetAttribute("aria-invalid"));

        // and once the app drops its own rejection, the form's still stands
        component.Render(parameters =>
        {
            parameters.Add(p => p.Invalid, false);
            parameters.Add(p => p.ErrorMessage, null);
        });

        Assert.IsTrue(component.Find(".bit-rtg").ClassList.Contains("bit-inv"));
        Assert.AreEqual("true", component.Find(".bit-rtg").GetAttribute("aria-invalid"));
    }

    [TestMethod, DataRow(false), DataRow(true)]
    public void BitRatingShouldKeepAnAriaInvalidThePageWroteWhileTheAppRejectsNothing(bool invalid)
    {
        var component = Context.Render(builder =>
        {
            builder.OpenComponent<BitRating>(0);
            builder.AddAttribute(1, nameof(BitRating.Invalid), invalid);
            builder.AddAttribute(2, "aria-invalid", "spelling");
            builder.CloseComponent();
        });

        // what the page wrote stands while the app rejects nothing, and a rejection by the app says "true"
        Assert.AreEqual(invalid ? "true" : "spelling", component.Find(".bit-rtg").GetAttribute("aria-invalid"));
    }
}
