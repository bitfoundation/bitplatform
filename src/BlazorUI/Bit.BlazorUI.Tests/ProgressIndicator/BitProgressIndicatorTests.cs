using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.ProgressIndicator;

[TestClass]
public class BitProgressIndicatorTests : BunitTestContext
{
    [DataTestMethod,
        DataRow(3),
        DataRow(12)
    ]
    public void BitProgressIndicatorHeightTest(int height)
    {
        var component = RenderComponent<BitProgressIndicator>(parameters =>
        {
            parameters.Add(p => p.Height, height);
        });

        var piWrapper = component.Find(".bit-pin-icn");
        var piWrapperStyle = piWrapper.GetAttribute("style");
        var expectedValue = $"height: {height}px";
        Assert.IsTrue(piWrapperStyle.Contains(expectedValue));
    }

    [DataTestMethod,
        DataRow(52),
        DataRow(43)
    ]
    public void BitProgressIndicatorBarWidthShouldBeEqualPercentValue(double percent)
    {
        var component = RenderComponent<BitProgressIndicator>(parameters =>
        {
            parameters.Add(p => p.Percent, percent);
        });

        var piBar = component.Find(".bit-pin-bar");
        var piBarStyle = piBar.GetAttribute("style");
        var expectedValue = $"width: {percent}%";
        Assert.IsTrue(piBarStyle.Contains(expectedValue));
    }

    [DataTestMethod,
        DataRow(520),
        DataRow(430)
    ]
    public void BitProgressIndicatorBarWidthCanNotBeBiggerThan100(double percent)
    {
        var component = RenderComponent<BitProgressIndicator>(parameters =>
        {
            parameters.Add(p => p.Percent, percent);
        });

        var piBar = component.Find(".bit-pin-bar");
        var piBarStyle = piBar.GetAttribute("style");
        var expectedValue = "width: 100%";
        Assert.IsTrue(piBarStyle.Contains(expectedValue));
    }

    [DataTestMethod,
        DataRow(-5),
        DataRow(-265)
    ]
    public void BitProgressIndicatorBarWidthCanNotBeSmallerThan0(double percent)
    {
        var component = RenderComponent<BitProgressIndicator>(parameters =>
        {
            parameters.Add(p => p.Percent, percent);
        });

        var piBar = component.Find(".bit-pin-bar");
        var piBarStyle = piBar.GetAttribute("style");
        var expectedValue = "width: 0%";
        Assert.IsTrue(piBarStyle.Contains(expectedValue));
    }

    
    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitProgressIndicatorIndeterminateClassTest(bool indeterminate)
    {
        var component = RenderComponent<BitProgressIndicator>(parameters =>
        {
            parameters.Add(p => p.Indeterminate, indeterminate);
        });

        var pin = component.Find(".bit-pin");
        Assert.AreEqual(indeterminate, pin.ClassList.Contains("bit-pin-ind"));
    }

    [DataTestMethod,
        DataRow("Label"),
        DataRow(null),
    ]
    public void BitProgressIndicatorLabelTest(string label)
    {
        var component = RenderComponent<BitProgressIndicator>(parameters =>
        {
            parameters.Add(p => p.Label, label);
        });

        var piBar = component.Find(".bit-pin-bar");
        if (string.IsNullOrEmpty(label))
        {
            Assert.ThrowsException<ElementNotFoundException>(() => component.Find(".bit-pin-lbl"));
            Assert.IsNull(piBar.GetAttribute("aria-labelledby"));
        }
        else
        {
            var piLabel = component.Find(".bit-pin-lbl");
            Assert.AreEqual(label, piLabel.TextContent);
            Assert.IsNotNull(piBar.GetAttribute("aria-labelledby"));
        }
    }

    [DataTestMethod,
        DataRow("Description"),
        DataRow(null),
    ]
    public void BitProgressIndicatorDescriptionTest(string description)
    {
        var component = RenderComponent<BitProgressIndicator>(parameters =>
        {
            parameters.Add(p => p.Description, description);
        });

        var piBar = component.Find(".bit-pin-bar");
        if (string.IsNullOrEmpty(description))
        {
            Assert.ThrowsException<ElementNotFoundException>(() => component.Find(".bit-pin-des"));
            Assert.IsNull(piBar.GetAttribute("aria-describedby"));
        }
        else
        {
            var piDescription = component.Find(".bit-pin-des");
            Assert.AreEqual(description, piDescription.TextContent);
            Assert.IsNotNull(piBar.GetAttribute("aria-describedby"));
        }
    }

    [DataTestMethod,
        DataRow("Aria Value Text"),
        DataRow(null),
    ]
    public void BitProgressIndicatorAriaValueTextTest(string txt)
    {
        var component = RenderComponent<BitProgressIndicator>(parameters =>
        {
            parameters.Add(p => p.AriaValueText, txt);
        });

        var piBar = component.Find(".bit-pin-bar");
        if (string.IsNullOrEmpty(txt))
        {
            Assert.IsNull(piBar.GetAttribute("aria-valuetext"));
        }
        else
        {
            Assert.AreEqual(txt, piBar.GetAttribute("aria-valuetext"));
        }
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)]
    public void BitProgressIndicatorShowPercentTest(bool showPercent)
    {
        var component = RenderComponent<BitProgressIndicator>(parameters =>
        {
            parameters.Add(p => p.ShowPercentNumber, showPercent);
        });


        if (showPercent)
        {
            var percentContainer = component.Find(".bit-pin-pct");
            Assert.IsNotNull(percentContainer);
        }
        else
        {
            Assert.ThrowsException<ElementNotFoundException>(() => component.Find(".bit-pin-pct"));
        }
    }

    [DataTestMethod,
        DataRow("<h1>this is a custom label</h1>")
    ]
    public void BitProgressIndicatorLabelTemplateTest(string labelTemplate)
    {
        var component = RenderComponent<BitProgressIndicator>(parameters =>
        {
            parameters.Add(p => p.LabelTemplate, labelTemplate);
        });

        var labelChildNodes = component.Find(".bit-pin").FirstChild.ChildNodes;
        labelChildNodes.MarkupMatches(labelTemplate);
    }

    [DataTestMethod,
        DataRow("<h1>this is a custom description</h1>"),
    ]
    public void BitProgressIndicatorDescriptionTemplateTest(string descriptionTemplate)
    {
        var component = RenderComponent<BitProgressIndicator>(parameters =>
        {
            parameters.Add(p => p.DescriptionTemplate, descriptionTemplate);
        });

        var descriptionChildNodes = component.Find(".bit-pin").LastChild.ChildNodes;
        descriptionChildNodes.MarkupMatches(descriptionTemplate);
    }
}
