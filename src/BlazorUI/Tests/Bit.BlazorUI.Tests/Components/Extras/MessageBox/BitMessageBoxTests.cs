using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.MessageBox;

[TestClass]
public class BitMessageBoxTests : BunitTestContext
{
    [TestMethod]
    public void BitMessageBoxShouldRenderTitleBodyAndDefaultOk()
    {
        var component = RenderComponent<BitMessageBox>(parameters =>
        {
            parameters.Add(p => p.Title, "Sample Title");
            parameters.Add(p => p.Body, "Sample Body");
        });

        Assert.IsTrue(component.Markup.Contains("Sample Title"));
        Assert.IsTrue(component.Markup.Contains("Sample Body"));

        var okButtonText = component.Find(".bit-msb-ftr .bit-btn-prt");
        Assert.AreEqual("Ok", okButtonText.TextContent);
    }

    [TestMethod]
    public void BitMessageBoxShouldRenderCustomOkText()
    {
        var component = RenderComponent<BitMessageBox>(parameters =>
        {
            parameters.Add(p => p.OkText, "Confirm");
        });

        var okButton = component.Find(".bit-msb-ftr .bit-btn-prt");

        Assert.AreEqual("Confirm", okButton.TextContent);
    }

    [TestMethod]
    public void BitMessageBoxShouldInvokeOnCloseFromCloseButton()
    {
        var closed = 0;

        var component = RenderComponent<BitMessageBox>(parameters =>
        {
            parameters.Add(p => p.OnClose, () => closed++);
        });

        var closeButton = component.Find(".bit-msb-hdr .bit-btn");

        closeButton.Click();

        Assert.AreEqual(1, closed);
    }

    [TestMethod]
    public void BitMessageBoxShouldInvokeOnCloseFromOkButton()
    {
        var closed = 0;

        var component = RenderComponent<BitMessageBox>(parameters =>
        {
            parameters.Add(p => p.OnClose, () => closed++);
        });

        var okButton = component.Find(".bit-msb-ftr .bit-btn");

        okButton.Click();

        Assert.AreEqual(1, closed);
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void BitMessageBoxShouldRespectIsEnabled(bool isEnabled)
    {
        var component = RenderComponent<BitMessageBox>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var root = component.Find(".bit-msb");

        if (isEnabled)
        {
            Assert.IsFalse(root.ClassList.Contains("bit-dis"));
        }
        else
        {
            Assert.IsTrue(root.ClassList.Contains("bit-dis"));
        }
    }

    [TestMethod]
    public void BitMessageBoxShouldDisableItsButtonsWhileDisabled()
    {
        var component = RenderComponent<BitMessageBox>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.Buttons, BitMessageBoxButtons.OkCancel);
        });

        foreach (var button in component.FindAll(".bit-msb .bit-btn"))
        {
            Assert.AreEqual("true", button.GetAttribute("aria-disabled"));
            Assert.IsTrue(button.ClassList.Contains("bit-dis"));
        }
    }

    [TestMethod]
    public void BitMessageBoxShouldNotAnswerWhileDisabled()
    {
        var results = new List<BitMessageBoxResult>();

        var component = RenderComponent<BitMessageBox>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.OnResult, r => results.Add(r));
        });

        component.Find(".bit-msb-ftr .bit-btn").Click();

        Assert.AreEqual(0, results.Count);
        Assert.AreEqual(BitMessageBoxResult.None, component.Instance.Result);
    }



    [TestMethod,
        DataRow(BitMessageBoxButtons.Ok, new[] { "Ok" }),
        DataRow(BitMessageBoxButtons.OkCancel, new[] { "Ok", "Cancel" }),
        DataRow(BitMessageBoxButtons.YesNo, new[] { "Yes", "No" }),
        DataRow(BitMessageBoxButtons.YesNoCancel, new[] { "Yes", "No", "Cancel" })]
    public void BitMessageBoxShouldRenderTheButtonsOfTheSetInOrder(BitMessageBoxButtons buttons, string[] expected)
    {
        var component = RenderComponent<BitMessageBox>(parameters =>
        {
            parameters.Add(p => p.Buttons, buttons);
        });

        var texts = component.FindAll(".bit-msb-ftr .bit-btn-prt").Select(b => b.TextContent).ToArray();

        CollectionAssert.AreEqual(expected, texts);
    }

    [TestMethod]
    public void BitMessageBoxShouldReverseTheOrderOfItsButtons()
    {
        var component = RenderComponent<BitMessageBox>(parameters =>
        {
            parameters.Add(p => p.Reversed, true);
            parameters.Add(p => p.Buttons, BitMessageBoxButtons.YesNoCancel);
        });

        var texts = component.FindAll(".bit-msb-ftr .bit-btn-prt").Select(b => b.TextContent).ToArray();

        CollectionAssert.AreEqual(new[] { "Cancel", "No", "Yes" }, texts);
    }

    [TestMethod]
    public void BitMessageBoxShouldKeepThePrimaryLookOnTheAffirmativeButtonWhileReversed()
    {
        var component = RenderComponent<BitMessageBox>(parameters =>
        {
            parameters.Add(p => p.Reversed, true);
            parameters.Add(p => p.Buttons, BitMessageBoxButtons.YesNo);
        });

        var buttons = component.FindAll(".bit-msb-ftr .bit-btn");

        // No comes first while reversed, and the fill variant is still on Yes.
        Assert.IsTrue(buttons[0].ClassList.Contains("bit-btn-otl"));
        Assert.IsTrue(buttons[1].ClassList.Contains("bit-btn-fil"));
    }

    [TestMethod]
    public void BitMessageBoxShouldRenderTheCustomTextOfEveryButton()
    {
        var component = RenderComponent<BitMessageBox>(parameters =>
        {
            parameters.Add(p => p.Buttons, BitMessageBoxButtons.YesNoCancel);
            parameters.Add(p => p.YesText, "Sure");
            parameters.Add(p => p.NoText, "Nope");
            parameters.Add(p => p.CancelText, "Later");
        });

        var texts = component.FindAll(".bit-msb-ftr .bit-btn-prt").Select(b => b.TextContent).ToArray();

        CollectionAssert.AreEqual(new[] { "Sure", "Nope", "Later" }, texts);
    }

    [TestMethod,
        DataRow(0, BitMessageBoxResult.Yes),
        DataRow(1, BitMessageBoxResult.No),
        DataRow(2, BitMessageBoxResult.Cancel)]
    public void BitMessageBoxShouldReportTheResultOfTheButtonThatWasPressed(int index, BitMessageBoxResult expected)
    {
        var results = new List<BitMessageBoxResult>();

        var component = RenderComponent<BitMessageBox>(parameters =>
        {
            parameters.Add(p => p.Buttons, BitMessageBoxButtons.YesNoCancel);
            parameters.Add(p => p.OnResult, r => results.Add(r));
        });

        component.FindAll(".bit-msb-ftr .bit-btn")[index].Click();

        Assert.AreEqual(1, results.Count);
        Assert.AreEqual(expected, results[0]);
        Assert.AreEqual(expected, component.Instance.Result);
    }

    [TestMethod]
    public void BitMessageBoxShouldReportNoAnswerFromTheCloseButton()
    {
        var results = new List<BitMessageBoxResult>();
        var cancelled = 0;

        var component = RenderComponent<BitMessageBox>(parameters =>
        {
            parameters.Add(p => p.Buttons, BitMessageBoxButtons.OkCancel);
            parameters.Add(p => p.OnResult, r => results.Add(r));
            parameters.Add(p => p.OnCancel, () => cancelled++);
        });

        component.Find(".bit-msb-hdr .bit-btn").Click();

        Assert.AreEqual(1, results.Count);
        Assert.AreEqual(BitMessageBoxResult.None, results[0]);
        Assert.AreEqual(0, cancelled);
    }

    [TestMethod]
    public void BitMessageBoxShouldInvokeThePerButtonCallbacks()
    {
        var ok = 0;
        var cancel = 0;
        var yes = 0;
        var no = 0;

        var component = RenderComponent<BitMessageBox>(parameters =>
        {
            parameters.Add(p => p.Buttons, BitMessageBoxButtons.YesNoCancel);
            parameters.Add(p => p.OnOk, () => ok++);
            parameters.Add(p => p.OnCancel, () => cancel++);
            parameters.Add(p => p.OnYes, () => yes++);
            parameters.Add(p => p.OnNo, () => no++);
        });

        component.FindAll(".bit-msb-ftr .bit-btn")[0].Click();
        component.FindAll(".bit-msb-ftr .bit-btn")[1].Click();
        component.FindAll(".bit-msb-ftr .bit-btn")[2].Click();

        Assert.AreEqual(0, ok);
        Assert.AreEqual(1, yes);
        Assert.AreEqual(1, no);
        Assert.AreEqual(1, cancel);
    }

    [TestMethod]
    public void BitMessageBoxShouldRaiseOnCloseAfterOnResult()
    {
        var order = new List<string>();

        var component = RenderComponent<BitMessageBox>(parameters =>
        {
            parameters.Add(p => p.OnOk, () => order.Add("ok"));
            parameters.Add(p => p.OnResult, _ => order.Add("result"));
            parameters.Add(p => p.OnClose, () => order.Add("close"));
        });

        component.Find(".bit-msb-ftr .bit-btn").Click();

        CollectionAssert.AreEqual(new[] { "ok", "result", "close" }, order);
    }



    [TestMethod]
    public void BitMessageBoxShouldDrawItsButtonsInTheNeutralColorByDefault()
    {
        var component = RenderComponent<BitMessageBox>(parameters =>
        {
            parameters.Add(p => p.Buttons, BitMessageBoxButtons.OkCancel);
        });

        foreach (var button in component.FindAll(".bit-msb-ftr .bit-btn"))
        {
            Assert.IsTrue(button.ClassList.Contains("bit-btn-ter"));
        }
    }

    [TestMethod]
    public void BitMessageBoxShouldPaintEveryActionButtonWithItsButtonColor()
    {
        var component = RenderComponent<BitMessageBox>(parameters =>
        {
            parameters.Add(p => p.Buttons, BitMessageBoxButtons.OkCancel);
            parameters.Add(p => p.ButtonColor, BitColor.Primary);
        });

        foreach (var button in component.FindAll(".bit-msb-ftr .bit-btn"))
        {
            Assert.IsTrue(button.ClassList.Contains("bit-btn-pri"));
        }
    }

    [TestMethod]
    public void BitMessageBoxShouldSingleOutTheAffirmativeButtonWithThePrimaryButtonColor()
    {
        var component = RenderComponent<BitMessageBox>(parameters =>
        {
            parameters.Add(p => p.Buttons, BitMessageBoxButtons.YesNo);
            parameters.Add(p => p.ButtonColor, BitColor.Primary);
            parameters.Add(p => p.PrimaryButtonColor, BitColor.Error);
        });

        var buttons = component.FindAll(".bit-msb-ftr .bit-btn");

        Assert.IsTrue(buttons[0].ClassList.Contains("bit-btn-err"));
        Assert.IsTrue(buttons[1].ClassList.Contains("bit-btn-pri"));
    }

    [TestMethod]
    public void BitMessageBoxShouldScaleItsButtonsWithItsSize()
    {
        var component = RenderComponent<BitMessageBox>(parameters =>
        {
            parameters.Add(p => p.Size, BitSize.Small);
            parameters.Add(p => p.Buttons, BitMessageBoxButtons.OkCancel);
        });

        foreach (var button in component.FindAll(".bit-msb .bit-btn"))
        {
            Assert.IsTrue(button.ClassList.Contains("bit-btn-sm"));
        }
    }

    [TestMethod]
    public void BitMessageBoxShouldRenderNoIconByDefault()
    {
        var component = RenderComponent<BitMessageBox>(parameters =>
        {
            parameters.Add(p => p.Title, "Title");
        });

        Assert.AreEqual(0, component.FindAll(".bit-msb-ict").Count);
    }

    [TestMethod,
        DataRow(BitColor.Info, "bit-msb-inf", "bit-icon--Info"),
        DataRow(BitColor.Success, "bit-msb-suc", "bit-icon--Completed"),
        DataRow(BitColor.Warning, "bit-msb-wrn", "bit-icon--Warning"),
        DataRow(BitColor.SevereWarning, "bit-msb-swr", "bit-icon--WarningSolid"),
        DataRow(BitColor.Error, "bit-msb-err", "bit-icon--ErrorBadge")]
    public void BitMessageBoxShouldRenderTheGlyphAndTheRoleClassOfItsColor(BitColor color, string rootClass, string iconClass)
    {
        var component = RenderComponent<BitMessageBox>(parameters =>
        {
            parameters.Add(p => p.Color, color);
        });

        Assert.IsTrue(component.Find(".bit-msb").ClassList.Contains(rootClass));
        Assert.IsTrue(component.Find(".bit-msb-ico").ClassList.Contains(iconClass));
    }

    [TestMethod]
    public void BitMessageBoxShouldLetIconNameOverrideTheGlyphOfItsColor()
    {
        var component = RenderComponent<BitMessageBox>(parameters =>
        {
            parameters.Add(p => p.Color, BitColor.Error);
            parameters.Add(p => p.IconName, "Lightbulb");
        });

        var icon = component.Find(".bit-msb-ico");

        Assert.IsTrue(icon.ClassList.Contains("bit-icon--Lightbulb"));
        Assert.IsFalse(icon.ClassList.Contains("bit-icon--ErrorBadge"));
    }

    [TestMethod]
    public void BitMessageBoxShouldRenderAnExternalIcon()
    {
        var component = RenderComponent<BitMessageBox>(parameters =>
        {
            parameters.Add(p => p.Icon, BitIconInfo.Fa("solid house"));
        });

        var icon = component.Find(".bit-msb-ico");

        Assert.IsTrue(icon.ClassList.Contains("fa-solid"));
        Assert.IsTrue(icon.ClassList.Contains("fa-house"));
    }

    [TestMethod]
    public void BitMessageBoxShouldHideItsIcon()
    {
        var component = RenderComponent<BitMessageBox>(parameters =>
        {
            parameters.Add(p => p.Color, BitColor.Error);
            parameters.Add(p => p.HideIcon, true);
        });

        Assert.AreEqual(0, component.FindAll(".bit-msb-ict").Count);
    }

    [TestMethod]
    public void BitMessageBoxShouldRenderAnIconTemplate()
    {
        var component = RenderComponent<BitMessageBox>(parameters =>
        {
            parameters.Add(p => p.IconTemplate, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "custom-icon");
                builder.CloseElement();
            }));
        });

        Assert.AreEqual(1, component.FindAll(".bit-msb-ict .custom-icon").Count);
        Assert.AreEqual(0, component.FindAll(".bit-msb-ico").Count);
    }

    [TestMethod]
    public void BitMessageBoxShouldHideItsIconFromTheReadingOrderByDefault()
    {
        var component = RenderComponent<BitMessageBox>(parameters =>
        {
            parameters.Add(p => p.Color, BitColor.Warning);
        });

        var iconContainer = component.Find(".bit-msb-ict");

        Assert.AreEqual("true", iconContainer.GetAttribute("aria-hidden"));
        Assert.IsNull(iconContainer.GetAttribute("role"));
    }

    [TestMethod]
    public void BitMessageBoxShouldNameItsIconWhenAskedTo()
    {
        var component = RenderComponent<BitMessageBox>(parameters =>
        {
            parameters.Add(p => p.Color, BitColor.Warning);
            parameters.Add(p => p.IconAriaLabel, "Warning");
        });

        var iconContainer = component.Find(".bit-msb-ict");

        Assert.AreEqual("img", iconContainer.GetAttribute("role"));
        Assert.AreEqual("Warning", iconContainer.GetAttribute("aria-label"));
        Assert.IsNull(iconContainer.GetAttribute("aria-hidden"));
    }



    [TestMethod]
    public void BitMessageBoxShouldNameItsCloseButton()
    {
        var component = RenderComponent<BitMessageBox>();

        var closeButton = component.Find(".bit-msb-hdr .bit-btn");

        Assert.AreEqual("Close", closeButton.GetAttribute("title"));
        Assert.AreEqual("Close", closeButton.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitMessageBoxShouldRenderACustomCloseButtonTitle()
    {
        var component = RenderComponent<BitMessageBox>(parameters =>
        {
            parameters.Add(p => p.CloseButtonTitle, "Dismiss");
        });

        var closeButton = component.Find(".bit-msb-hdr .bit-btn");

        Assert.AreEqual("Dismiss", closeButton.GetAttribute("title"));
        Assert.AreEqual("Dismiss", closeButton.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitMessageBoxShouldRenderACustomCloseIcon()
    {
        var component = RenderComponent<BitMessageBox>(parameters =>
        {
            parameters.Add(p => p.CloseIconName, "Cancel");
        });

        Assert.AreEqual(1, component.FindAll(".bit-msb-hdr .bit-btn .bit-icon--Cancel").Count);
    }

    [TestMethod]
    public void BitMessageBoxShouldRemoveItsCloseButton()
    {
        var component = RenderComponent<BitMessageBox>(parameters =>
        {
            parameters.Add(p => p.Title, "Title");
            parameters.Add(p => p.ShowCloseButton, false);
        });

        Assert.AreEqual(0, component.FindAll(".bit-msb-hdr .bit-btn").Count);
    }

    [TestMethod]
    public void BitMessageBoxShouldRenderNoHeaderWithNothingToPutInIt()
    {
        var component = RenderComponent<BitMessageBox>(parameters =>
        {
            parameters.Add(p => p.Body, "Only a body.");
            parameters.Add(p => p.ShowCloseButton, false);
        });

        Assert.AreEqual(0, component.FindAll(".bit-msb-hdr").Count);
    }

    [TestMethod]
    public void BitMessageBoxShouldRenderNoTitleElementWithoutATitle()
    {
        var component = RenderComponent<BitMessageBox>(parameters =>
        {
            parameters.Add(p => p.Body, "Only a body.");
        });

        Assert.AreEqual(0, component.FindAll(".bit-msb-ttl").Count);
    }

    [TestMethod]
    public void BitMessageBoxShouldRenderItsTitleAsAHeading()
    {
        var component = RenderComponent<BitMessageBox>(parameters =>
        {
            parameters.Add(p => p.Title, "Sample Title");
        });

        var title = component.Find(".bit-msb-ttl");

        Assert.AreEqual("H5", title.TagName);
    }

    [TestMethod]
    public void BitMessageBoxShouldGiveItsTitleAndBodyIdsDerivedFromItsOwn()
    {
        var component = RenderComponent<BitMessageBox>(parameters =>
        {
            parameters.Add(p => p.Id, "the-box");
            parameters.Add(p => p.Title, "Sample Title");
            parameters.Add(p => p.Body, "Sample Body");
        });

        Assert.AreEqual("the-box-ttl", component.Find(".bit-msb-ttl").Id);
        Assert.AreEqual("the-box-bdy", component.Find(".bit-msb-bdy").Id);
    }



    [TestMethod]
    public void BitMessageBoxShouldRenderABodyTemplateInsteadOfItsBody()
    {
        var component = RenderComponent<BitMessageBox>(parameters =>
        {
            parameters.Add(p => p.Body, "the string body");
            parameters.Add(p => p.BodyTemplate, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "custom-body");
                builder.CloseElement();
            }));
        });

        Assert.AreEqual(1, component.FindAll(".bit-msb-bdy .custom-body").Count);
        Assert.IsFalse(component.Markup.Contains("the string body"));
    }

    [TestMethod]
    public void BitMessageBoxShouldRenderItsChildContentAsTheBody()
    {
        var component = RenderComponent<BitMessageBox>(parameters =>
        {
            parameters.AddChildContent("<div class=\"custom-body\"></div>");
        });

        Assert.AreEqual(1, component.FindAll(".bit-msb-bdy .custom-body").Count);
    }

    [TestMethod]
    public void BitMessageBoxShouldRenderAHeaderTemplateInsteadOfItsOwnHeader()
    {
        var component = RenderComponent<BitMessageBox>(parameters =>
        {
            parameters.Add(p => p.Title, "Sample Title");
            parameters.Add(p => p.HeaderTemplate, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "custom-header");
                builder.CloseElement();
            }));
        });

        Assert.AreEqual(1, component.FindAll(".bit-msb-hdr .custom-header").Count);
        Assert.AreEqual(0, component.FindAll(".bit-msb-ttl").Count);
        Assert.AreEqual(0, component.FindAll(".bit-msb-hdr .bit-btn").Count);
    }

    [TestMethod]
    public void BitMessageBoxShouldRenderAFooterTemplateInsteadOfItsButtons()
    {
        var component = RenderComponent<BitMessageBox>(parameters =>
        {
            parameters.Add(p => p.Buttons, BitMessageBoxButtons.YesNoCancel);
            parameters.Add(p => p.FooterTemplate, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "custom-footer");
                builder.CloseElement();
            }));
        });

        Assert.AreEqual(1, component.FindAll(".bit-msb-ftr .custom-footer").Count);
        Assert.AreEqual(0, component.FindAll(".bit-msb-ftr .bit-btn").Count);
    }



    [TestMethod,
        DataRow(BitSize.Small, "bit-msb-sm"),
        DataRow(BitSize.Medium, "bit-msb-md"),
        DataRow(BitSize.Large, "bit-msb-lg")]
    public void BitMessageBoxShouldRenderTheClassOfItsSize(BitSize size, string expectedClass)
    {
        var component = RenderComponent<BitMessageBox>(parameters =>
        {
            parameters.Add(p => p.Size, size);
        });

        Assert.IsTrue(component.Find(".bit-msb").ClassList.Contains(expectedClass));
    }

    [TestMethod]
    public void BitMessageBoxShouldRenderNoSizeClassByDefault()
    {
        var component = RenderComponent<BitMessageBox>();

        var classList = component.Find(".bit-msb").ClassList;

        Assert.IsFalse(classList.Contains("bit-msb-sm"));
        Assert.IsFalse(classList.Contains("bit-msb-md"));
        Assert.IsFalse(classList.Contains("bit-msb-lg"));
    }

    [TestMethod,
        DataRow(BitDir.Ltr, "ltr"),
        DataRow(BitDir.Rtl, "rtl"),
        DataRow(BitDir.Auto, "auto")]
    public void BitMessageBoxShouldRenderItsDir(BitDir dir, string expected)
    {
        var component = RenderComponent<BitMessageBox>(parameters =>
        {
            parameters.Add(p => p.Dir, dir);
        });

        Assert.AreEqual(expected, component.Find(".bit-msb").GetAttribute("dir"));
    }



    [TestMethod]
    public void BitMessageBoxShouldApplyItsClassesToEveryPart()
    {
        var component = RenderComponent<BitMessageBox>(parameters =>
        {
            parameters.Add(p => p.Title, "Title");
            parameters.Add(p => p.Body, "Body");
            parameters.Add(p => p.Color, BitColor.Info);
            parameters.Add(p => p.Buttons, BitMessageBoxButtons.OkCancel);
            parameters.Add(p => p.Classes, new BitMessageBoxClassStyles
            {
                Root = "custom-root",
                Container = "custom-container",
                Header = "custom-header",
                IconContainer = "custom-icon-container",
                Icon = "custom-icon",
                Title = "custom-title",
                Body = "custom-body",
                Footer = "custom-footer",
                OkButton = new() { Root = "custom-ok" },
                CancelButton = new() { Root = "custom-cancel" },
                CloseButton = new() { Root = "custom-close" }
            });
        });

        Assert.IsTrue(component.Find(".bit-msb").ClassList.Contains("custom-root"));
        Assert.AreEqual(1, component.FindAll(".bit-msb-con.custom-container").Count);
        Assert.AreEqual(1, component.FindAll(".bit-msb-hdr.custom-header").Count);
        Assert.AreEqual(1, component.FindAll(".bit-msb-ict.custom-icon-container").Count);
        Assert.AreEqual(1, component.FindAll(".bit-msb-ico.custom-icon").Count);
        Assert.AreEqual(1, component.FindAll(".bit-msb-ttl.custom-title").Count);
        Assert.AreEqual(1, component.FindAll(".bit-msb-bdy.custom-body").Count);
        Assert.AreEqual(1, component.FindAll(".bit-msb-ftr.custom-footer").Count);
        Assert.AreEqual(1, component.FindAll(".bit-msb-ftr .custom-ok").Count);
        Assert.AreEqual(1, component.FindAll(".bit-msb-ftr .custom-cancel").Count);
        Assert.AreEqual(1, component.FindAll(".bit-msb-hdr .custom-close").Count);
    }

    [TestMethod]
    public void BitMessageBoxShouldApplyTheSharedActionButtonClassesToTheButtonsThatHaveNoneOfTheirOwn()
    {
        var component = RenderComponent<BitMessageBox>(parameters =>
        {
            parameters.Add(p => p.Buttons, BitMessageBoxButtons.OkCancel);
            parameters.Add(p => p.Classes, new BitMessageBoxClassStyles
            {
                ActionButton = new() { Root = "custom-action" },
                OkButton = new() { Root = "custom-ok" }
            });
        });

        // The Ok button has a set of its own, so the shared one is not what it uses.
        Assert.AreEqual(1, component.FindAll(".bit-msb-ftr .custom-ok").Count);
        Assert.AreEqual(0, component.FindAll(".bit-msb-ftr .custom-ok.custom-action").Count);
        Assert.AreEqual(1, component.FindAll(".bit-msb-ftr .custom-action").Count);
    }

    [TestMethod]
    public void BitMessageBoxShouldApplyItsStylesToEveryPart()
    {
        var component = RenderComponent<BitMessageBox>(parameters =>
        {
            parameters.Add(p => p.Title, "Title");
            parameters.Add(p => p.Body, "Body");
            parameters.Add(p => p.Styles, new BitMessageBoxClassStyles
            {
                Root = "color: red",
                Container = "color: blue",
                Header = "color: green",
                Title = "color: yellow",
                Body = "color: purple",
                Footer = "color: orange"
            });
        });

        Assert.IsTrue(component.Find(".bit-msb").GetAttribute("style")!.Contains("color: red"));
        Assert.IsTrue(component.Find(".bit-msb-con").GetAttribute("style")!.Contains("color: blue"));
        Assert.IsTrue(component.Find(".bit-msb-hdr").GetAttribute("style")!.Contains("color: green"));
        Assert.IsTrue(component.Find(".bit-msb-ttl").GetAttribute("style")!.Contains("color: yellow"));
        Assert.IsTrue(component.Find(".bit-msb-bdy").GetAttribute("style")!.Contains("color: purple"));
        Assert.IsTrue(component.Find(".bit-msb-ftr").GetAttribute("style")!.Contains("color: orange"));
    }
}
