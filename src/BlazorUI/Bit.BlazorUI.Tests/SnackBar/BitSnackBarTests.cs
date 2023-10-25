using System;
using System.Threading.Tasks;
using Bunit;
using Bunit.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.SnackBar;

[TestClass]
public class BitSnackBarTests : BunitTestContext
{
    [DataTestMethod,
     DataRow(BitSnackBarPosition.TopLeft),
     DataRow(BitSnackBarPosition.TopCenter),
     DataRow(BitSnackBarPosition.TopRight),
     DataRow(BitSnackBarPosition.BottomLeft),
     DataRow(BitSnackBarPosition.BottomCenter),
     DataRow(BitSnackBarPosition.BottomRight),
     DataRow(null)
    ]
    [TestMethod]
    public void BitSnackBarPositionTest(BitSnackBarPosition? position)
    {
        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            if (position.HasValue) parameters.Add(p => p.Position, position.Value);
        });

        var element = com.Find(".bit-snb");

        var positionClass = position switch
        {
            BitSnackBarPosition.TopLeft => "bit-snb-tlf",
            BitSnackBarPosition.TopCenter => "bit-snb-tcn",
            BitSnackBarPosition.TopRight => "bit-snb-trt",

            BitSnackBarPosition.BottomLeft => "bit-snb-blf",
            BitSnackBarPosition.BottomCenter => "bit-snb-bcn",
            BitSnackBarPosition.BottomRight => "bit-snb-brt",

            _ => "bit-snb-brt",
        };

        Assert.IsTrue(element.ClassList.Contains(positionClass));
    }

    [DataTestMethod,
     DataRow("title", "body"),
     DataRow("title", "")
    ]
    [TestMethod]
    public async Task BitSnackBarShowTest(string title, string body)
    {
        var com = RenderComponent<BitSnackBar>();

        await com.Instance.Show(title, body);

        var item = com.Find(".bit-snb-itm");
        Assert.IsNotNull(item);

        var titleElement = com.Find(".bit-snb-ttl");
        Assert.AreEqual(title, titleElement.InnerHtml);

        if (body.IsNullOrEmpty() is false)
        {
            var bodyElement = com.Find(".bit-snb-bdy");
            Assert.AreEqual(body, bodyElement.InnerHtml);
        }
    }

    [DataTestMethod,
     DataRow(true),
     DataRow(false)
    ]
    [TestMethod]
    public async Task BitSnackBarAutoDismissTest(bool autoDismiss)
    {
        var com = RenderComponent<BitSnackBar>(
            parameters =>
            {
                parameters.Add(p => p.AutoDismiss, autoDismiss);
            }
        );

        await com.Instance.Show("title");

        //Added a sec delay to be sure we are asserting after AutoDismissTime passed
        await Task.Delay(com.Instance.AutoDismissTime + TimeSpan.FromSeconds(1));

        var items = com.FindAll(".bit-snb-itm");

        Assert.AreEqual(autoDismiss ? 0 : 1, items.Count);
    }

    [DataTestMethod,
     DataRow("title", BitSnackBarType.Info),
     DataRow("title", BitSnackBarType.Warning),
     DataRow("title", BitSnackBarType.Success),
     DataRow("title", BitSnackBarType.Error),
     DataRow("title", BitSnackBarType.SevereWarning),
     DataRow("title", null)
    ]
    [TestMethod]
    public async Task BitSnackBarTypeTest(string title, BitSnackBarType? type)
    {
        var com = RenderComponent<BitSnackBar>();

        if (type.HasValue)
        {
            await com.Instance.Show(title, type: type.Value);
        }
        else
        {
            await com.Instance.Show(title);
        }

        var element = com.Find(".bit-snb-itm");

        var typeClass = type switch
        {
            BitSnackBarType.Info => $"bit-snb-info",
            BitSnackBarType.Warning => $"bit-snb-warning",
            BitSnackBarType.Success => $"bit-snb-success",
            BitSnackBarType.Error => $"bit-snb-error",
            BitSnackBarType.SevereWarning => $"bit-snb-severe-warning",
            _ => String.Empty
        };

        if (typeClass.IsNullOrEmpty())
        {
            Assert.AreEqual(1, element.ClassList.Length);
        }
        else
        {
            Assert.IsTrue(element.ClassList.Contains(typeClass));
        }
    }

    [DataTestMethod,
     DataRow("title")
    ]
    [TestMethod]
    public async Task BitSnackBarCloseButtonTest(string title)
    {
        var com = RenderComponent<BitSnackBar>();

        await com.Instance.Show(title);

        var closeButton = com.Find(".bit-snb-cbt");

        var itemsBeforeClose = com.FindAll(".bit-snb-itm");
        Assert.AreEqual(1, itemsBeforeClose.Count);

        closeButton.Click();

        var itemsAfterClose = com.FindAll(".bit-snb-itm");
        Assert.AreEqual(0, itemsAfterClose.Count);
    }

    [DataTestMethod,
     DataRow("title", "Go"),
     DataRow("title", "Cancel")
    ]
    [TestMethod]
    public async Task BitSnackBarDismissIconNameTest(string title, string iconName)
    {
        var com = RenderComponent<BitSnackBar>(
            parameters =>
            {
                parameters.Add(x => x.DismissIconName, iconName);
            }
        );

        await com.Instance.Show(title);

        var closeButtonIcon = com.Find(".bit-snb-cbt > .bit-icon");

        Assert.IsTrue(closeButtonIcon.ClassList.Contains($"bit-icon--{iconName}"));
    }

    [DataTestMethod,
     DataRow("title")
    ]
    [TestMethod]
    public async Task BitSnackBarTitleTemplateTest(string title)
    {
        RenderFragment<string> titleTemplate = (string text) => (builder) =>
        {
            builder.AddMarkupContent(0, $"<span>{text}</span>");
        };
        var com = RenderComponent<BitSnackBar>(parameters => parameters
            .Add(p => p.TitleTemplate, titleTemplate)
        );

        await com.Instance.Show(title);

        var titleTemplateElement = com.Find(".bit-snb-hdr");

        var expectedHtml = $@"<button diff:ignore></button>
                              <span>{title}</span>";

        titleTemplateElement.InnerHtml.MarkupMatches(expectedHtml);
    }

    [DataTestMethod,
     DataRow("title", "body")
    ]
    [TestMethod]
    public async Task BitSnackBarBodyTemplateTest(string title, string body)
    {
        RenderFragment<string> bodyTemplate = (string text) => (builder) =>
        {
            builder.AddMarkupContent(0, $"<p>{text}</p>");
        };
        var com = RenderComponent<BitSnackBar>(parameters => parameters
            .Add(p => p.BodyTemplate, bodyTemplate)
        );

        await com.Instance.Show(title, body);

        var itemTemplateElement = com.Find(".bit-snb-itm");

        var expectedHtml = $@"<div diff:ignore></div>
                              <p>{body}</p>
                              <span diff:ignore></span>";

        itemTemplateElement.InnerHtml.MarkupMatches(expectedHtml);
    }
}
