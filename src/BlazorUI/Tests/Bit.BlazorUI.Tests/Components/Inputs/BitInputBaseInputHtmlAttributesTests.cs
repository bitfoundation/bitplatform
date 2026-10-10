using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Bit.BlazorUI.Tests.Components.Inputs.TextField;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs;

/// <summary>
/// Pins the aria-invalid an input adds to its InputHtmlAttributes while its field fails validation: it reaches the
/// input element, and the dictionary the app passed - which several inputs may share - is never written into.
/// </summary>
[TestClass]
public class BitInputBaseInputHtmlAttributesTests : BunitTestContext
{
    [TestMethod]
    public void TheAriaInvalidOfAFailedValidationShouldLeaveTheDictionaryOfTheAppAsItWas()
    {
        var model = new BitTextFieldTestModel();
        var editContext = new EditContext(model);
        var messages = new ValidationMessageStore(editContext);
        var inputHtmlAttributes = new Dictionary<string, object> { ["data-test"] = "x" };

        var component = Context.Render(builder =>
        {
            builder.OpenComponent<CascadingValue<EditContext>>(0);
            builder.AddAttribute(1, "Value", editContext);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(child =>
            {
                child.OpenComponent<BitTextField>(0);
                child.AddAttribute(1, nameof(BitTextField.Value), model.Value);
                child.AddAttribute(2, nameof(BitTextField.ValueChanged), EventCallback.Factory.Create<string?>(this, value => model.Value = value));
                child.AddAttribute(3, nameof(BitTextField.ValueExpression), (Expression<Func<string?>>)(() => model.Value));
                child.AddAttribute(4, nameof(BitTextField.InputHtmlAttributes), inputHtmlAttributes);
                child.CloseComponent();
            }));
            builder.CloseComponent();
        });

        messages.Add(editContext.Field(nameof(BitTextFieldTestModel.Value)), "Invalid");
        component.InvokeAsync(editContext.NotifyValidationStateChanged);

        var input = component.Find("input");

        Assert.AreEqual("true", input.GetAttribute("aria-invalid"));
        Assert.AreEqual("x", input.GetAttribute("data-test"));
        Assert.HasCount(1, inputHtmlAttributes);

        messages.Clear();
        component.InvokeAsync(editContext.NotifyValidationStateChanged);

        input = component.Find("input");

        Assert.IsFalse(input.HasAttribute("aria-invalid"));
        Assert.AreEqual("x", input.GetAttribute("data-test"));
        Assert.HasCount(1, inputHtmlAttributes);
    }

    // A differently cased name is the same attribute: the field reads it back however the app cased it, rather than
    // writing a null over it while it is invalid.
    [TestMethod]
    [DataRow("aria-invalid")]
    [DataRow("Aria-Invalid")]
    public void AnAriaInvalidTheAppWroteShouldBeLeftWhereItIs(string name)
    {
        var model = new BitTextFieldTestModel();
        var editContext = new EditContext(model);
        var messages = new ValidationMessageStore(editContext);
        var inputHtmlAttributes = new Dictionary<string, object> { [name] = "grammar" };

        var component = Context.Render(builder =>
        {
            builder.OpenComponent<CascadingValue<EditContext>>(0);
            builder.AddAttribute(1, "Value", editContext);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(child =>
            {
                child.OpenComponent<BitTextField>(0);
                child.AddAttribute(1, nameof(BitTextField.Value), model.Value);
                child.AddAttribute(2, nameof(BitTextField.ValueChanged), EventCallback.Factory.Create<string?>(this, value => model.Value = value));
                child.AddAttribute(3, nameof(BitTextField.ValueExpression), (Expression<Func<string?>>)(() => model.Value));
                child.AddAttribute(4, nameof(BitTextField.InputHtmlAttributes), inputHtmlAttributes);
                child.CloseComponent();
            }));
            builder.CloseComponent();
        });

        messages.Add(editContext.Field(nameof(BitTextFieldTestModel.Value)), "Invalid");
        component.InvokeAsync(editContext.NotifyValidationStateChanged);

        Assert.AreEqual("grammar", component.Find("input").GetAttribute("aria-invalid"));

        messages.Clear();
        component.InvokeAsync(editContext.NotifyValidationStateChanged);

        // A valid field only takes off the aria-invalid it added itself.
        Assert.AreEqual("grammar", component.Find("input").GetAttribute("aria-invalid"));
        Assert.HasCount(1, inputHtmlAttributes);
    }
}
