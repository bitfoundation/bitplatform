using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

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
    public void BitSliderVerticalTest(bool vertical)
    {
        var com = RenderComponent<BitSliderTest>(parameters =>
        {
            parameters.Add(p => p.Vertical, vertical);
        });

        var bitSlider = com.Find(".bit-sld");
        Assert.IsTrue(bitSlider.ClassList.Contains($"{(vertical ? "vertical" : "horizontal")}"));
    }

    [DataTestMethod,
        DataRow(false),
        DataRow(true)
    ]
    public void BitSliderRangedVerticalTest(bool vertical)
    {
        var com = RenderComponent<BitSliderTest>(parameters =>
        {
            parameters.Add(p => p.Vertical, vertical);
            parameters.Add(p => p.Ranged, true);
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
        var com = RenderComponent<BitSliderTest>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var bitSlider = com.Find(".bit-sld");

        if (isEnabled)
        {
            Assert.IsFalse(bitSlider.ClassList.Contains("disabled"));
        }
        else
        {
            Assert.IsTrue(bitSlider.ClassList.Contains("disabled"));
        }

    }

    [DataTestMethod,
        DataRow(false),
        DataRow(true)
    ]
    public void BitSliderRangedTest(bool ranged)
    {
        var com = RenderComponent<BitSliderTest>(parameters =>
        {
            parameters.Add(p => p.Ranged, ranged);
        });

        var bitSlider = com.Find(".bit-sld");

        Assert.IsTrue(ranged is false || bitSlider.ClassList.Contains($"ranged-horizontal"));
        Assert.AreEqual(bitSlider.GetElementsByTagName("input").Length, ranged ? 2 : 1);
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

        var actualValue = lowerValue.HasValue ? lowerValue : defaultLowerValue;
        Assert.AreEqual(label.TextContent, actualValue.GetValueOrDefault().ToString());
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

        var actualValue = upperValue.HasValue ? upperValue : defaultUpperValue;

        Assert.AreEqual(2, labels.Count);
        Assert.AreEqual(labels.Last().TextContent, actualValue.GetValueOrDefault().ToString());
    }

    [DataTestMethod,
        DataRow(null),
        DataRow(2)
    ]
    public void BitSliderLowerValueTest(int? lowerValue)
    {
        var com = RenderComponent<BitSliderTest>(parameters =>
        {
            parameters.Add(p => p.LowerValue, lowerValue);
            parameters.Add(p => p.UpperValue, 6);
            parameters.Add(p => p.ShowValue, true);
            parameters.Add(p => p.Ranged, true);
        });

        var label = com.Find(".value-label");

        Assert.AreEqual(label.TextContent, lowerValue.GetValueOrDefault().ToString());
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

        Assert.AreEqual(labels.Count, 2);
        Assert.AreEqual(labels[^1].TextContent, defaultUpperValue.GetValueOrDefault().ToString());
    }

    [DataTestMethod,
        DataRow(null, null),
        DataRow(2, null),
        DataRow(null, 6),
        DataRow(2, 6)
    ]
    public void BitSliderLowerAndUpperValueTest(int? lowerValue, int? upperValue)
    {
        var com = RenderComponent<BitSliderTest>(parameters =>
        {
            parameters.Add(p => p.RangeValue, new BitSliderRangeValue { Lower = lowerValue, Upper = upperValue });
            parameters.Add(p => p.ShowValue, true);
            parameters.Add(p => p.Ranged, true);
        });

        // Find labels with valueLabel css class
        var labels = com.FindAll(".value-label");

        Assert.AreEqual(labels.Count, 2);
        Assert.AreEqual(labels[0].TextContent, lowerValue.GetValueOrDefault().ToString());
        Assert.AreEqual(labels[^1].TextContent, upperValue.GetValueOrDefault().ToString());
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
        var com = RenderComponent<BitSliderTest>(parameters =>
        {
            parameters.Add(p => p.Value, value);
            parameters.Add(p => p.ShowValue, true);
        });

        // Find first label with valueLabel css class
        var label = com.Find(".value-label");

        Assert.AreEqual(label.TextContent, value.GetValueOrDefault().ToString());
    }

    [DataTestMethod,
        DataRow(null),
        DataRow(2)
    ]
    public void BitSliderVerticalValueTest(int? value)
    {
        var com = RenderComponent<BitSliderTest>(parameters =>
        {
            parameters.Add(p => p.Value, value);
            parameters.Add(p => p.Vertical, true);
            parameters.Add(p => p.ShowValue, true);
        });

        // Find first label with valueLabel css class
        var label = com.Find(".value-label");

        Assert.AreEqual(label.TextContent, value.GetValueOrDefault().ToString());
    }

    [DataTestMethod,
        DataRow(false, null),
        DataRow(true, null),
        DataRow(false, 2),
        DataRow(true, 2)
    ]
    public void BitSliderStepTest(bool ranged, int? step)
    {
        var com = RenderComponent<BitSliderTest>(parameters =>
        {
            parameters.Add(p => p.Ranged, ranged);
            if (step.HasValue)
            {
                parameters.Add(p => p.Step, step.Value);
            }
        });

        var inputs = com.FindAll(".bit-sld input");
        Assert.AreEqual(inputs.Count, ranged ? 2 : 1);

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
        var com = RenderComponent<BitSliderTest>(parameters =>
        {
            parameters.Add(p => p.Ranged, ranged);
            parameters.Add(p => p.Min, min);
            parameters.Add(p => p.Max, max);
        });

        var inputs = com.FindAll(".bit-sld input");
        Assert.AreEqual(inputs.Count, ranged ? 2 : 1);

        foreach (var input in inputs)
        {
            Assert.AreEqual(input.GetAttribute("min"), min.ToString());
            Assert.AreEqual(input.GetAttribute("max"), max.ToString());
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
            Assert.AreEqual(labelElement.TextContent, label);
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
        var com = RenderComponent<BitSliderTest>(parameters =>
        {
            parameters.Add(p => p.Ranged, ranged);
            parameters.Add(p => p.ShowValue, showValue);
        });

        var labels = com.FindAll(".value-label");

        if (showValue)
        {
            Assert.IsTrue(labels.Any());
            Assert.AreEqual(labels.Count, ranged ? 2 : 1);
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
        var com = RenderComponent<BitSliderTest>(parameters =>
        {
            parameters.Add(p => p.Ranged, ranged);
            parameters.Add(p => p.OriginFromZero, originFromZero);
        });

        var spans = com.FindAll(".zero-tick");

        Assert.AreEqual(spans.Count, originFromZero ? 1 : 0);
    }

    [DataTestMethod,
        DataRow(false, null),
        DataRow(true, null),
        DataRow(false, "P01"),
        DataRow(true, "P01")
    ]
    public void BitSliderValueFormatTest(bool ranged, string valueFormat)
    {
        var com = RenderComponent<BitSliderTest>(parameters =>
        {
            parameters.Add(p => p.ShowValue, true);
            parameters.Add(p => p.Ranged, ranged);
            parameters.Add(p => p.ValueFormat, valueFormat);
        });

        var labels = com.FindAll(".value-label");

        Assert.AreEqual(labels.Count(l => l.TextContent.Contains('%') == valueFormat.HasValue()), ranged ? 2 : 1);
    }
}
