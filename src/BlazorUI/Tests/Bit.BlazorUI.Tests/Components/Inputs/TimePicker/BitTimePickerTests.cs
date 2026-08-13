using System;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.TimePicker;

[TestClass]
public class BitTimePickerTests : BunitTestContext
{
    #region basics

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitTimePickerTest(bool isEnabled)
    {
        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var bitTimePicker = component.Find(".bit-tpc");

        if (isEnabled)
        {
            Assert.IsFalse(bitTimePicker.ClassList.Contains("bit-dis"));
        }
        else
        {
            Assert.IsTrue(bitTimePicker.ClassList.Contains("bit-dis"));
        }
    }

    [TestMethod,
        DataRow("<div>This is labelTemplate</div>")
    ]
    public void BitTimePickerShouldRenderLabelTemplate(string labelTemplate)
    {
        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.LabelTemplate, builder =>
            {
                builder.AddMarkupContent(0, labelTemplate);
            });
        });

        var labelChild = component.Find(".bit-tpc > label").ChildNodes;
        labelChild.MarkupMatches(labelTemplate);
    }

    [TestMethod]
    public void BitTimePickerShouldReferenceTheLabelFromTheInput()
    {
        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.Label, "my label");
        });

        var input = component.Find(".bit-tpc-inp");
        var label = component.Find(".bit-tpc-lbl");

        Assert.AreEqual(label.GetAttribute("id"), input.GetAttribute("aria-labelledby"));
    }

    [TestMethod]
    public void BitTimePickerShouldReferenceTheLabelTemplateFromTheInput()
    {
        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.LabelTemplate, builder => builder.AddMarkupContent(0, "<span>custom</span>"));
        });

        var input = component.Find(".bit-tpc-inp");
        var label = component.Find(".bit-tpc > label");

        Assert.AreEqual(label.GetAttribute("id"), input.GetAttribute("aria-labelledby"));
    }

    [TestMethod,
        DataRow("ClockAria")
    ]
    public void BitTimePickerShouldRespectCalloutAriaLabel(string calloutAriaLabel)
    {
        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.CalloutAriaLabel, calloutAriaLabel);
        });

        var callout = component.Find(".bit-tpc-cac");
        var ariaLabel = callout.GetAttribute("aria-label");

        Assert.AreEqual(calloutAriaLabel, ariaLabel);
    }

    [TestMethod,
        DataRow(true, 1),
        DataRow(false, 0)
    ]
    public void BitTimePickerShouldHandleOnClickEvent(bool isEnabled, int count)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var clickedValue = 0;

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.OnClick, () => clickedValue++);
        });

        var bitTimePickerInput = component.Find(".bit-tpc-wrp");
        bitTimePickerInput.Click();

        Assert.AreEqual(count, clickedValue);
    }

    [TestMethod,
        DataRow("HourGlass")
    ]
    public void BitTimePickerShouldAcceptIcon(string iconName)
    {
        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.IconName, iconName);
        });

        var icon = component.Find(".bit-tpc-ico");

        Assert.IsTrue(icon.ClassList.Contains($"bit-icon--{iconName}"));
    }

    [TestMethod,
        DataRow("Select a time..."),
        DataRow(null)
    ]
    public void BitTimePickerShouldRespectPlaceholder(string placeholder)
    {
        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.Placeholder, placeholder);
        });

        var input = component.Find(".bit-tpc-inp");

        if (placeholder is not null)
        {
            Assert.IsTrue(input.HasAttribute("placeholder"));
            Assert.AreEqual(placeholder, input.GetAttribute("placeholder"));
        }
        else
        {
            Assert.IsFalse(input.HasAttribute("placeholder"));
        }
    }

    [TestMethod]
    public void BitTimePickerShouldRenderStandaloneInput()
    {
        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.Standalone, true);
        });

        var input = component.Find("input.bit-input-hidden");

        Assert.IsNotNull(input);
        Assert.AreEqual("-1", input.GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitTimePickerShouldShowAmPmWhenTwelveHours()
    {
        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.TimeFormat, BitTimeFormat.TwelveHours);
        });

        var ampm = component.FindAll(".bit-tpc-am-pm");
        Assert.AreEqual(1, ampm.Count);
    }

    [TestMethod]
    public void BitTimePickerCalloutHtmlAttributesTest()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var calloutHtmlAttributes = new Dictionary<string, object>
        {
            { "style", "color: blue" }
        };

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.CalloutHtmlAttributes, calloutHtmlAttributes);
        });

        var callout = component.Find(".bit-tpc-cac");
        var calloutStyle = callout.GetAttribute("style");

        Assert.AreEqual("color: blue", calloutStyle);
    }

    [TestMethod]
    public async Task BitTimePickerDisposeShouldNotThrow()
    {
        var component = RenderComponent<BitTimePicker>(p =>
        {
            p.Add(x => x.Value, TimeSpan.Zero);
        });

        await component.Instance.DisposeAsync();
    }

    [TestMethod]
    public void BitTimePickerShouldRespectDefaultValue()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var defaultValue = new TimeSpan(10, 30, 0);

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, defaultValue);
        });

        Assert.AreEqual(defaultValue, component.Instance.Value);
    }

    #endregion



    #region formatting

    [TestMethod]
    public void BitTimePickerShouldFormatTheValueInTwentyFourHoursFormat()
    {
        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.Value, new TimeSpan(13, 45, 0));
        });

        Assert.AreEqual("13:45", component.Find(".bit-tpc-inp").GetAttribute("value"));
    }

    [TestMethod]
    public void BitTimePickerShouldFormatTheValueInTwelveHoursFormat()
    {
        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.TimeFormat, BitTimeFormat.TwelveHours);
            parameters.Add(p => p.Value, new TimeSpan(13, 45, 0));
        });

        Assert.AreEqual("01:45 PM", component.Find(".bit-tpc-inp").GetAttribute("value"));
    }

    [TestMethod]
    public void BitTimePickerShouldIncludeSecondsInTheFormatWhenShowSecondsIsSet()
    {
        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.ShowSeconds, true);
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.Value, new TimeSpan(13, 45, 30));
        });

        Assert.AreEqual("13:45:30", component.Find(".bit-tpc-inp").GetAttribute("value"));
    }

    [TestMethod]
    public void BitTimePickerShouldRespectCustomValueFormat()
    {
        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.ValueFormat, "hh-mm.ss");
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.Value, new TimeSpan(13, 45, 30));
        });

        Assert.AreEqual("01-45.30", component.Find(".bit-tpc-inp").GetAttribute("value"));
    }

    [TestMethod]
    public void BitTimePickerShouldTakeTheDefaultFormatFromTheCultureInTwentyFourHours()
    {
        // en-US writes times on a 12-hour clock ("h:mm tt"), so the 24-hour picker rewrites that pattern
        // rather than falling back to one of its own - and pads the hour it writes narrow, so the field
        // spells the time the same way the two-digit inputs of the callout do.
        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.GetCultureInfo("en-US"));
            parameters.Add(p => p.Value, new TimeSpan(9, 5, 0));
        });

        Assert.AreEqual("09:05", component.Find(".bit-tpc-inp").GetAttribute("value"));
    }

    [TestMethod]
    public void BitTimePickerShouldTakeTheDefaultFormatFromTheCultureInTwelveHours()
    {
        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.GetCultureInfo("en-US"));
            parameters.Add(p => p.TimeFormat, BitTimeFormat.TwelveHours);
            parameters.Add(p => p.Value, new TimeSpan(13, 45, 0));
        });

        Assert.AreEqual("01:45 PM", component.Find(".bit-tpc-inp").GetAttribute("value"));
    }

    [TestMethod]
    public void BitTimePickerShouldTakeTheSecondsFormatFromTheCulture()
    {
        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.ShowSeconds, true);
            parameters.Add(p => p.Culture, CultureInfo.GetCultureInfo("en-US"));
            parameters.Add(p => p.Value, new TimeSpan(13, 45, 30));
        });

        Assert.AreEqual("13:45:30", component.Find(".bit-tpc-inp").GetAttribute("value"));
    }

    [TestMethod]
    public void BitTimePickerShouldKeepTheSeparatorsOfTheCulture()
    {
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.DateTimeFormat.ShortTimePattern = "H.mm";

        TimeSpan? value = null;

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, culture);
            parameters.Add(p => p.AllowTextInput, true);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find(".bit-tpc-inp").Input("13.45");

        Assert.AreEqual(new TimeSpan(13, 45, 0), value);
        Assert.AreEqual("13.45", component.Find(".bit-tpc-inp").GetAttribute("value"));
    }

    [TestMethod]
    public void BitTimePickerShouldPadTheValueOfACultureThatWritesTheHourNarrow()
    {
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.DateTimeFormat.ShortTimePattern = "H:mm"; // en-US and fa-IR write the hour like this

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, culture);
            parameters.Add(p => p.Value, new TimeSpan(9, 5, 0));
        });

        // The separators and the order come from the culture, but the parts are padded: the field would
        // otherwise spell the time differently from the two-digit inputs of the callout.
        Assert.AreEqual("09:05", component.Find(".bit-tpc-inp").GetAttribute("value"));
        Assert.AreEqual("09", component.Find(".bit-tpc-tin").GetAttribute("value"));
    }

    [TestMethod]
    public void BitTimePickerShouldPadTheValueOfATwelveHourCultureThatWritesTheHourNarrow()
    {
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.DateTimeFormat.ShortTimePattern = "h:mm tt";

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, culture);
            parameters.Add(p => p.TimeFormat, BitTimeFormat.TwelveHours);
            parameters.Add(p => p.Value, new TimeSpan(9, 5, 0));
        });

        Assert.AreEqual("09:05 AM", component.Find(".bit-tpc-inp").GetAttribute("value"));
    }

    [TestMethod]
    public void BitTimePickerShouldReadATypedTimeWithoutTheLeadingZeros()
    {
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.DateTimeFormat.ShortTimePattern = "H:mm";

        TimeSpan? value = null;

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, culture);
            parameters.Add(p => p.AllowTextInput, true);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        // The field is written padded, but the padded pattern would accept nothing but a padded value,
        // so the narrow one is read with as well - and the time is rewritten padded afterwards.
        component.Find(".bit-tpc-inp").Input("9:05");

        Assert.AreEqual(new TimeSpan(9, 5, 0), value);
        Assert.AreEqual("09:05", component.Find(".bit-tpc-inp").GetAttribute("value"));
    }

    [TestMethod]
    public void BitTimePickerShouldNotRewriteTheQuotedLiteralsOfThePatternOfTheCulture()
    {
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.DateTimeFormat.ShortTimePattern = "HH'h'mm";

        // The 'h' is a literal, not the hour specifier of a 12-hour culture, so the conversion to the
        // 12-hour clock has to leave it where it is.
        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, culture);
            parameters.Add(p => p.TimeFormat, BitTimeFormat.TwelveHours);
            parameters.Add(p => p.Value, new TimeSpan(13, 45, 0));
        });

        Assert.AreEqual("01h45 PM", component.Find(".bit-tpc-inp").GetAttribute("value"));
    }

    [TestMethod]
    public void BitTimePickerShouldNormalizeAValueOutsideOfADay()
    {
        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.Value, TimeSpan.FromHours(25));
        });

        Assert.AreEqual("01:00", component.Find(".bit-tpc-inp").GetAttribute("value"));
        Assert.AreEqual("01", component.FindAll(".bit-tpc-tin")[0].GetAttribute("value"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.Value, TimeSpan.FromHours(-1));
        });

        Assert.AreEqual("23:00", component.Find(".bit-tpc-inp").GetAttribute("value"));
        Assert.AreEqual("23", component.FindAll(".bit-tpc-tin")[0].GetAttribute("value"));
    }

    [TestMethod]
    public void BitTimePickerShouldUseTheCustomCultureToFormatAndRenderTheDesignators()
    {
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.DateTimeFormat.AMDesignator = "aa";
        culture.DateTimeFormat.PMDesignator = "pp";

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, culture);
            parameters.Add(p => p.TimeFormat, BitTimeFormat.TwelveHours);
            parameters.Add(p => p.Value, new TimeSpan(13, 45, 0));
        });

        Assert.AreEqual("01:45 pp", component.Find(".bit-tpc-inp").GetAttribute("value"));
        Assert.AreEqual("aa", component.Find(".bit-tpc-bam").TextContent.Trim());
        Assert.AreEqual("pp", component.Find(".bit-tpc-bpm").TextContent.Trim());
    }

    #endregion



    #region text input

    [TestMethod]
    public void BitTimePickerShouldParseATypedTime()
    {
        TimeSpan? value = null;

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.AllowTextInput, true);
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find(".bit-tpc-inp").Input("14:30");

        Assert.AreEqual(new TimeSpan(14, 30, 0), value);
    }

    [TestMethod]
    public void BitTimePickerShouldNotParseAnInvalidTypedTime()
    {
        TimeSpan? value = null;

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.AllowTextInput, true);
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find(".bit-tpc-inp").Input("not a time");

        Assert.IsNull(value);
    }

    [TestMethod]
    public void BitTimePickerShouldParseATimeTypedInTheOtherClockFormat()
    {
        TimeSpan? value = null;

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.AllowTextInput, true);
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find(".bit-tpc-inp").Input("02:30 PM");

        Assert.AreEqual(new TimeSpan(14, 30, 0), value);
    }

    [TestMethod]
    public void BitTimePickerShouldParseATypedTimeWithTheSecondsItDoesNotShow()
    {
        TimeSpan? value = null;

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.AllowTextInput, true);
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find(".bit-tpc-inp").Input("14:30:45");

        Assert.AreEqual(new TimeSpan(14, 30, 45), value);
    }

    [TestMethod]
    public void BitTimePickerShouldOnlyParseTheCustomValueFormat()
    {
        TimeSpan? value = null;

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.AllowTextInput, true);
            parameters.Add(p => p.ValueFormat, "hh-mm.ss");
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find(".bit-tpc-inp").Input("13:45");
        Assert.IsNull(value);

        component.Find(".bit-tpc-inp").Input("01-45.30");
        Assert.AreEqual(new TimeSpan(1, 45, 30), value);
    }

    [TestMethod]
    public void BitTimePickerShouldIgnoreTypedTextWhenTextInputIsNotAllowed()
    {
        TimeSpan? value = null;

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find(".bit-tpc-inp").Input("14:30");

        Assert.IsNull(value);
        Assert.IsTrue(component.Find(".bit-tpc-inp").HasAttribute("readonly"));
    }

    #endregion



    #region spin buttons

    [TestMethod]
    public void BitTimePickerShouldIncreaseAndDecreaseTheHour()
    {
        TimeSpan? value = new TimeSpan(10, 30, 0);

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find("button[title='Increase hour']").Click(new MouseEventArgs());
        Assert.AreEqual(new TimeSpan(11, 30, 0), value);

        component.Find("button[title='Decrease hour']").Click(new MouseEventArgs());
        component.Find("button[title='Decrease hour']").Click(new MouseEventArgs());
        Assert.AreEqual(new TimeSpan(9, 30, 0), value);
    }

    [TestMethod]
    public void BitTimePickerShouldIncreaseAndDecreaseTheMinute()
    {
        TimeSpan? value = new TimeSpan(10, 30, 0);

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find("button[title='Increase minute']").Click(new MouseEventArgs());
        Assert.AreEqual(new TimeSpan(10, 31, 0), value);

        component.Find("button[title='Decrease minute']").Click(new MouseEventArgs());
        component.Find("button[title='Decrease minute']").Click(new MouseEventArgs());
        Assert.AreEqual(new TimeSpan(10, 29, 0), value);
    }

    [TestMethod]
    public void BitTimePickerShouldIncreaseAndDecreaseTheSecond()
    {
        TimeSpan? value = new TimeSpan(10, 30, 30);

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.ShowSeconds, true);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find("button[title='Increase second']").Click(new MouseEventArgs());
        Assert.AreEqual(new TimeSpan(10, 30, 31), value);

        component.Find("button[title='Decrease second']").Click(new MouseEventArgs());
        component.Find("button[title='Decrease second']").Click(new MouseEventArgs());
        Assert.AreEqual(new TimeSpan(10, 30, 29), value);
    }

    [TestMethod]
    public void BitTimePickerShouldWrapTheHourAndTheMinuteAround()
    {
        TimeSpan? value = new TimeSpan(23, 59, 0);

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find("button[title='Increase hour']").Click(new MouseEventArgs());
        Assert.AreEqual(new TimeSpan(0, 59, 0), value);

        component.Find("button[title='Decrease hour']").Click(new MouseEventArgs());
        Assert.AreEqual(new TimeSpan(23, 59, 0), value);

        component.Find("button[title='Increase minute']").Click(new MouseEventArgs());
        Assert.AreEqual(new TimeSpan(23, 0, 0), value);

        component.Find("button[title='Decrease minute']").Click(new MouseEventArgs());
        Assert.AreEqual(new TimeSpan(23, 59, 0), value);
    }

    [TestMethod,
        DataRow(2, 15, 30)
    ]
    public void BitTimePickerShouldRespectTheSteps(int hourStep, int minuteStep, int secondStep)
    {
        TimeSpan? value = new TimeSpan(10, 30, 0);

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.ShowSeconds, true);
            parameters.Add(p => p.HourStep, hourStep);
            parameters.Add(p => p.MinuteStep, minuteStep);
            parameters.Add(p => p.SecondStep, secondStep);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find("button[title='Increase hour']").Click(new MouseEventArgs());
        Assert.AreEqual(new TimeSpan(12, 30, 0), value);

        component.Find("button[title='Increase minute']").Click(new MouseEventArgs());
        Assert.AreEqual(new TimeSpan(12, 45, 0), value);

        component.Find("button[title='Increase second']").Click(new MouseEventArgs());
        Assert.AreEqual(new TimeSpan(12, 45, 30), value);
    }

    [TestMethod]
    public void BitTimePickerShouldMoveAValueOffTheGridOntoIt()
    {
        TimeSpan? value = new TimeSpan(10, 7, 0);

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.MinuteStep, 15);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        // The step lays a grid over the hour rather than adding itself to whatever the minute happens to
        // be, so a bound value that sits between two grid points moves onto the next one, not 15 past it.
        component.Find("button[title='Increase minute']").Click(new MouseEventArgs());
        Assert.AreEqual(new TimeSpan(10, 15, 0), value);

        component.Find("button[title='Decrease minute']").Click(new MouseEventArgs());
        Assert.AreEqual(new TimeSpan(10, 0, 0), value);
    }

    [TestMethod]
    public void BitTimePickerShouldStayOnTheGridWhenTheStepDoesNotDivideTheRange()
    {
        TimeSpan? value = new TimeSpan(20, 0, 0);

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.HourStep, 5);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        // The grid is 0, 5, 10, 15, 20 and the four hours above 20 are not on it, so the hour wraps to the
        // top of the grid rather than to the 25th hour of a 24-hour day.
        component.Find("button[title='Increase hour']").Click(new MouseEventArgs());

        Assert.AreEqual(new TimeSpan(0, 0, 0), value);
    }

    [TestMethod]
    public void BitTimePickerShouldAnchorTheGridAtTheMinTime()
    {
        TimeSpan? value = new TimeSpan(9, 7, 0);

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.MinTime, new TimeSpan(9, 7, 0));
            parameters.Add(p => p.MinuteStep, 15);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        // The grid starts at the minute the range starts at, so the range's own first time is on it: a grid
        // pinned to the top of the hour would leave 09:07 unreachable in a picker that begins there.
        component.Find("button[title='Increase minute']").Click(new MouseEventArgs());
        Assert.AreEqual(new TimeSpan(9, 22, 0), value);

        component.Find("button[title='Decrease minute']").Click(new MouseEventArgs());
        Assert.AreEqual(new TimeSpan(9, 7, 0), value);
    }

    [TestMethod]
    public void BitTimePickerShouldNotHoldATypedTimeToTheGrid()
    {
        TimeSpan? value = null;

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.AllowTextInput, true);
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.MinuteStep, 15);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        // A step is the granularity the controls move in, not a constraint on the value: a person who
        // types 09:07 into a quarter-hour picker means 09:07.
        component.Find(".bit-tpc-inp").Input("09:07");

        Assert.AreEqual(new TimeSpan(9, 7, 0), value);
    }

    [TestMethod,
        DataRow(0),
        DataRow(-5)
    ]
    public void BitTimePickerShouldTreatANonPositiveStepAsOne(int step)
    {
        TimeSpan? value = new TimeSpan(10, 30, 0);

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.HourStep, step);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find("button[title='Increase hour']").Click(new MouseEventArgs());

        Assert.AreEqual(new TimeSpan(11, 30, 0), value);
    }

    [TestMethod]
    public void BitTimePickerShouldProduceAValueFromAnEmptyStateOnStep()
    {
        TimeSpan? value = null;

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find("button[title='Increase hour']").Click(new MouseEventArgs());

        Assert.AreEqual(new TimeSpan(1, 0, 0), value);
    }

    [TestMethod]
    public async Task BitTimePickerShouldStepWithAPointerPressAndStopOnRelease()
    {
        TimeSpan? value = new TimeSpan(10, 30, 0);

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        var button = component.Find("button[title='Increase hour']");

        var downTask = button.TriggerEventAsync("onpointerdown", new PointerEventArgs());

        await button.TriggerEventAsync("onpointerup", new PointerEventArgs());

        Assert.AreSame(downTask, await Task.WhenAny(downTask, Task.Delay(5000)), "The press did not stop stepping after the release.");
        await downTask;

        Assert.IsTrue(value >= new TimeSpan(11, 30, 0));

        // The loop steps every 75ms, so a value that is still the same well after the release is one nothing is stepping anymore.
        var valueOnRelease = value;
        await Task.Delay(300);
        Assert.AreEqual(valueOnRelease, value);
    }

    [TestMethod]
    public async Task BitTimePickerShouldNotStartTheContinuousSpinBeforeTheContinuousSpinDelay()
    {
        TimeSpan? value = new TimeSpan(10, 30, 0);

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            // Longer than the press below, so the held button contributes the one step every press makes
            // and the continuous spin never starts.
            parameters.Add(p => p.ContinuousSpinDelay, 60_000);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find("button[title='Increase hour']").PointerDown();

        await Task.Delay(600);

        Assert.AreEqual(new TimeSpan(11, 30, 0), value);

        component.Find("button[title='Increase hour']").PointerUp();
    }

    [TestMethod]
    public void BitTimePickerShouldRespectCustomSpinButtonTitles()
    {
        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.IncreaseHourTitle, "more hours");
            parameters.Add(p => p.DecreaseMinuteTitle, "less minutes");
        });

        var increaseHour = component.Find("button[title='more hours']");
        Assert.AreEqual("more hours", increaseHour.GetAttribute("aria-label"));

        var decreaseMinute = component.Find("button[title='less minutes']");
        Assert.AreEqual("less minutes", decreaseMinute.GetAttribute("aria-label"));
    }

    #endregion



    #region time inputs

    [TestMethod]
    public void BitTimePickerShouldRenderTheSecondsInputOnlyWhenShowSecondsIsSet()
    {
        var component = RenderComponent<BitTimePicker>();

        Assert.AreEqual(2, component.FindAll(".bit-tpc-tin").Count);

        component.Render(parameters =>
        {
            parameters.Add(p => p.ShowSeconds, true);
        });

        Assert.AreEqual(3, component.FindAll(".bit-tpc-tin").Count);
    }

    [TestMethod]
    public void BitTimePickerShouldApplyATypedHourInTwentyFourHoursFormat()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        TimeSpan? value = new TimeSpan(10, 30, 0);

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.FindAll(".bit-tpc-tin")[0].Input("18");

        Assert.AreEqual(new TimeSpan(18, 30, 0), value);
    }

    [TestMethod]
    public void BitTimePickerShouldClampATypedOutOfRangeHour()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        TimeSpan? value = new TimeSpan(10, 30, 0);

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.FindAll(".bit-tpc-tin")[0].Input("99");

        Assert.AreEqual(new TimeSpan(23, 30, 0), value);
    }

    [TestMethod]
    public void BitTimePickerShouldKeepTheMeridiemOfATypedHourInTwelveHoursFormat()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        TimeSpan? value = new TimeSpan(17, 30, 0);

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.TimeFormat, BitTimeFormat.TwelveHours);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.FindAll(".bit-tpc-tin")[0].Input("6");

        Assert.AreEqual(new TimeSpan(18, 30, 0), value);
    }

    [TestMethod]
    public void BitTimePickerShouldReadATypedTwelveAsTheTopOfTheHalf()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        TimeSpan? value = new TimeSpan(9, 30, 0);

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.TimeFormat, BitTimeFormat.TwelveHours);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.FindAll(".bit-tpc-tin")[0].Input("12");

        Assert.AreEqual(new TimeSpan(0, 30, 0), value);
    }

    [TestMethod]
    public void BitTimePickerShouldApplyATypedMinute()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        TimeSpan? value = new TimeSpan(10, 30, 0);

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.FindAll(".bit-tpc-tin")[1].Input("55");

        Assert.AreEqual(new TimeSpan(10, 55, 0), value);
    }

    [TestMethod]
    public void BitTimePickerShouldRespectCustomInputAriaLabels()
    {
        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.ShowSeconds, true);
            parameters.Add(p => p.HourInputAriaLabel, "the hour");
            parameters.Add(p => p.MinuteInputAriaLabel, "the minute");
            parameters.Add(p => p.SecondInputAriaLabel, "the second");
        });

        var inputs = component.FindAll(".bit-tpc-tin");

        Assert.AreEqual("the hour", inputs[0].GetAttribute("aria-label"));
        Assert.AreEqual("the minute", inputs[1].GetAttribute("aria-label"));
        Assert.AreEqual("the second", inputs[2].GetAttribute("aria-label"));
    }

    #endregion



    #region am/pm

    [TestMethod]
    public void BitTimePickerShouldMoveTheValueAcrossTheMeridiem()
    {
        TimeSpan? value = new TimeSpan(17, 30, 0);

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.TimeFormat, BitTimeFormat.TwelveHours);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find(".bit-tpc-bam").Click();
        Assert.AreEqual(new TimeSpan(5, 30, 0), value);

        component.Find(".bit-tpc-bpm").Click();
        Assert.AreEqual(new TimeSpan(17, 30, 0), value);
    }

    [TestMethod]
    public void BitTimePickerShouldMoveNoonAndMidnightAcrossTheMeridiem()
    {
        TimeSpan? value = new TimeSpan(12, 0, 0);

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.TimeFormat, BitTimeFormat.TwelveHours);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find(".bit-tpc-bam").Click();
        Assert.AreEqual(new TimeSpan(0, 0, 0), value);

        component.Find(".bit-tpc-bpm").Click();
        Assert.AreEqual(new TimeSpan(12, 0, 0), value);
    }

    [TestMethod]
    public void BitTimePickerShouldKeepTheHourWhenClickingTheMeridiemItIsAlreadyIn()
    {
        TimeSpan? value = new TimeSpan(12, 30, 0);

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.TimeFormat, BitTimeFormat.TwelveHours);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find(".bit-tpc-bpm").Click();
        Assert.AreEqual(new TimeSpan(12, 30, 0), value);

        component.Find(".bit-tpc-bam").Click();
        Assert.AreEqual(new TimeSpan(0, 30, 0), value);

        component.Find(".bit-tpc-bam").Click();
        Assert.AreEqual(new TimeSpan(0, 30, 0), value);
    }

    [TestMethod]
    public void BitTimePickerShouldIgnoreAMeridiemClickOnAnEmptyValue()
    {
        TimeSpan? value = null;
        var selectCount = 0;

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.TimeFormat, BitTimeFormat.TwelveHours);
            parameters.Bind(p => p.Value, value, v => value = v);
            parameters.Add(p => p.OnSelectTime, (TimeSpan? _) => selectCount++);
        });

        component.Find(".bit-tpc-bpm").Click();
        component.Find(".bit-tpc-bam").Click();

        Assert.IsNull(value);
        Assert.AreEqual(0, selectCount);
    }

    [TestMethod]
    public void BitTimePickerShouldMarkTheCurrentMeridiem()
    {
        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.TimeFormat, BitTimeFormat.TwelveHours);
            parameters.Add(p => p.Value, new TimeSpan(17, 30, 0));
        });

        Assert.AreEqual("false", component.Find(".bit-tpc-bam").GetAttribute("aria-pressed"));
        Assert.AreEqual("true", component.Find(".bit-tpc-bpm").GetAttribute("aria-pressed"));
        Assert.IsTrue(component.Find(".bit-tpc-bpm").ClassList.Contains("bit-tpc-bns"));
        Assert.IsFalse(component.Find(".bit-tpc-bam").ClassList.Contains("bit-tpc-bns"));
    }

    #endregion



    #region min/max

    [TestMethod]
    public void BitTimePickerShouldStepTheHourOnlyToAnHourInsideTheBounds()
    {
        TimeSpan? value = new TimeSpan(10, 0, 0);

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.MaxTime, new TimeSpan(10, 30, 0));
            parameters.Add(p => p.MinTime, new TimeSpan(9, 45, 0));
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        // Only the hours 9 and 10 are inside the range, so the hour wraps between the two of them; the
        // minute follows because a bound that falls inside an hour leaves a different set of minutes
        // selectable in it - 09:00 is not a time this picker can hold at all.
        component.Find("button[title='Increase hour']").Click(new MouseEventArgs());
        Assert.AreEqual(new TimeSpan(9, 45, 0), value);

        component.Find("button[title='Decrease hour']").Click(new MouseEventArgs());
        Assert.AreEqual(new TimeSpan(10, 30, 0), value);
    }

    [TestMethod]
    public void BitTimePickerShouldNotMoveTheMinuteWhenTheHourItStepsToLeavesItSelectable()
    {
        TimeSpan? value = new TimeSpan(9, 30, 0);

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.MinTime, new TimeSpan(9, 0, 0));
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        // The hour button moves the hour and nothing else: 8 is out of the range so the hour wraps to 23,
        // where the minute it was on is still selectable and therefore left alone.
        component.Find("button[title='Decrease hour']").Click(new MouseEventArgs());

        Assert.AreEqual(new TimeSpan(23, 30, 0), value);
    }

    [TestMethod]
    public void BitTimePickerShouldRejectATypedTimeOutsideOfTheBounds()
    {
        TimeSpan? value = null;

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.AllowTextInput, true);
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.MinTime, new TimeSpan(8, 0, 0));
            parameters.Add(p => p.MaxTime, new TimeSpan(17, 0, 0));
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find(".bit-tpc-inp").Input("19:00");
        Assert.IsNull(value);

        component.Find(".bit-tpc-inp").Input("09:00");
        Assert.AreEqual(new TimeSpan(9, 0, 0), value);
    }

    [TestMethod]
    public void BitTimePickerShouldPullBoundsOutsideOfADayBackIntoIt()
    {
        TimeSpan? value = new TimeSpan(23, 0, 0);

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.MaxTime, new TimeSpan(25, 0, 0));
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find("button[title='Increase hour']").Click(new MouseEventArgs());

        Assert.AreEqual(new TimeSpan(0, 0, 0), value);
    }

    [TestMethod]
    public void BitTimePickerShouldTakeTheCurrentTimeAsABoundWhenThePastIsDisabled()
    {
        TimeSpan? value = null;

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.DisablePast, true);
            parameters.Add(p => p.AllowTextInput, true);
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        var now = DateTime.Now.TimeOfDay;

        // Midnight is in the past on every run but the first minute of the day, which is the one moment the
        // bound lets it through - so the check follows the same rule the component does.
        var isPast = new TimeSpan(0, 0, 0) < new TimeSpan(now.Hours, now.Minutes, 0);

        component.Find(".bit-tpc-inp").Input("00:00");

        if (isPast)
        {
            Assert.IsNull(value);
        }
        else
        {
            Assert.AreEqual(TimeSpan.Zero, value);
        }

        // Stepping cannot leave the bound behind either: the value is pulled up to the current time.
        component.Find("button[title='Increase hour']").Click(new MouseEventArgs());

        Assert.IsNotNull(value);
        Assert.IsTrue(value >= new TimeSpan(now.Hours, now.Minutes, 0), $"{value} is before the current time.");
    }

    [TestMethod]
    public void BitTimePickerShouldTakeTheCurrentTimeAsABoundWhenTheFutureIsDisabled()
    {
        TimeSpan? value = null;

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.DisableFuture, true);
            parameters.Add(p => p.AllowTextInput, true);
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find(".bit-tpc-inp").Input("23:59");

        var now = DateTime.Now.TimeOfDay;

        if (new TimeSpan(23, 59, 0) > new TimeSpan(now.Hours, now.Minutes, 0))
        {
            Assert.IsNull(value);
        }
        else
        {
            Assert.AreEqual(new TimeSpan(23, 59, 0), value);
        }
    }

    [TestMethod]
    public void BitTimePickerShouldKeepTheNarrowerOfTheBoundAndTheDisabledHalf()
    {
        TimeSpan? value = null;

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.DisablePast, true);
            parameters.Add(p => p.MinTime, new TimeSpan(23, 59, 0));
            parameters.Add(p => p.AllowTextInput, true);
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        // The later of the two bounds wins, and 23:59 is the latest minute of the day: nothing before it is
        // selectable whatever the time of the run is.
        component.Find(".bit-tpc-inp").Input("23:58");
        Assert.IsNull(value);

        component.Find(".bit-tpc-inp").Input("23:59");
        Assert.AreEqual(new TimeSpan(23, 59, 0), value);
    }

    [TestMethod]
    public void BitTimePickerShouldSelectTheCurrentTimeWithTheNowButtonWhenThePastIsDisabled()
    {
        TimeSpan? value = null;

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.DisablePast, true);
            parameters.Add(p => p.ShowNowButton, true);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        var before = DateTime.Now.TimeOfDay;
        component.Find(".bit-tpc-abn").Click();

        Assert.IsNotNull(value);
        Assert.IsTrue(value >= new TimeSpan(before.Hours, before.Minutes, 0), $"{value} is before the current time.");
    }

    [TestMethod]
    public void BitTimePickerShouldSkipDisallowedValuesWhenStepping()
    {
        TimeSpan? value = new TimeSpan(10, 30, 0);

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.AllowedHours, h => h % 2 == 0);
            parameters.Add(p => p.AllowedMinutes, m => m % 15 == 0);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find("button[title='Increase hour']").Click(new MouseEventArgs());
        Assert.AreEqual(new TimeSpan(12, 30, 0), value);

        component.Find("button[title='Increase minute']").Click(new MouseEventArgs());
        Assert.AreEqual(new TimeSpan(12, 45, 0), value);

        component.Find("button[title='Decrease minute']").Click(new MouseEventArgs());
        component.Find("button[title='Decrease minute']").Click(new MouseEventArgs());
        Assert.AreEqual(new TimeSpan(12, 15, 0), value);
    }

    [TestMethod]
    public void BitTimePickerShouldNotMoveWhenNoValueIsAllowed()
    {
        TimeSpan? value = new TimeSpan(10, 30, 0);

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.AllowedHours, _ => false);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find("button[title='Increase hour']").Click(new MouseEventArgs());

        Assert.AreEqual(new TimeSpan(10, 30, 0), value);
    }

    [TestMethod]
    public void BitTimePickerShouldSnapATypedValueToTheNearestAllowedOne()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        TimeSpan? value = new TimeSpan(10, 30, 0);

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.AllowedMinutes, m => m % 15 == 0);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.FindAll(".bit-tpc-tin")[1].Input("20");

        Assert.AreEqual(new TimeSpan(10, 15, 0), value);
    }

    [TestMethod]
    public void BitTimePickerShouldSnapATypedHourInsideItsHalfOfTheDay()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        TimeSpan? value = new TimeSpan(14, 30, 0);

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.TimeFormat, BitTimeFormat.TwelveHours);
            parameters.Add(p => p.AllowedHours, h => h % 2 == 0);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.FindAll(".bit-tpc-tin")[0].Input("5");

        Assert.AreEqual(new TimeSpan(16, 30, 0), value);
    }

    [TestMethod]
    public void BitTimePickerShouldCarryOnPastTheCurrentValueWhenSnapping()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        // The arrow keys of a number input step by one, so with a sparser set of allowed values the value
        // they land on is refused - and the nearest allowed one is the value they just left. The snap has to
        // carry on in the direction of the change rather than leave the input stuck where it was.
        TimeSpan? value = new TimeSpan(10, 30, 0);

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.AllowedHours, h => h % 2 == 0);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.FindAll(".bit-tpc-tin")[0].Input("11");
        Assert.AreEqual(new TimeSpan(12, 30, 0), value);

        component.FindAll(".bit-tpc-tin")[0].Input("11");
        Assert.AreEqual(new TimeSpan(10, 30, 0), value);
    }

    [TestMethod]
    public void BitTimePickerShouldStillSnapATypedValueToTheNearestAllowedOne()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        // A jump of more than one is a typed value rather than a step, so it lands on the nearest allowed
        // value even when that is on the way back to where it came from.
        TimeSpan? value = new TimeSpan(10, 30, 0);

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.AllowedMinutes, m => m % 15 == 0);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.FindAll(".bit-tpc-tin")[1].Input("32");

        Assert.AreEqual(new TimeSpan(10, 30, 0), value);
    }

    [TestMethod]
    public void BitTimePickerShouldRejectATypedTimeOnADisallowedValue()
    {
        TimeSpan? value = null;

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.AllowTextInput, true);
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.AllowedMinutes, m => m % 15 == 0);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find(".bit-tpc-inp").Input("10:20");
        Assert.IsNull(value);

        component.Find(".bit-tpc-inp").Input("10:30");
        Assert.AreEqual(new TimeSpan(10, 30, 0), value);
    }

    #endregion



    #region now & clear buttons

    [TestMethod]
    public void BitTimePickerShouldSelectTheCurrentTimeWithTheNowButton()
    {
        TimeSpan? value = null;

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.ShowNowButton, true);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        var before = DateTime.Now.TimeOfDay;
        component.Find(".bit-tpc-abn").Click();
        var after = DateTime.Now.TimeOfDay;

        Assert.IsNotNull(value);
        Assert.IsTrue(value >= new TimeSpan(before.Hours, before.Minutes, 0));
        Assert.IsTrue(value <= new TimeSpan(after.Hours, after.Minutes, 0) + TimeSpan.FromMinutes(1));
        Assert.AreEqual(0, value.Value.Seconds);
    }

    [TestMethod]
    public void BitTimePickerShouldClearTheValueWithTheClearButton()
    {
        TimeSpan? value = new TimeSpan(10, 30, 0);

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.ShowClearButton, true);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        var clearButton = component.Find(".bit-tpc-abn");
        Assert.IsFalse(clearButton.HasAttribute("disabled"));

        clearButton.Click();

        Assert.IsNull(value);
        Assert.IsTrue(component.Find(".bit-tpc-abn").HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitTimePickerShouldSnapTheNowButtonToTheSteps()
    {
        TimeSpan? value = null;

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.ShowNowButton, true);
            parameters.Add(p => p.MinuteStep, 15);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find(".bit-tpc-abn").Click();

        Assert.IsNotNull(value);
        Assert.AreEqual(0, value.Value.Minutes % 15);
    }

    [TestMethod]
    public void BitTimePickerShouldSnapTheNowButtonToTheAllowedValues()
    {
        TimeSpan? value = null;

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.ShowNowButton, true);
            parameters.Add(p => p.AllowedMinutes, m => m % 30 == 0);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find(".bit-tpc-abn").Click();

        Assert.IsNotNull(value);
        Assert.AreEqual(0, value.Value.Minutes % 30);
    }

    [TestMethod]
    public void BitTimePickerShouldRaiseOnClearWithTheClearButton()
    {
        TimeSpan? value = new TimeSpan(10, 30, 0);
        var clearCount = 0;

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.ShowClearButton, true);
            parameters.Add(p => p.OnClear, () => clearCount++);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find(".bit-tpc-abn").Click();

        Assert.IsNull(value);
        Assert.AreEqual(1, clearCount);
    }

    [TestMethod]
    public void BitTimePickerShouldRespectCustomActionButtonTexts()
    {
        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.ShowNowButton, true);
            parameters.Add(p => p.ShowClearButton, true);
            parameters.Add(p => p.NowButtonText, "current");
            parameters.Add(p => p.ClearButtonText, "reset");
        });

        var buttons = component.FindAll(".bit-tpc-abn");

        Assert.AreEqual(2, buttons.Count);
        Assert.AreEqual("current", buttons[0].TextContent.Trim());
        Assert.AreEqual("reset", buttons[1].TextContent.Trim());
    }

    [TestMethod]
    public void BitTimePickerShouldNotRenderTheActionsRowWithoutTheButtons()
    {
        var component = RenderComponent<BitTimePicker>();

        Assert.AreEqual(0, component.FindAll(".bit-tpc-act").Count);
    }

    #endregion



    #region readonly & disabled

    [TestMethod]
    public void BitTimePickerShouldDisableEveryChangePathWhenReadOnly()
    {
        TimeSpan? value = new TimeSpan(10, 30, 0);

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.ShowNowButton, true);
            parameters.Add(p => p.TimeFormat, BitTimeFormat.TwelveHours);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        Assert.IsTrue(component.Find("button[title='Increase hour']").HasAttribute("disabled"));
        Assert.IsTrue(component.Find(".bit-tpc-bam").HasAttribute("disabled"));
        Assert.IsTrue(component.Find(".bit-tpc-abn").HasAttribute("disabled"));
        Assert.IsTrue(component.Find(".bit-tpc-inp").HasAttribute("readonly"));
        Assert.AreEqual("true", component.Find(".bit-tpc-inp").GetAttribute("aria-readonly"));

        component.Find("button[title='Increase hour']").Click(new MouseEventArgs());
        Assert.AreEqual(new TimeSpan(10, 30, 0), value);
    }

    [TestMethod]
    public void BitTimePickerShouldNotChangeTheValueOfATypedHourWhenReadOnly()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        TimeSpan? value = new TimeSpan(10, 30, 0);

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.FindAll(".bit-tpc-tin")[0].Input("18");

        Assert.AreEqual(new TimeSpan(10, 30, 0), value);
    }

    #endregion



    #region open/close

    [TestMethod]
    public void BitTimePickerShouldOpenOnClickAndReportIt()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var isOpen = false;
        var openCount = 0;

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.OnOpen, () => openCount++);
        });

        Assert.AreEqual("false", component.Find(".bit-tpc-inp").GetAttribute("aria-expanded"));

        component.Find(".bit-tpc-wrp").Click();

        Assert.IsTrue(isOpen);
        Assert.AreEqual(1, openCount);
        Assert.AreEqual("true", component.Find(".bit-tpc-inp").GetAttribute("aria-expanded"));
    }

    [TestMethod]
    public void BitTimePickerShouldCloseWithEscapeAndReportIt()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var isOpen = false;
        var closeCount = 0;

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.OnClose, () => closeCount++);
        });

        component.Find(".bit-tpc-wrp").Click();
        Assert.IsTrue(isOpen);

        component.Find(".bit-tpc-inp").KeyDown("Escape");

        Assert.IsFalse(isOpen);
        Assert.AreEqual(1, closeCount);
    }

    [TestMethod]
    public void BitTimePickerShouldOpenWithTheArrowKeys()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var isOpen = false;

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        component.Find(".bit-tpc-inp").KeyDown("ArrowDown");

        Assert.IsTrue(isOpen);
    }

    [TestMethod]
    public void BitTimePickerShouldCloseWithAltArrowUp()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var isOpen = false;

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        component.Find(".bit-tpc-inp").KeyDown(new KeyboardEventArgs { Key = "ArrowDown", AltKey = true });
        Assert.IsTrue(isOpen);

        component.Find(".bit-tpc-inp").KeyDown(new KeyboardEventArgs { Key = "ArrowUp", AltKey = true });
        Assert.IsFalse(isOpen);
    }

    [TestMethod]
    public void BitTimePickerShouldRenderTheRangeOfEachTimeInput()
    {
        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.HourStep, 2);
            parameters.Add(p => p.MinuteStep, 15);
            parameters.Add(p => p.SecondStep, 30);
            parameters.Add(p => p.ShowSeconds, true);
        });

        var inputs = component.FindAll(".bit-tpc-tin");

        Assert.AreEqual("0", inputs[0].GetAttribute("min"));
        Assert.AreEqual("23", inputs[0].GetAttribute("max"));
        Assert.AreEqual("0", inputs[1].GetAttribute("min"));
        Assert.AreEqual("59", inputs[1].GetAttribute("max"));
        Assert.AreEqual("0", inputs[2].GetAttribute("min"));
        Assert.AreEqual("59", inputs[2].GetAttribute("max"));

        // The step of the picker is not a step of the input: a value that does not sit on it - a bound time
        // of 10:37 in a picker that steps by 15 minutes - would make the input invalid, and a form holding a
        // closed (and therefore hidden) picker could not be submitted at all.
        Assert.IsFalse(inputs[0].HasAttribute("step"));
        Assert.IsFalse(inputs[1].HasAttribute("step"));
        Assert.IsFalse(inputs[2].HasAttribute("step"));
    }

    [TestMethod]
    public void BitTimePickerShouldRenderTheClockFaceRangeOfTheHourInputInTwelveHours()
    {
        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.TimeFormat, BitTimeFormat.TwelveHours);
        });

        var hourInput = component.FindAll(".bit-tpc-tin")[0];

        Assert.AreEqual("1", hourInput.GetAttribute("min"));
        Assert.AreEqual("12", hourInput.GetAttribute("max"));
    }

    [TestMethod]
    public void BitTimePickerShouldCloseOnTabWithoutReopening()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var isOpen = false;

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        component.Find(".bit-tpc-wrp").Click();
        Assert.IsTrue(isOpen);

        component.Find(".bit-tpc-inp").KeyDown("Tab");

        Assert.IsFalse(isOpen);
    }

    [TestMethod]
    public void BitTimePickerShouldRespectAnInitialIsOpen()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.IsOpenChanged, _ => { });
        });

        Assert.AreEqual("true", component.Find(".bit-tpc-inp").GetAttribute("aria-expanded"));
    }

    [TestMethod]
    public void BitTimePickerShouldCloseWithEscapeFromInsideTheCallout()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var isOpen = false;
        var closeCount = 0;

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.OnClose, () => closeCount++);
        });

        component.Find(".bit-tpc-wrp").Click();
        Assert.IsTrue(isOpen);

        component.FindAll(".bit-tpc-tin")[0].KeyDown("Escape");

        Assert.IsFalse(isOpen);
        Assert.AreEqual(1, closeCount);
    }

    [TestMethod]
    public void BitTimePickerShouldCloseWithTheCloseButton()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var isOpen = false;

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.ShowCloseButton, true);
            parameters.Add(p => p.CloseButtonTitle, "close it");
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        component.Find(".bit-tpc-wrp").Click();
        Assert.IsTrue(isOpen);

        var closeButton = component.Find(".bit-tpc-cbn");
        Assert.AreEqual("close it", closeButton.GetAttribute("title"));
        Assert.AreEqual("close it", closeButton.GetAttribute("aria-label"));

        closeButton.Click();

        Assert.IsFalse(isOpen);
    }

    [TestMethod]
    public void BitTimePickerShouldMoveTheFocusIntoTheCalloutOnOpen()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitTimePicker>();

        component.Find(".bit-tpc-wrp").Click();

        Assert.IsTrue(Context.JSInterop.Invocations.Any(i => i.Identifier == "Blazor._internal.domWrapper.focus"));
    }

    [TestMethod]
    public void BitTimePickerShouldKeepTheFocusOnAnEditableFieldOnOpen()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.AllowTextInput, true);
        });

        component.Find(".bit-tpc-wrp").Click();

        Assert.IsFalse(Context.JSInterop.Invocations.Any(i => i.Identifier == "Blazor._internal.domWrapper.focus"));
    }

    [TestMethod]
    public void BitTimePickerShouldNotOpenWhenDisabled()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var isOpen = false;

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        component.Find(".bit-tpc-wrp").Click();

        Assert.IsFalse(isOpen);
    }

    #endregion



    #region events

    [TestMethod]
    public void BitTimePickerShouldRaiseOnSelectTimeOnEveryChange()
    {
        TimeSpan? value = new TimeSpan(10, 30, 0);
        TimeSpan? selected = null;
        var selectCount = 0;

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Bind(p => p.Value, value, v => value = v);
            parameters.Add(p => p.OnSelectTime, (TimeSpan? t) => { selected = t; selectCount++; });
        });

        component.Find("button[title='Increase hour']").Click(new MouseEventArgs());

        Assert.AreEqual(1, selectCount);
        Assert.AreEqual(new TimeSpan(11, 30, 0), selected);
    }

    #endregion



    #region starting value

    [TestMethod]
    public void BitTimePickerShouldStepFromTheStartingValueWhenEmpty()
    {
        TimeSpan? value = null;

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.StartingValue, new TimeSpan(9, 30, 0));
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        // Empty until something is picked: the starting value is where the first change lands, not a value.
        Assert.IsNull(component.Find(".bit-tpc-inp").GetAttribute("value"));
        Assert.IsNull(component.FindAll(".bit-tpc-tin")[0].GetAttribute("value"));

        component.Find("button[title='Increase hour']").Click(new MouseEventArgs());

        Assert.AreEqual(new TimeSpan(10, 30, 0), value);
    }

    [TestMethod]
    public void BitTimePickerShouldNotUseTheStartingValueOnAPickerWithAValue()
    {
        TimeSpan? value = new TimeSpan(1, 0, 0);

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.StartingValue, new TimeSpan(9, 30, 0));
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find("button[title='Increase minute']").Click(new MouseEventArgs());

        Assert.AreEqual(new TimeSpan(1, 1, 0), value);
    }

    [TestMethod]
    public void BitTimePickerShouldTypeIntoAnEmptyPickerOverTheStartingValue()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        TimeSpan? value = null;

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.StartingValue, new TimeSpan(9, 30, 0));
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.FindAll(".bit-tpc-tin")[0].Input("14");

        Assert.AreEqual(new TimeSpan(14, 30, 0), value);
    }

    [TestMethod]
    public void BitTimePickerShouldNormalizeTheStartingValue()
    {
        TimeSpan? value = null;

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.StartingValue, TimeSpan.FromHours(-2));
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find("button[title='Increase minute']").Click(new MouseEventArgs());

        Assert.AreEqual(new TimeSpan(22, 1, 0), value);
    }

    [TestMethod]
    public void BitTimePickerShouldMoveTheStartingValueAcrossTheMeridiem()
    {
        TimeSpan? value = null;

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.StartingValue, new TimeSpan(9, 30, 0));
            parameters.Add(p => p.TimeFormat, BitTimeFormat.TwelveHours);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find(".bit-tpc-bpm").Click();

        Assert.AreEqual(new TimeSpan(21, 30, 0), value);
    }

    #endregion



    #region keyboard on the time inputs

    [TestMethod]
    public void BitTimePickerShouldJumpToTheEndsOfATimeInputWithHomeAndEnd()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        TimeSpan? value = new TimeSpan(10, 30, 0);

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.FindAll(".bit-tpc-tin")[1].KeyDown("End");
        Assert.AreEqual(new TimeSpan(10, 59, 0), value);

        component.FindAll(".bit-tpc-tin")[1].KeyDown("Home");
        Assert.AreEqual(new TimeSpan(10, 0, 0), value);

        component.FindAll(".bit-tpc-tin")[0].KeyDown("End");
        Assert.AreEqual(new TimeSpan(23, 0, 0), value);

        component.FindAll(".bit-tpc-tin")[0].KeyDown("Home");
        Assert.AreEqual(new TimeSpan(0, 0, 0), value);
    }

    [TestMethod]
    public void BitTimePickerShouldKeepHomeAndEndInsideTheHalfOfTheDay()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        TimeSpan? value = new TimeSpan(14, 30, 0);

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.TimeFormat, BitTimeFormat.TwelveHours);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.FindAll(".bit-tpc-tin")[0].KeyDown("End");
        Assert.AreEqual(new TimeSpan(23, 30, 0), value);

        component.FindAll(".bit-tpc-tin")[0].KeyDown("Home");
        Assert.AreEqual(new TimeSpan(12, 30, 0), value);
    }

    [TestMethod]
    public void BitTimePickerShouldSkipDisallowedValuesWithHomeAndEnd()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        TimeSpan? value = new TimeSpan(10, 30, 0);

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.AllowedMinutes, m => m is >= 10 and <= 50);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.FindAll(".bit-tpc-tin")[1].KeyDown("End");
        Assert.AreEqual(new TimeSpan(10, 50, 0), value);

        component.FindAll(".bit-tpc-tin")[1].KeyDown("Home");
        Assert.AreEqual(new TimeSpan(10, 10, 0), value);
    }

    [TestMethod]
    public void BitTimePickerShouldStepATimeInputWithThePageKeys()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        TimeSpan? value = new TimeSpan(10, 30, 0);

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.HourStep, 2);
            parameters.Add(p => p.MinuteStep, 15);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.FindAll(".bit-tpc-tin")[0].KeyDown("PageUp");
        Assert.AreEqual(new TimeSpan(12, 30, 0), value);

        component.FindAll(".bit-tpc-tin")[0].KeyDown("PageDown");
        Assert.AreEqual(new TimeSpan(10, 30, 0), value);

        component.FindAll(".bit-tpc-tin")[1].KeyDown("PageUp");
        Assert.AreEqual(new TimeSpan(10, 45, 0), value);
    }

    [TestMethod]
    public void BitTimePickerShouldCloseWithEnterFromATimeInput()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var isOpen = false;
        var closeCount = 0;

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.OnClose, () => closeCount++);
        });

        component.Find(".bit-tpc-wrp").Click();
        Assert.IsTrue(isOpen);

        component.FindAll(".bit-tpc-tin")[0].KeyDown("Enter");

        Assert.IsFalse(isOpen);
        Assert.AreEqual(1, closeCount);
    }

    [TestMethod]
    public void BitTimePickerShouldNotChangeATimeInputWithTheKeyboardWhenReadOnly()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        TimeSpan? value = new TimeSpan(10, 30, 0);
        var isOpen = false;

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find(".bit-tpc-wrp").Click();
        Assert.IsTrue(isOpen);

        component.FindAll(".bit-tpc-tin")[1].KeyDown("End");
        component.FindAll(".bit-tpc-tin")[1].KeyDown("Home");
        component.FindAll(".bit-tpc-tin")[1].KeyDown("PageUp");

        Assert.AreEqual(new TimeSpan(10, 30, 0), value);

        // A picker that only shows its value can still be closed from its inputs.
        component.FindAll(".bit-tpc-tin")[0].KeyDown("Enter");
        Assert.IsFalse(isOpen);
    }

    #endregion



    #region color, size & rtl

    [TestMethod,
        DataRow(BitColor.Primary, "bit-tpc-pri"),
        DataRow(BitColor.Secondary, "bit-tpc-sec"),
        DataRow(BitColor.Success, "bit-tpc-suc"),
        DataRow(BitColor.Error, "bit-tpc-err"),
        DataRow(BitColor.TertiaryBorder, "bit-tpc-tbr")
    ]
    public void BitTimePickerShouldApplyTheColorClassToTheRootAndTheCallout(BitColor color, string cssClass)
    {
        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.Color, color);
        });

        Assert.IsTrue(component.Find(".bit-tpc").ClassList.Contains(cssClass));
        Assert.IsTrue(component.Find(".bit-tpc-cal").ClassList.Contains(cssClass));
    }

    [TestMethod]
    public void BitTimePickerShouldFallBackToThePrimaryColor()
    {
        var component = RenderComponent<BitTimePicker>();

        Assert.IsTrue(component.Find(".bit-tpc").ClassList.Contains("bit-tpc-pri"));
        Assert.IsTrue(component.Find(".bit-tpc-cal").ClassList.Contains("bit-tpc-pri"));
    }

    [TestMethod,
        DataRow(BitSize.Small, "bit-tpc-sm"),
        DataRow(BitSize.Medium, "bit-tpc-md"),
        DataRow(BitSize.Large, "bit-tpc-lg")
    ]
    public void BitTimePickerShouldApplyTheSizeClassToTheRootAndTheCallout(BitSize size, string cssClass)
    {
        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.Size, size);
        });

        Assert.IsTrue(component.Find(".bit-tpc").ClassList.Contains(cssClass));
        Assert.IsTrue(component.Find(".bit-tpc-cal").ClassList.Contains(cssClass));
    }

    [TestMethod]
    public void BitTimePickerShouldRenderNoSizeClassByDefault()
    {
        var component = RenderComponent<BitTimePicker>();

        var classes = component.Find(".bit-tpc").ClassList;

        Assert.IsFalse(classes.Contains("bit-tpc-sm"));
        Assert.IsFalse(classes.Contains("bit-tpc-md"));
        Assert.IsFalse(classes.Contains("bit-tpc-lg"));
    }

    [TestMethod]
    public void BitTimePickerShouldFollowTheDirectionOfTheCulture()
    {
        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.GetCultureInfo("fa-IR"));
        });

        Assert.IsTrue(component.Find(".bit-tpc").ClassList.Contains("bit-rtl"));
        Assert.IsTrue(component.Find(".bit-tpc-cal").ClassList.Contains("bit-tpc-rtl"));
    }

    [TestMethod]
    public void BitTimePickerShouldNotFollowTheCultureOverAnExplicitDirection()
    {
        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Ltr);
            parameters.Add(p => p.Culture, CultureInfo.GetCultureInfo("fa-IR"));
        });

        Assert.IsFalse(component.Find(".bit-tpc").ClassList.Contains("bit-rtl"));
        Assert.IsFalse(component.Find(".bit-tpc-cal").ClassList.Contains("bit-tpc-rtl"));
    }

    [TestMethod]
    public void BitTimePickerShouldNotTurnTheStandaloneCalloutIntoASheet()
    {
        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.Responsive, true);
            parameters.Add(p => p.Standalone, true);
        });

        Assert.IsFalse(component.Find(".bit-tpc-cal").ClassList.Contains("bit-tpc-res"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Responsive, true);
            parameters.Add(p => p.Standalone, false);
        });

        Assert.IsTrue(component.Find(".bit-tpc-cal").ClassList.Contains("bit-tpc-res"));
    }

    #endregion



    #region accessibility & templates

    [TestMethod]
    public void BitTimePickerShouldAnnounceTheCalloutAsAModalDialog()
    {
        var component = RenderComponent<BitTimePicker>();

        var callout = component.Find(".bit-tpc-cac");

        Assert.AreEqual("dialog", callout.GetAttribute("role"));
        Assert.AreEqual("true", callout.GetAttribute("aria-modal"));
    }

    [TestMethod]
    public void BitTimePickerShouldNotAnnounceAStandaloneCalloutAsADialog()
    {
        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.Standalone, true);
        });

        var callout = component.Find(".bit-tpc-cac");

        // Standalone the picker is part of the page, where announcing a dialog would announce one the user
        // can never leave - but it is still a group of controls the label names.
        Assert.AreEqual("group", callout.GetAttribute("role"));
        Assert.IsFalse(callout.HasAttribute("aria-modal"));
    }

    [TestMethod]
    public void BitTimePickerShouldTrapTheFocusInAFloatingCallout()
    {
        var component = RenderComponent<BitTimePicker>();

        // A callout that reports itself a modal dialog has to hold the tab order, which happens on the JS
        // side - so it only works if the setup is actually told to trap.
        var setup = Context.JSInterop.Invocations["BitBlazorUI.TimePicker.setup"].Single();

        Assert.AreEqual(true, setup.Arguments[2]);
    }

    [TestMethod]
    public void BitTimePickerShouldNotTrapTheFocusInAStandaloneCallout()
    {
        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.Standalone, true);
        });

        // Standalone there is no dialog and no overlay, so the focus is free to leave the way it leaves any
        // other part of the page.
        var setup = Context.JSInterop.Invocations["BitBlazorUI.TimePicker.setup"].Single();

        Assert.AreEqual(false, setup.Arguments[2]);
    }

    [TestMethod]
    public void BitTimePickerShouldPointTheLabelOfAStandalonePickerAtTheHourInput()
    {
        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.Standalone, true);
            parameters.Add(p => p.Label, "my label");
        });

        var label = component.Find(".bit-tpc-lbl");
        var hourInput = component.FindAll(".bit-tpc-tin")[0];

        Assert.AreEqual(hourInput.GetAttribute("id"), label.GetAttribute("for"));
    }

    [TestMethod]
    public void BitTimePickerShouldAnnounceTheSelectedTimeInALiveRegion()
    {
        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.Value, new TimeSpan(10, 30, 0));
        });

        var live = component.Find(".bit-tpc-lve");

        Assert.AreEqual("polite", live.GetAttribute("aria-live"));
        Assert.AreEqual("10:30", live.TextContent.Trim());
    }

    [TestMethod]
    public void BitTimePickerShouldRenderTheCalloutHeaderAndFooterTemplates()
    {
        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.CalloutHeaderTemplate, builder => builder.AddMarkupContent(0, "<span>the header</span>"));
            parameters.Add(p => p.CalloutFooterTemplate, builder => builder.AddMarkupContent(0, "<span>the footer</span>"));
        });

        Assert.AreEqual("the header", component.Find(".bit-tpc-chd").TextContent.Trim());
        Assert.AreEqual("the footer", component.Find(".bit-tpc-cft").TextContent.Trim());
    }

    [TestMethod]
    public void BitTimePickerShouldRenderNoCalloutHeaderOrFooterWithoutTheTemplates()
    {
        var component = RenderComponent<BitTimePicker>();

        Assert.AreEqual(0, component.FindAll(".bit-tpc-chd").Count);
        Assert.AreEqual(0, component.FindAll(".bit-tpc-cft").Count);
    }

    #endregion



    #region validation

    [TestMethod]
    public void BitTimePickerValidationShouldFlagAMissingRequiredValue()
    {
        var component = RenderComponent<BitTimePickerValidationTest>();

        Assert.IsFalse(component.Find(".bit-tpc").ClassList.Contains("bit-inv"));

        component.Find("form").Submit();

        Assert.AreEqual(0, component.Instance.ValidCount);
        Assert.AreEqual(1, component.Instance.InvalidCount);
        Assert.IsTrue(component.Find(".bit-tpc").ClassList.Contains("bit-inv"));
        Assert.AreEqual("true", component.Find(".bit-tpc-inp").GetAttribute("aria-invalid"));
    }

    [TestMethod]
    public void BitTimePickerValidationShouldPassOnceAValueIsSet()
    {
        var component = RenderComponent<BitTimePickerValidationTest>(parameters =>
        {
            parameters.Add(p => p.TestModel, new BitTimePickerTestModel { Time = new TimeSpan(9, 0, 0) });
        });

        component.Find("form").Submit();

        Assert.AreEqual(1, component.Instance.ValidCount);
        Assert.AreEqual(0, component.Instance.InvalidCount);
        Assert.IsFalse(component.Find(".bit-tpc").ClassList.Contains("bit-inv"));
    }

    [TestMethod]
    public void BitTimePickerShouldUseTheCustomInvalidErrorMessage()
    {
        var component = RenderComponent<BitTimePickerValidationTest>(parameters =>
        {
            parameters.Add(p => p.InvalidErrorMessage, "nope");
        });

        component.Find(".bit-tpc-inp").Input("not a time");

        StringAssert.Contains(component.Find(".validation-message").TextContent, "nope");
    }

    [TestMethod]
    public void BitTimePickerShouldUseTheOutOfRangeErrorMessage()
    {
        var component = RenderComponent<BitTimePickerValidationTest>(parameters =>
        {
            parameters.Add(p => p.MinTime, new TimeSpan(8, 0, 0));
            parameters.Add(p => p.MaxTime, new TimeSpan(17, 0, 0));
            parameters.Add(p => p.InvalidErrorMessage, "not a time");
            parameters.Add(p => p.OutOfRangeErrorMessage, "too late");
        });

        component.Find(".bit-tpc-inp").Input("19:00");

        StringAssert.Contains(component.Find(".validation-message").TextContent, "too late");
    }

    [TestMethod]
    public void BitTimePickerShouldUseTheDisallowedTimeErrorMessage()
    {
        var component = RenderComponent<BitTimePickerValidationTest>(parameters =>
        {
            parameters.Add(p => p.AllowedMinutes, m => m % 15 == 0);
            parameters.Add(p => p.InvalidErrorMessage, "not a time");
            parameters.Add(p => p.DisallowedTimeErrorMessage, "quarter hours only");
        });

        component.Find(".bit-tpc-inp").Input("10:20");

        StringAssert.Contains(component.Find(".validation-message").TextContent, "quarter hours only");
    }

    [TestMethod]
    public void BitTimePickerShouldFallBackToTheDefaultErrorMessages()
    {
        var component = RenderComponent<BitTimePickerValidationTest>(parameters =>
        {
            parameters.Add(p => p.MaxTime, new TimeSpan(17, 0, 0));
        });

        component.Find(".bit-tpc-inp").Input("19:00");

        StringAssert.Contains(component.Find(".validation-message").TextContent, "out of the allowed range");
    }

    [TestMethod]
    public void BitTimePickerShouldRejectAnOutOfRangeTypedTimeInsideAForm()
    {
        var component = RenderComponent<BitTimePickerValidationTest>(parameters =>
        {
            parameters.Add(p => p.MinTime, new TimeSpan(8, 0, 0));
            parameters.Add(p => p.MaxTime, new TimeSpan(17, 0, 0));
            parameters.Add(p => p.InvalidErrorMessage, "out of range");
        });

        component.Find(".bit-tpc-inp").Input("19:00");

        Assert.IsNull(component.Instance.TestModel.Time);
        Assert.IsTrue(component.Find(".bit-tpc").ClassList.Contains("bit-inv"));

        component.Find(".bit-tpc-inp").Input("09:00");

        Assert.AreEqual(new TimeSpan(9, 0, 0), component.Instance.TestModel.Time);
        Assert.IsFalse(component.Find(".bit-tpc").ClassList.Contains("bit-inv"));
    }

    #endregion
}
