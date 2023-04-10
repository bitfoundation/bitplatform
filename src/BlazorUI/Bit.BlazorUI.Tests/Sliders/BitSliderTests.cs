using System.Linq;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Sliders;

[TestClass]
public class BitSliderTests : BunitTestContext
{
    [TestInitialize]
    public void SetupJsInteropMode()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [DataTestMethod,
        DataRow(false),
        DataRow(true)
    ]
    public void BitSliderVerticalTest(bool isVertical)
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsVertical, isVertical);
        });

        var bitSlider = com.Find(".bit-sld");
        Assert.IsTrue(bitSlider.ClassList.Contains(isVertical ? "vertical" : "horizontal"));
    }

    [DataTestMethod,
        DataRow(false),
        DataRow(true)
    ]
    public void BitSliderRangedVerticalTest(bool vertical)
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsVertical, vertical);
            parameters.Add(p => p.IsRanged, true);
        });

        var bitSlider = com.Find(".bit-sld");
        Assert.IsTrue(bitSlider.ClassList.Contains($"ranged-{(vertical ? "vertical" : "horizontal")}"));
    }

    [DataTestMethod,
        DataRow(false),
        DataRow(true)
    ]
    public void BitSliderEnabledTest(bool isEnabled)
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var bitSlider = com.Find(".bit-sld");

        if (isEnabled)
        {
            Assert.IsFalse(bitSlider.ClassList.Contains("bit-dis"));
        }
        else
        {
            Assert.IsTrue(bitSlider.ClassList.Contains("bit-dis"));
        }

    }

    [DataTestMethod,
        DataRow(false),
        DataRow(true)
    ]
    public void BitSliderRangedTest(bool ranged)
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, ranged);
        });

        var bitSlider = com.Find(".bit-sld");

        Assert.IsTrue(ranged is false || bitSlider.ClassList.Contains("ranged-horizontal"));
        Assert.AreEqual(ranged ? 2 : 1, bitSlider.GetElementsByTagName("input").Length);
    }

    [DataTestMethod,
        DataRow(null, 3),
        DataRow(2, null),
        DataRow(2, 3),
        DataRow(null, null)
    ]
    public void BitSliderDefaultLowerValueTest(int? lowerValue, int? defaultLowerValue)
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.LowerValue, lowerValue);
            parameters.Add(p => p.DefaultLowerValue, defaultLowerValue);
            parameters.Add(p => p.ShowValue, true);
            parameters.Add(p => p.IsRanged, true);
        });

        // Find first label with valueLabel css class
        var label = com.Find(".value-label");

        var expectedValue = lowerValue.HasValue ? lowerValue : defaultLowerValue;
        Assert.AreEqual(expectedValue.GetValueOrDefault().ToString(), label.TextContent);
    }

    [DataTestMethod,
        DataRow(null, 3),
        DataRow(2, null),
        DataRow(2, 3),
        DataRow(null, null)
    ]
    public void BitSliderDefaultUpperValueTest(int? upperValue, int? defaultUpperValue)
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.UpperValue, upperValue);
            parameters.Add(p => p.DefaultUpperValue, defaultUpperValue);
            parameters.Add(p => p.ShowValue, true);
            parameters.Add(p => p.IsRanged, true);
        });

        // Find labels with valueLabel css class
        var labels = com.FindAll(".value-label");

        var expectedValue = upperValue.HasValue ? upperValue : defaultUpperValue;

        Assert.AreEqual(2, labels.Count);
        Assert.AreEqual(expectedValue.GetValueOrDefault().ToString(), labels.Last().TextContent);
    }

    [DataTestMethod,
        DataRow(null),
        DataRow(2)
    ]
    public void BitSliderLowerValueTest(int? lowerValue)
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.LowerValue, lowerValue);
            parameters.Add(p => p.UpperValue, 6);
            parameters.Add(p => p.ShowValue, true);
            parameters.Add(p => p.IsRanged, true);
        });

        var label = com.Find(".value-label");

        Assert.AreEqual(lowerValue.GetValueOrDefault().ToString(), label.TextContent);
    }

    [DataTestMethod,
        DataRow(null),
        DataRow(2),
    ]
    public void BitSliderUpperValueTest(int? defaultUpperValue)
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.DefaultUpperValue, defaultUpperValue);
            parameters.Add(p => p.ShowValue, true);
            parameters.Add(p => p.IsRanged, true);
        });

        // Find labels with valueLabel css class
        var labels = com.FindAll(".value-label");

        Assert.AreEqual(2, labels.Count);
        Assert.AreEqual(defaultUpperValue.GetValueOrDefault().ToString(), labels[^1].TextContent);
    }

    [DataTestMethod,
        DataRow(null, null),
        DataRow(2, null),
        DataRow(null, 6),
        DataRow(2, 6)
    ]
    public void BitSliderLowerAndUpperValueTest(int? lowerValue, int? upperValue)
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.RangeValue, new BitSliderRangeValue { Lower = lowerValue, Upper = upperValue });
            parameters.Add(p => p.ShowValue, true);
            parameters.Add(p => p.IsRanged, true);
        });

        // Find labels with valueLabel css class
        var labels = com.FindAll(".value-label");

        Assert.AreEqual(2, labels.Count);
        Assert.AreEqual(lowerValue.GetValueOrDefault().ToString(), labels[0].TextContent);
        Assert.AreEqual(upperValue.GetValueOrDefault().ToString(), labels[^1].TextContent);
    }

    [DataTestMethod,
        DataRow(null),
        DataRow(2)
    ]
    public void BitSliderDefaultValueTest(int? defaultValue)
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, defaultValue);
            parameters.Add(p => p.ShowValue, true);
        });

        // Find first label with valueLabel css class
        var label = com.Find(".value-label");

        Assert.AreEqual(defaultValue.GetValueOrDefault().ToString(), label.TextContent);
    }

    [DataTestMethod,
        DataRow(null),
        DataRow(2)
    ]
    public void BitSliderVerticalDefaultValueTest(int? defaultValue)
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, defaultValue);
            parameters.Add(p => p.IsVertical, true);
            parameters.Add(p => p.ShowValue, true);
        });

        // Find first label with valueLabel css class
        var label = com.Find(".value-label");

        Assert.AreEqual(defaultValue.GetValueOrDefault().ToString(), label.TextContent);
    }

    [DataTestMethod,
        DataRow(null),
        DataRow(2)
    ]
    public void BitSliderValueTest(int? value)
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Value, value);
            parameters.Add(p => p.ShowValue, true);
        });

        // Find first label with valueLabel css class
        var label = com.Find(".value-label");

        Assert.AreEqual(value.GetValueOrDefault().ToString(), label.TextContent);
    }

    [DataTestMethod,
        DataRow(null),
        DataRow(2)
    ]
    public void BitSliderVerticalValueTest(int? value)
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Value, value);
            parameters.Add(p => p.IsVertical, true);
            parameters.Add(p => p.ShowValue, true);
        });

        // Find first label with valueLabel css class
        var label = com.Find(".value-label");

        Assert.AreEqual(value.GetValueOrDefault().ToString(), label.TextContent);
    }

    [DataTestMethod,
        DataRow(false, null),
        DataRow(true, null),
        DataRow(false, 2),
        DataRow(true, 2)
    ]
    public void BitSliderStepTest(bool ranged, int? step)
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, ranged);
            if (step.HasValue)
            {
                parameters.Add(p => p.Step, step.Value);
            }
        });

        var inputs = com.FindAll(".bit-sld input");
        Assert.AreEqual(ranged ? 2 : 1, inputs.Count);

        foreach (var input in inputs)
        {
            Assert.AreEqual(input.GetAttribute("step"), (step ?? com.Instance.Step).ToString());
        }
    }

    [DataTestMethod,
        DataRow(false, 4, 8),
        DataRow(true, 4, 8)
    ]
    public void BitSliderMinMaxTest(bool ranged, int min, int max)
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, ranged);
            parameters.Add(p => p.Min, min);
            parameters.Add(p => p.Max, max);
        });

        var inputs = com.FindAll(".bit-sld input");
        Assert.AreEqual(ranged ? 2 : 1, inputs.Count);

        foreach (var input in inputs)
        {
            Assert.AreEqual(min.ToString(), input.GetAttribute("min"));
            Assert.AreEqual(max.ToString(), input.GetAttribute("max"));
        }
    }

    [DataTestMethod,
        DataRow(null),
        DataRow("Bit Title")
    ]
    public void BitSliderLabelTest(string label)
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Label, label);
        });

        // Find all labels with title css class
        // Method 'FindAll' is used because if the component does not have a label, the element will not be rendered. 
        var labelElements = com.FindAll(".label");
        var labelElement = labelElements.SingleOrDefault();

        if (label.HasValue())
        {
            Assert.IsNotNull(labelElement);
            Assert.AreEqual(label, labelElement.TextContent);
        }
        else
        {
            Assert.IsNull(labelElement);
        }
    }

    [DataTestMethod,
        DataRow(false, false),
        DataRow(true, false),
        DataRow(false, true),
        DataRow(true, true)
    ]
    public void BitSliderShowValueTest(bool ranged, bool showValue)
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, ranged);
            parameters.Add(p => p.ShowValue, showValue);
        });

        var labels = com.FindAll(".value-label");

        if (showValue)
        {
            Assert.IsTrue(labels.Any());
            Assert.AreEqual(ranged ? 2 : 1, labels.Count);
        }
        else
        {
            Assert.IsFalse(labels.Any());
        }
    }

    [DataTestMethod,
        DataRow(false, false),
        DataRow(true, false),
        DataRow(false, true),
        DataRow(true, true)
    ]
    public void BitSliderOriginFromZeroTest(bool ranged, bool originFromZero)
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, ranged);
            parameters.Add(p => p.IsOriginFromZero, originFromZero);
        });

        var spans = com.FindAll(".zero-tick");

        Assert.AreEqual(originFromZero ? 1 : 0, spans.Count);
    }

    [DataTestMethod,
        DataRow(false, null),
        DataRow(true, null),
        DataRow(false, "P01"),
        DataRow(true, "P01")
    ]
    public void BitSliderValueFormatTest(bool ranged, string valueFormat)
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.ShowValue, true);
            parameters.Add(p => p.IsRanged, ranged);
            parameters.Add(p => p.ValueFormat, valueFormat);
        });

        var labels = com.FindAll(".value-label");

        Assert.AreEqual(ranged ? 2 : 1, labels.Count(l => l.TextContent.Contains('%') == valueFormat.HasValue()));
    }
}
