using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bit.BlazorUI.Tests.Components.Inputs.TextField;
using Bit.BlazorUI.Tests.Components.Inputs.TimePicker;
using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs;

/// <summary>
/// Pins the classes an input takes from the cascading EditContext onto its root, as every input of the framework
/// carries them (<c>EditContext.FieldCssClass</c>): <c>valid</c> or <c>invalid</c>, with <c>modified</c> once the field
/// has changed, or whatever the app's <see cref="FieldCssClassProvider"/> names instead - and none where the input has
/// no field in a form. Through BitTextField; every input takes them from BitInputBase.
/// </summary>
[TestClass]
public class BitInputBaseFieldCssClassTests : BunitTestContext
{
    private static readonly string[] FrameworkClasses = ["valid", "invalid", "modified"];

    private IRenderedComponent<BitTextField> RenderBoundTextField(EditContext editContext, BitTextFieldTestModel model, bool noValidate = false)
    {
        return RenderComponent<BitTextField>(parameters =>
        {
            parameters.AddCascadingValue(editContext);
            parameters.Add(p => p.Value, model.Value);
            parameters.Add(p => p.ValueChanged, (string? v) => model.Value = v);
            parameters.Add(p => p.ValueExpression, () => model.Value);
            parameters.Add(p => p.NoValidate, noValidate);
        });
    }

    // The framework's classes on the root, in a set order, whatever order the provider wrote them in.
    private static string[] FieldClasses(IElement root) => [.. root.ClassList.Where(FrameworkClasses.Contains).Order(StringComparer.Ordinal)];

    [TestMethod]
    public void TheRootShouldCarryTheClassesTheEditContextGivesTheField()
    {
        var model = new BitTextFieldTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderBoundTextField(editContext, model);

        // Untouched and not validated yet, the field is valid, as a framework input starts out.
        Assert.AreSequenceEqual(new[] { "valid" }, FieldClasses(component.Find(".bit-tfl")));

        component.Find("input").Change("not an email");

        Assert.AreSequenceEqual(new[] { "invalid", "modified" }, FieldClasses(component.Find(".bit-tfl")));
        Assert.IsTrue(component.Find(".bit-tfl").ClassList.Contains("bit-inv"));

        component.Find("input").Change("ada@bit.com");

        Assert.AreSequenceEqual(new[] { "modified", "valid" }, FieldClasses(component.Find(".bit-tfl")));
        Assert.IsFalse(component.Find(".bit-tfl").ClassList.Contains("bit-inv"));
    }

    [TestMethod]
    public async Task AValidationOfTheFormShouldMarkAnUntouchedFieldInvalid()
    {
        var model = new BitTextFieldTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderBoundTextField(editContext, model);

        // A submit with the required field left empty: invalid, but not modified, since nobody changed it.
        await component.InvokeAsync(() => editContext.Validate());

        Assert.AreSequenceEqual(new[] { "invalid" }, FieldClasses(component.Find(".bit-tfl")));
    }

    [TestMethod]
    public void AChangeShouldMarkTheFieldModifiedInAFormThatValidatesNothing()
    {
        var model = new BitTextFieldTestModel();

        // No validator, so the change raises no validation event: the input marks the field from its own report.
        var component = RenderBoundTextField(new EditContext(model), model);

        component.Find("input").Change("anything");

        Assert.AreSequenceEqual(new[] { "modified", "valid" }, FieldClasses(component.Find(".bit-tfl")));
    }

    [TestMethod]
    public void AFieldMarkedUnmodifiedShouldLoseTheClassOnTheNextParametersPass()
    {
        var model = new BitTextFieldTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderBoundTextField(editContext, model);

        component.Find("input").Change("ada@bit.com");

        Assert.AreSequenceEqual(new[] { "modified", "valid" }, FieldClasses(component.Find(".bit-tfl")));

        // MarkAsUnmodified raises no event, so it shows once the form around the input renders it again - as it shows
        // on a framework input's next render.
        editContext.MarkAsUnmodified();
        component.Render(parameters => parameters.Add(p => p.Value, model.Value));

        Assert.AreSequenceEqual(new[] { "valid" }, FieldClasses(component.Find(".bit-tfl")));
    }

    [TestMethod]
    public void AFieldMarkedUnmodifiedShouldLoseTheClassOnARenderOfTheInputAlone()
    {
        var model = new BitTextFieldTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderBoundTextField(editContext, model);

        component.Find("input").Change("ada@bit.com");

        Assert.AreSequenceEqual(new[] { "modified", "valid" }, FieldClasses(component.Find(".bit-tfl")));

        // No parameters pass: the input re-renders on an event of its own, and reads the field as it is then.
        editContext.MarkAsUnmodified();
        component.Find("input").FocusIn();

        Assert.AreSequenceEqual(new[] { "valid" }, FieldClasses(component.Find(".bit-tfl")));
    }

    [TestMethod]
    public void TurningNoValidateOnShouldTakeTheClassesOff()
    {
        var model = new BitTextFieldTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderBoundTextField(editContext, model);

        component.Find("input").Change("ada@bit.com");

        Assert.AreSequenceEqual(new[] { "modified", "valid" }, FieldClasses(component.Find(".bit-tfl")));

        component.Render(parameters => parameters.Add(p => p.NoValidate, true));

        Assert.IsEmpty(FieldClasses(component.Find(".bit-tfl")));

        // And they come back with the validation.
        component.Render(parameters => parameters.Add(p => p.NoValidate, false));

        Assert.AreSequenceEqual(new[] { "modified", "valid" }, FieldClasses(component.Find(".bit-tfl")));
    }

    [TestMethod]
    public void TheRootShouldCarryTheClassesOfTheAppsFieldCssClassProvider()
    {
        var model = new BitTextFieldTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);
        editContext.SetFieldCssClassProvider(new BootstrapFieldCssClassProvider());

        var component = RenderBoundTextField(editContext, model);

        Assert.IsTrue(component.Find(".bit-tfl").ClassList.Contains("is-valid"));

        component.Find("input").Change("not an email");

        // The app's names replace the framework's, as they do on the framework's inputs.
        var root = component.Find(".bit-tfl");
        Assert.IsTrue(root.ClassList.Contains("is-invalid"));
        Assert.IsFalse(root.ClassList.Contains("is-valid"));
        Assert.IsEmpty(FieldClasses(root));
    }

    [TestMethod]
    public void AFailedParseShouldMarkTheFieldModifiedAndInvalid()
    {
        var model = new BitTimePickerTestModel();
        var editContext = new EditContext(model);

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.AddCascadingValue(editContext);
            parameters.Add(p => p.Value, model.Time);
            parameters.Add(p => p.ValueChanged, (TimeSpan? v) => model.Time = v);
            parameters.Add(p => p.ValueExpression, () => model.Time);
            parameters.Add(p => p.AllowTextInput, true);
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
        });

        // The message of a failed parse is the input's own, with no validator involved.
        component.Find(".bit-tpc-inp").Input("nonsense");

        Assert.AreSequenceEqual(new[] { "invalid", "modified" }, FieldClasses(component.Find(".bit-tpc")));

        component.Find(".bit-tpc-inp").Input("10:30");

        Assert.AreSequenceEqual(new[] { "modified", "valid" }, FieldClasses(component.Find(".bit-tpc")));
    }

    [TestMethod]
    public void AnInputWithNoFieldInAFormShouldCarryNoneOfTheClasses()
    {
        var model = new BitTextFieldTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        // Outside any form.
        var outside = RenderComponent<BitTextField>(parameters =>
        {
            parameters.Add(p => p.Value, model.Value);
            parameters.Add(p => p.ValueChanged, (string? v) => model.Value = v);
            parameters.Add(p => p.ValueExpression, () => model.Value);
        });

        // In a form, bound to nothing: there is no field to look up, and the lookup must not throw.
        var unbound = RenderComponent<BitTextField>(parameters =>
        {
            parameters.AddCascadingValue(editContext);
        });

        // In a form, opted out of it.
        var noValidate = RenderBoundTextField(editContext, model, noValidate: true);

        foreach (var component in new[] { outside, unbound, noValidate })
        {
            component.Find("input").Change("not an email");

            Assert.IsEmpty(FieldClasses(component.Find(".bit-tfl")));
        }
    }

    // The names a Bootstrap app hands the framework's inputs.
    private sealed class BootstrapFieldCssClassProvider : FieldCssClassProvider
    {
        public override string GetFieldCssClass(EditContext editContext, in FieldIdentifier fieldIdentifier)
            => editContext.IsValid(fieldIdentifier) ? "is-valid" : "is-invalid";
    }
}
