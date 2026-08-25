using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Components.Web;
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



    #region Layout & root classes

    [TestMethod,
        DataRow(false),
        DataRow(true)
    ]
    public void BitSliderShouldRespectIsVertical(bool isVertical)
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsVertical, isVertical);
        });

        var slider = com.Find(".bit-sld");

        Assert.IsTrue(slider.ClassList.Contains(isVertical ? "bit-sld-vrt" : "bit-sld-hrz"));
        Assert.IsFalse(slider.ClassList.Contains(isVertical ? "bit-sld-hrz" : "bit-sld-vrt"));
    }

    [TestMethod,
        DataRow(false),
        DataRow(true)
    ]
    public void BitSliderShouldRespectIsRanged(bool ranged)
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, ranged);
        });

        var slider = com.Find(".bit-sld");

        Assert.AreEqual(ranged, slider.ClassList.Contains("bit-sld-rgd"));
        Assert.AreEqual(ranged ? 2 : 1, com.FindAll(".bit-sld-inp").Count);
        Assert.AreEqual(ranged ? 2 : 1, com.FindAll(".bit-sld-thb").Count);
    }

    [TestMethod,
        DataRow(false, false),
        DataRow(true, false),
        DataRow(false, true),
        DataRow(true, true)
    ]
    public void BitSliderShouldKeepTheLegacyCompositeClass(bool ranged, bool vertical)
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, ranged);
            parameters.Add(p => p.IsVertical, vertical);
        });

        var expected = $"bit-sld-{(ranged ? "rgd-" : null)}{(vertical ? "vrt" : "hrz")}";

        Assert.IsTrue(com.Find(".bit-sld").ClassList.Contains(expected));
    }

    [TestMethod,
        DataRow(false),
        DataRow(true)
    ]
    public void BitSliderShouldRespectIsEnabled(bool isEnabled)
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var slider = com.Find(".bit-sld");
        var input = com.Find(".bit-sld-inp");

        Assert.AreEqual(isEnabled is false, slider.ClassList.Contains("bit-dis"));
        Assert.AreEqual(isEnabled is false, input.HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitSliderShouldRespectReadOnly()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.DefaultValue, 4);
        });

        var slider = com.Find(".bit-sld");
        var input = com.Find(".bit-sld-inp");

        Assert.IsTrue(slider.ClassList.Contains("bit-sld-rdl"));
        Assert.AreEqual("true", input.GetAttribute("aria-readonly"));

        // A native range input ignores the readonly attribute, so the input is pinned by giving it a min and
        // a max that are both the current value: the browser then refuses the change itself.
        Assert.AreEqual("4", input.GetAttribute("min"));
        Assert.AreEqual("4", input.GetAttribute("max"));

        // The true bounds are still announced.
        Assert.AreEqual("0", input.GetAttribute("aria-valuemin"));
        Assert.AreEqual("10", input.GetAttribute("aria-valuemax"));

        // And the input is still focusable, unlike a disabled one.
        Assert.IsFalse(input.HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitSliderReadOnlyShouldRefuseAChange()
    {
        var changed = 0;
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.DefaultValue, 4);
            parameters.Add(p => p.OnChange, (double _) => changed++);
        });

        com.Find(".bit-sld-inp").Input("8");

        Assert.AreEqual(0, changed);
        Assert.AreEqual(4, com.Instance.Value);
    }

    [TestMethod,
        DataRow(BitVisibility.Visible, null),
        DataRow(BitVisibility.Hidden, "visibility:hidden"),
        DataRow(BitVisibility.Collapsed, "display:none")
    ]
    public void BitSliderShouldRespectVisibility(BitVisibility visibility, string expectedStyle)
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Visibility, visibility);
        });

        var style = com.Find(".bit-sld").GetAttribute("style") ?? string.Empty;

        if (expectedStyle is null)
        {
            Assert.IsFalse(style.Contains("visibility:hidden"));
            Assert.IsFalse(style.Contains("display:none"));
        }
        else
        {
            StringAssert.Contains(style, expectedStyle);
        }
    }

    [TestMethod]
    public void BitSliderShouldRespectDir()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Rtl);
        });

        var slider = com.Find(".bit-sld");

        Assert.IsTrue(slider.ClassList.Contains("bit-rtl"));
        Assert.AreEqual("rtl", slider.GetAttribute("dir"));
    }

    #endregion

    #region Color, size and thumb label classes

    [TestMethod,
        DataRow(null, "bit-sld-pri"),
        DataRow(BitColor.Primary, "bit-sld-pri"),
        DataRow(BitColor.Secondary, "bit-sld-sec"),
        DataRow(BitColor.Tertiary, "bit-sld-ter"),
        DataRow(BitColor.Info, "bit-sld-inf"),
        DataRow(BitColor.Success, "bit-sld-suc"),
        DataRow(BitColor.Warning, "bit-sld-wrn"),
        DataRow(BitColor.SevereWarning, "bit-sld-swr"),
        DataRow(BitColor.Error, "bit-sld-err"),
        DataRow(BitColor.PrimaryBackground, "bit-sld-pbg"),
        DataRow(BitColor.SecondaryBackground, "bit-sld-sbg"),
        DataRow(BitColor.TertiaryBackground, "bit-sld-tbg"),
        DataRow(BitColor.PrimaryForeground, "bit-sld-pfg"),
        DataRow(BitColor.SecondaryForeground, "bit-sld-sfg"),
        DataRow(BitColor.TertiaryForeground, "bit-sld-tfg"),
        DataRow(BitColor.PrimaryBorder, "bit-sld-pbr"),
        DataRow(BitColor.SecondaryBorder, "bit-sld-sbr"),
        DataRow(BitColor.TertiaryBorder, "bit-sld-tbr")
    ]
    public void BitSliderShouldRespectColor(BitColor? color, string expectedClass)
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Color, color);
        });

        Assert.IsTrue(com.Find(".bit-sld").ClassList.Contains(expectedClass));
    }

    [TestMethod,
        DataRow(null, "bit-sld-md"),
        DataRow(BitSize.Small, "bit-sld-sm"),
        DataRow(BitSize.Medium, "bit-sld-md"),
        DataRow(BitSize.Large, "bit-sld-lg")
    ]
    public void BitSliderShouldRespectSize(BitSize? size, string expectedClass)
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Size, size);
        });

        Assert.IsTrue(com.Find(".bit-sld").ClassList.Contains(expectedClass));
    }

    [TestMethod,
        DataRow(null, false, null),
        DataRow(BitSliderThumbLabel.Off, false, null),
        DataRow(BitSliderThumbLabel.Auto, true, "bit-sld-tla"),
        DataRow(BitSliderThumbLabel.On, true, "bit-sld-tlo")
    ]
    public void BitSliderShouldRespectThumbLabel(BitSliderThumbLabel? mode, bool rendered, string expectedClass)
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.ThumbLabel, mode);
            parameters.Add(p => p.DefaultValue, 3);
        });

        Assert.AreEqual(rendered ? 1 : 0, com.FindAll(".bit-sld-tlb").Count);

        if (expectedClass is not null)
        {
            Assert.IsTrue(com.Find(".bit-sld").ClassList.Contains(expectedClass));
            Assert.AreEqual("3", com.Find(".bit-sld-tlb").TextContent.Trim());
        }
    }

    [TestMethod]
    public void BitSliderShouldRespectThumbLabelTemplate()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.ThumbLabel, BitSliderThumbLabel.On);
            parameters.Add(p => p.Value, 3D);
            parameters.Add(p => p.ThumbLabelTemplate, (double v) => $"<span class='custom-thumb'>#{v}</span>");
        });

        var label = com.Find(".bit-sld-tlb .custom-thumb");

        Assert.AreEqual("#3", label.TextContent.Trim());
    }

    [TestMethod]
    public void BitSliderShouldRenderTheThumbLabelTemplateOnBothEndsOfARange()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.ThumbLabel, BitSliderThumbLabel.On);
            parameters.Add(p => p.LowerValue, 2D);
            parameters.Add(p => p.UpperValue, 7D);
            parameters.Add(p => p.ThumbLabelTemplate, (double v) => $"<span class='custom-thumb'>#{v}</span>");
        });

        var labels = com.FindAll(".bit-sld-tlb .custom-thumb");

        Assert.AreEqual(2, labels.Count);
        Assert.AreEqual("#2", labels[0].TextContent.Trim());
        Assert.AreEqual("#7", labels[1].TextContent.Trim());
    }

    [TestMethod]
    public void BitSliderShouldNotRenderTheThumbLabelTemplateWhileTheThumbLabelIsOff()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Value, 3D);
            parameters.Add(p => p.ThumbLabelTemplate, (double v) => $"<span class='custom-thumb'>#{v}</span>");
        });

        Assert.AreEqual(0, com.FindAll(".custom-thumb").Count);
    }

    [TestMethod]
    public void BitSliderShouldRenderAThumbLabelPerThumbWhenRanged()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.ThumbLabel, BitSliderThumbLabel.On);
            parameters.Add(p => p.DefaultLowerValue, 2D);
            parameters.Add(p => p.DefaultUpperValue, 7D);
        });

        var labels = com.FindAll(".bit-sld-tlb");

        Assert.AreEqual(2, labels.Count);
        Assert.AreEqual("2", labels[0].TextContent.Trim());
        Assert.AreEqual("7", labels[1].TextContent.Trim());
    }

    #endregion

    #region Label

    [TestMethod,
        DataRow(null),
        DataRow("Bit Label")
    ]
    public void BitSliderShouldRespectLabel(string label)
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Label, label);
        });

        var labelElement = com.FindAll(".bit-sld-lbl").SingleOrDefault();

        if (label.HasValue())
        {
            Assert.IsNotNull(labelElement);
            Assert.AreEqual(label, labelElement.TextContent.Trim());

            // The label is tied to the input that carries the tab stop, so clicking it lands the focus there.
            Assert.AreEqual(com.Find(".bit-sld-inp").GetAttribute("id"), labelElement.GetAttribute("for"));
        }
        else
        {
            Assert.IsNull(labelElement);
        }
    }

    [TestMethod]
    public void BitSliderShouldRespectLabelTemplate()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.LabelTemplate, "<span class='custom-label'>Custom</span>");
        });

        Assert.AreEqual(1, com.FindAll(".bit-sld-lbl .custom-label").Count);
    }

    #endregion

    #region Value, defaults and clamping

    [TestMethod,
        DataRow(0D),
        DataRow(2D),
        DataRow(5D)
    ]
    public void BitSliderShouldRespectValue(double value)
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Value, value);
        });

        Assert.AreEqual(value.ToString(CultureInfo.CurrentCulture), com.Find(".bit-sld-vlb").TextContent.Trim());
        Assert.AreEqual(value.ToString(CultureInfo.InvariantCulture), com.Find(".bit-sld-inp").GetAttribute("value"));
    }

    [TestMethod]
    public void BitSliderShouldRespectDefaultValue()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, 6D);
        });

        Assert.AreEqual(6, com.Instance.Value);
        Assert.AreEqual("6", com.Find(".bit-sld-vlb").TextContent.Trim());
    }

    [TestMethod]
    public void BitSliderShouldPreferTheBoundValueOverTheDefaultOne()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Value, 3D);
            parameters.Add(p => p.DefaultValue, 6D);
        });

        Assert.AreEqual(3, com.Instance.Value);
    }

    [TestMethod,
        DataRow(-5D, 0D),
        DataRow(99D, 10D),
        DataRow(4D, 4D)
    ]
    public void BitSliderShouldClampTheValueIntoTheRange(double value, double expected)
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Value, value);
        });

        Assert.AreEqual(expected, com.Instance.Value);
        Assert.AreEqual(expected.ToString(CultureInfo.CurrentCulture), com.Find(".bit-sld-vlb").TextContent.Trim());
    }

    [TestMethod]
    public void BitSliderShouldReadAnInvertedMinAndMaxInOrder()
    {
        // A Min above the Max would make the whole geometry run backwards - and divide by a negative range -
        // so the pair is read in order rather than taken literally.
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Min, 10D);
            parameters.Add(p => p.Max, 2D);
            parameters.Add(p => p.Value, 5D);
        });

        var input = com.Find(".bit-sld-inp");

        Assert.AreEqual("2", input.GetAttribute("min"));
        Assert.AreEqual("10", input.GetAttribute("max"));
    }

    [TestMethod]
    public void BitSliderShouldNotDivideByAnEmptyRange()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Min, 5D);
            parameters.Add(p => p.Max, 5D);
            parameters.Add(p => p.Value, 5D);
        });

        var fill = com.Find(".bit-sld-fil").GetAttribute("style") ?? string.Empty;

        Assert.IsFalse(fill.Contains("NaN"));
        Assert.IsFalse(fill.Contains("Infinity"));
    }

    #endregion

    #region Min, Max and Step attributes

    [TestMethod,
        DataRow(false, 4D, 8D),
        DataRow(true, -3D, 12D)
    ]
    public void BitSliderShouldRespectMinAndMax(bool ranged, double min, double max)
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, ranged);
            parameters.Add(p => p.Min, min);
            parameters.Add(p => p.Max, max);
        });

        var inputs = com.FindAll(".bit-sld-inp");

        Assert.AreEqual(ranged ? 2 : 1, inputs.Count);

        foreach (var input in inputs)
        {
            Assert.AreEqual(min.ToString(CultureInfo.InvariantCulture), input.GetAttribute("min"));
            Assert.AreEqual(max.ToString(CultureInfo.InvariantCulture), input.GetAttribute("max"));
            Assert.AreEqual(min.ToString(CultureInfo.InvariantCulture), input.GetAttribute("aria-valuemin"));
            Assert.AreEqual(max.ToString(CultureInfo.InvariantCulture), input.GetAttribute("aria-valuemax"));
        }
    }

    [TestMethod,
        DataRow(false, 1D, "1"),
        DataRow(true, 2.5D, "2.5"),
        // A step of zero or below would let the browser fall back to its own default silently,
        // so it is brought back to the documented 1 instead.
        DataRow(false, 0D, "1"),
        DataRow(false, -3D, "1")
    ]
    public void BitSliderShouldRespectStep(bool ranged, double step, string expected)
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, ranged);
            parameters.Add(p => p.Step, step);
        });

        foreach (var input in com.FindAll(".bit-sld-inp"))
        {
            Assert.AreEqual(expected, input.GetAttribute("step"));
        }
    }

    #endregion

    #region Value labels and formatting

    [TestMethod,
        DataRow(false, false),
        DataRow(true, false),
        DataRow(false, true),
        DataRow(true, true)
    ]
    public void BitSliderShouldRespectShowValue(bool ranged, bool showValue)
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, ranged);
            parameters.Add(p => p.ShowValue, showValue);
        });

        var labels = com.FindAll(".bit-sld-vlb");

        Assert.AreEqual(showValue ? (ranged ? 2 : 1) : 0, labels.Count);
    }

    [TestMethod]
    public void BitSliderShouldRespectValueFormat()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Max, 1D);
            parameters.Add(p => p.Step, 0.01D);
            parameters.Add(p => p.Value, 0.69D);
            parameters.Add(p => p.ValueFormat, "0 %");
        });

        Assert.AreEqual("69 %", com.Find(".bit-sld-vlb").TextContent.Trim());
    }

    [TestMethod]
    public void BitSliderShouldRespectGetValueTextOverValueFormat()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Value, 3D);
            parameters.Add(p => p.ValueFormat, "0.00");
            parameters.Add(p => p.GetValueText, (double v) => $"Step {v}");
        });

        Assert.AreEqual("Step 3", com.Find(".bit-sld-vlb").TextContent.Trim());
    }

    [TestMethod]
    public void BitSliderShouldOrderTheValueLabelsByEndWhenRanged()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.LowerValue, 2D);
            parameters.Add(p => p.UpperValue, 7D);
        });

        var labels = com.FindAll(".bit-sld-vlb");

        Assert.AreEqual(2, labels.Count);
        Assert.AreEqual("2", labels[0].TextContent.Trim());
        Assert.AreEqual("7", labels[1].TextContent.Trim());
    }

    [TestMethod,
        DataRow(false),
        DataRow(true)
    ]
    public void BitSliderShouldTrailTheValueLabelOfASingleValue(bool vertical)
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsVertical, vertical);
            parameters.Add(p => p.Value, 3D);
        });

        var children = com.Find(".bit-sld-cnt").Children;

        // The label follows the track in both orientations: beside it when lying down, under it when
        // standing up, which is the end of the scale a bottom-up fill starts from.
        Assert.IsTrue(children[^1].ClassList.Contains("bit-sld-vlb"));
        Assert.AreEqual("3", children[^1].TextContent.Trim());
    }

    [TestMethod]
    public void BitSliderShouldPutTheUpperValueOnTopWhenVerticalAndRanged()
    {
        // Standing upright, the labels stack instead of flanking, so the upper end belongs above the track.
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.IsVertical, true);
            parameters.Add(p => p.LowerValue, 2D);
            parameters.Add(p => p.UpperValue, 7D);
        });

        var labels = com.FindAll(".bit-sld-vlb");

        Assert.AreEqual("7", labels[0].TextContent.Trim());
        Assert.AreEqual("2", labels[1].TextContent.Trim());
    }

    #endregion

    #region Ranged values

    [TestMethod]
    public void BitSliderShouldRespectDefaultLowerAndUpperValue()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.DefaultLowerValue, 3D);
            parameters.Add(p => p.DefaultUpperValue, 8D);
        });

        Assert.AreEqual(3, com.Instance.LowerValue);
        Assert.AreEqual(8, com.Instance.UpperValue);
    }

    [TestMethod]
    public void BitSliderShouldPreferTheBoundEndsOverTheDefaultOnes()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.LowerValue, 1D);
            parameters.Add(p => p.UpperValue, 4D);
            parameters.Add(p => p.DefaultLowerValue, 3D);
            parameters.Add(p => p.DefaultUpperValue, 8D);
        });

        Assert.AreEqual(1, com.Instance.LowerValue);
        Assert.AreEqual(4, com.Instance.UpperValue);
    }

    [TestMethod]
    public void BitSliderShouldRespectRangeValue()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.RangeValue, new BitSliderRangeValue(2, 6));
        });

        Assert.AreEqual(2, com.Instance.LowerValue);
        Assert.AreEqual(6, com.Instance.UpperValue);
        Assert.AreEqual(new BitSliderRangeValue(2, 6), com.Instance.RangeValue);
    }

    [TestMethod]
    public void BitSliderShouldPreferRangeValueOverTheDefaultEnds()
    {
        // RangeValue carries both ends at once, so a slider given one is not an unbound slider looking for
        // a default - the defaults would silently overwrite half of what it was handed.
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.RangeValue, new BitSliderRangeValue(2, 6));
            parameters.Add(p => p.DefaultLowerValue, 3D);
            parameters.Add(p => p.DefaultUpperValue, 8D);
        });

        Assert.AreEqual(2, com.Instance.LowerValue);
        Assert.AreEqual(6, com.Instance.UpperValue);
    }

    [TestMethod]
    public void BitSliderShouldOrderTheEndsOfAnInvertedRange()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.LowerValue, 8D);
            parameters.Add(p => p.UpperValue, 2D);
        });

        Assert.AreEqual(2, com.Instance.LowerValue);
        Assert.AreEqual(8, com.Instance.UpperValue);
    }

    [TestMethod]
    public void BitSliderShouldClampTheEndsOfARangeIntoTheScale()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.Min, 0D);
            parameters.Add(p => p.Max, 10D);
            parameters.Add(p => p.LowerValue, -20D);
            parameters.Add(p => p.UpperValue, 40D);
        });

        Assert.AreEqual(0, com.Instance.LowerValue);
        Assert.AreEqual(10, com.Instance.UpperValue);
    }

    #endregion

    #region Interaction and events

    [TestMethod]
    public void BitSliderShouldChangeTheValueOnInput()
    {
        double? boundValue = null;
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Value, 2D);
            parameters.Add(p => p.ValueChanged, (double v) => boundValue = v);
        });

        com.Find(".bit-sld-inp").Input("7");

        Assert.AreEqual(7, boundValue);
        Assert.AreEqual(7, com.Instance.Value);
    }

    [TestMethod]
    public void BitSliderShouldCallOnChangeOnEveryStep()
    {
        var values = new List<double?>();
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.OnChange, (double v) => values.Add(v));
        });

        var input = com.Find(".bit-sld-inp");
        input.Input("3");
        input.Input("4");

        CollectionAssert.AreEqual(new List<double?> { 3, 4 }, values);
    }

    [TestMethod]
    public void BitSliderShouldCallOnChangeEndOnlyWhenTheInteractionEnds()
    {
        var stepCount = 0;
        double? committed = null;
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.OnChange, (double _) => stepCount++);
            parameters.Add(p => p.OnChangeEnd, (double v) => committed = v);
        });

        var input = com.Find(".bit-sld-inp");
        input.Input("3");
        input.Input("6");

        Assert.AreEqual(2, stepCount);
        Assert.IsNull(committed);

        input.Change("6");

        Assert.AreEqual(6, committed);
    }

    [TestMethod]
    public void BitSliderShouldRefuseAChangeOfAOneWayBoundValue()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Value, 2D);
        });

        com.Find(".bit-sld-inp").Input("7");

        Assert.AreEqual(2, com.Instance.Value);
    }

    [TestMethod]
    public void BitSliderShouldAcceptAChangeOfAOneWayBoundValueWhenOnChangeIsHandled()
    {
        double? changed = null;
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Value, 2D);
            parameters.Add(p => p.OnChange, (double v) => changed = v);
        });

        com.Find(".bit-sld-inp").Input("7");

        Assert.AreEqual(7, changed);
    }

    [TestMethod]
    public void BitSliderShouldNotChangeWhenDisabled()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.DefaultValue, 2D);
        });

        com.Find(".bit-sld-inp").Input("7");

        Assert.AreEqual(2, com.Instance.Value);
    }

    [TestMethod]
    public void BitSliderShouldMoveTheLowerEndOnInputOfTheLowerThumb()
    {
        double? lower = null;
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.LowerValue, 2D);
            parameters.Add(p => p.UpperValue, 8D);
            parameters.Add(p => p.LowerValueChanged, (double v) => lower = v);
            parameters.Add(p => p.UpperValueChanged, (double _) => { });
        });

        com.Find(".bit-sld-inp-lwr").Input("4");

        Assert.AreEqual(4, lower);
        Assert.AreEqual(4, com.Instance.LowerValue);
        Assert.AreEqual(8, com.Instance.UpperValue);
    }

    [TestMethod]
    public void BitSliderShouldNotifyRangeValueChangedWhenAnEndMoves()
    {
        // The composite parameter used to be left out of the notification entirely, which made
        // @bind-RangeValue a one-way binding in practice.
        BitSliderRangeValue? range = null;
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.RangeValue, new BitSliderRangeValue(2, 8));
            parameters.Add(p => p.RangeValueChanged, (BitSliderRangeValue? v) => range = v);
        });

        com.Find(".bit-sld-inp-upr").Input("5");

        Assert.IsNotNull(range);
        Assert.AreEqual(2, range.Lower);
        Assert.AreEqual(5, range.Upper);
    }

    [TestMethod]
    public void BitSliderShouldCallOnRangeChangeAndOnRangeChangeEnd()
    {
        var stepCount = 0;
        BitSliderRangeValue? committed = null;
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.DefaultLowerValue, 2D);
            parameters.Add(p => p.DefaultUpperValue, 8D);
            parameters.Add(p => p.OnRangeChange, (BitSliderRangeValue _) => stepCount++);
            parameters.Add(p => p.OnRangeChangeEnd, (BitSliderRangeValue v) => committed = v);
        });

        var upper = com.Find(".bit-sld-inp-upr");
        upper.Input("7");
        upper.Input("6");

        Assert.AreEqual(2, stepCount);
        Assert.IsNull(committed);

        upper.Change("6");

        Assert.IsNotNull(committed);
        Assert.AreEqual(2, committed.Lower);
        Assert.AreEqual(6, committed.Upper);
    }

    [TestMethod]
    public void BitSliderShouldRefuseARangeChangeOfAOneWayBoundEnd()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.LowerValue, 2D);
            parameters.Add(p => p.UpperValue, 8D);
        });

        com.Find(".bit-sld-inp-lwr").Input("4");

        Assert.AreEqual(2, com.Instance.LowerValue);
        Assert.AreEqual(8, com.Instance.UpperValue);
    }

    [TestMethod]
    public void BitSliderShouldCallOnFocusInAndOnFocusOut()
    {
        var focusedIn = 0;
        var focusedOut = 0;
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.OnFocusIn, () => focusedIn++);
            parameters.Add(p => p.OnFocusOut, () => focusedOut++);
        });

        var input = com.Find(".bit-sld-inp");
        input.FocusIn();
        input.FocusOut();

        Assert.AreEqual(1, focusedIn);
        Assert.AreEqual(1, focusedOut);
    }

    [TestMethod]
    public void BitSliderShouldCallTheFocusEventsFromBothThumbsOfARange()
    {
        var focusedIn = 0;
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.OnFocusIn, () => focusedIn++);
        });

        com.Find(".bit-sld-inp-lwr").FocusIn();
        com.Find(".bit-sld-inp-upr").FocusIn();

        Assert.AreEqual(2, focusedIn);
    }

    [TestMethod]
    public void BitSliderShouldNotCallTheFocusEventsWhenDisabled()
    {
        var focusedIn = 0;
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.OnFocusIn, () => focusedIn++);
        });

        com.Find(".bit-sld-inp").FocusIn();

        Assert.AreEqual(0, focusedIn);
    }

    [TestMethod]
    public void BitSliderShouldLetTheThumbsCrossWhenUnconstrained()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.DefaultLowerValue, 2D);
            parameters.Add(p => p.DefaultUpperValue, 5D);
        });

        // Dragging the lower thumb past the upper one makes it the upper end.
        com.Find(".bit-sld-inp-lwr").Input("9");

        Assert.AreEqual(5, com.Instance.LowerValue);
        Assert.AreEqual(9, com.Instance.UpperValue);

        // And the input that overtook keeps holding the upper end, so it is not yanked back mid-drag.
        Assert.AreEqual("9", com.Find(".bit-sld-inp-lwr").GetAttribute("value"));
        Assert.AreEqual("5", com.Find(".bit-sld-inp-upr").GetAttribute("value"));
    }

    #endregion

    #region Range constraints

    [TestMethod]
    public void BitSliderShouldBoundTheInputsAgainstEachOtherWhenNoSwap()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.NoSwap, true);
            parameters.Add(p => p.Max, 100D);
            parameters.Add(p => p.DefaultLowerValue, 30D);
            parameters.Add(p => p.DefaultUpperValue, 70D);
        });

        // The constraint is pushed into the bounds of the inputs, so the browser itself never produces a
        // value the slider would have to reject.
        Assert.AreEqual("70", com.Find(".bit-sld-inp-lwr").GetAttribute("max"));
        Assert.AreEqual("30", com.Find(".bit-sld-inp-upr").GetAttribute("min"));
    }

    [TestMethod]
    public void BitSliderShouldBoundTheInputsByMinRange()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.MinRange, 20D);
            parameters.Add(p => p.Max, 100D);
            parameters.Add(p => p.DefaultLowerValue, 20D);
            parameters.Add(p => p.DefaultUpperValue, 60D);
        });

        Assert.AreEqual("40", com.Find(".bit-sld-inp-lwr").GetAttribute("max"));
        Assert.AreEqual("40", com.Find(".bit-sld-inp-upr").GetAttribute("min"));
    }

    [TestMethod]
    public void BitSliderShouldBoundTheInputsByMaxRange()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.MaxRange, 30D);
            parameters.Add(p => p.Max, 100D);
            parameters.Add(p => p.DefaultLowerValue, 30D);
            parameters.Add(p => p.DefaultUpperValue, 50D);
        });

        Assert.AreEqual("20", com.Find(".bit-sld-inp-lwr").GetAttribute("min"));
        Assert.AreEqual("60", com.Find(".bit-sld-inp-upr").GetAttribute("max"));
    }

    [TestMethod]
    public void BitSliderShouldLayEachConstrainedInputOverTheSpanItsOwnBoundsCover()
    {
        // The constraints narrow the bounds of the inputs, and an input still stretched across the whole
        // track would map its value onto the wrong span: its own thumb - the only part of it that takes the
        // pointer - would sit well away from the drawn one, and the slider could not be grabbed where it is
        // seen. So each input is laid over exactly the piece of the track its bounds describe.
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.NoSwap, true);
            parameters.Add(p => p.Max, 100D);
            parameters.Add(p => p.DefaultLowerValue, 30D);
            parameters.Add(p => p.DefaultUpperValue, 70D);
        });

        var lower = com.Find(".bit-sld-inp-lwr").GetAttribute("style");
        var upper = com.Find(".bit-sld-inp-upr").GetAttribute("style");

        // The lower input runs from the start of the scale to the upper value, the upper one from the lower
        // value to the end - the very bounds each of them carries.
        StringAssert.Contains(lower, "--bit-sld-inp-start:0;");
        StringAssert.Contains(lower, "--bit-sld-inp-end:0.7");
        StringAssert.Contains(upper, "--bit-sld-inp-start:0.3;");
        StringAssert.Contains(upper, "--bit-sld-inp-end:1");
    }

    [TestMethod]
    public void BitSliderShouldLayAnUnconstrainedInputOverTheWholeTrack()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.Max, 100D);
            parameters.Add(p => p.DefaultLowerValue, 30D);
            parameters.Add(p => p.DefaultUpperValue, 70D);
        });

        foreach (var input in com.FindAll(".bit-sld-inp"))
        {
            var style = input.GetAttribute("style");

            StringAssert.Contains(style, "--bit-sld-inp-start:0;");
            StringAssert.Contains(style, "--bit-sld-inp-end:1");
        }
    }

    [TestMethod]
    public void BitSliderShouldWidenAStartingPairThatIsCloserThanMinRange()
    {
        // The constraints are handed to the inputs as their own bounds, which only works while the current
        // values already satisfy them - otherwise the browser would clamp an input somewhere the component
        // is not drawing.
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.MinRange, 20D);
            parameters.Add(p => p.Max, 100D);
            parameters.Add(p => p.DefaultLowerValue, 30D);
            parameters.Add(p => p.DefaultUpperValue, 35D);
        });

        Assert.AreEqual(30, com.Instance.LowerValue);
        Assert.AreEqual(50, com.Instance.UpperValue);

        // And the bounds that come out of it hold both inputs.
        Assert.AreEqual("30", com.Find(".bit-sld-inp-lwr").GetAttribute("max"));
        Assert.AreEqual("50", com.Find(".bit-sld-inp-upr").GetAttribute("min"));
    }

    [TestMethod]
    public void BitSliderShouldWidenAgainstTheFarEndWhenThereIsNoRoomAbove()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.MinRange, 40D);
            parameters.Add(p => p.Max, 100D);
            parameters.Add(p => p.DefaultLowerValue, 90D);
            parameters.Add(p => p.DefaultUpperValue, 95D);
        });

        Assert.AreEqual(60, com.Instance.LowerValue);
        Assert.AreEqual(100, com.Instance.UpperValue);
    }

    [TestMethod]
    public void BitSliderShouldNarrowAStartingPairThatIsWiderThanMaxRange()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.MaxRange, 30D);
            parameters.Add(p => p.Max, 100D);
            parameters.Add(p => p.DefaultLowerValue, 10D);
            parameters.Add(p => p.DefaultUpperValue, 90D);
        });

        Assert.AreEqual(10, com.Instance.LowerValue);
        Assert.AreEqual(40, com.Instance.UpperValue);
    }

    [TestMethod]
    public void BitSliderShouldTreatANegativeMinRangeAsNoDistance()
    {
        // Taken literally in the bounds of the inputs, a negative distance would be a licence to overlap -
        // and would quietly undo the ordering NoSwap is asking for.
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.NoSwap, true);
            parameters.Add(p => p.MinRange, -20D);
            parameters.Add(p => p.Max, 100D);
            parameters.Add(p => p.DefaultLowerValue, 30D);
            parameters.Add(p => p.DefaultUpperValue, 70D);
        });

        Assert.AreEqual("70", com.Find(".bit-sld-inp-lwr").GetAttribute("max"));
        Assert.AreEqual("30", com.Find(".bit-sld-inp-upr").GetAttribute("min"));
    }

    [TestMethod]
    public void BitSliderShouldHoldAContradictingMaxRangeAtTheMinRange()
    {
        // A MaxRange under the MinRange would hand the lower input a min above its own max, which the browser
        // resolves by pinning the input somewhere the component is not drawing.
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.MinRange, 30D);
            parameters.Add(p => p.MaxRange, 10D);
            parameters.Add(p => p.Max, 100D);
            parameters.Add(p => p.DefaultLowerValue, 20D);
            parameters.Add(p => p.DefaultUpperValue, 50D);
        });

        Assert.AreEqual(20, com.Instance.LowerValue);
        Assert.AreEqual(50, com.Instance.UpperValue);

        var lower = com.Find(".bit-sld-inp-lwr");
        var upper = com.Find(".bit-sld-inp-upr");

        Assert.AreEqual("20", lower.GetAttribute("min"));
        Assert.AreEqual("20", lower.GetAttribute("max"));
        Assert.AreEqual("50", upper.GetAttribute("min"));
        Assert.AreEqual("50", upper.GetAttribute("max"));
    }

    [TestMethod]
    public void BitSliderShouldTreatANegativeMaxRangeAsZero()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.MaxRange, -30D);
            parameters.Add(p => p.Max, 100D);
            parameters.Add(p => p.DefaultLowerValue, 20D);
            parameters.Add(p => p.DefaultUpperValue, 60D);
        });

        // No distance at all is allowed between the thumbs, so the pair collapses onto the lower end.
        Assert.AreEqual(20, com.Instance.LowerValue);
        Assert.AreEqual(20, com.Instance.UpperValue);
    }

    [TestMethod]
    public void BitSliderShouldReportACorrectedRangeToItsBinding()
    {
        // A pair the slider has to move is a pair the parent is now holding a different value for, so the
        // correction travels back rather than leaving the two silently disagreeing.
        double? lower = null;
        double? upper = null;
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.Min, 0D);
            parameters.Add(p => p.Max, 10D);
            parameters.Add(p => p.LowerValue, -20D);
            parameters.Add(p => p.UpperValue, 40D);
            parameters.Add(p => p.LowerValueChanged, (double v) => lower = v);
            parameters.Add(p => p.UpperValueChanged, (double v) => upper = v);
        });

        Assert.AreEqual(0, lower);
        Assert.AreEqual(10, upper);
    }

    [TestMethod]
    public void BitSliderShouldReportARangeWidenedByMinRange()
    {
        BitSliderRangeValue? range = null;
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.MinRange, 20D);
            parameters.Add(p => p.Max, 100D);
            parameters.Add(p => p.RangeValue, new BitSliderRangeValue(30, 35));
            parameters.Add(p => p.RangeValueChanged, (BitSliderRangeValue? v) => range = v);
        });

        Assert.IsNotNull(range);
        Assert.AreEqual(30, range.Lower);
        Assert.AreEqual(50, range.Upper);
    }

    [TestMethod]
    public void BitSliderShouldNotReportARangeThatNeededNoCorrection()
    {
        var reports = 0;
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.Max, 100D);
            parameters.Add(p => p.LowerValue, 30D);
            parameters.Add(p => p.UpperValue, 70D);
            parameters.Add(p => p.LowerValueChanged, (double _) => reports++);
        });

        Assert.AreEqual(0, reports);
    }

    [TestMethod]
    public void BitSliderShouldReportTheSameRejectedRangeOnlyOnce()
    {
        // A parent that does not store what it is told would otherwise hand the same rejected pair back on
        // the render the report itself caused, and the two would keep correcting each other forever.
        var reports = 0;
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.Max, 10D);
            parameters.Add(p => p.LowerValue, -20D);
            parameters.Add(p => p.UpperValue, 40D);
            parameters.Add(p => p.LowerValueChanged, (double _) => reports++);
        });

        com.Render();
        com.Render();

        Assert.AreEqual(1, reports);
    }

    [TestMethod]
    public void BitSliderShouldNotBoundTheInputsWhenUnconstrained()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.Max, 100D);
            parameters.Add(p => p.DefaultLowerValue, 30D);
            parameters.Add(p => p.DefaultUpperValue, 70D);
        });

        Assert.AreEqual("0", com.Find(".bit-sld-inp-lwr").GetAttribute("min"));
        Assert.AreEqual("100", com.Find(".bit-sld-inp-lwr").GetAttribute("max"));
        Assert.AreEqual("0", com.Find(".bit-sld-inp-upr").GetAttribute("min"));
        Assert.AreEqual("100", com.Find(".bit-sld-inp-upr").GetAttribute("max"));
    }

    #endregion

    #region Pushable

    [TestMethod]
    public void BitSliderShouldFreeTheInputsOfEachOtherWhenPushable()
    {
        // A thumb that pushes the other is no longer bounded by it: it is bounded by the end of the scale,
        // less the room the pushed thumb needs to keep there.
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.Pushable, true);
            parameters.Add(p => p.MinRange, 20D);
            parameters.Add(p => p.Max, 100D);
            parameters.Add(p => p.DefaultLowerValue, 20D);
            parameters.Add(p => p.DefaultUpperValue, 60D);
        });

        Assert.AreEqual("0", com.Find(".bit-sld-inp-lwr").GetAttribute("min"));
        Assert.AreEqual("80", com.Find(".bit-sld-inp-lwr").GetAttribute("max"));
        Assert.AreEqual("20", com.Find(".bit-sld-inp-upr").GetAttribute("min"));
        Assert.AreEqual("100", com.Find(".bit-sld-inp-upr").GetAttribute("max"));
    }

    [TestMethod]
    public void BitSliderShouldPushTheUpperEndAlongWithTheLowerOne()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.Pushable, true);
            parameters.Add(p => p.MinRange, 20D);
            parameters.Add(p => p.Max, 100D);
            parameters.Add(p => p.DefaultLowerValue, 20D);
            parameters.Add(p => p.DefaultUpperValue, 60D);
        });

        com.Find(".bit-sld-inp-lwr").Input("70");

        Assert.AreEqual(70, com.Instance.LowerValue);
        Assert.AreEqual(90, com.Instance.UpperValue);
    }

    [TestMethod]
    public void BitSliderShouldPushTheLowerEndAlongWithTheUpperOne()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.Pushable, true);
            parameters.Add(p => p.MinRange, 20D);
            parameters.Add(p => p.Max, 100D);
            parameters.Add(p => p.DefaultLowerValue, 20D);
            parameters.Add(p => p.DefaultUpperValue, 60D);
        });

        com.Find(".bit-sld-inp-upr").Input("30");

        Assert.AreEqual(10, com.Instance.LowerValue);
        Assert.AreEqual(30, com.Instance.UpperValue);
    }

    [TestMethod]
    public void BitSliderShouldStopThePushedEndAtTheEndOfTheScale()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.Pushable, true);
            parameters.Add(p => p.MinRange, 20D);
            parameters.Add(p => p.Max, 100D);
            parameters.Add(p => p.DefaultLowerValue, 20D);
            parameters.Add(p => p.DefaultUpperValue, 60D);
        });

        com.Find(".bit-sld-inp-lwr").Input("95");

        // The pushed thumb runs out of room at the far end, and the pushing one comes to rest beside it
        // rather than through it.
        Assert.AreEqual(80, com.Instance.LowerValue);
        Assert.AreEqual(100, com.Instance.UpperValue);
    }

    [TestMethod]
    public void BitSliderShouldPushWithoutADistanceWhenOnlyOrdered()
    {
        // NoSwap alone asks for the ordering and nothing else, so the thumbs may meet - and a pushable pair
        // that meets travels on together.
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.Pushable, true);
            parameters.Add(p => p.NoSwap, true);
            parameters.Add(p => p.Max, 100D);
            parameters.Add(p => p.DefaultLowerValue, 30D);
            parameters.Add(p => p.DefaultUpperValue, 70D);
        });

        com.Find(".bit-sld-inp-lwr").Input("90");

        Assert.AreEqual(90, com.Instance.LowerValue);
        Assert.AreEqual(90, com.Instance.UpperValue);
    }

    [TestMethod]
    public void BitSliderShouldIgnorePushableWithNothingToPushAgainst()
    {
        // Without a distance to keep or an ordering to hold, the thumbs simply trade places the way they
        // always did, and there is nothing for a push to mean.
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.Pushable, true);
            parameters.Add(p => p.DefaultLowerValue, 2D);
            parameters.Add(p => p.DefaultUpperValue, 5D);
        });

        com.Find(".bit-sld-inp-lwr").Input("9");

        Assert.AreEqual(5, com.Instance.LowerValue);
        Assert.AreEqual(9, com.Instance.UpperValue);
    }

    [TestMethod]
    public void BitSliderShouldKeepThePushedPairInOrderOnTheInputs()
    {
        // Pushing and crossing are opposites, so the two inputs keep their sides and the values they carry
        // stay in order however far a thumb is taken.
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.Pushable, true);
            parameters.Add(p => p.MinRange, 20D);
            parameters.Add(p => p.Max, 100D);
            parameters.Add(p => p.DefaultLowerValue, 20D);
            parameters.Add(p => p.DefaultUpperValue, 60D);
        });

        com.Find(".bit-sld-inp-lwr").Input("70");

        Assert.AreEqual("70", com.Find(".bit-sld-inp-lwr").GetAttribute("value"));
        Assert.AreEqual("90", com.Find(".bit-sld-inp-upr").GetAttribute("value"));
    }

    #endregion

    #region Hit areas

    [TestMethod]
    public void BitSliderShouldCutTheHitAreasOfARangeAtTheMidpointBetweenTheThumbs()
    {
        // The two inputs cover each other completely, so each one is cut down to its own side of the
        // midpoint: every point of the rail then belongs to the input whose thumb is nearest to it, which is
        // what lets a press on the bare rail move the near thumb instead of nothing at all.
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.Max, 100D);
            parameters.Add(p => p.LowerValue, 20D);
            parameters.Add(p => p.UpperValue, 60D);
        });

        // Both inputs run across the whole scale here, so the cut sits at the midpoint of each of them.
        StringAssert.Contains(com.Find(".bit-sld-inp-lwr").GetAttribute("style"), "--bit-sld-inp-mid:0.4");
        StringAssert.Contains(com.Find(".bit-sld-inp-upr").GetAttribute("style"), "--bit-sld-inp-mid:0.4");
    }

    [TestMethod]
    public void BitSliderShouldMeasureTheCutInsideTheSpanOfAConstrainedInput()
    {
        // A constrained input covers only part of the track, and the cut is a fraction of its own box rather
        // than of the whole scale - so the same midpoint comes out as two different fractions.
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.NoSwap, true);
            parameters.Add(p => p.Max, 100D);
            parameters.Add(p => p.DefaultLowerValue, 30D);
            parameters.Add(p => p.DefaultUpperValue, 70D);
        });

        // The lower input runs from 0 to 70 and the upper from 30 to 100, and the midpoint of the pair is 50.
        StringAssert.Contains(com.Find(".bit-sld-inp-lwr").GetAttribute("style"), "--bit-sld-inp-mid:0.7143");
        StringAssert.Contains(com.Find(".bit-sld-inp-upr").GetAttribute("style"), "--bit-sld-inp-mid:0.2857");
    }

    [TestMethod]
    public void BitSliderShouldPlaceTheInputsByWhereTheirThumbsStand()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.DefaultLowerValue, 2D);
            parameters.Add(p => p.DefaultUpperValue, 5D);
        });

        Assert.IsTrue(com.Find(".bit-sld-inp-lwr").ClassList.Contains("bit-sld-inp-near"));
        Assert.IsTrue(com.Find(".bit-sld-inp-upr").ClassList.Contains("bit-sld-inp-far"));
    }

    [TestMethod]
    public void BitSliderShouldFollowTheInputsWhenTheyCross()
    {
        // The half of the rail an input takes the pointer on has to follow its thumb rather than the end it
        // is named for: a crossed pair that kept its named sides would have each input cut away from its own
        // thumb, and neither one could be grabbed at all.
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.DefaultLowerValue, 2D);
            parameters.Add(p => p.DefaultUpperValue, 5D);
        });

        com.Find(".bit-sld-inp-lwr").Input("9");

        Assert.IsTrue(com.Find(".bit-sld-inp-lwr").ClassList.Contains("bit-sld-inp-far"));
        Assert.IsTrue(com.Find(".bit-sld-inp-upr").ClassList.Contains("bit-sld-inp-near"));
    }

    [TestMethod]
    public void BitSliderShouldNotCutTheHitAreaOfASingleValue()
    {
        // There is nothing to share a single-value slider's track with.
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Value, 4D);
        });

        Assert.IsFalse((com.Find(".bit-sld-inp").GetAttribute("style") ?? string.Empty).Contains("--bit-sld-inp-mid"));
    }

    #endregion

    #region Fill, origin and inversion

    [TestMethod]
    public void BitSliderShouldFillFromTheNearEndByDefault()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Max, 10D);
            parameters.Add(p => p.Value, 3D);
        });

        var style = com.Find(".bit-sld-fil").GetAttribute("style");

        StringAssert.Contains(style, "inset-inline-start:0%");
        StringAssert.Contains(style, "width:30%");
    }

    [TestMethod]
    public void BitSliderShouldFillFromTheFarEndWhenInverted()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Max, 10D);
            parameters.Add(p => p.Value, 3D);
            parameters.Add(p => p.Inverted, true);
        });

        var style = com.Find(".bit-sld-fil").GetAttribute("style");

        StringAssert.Contains(style, "inset-inline-start:30%");
        StringAssert.Contains(style, "width:70%");
        Assert.IsTrue(com.Find(".bit-sld").ClassList.Contains("bit-sld-ivt"));
    }

    [TestMethod]
    public void BitSliderShouldFillOutOfZeroWhenTheOriginIsAttachedToIt()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Min, -5D);
            parameters.Add(p => p.Max, 5D);
            parameters.Add(p => p.Value, 3D);
            parameters.Add(p => p.IsOriginFromZero, true);
        });

        var style = com.Find(".bit-sld-fil").GetAttribute("style");

        // Zero is halfway along a -5..5 scale, and the value covers three tenths of the way to the far end.
        StringAssert.Contains(style, "inset-inline-start:50%");
        StringAssert.Contains(style, "width:30%");
    }

    [TestMethod]
    public void BitSliderShouldPutTheOriginTickWhereZeroActuallySits()
    {
        // The old implementation put the tick at the geometric middle of the track, which is only where zero
        // is when the scale happens to be symmetric around it.
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Min, -2D);
            parameters.Add(p => p.Max, 8D);
            parameters.Add(p => p.Value, 4D);
            parameters.Add(p => p.IsOriginFromZero, true);
        });

        StringAssert.Contains(com.Find(".bit-sld-ztk").GetAttribute("style"), "inset-inline-start:20%");
    }

    [TestMethod]
    public void BitSliderShouldFillOutOfAnArbitraryOrigin()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Max, 100D);
            parameters.Add(p => p.Origin, 60D);
            parameters.Add(p => p.Value, 85D);
        });

        var style = com.Find(".bit-sld-fil").GetAttribute("style");

        StringAssert.Contains(style, "inset-inline-start:60%");
        StringAssert.Contains(style, "width:25%");
    }

    [TestMethod]
    public void BitSliderShouldFillBelowAnOriginTheValueSitsUnder()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Max, 100D);
            parameters.Add(p => p.Origin, 60D);
            parameters.Add(p => p.Value, 20D);
        });

        var style = com.Find(".bit-sld-fil").GetAttribute("style");

        StringAssert.Contains(style, "inset-inline-start:20%");
        StringAssert.Contains(style, "width:40%");
    }

    [TestMethod,
        DataRow(false, false, 0),
        DataRow(true, false, 1),
        DataRow(false, true, 1)
    ]
    public void BitSliderShouldRenderTheOriginTickOnlyWhenThereIsAnOrigin(bool fromZero, bool hasOrigin, int expected)
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsOriginFromZero, fromZero);
            if (hasOrigin) parameters.Add(p => p.Origin, 4D);
        });

        Assert.AreEqual(expected, com.FindAll(".bit-sld-ztk").Count);
    }

    [TestMethod]
    public void BitSliderShouldFillBetweenTheThumbsWhenRanged()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.Max, 100D);
            parameters.Add(p => p.LowerValue, 20D);
            parameters.Add(p => p.UpperValue, 60D);
        });

        var fills = com.FindAll(".bit-sld-fil");

        Assert.AreEqual(1, fills.Count);
        StringAssert.Contains(fills[0].GetAttribute("style"), "inset-inline-start:20%");
        StringAssert.Contains(fills[0].GetAttribute("style"), "width:40%");
    }

    [TestMethod]
    public void BitSliderShouldFillTheTwoOuterSegmentsOfAnInvertedRange()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.Inverted, true);
            parameters.Add(p => p.Max, 100D);
            parameters.Add(p => p.LowerValue, 35D);
            parameters.Add(p => p.UpperValue, 65D);
        });

        var fills = com.FindAll(".bit-sld-fil");

        Assert.AreEqual(2, fills.Count);
        StringAssert.Contains(fills[0].GetAttribute("style"), "inset-inline-start:0%");
        StringAssert.Contains(fills[0].GetAttribute("style"), "width:35%");
        StringAssert.Contains(fills[1].GetAttribute("style"), "inset-inline-start:65%");
        StringAssert.Contains(fills[1].GetAttribute("style"), "width:35%");
    }

    [TestMethod,
        DataRow(false),
        DataRow(true)
    ]
    public void BitSliderShouldDrawNoFillAtAllWhenNoFill(bool ranged)
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.NoFill, true);
            parameters.Add(p => p.IsRanged, ranged);
            parameters.Add(p => p.Max, 10D);
            parameters.Add(p => p.Value, 4D);
            parameters.Add(p => p.LowerValue, 2D);
            parameters.Add(p => p.UpperValue, 8D);
        });

        Assert.AreEqual(0, com.FindAll(".bit-sld-fil").Count);

        // The rail, the thumbs and everything else that is not the fill are still there.
        Assert.AreEqual(1, com.FindAll(".bit-sld-trk").Count);
        Assert.AreEqual(ranged ? 2 : 1, com.FindAll(".bit-sld-thb").Count);
    }

    [TestMethod]
    public void BitSliderShouldNotHighlightAnyMarkWhenNoFill()
    {
        // Nothing has been reached on a slider that draws no fill in the first place.
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.NoFill, true);
            parameters.Add(p => p.Max, 10D);
            parameters.Add(p => p.Value, 4D);
            parameters.Add(p => p.ShowMarks, true);
        });

        var marks = com.FindAll(".bit-sld-mrk");

        Assert.AreEqual(11, marks.Count);
        Assert.AreEqual(0, marks.Count(m => m.ClassList.Contains("bit-sld-mrk-act")));
    }

    [TestMethod]
    public void BitSliderShouldNotRenderTheOriginTickWhenNoFill()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.NoFill, true);
            parameters.Add(p => p.Min, -5D);
            parameters.Add(p => p.Max, 5D);
            parameters.Add(p => p.IsOriginFromZero, true);
        });

        Assert.AreEqual(0, com.FindAll(".bit-sld-ztk").Count);
    }

    [TestMethod]
    public void BitSliderShouldNotRenderTheOriginTickWhenRanged()
    {
        // A ranged slider fills between its own two ends, so an origin has nothing to anchor and its tick
        // would only be pointing at a value that has no part in what is drawn.
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.Min, -5D);
            parameters.Add(p => p.Max, 5D);
            parameters.Add(p => p.IsOriginFromZero, true);
            parameters.Add(p => p.Origin, 2D);
        });

        Assert.AreEqual(0, com.FindAll(".bit-sld-ztk").Count);
    }

    [TestMethod]
    public void BitSliderShouldLayTheFillAlongTheBlockAxisWhenVertical()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsVertical, true);
            parameters.Add(p => p.Max, 10D);
            parameters.Add(p => p.Value, 3D);
        });

        var style = com.Find(".bit-sld-fil").GetAttribute("style");

        // A vertical slider grows from its bottom edge upwards, so "more" is up.
        StringAssert.Contains(style, "bottom:0%");
        StringAssert.Contains(style, "height:30%");
    }

    #endregion

    #region Marks

    [TestMethod]
    public void BitSliderShouldNotRenderMarksByDefault()
    {
        var com = RenderComponent<BitSlider>();

        Assert.AreEqual(0, com.FindAll(".bit-sld-mrk").Count);
        Assert.AreEqual(0, com.FindAll(".bit-sld-mks").Count);
    }

    [TestMethod]
    public void BitSliderShouldGenerateAMarkPerStep()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Min, 0D);
            parameters.Add(p => p.Max, 10D);
            parameters.Add(p => p.Step, 1D);
            parameters.Add(p => p.ShowMarks, true);
        });

        Assert.AreEqual(11, com.FindAll(".bit-sld-mrk").Count);
    }

    [TestMethod]
    public void BitSliderShouldGenerateMarksAtTheMarkStep()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Min, 0D);
            parameters.Add(p => p.Max, 1000D);
            parameters.Add(p => p.Step, 10D);
            parameters.Add(p => p.MarkStep, 200D);
            parameters.Add(p => p.ShowMarks, true);
        });

        Assert.AreEqual(6, com.FindAll(".bit-sld-mrk").Count);
    }

    [TestMethod]
    public void BitSliderShouldCapTheNumberOfGeneratedMarks()
    {
        // A mark per step of a very fine slider would be thousands of elements nobody can tell apart.
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Min, 0D);
            parameters.Add(p => p.Max, 10000D);
            parameters.Add(p => p.Step, 1D);
            parameters.Add(p => p.ShowMarks, true);
        });

        // The cap widens the step until the series fits, which is the cap itself plus the mark on the far end.
        Assert.AreEqual(201, com.FindAll(".bit-sld-mrk").Count);
    }

    [TestMethod]
    public void BitSliderShouldCapTheGeneratedMarksOfAMarkStepFarFinerThanTheScale()
    {
        // Counted in an int, a step this small against the range overflows on the way in, and an overflowed
        // count produces a single mark rather than the capped series.
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Min, 0D);
            parameters.Add(p => p.Max, 10D);
            parameters.Add(p => p.MarkStep, 0.0000000001D);
            parameters.Add(p => p.ShowMarks, true);
        });

        var marks = com.FindAll(".bit-sld-mrk");

        Assert.AreEqual(201, marks.Count);
    }

    [TestMethod]
    public void BitSliderShouldLabelTheGeneratedMarks()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Max, 4D);
            parameters.Add(p => p.ShowMarks, true);
            parameters.Add(p => p.ShowMarkLabels, true);
        });

        var labels = com.FindAll(".bit-sld-mlb");

        Assert.AreEqual(5, labels.Count);
        Assert.AreEqual("0", labels[0].TextContent.Trim());
        Assert.AreEqual("4", labels[^1].TextContent.Trim());
    }

    [TestMethod]
    public void BitSliderShouldFormatTheMarkLabelsLikeTheValue()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Max, 4D);
            parameters.Add(p => p.ShowMarks, true);
            parameters.Add(p => p.ShowMarkLabels, true);
            parameters.Add(p => p.ValueFormat, "0.0");
        });

        Assert.AreEqual("0.0", com.FindAll(".bit-sld-mlb")[0].TextContent.Trim());
    }

    [TestMethod]
    public void BitSliderShouldRespectExplicitMarks()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Max, 4D);
            parameters.Add(p => p.ShowMarks, true);
            parameters.Add(p => p.Marks,
            [
                new BitSliderMark(0, "Low"),
                new BitSliderMark(2, "Medium"),
                new BitSliderMark(4, "High")
            ]);
        });

        var marks = com.FindAll(".bit-sld-mrk");
        var labels = com.FindAll(".bit-sld-mlb");

        // Explicit marks take precedence over the generated ones.
        Assert.AreEqual(3, marks.Count);
        Assert.AreEqual(3, labels.Count);
        Assert.AreEqual("Low", labels[0].TextContent.Trim());
        Assert.AreEqual("High", labels[^1].TextContent.Trim());
        StringAssert.Contains(marks[1].GetAttribute("style"), "inset-inline-start:50%");
    }

    [TestMethod]
    public void BitSliderShouldSkipTheMarksOutsideTheScale()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Min, 0D);
            parameters.Add(p => p.Max, 10D);
            parameters.Add(p => p.Marks,
            [
                new BitSliderMark(-4),
                new BitSliderMark(5),
                new BitSliderMark(40)
            ]);
        });

        Assert.AreEqual(1, com.FindAll(".bit-sld-mrk").Count);
    }

    [TestMethod]
    public void BitSliderShouldMarkTheReachedMarksAsActive()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Max, 10D);
            parameters.Add(p => p.Value, 4D);
            parameters.Add(p => p.ShowMarks, true);
        });

        var marks = com.FindAll(".bit-sld-mrk");

        // Everything up to and including the value is inside the fill.
        Assert.AreEqual(5, marks.Count(m => m.ClassList.Contains("bit-sld-mrk-act")));
    }

    [TestMethod]
    public void BitSliderShouldCarryTheClassAndStyleOfAMark()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Max, 10D);
            parameters.Add(p => p.Marks, [new BitSliderMark(5, "Half") { Class = "custom-mark", Style = "color:red" }]);
        });

        Assert.IsTrue(com.Find(".bit-sld-mrk").ClassList.Contains("custom-mark"));
        StringAssert.Contains(com.Find(".bit-sld-mrk").GetAttribute("style"), "color:red");
        Assert.IsTrue(com.Find(".bit-sld-mlb").ClassList.Contains("custom-mark"));
    }

    [TestMethod]
    public void BitSliderShouldInvertWhichMarksCountAsReachedOnAnInvertedRange()
    {
        // An inverted range fills the two outer segments rather than the span between the thumbs, so what
        // counts as reached is inverted with it.
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.Inverted, true);
            parameters.Add(p => p.Max, 10D);
            parameters.Add(p => p.LowerValue, 4D);
            parameters.Add(p => p.UpperValue, 6D);
            parameters.Add(p => p.ShowMarks, true);
        });

        var marks = com.FindAll(".bit-sld-mrk");

        // 0..3 and 7..10 are outside the thumbs, so eight of the eleven marks are on the filled segments.
        Assert.AreEqual(8, marks.Count(m => m.ClassList.Contains("bit-sld-mrk-act")));
    }

    #endregion

    #region RestrictToMarks

    [TestMethod]
    public void BitSliderShouldSnapToTheNearestMark()
    {
        double? bound = null;
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Max, 100D);
            parameters.Add(p => p.RestrictToMarks, true);
            parameters.Add(p => p.Marks, [new BitSliderMark(0), new BitSliderMark(25), new BitSliderMark(80)]);
            parameters.Add(p => p.Value, 0D);
            parameters.Add(p => p.ValueChanged, (double v) => bound = v);
        });

        com.Find(".bit-sld-inp").Input("31");

        Assert.AreEqual(25, bound);
    }

    [TestMethod]
    public void BitSliderShouldCarryAKeyboardStepOnToTheNextMark()
    {
        // A single Step is shorter than the gap between two marks, so rounding to the nearest one would
        // snap the value straight back and the arrow keys would do nothing at all.
        double? bound = null;
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Max, 100D);
            parameters.Add(p => p.Step, 1D);
            parameters.Add(p => p.RestrictToMarks, true);
            parameters.Add(p => p.Marks, [new BitSliderMark(0), new BitSliderMark(25), new BitSliderMark(80)]);
            parameters.Add(p => p.Value, 25D);
            parameters.Add(p => p.ValueChanged, (double v) => bound = v);
        });

        var input = com.Find(".bit-sld-inp");
        input.KeyDown("ArrowRight");
        input.Input("26");

        Assert.AreEqual(80, bound);
    }

    [TestMethod]
    public void BitSliderShouldCarryAKeyboardStepBackToThePreviousMark()
    {
        double? bound = null;
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Max, 100D);
            parameters.Add(p => p.RestrictToMarks, true);
            parameters.Add(p => p.Marks, [new BitSliderMark(0), new BitSliderMark(25), new BitSliderMark(80)]);
            parameters.Add(p => p.Value, 25D);
            parameters.Add(p => p.ValueChanged, (double v) => bound = v);
        });

        var input = com.Find(".bit-sld-inp");
        input.KeyDown("ArrowLeft");
        input.Input("24");

        Assert.AreEqual(0, bound);
    }

    [TestMethod]
    public void BitSliderShouldNotCarryAPointerMoveOnToTheNextMark()
    {
        // A pointer lands where it is pointing. Carrying its move on the way a key's is carried would send
        // the thumb a whole mark further than the pointer ever went - and, since the next move would then
        // round back the other way, would turn a drag into a thumb that flicks between two marks.
        var changed = 0;
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Max, 100D);
            parameters.Add(p => p.RestrictToMarks, true);
            parameters.Add(p => p.Marks, [new BitSliderMark(0), new BitSliderMark(25), new BitSliderMark(80)]);
            parameters.Add(p => p.DefaultValue, 25D);
            parameters.Add(p => p.OnChange, (double _) => changed++);
        });

        com.Find(".bit-sld-inp").Input("26");

        Assert.AreEqual(25, com.Instance.Value);
        Assert.AreEqual(0, changed);
    }

    [TestMethod]
    public void BitSliderShouldStillSnapAPointerMoveToTheNearestMark()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Max, 100D);
            parameters.Add(p => p.RestrictToMarks, true);
            parameters.Add(p => p.Marks, [new BitSliderMark(0), new BitSliderMark(25), new BitSliderMark(80)]);
            parameters.Add(p => p.DefaultValue, 25D);
        });

        // Past the halfway point between the two marks, so the nearest one is a different one.
        com.Find(".bit-sld-inp").Input("60");

        Assert.AreEqual(80, com.Instance.Value);
    }

    [TestMethod]
    public void BitSliderShouldNotTreatANonMovingKeyAsAKeyboardChange()
    {
        // A key that never moves a range input - tabbing out of the slider - must not leave the drag that
        // follows it looking like a keystroke.
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Max, 100D);
            parameters.Add(p => p.RestrictToMarks, true);
            parameters.Add(p => p.Marks, [new BitSliderMark(0), new BitSliderMark(25), new BitSliderMark(80)]);
            parameters.Add(p => p.DefaultValue, 25D);
        });

        var input = com.Find(".bit-sld-inp");
        input.KeyDown("Tab");
        input.Input("26");

        Assert.AreEqual(25, com.Instance.Value);
    }

    [TestMethod]
    public void BitSliderShouldCarryAKeyboardStepOnToTheNextMarkOnARange()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.Max, 100D);
            parameters.Add(p => p.RestrictToMarks, true);
            parameters.Add(p => p.Marks, [new BitSliderMark(0), new BitSliderMark(25), new BitSliderMark(80)]);
            parameters.Add(p => p.DefaultLowerValue, 0D);
            parameters.Add(p => p.DefaultUpperValue, 25D);
        });

        var upper = com.Find(".bit-sld-inp-upr");
        upper.KeyDown("ArrowRight");
        upper.Input("26");

        Assert.AreEqual(0, com.Instance.LowerValue);
        Assert.AreEqual(80, com.Instance.UpperValue);
    }

    [TestMethod]
    public void BitSliderShouldStayOnTheEndMarkWhenThereIsNoNextOne()
    {
        var changed = 0;
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Max, 100D);
            parameters.Add(p => p.RestrictToMarks, true);
            parameters.Add(p => p.Marks, [new BitSliderMark(0), new BitSliderMark(25), new BitSliderMark(80)]);
            parameters.Add(p => p.DefaultValue, 80D);
            parameters.Add(p => p.OnChange, (double _) => changed++);
        });

        com.Find(".bit-sld-inp").Input("81");

        Assert.AreEqual(80, com.Instance.Value);
        Assert.AreEqual(0, changed);
    }

    [TestMethod]
    public void BitSliderShouldSnapBothEndsOfARangeToTheMarks()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.Max, 100D);
            parameters.Add(p => p.RestrictToMarks, true);
            parameters.Add(p => p.Marks, [new BitSliderMark(0), new BitSliderMark(25), new BitSliderMark(80)]);
            parameters.Add(p => p.DefaultLowerValue, 0D);
            parameters.Add(p => p.DefaultUpperValue, 80D);
        });

        com.Find(".bit-sld-inp-lwr").Input("22");

        Assert.AreEqual(25, com.Instance.LowerValue);
        Assert.AreEqual(80, com.Instance.UpperValue);
    }

    [TestMethod]
    public void BitSliderShouldHoldASnappedThumbAgainstTheDistanceItHasToKeep()
    {
        // The browser holds every move inside the bounds the constraints gave the input, but the snap that
        // follows the move is a distance those bounds never saw. Left alone it would take the thumb through
        // the one beside it and drag that one along with it - a push on a slider nobody asked to be pushable.
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.Max, 100D);
            parameters.Add(p => p.MinRange, 30D);
            parameters.Add(p => p.RestrictToMarks, true);
            parameters.Add(p => p.Marks, [new BitSliderMark(0), new BitSliderMark(20), new BitSliderMark(60), new BitSliderMark(100)]);
            parameters.Add(p => p.DefaultLowerValue, 0D);
            parameters.Add(p => p.DefaultUpperValue, 60D);
        });

        // The mark at 60 is the nearest one, and the furthest the lower end may go is 30 - so it lands on the
        // outermost mark that does fit, and the upper end stays exactly where it was.
        com.Find(".bit-sld-inp-lwr").Input("55");

        Assert.AreEqual(20, com.Instance.LowerValue);
        Assert.AreEqual(60, com.Instance.UpperValue);
    }

    [TestMethod]
    public void BitSliderShouldNotCarryAKeyPastTheThumbBesideIt()
    {
        // Carrying a key on to the next mark is the one move the bounds of the input cannot hold, since the
        // browser is already done with it by then - and an arrow key that pushes the other thumb three marks
        // up the scale is not the single step it was pressed for.
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.Max, 100D);
            parameters.Add(p => p.MinRange, 30D);
            parameters.Add(p => p.RestrictToMarks, true);
            parameters.Add(p => p.Marks, [new BitSliderMark(0), new BitSliderMark(20), new BitSliderMark(60), new BitSliderMark(100)]);
            parameters.Add(p => p.DefaultLowerValue, 20D);
            parameters.Add(p => p.DefaultUpperValue, 60D);
        });

        var lower = com.Find(".bit-sld-inp-lwr");
        lower.KeyDown("ArrowRight");
        lower.Input("21");

        // The next mark up is 60, which the 30 the pair has to keep apart puts out of reach, so nothing moves.
        Assert.AreEqual(20, com.Instance.LowerValue);
        Assert.AreEqual(60, com.Instance.UpperValue);
    }

    [TestMethod]
    public void BitSliderShouldStillSnapWithinTheBoundsOfAConstrainedThumb()
    {
        // Holding the snap against the bounds must not stop it landing on the marks that do fit.
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.Max, 100D);
            parameters.Add(p => p.NoSwap, true);
            parameters.Add(p => p.RestrictToMarks, true);
            parameters.Add(p => p.Marks, [new BitSliderMark(0), new BitSliderMark(20), new BitSliderMark(60), new BitSliderMark(100)]);
            parameters.Add(p => p.DefaultLowerValue, 0D);
            parameters.Add(p => p.DefaultUpperValue, 60D);
        });

        com.Find(".bit-sld-inp-lwr").Input("25");

        Assert.AreEqual(20, com.Instance.LowerValue);
        Assert.AreEqual(60, com.Instance.UpperValue);
    }

    [TestMethod]
    public void BitSliderShouldStillPushASnappedThumbWhenPushable()
    {
        // Pushing is the one case where a snapped thumb is meant to take the other one with it.
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.Pushable, true);
            parameters.Add(p => p.Max, 100D);
            parameters.Add(p => p.MinRange, 30D);
            parameters.Add(p => p.RestrictToMarks, true);
            parameters.Add(p => p.Marks, [new BitSliderMark(0), new BitSliderMark(20), new BitSliderMark(60), new BitSliderMark(100)]);
            parameters.Add(p => p.DefaultLowerValue, 0D);
            parameters.Add(p => p.DefaultUpperValue, 60D);
        });

        com.Find(".bit-sld-inp-lwr").Input("55");

        Assert.AreEqual(60, com.Instance.LowerValue);
        Assert.AreEqual(90, com.Instance.UpperValue);
    }

    [TestMethod]
    public void BitSliderShouldPutTheCrossedPairBackOnTheInputs()
    {
        // What comes out of a move is what the inputs have to carry, keeping whichever of them holds the
        // upper end - otherwise an input would hold a value the thumb beside it is not drawn at, and a
        // re-render of an unbound slider never passes through the correction that would fix it.
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.Max, 100D);
            parameters.Add(p => p.RestrictToMarks, true);
            parameters.Add(p => p.Marks, [new BitSliderMark(0), new BitSliderMark(20), new BitSliderMark(60), new BitSliderMark(100)]);
            parameters.Add(p => p.DefaultLowerValue, 20D);
            parameters.Add(p => p.DefaultUpperValue, 60D);
        });

        // Snapped onto the mark at 100, which takes the lower input past the upper one and makes it the
        // upper end of the range.
        com.Find(".bit-sld-inp-lwr").Input("95");

        Assert.AreEqual(60, com.Instance.LowerValue);
        Assert.AreEqual(100, com.Instance.UpperValue);

        Assert.AreEqual("100", com.Find(".bit-sld-inp-lwr").GetAttribute("value"));
        Assert.AreEqual("60", com.Find(".bit-sld-inp-upr").GetAttribute("value"));
    }

    [TestMethod]
    public void BitSliderShouldEnumerateTheMarksOncePerParameterSet()
    {
        // Snapping looks the marks up on every step of a drag, and the render that follows reads them again,
        // so a series of up to two hundred of them would otherwise be rebuilt several times per drag step -
        // and an IEnumerable handed to Marks would be walked once per reader rather than once per parameter set.
        var counting = new CountingMarks([new BitSliderMark(0), new BitSliderMark(25), new BitSliderMark(80)]);

        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Max, 100D);
            parameters.Add(p => p.RestrictToMarks, true);
            parameters.Add(p => p.Marks, counting);
            parameters.Add(p => p.DefaultValue, 0D);
        });

        Assert.AreEqual(1, counting.Enumerations);

        com.Find(".bit-sld-inp").Input("31");

        Assert.AreEqual(25, com.Instance.Value);
        Assert.AreEqual(1, counting.Enumerations);

        // A new parameter set is what the marks are drawn from, so that - and only that - rebuilds them.
        com.Render(parameters => parameters.Add(p => p.Max, 200D));

        Assert.AreEqual(2, counting.Enumerations);
    }

    private sealed class CountingMarks(IEnumerable<BitSliderMark> marks) : IEnumerable<BitSliderMark>
    {
        public int Enumerations { get; private set; }

        public IEnumerator<BitSliderMark> GetEnumerator()
        {
            Enumerations++;

            return marks.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [TestMethod]
    public void BitSliderShouldIgnoreRestrictToMarksWithoutMarks()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.RestrictToMarks, true);
            parameters.Add(p => p.DefaultValue, 2D);
        });

        com.Find(".bit-sld-inp").Input("7");

        Assert.AreEqual(7, com.Instance.Value);
    }

    #endregion

    #region LargeStep

    [TestMethod,
        DataRow("PageUp"),
        DataRow("PageDown")
    ]
    public void BitSliderShouldTakeAPageKeyTheDistanceOfTheLargeStep(string key)
    {
        // Every browser measures a Page key its own way, so a scale that wants a jump of a stated size has to
        // name it - and the move the browser made is then re-measured against it.
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Max, 1000D);
            parameters.Add(p => p.Step, 10D);
            parameters.Add(p => p.LargeStep, 100D);
            parameters.Add(p => p.DefaultValue, 400D);
        });

        var up = key == "PageUp";
        var input = com.Find(".bit-sld-inp");
        input.KeyDown(new KeyboardEventArgs { Key = key });
        input.Input(up ? "410" : "390");

        Assert.AreEqual(up ? 500 : 300, com.Instance.Value);
    }

    [TestMethod,
        DataRow("ArrowRight", true, 500D),
        DataRow("ArrowUp", true, 500D),
        DataRow("ArrowLeft", false, 300D),
        DataRow("ArrowDown", false, 300D)
    ]
    public void BitSliderShouldTakeAShiftedArrowTheDistanceOfTheLargeStep(string key, bool up, double expected)
    {
        // A browser makes nothing at all of a Shift on a range input, so the arrow moves by a single step and
        // the jump is measured here instead.
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Max, 1000D);
            parameters.Add(p => p.Step, 10D);
            parameters.Add(p => p.LargeStep, 100D);
            parameters.Add(p => p.DefaultValue, 400D);
        });

        var input = com.Find(".bit-sld-inp");
        input.KeyDown(new KeyboardEventArgs { Key = key, ShiftKey = true });
        input.Input(up ? "410" : "390");

        Assert.AreEqual(expected, com.Instance.Value);
    }

    [TestMethod]
    public void BitSliderShouldReadTheDirectionOfALargeStepOffTheMoveRatherThanTheKey()
    {
        // A horizontal RTL slider maps the arrows onto the scale the other way round, so the key that was
        // pressed says nothing about which way the value went - only the move the browser already made does.
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Rtl);
            parameters.Add(p => p.Max, 1000D);
            parameters.Add(p => p.Step, 10D);
            parameters.Add(p => p.LargeStep, 100D);
            parameters.Add(p => p.DefaultValue, 400D);
        });

        var input = com.Find(".bit-sld-inp");
        input.KeyDown(new KeyboardEventArgs { Key = "ArrowLeft", ShiftKey = true });
        input.Input("410");

        Assert.AreEqual(500, com.Instance.Value);
    }

    [TestMethod]
    public void BitSliderShouldLeaveAPlainArrowAtASingleStep()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Max, 1000D);
            parameters.Add(p => p.Step, 10D);
            parameters.Add(p => p.LargeStep, 100D);
            parameters.Add(p => p.DefaultValue, 400D);
        });

        var input = com.Find(".bit-sld-inp");
        input.KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        input.Input("410");

        Assert.AreEqual(410, com.Instance.Value);
    }

    [TestMethod,
        DataRow("Home", 0D),
        DataRow("End", 1000D)
    ]
    public void BitSliderShouldLeaveHomeAndEndAtTheEndsOfTheScale(string key, double expected)
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Max, 1000D);
            parameters.Add(p => p.Step, 10D);
            parameters.Add(p => p.LargeStep, 100D);
            parameters.Add(p => p.DefaultValue, 400D);
        });

        var input = com.Find(".bit-sld-inp");
        input.KeyDown(new KeyboardEventArgs { Key = key });
        input.Input(expected.ToString(CultureInfo.InvariantCulture));

        Assert.AreEqual(expected, com.Instance.Value);
    }

    [TestMethod]
    public void BitSliderShouldLeaveThePageKeysToTheBrowserWithoutALargeStep()
    {
        // Taking the Page keys over only to hand them the very distance the browser already chose would be no
        // change at all, so nothing is intercepted until there is a LargeStep to measure against.
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Max, 1000D);
            parameters.Add(p => p.Step, 10D);
            parameters.Add(p => p.DefaultValue, 400D);
        });

        var input = com.Find(".bit-sld-inp");
        input.KeyDown(new KeyboardEventArgs { Key = "PageUp" });
        input.Input("500");

        Assert.AreEqual(500, com.Instance.Value);
    }

    [TestMethod,
        DataRow(0D),
        DataRow(-40D)
    ]
    public void BitSliderShouldIgnoreALargeStepThatIsNotADistance(double largeStep)
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Max, 1000D);
            parameters.Add(p => p.Step, 10D);
            parameters.Add(p => p.LargeStep, largeStep);
            parameters.Add(p => p.DefaultValue, 400D);
        });

        var input = com.Find(".bit-sld-inp");
        input.KeyDown(new KeyboardEventArgs { Key = "PageUp" });
        input.Input("500");

        Assert.AreEqual(500, com.Instance.Value);
    }

    [TestMethod]
    public void BitSliderShouldHoldALargeStepInsideTheScale()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Max, 1000D);
            parameters.Add(p => p.Step, 10D);
            parameters.Add(p => p.LargeStep, 100D);
            parameters.Add(p => p.DefaultValue, 950D);
        });

        var input = com.Find(".bit-sld-inp");
        input.KeyDown(new KeyboardEventArgs { Key = "PageUp" });
        input.Input("960");

        Assert.AreEqual(1000, com.Instance.Value);
    }

    [TestMethod]
    public void BitSliderShouldLandALargeStepOnAMark()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Max, 100D);
            parameters.Add(p => p.LargeStep, 30D);
            parameters.Add(p => p.RestrictToMarks, true);
            parameters.Add(p => p.Marks, [new BitSliderMark(0), new BitSliderMark(25), new BitSliderMark(80)]);
            parameters.Add(p => p.DefaultValue, 0D);
        });

        var input = com.Find(".bit-sld-inp");
        input.KeyDown(new KeyboardEventArgs { Key = "PageUp" });
        input.Input("1");

        // The jump lands on 30, which belongs to the mark at 25.
        Assert.AreEqual(25, com.Instance.Value);
    }

    [TestMethod]
    public void BitSliderShouldTakeALargeStepOnEitherEndOfARange()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.Max, 1000D);
            parameters.Add(p => p.Step, 10D);
            parameters.Add(p => p.LargeStep, 250D);
            parameters.Add(p => p.DefaultLowerValue, 200D);
            parameters.Add(p => p.DefaultUpperValue, 800D);
        });

        var upper = com.Find(".bit-sld-inp-upr");
        upper.KeyDown(new KeyboardEventArgs { Key = "PageDown" });
        upper.Input("790");

        Assert.AreEqual(200, com.Instance.LowerValue);
        Assert.AreEqual(550, com.Instance.UpperValue);
    }

    [TestMethod]
    public void BitSliderShouldHoldALargeStepAgainstTheOtherThumb()
    {
        // A jump measured here is not one the browser held inside the bounds of the input, so the constraints
        // that were pushed into those bounds have to be applied to it directly.
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.NoSwap, true);
            parameters.Add(p => p.Max, 1000D);
            parameters.Add(p => p.Step, 10D);
            parameters.Add(p => p.LargeStep, 250D);
            parameters.Add(p => p.DefaultLowerValue, 200D);
            parameters.Add(p => p.DefaultUpperValue, 300D);
        });

        var lower = com.Find(".bit-sld-inp-lwr");
        lower.KeyDown(new KeyboardEventArgs { Key = "PageUp" });
        lower.Input("210");

        Assert.AreEqual(300, com.Instance.LowerValue);
        Assert.AreEqual(300, com.Instance.UpperValue);
    }

    [TestMethod]
    public void BitSliderShouldPushWithALargeStep()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.Pushable, true);
            parameters.Add(p => p.MinRange, 20D);
            parameters.Add(p => p.Max, 100D);
            parameters.Add(p => p.LargeStep, 30D);
            parameters.Add(p => p.DefaultLowerValue, 20D);
            parameters.Add(p => p.DefaultUpperValue, 40D);
        });

        var lower = com.Find(".bit-sld-inp-lwr");
        lower.KeyDown(new KeyboardEventArgs { Key = "PageUp" });
        lower.Input("21");

        Assert.AreEqual(50, com.Instance.LowerValue);
        Assert.AreEqual(70, com.Instance.UpperValue);
    }

    [TestMethod]
    public void BitSliderShouldNotCarryADragOnAfterAKeyThatMovedNothing()
    {
        // A key that changes nothing - an ArrowRight already at the end of the scale - produces no input event
        // to clear the flag on, and the drag that came next would then be carried on to the following mark as
        // if it were a keystroke.
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Max, 100D);
            parameters.Add(p => p.RestrictToMarks, true);
            parameters.Add(p => p.Marks, [new BitSliderMark(0), new BitSliderMark(25), new BitSliderMark(80)]);
            parameters.Add(p => p.DefaultValue, 25D);
        });

        var input = com.Find(".bit-sld-inp");
        input.KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });

        input.PointerDown();
        input.Input("26");

        // A pointer lands on the mark nearest to where it actually is, which is the one it started on.
        Assert.AreEqual(25, com.Instance.Value);
    }

    [TestMethod]
    public void BitSliderShouldNotKeepAKeystrokeMadeWhileItWasNotInteractive()
    {
        // The flag is read and cleared before the change is refused as well as before it is taken, so a slider
        // that becomes interactive again does not begin with a keystroke somebody made while it was not.
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.Max, 100D);
            parameters.Add(p => p.RestrictToMarks, true);
            parameters.Add(p => p.Marks, [new BitSliderMark(0), new BitSliderMark(25), new BitSliderMark(80)]);
            parameters.Add(p => p.DefaultValue, 25D);
        });

        var input = com.Find(".bit-sld-inp");
        input.KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        input.Input("26");

        Assert.AreEqual(25, com.Instance.Value);

        com.Render(parameters => parameters.Add(p => p.ReadOnly, false));

        com.Find(".bit-sld-inp").Input("26");

        // A pointer lands on the mark nearest to where it actually is, which is the one it started on.
        Assert.AreEqual(25, com.Instance.Value);
    }

    [TestMethod]
    public void BitSliderShouldNotTakeALargeStepAfterAPageKeyThatMovedNothing()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Max, 1000D);
            parameters.Add(p => p.Step, 10D);
            parameters.Add(p => p.LargeStep, 100D);
            parameters.Add(p => p.DefaultValue, 1000D);
        });

        var input = com.Find(".bit-sld-inp");
        input.KeyDown(new KeyboardEventArgs { Key = "PageUp" });

        input.PointerDown();
        input.Input("400");

        Assert.AreEqual(400, com.Instance.Value);
    }

    #endregion

    #region Precision

    [TestMethod]
    public void BitSliderShouldNotCarryTheNoiseOfAFractionalLargeStep()
    {
        // Adding a step to a value is where binary floating point starts showing: 0.1 + 0.2 reaches
        // 0.30000000000000004, and the label, the announced text and the posted value would all carry it.
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Max, 1D);
            parameters.Add(p => p.Step, 0.1D);
            parameters.Add(p => p.LargeStep, 0.2D);
            parameters.Add(p => p.DefaultValue, 0.1D);
        });

        var input = com.Find(".bit-sld-inp");
        input.KeyDown(new KeyboardEventArgs { Key = "PageUp" });
        input.Input("0.2");

        Assert.AreEqual(0.3, com.Instance.Value);
        Assert.AreEqual("0.3", com.Find(".bit-sld-inp").GetAttribute("value"));
    }

    [TestMethod]
    public void BitSliderShouldNotCarryTheNoiseOfAFractionalMinRange()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.Max, 1D);
            parameters.Add(p => p.Step, 0.1D);
            parameters.Add(p => p.MinRange, 0.2D);
            parameters.Add(p => p.DefaultLowerValue, 0.1D);
            parameters.Add(p => p.DefaultUpperValue, 0.15D);
        });

        // Widened to the distance the pair has to keep, which is 0.1 + 0.2 - and that is 0.30000000000000004
        // until it is rounded back to the scale the slider is actually counting in.
        Assert.AreEqual(0.3, com.Instance.UpperValue);
    }

    [TestMethod]
    public void BitSliderShouldNotHandTheInputsBoundsWithNoiseInThem()
    {
        // The bounds travel to the browser as the min and the max of a real range input, which steps from its
        // own min - so noise in it would put the whole grid a fraction off the values the slider counts in.
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.Max, 1D);
            parameters.Add(p => p.Step, 0.1D);
            parameters.Add(p => p.MinRange, 0.2D);
            parameters.Add(p => p.DefaultLowerValue, 0.1D);
            parameters.Add(p => p.DefaultUpperValue, 0.5D);
        });

        Assert.AreEqual("0.3", com.Find(".bit-sld-inp-upr").GetAttribute("min"));
        Assert.AreEqual("0.3", com.Find(".bit-sld-inp-lwr").GetAttribute("max"));
    }

    [TestMethod]
    public void BitSliderShouldNotCarryTheNoiseOfAFractionalBandWidth()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.DraggableTrack, true);
            parameters.Add(p => p.Max, 1D);
            parameters.Add(p => p.Step, 0.1D);
            parameters.Add(p => p.DefaultLowerValue, 0.1D);
            parameters.Add(p => p.DefaultUpperValue, 0.3D);
        });

        com.Find(".bit-sld-inp-bar").Input("0.4");

        Assert.AreEqual(0.4, com.Instance.LowerValue);
        Assert.AreEqual(0.6, com.Instance.UpperValue);
    }

    #endregion

    #region DraggableTrack

    [TestMethod,
        DataRow(false, false),
        DataRow(true, false),
        DataRow(false, true)
    ]
    public void BitSliderShouldRenderTheDraggableBandOnlyWhenARangeAsksForIt(bool ranged, bool draggable)
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, ranged);
            parameters.Add(p => p.DraggableTrack, draggable);
            parameters.Add(p => p.DefaultLowerValue, 2D);
            parameters.Add(p => p.DefaultUpperValue, 6D);
        });

        Assert.AreEqual(0, com.FindAll(".bit-sld-inp-bar").Count);
    }

    [TestMethod]
    public void BitSliderShouldLayTheDraggableBandOverTheFillOfTheRange()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.DraggableTrack, true);
            parameters.Add(p => p.Max, 100D);
            parameters.Add(p => p.DefaultLowerValue, 20D);
            parameters.Add(p => p.DefaultUpperValue, 60D);
        });

        var bar = com.Find(".bit-sld-inp-bar");

        // The band spans the two ends of the range, which is what its thumb is made as wide as.
        StringAssert.Contains(bar.GetAttribute("style"), "--bit-sld-bar-start:0.2");
        StringAssert.Contains(bar.GetAttribute("style"), "--bit-sld-bar-end:0.6");

        // Its value is the lower end, and the room it has left is the room the band has left on the scale.
        Assert.AreEqual("0", bar.GetAttribute("min"));
        Assert.AreEqual("60", bar.GetAttribute("max"));
        Assert.AreEqual("20", bar.GetAttribute("value"));
    }

    [TestMethod]
    public void BitSliderShouldKeepTheDraggableBandOutOfTheTabOrderAndOutOfTheAccessibilityTree()
    {
        // The band selects nothing the two thumbs do not already carry, so a third slider announcing the same
        // lower value again would be noise rather than a way through.
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.DraggableTrack, true);
            parameters.Add(p => p.DefaultLowerValue, 2D);
            parameters.Add(p => p.DefaultUpperValue, 6D);
        });

        var bar = com.Find(".bit-sld-inp-bar");

        Assert.AreEqual("-1", bar.GetAttribute("tabindex"));
        Assert.AreEqual("true", bar.GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public void BitSliderShouldNotRenderTheDraggableBandOfACollapsedRange()
    {
        // A band of no width has nothing to be pressed on, and one that could be dragged while the thumbs it
        // covers could not would be a range that can never be opened up again.
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.DraggableTrack, true);
            parameters.Add(p => p.DefaultLowerValue, 4D);
            parameters.Add(p => p.DefaultUpperValue, 4D);
        });

        Assert.AreEqual(0, com.FindAll(".bit-sld-inp-bar").Count);
    }

    [TestMethod,
        DataRow(false, false),
        DataRow(true, true)
    ]
    public void BitSliderShouldNotRenderTheDraggableBandOfAPinnedRange(bool enabled, bool readOnly)
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.DraggableTrack, true);
            parameters.Add(p => p.IsEnabled, enabled);
            parameters.Add(p => p.ReadOnly, readOnly);
            parameters.Add(p => p.DefaultLowerValue, 2D);
            parameters.Add(p => p.DefaultUpperValue, 6D);
        });

        Assert.AreEqual(0, com.FindAll(".bit-sld-inp-bar").Count);
    }

    [TestMethod]
    public void BitSliderShouldMoveBothEndsWhenTheBandIsDragged()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.DraggableTrack, true);
            parameters.Add(p => p.Max, 100D);
            parameters.Add(p => p.DefaultLowerValue, 20D);
            parameters.Add(p => p.DefaultUpperValue, 60D);
        });

        com.Find(".bit-sld-inp-bar").Input("50");

        Assert.AreEqual(50, com.Instance.LowerValue);
        Assert.AreEqual(90, com.Instance.UpperValue);
    }

    [TestMethod]
    public void BitSliderShouldStopTheDraggedBandAtTheEndOfTheScale()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.DraggableTrack, true);
            parameters.Add(p => p.Max, 100D);
            parameters.Add(p => p.DefaultLowerValue, 20D);
            parameters.Add(p => p.DefaultUpperValue, 60D);
        });

        com.Find(".bit-sld-inp-bar").Input("95");

        // The band comes to rest rather than being squashed against the end.
        Assert.AreEqual(60, com.Instance.LowerValue);
        Assert.AreEqual(100, com.Instance.UpperValue);
    }

    [TestMethod]
    public void BitSliderShouldKeepEveryRangeConstraintWhileTheBandTravels()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.DraggableTrack, true);
            parameters.Add(p => p.MinRange, 20D);
            parameters.Add(p => p.MaxRange, 20D);
            parameters.Add(p => p.Max, 100D);
            parameters.Add(p => p.DefaultLowerValue, 30D);
            parameters.Add(p => p.DefaultUpperValue, 50D);
        });

        com.Find(".bit-sld-inp-bar").Input("70");

        Assert.AreEqual(70, com.Instance.LowerValue);
        Assert.AreEqual(90, com.Instance.UpperValue);
    }

    [TestMethod]
    public void BitSliderShouldSnapTheDraggedBandToTheMarks()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.DraggableTrack, true);
            parameters.Add(p => p.RestrictToMarks, true);
            parameters.Add(p => p.Max, 100D);
            parameters.Add(p => p.ShowMarks, true);
            parameters.Add(p => p.MarkStep, 20D);
            parameters.Add(p => p.DefaultLowerValue, 20D);
            parameters.Add(p => p.DefaultUpperValue, 40D);
        });

        com.Find(".bit-sld-inp-bar").Input("33");

        Assert.AreEqual(40, com.Instance.LowerValue);
        Assert.AreEqual(60, com.Instance.UpperValue);
    }

    [TestMethod]
    public void BitSliderShouldRestTheDraggedBandOnAMarkAtTheEndOfTheScale()
    {
        // Squashing the band against the end of the scale would leave its leading thumb on a value the marks
        // do not have, which is the one thing a mark-restricted slider promises never to happen.
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.DraggableTrack, true);
            parameters.Add(p => p.RestrictToMarks, true);
            parameters.Add(p => p.Max, 100D);
            parameters.Add(p => p.Marks, [new BitSliderMark(0), new BitSliderMark(30), new BitSliderMark(70), new BitSliderMark(100)]);
            parameters.Add(p => p.DefaultLowerValue, 0D);
            parameters.Add(p => p.DefaultUpperValue, 20D);
        });

        // The band is 20 wide, so the furthest its leading end may go is 80 - and the outermost mark that
        // still leaves room for the width is 70.
        com.Find(".bit-sld-inp-bar").Input("95");

        Assert.AreEqual(70, com.Instance.LowerValue);
        Assert.AreEqual(90, com.Instance.UpperValue);
    }

    [TestMethod]
    public void BitSliderShouldReportADraggedBandThroughTheRangeCallbacks()
    {
        var stepCount = 0;
        BitSliderRangeValue? committed = null;
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.DraggableTrack, true);
            parameters.Add(p => p.Max, 100D);
            parameters.Add(p => p.DefaultLowerValue, 20D);
            parameters.Add(p => p.DefaultUpperValue, 60D);
            parameters.Add(p => p.OnRangeChange, (BitSliderRangeValue _) => stepCount++);
            parameters.Add(p => p.OnRangeChangeEnd, (BitSliderRangeValue v) => committed = v);
        });

        var bar = com.Find(".bit-sld-inp-bar");
        bar.Input("30");
        bar.Input("40");

        Assert.AreEqual(2, stepCount);
        Assert.IsNull(committed);

        bar.Change("40");

        Assert.IsNotNull(committed);
        Assert.AreEqual(40, committed.Lower);
        Assert.AreEqual(80, committed.Upper);
    }

    [TestMethod]
    public void BitSliderShouldNotifyBothEndsWhenTheBandIsDragged()
    {
        double? lower = null;
        double? upper = null;
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.DraggableTrack, true);
            parameters.Add(p => p.Max, 100D);
            parameters.Add(p => p.LowerValue, 20D);
            parameters.Add(p => p.UpperValue, 60D);
            parameters.Add(p => p.LowerValueChanged, (double v) => lower = v);
            parameters.Add(p => p.UpperValueChanged, (double v) => upper = v);
        });

        com.Find(".bit-sld-inp-bar").Input("30");

        Assert.AreEqual(30, lower);
        Assert.AreEqual(70, upper);
    }

    [TestMethod]
    public void BitSliderShouldRefuseABandDragOfAOneWayBoundRange()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.DraggableTrack, true);
            parameters.Add(p => p.Max, 100D);
            parameters.Add(p => p.LowerValue, 20D);
            parameters.Add(p => p.UpperValue, 60D);
        });

        com.Find(".bit-sld-inp-bar").Input("50");

        Assert.AreEqual(20, com.Instance.LowerValue);
        Assert.AreEqual(60, com.Instance.UpperValue);
    }

    [TestMethod]
    public void BitSliderShouldRespectTheTrackInputClassAndStyle()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.DraggableTrack, true);
            parameters.Add(p => p.DefaultLowerValue, 2D);
            parameters.Add(p => p.DefaultUpperValue, 6D);
            parameters.Add(p => p.Classes, new BitSliderClassStyles { TrackInput = "custom-bar" });
            parameters.Add(p => p.Styles, new BitSliderClassStyles { TrackInput = "opacity:0.5" });
        });

        var bar = com.Find(".bit-sld-inp-bar");

        Assert.IsTrue(bar.ClassList.Contains("custom-bar"));
        StringAssert.Contains(bar.GetAttribute("style"), "opacity:0.5");
    }

    #endregion

    #region Accessibility

    [TestMethod]
    public void BitSliderShouldAnnounceItsValueAndBounds()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Min, 2D);
            parameters.Add(p => p.Max, 8D);
            parameters.Add(p => p.Value, 5D);
        });

        var input = com.Find(".bit-sld-inp");

        Assert.AreEqual("2", input.GetAttribute("aria-valuemin"));
        Assert.AreEqual("8", input.GetAttribute("aria-valuemax"));
        Assert.AreEqual("5", input.GetAttribute("aria-valuenow"));
        Assert.AreEqual("5", input.GetAttribute("aria-valuetext"));
    }

    [TestMethod]
    public void BitSliderShouldRespectAriaValueText()
    {
        // The old implementation ignored this parameter entirely outside of ranged mode.
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Value, 2D);
            parameters.Add(p => p.AriaValueText, (double v) => $"Level {v}");
        });

        Assert.AreEqual("Level 2", com.Find(".bit-sld-inp").GetAttribute("aria-valuetext"));
    }

    [TestMethod]
    public void BitSliderShouldFallBackToTheDisplayTextForAriaValueText()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Value, 2D);
            parameters.Add(p => p.GetValueText, (double v) => $"Step {v}");
        });

        Assert.AreEqual("Step 2", com.Find(".bit-sld-inp").GetAttribute("aria-valuetext"));
    }

    [TestMethod]
    public void BitSliderShouldNameItselfAfterItsLabel()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Label, "Volume");
        });

        Assert.AreEqual("Volume", com.Find(".bit-sld-inp").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitSliderShouldPreferAriaLabelOverTheLabel()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Label, "Volume");
            parameters.Add(p => p.AriaLabel, "Master volume");
        });

        Assert.AreEqual("Master volume", com.Find(".bit-sld-inp").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitSliderShouldNameTheTwoEndsOfARangeApart()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.AriaLabel, "Price range");
            parameters.Add(p => p.LowerAriaLabel, "Minimum price");
            parameters.Add(p => p.UpperAriaLabel, "Maximum price");
        });

        Assert.AreEqual("Minimum price", com.Find(".bit-sld-inp-lwr").GetAttribute("aria-label"));
        Assert.AreEqual("Maximum price", com.Find(".bit-sld-inp-upr").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitSliderShouldAnnounceTheValueEachInputActuallyHolds()
    {
        // The keyboard moves an input from the value the input itself is holding, so that is the value it has
        // to announce. After a cross the input named for the lower end is holding the upper one, and saying
        // otherwise would leave a screen reader reading out a number the arrow keys do not move from.
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.DefaultLowerValue, 2D);
            parameters.Add(p => p.DefaultUpperValue, 5D);
        });

        com.Find(".bit-sld-inp-lwr").Input("9");

        Assert.AreEqual("9", com.Find(".bit-sld-inp-lwr").GetAttribute("aria-valuenow"));
        Assert.AreEqual("9", com.Find(".bit-sld-inp-lwr").GetAttribute("aria-valuetext"));
        Assert.AreEqual("5", com.Find(".bit-sld-inp-upr").GetAttribute("aria-valuenow"));
        Assert.AreEqual("5", com.Find(".bit-sld-inp-upr").GetAttribute("aria-valuetext"));
    }

    [TestMethod]
    public void BitSliderShouldSwapTheThumbNamesWhenTheThumbsCross()
    {
        // The name has to describe what the thumb now is rather than what it was named for, since the value
        // beside it has already changed ends.
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.LowerAriaLabel, "Minimum price");
            parameters.Add(p => p.UpperAriaLabel, "Maximum price");
            parameters.Add(p => p.DefaultLowerValue, 2D);
            parameters.Add(p => p.DefaultUpperValue, 5D);
        });

        Assert.AreEqual("Minimum price", com.Find(".bit-sld-inp-lwr").GetAttribute("aria-label"));

        com.Find(".bit-sld-inp-lwr").Input("9");

        Assert.AreEqual("Maximum price", com.Find(".bit-sld-inp-lwr").GetAttribute("aria-label"));
        Assert.AreEqual("Minimum price", com.Find(".bit-sld-inp-upr").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitSliderShouldAnnounceTheBoundsEachEndIsHeldInside()
    {
        // The WAI-ARIA multi-thumb pattern asks each thumb for the bounds it can actually reach, which a
        // constraint narrows to the thumb beside it rather than leaving at the ends of the scale.
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.NoSwap, true);
            parameters.Add(p => p.Max, 100D);
            parameters.Add(p => p.DefaultLowerValue, 30D);
            parameters.Add(p => p.DefaultUpperValue, 70D);
        });

        Assert.AreEqual("0", com.Find(".bit-sld-inp-lwr").GetAttribute("aria-valuemin"));
        Assert.AreEqual("70", com.Find(".bit-sld-inp-lwr").GetAttribute("aria-valuemax"));
        Assert.AreEqual("30", com.Find(".bit-sld-inp-upr").GetAttribute("aria-valuemin"));
        Assert.AreEqual("100", com.Find(".bit-sld-inp-upr").GetAttribute("aria-valuemax"));
    }

    [TestMethod]
    public void BitSliderShouldAnnounceTheWholeScaleOnAPinnedRange()
    {
        // A read-only slider's bounds are a way of refusing the change rather than a description of the
        // value, so the true ends of the scale are what it announces.
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.Max, 100D);
            parameters.Add(p => p.DefaultLowerValue, 30D);
            parameters.Add(p => p.DefaultUpperValue, 70D);
        });

        foreach (var input in com.FindAll(".bit-sld-inp"))
        {
            Assert.AreEqual("0", input.GetAttribute("aria-valuemin"));
            Assert.AreEqual("100", input.GetAttribute("aria-valuemax"));
        }

        // And the bounds that pin it are still its own value.
        Assert.AreEqual("30", com.Find(".bit-sld-inp-lwr").GetAttribute("min"));
        Assert.AreEqual("30", com.Find(".bit-sld-inp-lwr").GetAttribute("max"));
    }

    [TestMethod]
    public void BitSliderShouldFallBackToTheSharedNameOnBothEnds()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.AriaLabel, "Price range");
        });

        Assert.AreEqual("Price range", com.Find(".bit-sld-inp-lwr").GetAttribute("aria-label"));
        Assert.AreEqual("Price range", com.Find(".bit-sld-inp-upr").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitSliderShouldPointANamelessThumbAtItsLabelElement()
    {
        // A slider captioned only by a LabelTemplate has no string to put in an aria-label, and the for/id
        // association of the label element only ever reaches one input - which would leave the upper thumb
        // of a ranged slider nameless.
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.LabelTemplate, "<span>Frequency band</span>");
        });

        var labelId = com.Find(".bit-sld-lbl").GetAttribute("id");

        Assert.IsNotNull(labelId);
        Assert.AreEqual(labelId, com.Find(".bit-sld-inp-lwr").GetAttribute("aria-labelledby"));
        Assert.AreEqual(labelId, com.Find(".bit-sld-inp-upr").GetAttribute("aria-labelledby"));
    }

    [TestMethod]
    public void BitSliderShouldNotPointAtTheLabelElementWhenTheThumbIsAlreadyNamed()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.Label, "Frequency band");
            parameters.Add(p => p.LowerAriaLabel, "Lowest frequency");
        });

        Assert.IsFalse(com.Find(".bit-sld-inp-lwr").HasAttribute("aria-labelledby"));
        Assert.IsFalse(com.Find(".bit-sld-inp-upr").HasAttribute("aria-labelledby"));
    }

    [TestMethod]
    public void BitSliderShouldNotPointAtALabelElementThatIsNotThere()
    {
        var com = RenderComponent<BitSlider>();

        Assert.IsFalse(com.Find(".bit-sld-inp").HasAttribute("aria-labelledby"));
    }

    [TestMethod,
        DataRow(false, null),
        DataRow(true, "vertical")
    ]
    public void BitSliderShouldAnnounceItsOrientation(bool vertical, string expected)
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsVertical, vertical);
        });

        Assert.AreEqual(expected, com.Find(".bit-sld-inp").GetAttribute("aria-orientation"));
    }

    [TestMethod,
        DataRow(false, null),
        DataRow(true, "true")
    ]
    public void BitSliderShouldAnnounceThatItIsRequired(bool required, string expected)
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Required, required);
        });

        Assert.AreEqual(expected, com.Find(".bit-sld-inp").GetAttribute("aria-required"));
    }

    [TestMethod]
    public void BitSliderShouldRespectName()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Name, "volume");
        });

        Assert.AreEqual("volume", com.Find(".bit-sld-inp").GetAttribute("name"));
    }

    [TestMethod]
    public void BitSliderShouldGiveTheTwoEndsOfARangeDistinctFormNames()
    {
        // Posted under one name twice, the two ends of a range would be indistinguishable to a plain form.
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.Name, "budget");
            parameters.Add(p => p.LowerValue, 2D);
            parameters.Add(p => p.UpperValue, 6D);
        });

        Assert.AreEqual("budget", com.Find(".bit-sld-inp-lwr").GetAttribute("name"));
        Assert.AreEqual("budget-upper", com.Find(".bit-sld-inp-upr").GetAttribute("name"));
    }

    [TestMethod,
        DataRow(false),
        DataRow(true)
    ]
    public void BitSliderShouldRespectAutoFocus(bool autoFocus)
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.AutoFocus, autoFocus);
        });

        Assert.AreEqual(autoFocus, com.Find(".bit-sld-inp").HasAttribute("autofocus"));
    }

    [TestMethod]
    public void BitSliderShouldExposeTheUpperInputOfARangeSeparately()
    {
        // FocusAsync is pointed at the single tab stop of a plain slider, and so at the lower thumb of a
        // ranged one, which leaves the upper thumb with no way of being reached from code at all.
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.DefaultLowerValue, 2D);
            parameters.Add(p => p.DefaultUpperValue, 6D);
        });

        Assert.AreNotEqual(com.Instance.SliderInputElement.Id, com.Instance.UpperSliderInputElement.Id);
    }

    [TestMethod]
    public void BitSliderShouldPointTheUpperInputAtTheOnlyOneASingleValueSliderHas()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Value, 4D);
        });

        Assert.AreEqual(com.Instance.SliderInputElement.Id, com.Instance.UpperSliderInputElement.Id);
    }

    [TestMethod]
    public void BitSliderShouldRespectTabIndex()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.TabIndex, "3");
        });

        Assert.AreEqual("3", com.Find(".bit-sld-inp").GetAttribute("tabindex"));
    }

    #endregion

    #region Styles, classes and html attributes

    [TestMethod]
    public void BitSliderShouldRespectStyleAndClass()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Style, "color:red");
            parameters.Add(p => p.Class, "custom-class");
        });

        var slider = com.Find(".bit-sld");

        StringAssert.Contains(slider.GetAttribute("style"), "color:red");
        Assert.IsTrue(slider.ClassList.Contains("custom-class"));
    }

    [TestMethod]
    public void BitSliderShouldRespectClassesAndStyles()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Label, "Labelled");
            parameters.Add(p => p.ShowMarks, true);
            parameters.Add(p => p.Classes, new BitSliderClassStyles
            {
                Root = "custom-root",
                Label = "custom-label",
                Container = "custom-container",
                SliderBox = "custom-box",
                Track = "custom-track",
                Fill = "custom-fill",
                ValueInput = "custom-input",
                ValueLabel = "custom-value",
                Mark = "custom-mark"
            });
            parameters.Add(p => p.Styles, new BitSliderClassStyles
            {
                Root = "font-size:2rem",
                Fill = "opacity:0.5"
            });
        });

        Assert.IsTrue(com.Find(".bit-sld").ClassList.Contains("custom-root"));
        Assert.IsTrue(com.Find(".bit-sld-lbl").ClassList.Contains("custom-label"));
        Assert.IsTrue(com.Find(".bit-sld-cnt").ClassList.Contains("custom-container"));
        Assert.IsTrue(com.Find(".bit-sld-box").ClassList.Contains("custom-box"));
        Assert.IsTrue(com.Find(".bit-sld-trk").ClassList.Contains("custom-track"));
        Assert.IsTrue(com.Find(".bit-sld-fil").ClassList.Contains("custom-fill"));
        Assert.IsTrue(com.Find(".bit-sld-inp").ClassList.Contains("custom-input"));
        Assert.IsTrue(com.Find(".bit-sld-vlb").ClassList.Contains("custom-value"));
        Assert.IsTrue(com.Find(".bit-sld-mrk").ClassList.Contains("custom-mark"));

        StringAssert.Contains(com.Find(".bit-sld").GetAttribute("style"), "font-size:2rem");
        StringAssert.Contains(com.Find(".bit-sld-fil").GetAttribute("style"), "opacity:0.5");
    }

    [TestMethod]
    public void BitSliderShouldRespectRangeSpecificClasses()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.Classes, new BitSliderClassStyles
            {
                LowerValueInput = "custom-lower-input",
                UpperValueInput = "custom-upper-input",
                LowerValueLabel = "custom-lower-label",
                UpperValueLabel = "custom-upper-label"
            });
        });

        Assert.IsTrue(com.Find(".bit-sld-inp-lwr").ClassList.Contains("custom-lower-input"));
        Assert.IsTrue(com.Find(".bit-sld-inp-upr").ClassList.Contains("custom-upper-input"));
        Assert.IsTrue(com.FindAll(".bit-sld-vlb")[0].ClassList.Contains("custom-lower-label"));
        Assert.IsTrue(com.FindAll(".bit-sld-vlb")[1].ClassList.Contains("custom-upper-label"));
    }

    [TestMethod]
    public void BitSliderShouldRespectTheThumbClassesAndStyles()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Value, 4D);
            parameters.Add(p => p.Classes, new BitSliderClassStyles { Thumb = "custom-thumb" });
            parameters.Add(p => p.Styles, new BitSliderClassStyles { Thumb = "border-radius:0.25rem" });
        });

        var thumb = com.Find(".bit-sld-thb");

        Assert.IsTrue(thumb.ClassList.Contains("custom-thumb"));
        StringAssert.Contains(thumb.GetAttribute("style"), "border-radius:0.25rem");

        // The position the thumb is drawn at is still there beside the custom style.
        StringAssert.Contains(thumb.GetAttribute("style"), "inset-inline-start:40%");
    }

    [TestMethod]
    public void BitSliderShouldRespectTheEndSpecificThumbClassesAndStyles()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.LowerValue, 2D);
            parameters.Add(p => p.UpperValue, 8D);
            parameters.Add(p => p.Classes, new BitSliderClassStyles
            {
                Thumb = "custom-thumb",
                LowerThumb = "custom-lower-thumb",
                UpperThumb = "custom-upper-thumb"
            });
            parameters.Add(p => p.Styles, new BitSliderClassStyles
            {
                LowerThumb = "border-color:mediumseagreen",
                UpperThumb = "border-color:orangered"
            });
        });

        var lower = com.Find(".bit-sld-thb-lwr");
        var upper = com.Find(".bit-sld-thb-upr");

        // The shared class reaches both thumbs, and the end-specific one only its own.
        Assert.IsTrue(lower.ClassList.Contains("custom-thumb"));
        Assert.IsTrue(upper.ClassList.Contains("custom-thumb"));
        Assert.IsTrue(lower.ClassList.Contains("custom-lower-thumb"));
        Assert.IsFalse(lower.ClassList.Contains("custom-upper-thumb"));
        Assert.IsTrue(upper.ClassList.Contains("custom-upper-thumb"));
        Assert.IsFalse(upper.ClassList.Contains("custom-lower-thumb"));

        StringAssert.Contains(lower.GetAttribute("style"), "border-color:mediumseagreen");
        StringAssert.Contains(upper.GetAttribute("style"), "border-color:orangered");
    }

    [TestMethod]
    public void BitSliderShouldRespectSliderBoxHtmlAttributes()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.SliderBoxHtmlAttributes, new Dictionary<string, object> { { "data-test", "box" } });
        });

        Assert.AreEqual("box", com.Find(".bit-sld-box").GetAttribute("data-test"));
    }

    [TestMethod]
    public void BitSliderShouldRespectInputHtmlAttributes()
    {
        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.InputHtmlAttributes, new Dictionary<string, object> { { "data-test", "input" } });
        });

        Assert.AreEqual("input", com.Find(".bit-sld-inp").GetAttribute("data-test"));
    }

    #endregion

    #region Validation

    [TestMethod]
    public void BitSliderShouldReportAnInvalidValueToTheEditContext()
    {
        var com = RenderComponent<BitSliderValidationTest>();

        com.Find("form").Submit();

        Assert.AreEqual(1, com.Instance.InvalidCount);
        Assert.AreEqual(0, com.Instance.ValidCount);
        Assert.IsTrue(com.Find(".bit-sld").ClassList.Contains("bit-inv"));
        Assert.AreEqual("true", com.Find(".bit-sld-inp").GetAttribute("aria-invalid"));
    }

    [TestMethod]
    public void BitSliderShouldClearTheInvalidStateOnceTheValueIsInRange()
    {
        var com = RenderComponent<BitSliderValidationTest>();

        com.Find("form").Submit();

        Assert.IsTrue(com.Find(".bit-sld").ClassList.Contains("bit-inv"));

        com.Find(".bit-sld-inp").Input("5");
        com.Find("form").Submit();

        Assert.AreEqual(1, com.Instance.ValidCount);
        Assert.IsFalse(com.Find(".bit-sld").ClassList.Contains("bit-inv"));
    }

    [TestMethod]
    public void BitSliderShouldReportAnInvalidRangeToTheEditContext()
    {
        // A ranged slider selects a pair rather than a single number, so the field it reports is the pair -
        // which is what lets it be annotated and validated like any other field of the model.
        var com = RenderComponent<BitSliderRangeValidationTest>();

        com.Find("form").Submit();

        Assert.AreEqual(1, com.Instance.InvalidCount);
        Assert.AreEqual(0, com.Instance.ValidCount);
        Assert.IsTrue(com.Find(".bit-sld").ClassList.Contains("bit-inv"));
        Assert.AreEqual("true", com.Find(".bit-sld-inp-lwr").GetAttribute("aria-invalid"));
        Assert.AreEqual("true", com.Find(".bit-sld-inp-upr").GetAttribute("aria-invalid"));
    }

    [TestMethod]
    public void BitSliderShouldRevalidateARangeAsAThumbMoves()
    {
        var com = RenderComponent<BitSliderRangeValidationTest>();

        com.Find("form").Submit();

        Assert.IsTrue(com.Find(".bit-sld").ClassList.Contains("bit-inv"));

        // Every move of a thumb is reported to the EditContext, so the error clears as soon as the pair is
        // wide enough rather than waiting for the next submit.
        com.Find(".bit-sld-inp-upr").Input("8");

        Assert.IsFalse(com.Find(".bit-sld").ClassList.Contains("bit-inv"));

        com.Find("form").Submit();

        Assert.AreEqual(1, com.Instance.ValidCount);
    }

    [TestMethod]
    public void BitSliderShouldNotValidateWhenNoValidateIsSet()
    {
        // The value of the fixture starts outside the range its model asks for, so the submit below leaves an
        // error on the field - which a slider that was told not to validate must not paint itself with.
        var com = RenderComponent<BitSliderValidationTest>(parameters =>
        {
            parameters.Add(p => p.NoValidate, true);
        });

        com.Find("form").Submit();

        Assert.AreEqual(1, com.Instance.InvalidCount);
        Assert.IsFalse(com.Find(".bit-sld").ClassList.Contains("bit-inv"));
        Assert.IsNull(com.Find(".bit-sld-inp").GetAttribute("aria-invalid"));
    }

    #endregion
}
