using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Progress.Progress;

[TestClass]
public class BitProgressTests : BunitTestContext
{
    [DataTestMethod,
        DataRow(3),
        DataRow(12)
    ]
    public void BitProgressThicknessTest(int thickness)
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Thickness, thickness);
        });

        var piWrapper = component.Find(".bit-prb-bcn");
        var piWrapperStyle = piWrapper.GetAttribute("style");
        var expectedValue = $"height: {thickness}px";
        Assert.IsTrue(piWrapperStyle.Contains(expectedValue));
    }

    [DataTestMethod,
        DataRow(52),
        DataRow(43)
    ]
    public void BitProgressWidthShouldBeEqualPercent(double percent)
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Percent, percent);
        });

        var piBar = component.Find(".bit-prb-bar");
        var piBarStyle = piBar.GetAttribute("style");
        var expectedValue = $"width: {percent}%";
        Assert.IsTrue(piBarStyle.Contains(expectedValue));
    }

    [DataTestMethod,
        DataRow(520),
        DataRow(430)
    ]
    public void BitProgressWidthCanNotBeBiggerThan100(double percent)
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Percent, percent);
        });

        var piBar = component.Find(".bit-prb-bar");
        var piBarStyle = piBar.GetAttribute("style");
        var expectedValue = "width: 100%";
        Assert.IsTrue(piBarStyle.Contains(expectedValue));
    }

    [DataTestMethod,
        DataRow(-5),
        DataRow(-265)
    ]
    public void BitProgressWidthCanNotBeSmallerThan0(double percent)
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Percent, percent);
        });

        var piBar = component.Find(".bit-prb-bar");
        var piBarStyle = piBar.GetAttribute("style");
        var expectedValue = "width: 0%";
        Assert.IsTrue(piBarStyle.Contains(expectedValue));
    }


    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitProgressIndeterminateClassTest(bool indeterminate)
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Indeterminate, indeterminate);
        });

        var pin = component.Find(".bit-prb-bar");
        Assert.AreEqual(indeterminate, pin.ClassList.Contains("bit-prb-ind"));
    }

    [DataTestMethod,
        DataRow("Label"),
        DataRow(null),
    ]
    public void BitProgressLabelTest(string label)
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Label, label);
        });

        var piBar = component.Find(".bit-prb-bar");
        if (string.IsNullOrEmpty(label))
        {
            Assert.ThrowsException<ElementNotFoundException>(() => component.Find(".bit-prb-lbl"));
            Assert.IsNull(piBar.GetAttribute("aria-labelledby"));
        }
        else
        {
            var piLabel = component.Find(".bit-prb-lbl");
            Assert.AreEqual(label, piLabel.TextContent);
            Assert.IsNotNull(piBar.GetAttribute("aria-labelledby"));
        }
    }

    [DataTestMethod,
        DataRow("Description"),
        DataRow(null),
    ]
    public void BitProgressDescriptionTest(string description)
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Description, description);
        });

        var piBar = component.Find(".bit-prb-bar");
        if (string.IsNullOrEmpty(description))
        {
            Assert.ThrowsException<ElementNotFoundException>(() => component.Find(".bit-prb-des"));
            Assert.IsNull(piBar.GetAttribute("aria-describedby"));
        }
        else
        {
            var piDescription = component.Find(".bit-prb-des");
            Assert.AreEqual(description, piDescription.TextContent);
            Assert.IsNotNull(piBar.GetAttribute("aria-describedby"));
        }
    }

    [DataTestMethod,
        DataRow("Aria Value Text"),
        DataRow(null),
    ]
    public void BitProgressAriaValueTextTest(string txt)
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.AriaValueText, txt);
        });

        var piBar = component.Find(".bit-prb-bar");
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
    public void BitProgressShowPercentNumberTest(bool showPercentNumber)
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.ShowPercentNumber, showPercentNumber);
        });


        if (showPercentNumber)
        {
            var percentContainer = component.Find(".bit-prb-pct");
            Assert.IsNotNull(percentContainer);
        }
        else
        {
            Assert.ThrowsException<ElementNotFoundException>(() => component.Find(".bit-prb-pct"));
        }
    }

    [DataTestMethod,
        DataRow("<h1>this is a custom label</h1>")
    ]
    public void BitProgressLabelTemplateTest(string labelTemplate)
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.LabelTemplate, labelTemplate);
        });

        var labelChildNodes = component.Find(".bit-prb").FirstChild.ChildNodes;
        labelChildNodes.MarkupMatches(labelTemplate);
    }

    [DataTestMethod,
        DataRow("<h1>this is a custom description</h1>"),
    ]
    public void BitProgressDescriptionTemplateTest(string descriptionTemplate)
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.DescriptionTemplate, descriptionTemplate);
        });

        var descriptionChildNodes = component.Find(".bit-prb").LastChild.ChildNodes;
        descriptionChildNodes.MarkupMatches(descriptionTemplate);
    }
}
