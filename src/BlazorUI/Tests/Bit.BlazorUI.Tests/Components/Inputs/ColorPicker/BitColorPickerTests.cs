using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;


namespace Bit.BlazorUI.Tests.Components.Inputs.ColorPicker;

[TestClass]
public class BitColorPickerTests : BunitTestContext
{
    [TestInitialize]
    public void SetupJsInteropMode()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitColorPickerMustRespectUiChange(bool show)
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.ShowPreview, show);
            parameters.Add(p => p.ShowAlphaSlider, show);
            parameters.Add(p => p.Color, "rgb(255,255,255)");
        });

        if (show)
        {
            var slider = com.Find(".bit-clp-asd");
            Assert.IsNotNull(slider);

            var previewBox = com.Find(".bit-clp-pre");
            Assert.IsNotNull(previewBox);
        }
        else
        {
            Assert.Throws<ElementNotFoundException>(() => com.Find(".bit-clp-asd"));
            Assert.Throws<ElementNotFoundException>(() => com.Find(".bit-clp-pre"));
        }
    }

    // A bound Color is echoed back exactly as it was given - it is a two-way parameter, not a normalizer -
    // while the read-only notation properties always describe the color the picker is actually on.
    [TestMethod,
        DataRow("#FC5603", "#FC5603", "rgb(252,86,3)", 1),
        DataRow("rgba(3,98,252,0.3)", "#0362FC", "rgb(3,98,252)", 0.3),
        DataRow("rgb(252,3,240)", "#FC03F0", "rgb(252,3,240)", 1)
    ]
    public void BitColorPickerMustRespectValueChange(string color, string hex, string rgb, double alpha)
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.Color, color);
            parameters.Add(p => p.Alpha, alpha);
        });

        Assert.AreEqual(color, com.Instance.Color);
        Assert.AreEqual(hex, com.Instance.Hex);
        Assert.AreEqual(rgb, com.Instance.Rgb);
        Assert.AreEqual(alpha, com.Instance.Alpha);
    }

    // With nothing bound to it, the Color parameter still reports the color the picker is on, so an
    // uncontrolled picker can be read without a binding.
    [TestMethod]
    public void BitColorPickerShouldStartOnOpaqueWhite()
    {
        var com = RenderComponent<BitColorPicker>();

        Assert.AreEqual("rgb(255,255,255)", com.Instance.Color);
        Assert.AreEqual("#FFFFFF", com.Instance.Hex);
        Assert.AreEqual("#FFFFFFFF", com.Instance.HexAlpha);
        Assert.AreEqual("rgba(255,255,255,1)", com.Instance.Rgba);
        Assert.AreEqual(1, com.Instance.Alpha);
    }

    [TestMethod,
        DataRow(BitColorFormat.Hex, "#FF0000"),
        DataRow(BitColorFormat.HexAlpha, "#FF000080"),
        DataRow(BitColorFormat.Rgb, "rgb(255,0,0)"),
        DataRow(BitColorFormat.Rgba, "rgba(255,0,0,0.5)"),
        DataRow(BitColorFormat.Hsl, "hsl(0,100%,50%)"),
        DataRow(BitColorFormat.Hsla, "hsla(0,100%,50%,0.5)"),
        DataRow(BitColorFormat.Hsv, "hsv(0,100%,100%)"),
        DataRow(BitColorFormat.Hsva, "hsva(0,100%,100%,0.5)")
    ]
    public void BitColorPickerShouldWriteTheRequestedFormat(BitColorFormat format, string expected)
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.Format, format);
            parameters.Add(p => p.ShowAlphaSlider, true);
        });

        // Uncontrolled, so the Color parameter is the picker's own answer in the requested notation.
        com.Instance.HandlePointerMove(1, 0);
        com.Instance.HandlePointerUp();
        com.Find(".bit-clp-hsd .bit-clp-inp").Input("0");
        com.Find(".bit-clp-asd .bit-clp-inp").Input("0.5");

        Assert.AreEqual(expected, com.Instance.Color);
    }

    // Left unset, the format is read off the value the picker was given, so a hexadecimal binding stays
    // hexadecimal and an rgb() one stays rgb() without anyone having to say so.
    [TestMethod,
        DataRow("#FFFFFF", "#FF0000"),
        DataRow("rgb(255,255,255)", "rgb(255,0,0)"),
        DataRow("rgba(255,255,255,1)", "rgba(255,0,0,1)"),
        DataRow("hsl(0,0%,100%)", "hsl(0,100%,50%)")
    ]
    public void BitColorPickerShouldKeepTheNotationItWasGiven(string initial, string expected)
    {
        var color = initial;

        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Bind(p => p.Color, color, v => color = v);
        });

        com.Instance.HandlePointerMove(1, 0);

        Assert.AreEqual(expected, color);
    }

    [TestMethod]
    public void BitColorPickerShouldRespectTwoWayBinding()
    {
        var color = "#FFFFFF";
        var alpha = 1d;

        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.ShowAlphaSlider, true);
            parameters.Bind(p => p.Color, color, v => color = v);
            parameters.Bind(p => p.Alpha, alpha, v => alpha = v);
        });

        com.Find(".bit-clp-asd .bit-clp-inp").Input("0.25");

        Assert.AreEqual(0.25, alpha);
        Assert.AreEqual(0.25, com.Instance.Alpha);
    }

    // A one-way Color has nowhere to report a change to, so the picker becomes a display of that value
    // rather than pretending to accept edits.
    [TestMethod]
    public void BitColorPickerShouldStayInertWithAOneWayColor()
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.Color, "#FFFFFF");
            parameters.Add(p => p.ShowAlphaSlider, true);
        });

        com.Instance.HandlePointerMove(1, 0);

        Assert.AreEqual("#FFFFFF", com.Instance.Color);
        Assert.AreEqual("#FFFFFF", com.Instance.Hex);

        // The sliders are native ranges, which move their own thumb before the handler that refuses the
        // change is reached, so they are disabled rather than left to be dragged to no effect.
        Assert.IsTrue(com.Find(".bit-clp-hsd .bit-clp-inp").HasAttribute("disabled"));
        Assert.IsTrue(com.Find(".bit-clp-asd .bit-clp-inp").HasAttribute("disabled"));
    }

    // OnChange is a valid binding of its own: it is how an uncontrolled picker reports its result.
    [TestMethod]
    public void BitColorPickerShouldChangeWithOnChangeAlone()
    {
        BitColorChangeEventArgs? args = null;

        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.Color, "#FFFFFF");
            parameters.Add(p => p.OnChange, (BitColorChangeEventArgs a) => args = a);
        });

        com.Instance.HandlePointerMove(1, 0);

        Assert.IsNotNull(args);
        Assert.AreEqual("#FF0000", args.Hex);
        Assert.AreEqual("rgb(255,0,0)", args.Rgb);
        Assert.AreEqual("rgba(255,0,0,1)", args.Rgba);
        Assert.AreEqual("#FF0000FF", args.HexAlpha);
        Assert.AreEqual(1, args.Alpha);
        Assert.AreEqual(0, Math.Round(args.Hsv.Hue));
        Assert.AreEqual(0.5, Math.Round(args.Hsl.Lightness, 2));
    }

    // OnChange fires on every step of a drag; OnChangeEnd fires once, when the gesture is over.
    [TestMethod]
    public void BitColorPickerShouldRaiseOnChangeEndOnlyWhenTheGestureEnds()
    {
        var changes = 0;
        var ends = 0;

        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.OnChange, () => changes++);
            parameters.Add(p => p.OnChangeEnd, () => ends++);
        });

        com.Instance.HandlePointerMove(0.2, 0.2);
        com.Instance.HandlePointerMove(0.4, 0.4);
        com.Instance.HandlePointerMove(0.6, 0.6);

        Assert.AreEqual(3, changes);
        Assert.AreEqual(0, ends);

        com.Instance.HandlePointerUp();

        Assert.AreEqual(3, changes);
        Assert.AreEqual(1, ends);
    }

    // A pointer up with no drag behind it has nothing to report, so it stays quiet.
    [TestMethod]
    public void BitColorPickerShouldNotRaiseOnChangeEndWithoutADrag()
    {
        var ends = 0;

        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.OnChangeEnd, () => ends++);
        });

        com.Instance.HandlePointerUp();

        Assert.AreEqual(0, ends);
    }

    // The pointer position arrives as a fraction of the area, measured from its top-left corner, so the
    // four corners are the four extremes of the saturation-brightness square.
    [TestMethod,
        DataRow(0d, 0d, "#FFFFFF"),
        DataRow(1d, 0d, "#FF0000"),
        DataRow(0d, 1d, "#000000"),
        DataRow(1d, 1d, "#000000"),
        DataRow(0.5d, 0.5d, "#804040")
    ]
    public void BitColorPickerShouldMapThePointerOntoTheArea(double x, double y, string expected)
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.Color, "#FF0000");
            parameters.Add(p => p.OnChange, () => { });
        });

        com.Instance.HandlePointerMove(x, y);

        Assert.AreEqual(expected, com.Instance.Hex);
    }

    // The area is measured from its inline-start edge, which is the right one in a right-to-left picker,
    // so the horizontal axis mirrors along with the gradient drawn on it.
    [TestMethod]
    public void BitColorPickerShouldMirrorThePointerAxisInRtl()
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Rtl);
            parameters.Add(p => p.Color, "#FF0000");
            parameters.Add(p => p.OnChange, () => { });
        });

        com.Instance.HandlePointerMove(0, 0);

        Assert.AreEqual("#FF0000", com.Instance.Hex);

        com.Instance.HandlePointerMove(1, 0);

        Assert.AreEqual("#FFFFFF", com.Instance.Hex);
    }

    [TestMethod]
    public void BitColorPickerShouldRespectTheHueSlider()
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.Color, "#FF0000");
            parameters.Add(p => p.OnChange, () => { });
        });

        com.Find(".bit-clp-hsd .bit-clp-inp").Input("120");

        Assert.AreEqual("#00FF00", com.Instance.Hex);
        Assert.AreEqual(120, Math.Round(com.Instance.Hsv.Hue));
    }

    [TestMethod]
    public void BitColorPickerShouldRespectTheAlphaSlider()
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.ShowAlphaSlider, true);
            parameters.Add(p => p.Color, "#FF0000");
            parameters.Add(p => p.OnChange, () => { });
        });

        com.Find(".bit-clp-asd .bit-clp-inp").Input("0.4");

        Assert.AreEqual(0.4, com.Instance.Alpha);
        Assert.AreEqual("rgba(255,0,0,0.4)", com.Instance.Rgba);
    }

    // The sliders report continuously while dragged and once more when released, which is the difference
    // between OnChange and OnChangeEnd on a native range input.
    [TestMethod]
    public void BitColorPickerSlidersShouldReportBothChangeAndChangeEnd()
    {
        var changes = 0;
        var ends = 0;

        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.OnChange, () => changes++);
            parameters.Add(p => p.OnChangeEnd, () => ends++);
        });

        com.Find(".bit-clp-hsd .bit-clp-inp").Input("120");
        Assert.AreEqual(1, changes);
        Assert.AreEqual(0, ends);

        com.Find(".bit-clp-hsd .bit-clp-inp").Change("200");
        Assert.AreEqual(2, changes);
        Assert.AreEqual(1, ends);
    }

    // Releasing a range fires change with the same value the last input already carried, so the release
    // only ends the gesture instead of reporting a change that has already been reported.
    [TestMethod]
    public void BitColorPickerSliderReleaseShouldNotReportTheSameValueTwice()
    {
        var changes = 0;
        var ends = 0;

        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.OnChange, () => changes++);
            parameters.Add(p => p.OnChangeEnd, () => ends++);
        });

        com.Find(".bit-clp-hsd .bit-clp-inp").Input("120");
        com.Find(".bit-clp-hsd .bit-clp-inp").Change("120");

        Assert.AreEqual(1, changes);
        Assert.AreEqual(1, ends);
    }

    // A pointer that has not left the color it is already on - a press that lands on the thumb, or a move
    // along an edge the position is clamped against - has nothing to report.
    [TestMethod]
    public void BitColorPickerShouldNotReportAPointerThatMovesNothing()
    {
        var changes = 0;

        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.OnChange, () => changes++);
        });

        // The picker starts on opaque white, which is the top-left corner of the area.
        com.Instance.HandlePointerMove(0, 0);

        Assert.AreEqual(0, changes);

        com.Instance.HandlePointerMove(0.5, 0.5);

        Assert.AreEqual(1, changes);
    }

    // The saturation-brightness area is a two-dimensional control that no ARIA role covers, so it is
    // exposed as a slider over the saturation, named a 2D slider, with both axes in its value text.
    [TestMethod]
    public void BitColorPickerShouldExposeTheAreaAsATwoDimensionalSlider()
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.Color, "#FF0000");
        });

        var area = com.Find(".bit-clp-rec");

        Assert.AreEqual("slider", area.GetAttribute("role"));
        Assert.AreEqual("2D slider", area.GetAttribute("aria-roledescription"));
        Assert.AreEqual("0", area.GetAttribute("aria-valuemin"));
        Assert.AreEqual("100", area.GetAttribute("aria-valuemax"));
        Assert.AreEqual("100", area.GetAttribute("aria-valuenow"));
        Assert.AreEqual("Saturation 100%, Brightness 100%, #FF0000", area.GetAttribute("aria-valuetext"));
        Assert.AreEqual("0", area.GetAttribute("tabindex"));
    }

    [TestMethod,
        DataRow("ArrowRight", false, 61, 50),
        DataRow("ArrowLeft", false, 59, 50),
        DataRow("ArrowUp", false, 60, 51),
        DataRow("ArrowDown", false, 60, 49),
        DataRow("ArrowRight", true, 70, 50),
        DataRow("ArrowLeft", true, 50, 50),
        DataRow("ArrowUp", true, 60, 60),
        DataRow("ArrowDown", true, 60, 40),
        DataRow("PageUp", false, 60, 60),
        DataRow("PageDown", false, 60, 40),
        DataRow("Home", false, 0, 50),
        DataRow("End", false, 100, 50)
    ]
    public void BitColorPickerShouldWalkTheAreaWithTheKeyboard(string key, bool shift, int saturation, int value)
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.Color, "hsv(210,60%,50%)");
            parameters.Add(p => p.OnChange, () => { });
        });

        com.Find(".bit-clp-rec").KeyDown(new KeyboardEventArgs { Key = key, ShiftKey = shift });

        Assert.AreEqual(saturation, (int)Math.Round(com.Instance.Hsv.Saturation * 100));
        Assert.AreEqual(value, (int)Math.Round(com.Instance.Hsv.Value * 100));
    }

    // The horizontal arrows follow the reading direction, so they swap in right-to-left.
    [TestMethod,
        DataRow("ArrowRight", 59),
        DataRow("ArrowLeft", 61)
    ]
    public void BitColorPickerShouldMirrorTheArrowKeysInRtl(string key, int saturation)
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Rtl);
            parameters.Add(p => p.Color, "hsv(210,60%,50%)");
            parameters.Add(p => p.OnChange, () => { });
        });

        com.Find(".bit-clp-rec").KeyDown(key);

        Assert.AreEqual(saturation, (int)Math.Round(com.Instance.Hsv.Saturation * 100));
    }

    // A keypress is a whole gesture on its own, so it commits rather than merely previewing.
    [TestMethod]
    public void BitColorPickerKeyboardShouldRaiseBothCallbacks()
    {
        var changes = 0;
        var ends = 0;

        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.OnChange, () => changes++);
            parameters.Add(p => p.OnChangeEnd, () => ends++);
        });

        com.Find(".bit-clp-rec").KeyDown("ArrowDown");

        Assert.AreEqual(1, changes);
        Assert.AreEqual(1, ends);
    }

    // A key that moves nothing - an arrow at the edge it already sits on, or a key the area does not
    // handle - reports nothing either.
    [TestMethod,
        DataRow("ArrowUp"),
        DataRow("Tab"),
        DataRow("Enter"),
        DataRow("Escape")
    ]
    public void BitColorPickerKeyboardShouldStayQuietWhenNothingMoves(string key)
    {
        var changes = 0;

        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.OnChange, () => changes++);
        });

        com.Find(".bit-clp-rec").KeyDown(key);

        Assert.AreEqual(0, changes);
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitColorPickerShouldRespectIsEnabled(bool isEnabled)
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.ShowAlphaSlider, true);
            parameters.Add(p => p.ShowInputs, true);
            parameters.Add(p => p.OnChange, () => { });
        });

        var root = com.Find(".bit-clp");

        Assert.AreEqual(isEnabled is false, root.ClassList.Contains("bit-dis"));
        Assert.AreEqual(isEnabled ? null : "true", root.GetAttribute("aria-disabled"));
        Assert.AreEqual(isEnabled ? "0" : "-1", com.Find(".bit-clp-rec").GetAttribute("tabindex"));
        Assert.AreEqual(isEnabled is false, com.Find(".bit-clp-hsd .bit-clp-inp").HasAttribute("disabled"));
        Assert.AreEqual(isEnabled is false, com.Find(".bit-clp-fhx .bit-clp-fin").HasAttribute("disabled"));

        com.Instance.HandlePointerMove(1, 1);

        Assert.AreEqual(isEnabled ? "#000000" : "#FFFFFF", com.Instance.Hex);
    }

    // A read-only picker keeps its full contrast - it is showing a value, not refusing one - but nothing
    // on it responds to the pointer or the keyboard.
    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitColorPickerShouldRespectReadOnly(bool readOnly)
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, readOnly);
            parameters.Add(p => p.OnChange, () => { });
        });

        var root = com.Find(".bit-clp");

        Assert.AreEqual(readOnly, root.ClassList.Contains("bit-clp-rdl"));
        Assert.IsFalse(root.ClassList.Contains("bit-dis"));
        Assert.AreEqual(readOnly ? "true" : null, root.GetAttribute("aria-readonly"));
        Assert.AreEqual(readOnly ? "-1" : "0", com.Find(".bit-clp-rec").GetAttribute("tabindex"));

        com.Find(".bit-clp-rec").KeyDown("ArrowDown");

        Assert.AreEqual(readOnly ? "#FFFFFF" : "#FCFCFC", com.Instance.Hex);
    }

    [TestMethod,
        DataRow(null, "bit-clp-md"),
        DataRow(BitSize.Small, "bit-clp-sm"),
        DataRow(BitSize.Medium, "bit-clp-md"),
        DataRow(BitSize.Large, "bit-clp-lg")
    ]
    public void BitColorPickerShouldRespectSize(BitSize? size, string expectedClass)
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.Size, size);
        });

        Assert.IsTrue(com.Find(".bit-clp").ClassList.Contains(expectedClass));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitColorPickerShouldRespectShowInputs(bool showInputs)
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.ShowInputs, showInputs);
        });

        if (showInputs)
        {
            // The hexadecimal field plus one per channel.
            Assert.AreEqual(4, com.FindAll(".bit-clp-fin").Count);
            Assert.AreEqual("#FFFFFF", com.Find(".bit-clp-fhx .bit-clp-fin").GetAttribute("value"));
        }
        else
        {
            Assert.AreEqual(0, com.FindAll(".bit-clp-fin").Count);
        }
    }

    // Turning the alpha slider on grows the hexadecimal field to eight digits and adds a percentage
    // field beside the channels.
    [TestMethod]
    public void BitColorPickerInputsShouldFollowTheAlphaSlider()
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.ShowInputs, true);
            parameters.Add(p => p.ShowAlphaSlider, true);
            parameters.Add(p => p.Color, "rgba(255,0,0,0.5)");
        });

        Assert.AreEqual(5, com.FindAll(".bit-clp-fin").Count);
        Assert.AreEqual("#FF000080", com.Find(".bit-clp-fhx .bit-clp-fin").GetAttribute("value"));
    }

    [TestMethod,
        DataRow("#00FF00", "#00FF00", 1.0),
        DataRow("#0f0", "#00FF00", 1.0),
        DataRow("00FF00", "#FFFFFF", 1.0),
        DataRow("#00FF0080", "#00FF00", 0.5),
        DataRow("nonsense", "#FFFFFF", 1.0)
    ]
    public void BitColorPickerShouldCommitTheHexField(string typed, string expectedHex, double expectedAlpha)
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.ShowInputs, true);
            parameters.Add(p => p.ShowAlphaSlider, true);
            parameters.Add(p => p.OnChange, () => { });
        });

        com.Find(".bit-clp-fhx .bit-clp-fin").Change(typed);

        Assert.AreEqual(expectedHex, com.Instance.Hex);
        Assert.AreEqual(expectedAlpha, Math.Round(com.Instance.Alpha, 2));
    }

    // A six-digit hex typed into a semi-transparent color keeps that transparency, since it says nothing
    // about the alpha; an eight-digit one brings its own.
    [TestMethod]
    public void BitColorPickerHexFieldShouldKeepTheCurrentAlpha()
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.ShowInputs, true);
            parameters.Add(p => p.ShowAlphaSlider, true);
            parameters.Add(p => p.Color, "rgba(255,0,0,0.25)");
            parameters.Add(p => p.OnChange, () => { });
        });

        com.Find(".bit-clp-fhx .bit-clp-fin").Change("#00FF00");

        Assert.AreEqual("#00FF00", com.Instance.Hex);
        Assert.AreEqual(0.25, com.Instance.Alpha);
    }

    [TestMethod,
        DataRow(1, "0", "#FF0000"),
        DataRow(1, "255", "#FFFF00"),
        DataRow(2, "128", "#FF0080"),
        DataRow(1, "999", "#FFFF00"),
        DataRow(1, "-5", "#FF0000")
    ]
    public void BitColorPickerShouldCommitTheChannelFields(int index, string typed, string expected)
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.ShowInputs, true);
            parameters.Add(p => p.Color, "#FF0000");
            parameters.Add(p => p.OnChange, () => { });
        });

        // The hexadecimal field comes first, so the channel fields start at the second input.
        com.FindAll(".bit-clp-fin")[index + 1].Change(typed);

        Assert.AreEqual(expected, com.Instance.Hex);
    }

    [TestMethod]
    public void BitColorPickerShouldCommitTheAlphaField()
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.ShowInputs, true);
            parameters.Add(p => p.ShowAlphaSlider, true);
            parameters.Add(p => p.Color, "#FF0000");
            parameters.Add(p => p.OnChange, () => { });
        });

        com.FindAll(".bit-clp-fin")[4].Change("40");

        Assert.AreEqual(0.4, com.Instance.Alpha);
    }

    [TestMethod]
    public void BitColorPickerShouldRenderPresets()
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.Presets, ["#FF0000", "rgb(0,255,0)", "blue"]);
            parameters.Add(p => p.Color, "#FF0000");
        });

        var presets = com.FindAll(".bit-clp-prt");

        Assert.AreEqual(3, presets.Count);
        Assert.AreEqual("#FF0000", presets[0].GetAttribute("aria-label"));
        Assert.AreEqual("true", presets[0].GetAttribute("aria-pressed"));
        Assert.AreEqual("false", presets[1].GetAttribute("aria-pressed"));

        // The raw preset string never reaches the style attribute: it is normalized first, which is also
        // what lets a swatch be compared against the current color.
        StringAssert.Contains(presets[2].GetAttribute("style"), "--bit-clp-prt-clr:rgba(0,0,255,1)");
    }

    [TestMethod]
    public void BitColorPickerShouldPickAPreset()
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.Presets, ["#FF0000", "rgba(0,0,255,0.5)"]);
            parameters.Add(p => p.OnChange, () => { });
        });

        com.FindAll(".bit-clp-prt")[0].Click();

        Assert.AreEqual("#FF0000", com.Instance.Hex);
        Assert.AreEqual(1, com.Instance.Alpha);

        // A preset that carries its own alpha applies it too.
        com.FindAll(".bit-clp-prt")[1].Click();

        Assert.AreEqual("#0000FF", com.Instance.Hex);
        Assert.AreEqual(0.5, com.Instance.Alpha);
    }

    // A preset that carries no alpha of its own keeps the transparency already dialled in.
    [TestMethod]
    public void BitColorPickerPresetShouldKeepTheCurrentAlpha()
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.ShowAlphaSlider, true);
            parameters.Add(p => p.Presets, ["#00FF00"]);
            parameters.Add(p => p.Color, "rgba(255,0,0,0.3)");
            parameters.Add(p => p.OnChange, () => { });
        });

        com.Find(".bit-clp-prt").Click();

        Assert.AreEqual("#00FF00", com.Instance.Hex);
        Assert.AreEqual(0.3, com.Instance.Alpha);
    }

    [TestMethod]
    public void BitColorPickerShouldNotRenderPresetsWhenThereAreNone()
    {
        var com = RenderComponent<BitColorPicker>();

        Assert.AreEqual(0, com.FindAll(".bit-clp-prs").Count);
    }

    // The eyedropper is a browser capability rather than a parameter: the button is only rendered once
    // the browser has confirmed it has one to open.
    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitColorPickerShouldRenderTheEyeDropperOnlyWhereItIsSupported(bool supported)
    {
        Context.JSInterop.Setup<bool>("BitBlazorUI.ColorPicker.isEyeDropperSupported").SetResult(supported);

        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.ShowEyeDropper, true);
        });

        Assert.AreEqual(supported ? 1 : 0, com.FindAll(".bit-clp-eyd").Count);
    }

    [TestMethod]
    public void BitColorPickerShouldNotRenderTheEyeDropperWhenNotAskedFor()
    {
        Context.JSInterop.Setup<bool>("BitBlazorUI.ColorPicker.isEyeDropperSupported").SetResult(true);

        var com = RenderComponent<BitColorPicker>();

        Assert.AreEqual(0, com.FindAll(".bit-clp-eyd").Count);
    }

    [TestMethod]
    public void BitColorPickerShouldApplyTheSampledEyeDropperColor()
    {
        Context.JSInterop.Setup<bool>("BitBlazorUI.ColorPicker.isEyeDropperSupported").SetResult(true);
        Context.JSInterop.Setup<string?>("BitBlazorUI.ColorPicker.openEyeDropper").SetResult("#123456");

        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.ShowEyeDropper, true);
            parameters.Add(p => p.OnChange, () => { });
        });

        com.Find(".bit-clp-eyd").Click();

        Assert.AreEqual("#123456", com.Instance.Hex);
    }

    // Dismissing the eyedropper resolves to null, which leaves the color exactly where it was.
    [TestMethod]
    public void BitColorPickerShouldKeepTheColorWhenTheEyeDropperIsDismissed()
    {
        Context.JSInterop.Setup<bool>("BitBlazorUI.ColorPicker.isEyeDropperSupported").SetResult(true);
        Context.JSInterop.Setup<string?>("BitBlazorUI.ColorPicker.openEyeDropper").SetResult(null);

        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.ShowEyeDropper, true);
            parameters.Add(p => p.Color, "#FF0000");
            parameters.Add(p => p.OnChange, () => { });
        });

        com.Find(".bit-clp-eyd").Click();

        Assert.AreEqual("#FF0000", com.Instance.Hex);
    }

    [TestMethod]
    public void BitColorPickerShouldRespectEyeDropperIcons()
    {
        Context.JSInterop.Setup<bool>("BitBlazorUI.ColorPicker.isEyeDropperSupported").SetResult(true);

        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.ShowEyeDropper, true);
        });

        Assert.IsTrue(com.Find(".bit-clp-eyi").ClassList.Contains("bit-icon--Eyedropper"));

        com.Render(parameters => parameters.Add(p => p.EyeDropperIconName, "Color"));
        Assert.IsTrue(com.Find(".bit-clp-eyi").ClassList.Contains("bit-icon--Color"));

        // The BitIconInfo wins when both are set, which is how an external icon library is used.
        com.Render(parameters => parameters.Add(p => p.EyeDropperIcon, BitIconInfo.Fa("solid eye-dropper")));
        Assert.IsTrue(com.Find(".bit-clp-eyi").ClassList.Contains("fa-eye-dropper"));
    }

    // The thumb is positioned as a percentage of the area rather than a pixel offset, so it stays on its
    // color when the picker is resized without anything having to measure the element.
    [TestMethod]
    public void BitColorPickerShouldPlaceTheThumbAsAPercentage()
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.Color, "hsv(210,25%,75%)");
        });

        var style = com.Find(".bit-clp-thumb").GetAttribute("style");

        StringAssert.Contains(style, "inset-inline-start:25%");
        StringAssert.Contains(style, "top:25%");
    }

    [TestMethod]
    public void BitColorPickerShouldRespectClassesAndStyles()
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.ShowInputs, true);
            parameters.Add(p => p.ShowPreview, true);
            parameters.Add(p => p.ShowAlphaSlider, true);
            parameters.Add(p => p.Color, "#FF0000");
            parameters.Add(p => p.Presets, ["#FF0000", "#00FF00"]);
            parameters.Add(p => p.Classes, new BitColorPickerClassStyles
            {
                Root = "custom-root",
                SaturationPicker = "custom-area",
                SaturationThumb = "custom-thumb",
                Preview = "custom-preview",
                FieldInput = "custom-field",
                Preset = "custom-preset",
                SelectedPreset = "custom-preset-selected"
            });
            parameters.Add(p => p.Styles, new BitColorPickerClassStyles
            {
                Root = "z-index: 1;",
                SaturationPicker = "border-radius: 1rem;",
                Preview = "border-radius: 50%;",
                Preset = "border-radius: 50%;",
                SelectedPreset = "--bit-clp-prt-sel-clr: blue;"
            });
        });

        Assert.IsTrue(com.Find(".bit-clp").ClassList.Contains("custom-root"));
        StringAssert.Contains(com.Find(".bit-clp").GetAttribute("style"), "z-index: 1;");
        Assert.IsTrue(com.Find(".bit-clp-rec").ClassList.Contains("custom-area"));
        StringAssert.Contains(com.Find(".bit-clp-rec").GetAttribute("style"), "border-radius: 1rem;");
        Assert.IsTrue(com.Find(".bit-clp-thumb").ClassList.Contains("custom-thumb"));
        Assert.IsTrue(com.Find(".bit-clp-pre").ClassList.Contains("custom-preview"));
        StringAssert.Contains(com.Find(".bit-clp-pre").GetAttribute("style"), "border-radius: 50%;");
        Assert.IsTrue(com.Find(".bit-clp-fin").ClassList.Contains("custom-field"));

        var presets = com.FindAll(".bit-clp-prt");
        Assert.IsTrue(presets.All(p => p.ClassList.Contains("custom-preset")));
        StringAssert.Contains(presets[0].GetAttribute("style"), "border-radius: 50%;");
        StringAssert.Contains(presets[1].GetAttribute("style"), "border-radius: 50%;");

        // The selected swatch is the one the color is on, and it takes the selected part on top of the
        // one every swatch gets rather than instead of it.
        Assert.IsTrue(presets[0].ClassList.Contains("custom-preset-selected"));
        StringAssert.Contains(presets[0].GetAttribute("style"), "--bit-clp-prt-sel-clr: blue;");
        Assert.IsFalse(presets[1].ClassList.Contains("custom-preset-selected"));
        Assert.IsFalse(presets[1].GetAttribute("style")!.Contains("--bit-clp-prt-sel-clr"));
    }

    // The panel carries no text of its own, so it names itself with the color it is on - unless an
    // explicit AriaLabel says otherwise.
    [TestMethod]
    public void BitColorPickerShouldNameItselfWithItsColor()
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.Color, "rgb(1,2,3)");
        });

        Assert.AreEqual("Color picker, Red 1 Green 2 Blue 3 selected.", com.Find(".bit-clp").GetAttribute("aria-label"));

        com.Render(parameters =>
        {
            parameters.Add(p => p.ShowAlphaSlider, true);
            parameters.Add(p => p.Alpha, 0.5);
        });

        Assert.AreEqual("Color picker, Red 1 Green 2 Blue 3 and Alpha 50% selected.", com.Find(".bit-clp").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitColorPickerShouldRespectAriaLabel()
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, "Choose the brand color");
        });

        Assert.AreEqual("Choose the brand color", com.Find(".bit-clp").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitColorPickerShouldRespectDir()
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Rtl);
        });

        var root = com.Find(".bit-clp");

        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        Assert.AreEqual("rtl", root.GetAttribute("dir"));
    }

    [TestMethod,
        DataRow(BitVisibility.Visible, null),
        DataRow(BitVisibility.Hidden, "visibility:hidden"),
        DataRow(BitVisibility.Collapsed, "display:none")
    ]
    public void BitColorPickerShouldRespectVisibility(BitVisibility visibility, string? expectedStyle)
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.Visibility, visibility);
        });

        var style = com.Find(".bit-clp").GetAttribute("style") ?? string.Empty;

        if (expectedStyle is null)
        {
            Assert.IsFalse(style.Contains("visibility:hidden", StringComparison.Ordinal));
            Assert.IsFalse(style.Contains("display:none", StringComparison.Ordinal));
        }
        else
        {
            StringAssert.Contains(style, expectedStyle);
        }
    }

    [TestMethod,
        DataRow("data-value", "ID-123"),
        DataRow("aria-describedby", "hint")
    ]
    public void BitColorPickerShouldRespectArbitraryHtmlAttributes(string name, string value)
    {
        // Arbitrary HTML attributes are captured by BitComponentBase from unmatched parameters, so
        // supply them as raw component attributes (as real markup would) rather than via the builder,
        // which rejects unmatched params on components without [Parameter(CaptureUnmatchedValues)].
        var com = Context.Render(builder =>
        {
            builder.OpenComponent<BitColorPicker>(0);
            builder.AddAttribute(1, name, value);
            builder.CloseComponent();
        });

        Assert.AreEqual(value, com.Find(".bit-clp").GetAttribute(name));
    }

    // The color is re-read from the Color parameter whenever the consumer pushes a new one in, and only
    // then: the strings the picker writes back out must not be re-parsed as if they were new values.
    [TestMethod]
    public void BitColorPickerShouldFollowThePushedColor()
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.Color, "#FF0000");
        });

        Assert.AreEqual("#FF0000", com.Instance.Hex);

        com.Render(parameters => parameters.Add(p => p.Color, "#00FF00"));

        Assert.AreEqual("#00FF00", com.Instance.Hex);
        Assert.AreEqual(120, Math.Round(com.Instance.Hsv.Hue));
    }

    // A Format switched at runtime rewrites the value that is already bound, rather than leaving it in the
    // old notation until the user happens to touch the picker again.
    [TestMethod]
    public void BitColorPickerShouldRewriteTheBoundValueWhenTheFormatChanges()
    {
        var color = "#FF0000";

        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.Format, BitColorFormat.Hex);
            parameters.Bind(p => p.Color, color, v => color = v);
        });

        Assert.AreEqual("#FF0000", color);

        // bUnit re-renders with exactly the parameters given, so the binding is supplied again alongside
        // the new format rather than being dropped by the second render.
        com.Render(parameters =>
        {
            parameters.Add(p => p.Format, BitColorFormat.Hsl);
            parameters.Bind(p => p.Color, color, v => color = v);
        });

        Assert.AreEqual("hsl(0,100%,50%)", color);
        Assert.AreEqual("#FF0000", com.Instance.Hex);
    }

    // Rewriting the value in a new notation is not a change of color, so it stays out of the callbacks.
    [TestMethod]
    public void BitColorPickerShouldNotRaiseCallbacksWhenOnlyTheFormatChanges()
    {
        var changes = 0;
        var ends = 0;

        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.Format, BitColorFormat.Hex);
            parameters.Add(p => p.OnChange, () => changes++);
            parameters.Add(p => p.OnChangeEnd, () => ends++);
        });

        com.Render(parameters =>
        {
            parameters.Add(p => p.Format, BitColorFormat.Rgb);
            parameters.Add(p => p.OnChange, () => changes++);
            parameters.Add(p => p.OnChangeEnd, () => ends++);
        });

        Assert.AreEqual("rgb(255,255,255)", com.Instance.Color);
        Assert.AreEqual(0, changes);
        Assert.AreEqual(0, ends);
    }

    // A color string can carry its own alpha, which then also becomes the answer the Alpha parameter gives.
    [TestMethod]
    public void BitColorPickerShouldReportTheAlphaCarriedByTheColor()
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.Color, "rgba(255,0,0,0.4)");
        });

        Assert.AreEqual(0.4, com.Instance.Alpha);

        com.Render(parameters => parameters.Add(p => p.Color, "#00FF0080"));

        Assert.AreEqual(0.5, Math.Round(com.Instance.Alpha, 2));
    }

    [TestMethod]
    public void BitColorPickerShouldRebuildThePresetsWhenThePaletteChanges()
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.Presets, ["#FF0000", "#00FF00"]);
        });

        Assert.AreEqual(2, com.FindAll(".bit-clp-prt").Count);

        com.Render(parameters => parameters.Add(p => p.Presets, ["#0000FF"]));

        var presets = com.FindAll(".bit-clp-prt");

        Assert.AreEqual(1, presets.Count);
        Assert.AreEqual("#0000FF", presets[0].GetAttribute("aria-label"));

        com.Render(parameters => parameters.Add(p => p.Presets, (IEnumerable<string>?)null));

        Assert.AreEqual(0, com.FindAll(".bit-clp-prt").Count);
    }

    // A bare number on a slider is a position on an unnamed scale, so both tracks spell out the unit they
    // are actually in.
    [TestMethod]
    public void BitColorPickerSlidersShouldAnnounceTheirUnits()
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.ShowAlphaSlider, true);
            parameters.Add(p => p.Color, "hsva(210,50%,50%,0.25)");
        });

        var hue = com.Find(".bit-clp-hsd .bit-clp-inp");
        var alpha = com.Find(".bit-clp-asd .bit-clp-inp");

        Assert.AreEqual("Hue", hue.GetAttribute("aria-label"));
        Assert.AreEqual("Hue 210 degrees", hue.GetAttribute("aria-valuetext"));
        Assert.AreEqual("Alpha", alpha.GetAttribute("aria-label"));
        Assert.AreEqual("Alpha 25%", alpha.GetAttribute("aria-valuetext"));
    }

    [TestMethod]
    public void BitColorPickerShouldFollowThePushedAlpha()
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.Color, "#FF0000");
            parameters.Add(p => p.Alpha, 1d);
        });

        com.Render(parameters => parameters.Add(p => p.Alpha, 0.2));

        Assert.AreEqual(0.2, com.Instance.Alpha);
        Assert.AreEqual("rgba(255,0,0,0.2)", com.Instance.Rgba);
        Assert.AreEqual("#FF0000", com.Instance.Hex);
    }

    // Every number rendered into the markup is written in the invariant format the browser reads them
    // back in, whatever culture the application happens to be running under.
    [TestMethod]
    public void BitColorPickerShouldRenderInvariantNumbers()
    {
        var culture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            var com = RenderComponent<BitColorPicker>(parameters =>
            {
                parameters.Add(p => p.ShowAlphaSlider, true);
                parameters.Add(p => p.Color, "hsva(210.5,50%,50%,0.25)");
            });

            Assert.AreEqual("0.25", com.Find(".bit-clp-asd .bit-clp-inp").GetAttribute("value"));
            StringAssert.Contains(com.Find(".bit-clp-hsd .bit-clp-inp").GetAttribute("value"), "210.5");
            StringAssert.Contains(com.Find(".bit-clp-thumb").GetAttribute("style"), "inset-inline-start:50%");
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    // A page holding @bind-Color re-renders from inside the binding callback, which lands the picker back
    // in OnParametersSet before the drag handler that raised the callback has returned. The value arriving
    // there is the one the picker itself has just written, so it must not be re-read: parsing it would
    // re-derive the hue from the eight-bit RGB the notation carries, and that derivation is coarse enough
    // at low saturation to walk the hue - and with it the whole panel - away from the color under the
    // pointer. This is the "hue slider drifts while the gradient is dragged" bug.
    [TestMethod,
        DataRow("#4D7FB3"),
        DataRow("rgb(77,127,179)"),
        DataRow("rgba(77,127,179,1)"),
        // The notations that carry the hue themselves do not lose it to the eight-bit channels, but they
        // are written to two decimal places, so re-reading them quantizes the color just the same.
        DataRow("hsl(210.588235,50%,50%)"),
        DataRow("hsv(210.588235,50%,50%)")
    ]
    public async Task BitColorPickerShouldNotDriftTheHueWhileTheGradientIsDragged(string initial)
    {
        var com = RenderComponent<ColorPickerBindingHost>(parameters =>
        {
            parameters.Add(p => p.Color, initial);
        });

        var picker = com.Instance.Picker;
        var hue = picker.Hsv.Hue;

        // Through the dispatcher, which is where the browser's JS interop invokes it: a StateHasChanged
        // raised from there renders straight away rather than being queued, which is what puts the
        // re-render inside the callback the picker is still awaiting.
        await com.InvokeAsync(() => picker.HandlePointerMove(0.2, 0.7));
        await com.InvokeAsync(() => picker.HandlePointerMove(0.15, 0.75));
        await com.InvokeAsync(() => picker.HandlePointerMove(0.1, 0.8));

        Assert.AreEqual(hue, picker.Hsv.Hue);
        Assert.AreEqual(0.1, Math.Round(picker.Hsv.Saturation, 10));
        Assert.AreEqual(0.2, Math.Round(picker.Hsv.Value, 10));
    }

    // The same round trip on the sliders: a hue dragged to a degree has to stay on that degree, rather than
    // being pulled off it by the notation the bound value happens to be written in.
    [TestMethod]
    public void BitColorPickerShouldNotDriftTheHueWhileTheHueSliderIsDragged()
    {
        var com = RenderComponent<ColorPickerBindingHost>(parameters =>
        {
            parameters.Add(p => p.Color, "#4D7FB3");
        });

        com.Find(".bit-clp-hsd .bit-clp-inp").Input("275");

        Assert.AreEqual(275, com.Instance.Picker.Hsv.Hue);
    }

    // Ignoring the value that comes back through a binding must not extend to ignoring the consumer: one
    // that answers a change with a different color - clamping it to a palette, refusing it, rounding it -
    // has pushed a color in, and the picker follows it like any other push even though it arrived in the
    // middle of the publish that provoked it.
    [TestMethod]
    public async Task BitColorPickerShouldFollowAColorTheBindingRewrites()
    {
        var com = RenderComponent<ColorPickerBindingHost>(parameters =>
        {
            parameters.Add(p => p.Color, "#4D7FB3");
            parameters.Add(p => p.Rewrite, _ => "#00FF00");
        });

        var picker = com.Instance.Picker;

        await com.InvokeAsync(() => picker.HandlePointerMove(0.2, 0.7));

        Assert.AreEqual("#00FF00", com.Instance.Color);
        Assert.AreEqual("#00FF00", picker.Hex);
        Assert.AreEqual(120, picker.Hsv.Hue);
    }
}
