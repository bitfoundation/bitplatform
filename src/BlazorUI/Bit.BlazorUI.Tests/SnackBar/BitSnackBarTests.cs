using System;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Bunit.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace Bit.BlazorUI.Tests.SnackBar;

[TestClass]
public class BitSnackBarTests : BunitTestContext
{
    [TestMethod]
    public void BitSnackBarInitialTest()
    {
        var com = RenderComponent<BitSnackBar>();

        var element = com.Find(".bit-snb");

        Assert.IsNotNull(element);
    }

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
        DataRow(BitSnackBarType.Info),
        DataRow(BitSnackBarType.Warning),
        DataRow(BitSnackBarType.Success),
        DataRow(BitSnackBarType.Error),
        DataRow(BitSnackBarType.SevereWarning),
        DataRow(null)
    ]
    [TestMethod]
    public async Task BitSnackBarTypeTest(BitSnackBarType? type)
    {
        var com = RenderComponent<BitSnackBar>();

        if (type.HasValue)
            await com.Instance.Show("title", string.Empty, type);
        else
            await com.Instance.Show("title");

        var element = com.Find(".bit-snb-itm");

        var typeClass = type switch
        {
            BitSnackBarType.Info => $"bit-snb-info",
            BitSnackBarType.Warning => $"bit-snb-warning",
            BitSnackBarType.Success => $"bit-snb-success",
            BitSnackBarType.Error => $"bit-snb-error",
            BitSnackBarType.SevereWarning => $"bit-snb-severe-warning",
            _ => "bit-snb-info"
        };

        Assert.IsTrue(element.ClassList.Contains(typeClass));
    }

    [TestMethod]
    public async Task BitSnackBarCloseButtonTest()
    {
        var com = RenderComponent<BitSnackBar>();

        await com.Instance.Show("title");

        var closeButton = com.Find(".bit-snb-cbt");
        closeButton.Click();

        var items = com.FindAll(".bit-snb-itm");

        Assert.AreEqual(0, items.Count);
    }

    [TestMethod]
    public async Task BitSnackBarDismissIconNameTest()
    {
        var com = RenderComponent<BitSnackBar>(
            parameters =>
            {
                parameters.Add(x => x.DismissIconName, "Go");
            }
        );

        await com.Instance.Show("title");

        var closeButtonIcon = com.Find(".bit-snb-cbt > .bit-icon");

        Assert.IsTrue(closeButtonIcon.ClassList.Contains("bit-icon--Go"));
    }

    [TestMethod]
    public async Task BitSnackBarTitleTemplateTest()
    {
        var wrappedCom = RenderComponent<BitSnackBarWithTitleTemplate>();

        var com = wrappedCom.FindComponent<BitSnackBar>();

        await com.Instance.Show("title");

        var titleTemplateElements = com.FindAll(".bit-snb-hdr > *").ToList();
        //remove button
        titleTemplateElements.RemoveAt(0);

        Assert.AreEqual("<span>title</span><span>second element</span>", string.Join(string.Empty, titleTemplateElements.Select(x => x.OuterHtml).ToArray()));
    }

    [TestMethod]
    public async Task BitSnackBarBodyTemplateTest()
    {
        var wrappedCom = RenderComponent<BitSnackBarWithBodyTemplate>();

        var com = wrappedCom.FindComponent<BitSnackBar>();

        await com.Instance.Show("title", "body");

        var itemTemplateElements = com.FindAll(".bit-snb-itm > *").ToList();
        //remove title
        itemTemplateElements.RemoveAt(0);
        //remove progressbar
        itemTemplateElements.RemoveAt(itemTemplateElements.Count - 1);

        Assert.AreEqual("<p>body</p><span>second element</span>", string.Join(string.Empty, itemTemplateElements.Select(x => x.OuterHtml).ToArray()));
    }


}
