using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.TextField;

/// <summary>
/// Covers ShowValidationMessage: the messages the cascading EditContext holds for the field, shown in the error line
/// under the text field and referenced by its input, giving way to the app's own ErrorMessage, and never announced.
/// </summary>
[TestClass]
public class BitTextFieldValidationMessageTests : BunitTestContext
{
    private const string LiveRegion = ".bit-tfl > [role=status]";

    [TestInitialize]
    public void SetupJsInteropMode()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [TestMethod, DataRow(false), DataRow(true)]
    public void BitTextFieldShowValidationMessageShouldShowTheMessageOfTheEditContextUnderTheField(bool multiline)
    {
        var model = new BitTextFieldTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderField(editContext, model, new()
        {
            [nameof(BitTextField.ShowValidationMessage)] = true,
            [nameof(BitTextField.Multiline)] = multiline,
        });

        // Nothing is shown before the EditContext has anything to say.
        Assert.AreEqual(0, component.FindAll(".bit-tfl-erm").Count);
        Assert.IsNull(component.Find(".bit-tfl-inp").GetAttribute("aria-describedby"));

        component.InvokeAsync(() => editContext.Validate());

        var error = component.Find(".bit-tfl-erm");
        var input = component.Find(".bit-tfl-inp");

        Assert.AreEqual("The Value field is required.", error.TextContent.Trim());
        Assert.AreEqual(error.Id, input.GetAttribute("aria-describedby"));
        Assert.AreEqual("true", input.GetAttribute("aria-invalid"));
        Assert.IsTrue(component.Find(".bit-tfl").ClassList.Contains("bit-inv"));

        // A value the EditContext accepts takes the line, and the reference to it, away again.
        component.Find(".bit-tfl-inp").Change("a@b.co");

        Assert.AreEqual(0, component.FindAll(".bit-tfl-erm").Count);
        Assert.IsNull(component.Find(".bit-tfl-inp").GetAttribute("aria-describedby"));
        Assert.IsNull(component.Find(".bit-tfl-inp").GetAttribute("aria-invalid"));
    }

    [TestMethod]
    public void BitTextFieldShowValidationMessageShouldFollowTheMessagesWhileTheFieldStaysInvalid()
    {
        var model = new BitTextFieldTestModel();
        var editContext = new EditContext(model);
        var field = new FieldIdentifier(model, nameof(BitTextFieldTestModel.Value));
        var store = new ValidationMessageStore(editContext);

        var component = RenderField(editContext, model, new() { [nameof(BitTextField.ShowValidationMessage)] = true });

        component.InvokeAsync(() =>
        {
            store.Add(field, "Too short.");
            editContext.NotifyValidationStateChanged();
        });

        Assert.AreEqual("Too short.", component.Find(".bit-tfl-erm").TextContent.Trim());

        // Every message the field holds is shown, in the order they were added, as the ValidationMessage component
        // lists them.
        component.InvokeAsync(() =>
        {
            store.Clear(field);
            store.Add(field, "Not an email address.");
            store.Add(field, "Too long.");
            editContext.NotifyValidationStateChanged();
        });

        Assert.AreEqual("Not an email address. Too long.", component.Find(".bit-tfl-erm").TextContent.Trim());
    }

    [TestMethod]
    public void BitTextFieldWithoutShowValidationMessageShouldLeaveTheMessagesToTheValidationMessageComponent()
    {
        var model = new BitTextFieldTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderField(editContext, model, new());

        component.InvokeAsync(() => editContext.Validate());

        // The field is marked as before, and the message stays where the page puts it.
        Assert.AreEqual("true", component.Find(".bit-tfl-inp").GetAttribute("aria-invalid"));
        Assert.AreEqual(0, component.FindAll(".bit-tfl-erm").Count);
        Assert.IsNull(component.Find(".bit-tfl-inp").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitTextFieldErrorMessageShouldWinTheLineOverTheMessagesOfTheEditContext()
    {
        var model = new BitTextFieldTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var attributes = new Dictionary<string, object?>
        {
            [nameof(BitTextField.ShowValidationMessage)] = true,
            [nameof(BitTextField.ErrorMessage)] = "That address is already registered.",
        };

        var component = RenderField(editContext, model, attributes);

        component.InvokeAsync(() => editContext.Validate());

        Assert.AreEqual(1, component.FindAll(".bit-tfl-erm").Count);
        Assert.AreEqual("That address is already registered.", component.Find(".bit-tfl-erm").TextContent.Trim());

        // Once the app takes its message back, the EditContext's takes the line.
        attributes[nameof(BitTextField.ErrorMessage)] = null;
        Rerender(component, editContext, model, attributes);

        Assert.AreEqual("The Value field is required.", component.Find(".bit-tfl-erm").TextContent.Trim());
    }

    [TestMethod]
    public void BitTextFieldShouldNotAnnounceTheMessagesOfTheEditContext()
    {
        var model = new BitTextFieldTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderField(editContext, model, new() { [nameof(BitTextField.ShowValidationMessage)] = true });

        component.InvokeAsync(() => editContext.Validate());

        // A submit validates every field at once; the field that takes the focus says what is wrong with it instead.
        Assert.AreEqual("The Value field is required.", component.Find(".bit-tfl-erm").TextContent.Trim());
        Assert.AreEqual(string.Empty, component.Find(LiveRegion).TextContent.Trim());
    }

    [TestMethod]
    public void BitTextFieldShouldDescribeTheFieldWithTheMessageBetweenThePagesOwnReferenceAndTheDescription()
    {
        var model = new BitTextFieldTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderField(editContext, model, new()
        {
            [nameof(BitTextField.ShowValidationMessage)] = true,
            [nameof(BitTextField.Description)] = "Use your work address.",
            [nameof(BitTextField.InputHtmlAttributes)] = new Dictionary<string, object> { ["aria-describedby"] = "page-hint" },
        });

        component.InvokeAsync(() => editContext.Validate());

        var error = component.Find(".bit-tfl-erm");
        var description = component.Find(".bit-tfl-des");

        Assert.AreEqual($"page-hint {error.Id} {description.Id}", component.Find(".bit-tfl-inp").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitTextFieldForcedInvalidLabelShouldGiveWayToAMessageOnScreen()
    {
        var model = new BitTextFieldTestModel();
        var editContext = new EditContext(model);
        var field = new FieldIdentifier(model, nameof(BitTextFieldTestModel.Value));
        var store = new ValidationMessageStore(editContext);

        var component = RenderField(editContext, model, new()
        {
            [nameof(BitTextField.ShowValidationMessage)] = true,
            [nameof(BitTextField.Invalid)] = true,
            [nameof(BitTextField.ErrorMessageAriaLabel)] = "Refused by the server.",
        });

        var labelId = component.Find(".bit-tfl-inp").GetAttribute("aria-describedby");

        Assert.AreEqual("Refused by the server.", component.Find($"#{labelId}").TextContent);

        component.InvokeAsync(() =>
        {
            store.Add(field, "Not an email address.");
            editContext.NotifyValidationStateChanged();
        });

        // The text on screen describes the field; the app's own rejection is still what the live region says.
        Assert.AreEqual(component.Find(".bit-tfl-erm").Id, component.Find(".bit-tfl-inp").GetAttribute("aria-describedby"));
        Assert.AreEqual("Refused by the server.", component.Find(LiveRegion).TextContent.Trim());

        component.InvokeAsync(() =>
        {
            store.Clear();
            editContext.NotifyValidationStateChanged();
        });

        Assert.AreEqual(labelId, component.Find(".bit-tfl-inp").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitTextFieldThatStopsValidatingShouldTakeTheMessageAway()
    {
        var model = new BitTextFieldTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var attributes = new Dictionary<string, object?>
        {
            [nameof(BitTextField.ShowValidationMessage)] = true,
            [nameof(BitTextField.NoValidate)] = false,
        };

        var component = RenderField(editContext, model, attributes);

        component.InvokeAsync(() => editContext.Validate());

        Assert.AreEqual(1, component.FindAll(".bit-tfl-erm").Count);

        attributes[nameof(BitTextField.NoValidate)] = true;
        Rerender(component, editContext, model, attributes);

        Assert.AreEqual(0, component.FindAll(".bit-tfl-erm").Count);
        Assert.IsNull(component.Find(".bit-tfl-inp").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitTextFieldBoundToNothingShouldShowNoMessage()
    {
        var model = new BitTextFieldTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderField(editContext, model, new() { [nameof(BitTextField.ShowValidationMessage)] = true }, bound: false);

        component.InvokeAsync(() => editContext.Validate());

        // The model has a message, but the field has no member of it the message could belong to.
        Assert.IsTrue(editContext.GetValidationMessages().Any());
        Assert.AreEqual(0, component.FindAll(".bit-tfl-erm").Count);
    }

    [TestMethod]
    public void BitTextFieldShowValidationMessageShouldReplaceTheValidationMessageComponentOfAnEditForm()
    {
        var component = RenderComponent<BitTextFieldValidationTest>(parameters =>
        {
            parameters.Add(p => p.ShowValidationMessage, true);
        });

        component.Find("form").Submit();

        Assert.AreEqual("The Value field is required.", component.Find(".bit-tfl-erm").TextContent.Trim());

        // Both of the annotations the value fails are shown.
        component.Find(".bit-tfl-inp").Change("not-an-address-at-all");

        var message = component.Find(".bit-tfl-erm").TextContent.Trim();

        StringAssert.Contains(message, "The Value field is not a valid e-mail address.");
        StringAssert.Contains(message, "The field Value must be a string with a maximum length of 16.");

        component.Find(".bit-tfl-inp").Change("a@b.co");

        Assert.AreEqual(0, component.FindAll(".bit-tfl-erm").Count);

        component.Find("form").Submit();

        Assert.AreEqual(1, component.Instance.ValidCount);
    }

    [TestMethod]
    public void BitTextFieldShouldShowTheMessagesWhenACascadeAsksForThem()
    {
        var model = new BitTextFieldTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        // One BitParams around a form turns the messages on for every text field in it.
        var component = RenderComponent<CascadingValue<EditContext>>(parameters =>
        {
            parameters.Add(p => p.Value, editContext);
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<BitParams>(0);
                builder.AddAttribute(1, nameof(BitParams.Parameters), new List<IBitComponentParams> { new BitTextFieldParams { ShowValidationMessage = true } });
                builder.AddAttribute(2, nameof(BitParams.ChildContent), (RenderFragment)(field => AddField(field, model, new(), bound: true)));
                builder.CloseComponent();
            }));
        });

        component.InvokeAsync(() => editContext.Validate());

        Assert.AreEqual("The Value field is required.", component.Find(".bit-tfl-erm").TextContent.Trim());
    }

    private IRenderedComponent<CascadingValue<EditContext>> RenderField(EditContext editContext, BitTextFieldTestModel model,
                                                                        Dictionary<string, object?> attributes, bool bound = true)
    {
        return RenderComponent<CascadingValue<EditContext>>(parameters =>
        {
            parameters.Add(p => p.Value, editContext);
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder => AddField(builder, model, attributes, bound)));
        });
    }

    // A CascadingValue takes its value - and its content - anew on every parameters pass, so both are handed to it again.
    private void Rerender(IRenderedComponent<CascadingValue<EditContext>> component, EditContext editContext, BitTextFieldTestModel model,
                          Dictionary<string, object?> attributes)
    {
        component.Render(parameters =>
        {
            parameters.Add(p => p.Value, editContext);
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder => AddField(builder, model, attributes, bound: true)));
        });
    }

    private void AddField(RenderTreeBuilder builder, BitTextFieldTestModel model, Dictionary<string, object?> attributes, bool bound)
    {
        builder.OpenComponent<BitTextField>(0);

        if (bound)
        {
            builder.AddAttribute(1, nameof(BitTextField.Value), model.Value);
            builder.AddAttribute(2, nameof(BitTextField.ValueChanged), EventCallback.Factory.Create<string?>(this, v => model.Value = v));
            builder.AddAttribute(3, nameof(BitTextField.ValueExpression), (Expression<Func<string?>>)(() => model.Value));
        }

        foreach (var (name, value) in attributes)
        {
            builder.AddAttribute(4, name, value);
        }

        builder.CloseComponent();
    }
}
