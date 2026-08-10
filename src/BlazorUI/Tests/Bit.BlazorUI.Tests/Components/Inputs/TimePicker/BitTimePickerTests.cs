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

        await Task.WhenAny(downTask, Task.Delay(5000));

        Assert.IsTrue(value >= new TimeSpan(11, 30, 0));
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
    public void BitTimePickerShouldClampASteppedValueIntoTheBounds()
    {
        TimeSpan? value = new TimeSpan(10, 0, 0);

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.MaxTime, new TimeSpan(10, 30, 0));
            parameters.Add(p => p.MinTime, new TimeSpan(9, 45, 0));
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find("button[title='Increase hour']").Click(new MouseEventArgs());
        Assert.AreEqual(new TimeSpan(10, 30, 0), value);

        component.Find("button[title='Decrease hour']").Click(new MouseEventArgs());
        Assert.AreEqual(new TimeSpan(9, 45, 0), value);
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
    public void BitTimePickerShouldRenderTheStepsOnTheTimeInputs()
    {
        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.HourStep, 2);
            parameters.Add(p => p.MinuteStep, 15);
            parameters.Add(p => p.SecondStep, 30);
            parameters.Add(p => p.ShowSeconds, true);
        });

        var inputs = component.FindAll(".bit-tpc-tin");

        Assert.AreEqual("2", inputs[0].GetAttribute("step"));
        Assert.AreEqual("15", inputs[1].GetAttribute("step"));
        Assert.AreEqual("30", inputs[2].GetAttribute("step"));
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
