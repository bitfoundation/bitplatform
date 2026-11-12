using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StringDropdown = Bit.BlazorUI.BitDropdown<Bit.BlazorUI.BitDropdownItem<string>, string>;

namespace Bit.BlazorUI.Tests.Components.Inputs.Dropdown;

/// <summary>
/// Covers ShowValidationMessage: the messages the cascading EditContext holds for the field - Value, or Values in the
/// multi select mode - shown in the error line under the dropdown and referenced by it, giving way to the app's own
/// ErrorMessage, and never announced.
/// </summary>
[TestClass]
public class BitDropdownValidationMessageTests : BunitTestContext
{
    private const string RequiredMessage = "The Value field is required.";
    private const string LiveRegion = ".bit-drp-lvr";

    // The element the dropdown is described from while it is not typed into.
    private const string Trigger = ".bit-drp-wrp";

    private readonly List<BitDropdownItem<string>> _items =
    [
        new() { Text = "Apple", Value = "f-app" },
        new() { Text = "Orange", Value = "f-ora" },
        new() { Text = "Banana", Value = "f-ban" },
        new() { Text = "Broccoli", Value = "v-bro" }
    ];

    [TestMethod]
    public void BitDropdownShowValidationMessageShouldShowTheMessageOfTheEditContextUnderTheField()
    {
        var model = new BitDropdownTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderDropdown(editContext, model, new() { [nameof(StringDropdown.ShowValidationMessage)] = true });

        // Nothing is shown before the EditContext has anything to say.
        Assert.IsEmpty(component.FindAll(".bit-drp-erm"));
        Assert.IsNull(component.Find(Trigger).GetAttribute("aria-describedby"));

        component.InvokeAsync(() => editContext.Validate());

        var error = component.Find(".bit-drp-erm");
        var trigger = component.Find(Trigger);

        Assert.AreEqual(RequiredMessage, error.TextContent.Trim());
        Assert.AreEqual(error.Id, trigger.GetAttribute("aria-describedby"));
        Assert.AreEqual("true", trigger.GetAttribute("aria-invalid"));
        Assert.IsTrue(component.Find(".bit-drp").ClassList.Contains("bit-inv"));

        // An item the EditContext accepts takes the line, and the reference to it, away again.
        component.Find(Trigger).Click();
        component.FindAll(".bit-drp-itm")[0].Click();

        Assert.AreEqual("f-app", model.Value);
        Assert.IsEmpty(component.FindAll(".bit-drp-erm"));
        Assert.IsNull(component.Find(Trigger).GetAttribute("aria-describedby"));
        Assert.IsNull(component.Find(Trigger).GetAttribute("aria-invalid"));
    }

    [TestMethod]
    public void BitDropdownShowValidationMessageShouldShowTheMessagesOfTheValuesInTheMultiSelectMode()
    {
        var model = new BitDropdownMultiSelectTestModel { Values = ["f-ora"] };

        var component = RenderComponent<BitDropdownMultiSelectValidationTest>(parameters =>
        {
            parameters.Add(p => p.IsMultiSelect, true);
            parameters.Add(p => p.Items, _items);
            parameters.Add(p => p.TestModel, model);
            parameters.Add(p => p.ShowValidationMessage, true);
        });

        component.Find("form").Submit();

        // The multi select mode is bound to Values, so the messages of that field are the ones shown.
        var error = component.Find(".bit-drp-erm");

        Assert.AreEqual(GetMessage(model, nameof(BitDropdownMultiSelectTestModel.Values)), error.TextContent.Trim());
        Assert.AreEqual(error.Id, component.Find(Trigger).GetAttribute("aria-describedby"));
        Assert.AreEqual(1, component.Instance.InvalidCount);

        // A second item brings the selection within the range the model asks for.
        component.Find(Trigger).Click();
        component.FindAll(".bit-drp-iwr button")[2].Click();

        Assert.AreSequenceEqual(new[] { "f-ora", "f-ban" }, model.Values!.ToArray());
        Assert.IsEmpty(component.FindAll(".bit-drp-erm"));

        component.Find("form").Submit();

        Assert.AreEqual(1, component.Instance.ValidCount);
    }

    [TestMethod]
    public void BitDropdownShowValidationMessageShouldFollowTheMessagesWhileTheFieldStaysInvalid()
    {
        var model = new BitDropdownTestModel();
        var editContext = new EditContext(model);
        var field = new FieldIdentifier(model, nameof(BitDropdownTestModel.Value));
        var store = new ValidationMessageStore(editContext);

        var component = RenderDropdown(editContext, model, new() { [nameof(StringDropdown.ShowValidationMessage)] = true });

        component.InvokeAsync(() =>
        {
            store.Add(field, "Out of season.");
            editContext.NotifyValidationStateChanged();
        });

        Assert.AreEqual("Out of season.", component.Find(".bit-drp-erm").TextContent.Trim());

        // Every message the field holds is shown, in the order they were added, as the ValidationMessage component
        // lists them.
        component.InvokeAsync(() =>
        {
            store.Clear(field);
            store.Add(field, "Not a fruit.");
            store.Add(field, "Sold out.");
            editContext.NotifyValidationStateChanged();
        });

        Assert.AreEqual("Not a fruit. Sold out.", component.Find(".bit-drp-erm").TextContent.Trim());
    }

    [TestMethod]
    public void BitDropdownWithoutShowValidationMessageShouldLeaveTheMessagesToTheValidationMessageComponent()
    {
        var model = new BitDropdownTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderDropdown(editContext, model, new());

        component.InvokeAsync(() => editContext.Validate());

        // The dropdown is marked as before, and the message stays where the page puts it.
        Assert.AreEqual("true", component.Find(Trigger).GetAttribute("aria-invalid"));
        Assert.IsEmpty(component.FindAll(".bit-drp-erm"));
        Assert.IsNull(component.Find(Trigger).GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitDropdownErrorMessageShouldWinTheLineOverTheMessagesOfTheEditContext()
    {
        var model = new BitDropdownTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var attributes = new Dictionary<string, object?>
        {
            [nameof(StringDropdown.ShowValidationMessage)] = true,
            [nameof(StringDropdown.ErrorMessage)] = "The store has none of these left.",
        };

        var component = RenderDropdown(editContext, model, attributes);

        component.InvokeAsync(() => editContext.Validate());

        Assert.ContainsSingle(component.FindAll(".bit-drp-erm"));
        Assert.AreEqual("The store has none of these left.", component.Find(".bit-drp-erm").TextContent.Trim());

        // Once the app takes its message back, the EditContext's takes the line.
        attributes[nameof(StringDropdown.ErrorMessage)] = null;
        Rerender(component, editContext, model, attributes);

        Assert.AreEqual(RequiredMessage, component.Find(".bit-drp-erm").TextContent.Trim());
    }

    [TestMethod]
    public void BitDropdownErrorMessageTemplateShouldKeepItsLabelOverTheMessagesOfTheEditContext()
    {
        var model = new BitDropdownTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderDropdown(editContext, model, new()
        {
            [nameof(StringDropdown.ShowValidationMessage)] = true,
            [nameof(StringDropdown.ErrorMessageAriaLabel)] = "The store is closed.",
            [nameof(StringDropdown.ErrorMessageTemplate)] = (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "b");
                builder.AddContent(1, "Closed");
                builder.CloseElement();
            }),
        });

        component.InvokeAsync(() => editContext.Validate());

        // The template is what is on screen, so its label is what the dropdown is described by.
        var labelId = component.Find(Trigger).GetAttribute("aria-describedby");

        Assert.AreEqual("Closed", component.Find(".bit-drp-erm b").TextContent);
        Assert.DoesNotContain(RequiredMessage, component.Find(".bit-drp-erm").TextContent);
        Assert.AreEqual("The store is closed.", component.Find($"#{labelId}").TextContent);
    }

    [TestMethod]
    public void BitDropdownShouldNotAnnounceTheMessagesOfTheEditContext()
    {
        var model = new BitDropdownTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderDropdown(editContext, model, new() { [nameof(StringDropdown.ShowValidationMessage)] = true });

        component.InvokeAsync(() => editContext.Validate());

        // A submit validates every field at once; the field that takes the focus says what is wrong with it instead.
        Assert.AreEqual(RequiredMessage, component.Find(".bit-drp-erm").TextContent.Trim());
        Assert.AreEqual(string.Empty, component.Find(LiveRegion).TextContent.Trim());
    }

    [TestMethod]
    public void BitDropdownShouldDescribeTheFieldWithTheMessageBeforeTheDescriptions()
    {
        var model = new BitDropdownTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderDropdown(editContext, model, new()
        {
            [nameof(StringDropdown.ShowValidationMessage)] = true,
            [nameof(StringDropdown.Description)] = "Pick the fruit of the week.",
            [nameof(StringDropdown.AriaDescription)] = "One item only.",
        });

        component.InvokeAsync(() => editContext.Validate());

        var ids = component.Find(Trigger).GetAttribute("aria-describedby")!.Split(' ');

        Assert.HasCount(3, ids);
        Assert.AreEqual(component.Find(".bit-drp-erm").Id, ids[0]);
        Assert.AreEqual(component.Find(".bit-drp-des").Id, ids[1]);
        Assert.AreEqual("One item only.", component.Find($"#{ids[2]}").TextContent);
    }

    [TestMethod]
    public void BitDropdownInComboBoxModeShouldDescribeItsInputWithTheMessage()
    {
        var model = new BitDropdownTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderDropdown(editContext, model, new()
        {
            [nameof(StringDropdown.ShowValidationMessage)] = true,
            [nameof(StringDropdown.Combo)] = true,
        });

        component.InvokeAsync(() => editContext.Validate());

        // The input is the combobox there, so it is the one described; the field around it is no control of its own.
        Assert.AreEqual(component.Find(".bit-drp-erm").Id, component.Find(".bit-drp-inp").GetAttribute("aria-describedby"));
        Assert.IsNull(component.Find(Trigger).GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitDropdownForcedInvalidLabelShouldGiveWayToAMessageOnScreen()
    {
        var model = new BitDropdownTestModel();
        var editContext = new EditContext(model);
        var field = new FieldIdentifier(model, nameof(BitDropdownTestModel.Value));
        var store = new ValidationMessageStore(editContext);

        var component = RenderDropdown(editContext, model, new()
        {
            [nameof(StringDropdown.ShowValidationMessage)] = true,
            [nameof(StringDropdown.Invalid)] = true,
            [nameof(StringDropdown.ErrorMessageAriaLabel)] = "Refused by the server.",
        });

        var labelId = component.Find(Trigger).GetAttribute("aria-describedby");

        Assert.AreEqual("Refused by the server.", component.Find($"#{labelId}").TextContent);

        component.InvokeAsync(() =>
        {
            store.Add(field, "Out of season.");
            editContext.NotifyValidationStateChanged();
        });

        // The text on screen describes the field; the app's own rejection is still what the live region says.
        Assert.AreEqual(component.Find(".bit-drp-erm").Id, component.Find(Trigger).GetAttribute("aria-describedby"));
        Assert.AreEqual("Refused by the server.", component.Find(LiveRegion).TextContent.Trim());

        component.InvokeAsync(() =>
        {
            store.Clear();
            editContext.NotifyValidationStateChanged();
        });

        Assert.AreEqual(labelId, component.Find(Trigger).GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitDropdownThatStopsValidatingShouldTakeTheMessageAway()
    {
        var model = new BitDropdownTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var attributes = new Dictionary<string, object?>
        {
            [nameof(StringDropdown.ShowValidationMessage)] = true,
            [nameof(StringDropdown.NoValidate)] = false,
        };

        var component = RenderDropdown(editContext, model, attributes);

        component.InvokeAsync(() => editContext.Validate());

        Assert.ContainsSingle(component.FindAll(".bit-drp-erm"));

        attributes[nameof(StringDropdown.NoValidate)] = true;
        Rerender(component, editContext, model, attributes);

        Assert.IsEmpty(component.FindAll(".bit-drp-erm"));
        Assert.IsNull(component.Find(Trigger).GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitDropdownBoundToNothingShouldShowNoMessage()
    {
        var model = new BitDropdownTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderDropdown(editContext, model, new() { [nameof(StringDropdown.ShowValidationMessage)] = true }, bound: false);

        component.InvokeAsync(() => editContext.Validate());

        // The model has a message, but the dropdown has no member of it the message could belong to.
        Assert.IsNotEmpty(editContext.GetValidationMessages());
        Assert.IsEmpty(component.FindAll(".bit-drp-erm"));
    }

    [TestMethod]
    public void BitDropdownShowValidationMessageShouldReplaceTheValidationMessageComponentOfAnEditForm()
    {
        var component = RenderComponent<BitDropdownValidationTest>(parameters =>
        {
            parameters.Add(p => p.Items, _items);
            parameters.Add(p => p.TestModel, new BitDropdownTestModel());
            parameters.Add(p => p.ShowValidationMessage, true);
        });

        component.Find("form").Submit();

        Assert.AreEqual(RequiredMessage, component.Find(".bit-drp-erm").TextContent.Trim());
        Assert.AreEqual(1, component.Instance.InvalidCount);

        component.Find(Trigger).Click();
        component.FindAll(".bit-drp-itm")[1].Click();

        Assert.IsEmpty(component.FindAll(".bit-drp-erm"));

        component.Find("form").Submit();

        Assert.AreEqual(1, component.Instance.ValidCount);
    }

    [TestMethod]
    public void BitDropdownShouldShowTheMessagesWhenACascadeAsksForThem()
    {
        var model = new BitDropdownTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        // One BitParams around a form turns the messages on for every dropdown in it.
        var component = RenderComponent<CascadingValue<EditContext>>(parameters =>
        {
            parameters.Add(p => p.Value, editContext);
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<BitParams>(0);
                builder.AddAttribute(1, nameof(BitParams.Parameters), new List<IBitComponentParams> { new BitDropdownParams<BitDropdownItem<string>, string> { ShowValidationMessage = true } });
                builder.AddAttribute(2, nameof(BitParams.ChildContent), (RenderFragment)(dropdown => AddDropdown(dropdown, model, new(), bound: true)));
                builder.CloseComponent();
            }));
        });

        component.InvokeAsync(() => editContext.Validate());

        Assert.AreEqual(RequiredMessage, component.Find(".bit-drp-erm").TextContent.Trim());
    }

    // The messages the data annotations of a member give for its current value, as the DataAnnotationsValidator of a
    // form reports them, so a test does not depend on how the framework words them.
    private static string GetMessage(object model, string member)
    {
        var results = new List<ValidationResult>();
        var value = model.GetType().GetProperty(member)!.GetValue(model);

        Validator.TryValidateProperty(value, new ValidationContext(model) { MemberName = member }, results);

        return string.Join(' ', results.Select(r => r.ErrorMessage));
    }

    private IRenderedComponent<CascadingValue<EditContext>> RenderDropdown(EditContext editContext, BitDropdownTestModel model,
                                                                           Dictionary<string, object?> attributes, bool bound = true)
    {
        return RenderComponent<CascadingValue<EditContext>>(parameters =>
        {
            parameters.Add(p => p.Value, editContext);
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder => AddDropdown(builder, model, attributes, bound)));
        });
    }

    // A CascadingValue takes its value - and its content - anew on every parameters pass, so both are handed to it again.
    private void Rerender(IRenderedComponent<CascadingValue<EditContext>> component, EditContext editContext, BitDropdownTestModel model,
                          Dictionary<string, object?> attributes)
    {
        component.Render(parameters =>
        {
            parameters.Add(p => p.Value, editContext);
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder => AddDropdown(builder, model, attributes, bound: true)));
        });
    }

    private void AddDropdown(RenderTreeBuilder builder, BitDropdownTestModel model, Dictionary<string, object?> attributes, bool bound)
    {
        builder.OpenComponent<StringDropdown>(0);
        builder.AddAttribute(1, nameof(StringDropdown.Items), _items);

        if (bound)
        {
            builder.AddAttribute(2, nameof(StringDropdown.Value), model.Value);
            builder.AddAttribute(3, nameof(StringDropdown.ValueChanged), EventCallback.Factory.Create<string?>(this, v => model.Value = v));
            builder.AddAttribute(4, nameof(StringDropdown.ValueExpression), (Expression<Func<string?>>)(() => model.Value));
        }

        foreach (var (name, value) in attributes)
        {
            builder.AddAttribute(5, name, value);
        }

        builder.CloseComponent();
    }
}
