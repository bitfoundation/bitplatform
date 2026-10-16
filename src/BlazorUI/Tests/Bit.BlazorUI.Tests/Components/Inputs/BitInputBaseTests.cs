using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Bit.BlazorUI.Tests.Components.Inputs.TextField;
using Bit.BlazorUI.Tests.Components.Inputs.TimePicker;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs;

/// <summary>
/// Pins the members every input inherits from BitInputBase, through the two inputs that exercise them: BitTextField
/// (a string, which never fails to parse) and BitTimePicker (a time typed as text, which does). The value and the
/// default value, the binding that refuses a change, the change callbacks and their order, the EditContext the input
/// reports to and the one it must never swap, the message a failed parse adds, and what the input leaves behind on
/// dispose.
/// </summary>
[TestClass]
public class BitInputBaseTests : BunitTestContext
{
    private IRenderedComponent<CascadingValue<EditContext>> RenderInContext(EditContext editContext, Action<Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder> content)
    {
        return RenderComponent<CascadingValue<EditContext>>(parameters =>
        {
            parameters.Add(p => p.Value, editContext);
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder => content(builder)));
        });
    }

    private void AddBoundTextField(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder, BitTextFieldTestModel model, bool noValidate = false)
    {
        builder.OpenComponent<BitTextField>(0);
        builder.AddAttribute(1, nameof(BitTextField.Value), model.Value);
        builder.AddAttribute(2, nameof(BitTextField.ValueChanged), EventCallback.Factory.Create<string?>(this, v => model.Value = v));
        builder.AddAttribute(3, nameof(BitTextField.ValueExpression), (Expression<Func<string?>>)(() => model.Value));
        builder.AddAttribute(4, nameof(BitTextField.NoValidate), noValidate);
        builder.CloseComponent();
    }

    private void AddBoundTimePicker(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder, BitTimePickerTestModel model, string? displayName = null, bool noValidate = false)
    {
        builder.OpenComponent<BitTimePicker>(0);
        builder.AddAttribute(1, nameof(BitTimePicker.Value), model.Time);
        builder.AddAttribute(2, nameof(BitTimePicker.ValueChanged), EventCallback.Factory.Create<TimeSpan?>(this, v => model.Time = v));
        builder.AddAttribute(3, nameof(BitTimePicker.ValueExpression), (Expression<Func<TimeSpan?>>)(() => model.Time));
        builder.AddAttribute(4, nameof(BitTimePicker.AllowTextInput), true);
        builder.AddAttribute(5, nameof(BitTimePicker.Culture), CultureInfo.InvariantCulture);
        builder.AddAttribute(6, nameof(BitTimePicker.DisplayName), displayName);
        builder.AddAttribute(7, nameof(BitTimePicker.NoValidate), noValidate);
        builder.CloseComponent();
    }

    [TestMethod]
    public void TheValueShouldWinOverTheDefaultValue()
    {
        var component = RenderComponent<BitTextField>(parameters =>
        {
            parameters.Add(p => p.Value, "bound");
            parameters.Add(p => p.DefaultValue, "default");
        });

        // The default is for the uncontrolled mode alone: a Value, even one passed one way, is the app's.
        Assert.AreEqual("bound", component.Instance.Value);
        Assert.AreEqual("bound", component.Find("input").GetAttribute("value"));
    }

    [TestMethod]
    public void TheDefaultValueShouldOnlySeedAnUnboundInput()
    {
        var component = RenderComponent<BitTextField>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, "default");
        });

        Assert.AreEqual("default", component.Instance.Value);

        // Once seeded the input holds its own value; a changed default is not a new value.
        component.Render(parameters => parameters.Add(p => p.DefaultValue, "another"));

        Assert.AreEqual("default", component.Instance.Value);
    }

    [TestMethod]
    public void AnEmptyStringShouldBecomeNullForANullableValue()
    {
        string? changed = "unset";

        var component = RenderComponent<BitTextField>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, "hello");
            parameters.Add(p => p.OnChange, (string? v) => changed = v);
        });

        component.Find("input").Change(string.Empty);

        // A nullable type reads an emptied input as its default rather than as an empty string.
        Assert.IsNull(component.Instance.Value);
        Assert.IsNull(changed);
    }

    [TestMethod]
    public void ValueChangedShouldBeRaisedBeforeOnChange()
    {
        var log = new List<string>();

        var component = RenderComponent<BitTextField>(parameters =>
        {
            parameters.Add(p => p.ValueChanged, (string? v) => log.Add($"bound:{v}"));
            parameters.Add(p => p.OnChange, (string? v) => log.Add($"change:{v}"));
        });

        component.Find("input").Change("typed");

        // The binding is written first, so an OnChange handler reads a model that already holds the value.
        CollectionAssert.AreEqual(new[] { "bound:typed", "change:typed" }, log);
        Assert.AreEqual("typed", component.Instance.Value);
    }

    [TestMethod]
    public void AValueWithOnChangeAloneShouldAcceptTheChange()
    {
        string? changed = null;

        var component = RenderComponent<BitTextField>(parameters =>
        {
            parameters.Add(p => p.Value, "a");
            parameters.Add(p => p.OnChange, (string? v) => changed = v);
        });

        component.Find("input").Change("b");

        // OnChange is as good a way to report a change as ValueChanged, so the Value beside it is writable.
        Assert.AreEqual("b", changed);
        Assert.AreEqual("b", component.Instance.Value);
    }

    [TestMethod]
    public void AChangeEventCarryingTheSameTextShouldStillBeReported()
    {
        var changes = 0;

        var component = RenderComponent<BitTextField>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, "same");
            parameters.Add(p => p.OnChange, (string? _) => changes++);
        });

        // The string path reports what the change event committed, whether or not it moved the value: the event is
        // the browser's word that the user finished an edit, and only the CurrentValue path compares before it reports.
        component.Find("input").Change("same");

        Assert.AreEqual(1, changes);
    }

    [TestMethod]
    public void TheCurrentValuePathShouldNotReportTheSameValueAgain()
    {
        var changes = 0;
        var selections = 0;

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.TimeFormat, BitTimeFormat.TwelveHours);
            parameters.Add(p => p.DefaultValue, new TimeSpan(13, 30, 0));
            parameters.Add(p => p.OnChange, (TimeSpan? _) => changes++);
            parameters.Add(p => p.OnSelectTime, (TimeSpan? _) => selections++);
        });

        // Clicking the half of the day the time is already in computes the very same value, which the CurrentValue
        // setter drops before the binding and OnChange hear of it; the picker's own OnSelectTime still reports the pick.
        component.Find(".bit-tpc-bpm").Click();

        Assert.AreEqual(0, changes);
        Assert.AreEqual(1, selections);
        Assert.AreEqual(new TimeSpan(13, 30, 0), component.Instance.Value);
    }

    [TestMethod]
    public void ABoundInputShouldTellTheEditContextOfItsChange()
    {
        var model = new BitTextFieldTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderInContext(editContext, builder => AddBoundTextField(builder, model));

        component.Find("input").Change("not an email");

        // The field is validated and marked modified by the change itself, no submit needed.
        Assert.IsTrue(editContext.IsModified(editContext.Field(nameof(BitTextFieldTestModel.Value))));
        Assert.IsTrue(editContext.GetValidationMessages().Any());
        Assert.IsTrue(component.Find(".bit-tfl").ClassList.Contains("bit-inv"));
        Assert.AreEqual("true", component.Find("input").GetAttribute("aria-invalid"));

        component.Find("input").Change("ada@bit.com");

        Assert.IsFalse(editContext.GetValidationMessages().Any());
        Assert.IsFalse(component.Find(".bit-tfl").ClassList.Contains("bit-inv"));
        Assert.IsFalse(component.Find("input").HasAttribute("aria-invalid"));
    }

    [TestMethod]
    public void NoValidateShouldKeepTheInputOutOfTheEditContext()
    {
        var model = new BitTextFieldTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderInContext(editContext, builder => AddBoundTextField(builder, model, noValidate: true));

        component.Find("input").Change("not an email");

        // The value is still bound; only the EditContext hears nothing of it, and a validation of the whole form
        // leaves the input unmarked as well.
        Assert.AreEqual("not an email", model.Value);
        Assert.IsFalse(editContext.IsModified());

        component.InvokeAsync(() => editContext.Validate());

        Assert.IsTrue(editContext.GetValidationMessages().Any());
        Assert.IsFalse(component.Find(".bit-tfl").ClassList.Contains("bit-inv"));
        Assert.IsFalse(component.Find("input").HasAttribute("aria-invalid"));
    }

    [TestMethod]
    public void ChangingTheEditContextShouldThrow()
    {
        var model = new BitTextFieldTestModel();

        var component = RenderInContext(new EditContext(model), builder => AddBoundTextField(builder, model));

        Exception? thrown = null;

        try
        {
            component.Render(parameters => parameters.Add(p => p.Value, new EditContext(model)));
        }
        catch (Exception exception)
        {
            thrown = exception;
        }

        // Clearing up the state and the handlers of the previous context is messy and has no strong use case, so a
        // swapped context is refused outright rather than half honored.
        Assert.IsNotNull(thrown, "A swapped EditContext was accepted.");

        // The renderer may hand the exception over wrapped, so the one the input threw is looked for down the chain.
        InvalidOperationException? invalidOperation = null;

        for (var exception = thrown; exception is not null && invalidOperation is null; exception = exception.InnerException)
        {
            invalidOperation = exception as InvalidOperationException;
        }

        Assert.IsNotNull(invalidOperation, $"Expected an InvalidOperationException, got {thrown.GetType().Name}: {thrown.Message}");
        StringAssert.Contains(invalidOperation.Message, "does not support changing the EditContext dynamically.");
    }

    [TestMethod]
    public void AFailedParseShouldKeepTheTypedTextAndAddAMessageToTheField()
    {
        var model = new BitTimePickerTestModel();
        var editContext = new EditContext(model);

        var component = RenderInContext(editContext, builder => AddBoundTimePicker(builder, model));

        component.Find(".bit-tpc-inp").Input("nonsense");

        // The rejected text stays on the input rather than being replaced by the value that did not come of it,
        // the field is marked modified and the message names the field after the model.
        Assert.IsNull(model.Time);
        Assert.AreEqual("nonsense", component.Find(".bit-tpc-inp").GetAttribute("value"));
        Assert.IsTrue(editContext.IsModified(editContext.Field(nameof(BitTimePickerTestModel.Time))));
        CollectionAssert.AreEqual(new[] { "The Time field is not valid." }, editContext.GetValidationMessages().ToArray());
        Assert.IsTrue(component.Find(".bit-tpc").ClassList.Contains("bit-inv"));
        Assert.AreEqual("true", component.Find(".bit-tpc-inp").GetAttribute("aria-invalid"));

        // A parse that succeeds takes the message away again.
        component.Find(".bit-tpc-inp").Input("10:30");

        Assert.AreEqual(new TimeSpan(10, 30, 0), model.Time);
        Assert.IsFalse(editContext.GetValidationMessages().Any());
        Assert.IsFalse(component.Find(".bit-tpc").ClassList.Contains("bit-inv"));
    }

    [TestMethod]
    public void TheDisplayNameShouldNameTheFieldInTheParsingMessage()
    {
        var model = new BitTimePickerTestModel();
        var editContext = new EditContext(model);

        var component = RenderInContext(editContext, builder => AddBoundTimePicker(builder, model, displayName: "Start"));

        component.Find(".bit-tpc-inp").Input("nonsense");

        CollectionAssert.AreEqual(new[] { "The Start field is not valid." }, editContext.GetValidationMessages().ToArray());
    }

    [TestMethod]
    public void NoValidateShouldDropTheParsingMessage()
    {
        var model = new BitTimePickerTestModel();
        var editContext = new EditContext(model);

        var component = RenderInContext(editContext, builder => AddBoundTimePicker(builder, model, noValidate: true));

        component.Find(".bit-tpc-inp").Input("nonsense");

        // Nothing is parsed into the value, and nothing is said about it either.
        Assert.IsNull(model.Time);
        Assert.IsFalse(editContext.GetValidationMessages().Any());
        Assert.IsFalse(editContext.IsModified());
        Assert.IsFalse(component.Find(".bit-tpc").ClassList.Contains("bit-inv"));
        Assert.IsFalse(component.Find(".bit-tpc-inp").HasAttribute("aria-invalid"));
    }

    [TestMethod]
    public void AFailedParseOfAnInputBoundToNothingShouldReportNothingToTheForm()
    {
        var model = new BitTimePickerTestModel();
        var editContext = new EditContext(model);
        TimeSpan? changed = null;

        var component = RenderInContext(editContext, builder =>
        {
            builder.OpenComponent<BitTimePicker>(0);
            builder.AddAttribute(1, nameof(BitTimePicker.AllowTextInput), true);
            builder.AddAttribute(2, nameof(BitTimePicker.Culture), CultureInfo.InvariantCulture);
            builder.AddAttribute(3, nameof(BitTimePicker.OnChange), EventCallback.Factory.Create<TimeSpan?>(this, v => changed = v));
            builder.CloseComponent();
        });

        // An input bound to nothing has no field the message could be held under, so the form is told nothing
        // rather than handed a field it cannot even look up; the text is still kept on the input.
        component.Find(".bit-tpc-inp").Input("nonsense");

        Assert.AreEqual("nonsense", component.Find(".bit-tpc-inp").GetAttribute("value"));
        Assert.IsFalse(editContext.GetValidationMessages().Any());
        Assert.IsFalse(editContext.IsModified());
        Assert.IsFalse(component.Find(".bit-tpc").ClassList.Contains("bit-inv"));

        // ... and a value that does parse is still taken and reported.
        component.Find(".bit-tpc-inp").Input("10:30");

        Assert.AreEqual(new TimeSpan(10, 30, 0), changed);
    }

    [TestMethod]
    public void RequiredAndReadOnlyChangesShouldRebuildTheClasses()
    {
        var component = RenderComponent<BitTextField>(parameters =>
        {
            parameters.Add(p => p.Label, "A label");
        });

        var root = component.Find(".bit-tfl");

        Assert.IsFalse(root.ClassList.Contains("bit-tfl-req"));
        Assert.IsFalse(root.ClassList.Contains("bit-tfl-rdl"));

        // Both are taken out of the parameters before the component's own set sees them, so the base class is the
        // one resetting the classes they decide when they move.
        component.Render(parameters =>
        {
            parameters.Add(p => p.Required, true);
            parameters.Add(p => p.ReadOnly, true);
        });

        root = component.Find(".bit-tfl");

        Assert.IsTrue(root.ClassList.Contains("bit-tfl-req"));
        Assert.IsTrue(root.ClassList.Contains("bit-tfl-rdl"));
        Assert.IsTrue(component.Find("input").HasAttribute("required"));
        Assert.IsTrue(component.Find("input").HasAttribute("readonly"));
    }

    [TestMethod]
    public async Task FocusAsyncShouldFocusTheInputElement()
    {
        var component = RenderComponent<BitTextField>();

        await component.InvokeAsync(() => component.Instance.FocusAsync().AsTask());
        await component.InvokeAsync(() => component.Instance.FocusAsync(true).AsTask());

        Assert.AreEqual(2, Context.JSInterop.Invocations.Count(i => i.Identifier == "Blazor._internal.domWrapper.focus"));
    }

    [TestMethod]
    public async Task DisposeShouldUnsubscribeFromTheEditContext()
    {
        var model = new BitTextFieldTestModel();
        var editContext = new EditContext(model);
        var messages = new ValidationMessageStore(editContext);

        RenderInContext(editContext, builder => AddBoundTextField(builder, model));

        await Context.DisposeComponentsAsync();

        // A disposed input no longer listens, so a validation of the form it sat in reaches no dead component.
        messages.Add(editContext.Field(nameof(BitTextFieldTestModel.Value)), "Invalid");
        editContext.NotifyValidationStateChanged();
    }
}
