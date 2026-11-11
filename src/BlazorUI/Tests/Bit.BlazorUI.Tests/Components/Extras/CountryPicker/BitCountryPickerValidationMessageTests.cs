using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.CountryPicker;

/// <summary>
/// Covers ShowValidationMessage: the messages the cascading EditContext holds for the field shown in the error line
/// under the field and referenced by it, giving way to the app's own ErrorMessage, and never announced.
/// </summary>
[TestClass]
public class BitCountryPickerValidationMessageTests : BunitTestContext
{
    private const string RequiredMessage = "The Country field is required.";

    private static readonly List<BitCountry> FiveCountries =
    [
        BitCountries.UnitedStates,
        BitCountries.Canada,
        BitCountries.UnitedKingdom,
        BitCountries.Germany,
        BitCountries.France,
    ];

    [TestMethod]
    public void BitCountryPickerShowValidationMessageShouldShowTheMessageOfTheFormUnderTheField()
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

                builder.OpenComponent<BitCountryPicker>(1);
                builder.AddAttribute(2, nameof(BitCountryPicker.Countries), FiveCountries);
                builder.AddAttribute(3, nameof(BitCountryPicker.ShowValidationMessage), true);
                builder.AddAttribute(4, nameof(BitCountryPicker.Value), model.Country);
                builder.AddAttribute(5, nameof(BitCountryPicker.ValueChanged), EventCallback.Factory.Create<BitCountry?>(this, c => model.Country = c));
                builder.AddAttribute(6, nameof(BitCountryPicker.ValueExpression), (Expression<Func<BitCountry?>>)(() => model.Country));
                builder.CloseComponent();
            }));
        });

        // Nothing is shown before the form has anything to say.
        Assert.IsEmpty(component.FindAll(".bit-cnp-erm"));
        Assert.IsNull(component.Find("button.bit-cnp-fld").GetAttribute("aria-describedby"));

        component.Find("form").Submit();

        component.WaitForAssertion(() =>
        {
            var field = component.Find("button.bit-cnp-fld");
            var error = component.Find(".bit-cnp-erm");

            Assert.AreEqual(1, invalidCount);
            Assert.AreEqual(RequiredMessage, error.TextContent.Trim());
            Assert.AreEqual(error.Id, field.GetAttribute("aria-describedby"));
            Assert.AreEqual("true", field.GetAttribute("aria-invalid"));
        });

        // A pick the form accepts takes the line, and the reference to it, away again.
        component.Find("button.bit-cnp-fld").Click();
        component.FindAll("button.bit-cnp-itm")[3].Click();

        var selectedCountry = model.Country;
        Assert.IsNotNull(selectedCountry);
        Assert.AreEqual("DE", selectedCountry.Iso2);

        component.WaitForAssertion(() =>
        {
            Assert.IsEmpty(component.FindAll(".bit-cnp-erm"));
            Assert.IsNull(component.Find("button.bit-cnp-fld").GetAttribute("aria-describedby"));
        });

        component.Find("form").Submit();

        Assert.AreEqual(1, validCount);
    }

    [TestMethod]
    public void BitCountryPickerShowValidationMessageShouldFollowTheMessagesWhileTheFieldStaysInvalid()
    {
        var model = new ValidationModel();
        var editContext = new EditContext(model);
        var field = new FieldIdentifier(model, nameof(ValidationModel.Country));
        var store = new ValidationMessageStore(editContext);

        var component = RenderPicker(editContext, model, parameters => parameters.Add(p => p.ShowValidationMessage, true));

        component.InvokeAsync(() =>
        {
            store.Add(field, "We do not ship there yet.");
            editContext.NotifyValidationStateChanged();
        });

        Assert.AreEqual("We do not ship there yet.", component.Find(".bit-cnp-erm").TextContent.Trim());

        // Every message the field holds is shown, in the order they were added, as the ValidationMessage component
        // lists them.
        component.InvokeAsync(() =>
        {
            store.Clear(field);
            store.Add(field, "Not in the European Union.");
            store.Add(field, "Ask our support team.");
            editContext.NotifyValidationStateChanged();
        });

        Assert.AreEqual("Not in the European Union. Ask our support team.", component.Find(".bit-cnp-erm").TextContent.Trim());
    }

    [TestMethod]
    public void BitCountryPickerWithoutShowValidationMessageShouldLeaveTheMessagesToTheValidationMessageComponent()
    {
        var model = new ValidationModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderPicker(editContext, model);

        component.InvokeAsync(() => editContext.Validate());

        // The field is marked as before, and the message stays where the page puts it.
        Assert.AreEqual("true", component.Find("button.bit-cnp-fld").GetAttribute("aria-invalid"));
        Assert.IsEmpty(component.FindAll(".bit-cnp-erm"));
        Assert.IsNull(component.Find("button.bit-cnp-fld").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitCountryPickerErrorMessageShouldWinTheLineOverTheMessagesOfTheEditContext()
    {
        var model = new ValidationModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderPicker(editContext, model, parameters =>
        {
            parameters.Add(p => p.ShowValidationMessage, true);
            parameters.Add(p => p.ErrorMessage, "We do not ship there yet.");
        });

        component.InvokeAsync(() => editContext.Validate());

        Assert.ContainsSingle(component.FindAll(".bit-cnp-erm"));
        Assert.AreEqual("We do not ship there yet.", component.Find(".bit-cnp-erm").TextContent.Trim());

        // Once the app takes its message back, the EditContext's takes the line.
        component.Render(parameters => parameters.Add(p => p.ErrorMessage, null));

        Assert.AreEqual(RequiredMessage, component.Find(".bit-cnp-erm").TextContent.Trim());
        Assert.AreEqual(component.Find(".bit-cnp-erm").Id, component.Find("button.bit-cnp-fld").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitCountryPickerErrorMessageTemplateShouldWinTheLineOverTheMessagesOfTheEditContext()
    {
        var model = new ValidationModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderPicker(editContext, model, parameters =>
        {
            parameters.Add(p => p.ShowValidationMessage, true);
            parameters.Add(p => p.ErrorMessageTemplate, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "b");
                builder.AddContent(1, "Not shipped");
                builder.CloseElement();
            }));
        });

        component.InvokeAsync(() => editContext.Validate());

        Assert.AreEqual("Not shipped", component.Find(".bit-cnp-erm b").TextContent);
        Assert.DoesNotContain(RequiredMessage, component.Find(".bit-cnp-erm").TextContent);
    }

    [TestMethod]
    public void BitCountryPickerShouldNotAnnounceTheMessagesOfTheEditContext()
    {
        var model = new ValidationModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderPicker(editContext, model, parameters => parameters.Add(p => p.ShowValidationMessage, true));

        component.InvokeAsync(() => editContext.Validate());

        // A submit validates every field at once; the field that takes the focus says what is wrong with it instead.
        Assert.AreEqual(RequiredMessage, component.Find(".bit-cnp-erm").TextContent.Trim());
        Assert.DoesNotContain(RequiredMessage, component.Find(".bit-cnp-lvr").TextContent);
    }

    [TestMethod]
    public void BitCountryPickerShouldDescribeTheFieldWithTheMessageAheadOfTheDescription()
    {
        var model = new ValidationModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderPicker(editContext, model, parameters =>
        {
            parameters.Add(p => p.ShowValidationMessage, true);
            parameters.Add(p => p.Description, "Where the card is registered.");
            parameters.Add(p => p.InputHtmlAttributes, new Dictionary<string, object> { ["aria-describedby"] = "page-hint" });
        });

        component.InvokeAsync(() => editContext.Validate());

        // What the page pointed the field at stays first, then what is wrong with the value, then the description.
        var ids = component.Find("button.bit-cnp-fld").GetAttribute("aria-describedby")!.Split(' ');

        Assert.HasCount(3, ids);
        Assert.AreEqual("page-hint", ids[0]);
        Assert.AreEqual(component.Find(".bit-cnp-erm").Id, ids[1]);
        Assert.AreEqual(component.Find(".bit-cnp-des").Id, ids[2]);
    }

    [TestMethod]
    public void BitCountryPickerThatStopsValidatingShouldTakeTheMessageAway()
    {
        var model = new ValidationModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderPicker(editContext, model, parameters =>
        {
            parameters.Add(p => p.ShowValidationMessage, true);
            parameters.Add(p => p.NoValidate, false);
        });

        component.InvokeAsync(() => editContext.Validate());

        Assert.ContainsSingle(component.FindAll(".bit-cnp-erm"));

        component.Render(parameters => parameters.Add(p => p.NoValidate, true));

        Assert.IsEmpty(component.FindAll(".bit-cnp-erm"));
        Assert.IsNull(component.Find("button.bit-cnp-fld").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitCountryPickerBoundToNothingShouldShowNoMessage()
    {
        var model = new ValidationModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderPicker(editContext, model, parameters => parameters.Add(p => p.ShowValidationMessage, true), bound: false);

        component.InvokeAsync(() => editContext.Validate());

        // The model has a message, but the picker has no member of it the message could belong to.
        Assert.IsNotEmpty(editContext.GetValidationMessages());
        Assert.IsEmpty(component.FindAll(".bit-cnp-erm"));
    }

    [TestMethod]
    public void BitCountryPickerShouldShowTheMessagesWhenACascadeAsksForThem()
    {
        var model = new ValidationModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        // One BitParams around a form turns the messages on for every country picker in it.
        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.AddCascadingValue(editContext);
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitCountryPickerParams { ShowValidationMessage = true } });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitCountryPicker>(0);
                builder.AddAttribute(1, nameof(BitCountryPicker.Countries), FiveCountries);
                builder.AddAttribute(2, nameof(BitCountryPicker.Value), model.Country);
                builder.AddAttribute(3, nameof(BitCountryPicker.ValueChanged), EventCallback.Factory.Create<BitCountry?>(this, c => model.Country = c));
                builder.AddAttribute(4, nameof(BitCountryPicker.ValueExpression), (Expression<Func<BitCountry?>>)(() => model.Country));
                builder.CloseComponent();
            });
        });

        component.InvokeAsync(() => editContext.Validate());

        Assert.AreEqual(RequiredMessage, component.Find(".bit-cnp-erm").TextContent.Trim());
    }

    private IRenderedComponent<BitCountryPicker> RenderPicker(EditContext editContext, ValidationModel model,
                                                              Action<ComponentParameterCollectionBuilder<BitCountryPicker>>? configure = null,
                                                              bool bound = true)
    {
        return RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.AddCascadingValue(editContext);
            parameters.Add(p => p.Countries, FiveCountries);

            if (bound)
            {
                parameters.Add(p => p.Value, model.Country);
                parameters.Add(p => p.ValueChanged, (BitCountry? c) => model.Country = c);
                parameters.Add(p => p.ValueExpression, () => model.Country);
            }

            configure?.Invoke(parameters);
        });
    }

    private class ValidationModel
    {
        [Required]
        public BitCountry? Country { get; set; }
    }
}
