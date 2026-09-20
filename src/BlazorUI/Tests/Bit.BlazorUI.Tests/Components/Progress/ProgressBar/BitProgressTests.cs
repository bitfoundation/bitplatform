using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Progress.Progress;

[TestClass]
public class BitProgressTests : BunitTestContext
{
    [TestMethod,
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
        Assert.IsTrue(piWrapperStyle?.Contains(expectedValue));
    }

    [TestMethod]
    public void BitProgressWithoutThicknessShouldReadTheThicknessToken()
    {
        var component = RenderComponent<BitProgress>();

        Assert.Contains("var(--bit-prb-thickness)", component.Find(".bit-prb-bcn").GetAttribute("style")!);
        Assert.Contains("var(--bit-prb-thickness)", component.Find(".bit-prb-trc").GetAttribute("style")!);
        Assert.Contains("var(--bit-prb-thickness)", component.Find(".bit-prb-bar").GetAttribute("style")!);
    }

    [TestMethod,
        DataRow(-5),
        DataRow(-100)
    ]
    public void BitProgressThicknessCanNotBeNegative(int thickness)
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Thickness, thickness);
        });

        Assert.Contains("height: 0px", component.Find(".bit-prb-bar").GetAttribute("style")!);
    }

    [TestMethod,
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
        Assert.IsTrue(piBarStyle?.Contains(expectedValue));
    }

    [TestMethod,
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
        Assert.IsTrue(piBarStyle?.Contains(expectedValue));
    }

    [TestMethod,
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
        Assert.IsTrue(piBarStyle?.Contains(expectedValue));
    }


    [TestMethod,
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

    [TestMethod,
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
            Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-prb-lbl"));
            Assert.IsNull(piBar.GetAttribute("aria-labelledby"));
        }
        else
        {
            var piLabel = component.Find(".bit-prb-lbl");
            Assert.AreEqual(label, piLabel.TextContent);
            Assert.IsNotNull(piBar.GetAttribute("aria-labelledby"));
            Assert.AreEqual(piLabel.Id, piBar.GetAttribute("aria-labelledby"));
        }
    }

    [TestMethod,
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
            Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-prb-des"));
            Assert.IsNull(piBar.GetAttribute("aria-describedby"));
        }
        else
        {
            var piDescription = component.Find(".bit-prb-des");
            Assert.AreEqual(description, piDescription.TextContent);
            Assert.IsNotNull(piBar.GetAttribute("aria-describedby"));
            Assert.AreEqual(piDescription.Id, piBar.GetAttribute("aria-describedby"));
        }
    }

    [TestMethod,
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

    [TestMethod,
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
            Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-prb-pct"));
        }
    }

    [TestMethod,
        DataRow("<h1>this is a custom label</h1>")
    ]
    public void BitProgressLabelTemplateTest(string labelTemplate)
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.LabelTemplate, labelTemplate);
        });

        var labelChildNodes = component?.Find(".bit-prb-lbl")?.ChildNodes;
        labelChildNodes?.MarkupMatches(labelTemplate);
    }

    [TestMethod,
        DataRow("<h1>this is a custom description</h1>"),
    ]
    public void BitProgressDescriptionTemplateTest(string descriptionTemplate)
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.DescriptionTemplate, descriptionTemplate);
        });

        var descriptionChildNodes = component?.Find(".bit-prb")?.LastChild?.ChildNodes;
        descriptionChildNodes?.MarkupMatches(descriptionTemplate);
    }

    [TestMethod]
    public void BitProgressLabelTemplateShouldCarryTheLabelSlotHooks()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.LabelTemplate, "<span>custom</span>");
            parameters.Add(p => p.Classes, new BitProgressClassStyles { Label = "custom-label" });
        });

        var label = component.Find(".bit-prb-lbl");
        Assert.IsTrue(label.ClassList.Contains("custom-label"));
        Assert.AreEqual(label.Id, component.Find(".bit-prb-bar").GetAttribute("aria-labelledby"));
    }

    [TestMethod]
    public void BitProgressDescriptionTemplateShouldCarryTheDescriptionSlotHooks()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.DescriptionTemplate, "<span>custom</span>");
            parameters.Add(p => p.Classes, new BitProgressClassStyles { Description = "custom-description" });
        });

        var description = component.Find(".bit-prb-des");
        Assert.IsTrue(description.ClassList.Contains("custom-description"));
        Assert.AreEqual(description.Id, component.Find(".bit-prb-bar").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitProgressLabelShouldNotBeALabelElement()
    {
        // A <label> that is "for" nothing is not a label at all - the bar is not a form control.
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Label, "Label");
        });

        Assert.AreEqual("DIV", component.Find(".bit-prb-lbl").TagName);
    }


    // ---------------------------------------------------------------- accessibility

    [TestMethod]
    public void BitProgressShouldRenderTheProgressbarRole()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Percent, 42);
        });

        var bar = component.Find(".bit-prb-bar");
        Assert.AreEqual("progressbar", bar.GetAttribute("role"));
        Assert.AreEqual("0", bar.GetAttribute("aria-valuemin"));
        Assert.AreEqual("100", bar.GetAttribute("aria-valuemax"));
        Assert.AreEqual("42", bar.GetAttribute("aria-valuenow"));
    }

    [TestMethod]
    public void BitProgressIndeterminateShouldNotReportAValue()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Indeterminate, true);
            parameters.Add(p => p.Percent, 42);
        });

        var bar = component.Find(".bit-prb-bar");
        Assert.AreEqual("progressbar", bar.GetAttribute("role"));
        Assert.IsNull(bar.GetAttribute("aria-valuenow"));
        Assert.IsNull(bar.GetAttribute("aria-valuemin"));
        Assert.IsNull(bar.GetAttribute("aria-valuemax"));
    }

    [TestMethod]
    public void BitProgressAriaLabelShouldNameTheBar()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, "Loading");
        });

        var bar = component.Find(".bit-prb-bar");
        Assert.AreEqual("Loading", bar.GetAttribute("aria-label"));
        Assert.IsNull(bar.GetAttribute("aria-labelledby"));
    }

    [TestMethod]
    public void BitProgressAriaLabelShouldWinOverTheVisibleLabel()
    {
        // aria-labelledby outranks aria-label in the accessible name computation, so an explicit
        // AriaLabel would be dead markup if the label reference were rendered beside it.
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Label, "Label");
            parameters.Add(p => p.AriaLabel, "Loading");
        });

        var bar = component.Find(".bit-prb-bar");
        Assert.AreEqual("Loading", bar.GetAttribute("aria-label"));
        Assert.IsNull(bar.GetAttribute("aria-labelledby"));
    }

    [TestMethod]
    public void BitProgressCircularShouldExposeTheProgressbarRole()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Circular, true);
            parameters.Add(p => p.Label, "Label");
            parameters.Add(p => p.Description, "Description");
            parameters.Add(p => p.AriaValueText, "42 percent");
            parameters.Add(p => p.Percent, 42);
        });

        var svg = component.Find(".bit-prb-cir");
        Assert.AreEqual("progressbar", svg.GetAttribute("role"));
        Assert.AreEqual("42", svg.GetAttribute("aria-valuenow"));
        Assert.AreEqual("0", svg.GetAttribute("aria-valuemin"));
        Assert.AreEqual("100", svg.GetAttribute("aria-valuemax"));
        Assert.AreEqual("42 percent", svg.GetAttribute("aria-valuetext"));
        Assert.AreEqual(component.Find(".bit-prb-lbl").Id, svg.GetAttribute("aria-labelledby"));
        Assert.AreEqual(component.Find(".bit-prb-des").Id, svg.GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitProgressCircularIndeterminateShouldNotReportAValue()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Circular, true);
            parameters.Add(p => p.Indeterminate, true);
        });

        var svg = component.Find(".bit-prb-cir");
        Assert.AreEqual("progressbar", svg.GetAttribute("role"));
        Assert.IsNull(svg.GetAttribute("aria-valuenow"));
    }

    [TestMethod]
    public void BitProgressMeterShouldReportAMeasurementRatherThanProgress()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Meter, true);
            parameters.Add(p => p.Value, 60);
            parameters.Add(p => p.Max, 100);
        });

        var bar = component.Find(".bit-prb-bar");
        Assert.AreEqual("meter", bar.GetAttribute("role"));
        Assert.AreEqual("60", bar.GetAttribute("aria-valuenow"));
        Assert.AreEqual("0", bar.GetAttribute("aria-valuemin"));
        Assert.AreEqual("100", bar.GetAttribute("aria-valuemax"));
    }

    [TestMethod]
    public void BitProgressCircularMeterShouldReportAMeasurementRatherThanProgress()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Meter, true);
            parameters.Add(p => p.Circular, true);
            parameters.Add(p => p.GapDegree, 120);
            parameters.Add(p => p.Percent, 42);
        });

        Assert.AreEqual("meter", component.Find(".bit-prb-cir").GetAttribute("role"));
    }

    [TestMethod]
    public void BitProgressAnIndeterminateMeterShouldStayAProgressbar()
    {
        // A meter always has a value, so there is no such thing as an indeterminate one.
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Meter, true);
            parameters.Add(p => p.Indeterminate, true);
        });

        Assert.AreEqual("progressbar", component.Find(".bit-prb-bar").GetAttribute("role"));
    }

    [TestMethod]
    public void BitProgressTheRoleShouldFollowAParameterChange()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Percent, 42);
        });

        Assert.AreEqual("progressbar", component.Find(".bit-prb-bar").GetAttribute("role"));

        component.Render(parameters => parameters.Add(p => p.Meter, true));

        Assert.AreEqual("meter", component.Find(".bit-prb-bar").GetAttribute("role"));
    }


    // ---------------------------------------------------------------- value, min & max

    [TestMethod,
        DataRow(3, 0, 10, "30"),
        DataRow(5, 0, 10, "50"),
        DataRow(32, 20, 40, "60"),
        DataRow(0, 0, 10, "0"),
        DataRow(10, 0, 10, "100")
    ]
    public void BitProgressValueShouldFillTheBarFromTheRange(double value, double min, double max, string expected)
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Value, value);
            parameters.Add(p => p.Min, min);
            parameters.Add(p => p.Max, max);
        });

        Assert.Contains($"width: {expected}%", component.Find(".bit-prb-bar").GetAttribute("style")!);
    }

    [TestMethod,
        DataRow(-4, 0, 10, "0"),
        DataRow(40, 0, 10, "100")
    ]
    public void BitProgressValueShouldBeClampedToTheRange(double value, double min, double max, string expected)
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Value, value);
            parameters.Add(p => p.Min, min);
            parameters.Add(p => p.Max, max);
        });

        Assert.Contains($"width: {expected}%", component.Find(".bit-prb-bar").GetAttribute("style")!);
    }

    [TestMethod]
    public void BitProgressValueShouldBeAnnouncedInItsOwnUnit()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Value, 3);
            parameters.Add(p => p.Max, 10);
        });

        var bar = component.Find(".bit-prb-bar");
        Assert.AreEqual("3", bar.GetAttribute("aria-valuenow"));
        Assert.AreEqual("0", bar.GetAttribute("aria-valuemin"));
        Assert.AreEqual("10", bar.GetAttribute("aria-valuemax"));
    }

    [TestMethod]
    public void BitProgressValueShouldTakeThePlaceOfPercent()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Percent, 90);
            parameters.Add(p => p.Value, 3);
            parameters.Add(p => p.Max, 10);
        });

        Assert.Contains("width: 30%", component.Find(".bit-prb-bar").GetAttribute("style")!);
    }

    [TestMethod]
    public void BitProgressAnEmptyRangeShouldNotDivideByZero()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Value, 5);
            parameters.Add(p => p.Min, 10);
            parameters.Add(p => p.Max, 10);
        });

        Assert.Contains("width: 0%", component.Find(".bit-prb-bar").GetAttribute("style")!);
    }

    [TestMethod]
    public void BitProgressAnInvertedRangeShouldNotProduceANegativeMax()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Value, 5);
            parameters.Add(p => p.Min, 10);
            parameters.Add(p => p.Max, 0);
        });

        var bar = component.Find(".bit-prb-bar");
        Assert.Contains("width: 0%", bar.GetAttribute("style")!);
        Assert.AreEqual("10", bar.GetAttribute("aria-valuemax"));
        Assert.AreEqual("10", bar.GetAttribute("aria-valuenow"));
    }

    [TestMethod]
    public void BitProgressValueShouldDriveTheCircularStroke()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Circular, true);
            parameters.Add(p => p.Value, 7);
            parameters.Add(p => p.Max, 14);
        });

        Assert.Contains("--bit-prb-percent: 50%", component.Find(".bit-prb-cbr").GetAttribute("style")!);
    }


    // ---------------------------------------------------------------- buffer

    [TestMethod]
    public void BitProgressBufferShouldRenderASecondBar()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Percent, 35);
            parameters.Add(p => p.Buffer, 62);
        });

        Assert.Contains("width: 62%", component.Find(".bit-prb-buf").GetAttribute("style")!);
        Assert.Contains("width: 35%", component.Find(".bit-prb-bar").GetAttribute("style")!);
    }

    [TestMethod]
    public void BitProgressWithoutBufferShouldNotRenderOne()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Percent, 35);
        });

        Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-prb-buf"));
    }

    [TestMethod]
    public void BitProgressBufferShouldBeIgnoredWhileIndeterminate()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Indeterminate, true);
            parameters.Add(p => p.Buffer, 62);
        });

        Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-prb-buf"));
    }

    [TestMethod]
    public void BitProgressBufferShouldBeReadOnTheValueScale()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Value, 4);
            parameters.Add(p => p.Max, 10);
            parameters.Add(p => p.Buffer, 8);
        });

        Assert.Contains("width: 80%", component.Find(".bit-prb-buf").GetAttribute("style")!);
    }

    [TestMethod,
        DataRow(-20, "0"),
        DataRow(220, "100")
    ]
    public void BitProgressBufferShouldBeClamped(double buffer, string expected)
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Buffer, buffer);
        });

        Assert.Contains($"width: {expected}%", component.Find(".bit-prb-buf").GetAttribute("style")!);
    }

    [TestMethod]
    public void BitProgressCircularBufferShouldRenderItsOwnCircle()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Circular, true);
            parameters.Add(p => p.Percent, 35);
            parameters.Add(p => p.Buffer, 62);
        });

        Assert.Contains("--bit-prb-buffer: 62%", component.Find(".bit-prb-cbf").GetAttribute("style")!);
    }

    [TestMethod]
    public void BitProgressBufferShouldTakeItsOwnClassAndStyleSlot()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Buffer, 62);
            parameters.Add(p => p.Classes, new BitProgressClassStyles { Buffer = "custom-buffer" });
            parameters.Add(p => p.Styles, new BitProgressClassStyles { Buffer = "opacity: 0.5;" });
        });

        var buffer = component.Find(".bit-prb-buf");
        Assert.IsTrue(buffer.ClassList.Contains("custom-buffer"));
        Assert.Contains("opacity: 0.5;", buffer.GetAttribute("style")!);
    }


    // ---------------------------------------------------------------- circular geometry

    [TestMethod]
    public void BitProgressCircularShouldRenderTheTrackAndTheBar()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Circular, true);
            parameters.Add(p => p.Percent, 42);
        });

        Assert.IsNotNull(component.Find(".bit-prb-crp"));
        Assert.IsNotNull(component.Find(".bit-prb-crt"));
        Assert.IsNotNull(component.Find(".bit-prb-cbr"));
        Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-prb-bar"));
        Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-prb-cri"));
    }

    [TestMethod]
    public void BitProgressCircularIndeterminateShouldSwapTheBarForTheSpinner()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Circular, true);
            parameters.Add(p => p.Indeterminate, true);
        });

        Assert.IsNotNull(component.Find(".bit-prb-cri"));
        Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-prb-cbr"));
    }

    [TestMethod,
        DataRow(null, 6, "12px"),
        DataRow(10, 6, "60px"),
        DataRow(10, 3, "30px")
    ]
    public void BitProgressCircularSizeShouldComeFromTheThicknessAndTheRadius(int? thickness, int radius, string expected)
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Circular, true);
            parameters.Add(p => p.Thickness, thickness);
            parameters.Add(p => p.Radius, radius);
        });

        var svg = component.Find(".bit-prb-cir");
        Assert.AreEqual(expected, svg.GetAttribute("width"));
        Assert.AreEqual(expected, svg.GetAttribute("height"));
    }

    [TestMethod]
    public void BitProgressDiameterShouldPinTheCircularSize()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Circular, true);
            parameters.Add(p => p.Thickness, 10);
            parameters.Add(p => p.Diameter, 24);
        });

        var svg = component.Find(".bit-prb-cir");
        Assert.AreEqual("24px", svg.GetAttribute("width"));
        Assert.AreEqual("24px", svg.GetAttribute("height"));
        // The token is pinned too, otherwise the stylesheet's per-size floor would win over the ask.
        Assert.Contains("--bit-prb-diameter: 24px", svg.GetAttribute("style")!);
    }

    [TestMethod]
    public void BitProgressWithoutDiameterShouldLeaveTheTokenToTheStylesheet()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Circular, true);
        });

        Assert.IsNull(component.Find(".bit-prb-cir").GetAttribute("style"));
    }

    [TestMethod]
    public void BitProgressCircularShouldNotDuplicateTheBarContainerSlot()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Circular, true);
            parameters.Add(p => p.Classes, new BitProgressClassStyles { BarContainer = "custom-container" });
        });

        Assert.IsTrue(component.Find(".bit-prb-crp").ClassList.Contains("custom-container"));
        Assert.IsFalse(component.Find(".bit-prb-cir").ClassList.Contains("custom-container"));
    }

    [TestMethod]
    public void BitProgressCircularStrokeWidthShouldFollowTheThickness()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Circular, true);
            parameters.Add(p => p.Thickness, 7);
            parameters.Add(p => p.Buffer, 50);
        });

        Assert.Contains("stroke-width: 7px", component.Find(".bit-prb-crt").GetAttribute("style")!);
        Assert.Contains("stroke-width: 7px", component.Find(".bit-prb-cbf").GetAttribute("style")!);
        Assert.Contains("stroke-width: 7px", component.Find(".bit-prb-cbr").GetAttribute("style")!);
    }


    // ---------------------------------------------------------------- percentage readout

    [TestMethod]
    public void BitProgressPercentNumberShouldUseTheFormat()
    {
        // The readout is formatted with the ambient culture, so the expected separator is only a dot
        // while the culture the test runs under says so.
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("en-US");

            var component = RenderComponent<BitProgress>(parameters =>
            {
                parameters.Add(p => p.Percent, 85.69);
                parameters.Add(p => p.ShowPercentNumber, true);
                parameters.Add(p => p.PercentNumberFormat, "{0:F2} %");
            });

            Assert.AreEqual("85.69 %", component.Find(".bit-prb-pnm").TextContent);
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [TestMethod]
    public void BitProgressPercentNumberShouldReadTheValueRange()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Value, 3);
            parameters.Add(p => p.Max, 10);
            parameters.Add(p => p.ShowPercentNumber, true);
        });

        Assert.AreEqual("30 %", component.Find(".bit-prb-pnm").TextContent);
    }

    [TestMethod]
    public void BitProgressCircularPercentNumberShouldTakeTheCircularSlot()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Circular, true);
            parameters.Add(p => p.Percent, 42);
            parameters.Add(p => p.ShowPercentNumber, true);
        });

        Assert.IsNotNull(component.Find(".bit-prb-ctx"));
        Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-prb-pct"));
    }

    [TestMethod]
    public void BitProgressIndeterminateShouldHideThePercentNumber()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Indeterminate, true);
            parameters.Add(p => p.ShowPercentNumber, true);
        });

        Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-prb-pct"));
    }

    [TestMethod]
    public void BitProgressPercentNumberTemplateShouldReplaceTheText()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Percent, 42);
            parameters.Add(p => p.PercentNumberTemplate, percent => $"<span class=\"tpl\">{percent} done</span>");
        });

        // The template alone is enough to render the readout - ShowPercentNumber is not also required.
        Assert.AreEqual("42 done", component.Find(".tpl").TextContent);
        Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-prb-pnm"));
    }

    [TestMethod]
    public void BitProgressPercentNumberTemplateShouldBeHiddenWhileIndeterminate()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Indeterminate, true);
            parameters.Add(p => p.PercentNumberTemplate, percent => $"<span class=\"tpl\">{percent}</span>");
        });

        Assert.Throws<ElementNotFoundException>(() => component.Find(".tpl"));
    }


    [TestMethod,
        DataRow(BitProgressPercentPosition.End, "bit-prb-pce"),
        DataRow(BitProgressPercentPosition.Start, "bit-prb-pcs"),
        DataRow(BitProgressPercentPosition.Center, "bit-prb-pcc"),
        DataRow(BitProgressPercentPosition.Inside, "bit-prb-pci"),
        DataRow(BitProgressPercentPosition.Top, "bit-prb-pco")
    ]
    public void BitProgressPercentNumberPositionTest(BitProgressPercentPosition position, string expectedClass)
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Percent, 42);
            parameters.Add(p => p.ShowPercentNumber, true);
            parameters.Add(p => p.PercentNumberPosition, position);
        });

        var readout = component.Find(".bit-prb-pct");
        Assert.IsTrue(readout.ClassList.Contains(expectedClass));
    }

    [TestMethod]
    public void BitProgressInsidePercentNumberShouldTravelWithTheBar()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Percent, 42);
            parameters.Add(p => p.ShowPercentNumber, true);
            parameters.Add(p => p.PercentNumberPosition, BitProgressPercentPosition.Inside);
        });

        var bar = component.Find(".bit-prb-bar");
        Assert.IsTrue(bar.ClassList.Contains("bit-prb-bri"));
        Assert.IsNotNull(component.Find(".bit-prb-bar > .bit-prb-pci"));
        Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-prb > .bit-prb-pct"));
    }

    [TestMethod]
    public void BitProgressInsidePercentNumberShouldNotClipABarThatHasNoReadout()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Percent, 42);
            parameters.Add(p => p.PercentNumberPosition, BitProgressPercentPosition.Inside);
        });

        Assert.IsFalse(component.Find(".bit-prb-bar").ClassList.Contains("bit-prb-bri"));
    }

    [TestMethod]
    public void BitProgressTopPercentNumberShouldShareTheLabelRow()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Label, "Uploading");
            parameters.Add(p => p.Percent, 42);
            parameters.Add(p => p.ShowPercentNumber, true);
            parameters.Add(p => p.PercentNumberPosition, BitProgressPercentPosition.Top);
        });

        var header = component.Find(".bit-prb-hdr");
        Assert.AreEqual(2, header.Children.Length);
        Assert.IsTrue(header.Children[0].ClassList.Contains("bit-prb-lbl"));
        Assert.IsTrue(header.Children[1].ClassList.Contains("bit-prb-pco"));

        // ... and it is not rendered a second time under the bar.
        Assert.AreEqual(1, component.FindAll(".bit-prb-pct").Count);
    }

    [TestMethod]
    public void BitProgressTopPercentNumberShouldNotNeedALabel()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Percent, 42);
            parameters.Add(p => p.ShowPercentNumber, true);
            parameters.Add(p => p.PercentNumberPosition, BitProgressPercentPosition.Top);
        });

        var header = component.Find(".bit-prb-hdr");
        Assert.AreEqual(1, header.Children.Length);
        Assert.IsTrue(header.Children[0].ClassList.Contains("bit-prb-pco"));
        Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-prb-lbl"));
    }

    [TestMethod]
    public void BitProgressALabelShouldAlwaysSitInTheHeaderRow()
    {
        // The row is the label's element whether or not a readout joins it, so a label laid out on its own is
        // laid out in exactly the element it is laid out in beside one.
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Label, "Label");
        });

        Assert.IsNotNull(component.Find(".bit-prb > .bit-prb-hdr > .bit-prb-lbl"));
    }

    [TestMethod]
    public void BitProgressWithoutALabelOrATopReadoutShouldRenderNoHeaderRow()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Percent, 42);
            parameters.Add(p => p.ShowPercentNumber, true);
        });

        Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-prb-hdr"));
    }

    [TestMethod]
    public void BitProgressAnIndeterminateTopReadoutShouldLeaveTheHeaderToTheLabel()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Label, "Preparing");
            parameters.Add(p => p.Indeterminate, true);
            parameters.Add(p => p.ShowPercentNumber, true);
            parameters.Add(p => p.PercentNumberPosition, BitProgressPercentPosition.Top);
        });

        var header = component.Find(".bit-prb-hdr");
        Assert.AreEqual(1, header.Children.Length);
        Assert.IsTrue(header.Children[0].ClassList.Contains("bit-prb-lbl"));
    }

    [TestMethod]
    public void BitProgressTopPercentNumberShouldCarryItsSlots()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Label, "Uploading");
            parameters.Add(p => p.Percent, 42);
            parameters.Add(p => p.ShowPercentNumber, true);
            parameters.Add(p => p.PercentNumberPosition, BitProgressPercentPosition.Top);
            parameters.Add(p => p.Classes, new BitProgressClassStyles { PercentNumber = "custom-pct" });
            parameters.Add(p => p.Styles, new BitProgressClassStyles { PercentNumber = "color: red;" });
        });

        var readout = component.Find(".bit-prb-pco");
        Assert.IsTrue(readout.ClassList.Contains("custom-pct"));
        Assert.Contains("color: red;", readout.GetAttribute("style")!);
    }

    [TestMethod]
    public void BitProgressCircularShouldIgnoreThePercentNumberPosition()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Circular, true);
            parameters.Add(p => p.Percent, 42);
            parameters.Add(p => p.ShowPercentNumber, true);
            parameters.Add(p => p.PercentNumberPosition, BitProgressPercentPosition.Inside);
        });

        Assert.IsNotNull(component.Find(".bit-prb-ctx"));
        Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-prb-pci"));
    }

    [TestMethod]
    public void BitProgressCircularShouldNotMoveItsReadoutToTheHeader()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Circular, true);
            parameters.Add(p => p.Label, "Steps");
            parameters.Add(p => p.Percent, 42);
            parameters.Add(p => p.ShowPercentNumber, true);
            parameters.Add(p => p.PercentNumberPosition, BitProgressPercentPosition.Top);
        });

        Assert.IsNotNull(component.Find(".bit-prb-ctx"));
        Assert.AreEqual(1, component.Find(".bit-prb-hdr").Children.Length);
    }


    // ---------------------------------------------------------------- shape & decoration

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitProgressRoundedTest(bool rounded)
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Rounded, rounded);
        });

        Assert.AreEqual(rounded, component.Find(".bit-prb").ClassList.Contains("bit-prb-rnd"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitProgressReversedTest(bool reversed)
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Reversed, reversed);
        });

        Assert.AreEqual(reversed, component.Find(".bit-prb").ClassList.Contains("bit-prb-rev"));
    }

    [TestMethod]
    public void BitProgressShapeClassesShouldFollowAParameterChange()
    {
        var component = RenderComponent<BitProgress>();

        Assert.IsFalse(component.Find(".bit-prb").ClassList.Contains("bit-prb-rnd"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Rounded, true);
            parameters.Add(p => p.Reversed, true);
        });

        var root = component.Find(".bit-prb");
        Assert.IsTrue(root.ClassList.Contains("bit-prb-rnd"));
        Assert.IsTrue(root.ClassList.Contains("bit-prb-rev"));
    }

    [TestMethod,
        DataRow(true, false),
        DataRow(true, true),
        DataRow(false, true)
    ]
    public void BitProgressStripedTest(bool striped, bool animation)
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Striped, striped);
            parameters.Add(p => p.StripedAnimation, animation);
        });

        var bar = component.Find(".bit-prb-bar");
        Assert.AreEqual(striped, bar.ClassList.Contains("bit-prb-stp"));
        Assert.AreEqual(striped && animation, bar.ClassList.Contains("bit-prb-sta"));
    }

    [TestMethod]
    public void BitProgressStripesShouldNotSurviveTheIndeterminateSweep()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Indeterminate, true);
            parameters.Add(p => p.Striped, true);
            parameters.Add(p => p.StripedAnimation, true);
        });

        var bar = component.Find(".bit-prb-bar");
        Assert.IsTrue(bar.ClassList.Contains("bit-prb-ind"));
        Assert.IsFalse(bar.ClassList.Contains("bit-prb-stp"));
        Assert.IsFalse(bar.ClassList.Contains("bit-prb-sta"));
    }


    // ---------------------------------------------------------------- gauge

    [TestMethod,
        DataRow(90, "90", "0.75"),
        DataRow(180, "180", "0.5"),
        DataRow(120, "120", "0.6666666666666666")
    ]
    public void BitProgressGapDegreeShouldShortenTheArc(double gap, string expectedGap, string expectedArc)
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Circular, true);
            parameters.Add(p => p.GapDegree, gap);
        });

        var root = component.Find(".bit-prb");
        Assert.IsTrue(root.ClassList.Contains("bit-prb-gap"));
        Assert.Contains($"--bit-prb-gap: {expectedGap}deg", root.GetAttribute("style")!);
        Assert.Contains($"--bit-prb-arc: {expectedArc}", root.GetAttribute("style")!);
    }

    [TestMethod,
        DataRow(0),
        DataRow(-30)
    ]
    public void BitProgressWithoutAGapIsAClosedRing(double gap)
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Circular, true);
            parameters.Add(p => p.GapDegree, gap);
        });

        Assert.IsFalse(component.Find(".bit-prb").ClassList.Contains("bit-prb-gap"));
    }

    [TestMethod]
    public void BitProgressGapDegreeShouldBeCappedAt295()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Circular, true);
            parameters.Add(p => p.GapDegree, 400);
        });

        Assert.Contains("--bit-prb-gap: 295deg", component.Find(".bit-prb").GetAttribute("style")!);
    }

    [TestMethod,
        DataRow(BitProgressGapPosition.Bottom, null),
        DataRow(BitProgressGapPosition.Top, "bit-prb-gpt"),
        DataRow(BitProgressGapPosition.Start, "bit-prb-gps"),
        DataRow(BitProgressGapPosition.End, "bit-prb-gpe")
    ]
    public void BitProgressGapPositionTest(BitProgressGapPosition position, string? expectedClass)
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Circular, true);
            parameters.Add(p => p.GapDegree, 90);
            parameters.Add(p => p.GapPosition, position);
        });

        var classList = component.Find(".bit-prb").ClassList;
        Assert.IsTrue(classList.Contains("bit-prb-gap"));

        foreach (var candidate in (string[])["bit-prb-gpt", "bit-prb-gps", "bit-prb-gpe"])
        {
            Assert.AreEqual(candidate == expectedClass, classList.Contains(candidate));
        }
    }

    [TestMethod]
    public void BitProgressGapPositionShouldNeedAGapToApply()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Circular, true);
            parameters.Add(p => p.GapPosition, BitProgressGapPosition.Top);
        });

        Assert.IsFalse(component.Find(".bit-prb").ClassList.Contains("bit-prb-gpt"));
    }

    [TestMethod]
    public void BitProgressGapPositionShouldFollowAParameterChange()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Circular, true);
            parameters.Add(p => p.GapDegree, 90);
        });

        Assert.IsFalse(component.Find(".bit-prb").ClassList.Contains("bit-prb-gpt"));

        component.Render(parameters => parameters.Add(p => p.GapPosition, BitProgressGapPosition.Top));

        Assert.IsTrue(component.Find(".bit-prb").ClassList.Contains("bit-prb-gpt"));
    }

    [TestMethod]
    public void BitProgressGapDegreeShouldNotApplyToTheLinearProgress()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.GapDegree, 90);
        });

        Assert.IsFalse(component.Find(".bit-prb").ClassList.Contains("bit-prb-gap"));
    }

    [TestMethod]
    public void BitProgressGapAndSegmentTokensShouldBothReachTheRoot()
    {
        // Both are written ahead of the style builder's own output, so neither can drop the other.
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Circular, true);
            parameters.Add(p => p.GapDegree, 90);
            parameters.Add(p => p.Style, "margin: 1rem;");
        });

        var style = component.Find(".bit-prb").GetAttribute("style")!;
        Assert.Contains("--bit-prb-gap: 90deg", style);
        Assert.Contains("margin: 1rem;", style);
    }


    // ---------------------------------------------------------------- vertical

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitProgressVerticalTest(bool vertical)
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Vertical, vertical);
        });

        Assert.AreEqual(vertical, component.Find(".bit-prb").ClassList.Contains("bit-prb-ver"));
    }

    [TestMethod]
    public void BitProgressVerticalShouldNotApplyToTheCircularProgress()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Circular, true);
            parameters.Add(p => p.Vertical, true);
        });

        Assert.IsFalse(component.Find(".bit-prb").ClassList.Contains("bit-prb-ver"));
    }

    [TestMethod]
    public void BitProgressVerticalShouldFillAlongTheOtherAxis()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Vertical, true);
            parameters.Add(p => p.Percent, 42);
            parameters.Add(p => p.Buffer, 70);
            parameters.Add(p => p.Thickness, 12);
        });

        var bar = component.Find(".bit-prb-bar").GetAttribute("style")!;
        Assert.Contains("height: 42%", bar);
        Assert.IsFalse(bar.Contains("width:"));

        Assert.Contains("height: 70%", component.Find(".bit-prb-buf").GetAttribute("style")!);
    }

    [TestMethod]
    public void BitProgressVerticalShouldTakeItsThicknessAndLengthOnTheContainer()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Vertical, true);
            parameters.Add(p => p.Thickness, 12);
            parameters.Add(p => p.Length, "8rem");
        });

        var container = component.Find(".bit-prb-bcn").GetAttribute("style")!;
        Assert.Contains("width: 12px", container);
        Assert.Contains("height: 8rem", container);
        Assert.IsFalse(container.Contains("min-height"));
    }

    [TestMethod]
    public void BitProgressVerticalWithoutALengthShouldReadTheLengthToken()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Vertical, true);
        });

        var container = component.Find(".bit-prb-bcn").GetAttribute("style")!;
        Assert.Contains("width: var(--bit-prb-thickness)", container);
        Assert.Contains("height: var(--bit-prb-length)", container);
    }

    [TestMethod]
    public void BitProgressLengthShouldNotReachAHorizontalBar()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Length, "8rem");
        });

        var container = component.Find(".bit-prb-bcn").GetAttribute("style")!;
        Assert.IsFalse(container.Contains("8rem"));
        Assert.Contains("min-height", container);
    }

    [TestMethod]
    public void BitProgressVerticalTrackShouldCarryNoInlineGeometry()
    {
        // The container carries the width for all three children, so the track is left to the sheet.
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Vertical, true);
            parameters.Add(p => p.Thickness, 12);
        });

        Assert.AreEqual(string.Empty, component.Find(".bit-prb-trc").GetAttribute("style") ?? string.Empty);
    }


    // ---------------------------------------------------------------- segments

    [TestMethod]
    public void BitProgressSegmentsShouldMaskTheBarContainer()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Segments, 5);
            parameters.Add(p => p.SegmentGap, 6);
        });

        var root = component.Find(".bit-prb");
        Assert.IsTrue(root.ClassList.Contains("bit-prb-seg"));
        Assert.Contains("--bit-prb-segments: 5", root.GetAttribute("style")!);
        Assert.Contains("--bit-prb-segment-gap: 6px", root.GetAttribute("style")!);
    }

    [TestMethod]
    public void BitProgressSegmentTokensAndTheRootStyleShouldBothReachTheRoot()
    {
        // The tokens are written ahead of the style builder's own output, so neither they nor the
        // consumer's style can drop the other.
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Segments, 5);
            parameters.Add(p => p.Style, "padding: 1rem;");
        });

        var style = component.Find(".bit-prb").GetAttribute("style")!;
        Assert.Contains("--bit-prb-segments: 5", style);
        Assert.Contains("padding: 1rem;", style);
    }

    [TestMethod,
        DataRow(null),
        DataRow(0),
        DataRow(1)
    ]
    public void BitProgressSegmentsBelowTwoIsNotASegmentedBar(int? segments)
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Segments, segments);
        });

        Assert.IsFalse(component.Find(".bit-prb").ClassList.Contains("bit-prb-seg"));
    }

    [TestMethod]
    public void BitProgressSegmentsShouldNotApplyToTheCircularProgress()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Circular, true);
            parameters.Add(p => p.Segments, 5);
        });

        Assert.IsFalse(component.Find(".bit-prb").ClassList.Contains("bit-prb-seg"));
    }

    [TestMethod]
    public void BitProgressSegmentGapCanNotBeNegative()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Segments, 5);
            parameters.Add(p => p.SegmentGap, -8);
        });

        Assert.Contains("--bit-prb-segment-gap: 0px", component.Find(".bit-prb").GetAttribute("style")!);
    }

    [TestMethod]
    public void BitProgressPercentNumberShouldStayOutsideTheMaskedContainer()
    {
        // The segment mask is set on the bar container, so anything else inside it would be cut into
        // pieces along with the bar.
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Segments, 5);
            parameters.Add(p => p.Percent, 42);
            parameters.Add(p => p.ShowPercentNumber, true);
        });

        Assert.IsTrue(component.Find(".bit-prb").ClassList.Contains("bit-prb-seg"));
        Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-prb-bcn .bit-prb-pct"));
        Assert.IsNotNull(component.Find(".bit-prb > .bit-prb-pct"));
    }


    // ---------------------------------------------------------------- announcements

    [TestMethod]
    public void BitProgressWithoutAnnounceProgressShouldRenderNoLiveRegion()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Percent, 42);
        });

        Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-prb-lvr"));
    }

    [TestMethod]
    public void BitProgressLiveRegionShouldBeOnThePageBeforeItHasAnythingToSay()
    {
        // A live region only announces what changes inside one the screen reader is already watching.
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.AnnounceProgress, true);
        });

        var region = component.Find(".bit-prb-lvr");
        Assert.AreEqual("polite", region.GetAttribute("aria-live"));
        Assert.AreEqual("true", region.GetAttribute("aria-atomic"));
        Assert.AreEqual(string.Empty, region.TextContent);
    }

    [TestMethod]
    public void BitProgressShouldNotAnnounceTheValueItStartedAt()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.AnnounceProgress, true);
            parameters.Add(p => p.Percent, 60);
        });

        Assert.AreEqual(string.Empty, component.Find(".bit-prb-lvr").TextContent);
    }

    [TestMethod]
    public void BitProgressShouldAnnounceOncePerStepCrossed()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.AnnounceProgress, true);
            parameters.Add(p => p.Percent, 0);
        });

        component.Render(parameters => parameters.Add(p => p.Percent, 10));
        Assert.AreEqual(string.Empty, component.Find(".bit-prb-lvr").TextContent);

        component.Render(parameters => parameters.Add(p => p.Percent, 30));
        Assert.AreEqual("25 %", component.Find(".bit-prb-lvr").TextContent);

        component.Render(parameters => parameters.Add(p => p.Percent, 40));
        Assert.AreEqual("25 %", component.Find(".bit-prb-lvr").TextContent);

        component.Render(parameters => parameters.Add(p => p.Percent, 55));
        Assert.AreEqual("50 %", component.Find(".bit-prb-lvr").TextContent);
    }

    [TestMethod]
    public void BitProgressShouldAlwaysAnnounceCompletion()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.AnnounceProgress, true);
            parameters.Add(p => p.AnnounceStep, 30);
            parameters.Add(p => p.Percent, 0);
        });

        component.Render(parameters => parameters.Add(p => p.Percent, 100));

        Assert.AreEqual("100 %", component.Find(".bit-prb-lvr").TextContent);
    }

    [TestMethod]
    public void BitProgressAnnounceStepShouldSetTheGranularity()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.AnnounceProgress, true);
            parameters.Add(p => p.AnnounceStep, 10);
            parameters.Add(p => p.Percent, 0);
        });

        component.Render(parameters => parameters.Add(p => p.Percent, 12));

        Assert.AreEqual("10 %", component.Find(".bit-prb-lvr").TextContent);
    }

    [TestMethod]
    public void BitProgressAnnouncementShouldCarryTheLabelAndTheValueText()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.AnnounceProgress, true);
            parameters.Add(p => p.Label, "Importing");
            parameters.Add(p => p.Percent, 0);
        });

        component.Render(parameters => parameters.Add(p => p.Percent, 50));
        Assert.AreEqual("Importing: 50 %", component.Find(".bit-prb-lvr").TextContent);

        component.Render(parameters =>
        {
            parameters.Add(p => p.Percent, 80);
            parameters.Add(p => p.AriaValueText, "8 of 10 rows");
        });
        Assert.AreEqual("Importing: 8 of 10 rows", component.Find(".bit-prb-lvr").TextContent);
    }

    [TestMethod]
    public void BitProgressAnnouncementShouldFollowThePercentNumberFormat()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.AnnounceProgress, true);
            parameters.Add(p => p.PercentNumberFormat, "{0:F0} percent complete");
            parameters.Add(p => p.Percent, 0);
        });

        component.Render(parameters => parameters.Add(p => p.Percent, 50));

        Assert.AreEqual("50 percent complete", component.Find(".bit-prb-lvr").TextContent);
    }

    [TestMethod]
    public void BitProgressGoingBackwardsShouldBeRecordedInSilence()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.AnnounceProgress, true);
            parameters.Add(p => p.Percent, 0);
        });

        component.Render(parameters => parameters.Add(p => p.Percent, 80));
        Assert.AreEqual("75 %", component.Find(".bit-prb-lvr").TextContent);

        // A reset is not an advance, and the announcement it leaves behind is not repeated either.
        component.Render(parameters => parameters.Add(p => p.Percent, 5));
        Assert.AreEqual("75 %", component.Find(".bit-prb-lvr").TextContent);

        component.Render(parameters => parameters.Add(p => p.Percent, 30));
        Assert.AreEqual("25 %", component.Find(".bit-prb-lvr").TextContent);
    }

    [TestMethod]
    public void BitProgressShouldAnnounceTheSameMilestoneTwiceAfterAReset()
    {
        // A milestone that was already announced is announced a second time once the progress has been
        // reset past it. The two announcements read the same, so the label is changed on the way back
        // up: only a re-announcement can put the new one in the region. (In a browser the keyed span is
        // what makes the live region fire again on identical text, but bUnit re-parses the whole markup
        // after every render, so element identity says nothing about it either way.)
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.AnnounceProgress, true);
            parameters.Add(p => p.Label, "First run");
            parameters.Add(p => p.Percent, 0);
        });

        component.Render(parameters => parameters.Add(p => p.Percent, 30));
        Assert.AreEqual("First run: 25 %", component.Find(".bit-prb-lvr span").TextContent);

        // The reset itself announces nothing - it leaves the last announcement standing in the region.
        component.Render(parameters => parameters.Add(p => p.Percent, 0));
        Assert.AreEqual("First run: 25 %", component.Find(".bit-prb-lvr span").TextContent);

        component.Render(parameters =>
        {
            parameters.Add(p => p.Label, "Second run");
            parameters.Add(p => p.Percent, 30);
        });
        Assert.AreEqual("Second run: 25 %", component.Find(".bit-prb-lvr span").TextContent);
    }

    [TestMethod]
    public void BitProgressIndeterminateShouldAnnounceNothing()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.AnnounceProgress, true);
            parameters.Add(p => p.Indeterminate, true);
            parameters.Add(p => p.Percent, 0);
        });

        component.Render(parameters => parameters.Add(p => p.Percent, 90));

        Assert.AreEqual(string.Empty, component.Find(".bit-prb-lvr").TextContent);
    }

    [TestMethod]
    public void BitProgressAnnouncementShouldReadTheValueRange()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.AnnounceProgress, true);
            parameters.Add(p => p.Max, 10);
            parameters.Add(p => p.Value, 0);
        });

        component.Render(parameters => parameters.Add(p => p.Value, 6));

        Assert.AreEqual("50 %", component.Find(".bit-prb-lvr").TextContent);
    }

    [TestMethod]
    public void BitProgressAnNonPositiveAnnounceStepShouldFallBackToTheDefault()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.AnnounceProgress, true);
            parameters.Add(p => p.AnnounceStep, 0);
            parameters.Add(p => p.Percent, 0);
        });

        component.Render(parameters => parameters.Add(p => p.Percent, 30));

        Assert.AreEqual("25 %", component.Find(".bit-prb-lvr").TextContent);
    }


    // ---------------------------------------------------------------- disabled

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitProgressIsEnabledTest(bool isEnabled)
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.Percent, 42);
        });

        Assert.AreEqual(isEnabled is false, component.Find(".bit-prb").ClassList.Contains("bit-dis"));
        // A disabled progress still reports where it got to; it is stopped, not emptied.
        Assert.AreEqual("42", component.Find(".bit-prb-bar").GetAttribute("aria-valuenow"));
    }


    // ---------------------------------------------------------------- color & size

    [TestMethod,
        DataRow(null, "bit-prb-pri"),
        DataRow(BitColor.Primary, "bit-prb-pri"),
        DataRow(BitColor.Secondary, "bit-prb-sec"),
        DataRow(BitColor.Tertiary, "bit-prb-ter"),
        DataRow(BitColor.Info, "bit-prb-inf"),
        DataRow(BitColor.Success, "bit-prb-suc"),
        DataRow(BitColor.Warning, "bit-prb-wrn"),
        DataRow(BitColor.SevereWarning, "bit-prb-swr"),
        DataRow(BitColor.Error, "bit-prb-err"),
        DataRow(BitColor.PrimaryBackground, "bit-prb-pbg"),
        DataRow(BitColor.SecondaryBackground, "bit-prb-sbg"),
        DataRow(BitColor.TertiaryBackground, "bit-prb-tbg"),
        DataRow(BitColor.PrimaryForeground, "bit-prb-pfg"),
        DataRow(BitColor.SecondaryForeground, "bit-prb-sfg"),
        DataRow(BitColor.TertiaryForeground, "bit-prb-tfg"),
        DataRow(BitColor.PrimaryBorder, "bit-prb-pbr"),
        DataRow(BitColor.SecondaryBorder, "bit-prb-sbr"),
        DataRow(BitColor.TertiaryBorder, "bit-prb-tbr")
    ]
    public void BitProgressColorTest(BitColor? color, string expectedClass)
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Color, color);
        });

        Assert.IsTrue(component.Find(".bit-prb").ClassList.Contains(expectedClass));
    }

    [TestMethod]
    public void BitProgressBarColorShouldReachTheRootAsAToken()
    {
        // One declaration on the root is what the bar, the ring stroke, the buffer tint and the stripes all
        // read, so the whole indicator follows a custom color rather than only the filled rectangle.
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Percent, 42);
            parameters.Add(p => p.BarColor, "tomato");
        });

        Assert.Contains("--bit-prb-bar-color: tomato;", component.Find(".bit-prb").GetAttribute("style")!);
    }

    [TestMethod]
    public void BitProgressTrackColorShouldReachTheRootAsAToken()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Percent, 42);
            parameters.Add(p => p.TrackColor, "#e9d5ff");
        });

        Assert.Contains("--bit-prb-track-color: #e9d5ff;", component.Find(".bit-prb").GetAttribute("style")!);
    }

    [TestMethod]
    public void BitProgressCustomColorsShouldAlsoReachTheCircularShape()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Circular, true);
            parameters.Add(p => p.Percent, 42);
            parameters.Add(p => p.BarColor, "tomato");
            parameters.Add(p => p.TrackColor, "gainsboro");
        });

        var style = component.Find(".bit-prb").GetAttribute("style")!;
        Assert.Contains("--bit-prb-bar-color: tomato;", style);
        Assert.Contains("--bit-prb-track-color: gainsboro;", style);
    }

    [TestMethod]
    public void BitProgressWithoutCustomColorsShouldDeclareNoColorToken()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Percent, 42);
            parameters.Add(p => p.Color, BitColor.Success);
        });

        var style = component.Find(".bit-prb").GetAttribute("style");

        Assert.IsTrue(style.HasNoValue() || style!.Contains("--bit-prb-bar-color") is false);
    }

    [TestMethod]
    public void BitProgressCustomColorsShouldNotOutrankTheRootStyles()
    {
        // The tokens are a prefix of the root style, so what the consumer wrote still comes last and wins.
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Percent, 42);
            parameters.Add(p => p.BarColor, "tomato");
            parameters.Add(p => p.Segments, 4);
            parameters.Add(p => p.Styles, new BitProgressClassStyles { Root = "--bit-prb-bar-color: rebeccapurple;" });
        });

        var style = component.Find(".bit-prb").GetAttribute("style")!;

        Assert.IsTrue(style.IndexOf("tomato") < style.IndexOf("rebeccapurple"));
        Assert.Contains("--bit-prb-segments: 4;", style);
    }

    [TestMethod]
    public void BitProgressCustomColorsShouldFollowAParameterChange()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Percent, 42);
            parameters.Add(p => p.BarColor, "tomato");
        });

        Assert.Contains("--bit-prb-bar-color: tomato;", component.Find(".bit-prb").GetAttribute("style")!);

        component.Render(parameters => parameters.Add(p => p.BarColor, "rebeccapurple"));

        Assert.Contains("--bit-prb-bar-color: rebeccapurple;", component.Find(".bit-prb").GetAttribute("style")!);
    }

    [TestMethod,
        DataRow(BitSize.Small, "bit-prb-sm"),
        DataRow(BitSize.Medium, "bit-prb-md"),
        DataRow(BitSize.Large, "bit-prb-lg")
    ]
    public void BitProgressSizeTest(BitSize size, string expectedClass)
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Size, size);
        });

        Assert.IsTrue(component.Find(".bit-prb").ClassList.Contains(expectedClass));
    }

    [TestMethod]
    public void BitProgressWithoutSizeShouldCarryNoSizeClass()
    {
        var component = RenderComponent<BitProgress>();

        var classList = component.Find(".bit-prb").ClassList;
        Assert.IsFalse(classList.Contains("bit-prb-sm"));
        Assert.IsFalse(classList.Contains("bit-prb-md"));
        Assert.IsFalse(classList.Contains("bit-prb-lg"));
    }

    [TestMethod,
        DataRow(BitSize.Small, 2),
        DataRow(BitSize.Medium, 4),
        DataRow(BitSize.Large, 8)
    ]
    public void BitProgressCircularThicknessShouldFollowTheSize(BitSize size, int expected)
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Circular, true);
            parameters.Add(p => p.Size, size);
        });

        Assert.Contains($"stroke-width: {expected}px", component.Find(".bit-prb-cbr").GetAttribute("style")!);
    }


    // ---------------------------------------------------------------- styles, classes & html

    [TestMethod]
    public void BitProgressClassesAndStylesTest()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Percent, 42);
            parameters.Add(p => p.Label, "Label");
            parameters.Add(p => p.Description, "Description");
            parameters.Add(p => p.ShowPercentNumber, true);
            parameters.Add(p => p.Classes, new BitProgressClassStyles
            {
                Root = "custom-root",
                Label = "custom-label",
                BarContainer = "custom-container",
                Track = "custom-track",
                Bar = "custom-bar",
                PercentNumber = "custom-percent",
                Description = "custom-description"
            });
            parameters.Add(p => p.Styles, new BitProgressClassStyles
            {
                Root = "color: red;",
                Label = "color: green;",
                BarContainer = "color: blue;",
                Track = "background-color: black;",
                Bar = "background-color: white;",
                PercentNumber = "color: gray;",
                Description = "color: yellow;"
            });
        });

        Assert.IsTrue(component.Find(".bit-prb").ClassList.Contains("custom-root"));
        Assert.Contains("color: red;", component.Find(".bit-prb").GetAttribute("style")!);

        Assert.IsTrue(component.Find(".bit-prb-lbl").ClassList.Contains("custom-label"));
        Assert.Contains("color: green;", component.Find(".bit-prb-lbl").GetAttribute("style")!);

        Assert.IsTrue(component.Find(".bit-prb-bcn").ClassList.Contains("custom-container"));
        Assert.Contains("color: blue;", component.Find(".bit-prb-bcn").GetAttribute("style")!);

        Assert.IsTrue(component.Find(".bit-prb-trc").ClassList.Contains("custom-track"));
        Assert.Contains("background-color: black;", component.Find(".bit-prb-trc").GetAttribute("style")!);

        Assert.IsTrue(component.Find(".bit-prb-bar").ClassList.Contains("custom-bar"));
        Assert.Contains("background-color: white;", component.Find(".bit-prb-bar").GetAttribute("style")!);

        Assert.IsTrue(component.Find(".bit-prb-pct").ClassList.Contains("custom-percent"));
        Assert.Contains("color: gray;", component.Find(".bit-prb-pct").GetAttribute("style")!);

        Assert.IsTrue(component.Find(".bit-prb-des").ClassList.Contains("custom-description"));
        Assert.Contains("color: yellow;", component.Find(".bit-prb-des").GetAttribute("style")!);
    }

    [TestMethod]
    public void BitProgressCustomStylesShouldComeAfterTheComputedGeometry()
    {
        // Last declaration wins in a style attribute, so a consumer overriding the width or the height
        // has to be written after what the component computed.
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Percent, 42);
            parameters.Add(p => p.Buffer, 60);
            parameters.Add(p => p.Styles, new BitProgressClassStyles
            {
                Bar = "width: 10%;",
                Track = "height: 3px;",
                Buffer = "width: 20%;"
            });
        });

        var barStyle = component.Find(".bit-prb-bar").GetAttribute("style")!;
        Assert.IsTrue(barStyle.IndexOf("width: 42%") < barStyle.IndexOf("width: 10%"));

        var trackStyle = component.Find(".bit-prb-trc").GetAttribute("style")!;
        Assert.IsTrue(trackStyle.IndexOf("var(--bit-prb-thickness)") < trackStyle.IndexOf("height: 3px"));

        var bufferStyle = component.Find(".bit-prb-buf").GetAttribute("style")!;
        Assert.IsTrue(bufferStyle.IndexOf("width: 60%") < bufferStyle.IndexOf("width: 20%"));
    }

    [TestMethod]
    public void BitProgressStyleAndClassTest()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Style, "padding: 1rem;");
            parameters.Add(p => p.Class, "custom-class");
        });

        var root = component.Find(".bit-prb");
        Assert.IsTrue(root.ClassList.Contains("custom-class"));
        Assert.Contains("padding: 1rem;", root.GetAttribute("style")!);
    }

    [TestMethod,
        DataRow(BitDir.Ltr, "ltr"),
        DataRow(BitDir.Rtl, "rtl"),
        DataRow(BitDir.Auto, "auto")
    ]
    public void BitProgressDirTest(BitDir dir, string expected)
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Dir, dir);
        });

        Assert.AreEqual(expected, component.Find(".bit-prb").GetAttribute("dir"));
    }

    [TestMethod,
        DataRow(BitVisibility.Visible, ""),
        DataRow(BitVisibility.Hidden, "visibility:hidden"),
        DataRow(BitVisibility.Collapsed, "display:none")
    ]
    public void BitProgressVisibilityTest(BitVisibility visibility, string expected)
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Visibility, visibility);
        });

        var style = component.Find(".bit-prb").GetAttribute("style") ?? string.Empty;

        if (expected.Length == 0)
        {
            Assert.IsFalse(style.Contains("visibility:hidden"));
            Assert.IsFalse(style.Contains("display:none"));
        }
        else
        {
            Assert.Contains(expected, style);
        }
    }

    [TestMethod]
    public void BitProgressHtmlAttributesTest()
    {
        // Arbitrary HTML attributes are captured by BitComponentBase from unmatched parameters, so
        // supply them as raw component attributes rather than via the builder, which rejects unmatched
        // params on components without [Parameter(CaptureUnmatchedValues)].
        var component = Context.Render(builder =>
        {
            builder.OpenComponent<BitProgress>(0);
            builder.AddAttribute(1, "data-test", "progress");
            builder.AddAttribute(2, "title", "A title");
            builder.CloseComponent();
        });

        var root = component.Find(".bit-prb");
        Assert.AreEqual("progress", root.GetAttribute("data-test"));
        Assert.AreEqual("A title", root.GetAttribute("title"));
    }

    [TestMethod]
    public void BitProgressIdTest()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Id, "custom-id");
        });

        Assert.AreEqual("custom-id", component.Find(".bit-prb").Id);
    }


    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitProgressForceAnimationTest(bool forceAnimation)
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Indeterminate, true);
            parameters.Add(p => p.ForceAnimation, forceAnimation);
        });

        Assert.AreEqual(forceAnimation, component.Find(".bit-prb").ClassList.Contains("bit-fam"));
    }

    [TestMethod]
    public void BitProgressRtlShouldCarryTheDirectionClass()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Rtl);
        });

        Assert.IsTrue(component.Find(".bit-prb").ClassList.Contains("bit-rtl"));
    }

    [TestMethod]
    public void BitProgressCircularClassesAndStylesTest()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Circular, true);
            parameters.Add(p => p.Indeterminate, true);
            parameters.Add(p => p.Classes, new BitProgressClassStyles { Track = "custom-track", Bar = "custom-bar" });
            parameters.Add(p => p.Styles, new BitProgressClassStyles { Track = "stroke: green;", Bar = "stroke: red;" });
        });

        var track = component.Find(".bit-prb-crt");
        Assert.IsTrue(track.ClassList.Contains("custom-track"));
        Assert.Contains("stroke: green;", track.GetAttribute("style")!);

        // The spinner circle takes the bar slot too, so a custom look survives the indeterminate swap.
        var bar = component.Find(".bit-prb-cri");
        Assert.IsTrue(bar.ClassList.Contains("custom-bar"));
        Assert.Contains("stroke: red;", bar.GetAttribute("style")!);
    }

    [TestMethod]
    public void BitProgressTwoInstancesShouldNotShareTheirLabelAndDescriptionIds()
    {
        var first = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Label, "Label");
            parameters.Add(p => p.Description, "Description");
        });
        var second = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Label, "Label");
            parameters.Add(p => p.Description, "Description");
        });

        Assert.AreNotEqual(first.Find(".bit-prb-lbl").Id, second.Find(".bit-prb-lbl").Id);
        Assert.AreNotEqual(first.Find(".bit-prb-des").Id, second.Find(".bit-prb-des").Id);
    }

    [TestMethod]
    public void BitProgressShapeOnlyClassesShouldFollowAChangeOfShape()
    {
        // Segments, the vertical orientation and the gauge gap each apply to one shape only, so the
        // class list has to be rebuilt when the shape itself changes.
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Segments, 5);
            parameters.Add(p => p.Vertical, true);
            parameters.Add(p => p.GapDegree, 90);
        });

        var root = component.Find(".bit-prb");
        Assert.IsTrue(root.ClassList.Contains("bit-prb-seg"));
        Assert.IsTrue(root.ClassList.Contains("bit-prb-ver"));
        Assert.IsFalse(root.ClassList.Contains("bit-prb-gap"));

        component.Render(parameters => parameters.Add(p => p.Circular, true));

        root = component.Find(".bit-prb");
        Assert.IsFalse(root.ClassList.Contains("bit-prb-seg"));
        Assert.IsFalse(root.ClassList.Contains("bit-prb-ver"));
        Assert.IsTrue(root.ClassList.Contains("bit-prb-gap"));
    }

    [TestMethod]
    public void BitProgressPercentShouldFollowAParameterChange()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Percent, 10);
        });

        Assert.Contains("width: 10%", component.Find(".bit-prb-bar").GetAttribute("style")!);

        component.Render(parameters => parameters.Add(p => p.Percent, 80));

        var bar = component.Find(".bit-prb-bar");
        Assert.Contains("width: 80%", bar.GetAttribute("style")!);
        Assert.AreEqual("80", bar.GetAttribute("aria-valuenow"));
    }

    [TestMethod]
    public void BitProgressSwitchingToIndeterminateShouldDropTheValue()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Percent, 80);
            parameters.Add(p => p.ShowPercentNumber, true);
        });

        Assert.AreEqual("80", component.Find(".bit-prb-bar").GetAttribute("aria-valuenow"));

        component.Render(parameters => parameters.Add(p => p.Indeterminate, true));

        Assert.IsNull(component.Find(".bit-prb-bar").GetAttribute("aria-valuenow"));
        Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-prb-pct"));
    }


    // ---------------------------------------------------------------- culture

    [TestMethod]
    public void BitProgressANullPercentNumberFormatShouldNotTakeTheRenderDown()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Percent, 42);
            parameters.Add(p => p.ShowPercentNumber, true);
            parameters.Add(p => p.PercentNumberFormat, null!);
        });

        Assert.AreEqual("42 %", component.Find(".bit-prb-pnm").TextContent);
    }

    [TestMethod]
    public void BitProgressANegativeRadiusShouldNotProduceANegativeSize()
    {
        var component = RenderComponent<BitProgress>(parameters =>
        {
            parameters.Add(p => p.Circular, true);
            parameters.Add(p => p.Radius, -6);
        });

        Assert.AreEqual("0px", component.Find(".bit-prb-cir").GetAttribute("width"));
    }

    [TestMethod]
    public void BitProgressCssNumbersShouldNotFollowTheCulture()
    {
        // A culture with a comma decimal separator would otherwise emit "width: 52,5%", which no
        // engine parses, and an aria value no assistive technology reads as a number.
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            var component = RenderComponent<BitProgress>(parameters =>
            {
                parameters.Add(p => p.Percent, 52.5);
                parameters.Add(p => p.Buffer, 62.5);
            });

            var bar = component.Find(".bit-prb-bar");
            Assert.Contains("width: 52.5%", bar.GetAttribute("style")!);
            Assert.AreEqual("52.5", bar.GetAttribute("aria-valuenow"));
            Assert.Contains("width: 62.5%", component.Find(".bit-prb-buf").GetAttribute("style")!);
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }
}
