using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Accordion;

[TestClass]
public class BitAccordionTests : BunitTestContext
{
    [TestMethod,
        DataRow(true),
        DataRow(false),
    ]
    public void BitAccordionIsEnabledTest(bool isEnabled)
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var bitAccordion = com.Find(".bit-acd");

        if (isEnabled)
        {
            Assert.IsFalse(bitAccordion.ClassList.Contains("bit-dis"));
        }
        else
        {
            Assert.IsTrue(bitAccordion.ClassList.Contains("bit-dis"));
        }
    }

    [TestMethod,
        DataRow(true),
        DataRow(false),
    ]
    public void BitAccordionIsEnabledShouldDriveTheHeaderState(bool isEnabled)
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var header = com.Find(".bit-acd-hdr");

        Assert.AreEqual(isEnabled ? null : "true", header.GetAttribute("aria-disabled"));
        Assert.AreEqual(isEnabled ? "0" : "-1", header.GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitAccordionDisabledShouldNotToggleOnClick()
    {
        var clicked = false;
        var changed = false;

        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.OnClick, () => clicked = true);
            parameters.Add(p => p.OnChange, () => changed = true);
        });

        com.Find(".bit-acd-hdr").Click();

        Assert.IsFalse(clicked);
        Assert.IsFalse(changed);
        Assert.IsFalse(com.Find(".bit-acd").ClassList.Contains("bit-acd-exp"));
        Assert.AreEqual("false", com.Find(".bit-acd-hdr").GetAttribute("aria-expanded"));
    }

    [TestMethod]
    public void BitAccordionShouldBeSetTitleAndDescriptionAndText()
    {
        var title = "title-value";
        var description = "description-value";
        var text = "text-value";

        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.Title, title);
            parameters.Add(p => p.Description, description);
            parameters.Add(p => p.ChildContent, text);
        });

        var bitAccordionTitle = com.Find(".bit-acd-ttl");
        var bitAccordionDescription = com.Find(".bit-acd-des");
        var bitAccordionText = com.Find(".bit-acd-con");

        Assert.AreEqual(bitAccordionTitle.TextContent, title);
        Assert.AreEqual(bitAccordionDescription.TextContent, description);
        Assert.AreEqual(bitAccordionText.TextContent, text);
    }

    [TestMethod]
    public void BitAccordionShouldNotRenderTheTitleWhenThereIsNothingToPutInIt()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.Description, "description-value");
        });

        Assert.AreEqual(0, com.FindAll(".bit-acd-ttl").Count);
        Assert.AreEqual("description-value", com.Find(".bit-acd-des").TextContent);
    }

    [TestMethod]
    public void BitAccordionShouldRenderTheTitleForATitleTemplateWithoutATitle()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.TitleTemplate, "template-value");
        });

        Assert.AreEqual("template-value", com.Find(".bit-acd-ttl").TextContent);
    }

    [TestMethod]
    public void BitAccordionShouldNotRenderDescriptionWhenItIsEmpty()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.Title, "title");
        });

        Assert.AreEqual(0, com.FindAll(".bit-acd-des").Count);
    }

    [TestMethod]
    public void BitAccordionShouldRenderTheBodyAliasOfChildContent()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.Body, "body-value");
        });

        Assert.AreEqual("body-value", com.Find(".bit-acd-con").TextContent);
    }

    [TestMethod]
    public void BitAccordionChildContentShouldWinOverBody()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.ChildContent, "child-value");
            parameters.Add(p => p.Body, "body-value");
        });

        Assert.AreEqual("child-value", com.Find(".bit-acd-con").TextContent);
    }

    [TestMethod]
    public void BitAccordionShouldBeExpandWhenClicked()
    {
        var com = RenderComponent<BitAccordion>();

        com.Find(".bit-acd-hdr").Click();

        Assert.IsTrue(com.Find(".bit-acd-eiw").ClassList.Contains("bit-ico--r180"));
        Assert.IsTrue(com.Find(".bit-acd-con").ClassList.Contains("bit-acd-cex"));
        Assert.IsTrue(com.Find(".bit-acd").ClassList.Contains("bit-acd-exp"));
    }

    [TestMethod]
    public void BitAccordionShouldBeCollapsedOnTheSecondClick()
    {
        var com = RenderComponent<BitAccordion>();

        com.Find(".bit-acd-hdr").Click();
        com.Find(".bit-acd-hdr").Click();

        Assert.IsFalse(com.Find(".bit-acd").ClassList.Contains("bit-acd-exp"));
        Assert.IsTrue(com.Find(".bit-acd-con").ClassList.Contains("bit-acd-cco"));
        Assert.AreEqual("false", com.Find(".bit-acd-hdr").GetAttribute("aria-expanded"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitAccordionShouldBeSetDefaultIsExpanded(bool defaultIsExpanded)
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.DefaultIsExpanded, defaultIsExpanded);
        });

        var expanderIconWrapper = com.Find(".bit-acd-eiw");
        var content = com.Find(".bit-acd-con");

        Assert.AreEqual(defaultIsExpanded, expanderIconWrapper?.ClassName?.Contains("bit-ico--r180"));
        Assert.AreEqual(defaultIsExpanded, content?.ClassName?.Contains("bit-acd-cex"));
    }

    [TestMethod]
    public void BitAccordionIsExpandedShouldWinOverDefaultIsExpanded()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.DefaultIsExpanded, true);
            parameters.Add(p => p.IsExpanded, false);
        });

        Assert.IsFalse(com.Find(".bit-acd").ClassList.Contains("bit-acd-exp"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitAccordionShouldBeOnClickAndOnChange(bool isClick)
    {
        var isClicked = !isClick;
        var isChanged = !isClick;

        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.OnClick, () => isClicked = isClick);
            parameters.Add(p => p.OnChange, () => isChanged = isClick);
        });

        var bitAccordionHeader = com.Find(".bit-acd-hdr");
        bitAccordionHeader.Click();

        Assert.AreEqual(isClick, isClicked);
        Assert.AreEqual(isClick, isChanged);
    }

    [TestMethod]
    public void BitAccordionOnChangeShouldReportTheNewState()
    {
        var values = new List<bool>();

        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.OnChange, (bool v) => values.Add(v));
        });

        com.Find(".bit-acd-hdr").Click();
        com.Find(".bit-acd-hdr").Click();

        Assert.AreEqual(2, values.Count);
        Assert.IsTrue(values[0]);
        Assert.IsFalse(values[1]);
    }

    [TestMethod]
    public void BitAccordionShouldRaiseOnExpandAndOnCollapse()
    {
        var expanded = 0;
        var collapsed = 0;

        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.OnExpand, () => expanded++);
            parameters.Add(p => p.OnCollapse, () => collapsed++);
        });

        com.Find(".bit-acd-hdr").Click();

        Assert.AreEqual(1, expanded);
        Assert.AreEqual(0, collapsed);

        com.Find(".bit-acd-hdr").Click();

        Assert.AreEqual(1, expanded);
        Assert.AreEqual(1, collapsed);
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitAccordionShouldBeSetHeaderTemplate(bool defaultIsExpanded)
    {
        var expandedHeaderHtml = "<h1>Expanded</h1>";
        var collapsedHeaderHtml = "<h1>Collapsed</h1>";

        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.DefaultIsExpanded, defaultIsExpanded);
            parameters.Add(p => p.HeaderTemplate, (isExpanded) => isExpanded ? expandedHeaderHtml : collapsedHeaderHtml);
        });

        var header = com.Find(".bit-acd-hdr");

        Assert.AreEqual(defaultIsExpanded, header.InnerHtml.Equals(expandedHeaderHtml));
        Assert.AreEqual(!defaultIsExpanded, header.InnerHtml.Equals(collapsedHeaderHtml));
    }

    [TestMethod]
    public void BitAccordionTitleTemplateShouldKeepTheRestOfTheHeader()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.Title, "title-value");
            parameters.Add(p => p.Description, "description-value");
            parameters.Add(p => p.IconName, "Settings");
            parameters.Add(p => p.TitleTemplate, "<span id=\"tpl\">template-value</span>");
        });

        Assert.AreEqual("template-value", com.Find(".bit-acd-ttl").TextContent);
        Assert.AreEqual("tpl", com.Find(".bit-acd-ttl").FirstElementChild!.Id);
        Assert.AreEqual("description-value", com.Find(".bit-acd-des").TextContent);
        Assert.AreEqual(1, com.FindAll(".bit-acd-ico").Count);
        Assert.AreEqual(1, com.FindAll(".bit-acd-eiw").Count);
    }

    [TestMethod]
    public void BitAccordionHeaderTemplateShouldWinOverTitleTemplate()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.TitleTemplate, "title-template");
            parameters.Add(p => p.HeaderTemplate, _ => "header-template");
        });

        Assert.AreEqual("header-template", com.Find(".bit-acd-hdr").TextContent);
        Assert.AreEqual(0, com.FindAll(".bit-acd-ttl").Count);
    }

    [TestMethod]
    public void BitAccordionShouldBeSetContentTemplate()
    {
        var contentHtml = "<h1>ContentTemplate</h1>";

        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.ChildContent, contentHtml);
        });

        var content = com.Find(".bit-acd-con");

        Assert.IsTrue(content.InnerHtml.Equals(contentHtml));
    }



    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitAccordionShouldAlwaysRenderAriaExpanded(bool isExpanded)
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.DefaultIsExpanded, isExpanded);
        });

        Assert.AreEqual(isExpanded ? "true" : "false", com.Find(".bit-acd-hdr").GetAttribute("aria-expanded"));
    }

    [TestMethod]
    public void BitAccordionShouldWireTheHeaderAndThePanelTogether()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.Id, "acd-id");
        });

        var header = com.Find(".bit-acd-hdr");
        var content = com.Find(".bit-acd-con");

        Assert.AreEqual("acd-id-hdr", header.GetAttribute("id"));
        Assert.AreEqual("acd-id-cnt", content.GetAttribute("id"));
        Assert.AreEqual(content.GetAttribute("id"), header.GetAttribute("aria-controls"));
        Assert.AreEqual(header.GetAttribute("id"), content.GetAttribute("aria-labelledby"));
        Assert.AreEqual("region", content.GetAttribute("role"));
    }

    [TestMethod]
    public void BitAccordionShouldWireTheHeaderAndThePanelTogetherWithoutAnId()
    {
        var com = RenderComponent<BitAccordion>();

        var header = com.Find(".bit-acd-hdr");
        var content = com.Find(".bit-acd-con");

        Assert.IsFalse(string.IsNullOrEmpty(header.GetAttribute("id")));
        Assert.AreEqual(content.GetAttribute("id"), header.GetAttribute("aria-controls"));
        Assert.AreEqual(header.GetAttribute("id"), content.GetAttribute("aria-labelledby"));
    }

    [TestMethod]
    public void BitAccordionShouldNameTheHeaderWithTheHeaderAriaLabel()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.HeaderAriaLabel, "header-label");
            parameters.Add(p => p.AriaLabel, "root-label");
        });

        Assert.AreEqual("header-label", com.Find(".bit-acd-hdr").GetAttribute("aria-label"));
        Assert.AreEqual("root-label", com.Find(".bit-acd").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitAccordionShouldNotRenderTheHeaderAriaLabelWithoutIt()
    {
        var com = RenderComponent<BitAccordion>();

        Assert.IsNull(com.Find(".bit-acd-hdr").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitAccordionShouldWrapTheHeaderInAHeading()
    {
        var com = RenderComponent<BitAccordion>();

        var heading = com.Find(".bit-acd-hed");

        Assert.AreEqual("heading", heading.GetAttribute("role"));
        Assert.AreEqual("3", heading.GetAttribute("aria-level"));
        Assert.AreEqual("BUTTON", heading.FirstElementChild!.TagName);
    }

    [TestMethod,
        DataRow(1, "1"),
        DataRow(2, "2"),
        DataRow(6, "6"),
        DataRow(0, "1"),
        DataRow(-3, "1"),
        DataRow(9, "6")
    ]
    public void BitAccordionShouldClampTheHeadingLevel(int headingLevel, string expected)
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.HeadingLevel, headingLevel);
        });

        Assert.AreEqual(expected, com.Find(".bit-acd-hed").GetAttribute("aria-level"));
    }

    [TestMethod]
    public void BitAccordionCollapsedContentShouldNotBeFocusable()
    {
        var com = RenderComponent<BitAccordion>();

        Assert.IsNull(com.Find(".bit-acd-con").GetAttribute("tabindex"));

        com.Find(".bit-acd-hdr").Click();

        Assert.IsNull(com.Find(".bit-acd-con").GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitAccordionScrollingContentShouldBeFocusableWhileExpanded()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.MaxHeight, "10rem");
        });

        Assert.IsNull(com.Find(".bit-acd-con").GetAttribute("tabindex"));

        com.Find(".bit-acd-hdr").Click();

        Assert.AreEqual("0", com.Find(".bit-acd-con").GetAttribute("tabindex"));
    }



    [TestMethod,
        DataRow(BitColorKind.Primary, "bit-acd-pbg"),
        DataRow(BitColorKind.Secondary, "bit-acd-sbg"),
        DataRow(BitColorKind.Tertiary, "bit-acd-tbg"),
        DataRow(BitColorKind.Transparent, "bit-acd-rbg")
    ]
    public void BitAccordionShouldRenderTheBackgroundClass(BitColorKind background, string expectedClass)
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.Background, background);
        });

        Assert.IsTrue(com.Find(".bit-acd").ClassList.Contains(expectedClass));
    }

    [TestMethod,
        DataRow(BitColorKind.Primary, "bit-acd-pbr"),
        DataRow(BitColorKind.Secondary, "bit-acd-sbr"),
        DataRow(BitColorKind.Tertiary, "bit-acd-tbr"),
        DataRow(BitColorKind.Transparent, "bit-acd-rbr")
    ]
    public void BitAccordionShouldRenderTheBorderClass(BitColorKind border, string expectedClass)
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.Border, border);
        });

        Assert.IsTrue(com.Find(".bit-acd").ClassList.Contains(expectedClass));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitAccordionShouldRenderTheNoBorderClass(bool noBorder)
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.NoBorder, noBorder);
        });

        Assert.AreEqual(noBorder, com.Find(".bit-acd").ClassList.Contains("bit-acd-nbd"));
    }

    [TestMethod,
        DataRow(BitSize.Small, "bit-acd-sm"),
        DataRow(BitSize.Medium, "bit-acd-md"),
        DataRow(BitSize.Large, "bit-acd-lg"),
        DataRow(null, "bit-acd-md")
    ]
    public void BitAccordionShouldRenderTheSizeClass(BitSize? size, string expectedClass)
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.Size, size);
        });

        Assert.IsTrue(com.Find(".bit-acd").ClassList.Contains(expectedClass));
    }

    [TestMethod,
        DataRow(BitIconPosition.Start, true),
        DataRow(BitIconPosition.End, false),
        DataRow(null, false)
    ]
    public void BitAccordionShouldRenderTheExpanderIconPositionClass(BitIconPosition? position, bool expected)
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.ExpanderIconPosition, position);
        });

        Assert.AreEqual(expected, com.Find(".bit-acd").ClassList.Contains("bit-acd-sei"));
    }



    [TestMethod]
    public void BitAccordionShouldRenderTheDefaultExpanderIcon()
    {
        var com = RenderComponent<BitAccordion>();

        var icon = com.Find(".bit-acd-eic");

        Assert.IsTrue(icon.ClassList.Contains("bit-icon--ChevronRight"));
        Assert.IsTrue(icon.ClassList.Contains("bit-ico-r90"));
    }

    [TestMethod]
    public void BitAccordionShouldRenderTheExpanderIconName()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.ExpanderIconName, "ChevronDown");
        });

        Assert.IsTrue(com.Find(".bit-acd-eic").ClassList.Contains("bit-icon--ChevronDown"));
    }

    [TestMethod]
    public void BitAccordionExpanderIconShouldWinOverExpanderIconName()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.ExpanderIconName, "ChevronDown");
            parameters.Add(p => p.ExpanderIcon, BitIconInfo.Css("fa-solid fa-chevron-down"));
        });

        var icon = com.Find(".bit-acd-eic");

        Assert.IsTrue(icon.ClassList.Contains("fa-chevron-down"));
        Assert.IsFalse(icon.ClassList.Contains("bit-icon--ChevronDown"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitAccordionShouldHideTheExpanderIcon(bool hideExpanderIcon)
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.HideExpanderIcon, hideExpanderIcon);
        });

        Assert.AreEqual(hideExpanderIcon ? 0 : 1, com.FindAll(".bit-acd-eiw").Count);
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitAccordionShouldNotRotateTheExpanderIconWhenAsked(bool noExpanderRotation)
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.DefaultIsExpanded, true);
            parameters.Add(p => p.NoExpanderRotation, noExpanderRotation);
        });

        Assert.AreEqual(!noExpanderRotation, com.Find(".bit-acd-eiw").ClassList.Contains("bit-ico--r180"));
    }

    [TestMethod]
    public void BitAccordionShouldSwapTheExpanderIconWhileExpanded()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.ExpanderIconName, "Add");
            parameters.Add(p => p.ExpandedExpanderIconName, "Remove");
        });

        Assert.IsTrue(com.Find(".bit-acd-eic").ClassList.Contains("bit-icon--Add"));

        com.Find(".bit-acd-hdr").Click();

        Assert.IsTrue(com.Find(".bit-acd-eic").ClassList.Contains("bit-icon--Remove"));
        Assert.IsFalse(com.Find(".bit-acd-eiw").ClassList.Contains("bit-ico--r180"));
    }

    [TestMethod]
    public void BitAccordionExpandedExpanderIconShouldWinOverItsName()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.DefaultIsExpanded, true);
            parameters.Add(p => p.ExpandedExpanderIconName, "Remove");
            parameters.Add(p => p.ExpandedExpanderIcon, BitIconInfo.Css("fa-solid fa-minus"));
        });

        Assert.IsTrue(com.Find(".bit-acd-eic").ClassList.Contains("fa-minus"));
    }

    [TestMethod]
    public void BitAccordionShouldRenderTheHeaderIcon()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.IconName, "Settings");
        });

        Assert.IsTrue(com.Find(".bit-acd-ico").ClassList.Contains("bit-icon--Settings"));
    }

    [TestMethod]
    public void BitAccordionIconShouldWinOverIconName()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.IconName, "Settings");
            parameters.Add(p => p.Icon, BitIconInfo.Css("fa-solid fa-gear"));
        });

        Assert.IsTrue(com.Find(".bit-acd-ico").ClassList.Contains("fa-gear"));
    }

    [TestMethod]
    public void BitAccordionShouldNotRenderTheHeaderIconByDefault()
    {
        var com = RenderComponent<BitAccordion>();

        Assert.AreEqual(0, com.FindAll(".bit-acd-ico").Count);
    }



    [TestMethod]
    public void BitAccordionShouldRenderTheActionsOutsideOfTheHeading()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.Title, "title");
            parameters.Add(p => p.Actions, "<button id=\"act\">act</button>");
        });

        var actions = com.Find(".bit-acd-act");

        Assert.AreEqual("act", actions.FirstElementChild!.Id);
        Assert.IsNull(actions.Closest(".bit-acd-hed"));
        Assert.IsNull(actions.Closest("button"));
        Assert.AreEqual("title", com.Find(".bit-acd-hed").TextContent.Trim());
    }

    [TestMethod]
    public void BitAccordionShouldNotRenderTheActionsWhenNotGiven()
    {
        var com = RenderComponent<BitAccordion>();

        Assert.AreEqual(0, com.FindAll(".bit-acd-act").Count);
    }



    [TestMethod]
    public void BitAccordionShouldRenderTheContentByDefault()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.ChildContent, "content");
        });

        Assert.AreEqual("content", com.Find(".bit-acd-con").TextContent);
    }

    [TestMethod]
    public void BitAccordionLazyContentShouldRenderOnTheFirstExpandAndStay()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.LazyContent, true);
            parameters.Add(p => p.ChildContent, "content");
        });

        Assert.AreEqual(string.Empty, com.Find(".bit-acd-con").TextContent);

        com.Find(".bit-acd-hdr").Click();

        Assert.AreEqual("content", com.Find(".bit-acd-con").TextContent);

        com.Find(".bit-acd-hdr").Click();

        Assert.AreEqual("content", com.Find(".bit-acd-con").TextContent);
    }

    [TestMethod]
    public void BitAccordionLazyContentShouldRenderWhenExpandedFromTheStart()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.LazyContent, true);
            parameters.Add(p => p.DefaultIsExpanded, true);
            parameters.Add(p => p.ChildContent, "content");
        });

        Assert.AreEqual("content", com.Find(".bit-acd-con").TextContent);
    }

    [TestMethod]
    public void BitAccordionUnmountOnCollapseShouldDropTheContentOnEveryCollapse()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.UnmountOnCollapse, true);
            parameters.Add(p => p.ChildContent, "content");
        });

        Assert.AreEqual(string.Empty, com.Find(".bit-acd-con").TextContent);

        com.Find(".bit-acd-hdr").Click();

        Assert.AreEqual("content", com.Find(".bit-acd-con").TextContent);

        com.Find(".bit-acd-hdr").Click();

        Assert.AreEqual(string.Empty, com.Find(".bit-acd-con").TextContent);
    }

    [TestMethod]
    public void BitAccordionUnmountOnCollapseShouldWinOverLazyContent()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.LazyContent, true);
            parameters.Add(p => p.UnmountOnCollapse, true);
            parameters.Add(p => p.ChildContent, "content");
        });

        com.Find(".bit-acd-hdr").Click();
        com.Find(".bit-acd-hdr").Click();

        Assert.AreEqual(string.Empty, com.Find(".bit-acd-con").TextContent);
    }



    [TestMethod]
    public void BitAccordionShouldRenderTheMaxHeightVariable()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.MaxHeight, "10rem");
        });

        var root = com.Find(".bit-acd");

        Assert.IsTrue(root.ClassList.Contains("bit-acd-mxh"));
        Assert.IsTrue(root.GetAttribute("style")!.Contains("--bit-acd-max-h:10rem"));
    }

    [TestMethod]
    public void BitAccordionShouldNotRenderTheMaxHeightVariableWithoutIt()
    {
        var com = RenderComponent<BitAccordion>();

        var root = com.Find(".bit-acd");

        Assert.IsFalse(root.ClassList.Contains("bit-acd-mxh"));
        Assert.IsFalse((root.GetAttribute("style") ?? string.Empty).Contains("--bit-acd-max-h"));
    }

    [TestMethod,
        DataRow(0, "--bit-acd-dur-full:0ms"),
        DataRow(500, "--bit-acd-dur-full:500ms"),
        DataRow(-5, "--bit-acd-dur-full:0ms")
    ]
    public void BitAccordionShouldRenderTheTransitionDurationVariable(int duration, string expected)
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.TransitionDuration, duration);
        });

        Assert.IsTrue(com.Find(".bit-acd").GetAttribute("style")!.Contains(expected));
    }

    [TestMethod]
    public void BitAccordionShouldNotRenderTheTransitionDurationVariableWithoutIt()
    {
        var com = RenderComponent<BitAccordion>();

        Assert.IsFalse((com.Find(".bit-acd").GetAttribute("style") ?? string.Empty).Contains("--bit-acd-dur-full"));
    }



    [TestMethod]
    public void BitAccordionShouldRespectTheTwoWayBinding()
    {
        var isExpanded = false;

        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Bind(p => p.IsExpanded, isExpanded, v => isExpanded = v);
        });

        com.Find(".bit-acd-hdr").Click();

        Assert.IsTrue(isExpanded);
        Assert.IsTrue(com.Find(".bit-acd").ClassList.Contains("bit-acd-exp"));
    }

    [TestMethod]
    public void BitAccordionControlledShouldNotToggleItself()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.IsExpanded, false);
        });

        com.Find(".bit-acd-hdr").Click();

        Assert.IsFalse(com.Find(".bit-acd").ClassList.Contains("bit-acd-exp"));
    }

    [TestMethod]
    public void BitAccordionControlledShouldFollowTheParameter()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.IsExpanded, false);
        });

        com.Render(parameters => parameters.Add(p => p.IsExpanded, true));

        Assert.IsTrue(com.Find(".bit-acd").ClassList.Contains("bit-acd-exp"));
        Assert.AreEqual("true", com.Find(".bit-acd-hdr").GetAttribute("aria-expanded"));
    }



    [TestMethod]
    public async Task BitAccordionExpandShouldExpandTheAccordion()
    {
        var expanded = 0;
        var changes = new List<bool>();

        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.OnExpand, () => expanded++);
            parameters.Add(p => p.OnChange, (bool v) => changes.Add(v));
        });

        await com.InvokeAsync(() => com.Instance.Expand());

        Assert.IsTrue(com.Find(".bit-acd").ClassList.Contains("bit-acd-exp"));
        Assert.AreEqual(1, expanded);
        Assert.AreEqual(1, changes.Count);
        Assert.IsTrue(changes[0]);

        await com.InvokeAsync(() => com.Instance.Expand());

        Assert.AreEqual(1, expanded);
        Assert.AreEqual(1, changes.Count);
    }

    [TestMethod]
    public async Task BitAccordionCollapseShouldCollapseTheAccordion()
    {
        var collapsed = 0;

        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.DefaultIsExpanded, true);
            parameters.Add(p => p.OnCollapse, () => collapsed++);
        });

        await com.InvokeAsync(() => com.Instance.Collapse());

        Assert.IsFalse(com.Find(".bit-acd").ClassList.Contains("bit-acd-exp"));
        Assert.AreEqual(1, collapsed);

        await com.InvokeAsync(() => com.Instance.Collapse());

        Assert.AreEqual(1, collapsed);
    }

    [TestMethod]
    public async Task BitAccordionToggleShouldFlipTheAccordion()
    {
        var com = RenderComponent<BitAccordion>();

        await com.InvokeAsync(() => com.Instance.Toggle());
        Assert.IsTrue(com.Find(".bit-acd").ClassList.Contains("bit-acd-exp"));

        await com.InvokeAsync(() => com.Instance.Toggle());
        Assert.IsFalse(com.Find(".bit-acd").ClassList.Contains("bit-acd-exp"));
    }

    [TestMethod]
    public async Task BitAccordionToggleShouldWorkOnADisabledAccordion()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
        });

        await com.InvokeAsync(() => com.Instance.Toggle());

        Assert.IsTrue(com.Find(".bit-acd").ClassList.Contains("bit-acd-exp"));
    }

    [TestMethod]
    public async Task BitAccordionToggleShouldWriteBackToTheBinding()
    {
        var isExpanded = false;

        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Bind(p => p.IsExpanded, isExpanded, v => isExpanded = v);
        });

        await com.InvokeAsync(() => com.Instance.Toggle());

        Assert.IsTrue(isExpanded);
    }



    [TestMethod,
        DataRow(BitDir.Rtl, "rtl"),
        DataRow(BitDir.Ltr, "ltr"),
        DataRow(BitDir.Auto, "auto")
    ]
    public void BitAccordionShouldRenderTheDir(BitDir dir, string expected)
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.Dir, dir);
        });

        Assert.AreEqual(expected, com.Find(".bit-acd").GetAttribute("dir"));
    }

    [TestMethod,
        DataRow(BitVisibility.Visible, false, false),
        DataRow(BitVisibility.Hidden, true, false),
        DataRow(BitVisibility.Collapsed, false, true)
    ]
    public void BitAccordionShouldRespectVisibility(BitVisibility visibility, bool isHidden, bool isCollapsed)
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.Visibility, visibility);
        });

        var style = com.Find(".bit-acd").GetAttribute("style") ?? string.Empty;

        Assert.AreEqual(isHidden, style.Contains("visibility:hidden"));
        Assert.AreEqual(isCollapsed, style.Contains("display:none"));
    }

    [TestMethod]
    public void BitAccordionShouldRenderTheAriaLabel()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, "label-value");
        });

        Assert.AreEqual("label-value", com.Find(".bit-acd").GetAttribute("aria-label"));
    }



    [TestMethod]
    public void BitAccordionShouldApplyTheClasses()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.Title, "title");
            parameters.Add(p => p.Description, "description");
            parameters.Add(p => p.IconName, "Settings");
            parameters.Add(p => p.Actions, "act");
            parameters.Add(p => p.Classes, new BitAccordionClassStyles
            {
                Root = "custom-root",
                HeaderWrapper = "custom-header-wrapper",
                Heading = "custom-heading",
                Header = "custom-header",
                Icon = "custom-icon",
                HeaderContent = "custom-header-content",
                Title = "custom-title",
                Description = "custom-description",
                ExpanderIconWrapper = "custom-expander-wrapper",
                ExpanderIcon = "custom-expander-icon",
                Actions = "custom-actions",
                ContentContainer = "custom-content-container",
                ContentWrapper = "custom-content-wrapper",
                Content = "custom-content"
            });
        });

        Assert.IsTrue(com.Find(".bit-acd").ClassList.Contains("custom-root"));
        Assert.IsTrue(com.Find(".bit-acd-hwr").ClassList.Contains("custom-header-wrapper"));
        Assert.IsTrue(com.Find(".bit-acd-hed").ClassList.Contains("custom-heading"));
        Assert.IsTrue(com.Find(".bit-acd-hdr").ClassList.Contains("custom-header"));
        Assert.IsTrue(com.Find(".bit-acd-ico").ClassList.Contains("custom-icon"));
        Assert.IsTrue(com.Find(".bit-acd-wrp").ClassList.Contains("custom-header-content"));
        Assert.IsTrue(com.Find(".bit-acd-ttl").ClassList.Contains("custom-title"));
        Assert.IsTrue(com.Find(".bit-acd-des").ClassList.Contains("custom-description"));
        Assert.IsTrue(com.Find(".bit-acd-eiw").ClassList.Contains("custom-expander-wrapper"));
        Assert.IsTrue(com.Find(".bit-acd-eic").ClassList.Contains("custom-expander-icon"));
        Assert.IsTrue(com.Find(".bit-acd-act").ClassList.Contains("custom-actions"));
        Assert.IsTrue(com.Find(".bit-acd-cnt").ClassList.Contains("custom-content-container"));
        Assert.IsTrue(com.Find(".bit-acd-cwr").ClassList.Contains("custom-content-wrapper"));
        Assert.IsTrue(com.Find(".bit-acd-con").ClassList.Contains("custom-content"));
    }

    [TestMethod]
    public void BitAccordionShouldApplyTheExpandedClasses()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.DefaultIsExpanded, true);
            parameters.Add(p => p.Classes, new BitAccordionClassStyles
            {
                Expanded = "custom-expanded",
                ExpandedIcon = "custom-expanded-icon"
            });
        });

        Assert.IsTrue(com.Find(".bit-acd").ClassList.Contains("custom-expanded"));
        Assert.IsTrue(com.Find(".bit-acd-eic").ClassList.Contains("custom-expanded-icon"));
    }

    [TestMethod]
    public void BitAccordionShouldNotApplyTheExpandedClassesWhileCollapsed()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.Classes, new BitAccordionClassStyles
            {
                Expanded = "custom-expanded",
                ExpandedIcon = "custom-expanded-icon"
            });
        });

        Assert.IsFalse(com.Find(".bit-acd").ClassList.Contains("custom-expanded"));
        Assert.IsFalse(com.Find(".bit-acd-eic").ClassList.Contains("custom-expanded-icon"));
    }

    [TestMethod]
    public void BitAccordionShouldApplyTheStyles()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.Title, "title");
            parameters.Add(p => p.Description, "description");
            parameters.Add(p => p.IconName, "Settings");
            parameters.Add(p => p.Actions, "act");
            parameters.Add(p => p.Styles, new BitAccordionClassStyles
            {
                Root = "color: red;",
                HeaderWrapper = "color: blue;",
                Heading = "color: green;",
                Header = "color: yellow;",
                Icon = "color: pink;",
                HeaderContent = "color: purple;",
                Title = "color: orange;",
                Description = "color: brown;",
                ExpanderIconWrapper = "color: gray;",
                ExpanderIcon = "color: teal;",
                Actions = "color: olive;",
                ContentContainer = "color: navy;",
                ContentWrapper = "color: lime;",
                Content = "color: aqua;"
            });
        });

        Assert.IsTrue(com.Find(".bit-acd").GetAttribute("style")!.Contains("color: red;"));
        Assert.AreEqual("color: blue;", com.Find(".bit-acd-hwr").GetAttribute("style"));
        Assert.AreEqual("color: green;", com.Find(".bit-acd-hed").GetAttribute("style"));
        Assert.AreEqual("color: yellow;", com.Find(".bit-acd-hdr").GetAttribute("style"));
        Assert.AreEqual("color: pink;", com.Find(".bit-acd-ico").GetAttribute("style"));
        Assert.AreEqual("color: purple;", com.Find(".bit-acd-wrp").GetAttribute("style"));
        Assert.AreEqual("color: orange;", com.Find(".bit-acd-ttl").GetAttribute("style"));
        Assert.AreEqual("color: brown;", com.Find(".bit-acd-des").GetAttribute("style"));
        Assert.AreEqual("color: gray;", com.Find(".bit-acd-eiw").GetAttribute("style"));
        Assert.AreEqual("color: teal;", com.Find(".bit-acd-eic").GetAttribute("style"));
        Assert.AreEqual("color: olive;", com.Find(".bit-acd-act").GetAttribute("style"));
        Assert.AreEqual("color: navy;", com.Find(".bit-acd-cnt").GetAttribute("style"));
        Assert.AreEqual("color: lime;", com.Find(".bit-acd-cwr").GetAttribute("style"));
        Assert.AreEqual("color: aqua;", com.Find(".bit-acd-con").GetAttribute("style"));
    }

    [TestMethod]
    public void BitAccordionShouldJoinTheExpanderIconStyles()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.DefaultIsExpanded, true);
            parameters.Add(p => p.Styles, new BitAccordionClassStyles
            {
                ExpanderIcon = "color: teal",
                ExpandedIcon = "font-size: 2rem"
            });
        });

        var style = com.Find(".bit-acd-eic").GetAttribute("style")!;

        Assert.IsTrue(style.Contains("color: teal"));
        Assert.IsTrue(style.Contains("font-size: 2rem"));
        Assert.IsTrue(style.Contains(';'));
    }

    [TestMethod]
    public void BitAccordionShouldApplyTheStyleAndClass()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.Style, "color: red;");
            parameters.Add(p => p.Class, "custom-class");
        });

        var root = com.Find(".bit-acd");

        Assert.IsTrue(root.GetAttribute("style")!.Contains("color: red;"));
        Assert.IsTrue(root.ClassList.Contains("custom-class"));
    }



    [TestMethod]
    public void BitAccordionShouldNestTheContentInTheAnimatedGrid()
    {
        var com = RenderComponent<BitAccordion>();

        var container = com.Find(".bit-acd-cnt");
        var wrapper = container.FirstElementChild!;
        var content = wrapper.FirstElementChild!;

        Assert.IsTrue(wrapper.ClassList.Contains("bit-acd-cwr"));
        Assert.IsTrue(content.ClassList.Contains("bit-acd-con"));
        Assert.AreEqual(1, container.ChildElementCount);
        Assert.AreEqual(1, wrapper.ChildElementCount);
    }

    // The stylesheet chains every state rule of the root down through these two children rather than reaching
    // for a descendant, so that an accordion nested in a panel does not inherit the state of the one holding
    // it. bUnit does not evaluate CSS, so what is guarded here is the shape those selectors are written for.
    [TestMethod]
    public void BitAccordionShouldKeepTheHeaderAndTheContentAsDirectChildrenOfTheRoot()
    {
        var com = RenderComponent<BitAccordion>();

        var root = com.Find(".bit-acd");

        Assert.AreEqual(2, root.ChildElementCount);
        Assert.IsTrue(root.Children[0].ClassList.Contains("bit-acd-hwr"));
        Assert.IsTrue(root.Children[1].ClassList.Contains("bit-acd-cnt"));
    }

    [TestMethod]
    public void BitAccordionHeaderTemplateShouldReplaceTheExpanderIcon()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.Title, "title");
            parameters.Add(p => p.HeaderTemplate, _ => "header");
        });

        Assert.AreEqual(0, com.FindAll(".bit-acd-eiw").Count);
        Assert.AreEqual(0, com.FindAll(".bit-acd-ttl").Count);
    }

    [TestMethod]
    public void BitAccordionShouldRenderTheActionsBesideAHeaderTemplate()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.HeaderTemplate, _ => "header");
            parameters.Add(p => p.Actions, "actions");
        });

        Assert.AreEqual("header", com.Find(".bit-acd-hdr").TextContent);
        Assert.AreEqual("actions", com.Find(".bit-acd-act").TextContent);
    }

    [TestMethod]
    public void BitAccordionShouldRenderBothOfTheCssVariablesTogether()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.MaxHeight, "10rem");
            parameters.Add(p => p.TransitionDuration, 250);
        });

        var style = com.Find(".bit-acd").GetAttribute("style")!;

        Assert.IsTrue(style.Contains("--bit-acd-max-h:10rem"));
        Assert.IsTrue(style.Contains("--bit-acd-dur-full:250ms"));
    }

    [TestMethod]
    public void BitAccordionLazyContentShouldFollowAParameterDrivenExpansion()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.LazyContent, true);
            parameters.Add(p => p.IsExpanded, false);
            parameters.Add(p => p.ChildContent, "content");
        });

        Assert.AreEqual(string.Empty, com.Find(".bit-acd-con").TextContent);

        com.Render(parameters => parameters.Add(p => p.IsExpanded, true));

        Assert.AreEqual("content", com.Find(".bit-acd-con").TextContent);

        com.Render(parameters => parameters.Add(p => p.IsExpanded, false));

        Assert.AreEqual("content", com.Find(".bit-acd-con").TextContent);
    }

    [TestMethod]
    public async Task BitAccordionToggleShouldNotChangeAControlledAccordion()
    {
        var expanded = 0;

        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.IsExpanded, false);
            parameters.Add(p => p.OnExpand, () => expanded++);
        });

        await com.InvokeAsync(() => com.Instance.Toggle());

        Assert.AreEqual(0, expanded);
        Assert.IsFalse(com.Find(".bit-acd").ClassList.Contains("bit-acd-exp"));
    }

    [TestMethod]
    public void BitAccordionShouldHandTheMouseEventArgsToOnClick()
    {
        MouseEventArgs? args = null;

        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.OnClick, (MouseEventArgs e) => args = e);
        });

        com.Find(".bit-acd-hdr").Click(new MouseEventArgs { Detail = 2 });

        Assert.IsNotNull(args);
        Assert.AreEqual(2, args!.Detail);
    }

    [TestMethod]
    public void BitAccordionShouldKeepNestedAccordionsIndependent()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.Title, "outer");
            parameters.Add(p => p.ChildContent, (RenderFragment)(inner =>
            {
                inner.OpenComponent<BitAccordion>(0);
                inner.CloseComponent();
            }));
        });

        var headers = com.FindAll(".bit-acd-hdr");
        Assert.AreEqual(2, headers.Count);

        headers[1].Click();

        var roots = com.FindAll(".bit-acd");
        Assert.IsFalse(roots[0].ClassList.Contains("bit-acd-exp"));
        Assert.IsTrue(roots[1].ClassList.Contains("bit-acd-exp"));
    }

    [TestMethod]
    public void BitAccordionShouldTakeTheCascadingDir()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.AddCascadingValue(BitDir.Rtl);
        });

        Assert.AreEqual("rtl", com.Find(".bit-acd").GetAttribute("dir"));
        Assert.IsTrue(com.Find(".bit-acd").ClassList.Contains("bit-rtl"));
    }



    [TestMethod]
    public void BitAccordionShouldGiveThePanelTheRegionRoleByDefault()
    {
        var com = RenderComponent<BitAccordion>();

        var header = com.Find(".bit-acd-hdr");
        var content = com.Find(".bit-acd-con");

        Assert.AreEqual("region", content.GetAttribute("role"));
        Assert.AreEqual(header.GetAttribute("id"), content.GetAttribute("aria-labelledby"));
    }

    [TestMethod]
    public void BitAccordionNoContentRegionShouldDropTheRoleAndItsLabel()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.NoContentRegion, true);
        });

        var header = com.Find(".bit-acd-hdr");
        var content = com.Find(".bit-acd-con");

        Assert.IsNull(content.GetAttribute("role"));
        Assert.IsNull(content.GetAttribute("aria-labelledby"));

        // The panel is still the element the header controls, so the wiring that does not depend on the role
        // has to survive it.
        Assert.IsFalse(string.IsNullOrEmpty(content.GetAttribute("id")));
        Assert.AreEqual(content.GetAttribute("id"), header.GetAttribute("aria-controls"));
    }



    [TestMethod]
    public void BitAccordionNestedShouldTakeTheHeadingLevelBelowItsParent()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.ChildContent, (RenderFragment)(inner =>
            {
                inner.OpenComponent<BitAccordion>(0);
                inner.CloseComponent();
            }));
        });

        var headings = com.FindAll(".bit-acd-hed");

        Assert.AreEqual(2, headings.Count);
        Assert.AreEqual("3", headings[0].GetAttribute("aria-level"));
        Assert.AreEqual("4", headings[1].GetAttribute("aria-level"));
    }

    [TestMethod]
    public void BitAccordionNestedShouldFollowAnExplicitParentHeadingLevel()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.HeadingLevel, 2);
            parameters.Add(p => p.ChildContent, (RenderFragment)(inner =>
            {
                inner.OpenComponent<BitAccordion>(0);
                inner.CloseComponent();
            }));
        });

        var headings = com.FindAll(".bit-acd-hed");

        Assert.AreEqual("2", headings[0].GetAttribute("aria-level"));
        Assert.AreEqual("3", headings[1].GetAttribute("aria-level"));
    }

    [TestMethod]
    public void BitAccordionNestedHeadingLevelShouldWinOverTheInheritedOne()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.ChildContent, (RenderFragment)(inner =>
            {
                inner.OpenComponent<BitAccordion>(0);
                inner.AddComponentParameter(1, nameof(BitAccordion.HeadingLevel), 2);
                inner.CloseComponent();
            }));
        });

        var headings = com.FindAll(".bit-acd-hed");

        Assert.AreEqual("3", headings[0].GetAttribute("aria-level"));
        Assert.AreEqual("2", headings[1].GetAttribute("aria-level"));
    }

    [TestMethod]
    public void BitAccordionNestedHeadingLevelShouldStopAtSix()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.HeadingLevel, 6);
            parameters.Add(p => p.ChildContent, (RenderFragment)(inner =>
            {
                inner.OpenComponent<BitAccordion>(0);
                inner.CloseComponent();
            }));
        });

        var headings = com.FindAll(".bit-acd-hed");

        Assert.AreEqual("6", headings[0].GetAttribute("aria-level"));
        Assert.AreEqual("6", headings[1].GetAttribute("aria-level"));
    }

    [TestMethod]
    public void BitAccordionUnmountedPanelShouldStillHandTheHeadingLevelDownOnceItOpens()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.UnmountOnCollapse, true);
            parameters.Add(p => p.ChildContent, (RenderFragment)(inner =>
            {
                inner.OpenComponent<BitAccordion>(0);
                inner.CloseComponent();
            }));
        });

        Assert.AreEqual(1, com.FindAll(".bit-acd-hed").Count);

        com.Find(".bit-acd-hdr").Click();

        var headings = com.FindAll(".bit-acd-hed");

        Assert.AreEqual(2, headings.Count);
        Assert.AreEqual("4", headings[1].GetAttribute("aria-level"));
    }



    [TestMethod]
    public void BitAccordionOnTogglingShouldReportTheClickAndTheStateItIsMovingTo()
    {
        var args = new List<BitAccordionToggleArgs>();

        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.OnToggling, (BitAccordionToggleArgs a) => args.Add(a));
        });

        com.Find(".bit-acd-hdr").Click();
        com.Find(".bit-acd-hdr").Click();

        Assert.AreEqual(2, args.Count);
        Assert.IsTrue(args[0].IsExpanding);
        Assert.IsFalse(args[1].IsExpanding);
        Assert.IsTrue(args.TrueForAll(a => a.Reason == BitAccordionToggleReason.Click));
    }

    [TestMethod]
    public async Task BitAccordionOnTogglingShouldReportAMethodCall()
    {
        BitAccordionToggleArgs? args = null;

        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.OnToggling, (BitAccordionToggleArgs a) => args = a);
        });

        await com.InvokeAsync(() => com.Instance.Expand());

        Assert.IsNotNull(args);
        Assert.IsTrue(args!.IsExpanding);
        Assert.AreEqual(BitAccordionToggleReason.Method, args.Reason);
    }

    [TestMethod]
    public void BitAccordionCancelledTogglingShouldLeaveTheAccordionAsItIs()
    {
        var changed = 0;
        var expanded = 0;

        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.OnToggling, (BitAccordionToggleArgs a) => a.Cancel = true);
            parameters.Add(p => p.OnChange, () => changed++);
            parameters.Add(p => p.OnExpand, () => expanded++);
        });

        com.Find(".bit-acd-hdr").Click();

        Assert.IsFalse(com.Find(".bit-acd").ClassList.Contains("bit-acd-exp"));
        Assert.AreEqual("false", com.Find(".bit-acd-hdr").GetAttribute("aria-expanded"));
        Assert.AreEqual(0, changed);
        Assert.AreEqual(0, expanded);
    }

    [TestMethod]
    public void BitAccordionCancelledTogglingShouldNotWriteBackToTheBinding()
    {
        var isExpanded = false;

        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Bind(p => p.IsExpanded, isExpanded, v => isExpanded = v);
            parameters.Add(p => p.OnToggling, (BitAccordionToggleArgs a) => a.Cancel = true);
        });

        com.Find(".bit-acd-hdr").Click();

        Assert.IsFalse(isExpanded);
    }

    [TestMethod]
    public void BitAccordionCancelledTogglingShouldStillReportTheClick()
    {
        var clicked = 0;

        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.OnClick, () => clicked++);
            parameters.Add(p => p.OnToggling, (BitAccordionToggleArgs a) => a.Cancel = true);
        });

        com.Find(".bit-acd-hdr").Click();

        Assert.AreEqual(1, clicked);
    }

    [TestMethod]
    public async Task BitAccordionCancelledTogglingShouldRefuseAMethodCallToo()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.DefaultIsExpanded, true);
            parameters.Add(p => p.OnToggling, (BitAccordionToggleArgs a) => a.Cancel = true);
        });

        await com.InvokeAsync(() => com.Instance.Collapse());

        Assert.IsTrue(com.Find(".bit-acd").ClassList.Contains("bit-acd-exp"));
    }

    [TestMethod]
    public async Task BitAccordionOnTogglingShouldNotBeCalledWithoutAChange()
    {
        var calls = 0;

        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.DefaultIsExpanded, true);
            parameters.Add(p => p.OnToggling, (BitAccordionToggleArgs _) => calls++);
        });

        await com.InvokeAsync(() => com.Instance.Expand());

        Assert.AreEqual(0, calls);
    }

    [TestMethod]
    public void BitAccordionOnTogglingShouldNotBeCalledForADisabledHeader()
    {
        var calls = 0;

        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.OnToggling, (BitAccordionToggleArgs _) => calls++);
        });

        com.Find(".bit-acd-hdr").Click();

        Assert.AreEqual(0, calls);
    }

    [TestMethod]
    public async Task BitAccordionOnTogglingShouldKeepASecondToggleOutWhileItIsAwaited()
    {
        var calls = 0;
        var gate = new TaskCompletionSource();

        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.OnToggling, async (BitAccordionToggleArgs _) =>
            {
                calls++;
                await gate.Task;
            });
        });

        var first = com.InvokeAsync(() => com.Instance.Toggle());

        // The first change is still waiting on the callback, so this one has nothing to do.
        await com.InvokeAsync(() => com.Instance.Toggle());

        Assert.AreEqual(1, calls);

        gate.SetResult();

        await first;

        Assert.AreEqual(1, calls);
        Assert.IsTrue(com.Find(".bit-acd").ClassList.Contains("bit-acd-exp"));
    }

    [TestMethod]
    public void BitAccordionOnTogglingShouldNotBeCalledForAControlledAccordion()
    {
        var calls = 0;

        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.IsExpanded, false);
            parameters.Add(p => p.OnToggling, (BitAccordionToggleArgs _) => calls++);
        });

        com.Find(".bit-acd-hdr").Click();

        Assert.AreEqual(0, calls);
        Assert.IsFalse(com.Find(".bit-acd").ClassList.Contains("bit-acd-exp"));
    }

    [TestMethod]
    public void BitAccordionOnTogglingShouldBeCalledForABoundAccordion()
    {
        var calls = 0;
        var isExpanded = false;

        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Bind(p => p.IsExpanded, isExpanded, v => isExpanded = v);
            parameters.Add(p => p.OnToggling, (BitAccordionToggleArgs _) => calls++);
        });

        com.Find(".bit-acd-hdr").Click();

        Assert.AreEqual(1, calls);
        Assert.IsTrue(isExpanded);
    }



    [TestMethod]
    public async Task BitAccordionShouldReturnTheFocusToTheHeaderWhenItClosesOnIt()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.DefaultIsExpanded, true);
            parameters.Add(p => p.ChildContent, "<button id=\"inside\">inside</button>");
        });

        com.Find(".bit-acd-con").TriggerEvent("onfocusin", new FocusEventArgs());

        await com.InvokeAsync(() => com.Instance.Collapse());

        Context.JSInterop.VerifyInvoke("Blazor._internal.domWrapper.focus");
    }

    [TestMethod]
    public async Task BitAccordionShouldNotTouchTheFocusWhenThePanelDoesNotHoldIt()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.DefaultIsExpanded, true);
            parameters.Add(p => p.ChildContent, "<button id=\"inside\">inside</button>");
        });

        com.Find(".bit-acd-con").TriggerEvent("onfocusin", new FocusEventArgs());
        com.Find(".bit-acd-con").TriggerEvent("onfocusout", new FocusEventArgs());

        await com.InvokeAsync(() => com.Instance.Collapse());

        Assert.AreEqual(0, Context.JSInterop.Invocations.Count(i => i.Identifier.Contains("focus")));
    }

    [TestMethod]
    public async Task BitAccordionShouldNotTouchTheFocusWhenItExpands()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.ChildContent, "<button id=\"inside\">inside</button>");
        });

        com.Find(".bit-acd-con").TriggerEvent("onfocusin", new FocusEventArgs());

        await com.InvokeAsync(() => com.Instance.Expand());

        Assert.AreEqual(0, Context.JSInterop.Invocations.Count(i => i.Identifier.Contains("focus")));
    }

    [TestMethod]
    public void BitAccordionShouldReturnTheFocusToTheHeaderWhenAControlledCollapseClosesOnIt()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.IsExpanded, true);
            parameters.Add(p => p.ChildContent, "<button id=\"inside\">inside</button>");
        });

        com.Find(".bit-acd-con").TriggerEvent("onfocusin", new FocusEventArgs());

        com.Render(parameters => parameters.Add(p => p.IsExpanded, false));

        Context.JSInterop.VerifyInvoke("Blazor._internal.domWrapper.focus");
    }

    [TestMethod]
    public void BitAccordionShouldReturnTheFocusToTheHeaderWhenABoundCollapseClosesOnIt()
    {
        var isExpanded = true;

        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Bind(p => p.IsExpanded, isExpanded, v => isExpanded = v);
            parameters.Add(p => p.ChildContent, "<button id=\"inside\">inside</button>");
        });

        com.Find(".bit-acd-con").TriggerEvent("onfocusin", new FocusEventArgs());

        com.Render(parameters => parameters.Bind(p => p.IsExpanded, false, v => isExpanded = v));

        Context.JSInterop.VerifyInvoke("Blazor._internal.domWrapper.focus");
    }

    [TestMethod]
    public void BitAccordionShouldNotTouchTheFocusWhenAParameterChangeExpandsIt()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.IsExpanded, false);
            parameters.Add(p => p.ChildContent, "<button id=\"inside\">inside</button>");
        });

        com.Find(".bit-acd-con").TriggerEvent("onfocusin", new FocusEventArgs());

        com.Render(parameters => parameters.Add(p => p.IsExpanded, true));

        Assert.AreEqual(0, Context.JSInterop.Invocations.Count(i => i.Identifier.Contains("focus")));
    }

    [TestMethod]
    public async Task BitAccordionFocusAsyncShouldFocusTheHeader()
    {
        var com = RenderComponent<BitAccordion>(parameters => parameters.Add(p => p.Title, "title-value"));

        await com.InvokeAsync(() => com.Instance.FocusAsync());

        Context.JSInterop.VerifyInvoke("Blazor._internal.domWrapper.focus");
    }

    [TestMethod]
    public async Task BitAccordionFocusAsyncShouldFocusTheHeaderWithoutScrollingIt()
    {
        var com = RenderComponent<BitAccordion>(parameters => parameters.Add(p => p.Title, "title-value"));

        await com.InvokeAsync(() => com.Instance.FocusAsync(true));

        Context.JSInterop.VerifyInvoke("Blazor._internal.domWrapper.focus");
    }



    [TestMethod,
        DataRow(true),
        DataRow(false),
    ]
    public void BitAccordionShouldRenderTheExpanderTemplateInPlaceOfTheIcon(bool defaultIsExpanded)
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.Title, "title-value");
            parameters.Add(p => p.DefaultIsExpanded, defaultIsExpanded);
            parameters.Add(p => p.ExpanderTemplate, (isExpanded) => isExpanded ? "expanded-expander" : "collapsed-expander");
        });

        var wrapper = com.Find(".bit-acd-eiw");

        Assert.AreEqual(defaultIsExpanded ? "expanded-expander" : "collapsed-expander", wrapper.TextContent);
        Assert.AreEqual(0, com.FindAll(".bit-acd-eic").Count);

        // The rest of the header is exactly where it was.
        Assert.AreEqual("title-value", com.Find(".bit-acd-ttl").TextContent);
    }

    [TestMethod]
    public void BitAccordionExpanderTemplateShouldStillTurnOverWithThePanel()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.DefaultIsExpanded, true);
            parameters.Add(p => p.ExpanderTemplate, _ => "expander-template");
        });

        Assert.IsTrue(com.Find(".bit-acd-eiw").ClassList.Contains("bit-ico--r180"));
    }

    [TestMethod]
    public void BitAccordionExpanderTemplateShouldStayStillWithNoExpanderRotation()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.DefaultIsExpanded, true);
            parameters.Add(p => p.NoExpanderRotation, true);
            parameters.Add(p => p.ExpanderTemplate, _ => "expander-template");
        });

        Assert.IsFalse(com.Find(".bit-acd-eiw").ClassList.Contains("bit-ico--r180"));
    }

    [TestMethod]
    public void BitAccordionHideExpanderIconShouldRemoveTheExpanderTemplateToo()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.HideExpanderIcon, true);
            parameters.Add(p => p.ExpanderTemplate, _ => "expander-template");
        });

        Assert.AreEqual(0, com.FindAll(".bit-acd-eiw").Count);
        Assert.IsFalse(com.Find(".bit-acd-hdr").TextContent.Contains("expander-template"));
    }

    [TestMethod]
    public void BitAccordionHeaderTemplateShouldWinOverTheExpanderTemplate()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.ExpanderTemplate, _ => "expander-template");
            parameters.Add(p => p.HeaderTemplate, _ => "header-template");
        });

        Assert.AreEqual("header-template", com.Find(".bit-acd-hdr").TextContent);
        Assert.AreEqual(0, com.FindAll(".bit-acd-eiw").Count);
    }

    [TestMethod]
    public async Task BitAccordionOnTogglingShouldStandDownWhenTheStateMovedOnWhileItWasAwaited()
    {
        var expanded = 0;
        var gate = new TaskCompletionSource();

        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.OnExpand, () => expanded++);
            parameters.Add(p => p.OnToggling, async (BitAccordionToggleArgs _) => await gate.Task);
        });

        var toggling = com.InvokeAsync(() => com.Instance.Toggle());

        // The page drives the bound state itself while the callback is still open, so by the time it comes
        // back the accordion is already where it was being asked to go.
        com.Render(parameters => parameters.Add(p => p.IsExpanded, true));

        gate.SetResult();

        await toggling;

        Assert.AreEqual(0, expanded);
        Assert.IsTrue(com.Find(".bit-acd").ClassList.Contains("bit-acd-exp"));
    }



    [TestMethod,
        DataRow(true),
        DataRow(false),
    ]
    public void BitAccordionShouldRenderTheReadOnlyClass(bool readOnly)
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, readOnly);
        });

        Assert.AreEqual(readOnly, com.Find(".bit-acd").ClassList.Contains("bit-acd-rdo"));
    }

    [TestMethod]
    public void BitAccordionReadOnlyShouldNotToggleOnClick()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.DefaultIsExpanded, true);
        });

        com.Find(".bit-acd-hdr").Click();

        Assert.IsTrue(com.Find(".bit-acd").ClassList.Contains("bit-acd-exp"));
        Assert.AreEqual("true", com.Find(".bit-acd-hdr").GetAttribute("aria-expanded"));
    }

    [TestMethod]
    public void BitAccordionReadOnlyShouldStillReportTheClick()
    {
        var clicks = 0;
        var changes = 0;

        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.OnClick, () => clicks++);
            parameters.Add(p => p.OnChange, (bool _) => changes++);
        });

        com.Find(".bit-acd-hdr").Click();

        Assert.AreEqual(1, clicks);
        Assert.AreEqual(0, changes);
    }

    [TestMethod]
    public void BitAccordionReadOnlyShouldNotCallOnToggling()
    {
        var calls = 0;

        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.OnToggling, (BitAccordionToggleArgs _) => calls++);
        });

        com.Find(".bit-acd-hdr").Click();

        Assert.AreEqual(0, calls);
    }

    [TestMethod]
    public void BitAccordionReadOnlyShouldKeepTheHeaderInTheTabOrderAndReportItAsDisabled()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
        });

        var header = com.Find(".bit-acd-hdr");

        Assert.AreEqual("0", header.GetAttribute("tabindex"));
        Assert.AreEqual("true", header.GetAttribute("aria-disabled"));
        Assert.IsFalse(com.Find(".bit-acd").ClassList.Contains("bit-dis"));
    }

    [TestMethod]
    public async Task BitAccordionReadOnlyShouldStillAnswerTheMethods()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
        });

        await com.InvokeAsync(() => com.Instance.Expand());

        Assert.IsTrue(com.Find(".bit-acd").ClassList.Contains("bit-acd-exp"));

        await com.InvokeAsync(() => com.Instance.Collapse());

        Assert.IsFalse(com.Find(".bit-acd").ClassList.Contains("bit-acd-exp"));
    }



    [TestMethod]
    public void BitAccordionShouldGiveTheHeaderATabStopOfItsOwn()
    {
        var com = RenderComponent<BitAccordion>();

        Assert.AreEqual("0", com.Find(".bit-acd-hdr").GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitAccordionShouldRenderTheTabIndexOnTheHeader()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.TabIndex, "3");
        });

        Assert.AreEqual("3", com.Find(".bit-acd-hdr").GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitAccordionDisabledHeaderShouldStayOutOfTheTabOrderWhateverTheTabIndexIs()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.TabIndex, "3");
        });

        Assert.AreEqual("-1", com.Find(".bit-acd-hdr").GetAttribute("tabindex"));
    }



    [TestMethod,
        DataRow(true),
        DataRow(false),
    ]
    public void BitAccordionShouldRenderTheExpandOnPrintClass(bool expandOnPrint)
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.ExpandOnPrint, expandOnPrint);
        });

        Assert.AreEqual(expandOnPrint, com.Find(".bit-acd").ClassList.Contains("bit-acd-eop"));
    }



    [TestMethod]
    public async Task BitAccordionShouldReportTheHeaderAsBusyWhileOnTogglingIsAwaited()
    {
        var gate = new TaskCompletionSource();

        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.OnToggling, async (BitAccordionToggleArgs _) => await gate.Task);
        });

        Assert.IsNull(com.Find(".bit-acd-hdr").GetAttribute("aria-busy"));

        var toggling = com.InvokeAsync(() => com.Instance.Toggle());

        Assert.AreEqual("true", com.Find(".bit-acd-hdr").GetAttribute("aria-busy"));
        Assert.IsTrue(com.Find(".bit-acd-hdr").ClassList.Contains("bit-acd-bsy"));

        gate.SetResult();

        await toggling;

        Assert.IsNull(com.Find(".bit-acd-hdr").GetAttribute("aria-busy"));
        Assert.IsFalse(com.Find(".bit-acd-hdr").ClassList.Contains("bit-acd-bsy"));
    }

    [TestMethod]
    public async Task BitAccordionShouldDropTheBusyStateOfACancelledToggleToo()
    {
        var gate = new TaskCompletionSource();

        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.OnToggling, async (BitAccordionToggleArgs args) =>
            {
                await gate.Task;
                args.Cancel = true;
            });
        });

        var toggling = com.InvokeAsync(() => com.Instance.Toggle());

        Assert.AreEqual("true", com.Find(".bit-acd-hdr").GetAttribute("aria-busy"));

        gate.SetResult();

        await toggling;

        Assert.IsNull(com.Find(".bit-acd-hdr").GetAttribute("aria-busy"));
        Assert.IsFalse(com.Find(".bit-acd").ClassList.Contains("bit-acd-exp"));
    }

    [TestMethod]
    public void BitAccordionShouldNotReportTheHeaderAsBusyWithoutAnOnToggling()
    {
        var com = RenderComponent<BitAccordion>();

        com.Find(".bit-acd-hdr").Click();

        Assert.IsNull(com.Find(".bit-acd-hdr").GetAttribute("aria-busy"));
        Assert.IsTrue(com.Find(".bit-acd").ClassList.Contains("bit-acd-exp"));
    }



    [TestMethod]
    public void BitAccordionShouldNotRenderAgainWhenTheFocusMovesInsideThePanel()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.DefaultIsExpanded, true);
            parameters.Add(p => p.ChildContent, "<button id=\"inside\">inside</button>");
        });

        var renders = com.RenderCount;

        com.Find(".bit-acd-con").TriggerEvent("onfocusin", new FocusEventArgs());
        com.Find(".bit-acd-con").TriggerEvent("onfocusout", new FocusEventArgs());
        com.Find(".bit-acd-con").TriggerEvent("onfocusin", new FocusEventArgs());

        Assert.AreEqual(renders, com.RenderCount);
    }

    [TestMethod]
    public void BitAccordionShouldStillRenderTheClickThatFollowsAFocusMove()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.DefaultIsExpanded, true);
            parameters.Add(p => p.ChildContent, "<button id=\"inside\">inside</button>");
        });

        com.Find(".bit-acd-con").TriggerEvent("onfocusout", new FocusEventArgs());

        com.Find(".bit-acd-hdr").Click();

        Assert.IsFalse(com.Find(".bit-acd").ClassList.Contains("bit-acd-exp"));
        Assert.AreEqual("false", com.Find(".bit-acd-hdr").GetAttribute("aria-expanded"));
    }

    [TestMethod]
    public void BitAccordionShouldStillRenderAParameterChangeThatFollowsAFocusMove()
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.Title, "before");
        });

        com.Find(".bit-acd-con").TriggerEvent("onfocusin", new FocusEventArgs());

        com.Render(parameters => parameters.Add(p => p.Title, "after"));

        Assert.AreEqual("after", com.Find(".bit-acd-ttl").TextContent.Trim());
    }
}
