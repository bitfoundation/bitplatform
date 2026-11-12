using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.ChoiceGroup;

/// <summary>
/// Covers ShowValidationMessage: the messages the cascading EditContext holds for the field shown in the error line
/// under the items and referenced by the group, giving way to the app's own ErrorMessage, and never announced.
/// </summary>
[TestClass]
public class BitChoiceGroupValidationMessageTests : BunitTestContext
{
    private const string RequiredMessage = "The Choice field is required.";

    private static List<BitChoiceGroupItem<string>> GetItems() =>
    [
        new() { Text = "Item A", Value = "A" },
        new() { Text = "Item B", Value = "B" },
    ];

    [TestMethod]
    public void BitChoiceGroupShowValidationMessageShouldShowTheMessageOfTheFormUnderTheItems()
    {
        var component = RenderComponent<BitChoiceGroupValidationTest>(parameters => parameters.Add(p => p.ShowValidationMessage, true));

        // Nothing is shown before the form has anything to say.
        Assert.IsEmpty(component.FindAll(".bit-chg-erm"));
        Assert.IsNull(component.Find(".bit-chg").GetAttribute("aria-describedby"));

        component.Find("form").Submit();

        var root = component.Find(".bit-chg");
        var error = component.Find(".bit-chg-erm");

        Assert.AreEqual(1, component.Instance.InvalidCount);
        Assert.AreEqual(RequiredMessage, error.TextContent.Trim());
        Assert.AreEqual(error.Id, root.GetAttribute("aria-describedby"));
        Assert.AreEqual("true", root.GetAttribute("aria-invalid"));
        Assert.IsTrue(root.ClassList.Contains("bit-inv"));

        // The line sits right under the items, where the app's own message goes. The group is what it describes, so it
        // is read out when the group is entered; the radios keep to their own descriptions.
        var parts = root.Children.Select(c => c.ClassList.Contains("bit-chg-erm") ? "error" : c.ClassList.Contains("bit-chg-cnt") ? "items" : null)
                                 .Where(p => p is not null)
                                 .ToArray();

        Assert.AreSequenceEqual(new[] { "items", "error" }, parts);
        Assert.DoesNotContain(i => i.HasAttribute("aria-describedby"), component.FindAll(".bit-chg-icn input"));

        // A pick the form accepts takes the line, and the reference to it, away again.
        component.FindAll(".bit-chg-icn input")[1].Change(true);

        Assert.AreEqual("B", component.Instance.TestModel.Choice);
        Assert.IsEmpty(component.FindAll(".bit-chg-erm"));
        Assert.IsNull(component.Find(".bit-chg").GetAttribute("aria-describedby"));

        component.Find("form").Submit();

        Assert.AreEqual(1, component.Instance.ValidCount);
    }

    [TestMethod]
    public void BitChoiceGroupShowValidationMessageShouldFollowTheMessagesWhileTheFieldStaysInvalid()
    {
        var model = new BitChoiceGroupTestModel();
        var editContext = new EditContext(model);
        var field = new FieldIdentifier(model, nameof(BitChoiceGroupTestModel.Choice));
        var store = new ValidationMessageStore(editContext);

        var component = RenderGroup(editContext, model, parameters => parameters.Add(p => p.ShowValidationMessage, true));

        component.InvokeAsync(() =>
        {
            store.Add(field, "Item A is out of stock.");
            editContext.NotifyValidationStateChanged();
        });

        Assert.AreEqual("Item A is out of stock.", component.Find(".bit-chg-erm").TextContent.Trim());

        // Every message the field holds is shown, in the order they were added, as the ValidationMessage component
        // lists them.
        component.InvokeAsync(() =>
        {
            store.Clear(field);
            store.Add(field, "Not for this region.");
            store.Add(field, "Not with this plan.");
            editContext.NotifyValidationStateChanged();
        });

        Assert.AreEqual("Not for this region. Not with this plan.", component.Find(".bit-chg-erm").TextContent.Trim());
    }

    [TestMethod]
    public void BitChoiceGroupWithoutShowValidationMessageShouldLeaveTheMessagesToTheValidationMessageComponent()
    {
        var model = new BitChoiceGroupTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderGroup(editContext, model);

        component.InvokeAsync(() => editContext.Validate());

        // The group is marked as before, and the message stays where the page puts it.
        Assert.AreEqual("true", component.Find(".bit-chg").GetAttribute("aria-invalid"));
        Assert.IsEmpty(component.FindAll(".bit-chg-erm"));
        Assert.IsNull(component.Find(".bit-chg").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitChoiceGroupErrorMessageShouldWinTheLineOverTheMessagesOfTheEditContext()
    {
        var model = new BitChoiceGroupTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderGroup(editContext, model, parameters =>
        {
            parameters.Add(p => p.ShowValidationMessage, true);
            parameters.Add(p => p.ErrorMessage, "Express shipping is not available to your address.");
        });

        component.InvokeAsync(() => editContext.Validate());

        Assert.ContainsSingle(component.FindAll(".bit-chg-erm"));
        Assert.AreEqual("Express shipping is not available to your address.", component.Find(".bit-chg-erm").TextContent.Trim());

        // Once the app takes its message back, the EditContext's takes the line.
        component.Render(parameters => parameters.Add(p => p.ErrorMessage, null));

        Assert.AreEqual(RequiredMessage, component.Find(".bit-chg-erm").TextContent.Trim());
        Assert.AreEqual(component.Find(".bit-chg-erm").Id, component.Find(".bit-chg").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitChoiceGroupErrorMessageTemplateShouldKeepItsLabelOverTheMessagesOfTheEditContext()
    {
        var model = new BitChoiceGroupTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderGroup(editContext, model, parameters =>
        {
            parameters.Add(p => p.ShowValidationMessage, true);
            parameters.Add(p => p.ErrorMessageAriaLabel, "This plan is not available in your region.");
            parameters.Add(p => p.ErrorMessageTemplate, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "b");
                builder.AddContent(1, "Unavailable");
                builder.CloseElement();
            }));
        });

        component.InvokeAsync(() => editContext.Validate());

        // The template is what is on screen, so its label is what the group is described by.
        var labelId = component.Find(".bit-chg").GetAttribute("aria-describedby");

        Assert.AreEqual("Unavailable", component.Find(".bit-chg-erm b").TextContent);
        Assert.DoesNotContain(RequiredMessage, component.Find(".bit-chg-erm").TextContent);
        Assert.AreEqual("This plan is not available in your region.", component.Find($"#{labelId}").TextContent);
    }

    [TestMethod]
    public void BitChoiceGroupShouldNotAnnounceTheMessagesOfTheEditContext()
    {
        var model = new BitChoiceGroupTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        // One group is wired for the live region by an ErrorMessage written as a null for now, the other is not.
        var wired = RenderGroup(editContext, model, parameters =>
        {
            parameters.Add(p => p.ShowValidationMessage, true);
            parameters.Add(p => p.ErrorMessage, null);
        });

        var plain = RenderGroup(editContext, model, parameters => parameters.Add(p => p.ShowValidationMessage, true));

        wired.InvokeAsync(() => editContext.Validate());

        // A submit validates every field at once; the group that takes the focus says what is wrong with it instead.
        Assert.AreEqual(RequiredMessage, wired.Find(".bit-chg-erm").TextContent.Trim());
        Assert.AreEqual(string.Empty, wired.Find("[role=status]").TextContent.Trim());

        // The messages do not wire a group for a live region of its own either.
        Assert.AreEqual(RequiredMessage, plain.Find(".bit-chg-erm").TextContent.Trim());
        Assert.IsEmpty(plain.FindAll("[role=status]"));
    }

    [TestMethod]
    public void BitChoiceGroupShouldDescribeTheGroupWithTheMessageAfterItsOtherDescriptions()
    {
        var model = new BitChoiceGroupTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderGroup(editContext, model, parameters =>
        {
            parameters.Add(p => p.ShowValidationMessage, true);
            parameters.Add(p => p.Description, "Pick the one you use most.");
            parameters.Add(p => p.HtmlAttributes, new Dictionary<string, object> { ["aria-describedby"] = "page-hint" });
        });

        component.InvokeAsync(() => editContext.Validate());

        // They are read in the order they sit on the page: what the page pointed at, the description under the label,
        // and the line under the items.
        var ids = component.Find(".bit-chg").GetAttribute("aria-describedby")!.Split(' ');

        Assert.HasCount(3, ids);
        Assert.AreEqual("page-hint", ids[0]);
        Assert.AreEqual(component.Find(".bit-chg-gds").Id, ids[1]);
        Assert.AreEqual(component.Find(".bit-chg-erm").Id, ids[2]);
    }

    [TestMethod]
    public void BitChoiceGroupForcedInvalidLabelShouldGiveWayToAMessageOnScreen()
    {
        var model = new BitChoiceGroupTestModel();
        var editContext = new EditContext(model);
        var field = new FieldIdentifier(model, nameof(BitChoiceGroupTestModel.Choice));
        var store = new ValidationMessageStore(editContext);

        var component = RenderGroup(editContext, model, parameters =>
        {
            parameters.Add(p => p.ShowValidationMessage, true);
            parameters.Add(p => p.Invalid, true);
            parameters.Add(p => p.ErrorMessageAriaLabel, "Refused by the server.");
        });

        var labelId = component.Find(".bit-chg").GetAttribute("aria-describedby");

        Assert.AreEqual("Refused by the server.", component.Find($"#{labelId}").TextContent);

        component.InvokeAsync(() =>
        {
            store.Add(field, "Item A is out of stock.");
            editContext.NotifyValidationStateChanged();
        });

        // The text on screen describes the group; the app's own rejection is still what the live region says.
        Assert.AreEqual(component.Find(".bit-chg-erm").Id, component.Find(".bit-chg").GetAttribute("aria-describedby"));
        Assert.AreEqual("Refused by the server.", component.Find("[role=status]").TextContent.Trim());

        component.InvokeAsync(() =>
        {
            store.Clear();
            editContext.NotifyValidationStateChanged();
        });

        Assert.AreEqual(labelId, component.Find(".bit-chg").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitChoiceGroupThatStopsValidatingShouldTakeTheMessageAway()
    {
        var model = new BitChoiceGroupTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderGroup(editContext, model, parameters =>
        {
            parameters.Add(p => p.ShowValidationMessage, true);
            parameters.Add(p => p.NoValidate, false);
        });

        component.InvokeAsync(() => editContext.Validate());

        Assert.ContainsSingle(component.FindAll(".bit-chg-erm"));

        component.Render(parameters => parameters.Add(p => p.NoValidate, true));

        Assert.IsEmpty(component.FindAll(".bit-chg-erm"));
        Assert.IsNull(component.Find(".bit-chg").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitChoiceGroupBoundToNothingShouldShowNoMessage()
    {
        var model = new BitChoiceGroupTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderGroup(editContext, model, parameters => parameters.Add(p => p.ShowValidationMessage, true), bound: false);

        component.InvokeAsync(() => editContext.Validate());

        // The model has a message, but the group has no member of it the message could belong to.
        Assert.IsNotEmpty(editContext.GetValidationMessages());
        Assert.IsEmpty(component.FindAll(".bit-chg-erm"));
    }

    [TestMethod]
    public void BitChoiceGroupOfOptionsShouldShowTheMessageOfTheEditContext()
    {
        var model = new BitChoiceGroupTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupOption<string>, string>>(parameters =>
        {
            parameters.AddCascadingValue(editContext);
            parameters.Add(p => p.ShowValidationMessage, true);
            parameters.Add(p => p.Value, model.Choice);
            parameters.Add(p => p.ValueChanged, (string v) => model.Choice = v);
            parameters.Add(p => p.ValueExpression, () => model.Choice!);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitChoiceGroupOption<string>>(0);
                builder.AddAttribute(1, nameof(BitChoiceGroupOption<string>.Text), "Option A");
                builder.AddAttribute(2, nameof(BitChoiceGroupOption<string>.Value), "A");
                builder.CloseComponent();

                builder.OpenComponent<BitChoiceGroupOption<string>>(3);
                builder.AddAttribute(4, nameof(BitChoiceGroupOption<string>.Text), "Option B");
                builder.AddAttribute(5, nameof(BitChoiceGroupOption<string>.Value), "B");
                builder.CloseComponent();
            });
        });

        component.InvokeAsync(() => editContext.Validate());

        // The options render inside the items container, the line under it, so the options API shows it the same way.
        Assert.AreEqual(RequiredMessage, component.Find(".bit-chg-erm").TextContent.Trim());
        Assert.AreEqual(component.Find(".bit-chg-erm").Id, component.Find(".bit-chg").GetAttribute("aria-describedby"));

        component.FindAll(".bit-chg-icn input")[0].Change(true);

        Assert.AreEqual("A", model.Choice);
        Assert.IsEmpty(component.FindAll(".bit-chg-erm"));
    }

    [TestMethod]
    public void BitChoiceGroupShouldShowTheMessagesWhenACascadeAsksForThem()
    {
        var model = new BitChoiceGroupTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        // One BitParams around a form turns the messages on for every choice group in it.
        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.AddCascadingValue(editContext);
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitChoiceGroupParams { ShowValidationMessage = true } });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(0);
                builder.AddAttribute(1, nameof(BitChoiceGroup<BitChoiceGroupItem<string>, string>.Items), GetItems());
                builder.AddAttribute(2, nameof(BitChoiceGroup<BitChoiceGroupItem<string>, string>.Value), model.Choice);
                builder.AddAttribute(3, nameof(BitChoiceGroup<BitChoiceGroupItem<string>, string>.ValueChanged), EventCallback.Factory.Create<string>(this, v => model.Choice = v));
                builder.AddAttribute(4, nameof(BitChoiceGroup<BitChoiceGroupItem<string>, string>.ValueExpression), (Expression<Func<string>>)(() => model.Choice!));
                builder.CloseComponent();
            });
        });

        component.InvokeAsync(() => editContext.Validate());

        Assert.AreEqual(RequiredMessage, component.Find(".bit-chg-erm").TextContent.Trim());
    }

    private IRenderedComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>> RenderGroup(
        EditContext editContext, BitChoiceGroupTestModel model,
        Action<ComponentParameterCollectionBuilder<BitChoiceGroup<BitChoiceGroupItem<string>, string>>>? configure = null,
        bool bound = true)
    {
        return RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.AddCascadingValue(editContext);
            parameters.Add(p => p.Items, GetItems());

            if (bound)
            {
                parameters.Add(p => p.Value, model.Choice);
                parameters.Add(p => p.ValueChanged, (string v) => model.Choice = v);
                parameters.Add(p => p.ValueExpression, () => model.Choice!);
            }

            configure?.Invoke(parameters);
        });
    }
}
