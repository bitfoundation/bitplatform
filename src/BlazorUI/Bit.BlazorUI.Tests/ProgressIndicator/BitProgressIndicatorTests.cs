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
    public void BitProgressIndicatorBarHeightTest(int barHeight)
    {
        var component = RenderComponent<BitProgressIndicatorTest>(parameters =>
        {
            parameters.Add(p => p.BarHeight, barHeight);
        });

        var piWrapper = component.Find(".wrapper");
        var piWrapperStyle = piWrapper.GetAttribute("style");
        var expectedValue = $"height: {barHeight}px";
        Assert.IsTrue(piWrapperStyle.Contains(expectedValue));
    }

    [DataTestMethod,
        DataRow(52),
        DataRow(43)
    ]
    public void BitProgressIndicatorBarWidthShouldBeEqualPercentCompleteValue(double percentComplete)
    {
        var component = RenderComponent<BitProgressIndicatorTest>(parameters =>
        {
            parameters.Add(p => p.PercentComplete, percentComplete);
        });

        var piBar = component.Find(".bar");
        var piBarStyle = piBar.GetAttribute("style");
        var expectedValue = $"width: {percentComplete}%";
        Assert.IsTrue(piBarStyle.Contains(expectedValue));
    }

    [DataTestMethod,
        DataRow(520),
        DataRow(430)
    ]
    public void BitProgressIndicatorBarWidthCanNotBeBiggerThan100(double percentComplete)
    {
        var component = RenderComponent<BitProgressIndicatorTest>(parameters =>
        {
            parameters.Add(p => p.PercentComplete, percentComplete);
        });

        var piBar = component.Find(".bar");
        var piBarStyle = piBar.GetAttribute("style");
        var expectedValue = $"width: 100%";
        Assert.IsTrue(piBarStyle.Contains(expectedValue));
    }

    [DataTestMethod,
        DataRow(-5),
        DataRow(-265)
    ]
    public void BitProgressIndicatorBarWidthCanNotBeSmallerThan0(double percentComplete)
    {
        var component = RenderComponent<BitProgressIndicatorTest>(parameters =>
        {
            parameters.Add(p => p.PercentComplete, percentComplete);
        });

        var piBar = component.Find(".bar");
        var piBarStyle = piBar.GetAttribute("style");
        var expectedValue = $"width: 0%";
        Assert.IsTrue(piBarStyle.Contains(expectedValue));
    }

    
    [DataTestMethod,
        DataRow(32.0),
        DataRow(null)
    ]
    public void BitProgressIndicatorIndeterminateClassTest(double? percentComplete)
    {
        var component = RenderComponent<BitProgressIndicatorTest>(parameters =>
        {
            parameters.Add(p => p.PercentComplete, percentComplete);
        });

        var pin = component.Find(".bit-pin");
        Assert.AreEqual(percentComplete is null, pin.ClassList.Contains($"indeterminate"));
    }

    [DataTestMethod,
        DataRow("Label"),
        DataRow(null),
    ]
    public void BitProgressIndicatorLabelTest(string label)
    {
        var component = RenderComponent<BitProgressIndicatorTest>(parameters =>
        {
            parameters.Add(p => p.Label, label);
        });

        var piBar = component.Find(".bar");
        if (label is not null)
        {
            var piLabel = component.Find(".label");
            Assert.AreEqual(label, piLabel.TextContent);
            Assert.IsNotNull(piBar.GetAttribute("aria-labelledby"));
        }
        else
        {
            Assert.ThrowsException<ElementNotFoundException>(() => component.Find(".label"));
            Assert.IsNull(piBar.GetAttribute("aria-labelledby"));
        }
    }

    [DataTestMethod,
        DataRow("Description"),
        DataRow(null),
    ]
    public void BitProgressIndicatorDescriptionTest(string description)
    {
        var component = RenderComponent<BitProgressIndicatorTest>(parameters =>
        {
            parameters.Add(p => p.Description, description);
        });

        var piBar = component.Find(".bar");
        if (description is not null)
        {
            var piDescription = component.Find(".description");
            Assert.AreEqual(description, piDescription.TextContent);
            Assert.IsNotNull(piBar.GetAttribute("aria-describedby"));
        }
        else
        {
            Assert.ThrowsException<ElementNotFoundException>(() => component.Find(".description"));
            Assert.IsNull(piBar.GetAttribute("aria-describedby"));
        }
    }

    [DataTestMethod,
        DataRow("Aria Value Text"),
        DataRow(null),
    ]
    public void BitProgressIndicatorAriaValueTextTest(string txt)
    {
        var component = RenderComponent<BitProgressIndicatorTest>(parameters =>
        {
            parameters.Add(p => p.AriaValueText, txt);
        });

        var piBar = component.Find(".bar");
        if (txt is not null)
        {
            Assert.AreEqual(txt, piBar.GetAttribute("aria-valuetext"));
        }
        else
        {
            Assert.IsNull(piBar.GetAttribute("aria-valuetext"));
        }
    }

    [DataTestMethod]
    public void BitProgressIndicatorIsProgressHiddenTest()
    {
        var component = RenderComponent<BitProgressIndicatorTest>(parameters =>
        {
            parameters.Add(p => p.IsProgressHidden, true);
        });

        Assert.ThrowsException<ElementNotFoundException>(() => component.Find(".wrapper"));
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

        var labelChildNodes = component.Find(".label").ChildNodes;
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

        var descriptionChildNodes = component.Find(".description").ChildNodes;
        descriptionChildNodes.MarkupMatches(descriptionTemplate);
    }
}
