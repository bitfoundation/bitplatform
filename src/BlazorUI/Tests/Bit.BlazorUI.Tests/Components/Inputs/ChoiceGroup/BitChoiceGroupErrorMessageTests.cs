using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.ChoiceGroup;

/// <summary>
/// Covers the rejection the app reports on a ChoiceGroup by itself - ErrorMessage, ErrorMessageTemplate,
/// ErrorMessageAriaLabel and Invalid - as opposed to the one the cascading EditContext reports: the line under the
/// items, the invalid state on the group and on its radios, the aria-describedby wiring and the live region.
/// </summary>
[TestClass]
public class BitChoiceGroupErrorMessageTests : BunitTestContext
{
    private static List<BitChoiceGroupItem<string>> GetItems() =>
    [
        new() { Text = "Item A", Value = "A" },
        new() { Text = "Item B", Value = "B" },
    ];

    [TestMethod]
    public void BitChoiceGroupErrorMessageShouldRenderUnderTheItemsAndMarkTheGroupInvalid()
    {
        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.Label, "Shipping");
            parameters.Add(p => p.ErrorMessage, "Express shipping is not available to your address.");
        });

        var root = component.Find(".bit-chg");

        Assert.AreEqual("Express shipping is not available to your address.", component.Find(".bit-chg-erm").TextContent.Trim());

        // a message saying what is wrong with the choice is a rejection of it, so it marks the group the same way the
        // flag does - on the group and on every radio, as the cascading EditContext does
        Assert.IsTrue(root.ClassList.Contains("bit-inv"));
        Assert.AreEqual("true", root.GetAttribute("aria-invalid"));
        Assert.IsTrue(component.FindAll(".bit-chg-icn input").All(i => i.GetAttribute("aria-invalid") == "true"));

        // the line sits right under the items, outside every item label, so it is no part of an option's name
        var parts = root.Children.Select(c => c.ClassList.Contains("bit-chg-erm") ? "error" : c.ClassList.Contains("bit-chg-cnt") ? "items" : null)
                                 .Where(p => p is not null)
                                 .ToArray();

        CollectionAssert.AreEqual(new[] { "items", "error" }, parts);
        Assert.AreEqual(0, component.FindAll(".bit-chg-itl .bit-chg-erm").Count);
    }

    [TestMethod]
    public void BitChoiceGroupWithoutAnErrorMessageShouldRenderNoneOfIt()
    {
        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
        });

        var root = component.Find(".bit-chg");

        Assert.AreEqual(0, component.FindAll(".bit-chg-erm").Count);
        Assert.IsFalse(root.ClassList.Contains("bit-inv"));
        Assert.IsFalse(root.HasAttribute("aria-invalid"));
        Assert.IsFalse(root.HasAttribute("aria-describedby"));
        Assert.IsFalse(component.FindAll(".bit-chg-icn input").Any(i => i.HasAttribute("aria-invalid")));

        // a form full of plain choice groups does not carry an empty live region per group
        Assert.AreEqual(0, component.FindAll("[role=status]").Count);
    }

    [TestMethod]
    public void BitChoiceGroupInvalidShouldMarkTheGroupWithoutRenderingAMessage()
    {
        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.Invalid, true);
        });

        Assert.IsTrue(component.Find(".bit-chg").ClassList.Contains("bit-inv"));
        Assert.AreEqual("true", component.Find(".bit-chg").GetAttribute("aria-invalid"));
        Assert.IsTrue(component.FindAll(".bit-chg-icn input").All(i => i.GetAttribute("aria-invalid") == "true"));

        // the flag says the choice was rejected; it does not invent a line saying why
        Assert.AreEqual(0, component.FindAll(".bit-chg-erm").Count);

        component.Render(parameters => parameters.Add(p => p.Invalid, false));

        Assert.IsFalse(component.Find(".bit-chg").ClassList.Contains("bit-inv"));
        Assert.IsFalse(component.Find(".bit-chg").HasAttribute("aria-invalid"));
        Assert.IsFalse(component.FindAll(".bit-chg-icn input").Any(i => i.HasAttribute("aria-invalid")));
    }

    [TestMethod]
    public void BitChoiceGroupInvalidShouldReachTheRadiosOfTheOptionsApi()
    {
        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupOption<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Invalid, true);
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

        Assert.AreEqual(2, component.FindAll(".bit-chg-icn input").Count);
        Assert.IsTrue(component.FindAll(".bit-chg-icn input").All(i => i.GetAttribute("aria-invalid") == "true"));

        // an option renders its radio itself, and is pushed a re-render when only the group's parameters change
        component.Render(parameters => parameters.Add(p => p.Invalid, false));

        Assert.IsFalse(component.FindAll(".bit-chg-icn input").Any(i => i.HasAttribute("aria-invalid")));
    }

    [TestMethod]
    public void BitChoiceGroupErrorMessageTemplateShouldReplaceThePlainMessage()
    {
        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.ErrorMessage, "Plain message");
            parameters.Add(p => p.ErrorMessageTemplate, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "custom-error");
                builder.AddContent(2, "Templated message");
                builder.CloseElement();
            }));
        });

        Assert.AreEqual("Templated message", component.Find(".bit-chg-erm .custom-error").TextContent.Trim());
        Assert.IsFalse(component.Find(".bit-chg-erm").TextContent.Contains("Plain message"));

        // the plain text is not on screen, so it is not what the live region announces either
        Assert.AreEqual("Invalid input", component.Find("[role=status]").TextContent.Trim());
        Assert.AreEqual(component.Find(".bit-chg-erm").Id, component.Find(".bit-chg").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitChoiceGroupErrorMessageAriaLabelShouldSpeakForTheTemplate()
    {
        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.ErrorMessageAriaLabel, "این روش ارسال برای نشانی شما در دسترس نیست");
            parameters.Add(p => p.ErrorMessage, "the plain text");
            parameters.Add(p => p.ErrorMessageTemplate, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "i");
                builder.AddAttribute(1, "class", "warning-icon");
                builder.CloseElement();
            }));
        });

        var label = component.Find($"#{component.Find(".bit-chg").GetAttribute("aria-describedby")}");

        Assert.AreEqual("این روش ارسال برای نشانی شما در دسترس نیست", component.Find("[role=status]").TextContent.Trim());
        Assert.AreEqual("این روش ارسال برای نشانی شما در دسترس نیست", label.TextContent);

        // what is read and not seen is visually hidden - not the item description class, which is on screen
        Assert.IsTrue(label.ClassList.Contains("bit-chg-vhd"));
        Assert.IsTrue(component.Find("[role=status]").ClassList.Contains("bit-chg-vhd"));

        // once the template goes, the plain text is on screen, and it is its own accessible text
        component.Render(parameters => parameters.Add(p => p.ErrorMessageTemplate, (RenderFragment?)null));

        Assert.AreEqual("the plain text", component.Find("[role=status]").TextContent.Trim());
        Assert.AreEqual(component.Find(".bit-chg-erm").Id, component.Find(".bit-chg").GetAttribute("aria-describedby"));
    }

    [TestMethod, DataRow(""), DataRow("  ")]
    public void BitChoiceGroupBlankErrorMessageAriaLabelShouldFallBackToTheDefault(string label)
    {
        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.ErrorMessageAriaLabel, label);
            parameters.Add(p => p.ErrorMessageTemplate, (RenderFragment)(builder => builder.AddContent(0, "!")));
        });

        Assert.AreEqual("Invalid input", component.Find("[role=status]").TextContent.Trim());
        Assert.AreEqual(component.Find(".bit-chg-erm").Id, component.Find(".bit-chg").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitChoiceGroupInvalidShouldOnlyBeAnnouncedWithAnErrorMessageAriaLabel()
    {
        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.Invalid, true);
        });

        // the flag on its own has nothing to say, and so no live region to say it in
        Assert.AreEqual(0, component.FindAll("[role=status]").Count);
        Assert.IsFalse(component.Find(".bit-chg").HasAttribute("aria-describedby"));

        component.Render(parameters => parameters.Add(p => p.ErrorMessageAriaLabel, "انتخاب نامعتبر"));

        Assert.AreEqual("انتخاب نامعتبر", component.Find("[role=status]").TextContent.Trim());
        Assert.AreEqual("انتخاب نامعتبر", component.Find($"#{component.Find(".bit-chg").GetAttribute("aria-describedby")}").TextContent);
    }

    [TestMethod]
    public void BitChoiceGroupLiveRegionShouldBeRenderedEmptyUntilThereIsAMessage()
    {
        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.ErrorMessage, null);
        });

        // a live region that arrives with its text already in it is the one thing screen readers are known to miss,
        // so a group wired for a message has the region in the markup and only its text comes and goes
        Assert.AreEqual(string.Empty, component.Find("[role=status]").TextContent.Trim());
        Assert.AreEqual("polite", component.Find("[role=status]").GetAttribute("aria-live"));

        component.Render(parameters => parameters.Add(p => p.ErrorMessage, "Rejected by the server."));

        Assert.AreEqual("Rejected by the server.", component.Find("[role=status]").TextContent.Trim());

        component.Render(parameters => parameters.Add(p => p.ErrorMessage, null));

        Assert.AreEqual(string.Empty, component.Find("[role=status]").TextContent.Trim());
        Assert.IsFalse(component.Find(".bit-chg").ClassList.Contains("bit-inv"));
    }

    [TestMethod]
    public void BitChoiceGroupShouldAnnounceTheErrorMessageAPickBrings()
    {
        IRenderedComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>> component = default!;

        // the app answers a pick with a message of its own, the way a bound value with a rule on it does
        component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.Value, "A");
            parameters.Add(p => p.ErrorMessage, null);
            parameters.Add(p => p.ValueChanged, (string? value) => component.Render(ps =>
            {
                ps.Add(p => p.Value, value);
                ps.Add(p => p.ErrorMessage, value == "B" ? "Item B is out of stock." : null);
            }));
        });

        Assert.AreEqual(string.Empty, component.Find("[role=status]").TextContent.Trim());

        component.FindAll(".bit-chg-icn input")[1].Change(true);

        Assert.AreEqual("B", component.Instance.Value);
        Assert.AreEqual("Item B is out of stock.", component.Find("[role=status]").TextContent.Trim());
        Assert.IsTrue(component.FindAll(".bit-chg-icn input").All(i => i.GetAttribute("aria-invalid") == "true"));

        component.FindAll(".bit-chg-icn input")[0].Change(true);

        Assert.AreEqual("A", component.Instance.Value);
        Assert.AreEqual(string.Empty, component.Find("[role=status]").TextContent.Trim());
        Assert.IsFalse(component.FindAll(".bit-chg-icn input").Any(i => i.HasAttribute("aria-invalid")));
    }

    [TestMethod]
    public void BitChoiceGroupErrorMessageShouldBeDescribedAfterTheDescription()
    {
        var component = Context.Render(builder =>
        {
            builder.OpenComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(0);
            builder.AddAttribute(1, nameof(BitChoiceGroup<BitChoiceGroupItem<string>, string>.Items), GetItems());
            builder.AddAttribute(2, nameof(BitChoiceGroup<BitChoiceGroupItem<string>, string>.Label), "Shipping");
            builder.AddAttribute(3, nameof(BitChoiceGroup<BitChoiceGroupItem<string>, string>.Description), "Pick how fast the order should arrive.");
            builder.AddAttribute(4, nameof(BitChoiceGroup<BitChoiceGroupItem<string>, string>.ErrorMessage), "Express shipping is not available to your address.");
            builder.AddAttribute(5, "aria-describedby", "shipping-hint");
            builder.CloseComponent();
        });

        var root = component.Find(".bit-chg");
        var describedBy = root.GetAttribute("aria-describedby")!.Split(' ');

        var descriptionId = component.Find(".bit-chg-gds").Id;
        var errorId = component.Find(".bit-chg-erm").Id;

        // the page's own reference is kept, and the two lines are read in the order they sit on the page: the
        // description under the label, the error line under the items
        CollectionAssert.AreEqual(new[] { "shipping-hint", descriptionId, errorId }, describedBy);

        var lines = root.Children.Select(c => c.ClassList.Contains("bit-chg-erm") ? "error" : c.ClassList.Contains("bit-chg-gds") ? "description" : null)
                                 .Where(l => l is not null)
                                 .ToArray();

        CollectionAssert.AreEqual(new[] { "description", "error" }, lines);
    }

    [TestMethod]
    public void BitChoiceGroupErrorMessageStyleAndClassTest()
    {
        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.ErrorMessage, "Rejected.");
            parameters.Add(p => p.Styles, new BitChoiceGroupClassStyles
            {
                ErrorMessage = "font-style: italic",
                ErrorMessageContainer = "margin-top: 1rem"
            });
            parameters.Add(p => p.Classes, new BitChoiceGroupClassStyles
            {
                ErrorMessage = "custom-error",
                ErrorMessageContainer = "custom-error-container"
            });
        });

        var container = component.Find(".bit-chg-erm");

        Assert.IsTrue(container.ClassList.Contains("custom-error-container"));
        Assert.AreEqual("margin-top: 1rem", container.GetAttribute("style"));

        Assert.AreEqual("font-style: italic", component.Find(".bit-chg-erm .custom-error").GetAttribute("style"));
    }

    [TestMethod]
    public async Task BitChoiceGroupAChoiceTheEditContextRejectsShouldNotBeMarkedInvalidTwice()
    {
        var model = new BitChoiceGroupTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.AddCascadingValue(editContext);
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.Value, model.Choice);
            parameters.Add(p => p.ValueChanged, (string? v) => model.Choice = v);
            parameters.Add(p => p.ValueExpression, () => model.Choice);
            parameters.Add(p => p.Invalid, true);
            parameters.Add(p => p.ErrorMessage, "Rejected by the server.");
        });

        await component.InvokeAsync(() => editContext.Validate());

        // the base class registers the class for any rejection, the form's and the app's alike, so the two together
        // still make a single one
        Assert.AreEqual(1, component.Find(".bit-chg").ClassList.Count(c => c == "bit-inv"));
        Assert.AreEqual("true", component.Find(".bit-chg").GetAttribute("aria-invalid"));
        Assert.IsTrue(component.FindAll(".bit-chg-icn input").All(i => i.GetAttribute("aria-invalid") == "true"));

        // and once the app drops its own rejection, the form's still stands
        component.Render(parameters =>
        {
            parameters.Add(p => p.Invalid, false);
            parameters.Add(p => p.ErrorMessage, null);
        });

        Assert.IsTrue(component.Find(".bit-chg").ClassList.Contains("bit-inv"));
        Assert.AreEqual("true", component.Find(".bit-chg").GetAttribute("aria-invalid"));
        Assert.IsTrue(component.FindAll(".bit-chg-icn input").All(i => i.GetAttribute("aria-invalid") == "true"));
    }

    [TestMethod, DataRow(false), DataRow(true)]
    public void BitChoiceGroupShouldKeepAnAriaInvalidThePageWroteWhileTheAppRejectsNothing(bool invalid)
    {
        var component = Context.Render(builder =>
        {
            builder.OpenComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(0);
            builder.AddAttribute(1, nameof(BitChoiceGroup<BitChoiceGroupItem<string>, string>.Items), GetItems());
            builder.AddAttribute(2, nameof(BitChoiceGroup<BitChoiceGroupItem<string>, string>.InputHtmlAttributes), new Dictionary<string, object> { { "aria-invalid", "grammar" } });
            builder.AddAttribute(3, nameof(BitChoiceGroup<BitChoiceGroupItem<string>, string>.Invalid), invalid);
            builder.AddAttribute(4, "aria-invalid", "spelling");
            builder.CloseComponent();
        });

        // what the page wrote on the group and on the radios stands while the app rejects nothing, and a rejection by
        // the app says "true" on both, whatever the page wrote
        Assert.AreEqual(invalid ? "true" : "spelling", component.Find(".bit-chg").GetAttribute("aria-invalid"));
        Assert.IsTrue(component.FindAll(".bit-chg-icn input").All(i => i.GetAttribute("aria-invalid") == (invalid ? "true" : "grammar")));
    }
}
