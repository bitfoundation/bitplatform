using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
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
        DataRow(BitColorFormat.Hsva, "hsva(0,100%,100%,0.5)"),
        DataRow(BitColorFormat.Hwb, "hwb(0 0% 0%)"),
        DataRow(BitColorFormat.Hwba, "hwb(0 0% 0% / 0.5)")
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
        Context.JSInterop.Setup<bool>("BitBlazorUI.ColorPicker.isEyeDropperSupported").SetResult(true);

        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.Color, "#FFFFFF");
            parameters.Add(p => p.ShowAlphaSlider, true);
            parameters.Add(p => p.ShowInputs, true);
            parameters.Add(p => p.ShowEyeDropper, true);
            parameters.Add(p => p.Presets, ["#FF0000"]);
        });

        com.Instance.HandlePointerMove(1, 0);

        Assert.AreEqual("#FFFFFF", com.Instance.Color);
        Assert.AreEqual("#FFFFFF", com.Instance.Hex);

        // Every control refuses the gesture at the element rather than in its handler: a native range moves
        // its own thumb, and a text field keeps whatever was typed into it, before any handler is reached,
        // so a picker that is not going to accept the change would otherwise be left showing one it is not on.
        Assert.IsTrue(com.Find(".bit-clp-hsd .bit-clp-inp").HasAttribute("disabled"));
        Assert.IsTrue(com.Find(".bit-clp-asd .bit-clp-inp").HasAttribute("disabled"));
        Assert.IsTrue(com.FindAll(".bit-clp-fin").All(f => f.HasAttribute("disabled")));
        Assert.IsTrue(com.Find(".bit-clp-prt").HasAttribute("disabled"));
        Assert.IsTrue(com.Find(".bit-clp-eyd").HasAttribute("disabled"));
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

        // The args carry the color in every notation the picker knows, so a handler that needs one of them
        // does not have to convert another back.
        Assert.AreEqual(0, Math.Round(args.Hwb.Whiteness, 4));
        Assert.AreEqual(0, Math.Round(args.Hwb.Blackness, 4));
        Assert.AreEqual(0.628, Math.Round(args.Oklch.Lightness, 3), 0.001);
        Assert.AreEqual("vibrant red", args.ColorDescription);
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
        // The color is named as well as measured: a screen reader user hears which color the area has landed
        // on rather than a pair of coordinates they have to picture.
        Assert.AreEqual("vibrant red, Saturation 100%, Brightness 100%, #FF0000", area.GetAttribute("aria-valuetext"));
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

        // aria-readonly is not a state a "group" carries - it belongs to the widgets inside it, which is
        // where the saturation slider and the native ranges declare it.
        Assert.IsNull(root.GetAttribute("aria-readonly"));

        // It keeps its place in the tab order either way: a read-only picker is still showing a color, and
        // the saturation area is what announces it.
        Assert.AreEqual("0", com.Find(".bit-clp-rec").GetAttribute("tabindex"));
        Assert.AreEqual(readOnly ? "true" : null, com.Find(".bit-clp-rec").GetAttribute("aria-readonly"));

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

    // The text fields can be written in any of the models a color is thought of in, and which one they are
    // in says nothing about the notation the picker publishes.
    [TestMethod,
        DataRow(BitColorInputsMode.HexRgb, true, "R", "G", "B"),
        DataRow(BitColorInputsMode.Hex, true, null, null, null),
        DataRow(BitColorInputsMode.Rgb, false, "R", "G", "B"),
        DataRow(BitColorInputsMode.Hsl, false, "H", "S", "L"),
        DataRow(BitColorInputsMode.Hsv, false, "H", "S", "V")
    ]
    public void BitColorPickerShouldRespectTheInputsMode(BitColorInputsMode mode, bool hasHex, string? first, string? second, string? third)
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.ShowInputs, true);
            parameters.Add(p => p.InputsMode, mode);
            parameters.Add(p => p.Color, "#4D7FB3");
        });

        Assert.AreEqual(hasHex, com.FindAll(".bit-clp-fhx").Count == 1);

        var labels = com.FindAll(".bit-clp-flb").Select(l => l.TextContent).ToList();

        if (hasHex)
        {
            Assert.AreEqual("Hex", labels[0]);
            labels.RemoveAt(0);
        }

        if (first is null)
        {
            Assert.AreEqual(0, labels.Count);
        }
        else
        {
            CollectionAssert.AreEqual(new[] { first, second, third }, labels);
        }
    }

    // #4D7FB3 is hsl(210, 40%, 50%), and the fields carry it to the degree and to the percentage point.
    [TestMethod]
    public void BitColorPickerShouldFillTheHslFields()
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.ShowInputs, true);
            parameters.Add(p => p.InputsMode, BitColorInputsMode.Hsl);
            parameters.Add(p => p.Color, "#4D7FB3");
        });

        var fields = com.FindAll(".bit-clp-fin");

        Assert.AreEqual("211", fields[0].GetAttribute("value"));
        Assert.AreEqual("40", fields[1].GetAttribute("value"));
        Assert.AreEqual("50", fields[2].GetAttribute("value"));
        Assert.AreEqual("360", fields[0].GetAttribute("max"));
        Assert.AreEqual("100", fields[1].GetAttribute("max"));
    }

    [TestMethod,
        DataRow(0, "120", "#4DB34D"),
        DataRow(1, "100", "#017EFF"),
        DataRow(2, "25", "#263F59")
    ]
    public void BitColorPickerShouldCommitTheHslFields(int index, string typed, string expected)
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.ShowInputs, true);
            parameters.Add(p => p.InputsMode, BitColorInputsMode.Hsl);
            parameters.Add(p => p.Color, "#4D7FB3");
            parameters.Add(p => p.OnChange, () => { });
        });

        com.FindAll(".bit-clp-fin")[index].Change(typed);

        Assert.AreEqual(expected, com.Instance.Hex);
    }

    [TestMethod,
        DataRow(0, "120", "#4DB34D"),
        DataRow(1, "100", "#0058B3"),
        DataRow(2, "100", "#6EB5FF")
    ]
    public void BitColorPickerShouldCommitTheHsvFields(int index, string typed, string expected)
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.ShowInputs, true);
            parameters.Add(p => p.InputsMode, BitColorInputsMode.Hsv);
            parameters.Add(p => p.Color, "#4D7FB3");
            parameters.Add(p => p.OnChange, () => { });
        });

        com.FindAll(".bit-clp-fin")[index].Change(typed);

        Assert.AreEqual(expected, com.Instance.Hex);
    }

    // Editing one channel takes the other two from the color itself rather than from the whole numbers
    // standing in their fields, so a hue nudged by a degree does not also round the saturation.
    [TestMethod]
    public void BitColorPickerHslFieldsShouldNotRoundTheChannelsTheyDoNotTouch()
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.ShowInputs, true);
            parameters.Add(p => p.InputsMode, BitColorInputsMode.Hsl);
            parameters.Add(p => p.Color, "hsl(210.5,40.4%,50.6%)");
            parameters.Add(p => p.OnChange, () => { });
        });

        com.FindAll(".bit-clp-fin")[0].Change("120");

        var (hue, saturation, lightness) = com.Instance.Hsl;

        Assert.AreEqual(120, Math.Round(hue, 4));
        Assert.AreEqual(0.404, Math.Round(saturation, 3));
        Assert.AreEqual(0.506, Math.Round(lightness, 3));
    }

    // The switch is what lets the user type the color in the model they are thinking in rather than the one
    // the page picked for them, and it reports where it has landed.
    [TestMethod]
    public void BitColorPickerShouldCycleTheInputsMode()
    {
        var mode = BitColorInputsMode.HexRgb;

        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.ShowInputs, true);
            parameters.Add(p => p.ShowInputsModeSwitch, true);
            parameters.Bind(p => p.InputsMode, mode, v => mode = v);
        });

        BitColorInputsMode[] expected =
        [
            BitColorInputsMode.Hex,
            BitColorInputsMode.Rgb,
            BitColorInputsMode.Hsl,
            BitColorInputsMode.Hsv,
            BitColorInputsMode.HexRgb
        ];

        foreach (var step in expected)
        {
            com.Find(".bit-clp-swb").Click();

            Assert.AreEqual(step, mode);
            Assert.AreEqual(step, com.Instance.InputsMode);
        }
    }

    [TestMethod]
    public void BitColorPickerShouldNotRenderTheInputsModeSwitchWithoutInputs()
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.ShowInputsModeSwitch, true);
        });

        Assert.AreEqual(0, com.FindAll(".bit-clp-swb").Count);
    }

    // Moving the fields to another set of channels is not a change to the color, so a read-only picker has
    // nothing to refuse about it; a disabled one refuses everything.
    [TestMethod,
        DataRow(true, false, false),
        DataRow(false, true, false),
        DataRow(false, false, true)
    ]
    public void BitColorPickerInputsModeSwitchShouldFollowTheEnabledStateOnly(bool readOnly, bool disabled, bool oneWay)
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.ShowInputs, true);
            parameters.Add(p => p.ShowInputsModeSwitch, true);
            parameters.Add(p => p.ReadOnly, readOnly);
            parameters.Add(p => p.IsEnabled, disabled is false);

            if (oneWay)
            {
                parameters.Add(p => p.Color, "#FF0000");
            }
        });

        var button = com.Find(".bit-clp-swb");

        Assert.AreEqual(disabled, button.HasAttribute("disabled"));

        if (disabled) return;

        button.Click();

        Assert.AreEqual(BitColorInputsMode.Hex, com.Instance.InputsMode);
    }

    [TestMethod]
    public void BitColorPickerShouldRespectInputsModeSwitchIcons()
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.ShowInputs, true);
            parameters.Add(p => p.ShowInputsModeSwitch, true);
        });

        Assert.IsTrue(com.Find(".bit-clp-swi").ClassList.Contains("bit-icon--Sort"));

        com.Render(parameters => parameters.Add(p => p.InputsModeSwitchIconName, "Color"));
        Assert.IsTrue(com.Find(".bit-clp-swi").ClassList.Contains("bit-icon--Color"));

        com.Render(parameters => parameters.Add(p => p.InputsModeSwitchIcon, BitIconInfo.Fa("solid sort")));
        Assert.IsTrue(com.Find(".bit-clp-swi").ClassList.Contains("fa-sort"));
    }

    // A field left holding something the picker did not take is written back to directly rather than being
    // replaced, so correcting a typo does not also cost the user their place in the panel.
    [TestMethod,
        DataRow("nonsense", "#FFFFFF"),
        DataRow("#0f0", "#00FF00")
    ]
    public void BitColorPickerShouldWriteTheCommittedValueBackIntoTheField(string typed, string committed)
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.ShowInputs, true);
            parameters.Add(p => p.OnChange, () => { });
        });

        com.Find(".bit-clp-fhx .bit-clp-fin").Change(typed);

        var calls = Context.JSInterop.Invocations["BitBlazorUI.Utils.setProperty"];

        Assert.AreEqual(1, calls.Count);
        Assert.AreEqual("value", calls[0].Arguments[1]);
        Assert.AreEqual(committed, calls[0].Arguments[2]);
    }

    // A value the picker took exactly as it was typed is already in the field, so nothing is written back.
    [TestMethod]
    public void BitColorPickerShouldLeaveAFieldItAgreesWithAlone()
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.ShowInputs, true);
            parameters.Add(p => p.OnChange, () => { });
        });

        com.Find(".bit-clp-fhx .bit-clp-fin").Change("#00FF00");

        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.setProperty"].Count);
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
        // Named as well as spelled out, since a hexadecimal string read aloud digit by digit says nothing
        // about the color it stands for.
        Assert.AreEqual("vibrant red, #FF0000", presets[0].GetAttribute("aria-label"));
        Assert.AreEqual("#FF0000", presets[0].GetAttribute("title"));
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

        // And it reads as the swatch the picker is on, since the alpha it kept is not one the swatch named.
        Assert.AreEqual("true", com.Find(".bit-clp-prt").GetAttribute("aria-pressed"));
        Assert.IsTrue(com.Find(".bit-clp-prt").ClassList.Contains("bit-clp-prt-sel"));
    }

    // A swatch that does name an alpha of its own is only the one the picker is on when that alpha matches.
    [TestMethod]
    public void BitColorPickerShouldCompareTheAlphaOfAPresetThatCarriesOne()
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.ShowAlphaSlider, true);
            parameters.Add(p => p.Presets, ["rgba(255,0,0,0.5)", "#FF0000"]);
            parameters.Add(p => p.Color, "rgba(255,0,0,0.3)");
        });

        var presets = com.FindAll(".bit-clp-prt");

        Assert.AreEqual("false", presets[0].GetAttribute("aria-pressed"));
        Assert.AreEqual("true", presets[1].GetAttribute("aria-pressed"));

        com.Render(parameters => parameters.Add(p => p.Color, "rgba(255,0,0,0.5)"));

        Assert.AreEqual("true", com.FindAll(".bit-clp-prt")[0].GetAttribute("aria-pressed"));
    }

    // A palette given a row length is laid out on a grid rather than left to wrap, so a column of the
    // palette stays a column on the screen however wide the panel happens to be.
    [TestMethod]
    public void BitColorPickerShouldRespectPresetsPerRow()
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.Presets, ["#FF0000", "#00FF00", "#0000FF", "#FFFF00"]);
        });

        Assert.IsFalse(com.Find(".bit-clp-prs").ClassList.Contains("bit-clp-prg"));

        com.Render(parameters => parameters.Add(p => p.PresetsPerRow, 2));

        var presets = com.Find(".bit-clp-prs");

        Assert.IsTrue(presets.ClassList.Contains("bit-clp-prg"));
        StringAssert.Contains(presets.GetAttribute("style"), "--bit-clp-prs-cols:2");
    }

    // A palette is a set of alternatives, not a queue of controls: one Tab reaches it and the arrow keys
    // move within it, so a thirty-color palette does not put thirty tab stops between the picker and
    // whatever comes after it on the page.
    [TestMethod]
    public void BitColorPickerPresetsShouldBeASingleTabStop()
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.Presets, ["#FF0000", "#00FF00", "#0000FF"]);
            parameters.Add(p => p.Color, "#00FF00");
            parameters.Add(p => p.OnChange, () => { });
        });

        Assert.AreEqual("toolbar", com.Find(".bit-clp-prs").GetAttribute("role"));

        var presets = com.FindAll(".bit-clp-prt");

        // Entering the palette lands on the color the picker is already on.
        CollectionAssert.AreEqual(new[] { "-1", "0", "-1" }, presets.Select(p => p.GetAttribute("tabindex")).ToArray());

        // With nothing of the palette picked, the first swatch is the way in.
        com.Render(parameters => parameters.Add(p => p.Color, "#123456"));

        CollectionAssert.AreEqual(new[] { "0", "-1", "-1" }, com.FindAll(".bit-clp-prt").Select(p => p.GetAttribute("tabindex")).ToArray());
    }

    // Only the focus moves - a swatch is picked by pressing it, the same way it is with the pointer - so
    // arrowing across a palette does not fire a change for every color it passes over.
    [TestMethod,
        DataRow("ArrowRight", 5),
        DataRow("ArrowLeft", 3),
        DataRow("Home", 0),
        DataRow("End", 5),
        DataRow("ArrowDown", 5),
        DataRow("ArrowUp", 1)
    ]
    public void BitColorPickerShouldWalkThePresetsWithTheArrowKeys(string key, int expected)
    {
        var changes = 0;

        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.Presets, ["#FF0000", "#00FF00", "#0000FF", "#FFFF00", "#00FFFF", "#FF00FF"]);
            parameters.Add(p => p.PresetsPerRow, 3);
            parameters.Add(p => p.OnChange, () => changes++);
        });

        com.FindAll(".bit-clp-prt")[4].KeyDown(key);

        Assert.AreEqual(0, changes);
        Assert.AreEqual("0", com.FindAll(".bit-clp-prt")[expected].GetAttribute("tabindex"));
    }

    // Clamped rather than wrapped: a palette is read as a picture of the colors on offer, and a focus that
    // jumps from one end to the other reads as having lost its place.
    [TestMethod,
        DataRow(0, "ArrowLeft", 0),
        DataRow(2, "ArrowRight", 2),
        DataRow(0, "ArrowUp", 0),
        DataRow(2, "ArrowDown", 2)
    ]
    public void BitColorPickerPresetNavigationShouldClampAtTheEnds(int from, string key, int expected)
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.Presets, ["#FF0000", "#00FF00", "#0000FF"]);
        });

        com.FindAll(".bit-clp-prt")[from].KeyDown(key);

        Assert.AreEqual("0", com.FindAll(".bit-clp-prt")[expected].GetAttribute("tabindex"));
    }

    // The horizontal arrows follow the reading direction, so they swap in right-to-left.
    [TestMethod,
        DataRow("ArrowRight", 0),
        DataRow("ArrowLeft", 2)
    ]
    public void BitColorPickerShouldMirrorThePresetArrowKeysInRtl(string key, int expected)
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Rtl);
            parameters.Add(p => p.Presets, ["#FF0000", "#00FF00", "#0000FF"]);
        });

        com.FindAll(".bit-clp-prt")[1].KeyDown(key);

        Assert.AreEqual("0", com.FindAll(".bit-clp-prt")[expected].GetAttribute("tabindex"));
    }

    // The navigation keys all scroll the page by default, which would carry the palette out from under the
    // user while they are moving through it, so they are suppressed on the container.
    [TestMethod]
    public void BitColorPickerShouldSuppressTheScrollingOfThePresetNavigationKeys()
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.Presets, ["#FF0000", "#00FF00"]);
        });

        var calls = Context.JSInterop.Invocations["BitBlazorUI.Utils.registerPreventKeys"];

        Assert.AreEqual(1, calls.Count);
        CollectionAssert.AreEqual(new[] { "ArrowUp", "ArrowDown", "ArrowLeft", "ArrowRight", "Home", "End" },
                                  (string[])calls[0].Arguments[1]!);
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

        Assert.AreEqual("Color picker, black, Red 1 Green 2 Blue 3 selected.", com.Find(".bit-clp").GetAttribute("aria-label"));

        com.Render(parameters =>
        {
            parameters.Add(p => p.ShowAlphaSlider, true);
            parameters.Add(p => p.Alpha, 0.5);
        });

        Assert.AreEqual("Color picker, black, Red 1 Green 2 Blue 3 and Alpha 50% selected.", com.Find(".bit-clp").GetAttribute("aria-label"));
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

    // The label is not a label element: with no single input to point a "for" at, one would label nothing,
    // so the panel is named through aria-labelledby referencing it instead.
    [TestMethod]
    public void BitColorPickerShouldRespectLabel()
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.Label, "Brand color");
        });

        var root = com.Find(".bit-clp");
        var label = com.Find(".bit-clp-lbl");

        Assert.AreEqual("Brand color", label.TextContent);
        Assert.AreEqual(root.GetAttribute("aria-labelledby"), com.Find(".bit-clp-lbc").Id);
        Assert.IsNull(root.GetAttribute("aria-label"));

        // An explicit AriaLabel still wins, since it is the more specific answer of the two.
        com.Render(parameters => parameters.Add(p => p.AriaLabel, "Choose the brand color"));

        Assert.IsNull(com.Find(".bit-clp").GetAttribute("aria-labelledby"));
        Assert.AreEqual("Choose the brand color", com.Find(".bit-clp").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitColorPickerShouldRespectLabelTemplate()
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.Label, "Brand color");
            parameters.Add(p => p.LabelTemplate, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "custom-label");
                builder.AddContent(2, "Pick one");
                builder.CloseElement();
            }));
        });

        Assert.AreEqual(0, com.FindAll(".bit-clp-lbl").Count);
        Assert.AreEqual("Pick one", com.Find(".custom-label").TextContent);
        Assert.AreEqual(com.Find(".bit-clp").GetAttribute("aria-labelledby"), com.Find(".bit-clp-lbc").Id);
    }

    [TestMethod]
    public void BitColorPickerShouldNotRenderALabelContainerWithoutOne()
    {
        var com = RenderComponent<BitColorPicker>();

        Assert.AreEqual(0, com.FindAll(".bit-clp-lbc").Count);
        Assert.IsNull(com.Find(".bit-clp").GetAttribute("aria-labelledby"));
    }

    // The contrast readout answers the question a color is usually being picked to settle - "can this be
    // read?" - at the moment it is being picked.
    [TestMethod,
        DataRow("#000000", "#FFFFFF", "21:1", true, true),
        DataRow("#FFFFFF", "#FFFFFF", "1:1", false, false),
        DataRow("#767676", "#FFFFFF", "4.54:1", true, true),
        DataRow("#949494", "#FFFFFF", "3.03:1", false, true)
    ]
    public void BitColorPickerShouldReportTheContrast(string color, string background, string ratio, bool aa, bool aaLarge)
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.ShowContrast, true);
            parameters.Add(p => p.Color, color);
            parameters.Add(p => p.ContrastColor, background);
        });

        Assert.AreEqual(ratio, com.Find(".bit-clp-cnv").TextContent.Trim());

        var badges = com.FindAll(".bit-clp-cnb");

        Assert.AreEqual(aa, badges[0].ClassList.Contains("bit-clp-cnp"));
        Assert.AreEqual(aaLarge, badges[1].ClassList.Contains("bit-clp-cnp"));
        Assert.AreEqual(aa is false, badges[0].ClassList.Contains("bit-clp-cnf"));
    }

    // A semi-transparent color is composited onto the background before the ratio is taken, since a ratio
    // against a color the eye never sees would be a reassuring answer to the wrong question.
    [TestMethod]
    public void BitColorPickerShouldCompositeTheAlphaIntoTheContrast()
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.ShowContrast, true);
            parameters.Add(p => p.ShowAlphaSlider, true);
            parameters.Add(p => p.Color, "rgba(0,0,0,1)");
            parameters.Add(p => p.ContrastColor, "#FFFFFF");
        });

        Assert.AreEqual("21:1", com.Find(".bit-clp-cnv").TextContent.Trim());

        // Half-transparent black over white is mid grey, which no longer clears the bar for body text.
        com.Render(parameters => parameters.Add(p => p.Color, "rgba(0,0,0,0.5)"));

        Assert.AreEqual("3.95:1", com.Find(".bit-clp-cnv").TextContent.Trim());
        Assert.IsTrue(com.FindAll(".bit-clp-cnb")[0].ClassList.Contains("bit-clp-cnf"));
    }

    // Left unset the readout measures against white, which is the background of most pages it will be
    // dropped into, and it accepts any of the notations the Color parameter does.
    [TestMethod]
    public void BitColorPickerContrastShouldDefaultToWhite()
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.ShowContrast, true);
            parameters.Add(p => p.Color, "#000000");
        });

        Assert.AreEqual("21:1", com.Find(".bit-clp-cnv").TextContent.Trim());

        com.Render(parameters => parameters.Add(p => p.ContrastColor, "black"));

        Assert.AreEqual("1:1", com.Find(".bit-clp-cnv").TextContent.Trim());
    }

    [TestMethod]
    public void BitColorPickerShouldNotRenderTheContrastWhenNotAskedFor()
    {
        var com = RenderComponent<BitColorPicker>();

        Assert.AreEqual(0, com.FindAll(".bit-clp-cnr").Count);
    }

    [TestMethod,
        DataRow(true, true),
        DataRow(true, false),
        DataRow(false, true)
    ]
    public void BitColorPickerShouldRespectAutoFocus(bool autoFocus, bool isEnabled)
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.AutoFocus, autoFocus);
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        // bUnit has no focus of its own, so the request is asserted where it is made: the focus call the
        // renderer sends for the element reference.
        var focused = Context.JSInterop.Invocations
                                       .Any(i => i.Identifier.Contains("focus", StringComparison.OrdinalIgnoreCase));

        Assert.AreEqual(autoFocus && isEnabled, focused);
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
        Assert.AreEqual("#0000FF", presets[0].GetAttribute("title"));

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
        var defaultCulture = CultureInfo.DefaultThreadCurrentCulture;

        try
        {
            var german = new CultureInfo("de-DE");

            // Both, because the renderer does its work on a dispatcher thread of its own: the current
            // culture covers this thread, and the default thread culture is what the other one picks up.
            CultureInfo.CurrentCulture = german;
            CultureInfo.DefaultThreadCurrentCulture = german;

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
            CultureInfo.DefaultThreadCurrentCulture = defaultCulture;
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
            parameters.Add(p => p.InitialColor, initial);
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
            parameters.Add(p => p.InitialColor, "#4D7FB3");
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
            parameters.Add(p => p.InitialColor, "#4D7FB3");
            parameters.Add(p => p.Rewrite, _ => "#00FF00");
        });

        var picker = com.Instance.Picker;

        await com.InvokeAsync(() => picker.HandlePointerMove(0.2, 0.7));

        Assert.AreEqual("#00FF00", com.Instance.Color);
        Assert.AreEqual("#00FF00", picker.Hex);
        Assert.AreEqual(120, picker.Hsv.Hue);
    }
}
