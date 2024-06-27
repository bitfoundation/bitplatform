using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Inputs.Slider;

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
        Assert.IsTrue(bitSlider.ClassList.Contains(isVertical ? "bit-sld-vrt" : "bit-sld-hrz"));
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
        Assert.IsTrue(bitSlider.ClassList.Contains($"bit-sld-rgd-{(vertical ? "vrt" : "hrz")}"));
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

        Assert.IsTrue(ranged is false || bitSlider.ClassList.Contains("bit-sld-rgd-hrz"));
        Assert.AreEqual(ranged ? 2 : 1, bitSlider.GetElementsByTagName("input").Length);
    }

    [DataTestMethod,
        DataRow(-1000, 3D),
        DataRow(2, null),
        DataRow(2, 3D),
        DataRow(-1000, null)
    ]
    public void BitSliderDefaultLowerValueTest(double lowerValue, double? defaultLowerValue)
    {
        var hasLowerValue = lowerValue != -1000;
        var com = RenderComponent<BitSlider>(parameters =>
        {
            if (hasLowerValue) parameters.Add(p => p.LowerValue, lowerValue);
            parameters.Add(p => p.DefaultLowerValue, defaultLowerValue);
            parameters.Add(p => p.ShowValue, true);
            parameters.Add(p => p.IsRanged, true);
        });

        // Find first label with valueLabel css class
        var label = com.Find(".bit-sld-vlb");

        var expectedValue = hasLowerValue ? lowerValue : defaultLowerValue;

        Assert.AreEqual(expectedValue.GetValueOrDefault().ToString(), label.TextContent);
    }

    [DataTestMethod,
        DataRow(-1000, 3D),
        DataRow(2, null),
        DataRow(2, 3D),
        DataRow(-1000, null)
    ]
    public void BitSliderDefaultUpperValueTest(double upperValue, double? defaultUpperValue)
    {
        var hasUpperValue = upperValue != -1000;
        var com = RenderComponent<BitSlider>(parameters =>
        {
            if(hasUpperValue) parameters.Add(p => p.UpperValue, upperValue);
            parameters.Add(p => p.DefaultUpperValue, defaultUpperValue);
            parameters.Add(p => p.ShowValue, true);
            parameters.Add(p => p.IsRanged, true);
        });

        // Find labels with valueLabel css class
        var labels = com.FindAll(".bit-sld-vlb");

        var expectedValue = hasUpperValue ? upperValue : defaultUpperValue;

        Assert.AreEqual(2, labels.Count);
        Assert.AreEqual(expectedValue.GetValueOrDefault().ToString(), labels[^1].TextContent);
    }

    [DataTestMethod,
        DataRow(null),
        DataRow(2)
    ]
    public void BitSliderLowerValueTest(double lowerValue)
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.LowerValue, lowerValue);
            parameters.Add(p => p.UpperValue, 6);
            parameters.Add(p => p.ShowValue, true);
            parameters.Add(p => p.IsRanged, true);
        });

        var label = com.Find(".bit-sld-vlb");

        Assert.AreEqual(lowerValue.ToString(), label.TextContent);
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
        var labels = com.FindAll(".bit-sld-vlb");

        Assert.AreEqual(2, labels.Count);
        Assert.AreEqual(defaultUpperValue.GetValueOrDefault().ToString(), labels[^1].TextContent);
    }

    [DataTestMethod,
        DataRow(2, 6),
        DataRow(0, 10)
    ]
    public void BitSliderLowerAndUpperValueTest(double lowerValue, double upperValue)
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.RangeValue, new BitSliderRangeValue { Lower = lowerValue, Upper = upperValue });
            parameters.Add(p => p.ShowValue, true);
            parameters.Add(p => p.IsRanged, true);
        });

        // Find labels with valueLabel css class
        var labels = com.FindAll(".bit-sld-vlb");

        Assert.AreEqual(2, labels.Count);
        Assert.AreEqual(lowerValue.ToString(), labels[0].TextContent);
        Assert.AreEqual(upperValue.ToString(), labels[^1].TextContent);
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
        var label = com.Find(".bit-sld-vlb");

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
        var label = com.Find(".bit-sld-vlb");

        Assert.AreEqual(defaultValue.GetValueOrDefault().ToString(), label.TextContent);
    }

    [DataTestMethod,
        DataRow(5),
        DataRow(2)
    ]
    public void BitSliderValueTest(double value)
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Value, value);
            parameters.Add(p => p.ShowValue, true);
        });

        // Find first label with valueLabel css class
        var label = com.Find(".bit-sld-vlb");

        Assert.AreEqual(value.ToString(), label.TextContent);
    }

    [DataTestMethod,
        DataRow(5),
        DataRow(2)
    ]
    public void BitSliderVerticalValueTest(double value)
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Value, value);
            parameters.Add(p => p.IsVertical, true);
            parameters.Add(p => p.ShowValue, true);
        });

        // Find first label with valueLabel css class
        var label = com.Find(".bit-sld-vlb");

        Assert.AreEqual(value.ToString(), label.TextContent);
    }

    [DataTestMethod,
        DataRow(false, null),
        DataRow(true, null),
        DataRow(false, 2D),
        DataRow(true, 2D)
    ]
    public void BitSliderStepTest(bool ranged, double? step)
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
        var expected = (step ?? com.Instance.Step).ToString();

        Assert.AreEqual(ranged ? 2 : 1, inputs.Count);

        foreach (var input in inputs)
        {
            Assert.AreEqual(expected, input.GetAttribute("step"));
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
        var labelElements = com.FindAll(".bit-sld-lbl");
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

        var labels = com.FindAll(".bit-sld-vlb");

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

        var spans = com.FindAll(".bit-sld-ztk");

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

        var labels = com.FindAll(".bit-sld-vlb");

        Assert.AreEqual(ranged ? 2 : 1, labels.Count(l => l.TextContent.Contains('%') == valueFormat.HasValue()));
    }
}
