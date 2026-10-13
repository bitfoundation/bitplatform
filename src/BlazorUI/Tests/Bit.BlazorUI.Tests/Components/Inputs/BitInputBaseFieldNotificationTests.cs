using System.Linq;
using Bit.BlazorUI.Tests.Components.Inputs.TextField;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs;

/// <summary>
/// Pins what an input tells the cascading EditContext: an input bound to nothing has no field to report, so typing
/// into one inside a form validates nothing rather than handing the EditContext a field it cannot even look up.
/// </summary>
[TestClass]
public class BitInputBaseFieldNotificationTests : BunitTestContext
{
    [TestMethod]
    public void AnInputBoundToNothingShouldTellTheEditContextNothing()
    {
        var model = new BitTextFieldTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        string? changed = null;

        var component = RenderComponent<CascadingValue<EditContext>>(parameters =>
        {
            parameters.Add(p => p.Value, editContext);
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<BitTextField>(0);
                builder.AddAttribute(1, nameof(BitTextField.OnChange),
                                     EventCallback.Factory.Create<string?>(this, v => changed = v));
                builder.CloseComponent();
            }));
        });

        component.Find("input").Change("typed");

        // the value is still taken and reported; only the field it does not have is left out of the form.
        Assert.AreEqual("typed", changed);
        Assert.IsFalse(editContext.IsModified());
        Assert.IsFalse(editContext.GetValidationMessages().Any());
    }

    [TestMethod]
    public void AnInputBoundToNothingShouldSitThroughTheValidationOfItsForm()
    {
        var model = new BitTextFieldTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderComponent<CascadingValue<EditContext>>(parameters =>
        {
            parameters.Add(p => p.Value, editContext);
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<BitTextField>(0);
                builder.AddAttribute(1, nameof(BitTextField.Value), model.Value);
                builder.AddAttribute(2, nameof(BitTextField.ValueChanged),
                                     EventCallback.Factory.Create<string?>(this, v => model.Value = v));
                builder.AddAttribute(3, nameof(BitTextField.ValueExpression),
                                     (System.Linq.Expressions.Expression<System.Func<string?>>)(() => model.Value));
                builder.AddAttribute(4, "id", "bound");
                builder.CloseComponent();

                builder.OpenComponent<BitTextField>(5);
                builder.AddAttribute(6, "id", "unbound");
                builder.CloseComponent();
            }));
        });

        // the bound field gives the form a field state, after which a lookup of the field the unbound input does not
        // have would no longer be skipped as a lookup in an empty table.
        component.FindAll("input")[0].Change("not an email");

        component.InvokeAsync(() => editContext.Validate());

        Assert.IsTrue(component.FindAll(".bit-tfl")[0].ClassList.Contains("bit-inv"));
        Assert.IsFalse(component.FindAll(".bit-tfl")[1].ClassList.Contains("bit-inv"));
    }
}
