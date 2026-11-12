using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.TimeZonePicker;

/// <summary>
/// Covers ShowValidationMessage: the messages the cascading EditContext holds for the field shown in the error line
/// under the field and referenced by it, giving way to the app's own ErrorMessage, and never announced.
/// </summary>
[TestClass]
public class BitTimeZonePickerValidationMessageTests : BunitTestContext
{
    private const string RequiredMessage = "The TimeZoneId field is required.";

    // A moment without daylight saving time in the northern hemisphere, so the list reads the same whenever the tests run.
    private static readonly DateTimeOffset January = new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    private static readonly List<BitTimeZone> ThreeTimeZones =
    [
        BitTimeZones.Find("America/New_York")!,
        BitTimeZones.Find("Europe/Berlin")!,
        BitTimeZones.Find("Asia/Tokyo")!,
    ];

    [TestMethod]
    public void BitTimeZonePickerShowValidationMessageShouldShowTheMessageOfTheFormUnderTheField()
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

                builder.OpenComponent<BitTimeZonePicker<string>>(1);
                builder.AddAttribute(2, nameof(BitTimeZonePicker<string>.TimeZones), ThreeTimeZones);
                builder.AddAttribute(3, nameof(BitTimeZonePicker<string>.ReferenceDate), (DateTimeOffset?)January);
                builder.AddAttribute(4, nameof(BitTimeZonePicker<string>.ShowValidationMessage), true);
                builder.AddAttribute(5, nameof(BitTimeZonePicker<string>.Value), model.TimeZoneId);
                builder.AddAttribute(6, nameof(BitTimeZonePicker<string>.ValueChanged), EventCallback.Factory.Create<string?>(this, v => model.TimeZoneId = v));
                builder.AddAttribute(7, nameof(BitTimeZonePicker<string>.ValueExpression), (Expression<Func<string?>>)(() => model.TimeZoneId));
                builder.CloseComponent();
            }));
        });

        // Nothing is shown before the form has anything to say.
        Assert.IsEmpty(component.FindAll(".bit-tzp-erm"));
        Assert.IsNull(component.Find("button.bit-tzp-fld").GetAttribute("aria-describedby"));

        component.Find("form").Submit();

        component.WaitForAssertion(() =>
        {
            var field = component.Find("button.bit-tzp-fld");
            var error = component.Find(".bit-tzp-erm");

            Assert.AreEqual(1, invalidCount);
            Assert.AreEqual(RequiredMessage, error.TextContent.Trim());
            Assert.AreEqual(error.Id, field.GetAttribute("aria-describedby"));
            Assert.AreEqual("true", field.GetAttribute("aria-invalid"));
        });

        // A pick the form accepts takes the line, and the reference to it, away again.
        component.Find("button.bit-tzp-fld").Click();
        component.FindAll("button.bit-tzp-itm").Single(i => i.QuerySelector(".bit-tzp-inm")!.TextContent == "Berlin").Click();

        Assert.AreEqual("Europe/Berlin", model.TimeZoneId);

        component.WaitForAssertion(() =>
        {
            Assert.IsEmpty(component.FindAll(".bit-tzp-erm"));
            Assert.IsNull(component.Find("button.bit-tzp-fld").GetAttribute("aria-describedby"));
        });

        component.Find("form").Submit();

        Assert.AreEqual(1, validCount);
    }

    [TestMethod]
    public void BitTimeZonePickerShowValidationMessageShouldFollowTheMessagesWhileTheFieldStaysInvalid()
    {
        var model = new ValidationModel();
        var editContext = new EditContext(model);
        var field = new FieldIdentifier(model, nameof(ValidationModel.TimeZoneId));
        var store = new ValidationMessageStore(editContext);

        var component = RenderPicker(editContext, model, parameters => parameters.Add(p => p.ShowValidationMessage, true));

        component.InvokeAsync(() =>
        {
            store.Add(field, "We have no office there yet.");
            editContext.NotifyValidationStateChanged();
        });

        Assert.AreEqual("We have no office there yet.", component.Find(".bit-tzp-erm").TextContent.Trim());

        // Every message the field holds is shown, in the order they were added, as the ValidationMessage component
        // lists them.
        component.InvokeAsync(() =>
        {
            store.Clear(field);
            store.Add(field, "Outside our support hours.");
            store.Add(field, "Ask your manager.");
            editContext.NotifyValidationStateChanged();
        });

        Assert.AreEqual("Outside our support hours. Ask your manager.", component.Find(".bit-tzp-erm").TextContent.Trim());
    }

    [TestMethod]
    public void BitTimeZonePickerWithoutShowValidationMessageShouldLeaveTheMessagesToTheValidationMessageComponent()
    {
        var model = new ValidationModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderPicker(editContext, model);

        component.InvokeAsync(() => editContext.Validate());

        // The field is marked as before, and the message stays where the page puts it.
        Assert.AreEqual("true", component.Find("button.bit-tzp-fld").GetAttribute("aria-invalid"));
        Assert.IsEmpty(component.FindAll(".bit-tzp-erm"));
        Assert.IsNull(component.Find("button.bit-tzp-fld").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitTimeZonePickerErrorMessageShouldWinTheLineOverTheMessagesOfTheEditContext()
    {
        var model = new ValidationModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderPicker(editContext, model, parameters =>
        {
            parameters.Add(p => p.ShowValidationMessage, true);
            parameters.Add(p => p.ErrorMessage, "We have no office there yet.");
        });

        component.InvokeAsync(() => editContext.Validate());

        Assert.ContainsSingle(component.FindAll(".bit-tzp-erm"));
        Assert.AreEqual("We have no office there yet.", component.Find(".bit-tzp-erm").TextContent.Trim());

        // Once the app takes its message back, the EditContext's takes the line.
        component.Render(parameters => parameters.Add(p => p.ErrorMessage, null));

        Assert.AreEqual(RequiredMessage, component.Find(".bit-tzp-erm").TextContent.Trim());
        Assert.AreEqual(component.Find(".bit-tzp-erm").Id, component.Find("button.bit-tzp-fld").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitTimeZonePickerErrorMessageTemplateShouldWinTheLineOverTheMessagesOfTheEditContext()
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
                builder.AddContent(1, "No office");
                builder.CloseElement();
            }));
        });

        component.InvokeAsync(() => editContext.Validate());

        Assert.AreEqual("No office", component.Find(".bit-tzp-erm b").TextContent);
        Assert.DoesNotContain(RequiredMessage, component.Find(".bit-tzp-erm").TextContent);
    }

    [TestMethod]
    public void BitTimeZonePickerShouldNotAnnounceTheMessagesOfTheEditContext()
    {
        var model = new ValidationModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderPicker(editContext, model, parameters => parameters.Add(p => p.ShowValidationMessage, true));

        component.InvokeAsync(() => editContext.Validate());

        // A submit validates every field at once; the field that takes the focus says what is wrong with it instead.
        Assert.AreEqual(RequiredMessage, component.Find(".bit-tzp-erm").TextContent.Trim());
        Assert.DoesNotContain(RequiredMessage, component.Find(".bit-tzp-lvr").TextContent);
    }

    [TestMethod]
    public void BitTimeZonePickerShouldDescribeTheFieldWithTheMessageAheadOfTheDescription()
    {
        var model = new ValidationModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderPicker(editContext, model, parameters =>
        {
            parameters.Add(p => p.ShowValidationMessage, true);
            parameters.Add(p => p.Description, "Meetings are scheduled in this time zone.");
            parameters.Add(p => p.InputHtmlAttributes, new Dictionary<string, object> { ["aria-describedby"] = "page-hint" });
        });

        component.InvokeAsync(() => editContext.Validate());

        // What the page pointed the field at stays first, then what is wrong with the value, then the description.
        var ids = component.Find("button.bit-tzp-fld").GetAttribute("aria-describedby")!.Split(' ');

        Assert.HasCount(3, ids);
        Assert.AreEqual("page-hint", ids[0]);
        Assert.AreEqual(component.Find(".bit-tzp-erm").Id, ids[1]);
        Assert.AreEqual(component.Find(".bit-tzp-des").Id, ids[2]);
    }

    [TestMethod]
    public void BitTimeZonePickerThatStopsValidatingShouldTakeTheMessageAway()
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

        Assert.ContainsSingle(component.FindAll(".bit-tzp-erm"));

        component.Render(parameters => parameters.Add(p => p.NoValidate, true));

        Assert.IsEmpty(component.FindAll(".bit-tzp-erm"));
        Assert.IsNull(component.Find("button.bit-tzp-fld").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitTimeZonePickerBoundToNothingShouldShowNoMessage()
    {
        var model = new ValidationModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderPicker(editContext, model, parameters => parameters.Add(p => p.ShowValidationMessage, true), bound: false);

        component.InvokeAsync(() => editContext.Validate());

        // The model has a message, but the picker has no member of it the message could belong to.
        Assert.IsNotEmpty(editContext.GetValidationMessages());
        Assert.IsEmpty(component.FindAll(".bit-tzp-erm"));
    }

    [TestMethod]
    public void BitTimeZonePickerOfTimeZoneInfoShouldShowTheMessageOfTheEditContext()
    {
        var model = new TimeZoneInfoModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        // The picker holds any of the value types it takes; the message is the field's, whatever its type.
        var component = RenderComponent<BitTimeZonePicker<TimeZoneInfo>>(parameters =>
        {
            parameters.AddCascadingValue(editContext);
            parameters.Add(p => p.TimeZones, ThreeTimeZones);
            parameters.Add(p => p.ReferenceDate, January);
            parameters.Add(p => p.ShowValidationMessage, true);
            parameters.Add(p => p.Value, model.Zone);
            parameters.Add(p => p.ValueChanged, (TimeZoneInfo v) => model.Zone = v);
            parameters.Add(p => p.ValueExpression, () => model.Zone!);
        });

        component.InvokeAsync(() => editContext.Validate());

        Assert.AreEqual("The Zone field is required.", component.Find(".bit-tzp-erm").TextContent.Trim());
        Assert.AreEqual(component.Find(".bit-tzp-erm").Id, component.Find("button.bit-tzp-fld").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitTimeZonePickerShouldShowTheMessagesWhenACascadeAsksForThem()
    {
        var model = new ValidationModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        // One BitParams around a form turns the messages on for every time zone picker in it.
        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.AddCascadingValue(editContext);
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitTimeZonePickerParams { ShowValidationMessage = true } });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitTimeZonePicker<string>>(0);
                builder.AddAttribute(1, nameof(BitTimeZonePicker<string>.TimeZones), ThreeTimeZones);
                builder.AddAttribute(2, nameof(BitTimeZonePicker<string>.Value), model.TimeZoneId);
                builder.AddAttribute(3, nameof(BitTimeZonePicker<string>.ValueChanged), EventCallback.Factory.Create<string?>(this, v => model.TimeZoneId = v));
                builder.AddAttribute(4, nameof(BitTimeZonePicker<string>.ValueExpression), (Expression<Func<string?>>)(() => model.TimeZoneId));
                builder.CloseComponent();
            });
        });

        component.InvokeAsync(() => editContext.Validate());

        Assert.AreEqual(RequiredMessage, component.Find(".bit-tzp-erm").TextContent.Trim());
    }

    private IRenderedComponent<BitTimeZonePicker<string>> RenderPicker(EditContext editContext, ValidationModel model,
                                                                        Action<ComponentParameterCollectionBuilder<BitTimeZonePicker<string>>>? configure = null,
                                                                        bool bound = true)
    {
        return RenderComponent<BitTimeZonePicker<string>>(parameters =>
        {
            parameters.AddCascadingValue(editContext);
            parameters.Add(p => p.TimeZones, ThreeTimeZones);
            parameters.Add(p => p.ReferenceDate, January);

            if (bound)
            {
                parameters.Add(p => p.Value, model.TimeZoneId);
                parameters.Add(p => p.ValueChanged, (string v) => model.TimeZoneId = v);
                parameters.Add(p => p.ValueExpression, () => model.TimeZoneId!);
            }

            configure?.Invoke(parameters);
        });
    }

    private class ValidationModel
    {
        [Required]
        public string? TimeZoneId { get; set; }
    }

    private class TimeZoneInfoModel
    {
        [Required]
        public TimeZoneInfo? Zone { get; set; }
    }
}
