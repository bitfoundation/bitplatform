using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.Rating;

/// <summary>
/// Covers ShowValidationMessage: the messages the cascading EditContext holds for the field shown in the error line
/// under the items and referenced by the rating, giving way to the app's own ErrorMessage, and never announced.
/// </summary>
[TestClass]
public class BitRatingValidationMessageTests : BunitTestContext
{
    private const string RangeMessage = "Your rate must be between 1 and 5";

    [TestMethod]
    public void BitRatingShowValidationMessageShouldShowTheMessageOfTheFormUnderTheItems()
    {
        var component = RenderComponent<BitRatingValidationTest>(parameters => parameters.Add(p => p.ShowValidationMessage, true));

        // Nothing is shown before the form has anything to say.
        Assert.AreEqual(0, component.FindAll(".bit-rtg-erm").Count);
        Assert.IsNull(component.Find(".bit-rtg").GetAttribute("aria-describedby"));

        component.Find("form").Submit();

        var root = component.Find(".bit-rtg");
        var error = component.Find(".bit-rtg-erm");

        Assert.AreEqual(1, component.Instance.InvalidCount);
        Assert.AreEqual(RangeMessage, error.TextContent.Trim());
        Assert.AreEqual(error.Id, root.GetAttribute("aria-describedby"));
        Assert.AreEqual("true", root.GetAttribute("aria-invalid"));
        Assert.IsTrue(root.ClassList.Contains("bit-inv"));

        // The line sits right under the items, where the app's own message goes, outside every item.
        var parts = root.Children.Select(c => c.ClassList.Contains("bit-rtg-erm") ? "error" : c.ClassList.Contains("bit-rtg-cnt") ? "items" : null)
                                 .Where(p => p is not null)
                                 .ToArray();

        CollectionAssert.AreEqual(new[] { "items", "error" }, parts);

        // A pick the form accepts takes the line, and the reference to it, away again.
        component.FindAll(".bit-rtg-btn")[2].Click();

        Assert.AreEqual(3, component.Instance.TestModel.Value);
        Assert.AreEqual(0, component.FindAll(".bit-rtg-erm").Count);
        Assert.IsNull(component.Find(".bit-rtg").GetAttribute("aria-describedby"));

        component.Find("form").Submit();

        Assert.AreEqual(1, component.Instance.ValidCount);
    }

    [TestMethod]
    public void BitRatingShowValidationMessageShouldFollowTheMessagesWhileTheFieldStaysInvalid()
    {
        var model = new BitRatingTestModel();
        var editContext = new EditContext(model);
        var field = new FieldIdentifier(model, nameof(BitRatingTestModel.Value));
        var store = new ValidationMessageStore(editContext);

        var component = RenderRating(editContext, model, parameters => parameters.Add(p => p.ShowValidationMessage, true));

        component.InvokeAsync(() =>
        {
            store.Add(field, "Rate the delivery too.");
            editContext.NotifyValidationStateChanged();
        });

        Assert.AreEqual("Rate the delivery too.", component.Find(".bit-rtg-erm").TextContent.Trim());

        // Every message the field holds is shown, in the order they were added, as the ValidationMessage component
        // lists them.
        component.InvokeAsync(() =>
        {
            store.Clear(field);
            store.Add(field, "One star needs a reason.");
            store.Add(field, "Reviews close on Friday.");
            editContext.NotifyValidationStateChanged();
        });

        Assert.AreEqual("One star needs a reason. Reviews close on Friday.", component.Find(".bit-rtg-erm").TextContent.Trim());
    }

    [TestMethod]
    public void BitRatingWithoutShowValidationMessageShouldLeaveTheMessagesToTheValidationMessageComponent()
    {
        var model = new BitRatingTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderRating(editContext, model);

        component.InvokeAsync(() => editContext.Validate());

        // The rating is marked as before, and the message stays where the page puts it.
        Assert.AreEqual("true", component.Find(".bit-rtg").GetAttribute("aria-invalid"));
        Assert.AreEqual(0, component.FindAll(".bit-rtg-erm").Count);
        Assert.IsNull(component.Find(".bit-rtg").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitRatingErrorMessageShouldWinTheLineOverTheMessagesOfTheEditContext()
    {
        var model = new BitRatingTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderRating(editContext, model, parameters =>
        {
            parameters.Add(p => p.ShowValidationMessage, true);
            parameters.Add(p => p.ErrorMessage, "Five stars need a written review.");
        });

        component.InvokeAsync(() => editContext.Validate());

        Assert.AreEqual(1, component.FindAll(".bit-rtg-erm").Count);
        Assert.AreEqual("Five stars need a written review.", component.Find(".bit-rtg-erm").TextContent.Trim());

        // Once the app takes its message back, the EditContext's takes the line.
        component.Render(parameters => parameters.Add(p => p.ErrorMessage, null));

        Assert.AreEqual(RangeMessage, component.Find(".bit-rtg-erm").TextContent.Trim());
        Assert.AreEqual(component.Find(".bit-rtg-erm").Id, component.Find(".bit-rtg").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitRatingErrorMessageTemplateShouldKeepItsLabelOverTheMessagesOfTheEditContext()
    {
        var model = new BitRatingTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderRating(editContext, model, parameters =>
        {
            parameters.Add(p => p.ShowValidationMessage, true);
            parameters.Add(p => p.ErrorMessageAriaLabel, "Low scores need a reason before they are published.");
            parameters.Add(p => p.ErrorMessageTemplate, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "b");
                builder.AddContent(1, "Add a reason");
                builder.CloseElement();
            }));
        });

        component.InvokeAsync(() => editContext.Validate());

        // The template is what is on screen, so its label is what the rating is described by.
        var labelId = component.Find(".bit-rtg").GetAttribute("aria-describedby");

        Assert.AreEqual("Add a reason", component.Find(".bit-rtg-erm b").TextContent);
        Assert.IsFalse(component.Find(".bit-rtg-erm").TextContent.Contains(RangeMessage));
        Assert.AreEqual("Low scores need a reason before they are published.", component.Find($"#{labelId}").TextContent);
    }

    [TestMethod]
    public void BitRatingShouldNotAnnounceTheMessagesOfTheEditContext()
    {
        var model = new BitRatingTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        // One rating is wired for the live region of its errors by an ErrorMessage written as a null for now, the
        // other is not.
        var wired = RenderRating(editContext, model, parameters =>
        {
            parameters.Add(p => p.ShowValidationMessage, true);
            parameters.Add(p => p.ErrorMessage, null);
        });

        var plain = RenderRating(editContext, model, parameters => parameters.Add(p => p.ShowValidationMessage, true));

        wired.InvokeAsync(() => editContext.Validate());

        // A submit validates every field at once; the rating that takes the focus says what is wrong with it instead.
        // The region of the value says the value and nothing else.
        Assert.AreEqual(RangeMessage, wired.Find(".bit-rtg-erm").TextContent.Trim());
        Assert.AreEqual(string.Empty, wired.Find("[role=status]").TextContent.Trim());
        Assert.IsFalse(wired.FindAll("[aria-live]").Any(r => r.TextContent.Contains(RangeMessage)));

        // The messages do not wire a rating for a live region of its errors either.
        Assert.AreEqual(RangeMessage, plain.Find(".bit-rtg-erm").TextContent.Trim());
        Assert.AreEqual(0, plain.FindAll("[role=status]").Count);
        Assert.IsFalse(plain.FindAll("[aria-live]").Any(r => r.TextContent.Contains(RangeMessage)));
    }

    [TestMethod]
    public void BitRatingShouldDescribeTheRatingWithTheMessageAheadOfTheDescription()
    {
        var model = new BitRatingTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderRating(editContext, model, parameters =>
        {
            parameters.Add(p => p.ShowValidationMessage, true);
            parameters.Add(p => p.Description, "Half stars are allowed.");
            parameters.Add(p => p.HtmlAttributes, new Dictionary<string, object> { ["aria-describedby"] = "page-hint" });
        });

        component.InvokeAsync(() => editContext.Validate());

        // What the page pointed the rating at stays first, then what is wrong with the value, then the description -
        // the order the line and the description sit in under the items.
        var ids = component.Find(".bit-rtg").GetAttribute("aria-describedby")!.Split(' ');

        Assert.AreEqual(3, ids.Length);
        Assert.AreEqual("page-hint", ids[0]);
        Assert.AreEqual(component.Find(".bit-rtg-erm").Id, ids[1]);
        Assert.AreEqual(component.Find(".bit-rtg-dsc").Id, ids[2]);
    }

    [TestMethod]
    public void BitRatingForcedInvalidLabelShouldGiveWayToAMessageOnScreen()
    {
        var model = new BitRatingTestModel();
        var editContext = new EditContext(model);
        var field = new FieldIdentifier(model, nameof(BitRatingTestModel.Value));
        var store = new ValidationMessageStore(editContext);

        var component = RenderRating(editContext, model, parameters =>
        {
            parameters.Add(p => p.ShowValidationMessage, true);
            parameters.Add(p => p.Invalid, true);
            parameters.Add(p => p.ErrorMessageAriaLabel, "Refused by the server.");
        });

        var labelId = component.Find(".bit-rtg").GetAttribute("aria-describedby");

        Assert.AreEqual("Refused by the server.", component.Find($"#{labelId}").TextContent);

        component.InvokeAsync(() =>
        {
            store.Add(field, "Rate the delivery too.");
            editContext.NotifyValidationStateChanged();
        });

        // The text on screen describes the rating; the app's own rejection is still what the live region says.
        Assert.AreEqual(component.Find(".bit-rtg-erm").Id, component.Find(".bit-rtg").GetAttribute("aria-describedby"));
        Assert.AreEqual("Refused by the server.", component.Find("[role=status]").TextContent.Trim());

        component.InvokeAsync(() =>
        {
            store.Clear();
            editContext.NotifyValidationStateChanged();
        });

        Assert.AreEqual(labelId, component.Find(".bit-rtg").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitRatingThatStopsValidatingShouldTakeTheMessageAway()
    {
        var model = new BitRatingTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderRating(editContext, model, parameters =>
        {
            parameters.Add(p => p.ShowValidationMessage, true);
            parameters.Add(p => p.NoValidate, false);
        });

        component.InvokeAsync(() => editContext.Validate());

        Assert.AreEqual(1, component.FindAll(".bit-rtg-erm").Count);

        component.Render(parameters => parameters.Add(p => p.NoValidate, true));

        Assert.AreEqual(0, component.FindAll(".bit-rtg-erm").Count);
        Assert.IsNull(component.Find(".bit-rtg").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitRatingBoundToNothingShouldShowNoMessage()
    {
        var model = new BitRatingTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderRating(editContext, model, parameters => parameters.Add(p => p.ShowValidationMessage, true), bound: false);

        component.InvokeAsync(() => editContext.Validate());

        // The model has a message, but the rating has no member of it the message could belong to.
        Assert.IsTrue(editContext.GetValidationMessages().Any());
        Assert.AreEqual(0, component.FindAll(".bit-rtg-erm").Count);
    }

    [TestMethod]
    public void BitRatingShouldShowTheMessagesWhenACascadeAsksForThem()
    {
        var model = new BitRatingTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        // One BitParams around a form turns the messages on for every rating in it.
        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.AddCascadingValue(editContext);
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitRatingParams { ShowValidationMessage = true } });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitRating>(0);
                builder.AddAttribute(1, nameof(BitRating.AllowZeroStars), true);
                builder.AddAttribute(2, nameof(BitRating.Value), model.Value);
                builder.AddAttribute(3, nameof(BitRating.ValueChanged), EventCallback.Factory.Create<double>(this, v => model.Value = v));
                builder.AddAttribute(4, nameof(BitRating.ValueExpression), (Expression<Func<double>>)(() => model.Value));
                builder.CloseComponent();
            });
        });

        component.InvokeAsync(() => editContext.Validate());

        Assert.AreEqual(RangeMessage, component.Find(".bit-rtg-erm").TextContent.Trim());
    }

    private IRenderedComponent<BitRating> RenderRating(EditContext editContext, BitRatingTestModel model,
                                                       Action<ComponentParameterCollectionBuilder<BitRating>>? configure = null,
                                                       bool bound = true)
    {
        return RenderComponent<BitRating>(parameters =>
        {
            parameters.AddCascadingValue(editContext);
            parameters.Add(p => p.AllowZeroStars, true);

            if (bound)
            {
                parameters.Add(p => p.Value, model.Value);
                parameters.Add(p => p.ValueChanged, (double v) => model.Value = v);
                parameters.Add(p => p.ValueExpression, () => model.Value);
            }

            configure?.Invoke(parameters);
        });
    }
}
