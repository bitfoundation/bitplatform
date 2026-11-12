using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.PhoneInput;

/// <summary>
/// Covers ShowValidationMessage: the messages the cascading EditContext holds for the field shown in the error line
/// under the field and referenced by the number input, giving way to the app's own ErrorMessage, and never announced.
/// </summary>
[TestClass]
public class BitPhoneInputValidationMessageTests : BunitTestContext
{
    private const string RequiredMessage = "The Phone field is required.";
    private const string PatternMessage = "Enter the full number.";

    [TestMethod]
    public void BitPhoneInputShowValidationMessageShouldShowTheMessagesOfTheFormUnderTheField()
    {
        var model = new ValidationModel();
        var validCount = 0;
        var invalidCount = 0;

        var component = RenderComponent<EditForm>(parameters =>
        {
            parameters.Add(p => p.Model, model);
            parameters.Add(p => p.OnValidSubmit, () => validCount++);
            parameters.Add(p => p.OnInvalidSubmit, () => invalidCount++);
            parameters.Add(p => p.ChildContent, (RenderFragment<EditContext>)(_ => builder =>
            {
                builder.OpenComponent<DataAnnotationsValidator>(0);
                builder.CloseComponent();

                builder.OpenComponent<BitPhoneInput>(1);
                builder.AddAttribute(2, nameof(BitPhoneInput.DefaultCountry), BitCountries.Germany);
                builder.AddAttribute(3, nameof(BitPhoneInput.ShowValidationMessage), true);
                builder.AddAttribute(4, nameof(BitPhoneInput.Value), model.Phone);
                builder.AddAttribute(5, nameof(BitPhoneInput.ValueChanged), EventCallback.Factory.Create<string?>(this, v => model.Phone = v));
                builder.AddAttribute(6, nameof(BitPhoneInput.ValueExpression), (Expression<Func<string?>>)(() => model.Phone));
                builder.CloseComponent();
            }));
        });

        // Nothing is shown before the form has anything to say.
        Assert.IsEmpty(component.FindAll(".bit-phi-erm"));
        Assert.IsNull(component.Find("input.bit-phi-inp").GetAttribute("aria-describedby"));

        component.Find("form").Submit();

        component.WaitForAssertion(() =>
        {
            var input = component.Find("input.bit-phi-inp");
            var error = component.Find(".bit-phi-erm");

            Assert.AreEqual(1, invalidCount);
            Assert.AreEqual(RequiredMessage, error.TextContent.Trim());
            Assert.AreEqual(error.Id, input.GetAttribute("aria-describedby"));
            Assert.AreEqual("true", input.GetAttribute("aria-invalid"));
        });

        // The full E.164 value is what is validated, so a number too short for the pattern says so in its place.
        component.Find("input.bit-phi-inp").Change("30");

        component.WaitForAssertion(() =>
        {
            Assert.AreEqual("+4930", model.Phone);
            Assert.AreEqual(PatternMessage, component.Find(".bit-phi-erm").TextContent.Trim());
        });

        // A number the form accepts takes the line, and the reference to it, away again.
        component.Find("input.bit-phi-inp").Change("30123456");

        component.WaitForAssertion(() =>
        {
            Assert.IsEmpty(component.FindAll(".bit-phi-erm"));
            Assert.IsNull(component.Find("input.bit-phi-inp").GetAttribute("aria-describedby"));
        });

        component.Find("form").Submit();

        Assert.AreEqual(1, validCount);
    }

    [TestMethod]
    public void BitPhoneInputShowValidationMessageShouldFollowTheMessagesWhileTheFieldStaysInvalid()
    {
        var model = new ValidationModel();
        var editContext = new EditContext(model);
        var field = new FieldIdentifier(model, nameof(ValidationModel.Phone));
        var store = new ValidationMessageStore(editContext);

        var component = RenderPhoneInput(editContext, model, parameters => parameters.Add(p => p.ShowValidationMessage, true));

        component.InvokeAsync(() =>
        {
            store.Add(field, "That number is already registered.");
            editContext.NotifyValidationStateChanged();
        });

        Assert.AreEqual("That number is already registered.", component.Find(".bit-phi-erm").TextContent.Trim());

        // Every message the field holds is shown, in the order they were added, as the ValidationMessage component
        // lists them.
        component.InvokeAsync(() =>
        {
            store.Clear(field);
            store.Add(field, "Mobile numbers only.");
            store.Add(field, "No premium-rate numbers.");
            editContext.NotifyValidationStateChanged();
        });

        Assert.AreEqual("Mobile numbers only. No premium-rate numbers.", component.Find(".bit-phi-erm").TextContent.Trim());
    }

    [TestMethod]
    public void BitPhoneInputWithoutShowValidationMessageShouldLeaveTheMessagesToTheValidationMessageComponent()
    {
        var model = new ValidationModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderPhoneInput(editContext, model);

        component.InvokeAsync(() => editContext.Validate());

        // The field is marked as before, and the message stays where the page puts it.
        Assert.AreEqual("true", component.Find("input.bit-phi-inp").GetAttribute("aria-invalid"));
        Assert.IsEmpty(component.FindAll(".bit-phi-erm"));
        Assert.IsNull(component.Find("input.bit-phi-inp").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitPhoneInputErrorMessageShouldWinTheLineOverTheMessagesOfTheEditContext()
    {
        var model = new ValidationModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderPhoneInput(editContext, model, parameters =>
        {
            parameters.Add(p => p.ShowValidationMessage, true);
            parameters.Add(p => p.ErrorMessage, "That number is already registered.");
        });

        component.InvokeAsync(() => editContext.Validate());

        Assert.ContainsSingle(component.FindAll(".bit-phi-erm"));
        Assert.AreEqual("That number is already registered.", component.Find(".bit-phi-erm").TextContent.Trim());

        // Once the app takes its message back, the EditContext's takes the line.
        component.Render(parameters => parameters.Add(p => p.ErrorMessage, null));

        Assert.AreEqual(RequiredMessage, component.Find(".bit-phi-erm").TextContent.Trim());
        Assert.AreEqual(component.Find(".bit-phi-erm").Id, component.Find("input.bit-phi-inp").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitPhoneInputErrorMessageTemplateShouldWinTheLineOverTheMessagesOfTheEditContext()
    {
        var model = new ValidationModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderPhoneInput(editContext, model, parameters =>
        {
            parameters.Add(p => p.ShowValidationMessage, true);
            parameters.Add(p => p.ErrorMessageTemplate, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "b");
                builder.AddContent(1, "Already registered");
                builder.CloseElement();
            }));
        });

        component.InvokeAsync(() => editContext.Validate());

        Assert.AreEqual("Already registered", component.Find(".bit-phi-erm b").TextContent);
        Assert.DoesNotContain(RequiredMessage, component.Find(".bit-phi-erm").TextContent);
    }

    [TestMethod]
    public void BitPhoneInputShouldNotAnnounceTheMessagesOfTheEditContext()
    {
        var model = new ValidationModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderPhoneInput(editContext, model, parameters => parameters.Add(p => p.ShowValidationMessage, true));

        component.InvokeAsync(() => editContext.Validate());

        // A submit validates every field at once; the field that takes the focus says what is wrong with it instead.
        Assert.AreEqual(RequiredMessage, component.Find(".bit-phi-erm").TextContent.Trim());
        Assert.DoesNotContain(RequiredMessage, component.Find(".bit-phi-lvr").TextContent);
    }

    [TestMethod]
    public void BitPhoneInputShouldDescribeTheNumberWithTheCountryAndThenTheMessageAheadOfTheDescription()
    {
        var model = new ValidationModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderPhoneInput(editContext, model, parameters =>
        {
            parameters.Add(p => p.ShowValidationMessage, true);
            parameters.Add(p => p.NoDropdown, true);
            parameters.Add(p => p.Description, "German numbers only.");
            parameters.Add(p => p.InputHtmlAttributes, new Dictionary<string, object> { ["aria-describedby"] = "page-hint" });
        });

        component.InvokeAsync(() => editContext.Validate());

        // What the page pointed the input at stays first; a country the keyboard never reaches comes next, since the
        // number means nothing without it; then what is wrong with the value, and last the description.
        var ids = component.Find("input.bit-phi-inp").GetAttribute("aria-describedby")!.Split(' ');

        Assert.HasCount(4, ids);
        Assert.AreEqual("page-hint", ids[0]);
        Assert.AreEqual(component.Find("div.bit-phi-drp").Id, ids[1]);
        Assert.AreEqual(component.Find(".bit-phi-erm").Id, ids[2]);
        Assert.AreEqual(component.Find(".bit-phi-des").Id, ids[3]);
    }

    [TestMethod]
    public void BitPhoneInputThatStopsValidatingShouldTakeTheMessageAway()
    {
        var model = new ValidationModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderPhoneInput(editContext, model, parameters =>
        {
            parameters.Add(p => p.ShowValidationMessage, true);
            parameters.Add(p => p.NoValidate, false);
        });

        component.InvokeAsync(() => editContext.Validate());

        Assert.ContainsSingle(component.FindAll(".bit-phi-erm"));

        component.Render(parameters => parameters.Add(p => p.NoValidate, true));

        Assert.IsEmpty(component.FindAll(".bit-phi-erm"));
        Assert.IsNull(component.Find("input.bit-phi-inp").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitPhoneInputBoundToNothingShouldShowNoMessage()
    {
        var model = new ValidationModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderPhoneInput(editContext, model, parameters => parameters.Add(p => p.ShowValidationMessage, true), bound: false);

        component.InvokeAsync(() => editContext.Validate());

        // The model has a message, but the phone input has no member of it the message could belong to.
        Assert.IsNotEmpty(editContext.GetValidationMessages());
        Assert.IsEmpty(component.FindAll(".bit-phi-erm"));
    }

    [TestMethod]
    public void BitPhoneInputShouldShowTheMessagesWhenACascadeAsksForThem()
    {
        var model = new ValidationModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        // One BitParams around a form turns the messages on for every phone input in it.
        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.AddCascadingValue(editContext);
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitPhoneInputParams { ShowValidationMessage = true } });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitPhoneInput>(0);
                builder.AddAttribute(1, nameof(BitPhoneInput.DefaultCountry), BitCountries.Germany);
                builder.AddAttribute(2, nameof(BitPhoneInput.Value), model.Phone);
                builder.AddAttribute(3, nameof(BitPhoneInput.ValueChanged), EventCallback.Factory.Create<string?>(this, v => model.Phone = v));
                builder.AddAttribute(4, nameof(BitPhoneInput.ValueExpression), (Expression<Func<string?>>)(() => model.Phone));
                builder.CloseComponent();
            });
        });

        component.InvokeAsync(() => editContext.Validate());

        Assert.AreEqual(RequiredMessage, component.Find(".bit-phi-erm").TextContent.Trim());
    }

    private IRenderedComponent<BitPhoneInput> RenderPhoneInput(EditContext editContext, ValidationModel model,
                                                               Action<ComponentParameterCollectionBuilder<BitPhoneInput>>? configure = null,
                                                               bool bound = true)
    {
        return RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.AddCascadingValue(editContext);
            parameters.Add(p => p.DefaultCountry, BitCountries.Germany);

            if (bound)
            {
                parameters.Add(p => p.Value, model.Phone);
                parameters.Add(p => p.ValueChanged, (string? v) => model.Phone = v);
                parameters.Add(p => p.ValueExpression, () => model.Phone);
            }

            configure?.Invoke(parameters);
        });
    }

    private class ValidationModel
    {
        [Required]
        [RegularExpression(@"^\+[0-9]{8,15}$", ErrorMessage = PatternMessage)]
        public string? Phone { get; set; }
    }
}
