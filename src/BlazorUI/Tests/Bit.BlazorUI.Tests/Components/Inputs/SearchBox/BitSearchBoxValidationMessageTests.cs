using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.SearchBox;

/// <summary>
/// Covers ShowValidationMessage: the messages the cascading EditContext holds for the field, shown in the error line
/// under the search box and referenced by its input, giving way to the app's own ErrorMessage, and never announced.
/// </summary>
[TestClass]
public class BitSearchBoxValidationMessageTests : BunitTestContext
{
    private const string RequiredMessage = "The Value field is required.";
    private const string EmailMessage = "The Value field is not a valid e-mail address.";

    // The region the rejections of the search box are announced through, apart from the one of its suggestions.
    private const string LiveRegion = ".bit-srb-elv";

    [TestMethod]
    public void BitSearchBoxShowValidationMessageShouldShowTheMessageOfTheEditContextUnderTheField()
    {
        var model = new BitSearchBoxTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderSearchBox(editContext, model, new() { [nameof(BitSearchBox.ShowValidationMessage)] = true });

        // Nothing is shown before the EditContext has anything to say.
        Assert.IsEmpty(component.FindAll(".bit-srb-erm"));
        Assert.IsNull(component.Find(".bit-srb-inp").GetAttribute("aria-describedby"));

        component.InvokeAsync(() => editContext.Validate());

        var error = component.Find(".bit-srb-erm");
        var input = component.Find(".bit-srb-inp");

        Assert.AreEqual(RequiredMessage, error.TextContent.Trim());
        Assert.AreEqual(error.Id, input.GetAttribute("aria-describedby"));
        Assert.AreEqual("true", input.GetAttribute("aria-invalid"));
        Assert.IsTrue(component.Find(".bit-srb").ClassList.Contains("bit-inv"));

        // Another rule the value fails takes the line over as it is typed.
        component.Find(".bit-srb-inp").Input("bit.com");

        Assert.AreEqual(EmailMessage, component.Find(".bit-srb-erm").TextContent.Trim());

        // A value the EditContext accepts takes the line, and the reference to it, away again.
        component.Find(".bit-srb-inp").Input("test@bit.com");

        Assert.IsEmpty(component.FindAll(".bit-srb-erm"));
        Assert.IsNull(component.Find(".bit-srb-inp").GetAttribute("aria-describedby"));
        Assert.IsNull(component.Find(".bit-srb-inp").GetAttribute("aria-invalid"));
    }

    [TestMethod]
    public void BitSearchBoxShowValidationMessageShouldFollowTheMessagesWhileTheFieldStaysInvalid()
    {
        var model = new BitSearchBoxTestModel();
        var editContext = new EditContext(model);
        var field = new FieldIdentifier(model, nameof(BitSearchBoxTestModel.Value));
        var store = new ValidationMessageStore(editContext);

        var component = RenderSearchBox(editContext, model, new() { [nameof(BitSearchBox.ShowValidationMessage)] = true });

        component.InvokeAsync(() =>
        {
            store.Add(field, "Too short.");
            editContext.NotifyValidationStateChanged();
        });

        Assert.AreEqual("Too short.", component.Find(".bit-srb-erm").TextContent.Trim());

        // Every message the field holds is shown, in the order they were added, as the ValidationMessage component
        // lists them.
        component.InvokeAsync(() =>
        {
            store.Clear(field);
            store.Add(field, "No such product.");
            store.Add(field, "Try a shorter term.");
            editContext.NotifyValidationStateChanged();
        });

        Assert.AreEqual("No such product. Try a shorter term.", component.Find(".bit-srb-erm").TextContent.Trim());
    }

    [TestMethod]
    public void BitSearchBoxWithoutShowValidationMessageShouldLeaveTheMessagesToTheValidationMessageComponent()
    {
        var model = new BitSearchBoxTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderSearchBox(editContext, model, new());

        component.InvokeAsync(() => editContext.Validate());

        // The field is marked as before, and the message stays where the page puts it.
        Assert.AreEqual("true", component.Find(".bit-srb-inp").GetAttribute("aria-invalid"));
        Assert.IsEmpty(component.FindAll(".bit-srb-erm"));
        Assert.IsNull(component.Find(".bit-srb-inp").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitSearchBoxErrorMessageShouldWinTheLineOverTheMessagesOfTheEditContext()
    {
        var model = new BitSearchBoxTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var attributes = new Dictionary<string, object?>
        {
            [nameof(BitSearchBox.ShowValidationMessage)] = true,
            [nameof(BitSearchBox.ErrorMessage)] = "The search service is unavailable.",
        };

        var component = RenderSearchBox(editContext, model, attributes);

        component.InvokeAsync(() => editContext.Validate());

        Assert.ContainsSingle(component.FindAll(".bit-srb-erm"));
        Assert.AreEqual("The search service is unavailable.", component.Find(".bit-srb-erm").TextContent.Trim());

        // Once the app takes its message back, the EditContext's takes the line.
        attributes[nameof(BitSearchBox.ErrorMessage)] = null;
        Rerender(component, editContext, model, attributes);

        Assert.AreEqual(RequiredMessage, component.Find(".bit-srb-erm").TextContent.Trim());
    }

    [TestMethod]
    public void BitSearchBoxErrorMessageTemplateShouldKeepItsLabelOverTheMessagesOfTheEditContext()
    {
        var model = new BitSearchBoxTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderSearchBox(editContext, model, new()
        {
            [nameof(BitSearchBox.ShowValidationMessage)] = true,
            [nameof(BitSearchBox.ErrorMessageAriaLabel)] = "The search service is unavailable.",
            [nameof(BitSearchBox.ErrorMessageTemplate)] = (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "b");
                builder.AddContent(1, "Unavailable");
                builder.CloseElement();
            }),
        });

        component.InvokeAsync(() => editContext.Validate());

        // The template is what is on screen, so its label is what the input is described by.
        var labelId = component.Find(".bit-srb-inp").GetAttribute("aria-describedby");

        Assert.AreEqual("Unavailable", component.Find(".bit-srb-erm b").TextContent);
        Assert.DoesNotContain(RequiredMessage, component.Find(".bit-srb-erm").TextContent);
        Assert.AreEqual("The search service is unavailable.", component.Find($"#{labelId}").TextContent);
    }

    [TestMethod]
    public void BitSearchBoxShouldNotAnnounceTheMessagesOfTheEditContext()
    {
        var model = new BitSearchBoxTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderSearchBox(editContext, model, new() { [nameof(BitSearchBox.ShowValidationMessage)] = true });

        component.InvokeAsync(() => editContext.Validate());

        // A submit validates every field at once; the field that takes the focus says what is wrong with it instead.
        Assert.AreEqual(RequiredMessage, component.Find(".bit-srb-erm").TextContent.Trim());
        Assert.AreEqual(string.Empty, component.Find(LiveRegion).TextContent.Trim());
    }

    [TestMethod]
    public void BitSearchBoxShouldDescribeTheFieldWithTheMessageBetweenThePagesOwnReferenceAndTheDescription()
    {
        var model = new BitSearchBoxTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderSearchBox(editContext, model, new()
        {
            [nameof(BitSearchBox.ShowValidationMessage)] = true,
            [nameof(BitSearchBox.Description)] = "Search by the e-mail address of the customer.",
            [nameof(BitSearchBox.AriaDescription)] = "Results update as you type.",
            [nameof(BitSearchBox.InputHtmlAttributes)] = new Dictionary<string, object> { ["aria-describedby"] = "page-hint" },
        });

        component.InvokeAsync(() => editContext.Validate());

        var error = component.Find(".bit-srb-erm");
        var description = component.Find(".bit-srb-des");
        var ariaDescription = component.FindAll(".bit-srb-ard").Single(e => e.TextContent == "Results update as you type.");

        Assert.AreEqual($"page-hint {error.Id} {description.Id} {ariaDescription.Id}", component.Find(".bit-srb-inp").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitSearchBoxThatStopsValidatingShouldTakeTheMessageAway()
    {
        var model = new BitSearchBoxTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var attributes = new Dictionary<string, object?>
        {
            [nameof(BitSearchBox.ShowValidationMessage)] = true,
            [nameof(BitSearchBox.NoValidate)] = false,
        };

        var component = RenderSearchBox(editContext, model, attributes);

        component.InvokeAsync(() => editContext.Validate());

        Assert.ContainsSingle(component.FindAll(".bit-srb-erm"));

        attributes[nameof(BitSearchBox.NoValidate)] = true;
        Rerender(component, editContext, model, attributes);

        Assert.IsEmpty(component.FindAll(".bit-srb-erm"));
        Assert.IsNull(component.Find(".bit-srb-inp").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitSearchBoxBoundToNothingShouldShowNoMessage()
    {
        var model = new BitSearchBoxTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderSearchBox(editContext, model, new() { [nameof(BitSearchBox.ShowValidationMessage)] = true }, bound: false);

        component.InvokeAsync(() => editContext.Validate());

        // The model has a message, but the field has no member of it the message could belong to.
        Assert.IsNotEmpty(editContext.GetValidationMessages());
        Assert.IsEmpty(component.FindAll(".bit-srb-erm"));
    }

    [TestMethod]
    public void BitSearchBoxShowValidationMessageShouldReplaceTheValidationMessageComponentOfAnEditForm()
    {
        var component = RenderComponent<BitSearchBoxValidationTest>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.ShowValidationMessage, true);
        });

        component.Find("form").Submit();

        Assert.AreEqual(RequiredMessage, component.Find(".bit-srb-erm").TextContent.Trim());
        Assert.AreEqual(1, component.Instance.InvalidCount);

        component.Find(".bit-srb-inp").Input("bit.com");

        Assert.AreEqual(EmailMessage, component.Find(".bit-srb-erm").TextContent.Trim());

        component.Find(".bit-srb-inp").Input("test@bit.com");

        Assert.IsEmpty(component.FindAll(".bit-srb-erm"));

        component.Find("form").Submit();

        Assert.AreEqual(1, component.Instance.ValidCount);
    }

    [TestMethod]
    public void BitSearchBoxShouldShowTheMessagesWhenACascadeAsksForThem()
    {
        var model = new BitSearchBoxTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        // One BitParams around a form turns the messages on for every search box in it.
        var component = RenderComponent<CascadingValue<EditContext>>(parameters =>
        {
            parameters.Add(p => p.Value, editContext);
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<BitParams>(0);
                builder.AddAttribute(1, nameof(BitParams.Parameters), new List<IBitComponentParams> { new BitSearchBoxParams { ShowValidationMessage = true } });
                builder.AddAttribute(2, nameof(BitParams.ChildContent), (RenderFragment)(searchBox => AddSearchBox(searchBox, model, new(), bound: true)));
                builder.CloseComponent();
            }));
        });

        component.InvokeAsync(() => editContext.Validate());

        Assert.AreEqual(RequiredMessage, component.Find(".bit-srb-erm").TextContent.Trim());
    }

    private IRenderedComponent<CascadingValue<EditContext>> RenderSearchBox(EditContext editContext, BitSearchBoxTestModel model,
                                                                            Dictionary<string, object?> attributes, bool bound = true)
    {
        return RenderComponent<CascadingValue<EditContext>>(parameters =>
        {
            parameters.Add(p => p.Value, editContext);
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder => AddSearchBox(builder, model, attributes, bound)));
        });
    }

    // A CascadingValue takes its value - and its content - anew on every parameters pass, so both are handed to it again.
    private void Rerender(IRenderedComponent<CascadingValue<EditContext>> component, EditContext editContext, BitSearchBoxTestModel model,
                          Dictionary<string, object?> attributes)
    {
        component.Render(parameters =>
        {
            parameters.Add(p => p.Value, editContext);
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder => AddSearchBox(builder, model, attributes, bound: true)));
        });
    }

    // Immediate commits the value on every keystroke, which is what lets a test type into the field and read the
    // verdict of the EditContext right after.
    private void AddSearchBox(RenderTreeBuilder builder, BitSearchBoxTestModel model, Dictionary<string, object?> attributes, bool bound)
    {
        builder.OpenComponent<BitSearchBox>(0);
        builder.AddAttribute(1, nameof(BitSearchBox.Immediate), true);

        if (bound)
        {
            builder.AddAttribute(2, nameof(BitSearchBox.Value), model.Value);
            builder.AddAttribute(3, nameof(BitSearchBox.ValueChanged), EventCallback.Factory.Create<string?>(this, v => model.Value = v));
            builder.AddAttribute(4, nameof(BitSearchBox.ValueExpression), (Expression<Func<string?>>)(() => model.Value));
        }

        foreach (var (name, value) in attributes)
        {
            builder.AddAttribute(5, name, value);
        }

        builder.CloseComponent();
    }
}
