using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.CircularTimePicker;

[TestClass]
public class BitCircularTimePickerTests : BunitTestContext
{
    // The dial reports a pointer as an angle clockwise from 12 o'clock and a distance as a fraction of the
    // radius, so a press on a given number is expressed the same way the JS side would express it.
    private const double OuterRing = 0.9;
    private const double InnerRing = 0.5;

    private static double HourAngle(int hour) => (hour % 12) * 30;

    private static double MinuteAngle(int minute) => minute * 6;

    private static Task Press(IRenderedComponent<BitCircularTimePicker> com, double angle, double distance)
    {
        return com.InvokeAsync(async () =>
        {
            await com.Instance._HandlePointerDown(angle, distance);
            await com.Instance._HandlePointerUp();
        });
    }

    private static Task KeyDown(IRenderedComponent<BitCircularTimePicker> com, string key)
    {
        return com.InvokeAsync(() => com.Find(".bit-ctp-clf").KeyDown(new KeyboardEventArgs { Key = key }));
    }



    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitCircularTimePickerTest(bool isEnabled)
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var bitCircularTimePicker = component.Find(".bit-ctp");

        Assert.AreEqual(isEnabled is false, bitCircularTimePicker.ClassList.Contains("bit-dis"));

        // the callout is a sibling of the root, so the disabled state has to be carried onto it separately
        Assert.AreEqual(isEnabled is false, component.Find(".bit-ctp-cal").ClassList.Contains("bit-dis"));

        // a disabled dial leaves the tab order instead of only looking greyed out
        Assert.AreEqual(isEnabled ? "0" : "-1", component.Find(".bit-ctp-clf").GetAttribute("tabindex"));
    }

    [TestMethod,
        DataRow("<div>This is labelTemplate</div>")
    ]
    public void BitCircularTimePickerShouldRenderLabelTemplate(string labelTemplate)
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.LabelTemplate, builder =>
            {
                builder.AddMarkupContent(0, labelTemplate);
            });
        });

        var labelChild = component.Find(".bit-ctp > label").ChildNodes;
        labelChild.MarkupMatches(labelTemplate);
    }

    [TestMethod,
        DataRow("ClockAria")
    ]
    public void BitCircularTimePickerShouldRespectCalloutAriaLabel(string calloutAriaLabel)
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.CalloutAriaLabel, calloutAriaLabel);
        });

        var callout = component.Find(".bit-ctp-cac");

        Assert.AreEqual(calloutAriaLabel, callout.GetAttribute("aria-label"));
        Assert.AreEqual("dialog", callout.GetAttribute("role"));
    }

    [TestMethod,
        DataRow(true, 1),
        DataRow(false, 0)
    ]
    public void BitCircularTimePickerShouldHandleOnClickEvent(bool isEnabled, int count)
    {
        var clickedValue = 0;

        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.OnClick, () => clickedValue++);
        });

        component.Find(".bit-ctp-wrp").Click();

        Assert.AreEqual(count, clickedValue);
    }

    [TestMethod,
        DataRow("HourGlass")
    ]
    public void BitCircularTimePickerShouldAcceptIcon(string iconName)
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.IconName, iconName);
        });

        var icon = component.Find(".bit-ctp-ico");

        Assert.IsTrue(icon.ClassList.Contains($"bit-icon--{iconName}"));
    }

    [TestMethod,
        DataRow("Select a time..."),
        DataRow(null)
    ]
    public void BitCircularTimePickerShouldRespectPlaceholder(string placeholder)
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.Placeholder, placeholder);
        });

        var input = component.Find(".bit-ctp-inp");

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
    public void BitCircularTimePickerShouldRenderStandaloneInput()
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.Standalone, true);
        });

        Assert.IsNotNull(component.Find("input.bit-input-hidden"));

        // a standalone picker has no callout to dismiss, so it renders neither the overlay nor a close button
        Assert.AreEqual(0, component.FindAll(".bit-ctp-ovl").Count);
        Assert.AreEqual(0, component.FindAll(".bit-ctp-cbn").Count);
    }

    [TestMethod]
    public void BitCircularTimePickerCalloutHtmlAttributesTest()
    {
        var calloutHtmlAttributes = new Dictionary<string, object>
        {
            { "data-test", "data-value" }
        };

        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.CalloutHtmlAttributes, calloutHtmlAttributes);
        });

        var callout = component.Find(".bit-ctp-cac");

        Assert.AreEqual("data-value", callout.GetAttribute("data-test"));
    }

    [TestMethod]
    public void BitCircularTimePickerShouldRespectDefaultValue()
    {
        var defaultValue = new TimeSpan(10, 30, 0);

        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, defaultValue);
        });

        Assert.AreEqual(defaultValue, component.Instance.Value);
        Assert.AreEqual("10:30", component.Find(".bit-ctp-inp").GetAttribute("value"));
    }



    #region dial geometry

    [TestMethod,
        DataRow(12, 0),   // the top of the 12-hour ring is 12, which is midnight in the AM half
        DataRow(1, 1),
        DataRow(3, 3),
        DataRow(11, 11)
    ]
    public async Task BitCircularTimePickerTwelveHourDialShouldSelectTheNumberItPointsAt(int number, int expectedHour)
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.TimeFormat, BitTimeFormat.TwelveHours);
            parameters.Add(p => p.DefaultValue, new TimeSpan(0, 0, 0));
        });

        await Press(component, HourAngle(number), OuterRing);

        Assert.AreEqual(expectedHour, component.Instance.Value!.Value.Hours);
    }

    [TestMethod,
        DataRow(0, 0),    // the top of the outer ring is 00
        DataRow(1, 13),
        DataRow(5, 17),
        DataRow(11, 23)
    ]
    public async Task BitCircularTimePickerTwentyFourHourOuterRingShouldSelectTheAfternoonHours(int section, int expectedHour)
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, new TimeSpan(0, 0, 0));
        });

        await Press(component, section * 30, OuterRing);

        Assert.AreEqual(expectedHour, component.Instance.Value!.Value.Hours);
    }

    [TestMethod,
        DataRow(0, 12),   // the top of the inner ring is 12
        DataRow(1, 1),
        DataRow(5, 5),
        DataRow(11, 11)
    ]
    public async Task BitCircularTimePickerTwentyFourHourInnerRingShouldSelectTheMorningHours(int section, int expectedHour)
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, new TimeSpan(0, 0, 0));
        });

        await Press(component, section * 30, InnerRing);

        Assert.AreEqual(expectedHour, component.Instance.Value!.Value.Hours);
    }

    [TestMethod,
        DataRow(0),      // the top of the minute ring is 00
        DataRow(5),
        DataRow(30),
        DataRow(59)
    ]
    public async Task BitCircularTimePickerMinuteDialShouldSelectTheMinuteItPointsAt(int minute)
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, new TimeSpan(9, 0, 0));
            parameters.Add(p => p.EditMode, BitCircularTimePickerEditMode.OnlyMinutes);
        });

        await Press(component, MinuteAngle(minute), OuterRing);

        Assert.AreEqual(minute, component.Instance.Value!.Value.Minutes);
    }

    [TestMethod]
    public async Task BitCircularTimePickerShouldMoveToTheMinuteViewAfterAnHourIsPicked()
    {
        var views = new List<BitCircularTimePickerView>();

        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.Standalone, true);
            parameters.Add(p => p.OnViewChange, v => views.Add(v));
        });

        Assert.AreEqual(BitCircularTimePickerView.Hour, component.Instance.View);

        await Press(component, HourAngle(3), OuterRing);

        Assert.AreEqual(BitCircularTimePickerView.Minute, component.Instance.View);
        CollectionAssert.AreEqual(new[] { BitCircularTimePickerView.Minute }, views);
    }

    [TestMethod]
    public async Task BitCircularTimePickerShouldNotChangeTheTimeWhenReadOnly()
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.Standalone, true);
            parameters.Add(p => p.DefaultValue, new TimeSpan(4, 20, 0));
        });

        await Press(component, HourAngle(9), OuterRing);

        Assert.AreEqual(new TimeSpan(4, 20, 0), component.Instance.Value);

        // a read-only dial does not move on to the minutes either, since there was no pick to follow
        Assert.AreEqual(BitCircularTimePickerView.Hour, component.Instance.View);
    }

    #endregion



    #region am / pm

    [TestMethod]
    public async Task BitCircularTimePickerPmShouldKeepNoonAsNoon()
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.TimeFormat, BitTimeFormat.TwelveHours);
            parameters.Add(p => p.DefaultValue, new TimeSpan(12, 0, 0));
        });

        await component.InvokeAsync(() => component.FindAll(".bit-ctp-apb")[1].Click());

        Assert.AreEqual(new TimeSpan(12, 0, 0), component.Instance.Value);
    }

    [TestMethod,
        DataRow(0, 12),
        DataRow(5, 17),
        DataRow(11, 23),
        DataRow(17, 17)
    ]
    public async Task BitCircularTimePickerPmShouldMoveTheHourIntoTheAfternoon(int hour, int expected)
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.TimeFormat, BitTimeFormat.TwelveHours);
            parameters.Add(p => p.DefaultValue, new TimeSpan(hour, 0, 0));
        });

        await component.InvokeAsync(() => component.FindAll(".bit-ctp-apb")[1].Click());

        Assert.AreEqual(new TimeSpan(expected, 0, 0), component.Instance.Value);
    }

    [TestMethod,
        DataRow(12, 0),
        DataRow(17, 5),
        DataRow(23, 11),
        DataRow(5, 5)
    ]
    public async Task BitCircularTimePickerAmShouldMoveTheHourIntoTheMorning(int hour, int expected)
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.TimeFormat, BitTimeFormat.TwelveHours);
            parameters.Add(p => p.DefaultValue, new TimeSpan(hour, 0, 0));
        });

        await component.InvokeAsync(() => component.FindAll(".bit-ctp-apb")[0].Click());

        Assert.AreEqual(new TimeSpan(expected, 0, 0), component.Instance.Value);
    }

    [TestMethod]
    public void BitCircularTimePickerWithoutAValueShouldReadAsAm()
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.TimeFormat, BitTimeFormat.TwelveHours);
        });

        var buttons = component.FindAll(".bit-ctp-apb");

        Assert.AreEqual("true", buttons[0].GetAttribute("aria-pressed"));
        Assert.AreEqual("false", buttons[1].GetAttribute("aria-pressed"));
    }

    #endregion



    #region edit modes

    [TestMethod]
    public void BitCircularTimePickerOnlyMinutesShouldStartOnTheMinuteView()
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.Standalone, true);
            parameters.Add(p => p.EditMode, BitCircularTimePickerEditMode.OnlyMinutes);
        });

        Assert.AreEqual(BitCircularTimePickerView.Minute, component.Instance.View);

        // the minute ring carries the twelve five-minute marks
        var numbers = component.FindAll(".bit-ctp-num");
        Assert.AreEqual(12, numbers.Count);
        Assert.AreEqual("00", numbers[0].TextContent);
        Assert.AreEqual("55", numbers[11].TextContent);
    }

    [TestMethod]
    public async Task BitCircularTimePickerOnlyHoursShouldProduceAValueFromASingleHourPick()
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.Standalone, true);
            parameters.Add(p => p.EditMode, BitCircularTimePickerEditMode.OnlyHours);
        });

        await Press(component, HourAngle(0), InnerRing);

        Assert.AreEqual(new TimeSpan(12, 0, 0), component.Instance.Value);

        // and it stays on the hours, since the minutes are not editable in this mode
        Assert.AreEqual(BitCircularTimePickerView.Hour, component.Instance.View);
    }

    [TestMethod]
    public async Task BitCircularTimePickerOnlyMinutesShouldProduceAValueFromASingleMinutePick()
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.Standalone, true);
            parameters.Add(p => p.EditMode, BitCircularTimePickerEditMode.OnlyMinutes);
        });

        await Press(component, MinuteAngle(45), OuterRing);

        Assert.AreEqual(new TimeSpan(0, 45, 0), component.Instance.Value);
    }

    [TestMethod]
    public void BitCircularTimePickerSingleEditModesShouldNotRenderTheViewSwitchButtons()
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.EditMode, BitCircularTimePickerEditMode.OnlyHours);
        });

        Assert.AreEqual(0, component.FindAll(".bit-ctp-txt button").Count);
        Assert.AreEqual(0, component.FindAll("button.bit-ctp-txt").Count);
    }

    [TestMethod]
    public async Task BitCircularTimePickerShouldRespectStartView()
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.StartView, BitCircularTimePickerView.Minute);
        });

        Assert.AreEqual(BitCircularTimePickerView.Minute, component.Instance.View);

        // the toolbar reflects it: the minute button is the pressed one
        var buttons = component.FindAll("button.bit-ctp-txt");
        Assert.AreEqual("false", buttons[0].GetAttribute("aria-pressed"));
        Assert.AreEqual("true", buttons[1].GetAttribute("aria-pressed"));

        await component.InvokeAsync(() => component.Instance.SwitchView(BitCircularTimePickerView.Hour));

        Assert.AreEqual(BitCircularTimePickerView.Hour, component.Instance.View);
    }

    [TestMethod]
    public async Task BitCircularTimePickerSwitchViewShouldBeRefusedByASingleEditMode()
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.EditMode, BitCircularTimePickerEditMode.OnlyHours);
        });

        await component.InvokeAsync(() => component.Instance.SwitchView(BitCircularTimePickerView.Minute));

        Assert.AreEqual(BitCircularTimePickerView.Hour, component.Instance.View);
    }

    #endregion



    #region steps

    [TestMethod,
        DataRow(2, 0),
        DataRow(3, 5),
        DataRow(7, 5),
        DataRow(8, 10),
        DataRow(58, 0)   // rounding past the top of the hour wraps back onto 00
    ]
    public async Task BitCircularTimePickerShouldSnapTheMinuteToTheStep(int pointedAt, int expected)
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.MinuteStep, 5);
            parameters.Add(p => p.EditMode, BitCircularTimePickerEditMode.OnlyMinutes);
            parameters.Add(p => p.DefaultValue, new TimeSpan(9, 20, 0));
        });

        await Press(component, MinuteAngle(pointedAt), OuterRing);

        Assert.AreEqual(expected, component.Instance.Value!.Value.Minutes);
    }

    [TestMethod]
    public void BitCircularTimePickerHourStepShouldDimTheHoursInBetween()
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.HourStep, 3);
        });

        var numbers = component.FindAll(".bit-ctp-num");

        // the 24-hour dial renders 13..23 and 00 on the outer ring, then 01..12 on the inner one
        var enabled = numbers.Where(n => n.ClassList.Contains("bit-ctp-dis") is false)
                             .Select(n => int.Parse(n.TextContent))
                             .OrderBy(h => h)
                             .ToArray();

        CollectionAssert.AreEqual(new[] { 0, 3, 6, 9, 12, 15, 18, 21 }, enabled);
    }

    [TestMethod]
    public void BitCircularTimePickerMinuteStepShouldDimTheMarksInBetween()
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.MinuteStep, 15);
            parameters.Add(p => p.EditMode, BitCircularTimePickerEditMode.OnlyMinutes);
        });

        var enabled = component.FindAll(".bit-ctp-num")
                               .Where(n => n.ClassList.Contains("bit-ctp-dis") is false)
                               .Select(n => int.Parse(n.TextContent))
                               .ToArray();

        CollectionAssert.AreEqual(new[] { 0, 15, 30, 45 }, enabled);
    }

    #endregion



    #region selectable range

    [TestMethod]
    public void BitCircularTimePickerShouldDimTheHoursOutsideTheRange()
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.MinTime, new TimeSpan(8, 30, 0));
            parameters.Add(p => p.MaxTime, new TimeSpan(17, 15, 0));
        });

        var enabled = component.FindAll(".bit-ctp-num")
                               .Where(n => n.ClassList.Contains("bit-ctp-dis") is false)
                               .Select(n => int.Parse(n.TextContent))
                               .OrderBy(h => h)
                               .ToArray();

        CollectionAssert.AreEqual(new[] { 8, 9, 10, 11, 12, 13, 14, 15, 16, 17 }, enabled);
    }

    [TestMethod]
    public async Task BitCircularTimePickerShouldRefuseAnHourOutsideTheRange()
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.Standalone, true);
            parameters.Add(p => p.MinTime, new TimeSpan(8, 0, 0));
            parameters.Add(p => p.MaxTime, new TimeSpan(17, 0, 0));
            parameters.Add(p => p.DefaultValue, new TimeSpan(9, 0, 0));
        });

        await Press(component, 3 * 30, OuterRing); // 15 on the outer ring - inside the range

        Assert.AreEqual(15, component.Instance.Value!.Value.Hours);

        await Press(component, 9 * 30, OuterRing); // 21 on the outer ring - outside it

        Assert.AreEqual(15, component.Instance.Value!.Value.Hours);
    }

    [TestMethod]
    public void BitCircularTimePickerShouldBoundTheMinutesInsideTheBoundaryHourOnly()
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.MinTime, new TimeSpan(8, 30, 0));
            parameters.Add(p => p.EditMode, BitCircularTimePickerEditMode.OnlyMinutes);
            parameters.Add(p => p.DefaultValue, new TimeSpan(8, 30, 0));
        });

        var enabled = component.FindAll(".bit-ctp-num")
                               .Where(n => n.ClassList.Contains("bit-ctp-dis") is false)
                               .Select(n => int.Parse(n.TextContent))
                               .ToArray();

        CollectionAssert.AreEqual(new[] { 30, 35, 40, 45, 50, 55 }, enabled);
    }

    [TestMethod]
    public void BitCircularTimePickerShouldRespectTheAllowedValuePredicates()
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.AllowedHours, h => h is >= 9 and <= 11);
        });

        var enabled = component.FindAll(".bit-ctp-num")
                               .Where(n => n.ClassList.Contains("bit-ctp-dis") is false)
                               .Select(n => int.Parse(n.TextContent))
                               .OrderBy(h => h)
                               .ToArray();

        CollectionAssert.AreEqual(new[] { 9, 10, 11 }, enabled);
    }

    [TestMethod]
    public void BitCircularTimePickerShouldMarkTheUnselectableNumbersAsAriaDisabled()
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.AllowedHours, h => h == 9);
        });

        var numbers = component.FindAll(".bit-ctp-num");

        Assert.AreEqual(23, numbers.Count(n => n.GetAttribute("aria-disabled") == "true"));
        Assert.AreEqual(1, numbers.Count(n => n.HasAttribute("aria-disabled") is false));
    }

    [TestMethod]
    public void BitCircularTimePickerShouldRejectATypedTimeOutsideTheRange()
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.AllowTextInput, true);
            parameters.Add(p => p.MinTime, new TimeSpan(8, 0, 0));
            parameters.Add(p => p.MaxTime, new TimeSpan(17, 0, 0));
            parameters.Add(p => p.InvalidErrorMessage, "out of range");
        });

        component.Find(".bit-ctp-inp").Input("07:00");

        Assert.IsNull(component.Instance.Value);

        component.Find(".bit-ctp-inp").Input("09:00");

        Assert.AreEqual(new TimeSpan(9, 0, 0), component.Instance.Value);

        component.Find(".bit-ctp-inp").Input("18:00");

        Assert.AreEqual(new TimeSpan(9, 0, 0), component.Instance.Value);
    }

    #endregion



    #region keyboard

    [TestMethod]
    public async Task BitCircularTimePickerArrowKeysShouldMoveTheHour()
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.Standalone, true);
            parameters.Add(p => p.DefaultValue, new TimeSpan(9, 0, 0));
        });

        await KeyDown(component, "ArrowUp");
        Assert.AreEqual(10, component.Instance.Value!.Value.Hours);

        await KeyDown(component, "ArrowRight");
        Assert.AreEqual(11, component.Instance.Value!.Value.Hours);

        await KeyDown(component, "ArrowDown");
        Assert.AreEqual(10, component.Instance.Value!.Value.Hours);

        await KeyDown(component, "ArrowLeft");
        Assert.AreEqual(9, component.Instance.Value!.Value.Hours);

        await KeyDown(component, "PageUp");
        Assert.AreEqual(14, component.Instance.Value!.Value.Hours);

        await KeyDown(component, "PageDown");
        Assert.AreEqual(9, component.Instance.Value!.Value.Hours);
    }

    [TestMethod]
    public async Task BitCircularTimePickerArrowKeysShouldFollowTheStep()
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.Standalone, true);
            parameters.Add(p => p.MinuteStep, 15);
            parameters.Add(p => p.EditMode, BitCircularTimePickerEditMode.OnlyMinutes);
            parameters.Add(p => p.DefaultValue, new TimeSpan(9, 0, 0));
        });

        await KeyDown(component, "ArrowUp");
        Assert.AreEqual(15, component.Instance.Value!.Value.Minutes);

        await KeyDown(component, "ArrowUp");
        Assert.AreEqual(30, component.Instance.Value!.Value.Minutes);

        await KeyDown(component, "ArrowDown");
        Assert.AreEqual(15, component.Instance.Value!.Value.Minutes);
    }

    [TestMethod]
    public async Task BitCircularTimePickerHomeAndEndShouldJumpToTheEdgesOfTheRange()
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.Standalone, true);
            parameters.Add(p => p.MinTime, new TimeSpan(8, 0, 0));
            parameters.Add(p => p.MaxTime, new TimeSpan(17, 0, 0));
            parameters.Add(p => p.DefaultValue, new TimeSpan(12, 0, 0));
        });

        await KeyDown(component, "Home");
        Assert.AreEqual(8, component.Instance.Value!.Value.Hours);

        await KeyDown(component, "End");
        Assert.AreEqual(17, component.Instance.Value!.Value.Hours);
    }

    [TestMethod]
    public async Task BitCircularTimePickerArrowKeysShouldStepOverTheRefusedValues()
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.Standalone, true);
            parameters.Add(p => p.AllowedHours, h => h is 9 or 14);
            parameters.Add(p => p.DefaultValue, new TimeSpan(9, 0, 0));
        });

        await KeyDown(component, "ArrowUp");

        Assert.AreEqual(14, component.Instance.Value!.Value.Hours);
    }

    [TestMethod]
    public async Task BitCircularTimePickerEnterShouldSettleTheCurrentView()
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.Standalone, true);
            parameters.Add(p => p.DefaultValue, new TimeSpan(9, 0, 0));
        });

        await KeyDown(component, "Enter");

        Assert.AreEqual(BitCircularTimePickerView.Minute, component.Instance.View);
    }

    [TestMethod]
    public async Task BitCircularTimePickerKeyboardShouldNotMoveAReadOnlyDial()
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.Standalone, true);
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.DefaultValue, new TimeSpan(9, 0, 0));
        });

        await KeyDown(component, "ArrowUp");
        await KeyDown(component, "Home");

        Assert.AreEqual(new TimeSpan(9, 0, 0), component.Instance.Value);
    }

    [TestMethod]
    public async Task BitCircularTimePickerInputKeysShouldOpenAndCloseTheCallout()
    {
        var isOpen = false;

        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        var input = component.Find(".bit-ctp-inp");

        await component.InvokeAsync(() => input.KeyDown(new KeyboardEventArgs { Key = "Enter" }));
        Assert.IsTrue(isOpen);

        await component.InvokeAsync(() => input.KeyDown(new KeyboardEventArgs { Key = "Escape" }));
        Assert.IsFalse(isOpen);

        await component.InvokeAsync(() => input.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" }));
        Assert.IsTrue(isOpen);
    }

    [TestMethod]
    public void BitCircularTimePickerShouldHandItsFieldToTheKeyDefaultCanceller()
    {
        var component = RenderComponent<BitCircularTimePicker>();

        // Blazor cannot preventDefault per key, so the keys the field works the callout with have their
        // defaults cancelled on the JS side - which only works if the field itself is handed to the setup.
        var setup = Context.JSInterop.Invocations["BitBlazorUI.CircularTimePicker.setup"].Single();

        Assert.AreEqual(6, setup.Arguments.Count);
        Assert.AreEqual(component.Instance.InputElement.Id, ((ElementReference)setup.Arguments[2]!).Id);
    }

    [TestMethod]
    public async Task BitCircularTimePickerEnterShouldNotOpenAnEditableInput()
    {
        var isOpen = false;

        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.AllowTextInput, true);
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        await component.InvokeAsync(() => component.Find(".bit-ctp-inp").KeyDown(new KeyboardEventArgs { Key = "Enter" }));

        Assert.IsFalse(isOpen);
    }

    #endregion



    #region actions

    [TestMethod]
    public void BitCircularTimePickerShouldRenderTheActionsOnlyWhenAsked()
    {
        var component = RenderComponent<BitCircularTimePicker>();

        Assert.AreEqual(0, component.FindAll(".bit-ctp-act").Count);

        component.Render(parameters =>
        {
            parameters.Add(p => p.ShowNowButton, true);
            parameters.Add(p => p.ShowClearButton, true);
        });

        Assert.AreEqual(1, component.FindAll(".bit-ctp-act").Count);
        Assert.AreEqual(2, component.FindAll(".bit-ctp-abn").Count);
    }

    [TestMethod]
    public async Task BitCircularTimePickerClearButtonShouldEmptyTheValue()
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.Standalone, true);
            parameters.Add(p => p.ShowClearButton, true);
            parameters.Add(p => p.DefaultValue, new TimeSpan(9, 30, 0));
        });

        await component.InvokeAsync(() => component.Find(".bit-ctp-abn").Click());

        Assert.IsNull(component.Instance.Value);

        // and the toolbar goes back to showing nothing rather than a stale time
        Assert.AreEqual("--", component.FindAll("button.bit-ctp-txt")[0].TextContent.Trim());
        Assert.AreEqual("--", component.FindAll("button.bit-ctp-txt")[1].TextContent.Trim());
    }

    [TestMethod]
    public void BitCircularTimePickerClearButtonShouldBeDisabledWithoutAValue()
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.ShowClearButton, true);
        });

        Assert.IsTrue(component.Find(".bit-ctp-abn").HasAttribute("disabled"));
    }

    [TestMethod]
    public async Task BitCircularTimePickerNowButtonShouldSnapToTheStepsAndTheRange()
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.Standalone, true);
            parameters.Add(p => p.ShowNowButton, true);
            parameters.Add(p => p.MinuteStep, 15);
            parameters.Add(p => p.AllowedHours, h => h is 9 or 10);
        });

        await component.InvokeAsync(() => component.Find(".bit-ctp-abn").Click());

        var value = component.Instance.Value;

        Assert.IsNotNull(value);
        Assert.IsTrue(value!.Value.Hours is 9 or 10);
        Assert.AreEqual(0, value.Value.Minutes % 15);
    }

    [TestMethod]
    public async Task BitCircularTimePickerAutoCloseShouldCloseOnceTheSelectionIsComplete()
    {
        var isOpen = true;

        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.AutoClose, true);
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        // the hour only moves the dial on to the minutes
        await Press(component, HourAngle(3), OuterRing);
        Assert.IsTrue(isOpen);

        await Press(component, MinuteAngle(30), OuterRing);
        Assert.IsFalse(isOpen);
    }

    [TestMethod]
    public async Task BitCircularTimePickerCloseButtonShouldCloseTheCallout()
    {
        var isOpen = true;
        var closed = 0;

        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.ShowCloseButton, true);
            parameters.Add(p => p.OnClose, () => closed++);
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        await component.InvokeAsync(() => component.Find(".bit-ctp-cbn").Click());

        Assert.IsFalse(isOpen);
        Assert.AreEqual(1, closed);
    }

    #endregion



    #region aria

    [TestMethod]
    public void BitCircularTimePickerClockShouldBeAListboxOfTheCurrentView()
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, new TimeSpan(9, 30, 0));
            parameters.Add(p => p.HourButtonTitle, "Pick an hour");
        });

        var clock = component.Find(".bit-ctp-clf");

        Assert.AreEqual("listbox", clock.GetAttribute("role"));
        Assert.AreEqual("Pick an hour", clock.GetAttribute("aria-label"));

        var options = component.FindAll("[role=option]");
        Assert.AreEqual(24, options.Count);

        var selected = options.Single(o => o.GetAttribute("aria-selected") == "true");
        Assert.AreEqual("09", selected.TextContent);
        Assert.AreEqual(selected.Id, clock.GetAttribute("aria-activedescendant"));
    }

    [TestMethod]
    public async Task BitCircularTimePickerShouldDropTheActiveDescendantBetweenTwoMarks()
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.Standalone, true);
            parameters.Add(p => p.EditMode, BitCircularTimePickerEditMode.OnlyMinutes);
            parameters.Add(p => p.DefaultValue, new TimeSpan(9, 0, 0));
        });

        Assert.IsTrue(component.Find(".bit-ctp-clf").HasAttribute("aria-activedescendant"));

        await Press(component, MinuteAngle(7), OuterRing);

        Assert.IsFalse(component.Find(".bit-ctp-clf").HasAttribute("aria-activedescendant"));
    }

    [TestMethod]
    public void BitCircularTimePickerInputShouldExposeItsPopupState()
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.Label, "Time");
        });

        var input = component.Find(".bit-ctp-inp");

        Assert.AreEqual("combobox", input.GetAttribute("role"));
        Assert.AreEqual("dialog", input.GetAttribute("aria-haspopup"));
        Assert.AreEqual("false", input.GetAttribute("aria-expanded"));
        Assert.IsFalse(input.HasAttribute("aria-controls"));

        component.Render(parameters => parameters.Add(p => p.IsOpen, true));

        Assert.AreEqual("true", component.Find(".bit-ctp-inp").GetAttribute("aria-expanded"));
        Assert.IsTrue(component.Find(".bit-ctp-inp").HasAttribute("aria-controls"));
    }

    [TestMethod]
    public void BitCircularTimePickerReadOnlyShouldBeAnnouncedRatherThanDisabled()
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
        });

        Assert.AreEqual("true", component.Find(".bit-ctp-inp").GetAttribute("aria-readonly"));
        Assert.AreEqual("true", component.Find(".bit-ctp-clf").GetAttribute("aria-readonly"));

        // it is still reachable, unlike a disabled picker
        Assert.AreEqual("0", component.Find(".bit-ctp-clf").GetAttribute("tabindex"));
    }

    #endregion



    #region appearance

    [TestMethod,
        DataRow(BitColor.Primary, "bit-ctp-pri"),
        DataRow(BitColor.Success, "bit-ctp-suc"),
        DataRow(BitColor.Error, "bit-ctp-err"),
        DataRow(null, "bit-ctp-pri")
    ]
    public void BitCircularTimePickerShouldRespectColor(BitColor? color, string expectedClass)
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.Color, color);
        });

        Assert.IsTrue(component.Find(".bit-ctp").ClassList.Contains(expectedClass));

        // the callout is a sibling of the root, so it has to carry the accent itself
        Assert.IsTrue(component.Find(".bit-ctp-cal").ClassList.Contains(expectedClass));
    }

    [TestMethod,
        DataRow(BitSize.Small, "bit-ctp-sm"),
        DataRow(BitSize.Medium, "bit-ctp-md"),
        DataRow(BitSize.Large, "bit-ctp-lg"),
        DataRow(null, "bit-ctp-md")
    ]
    public void BitCircularTimePickerShouldRespectSize(BitSize? size, string expectedClass)
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.Size, size);
        });

        Assert.IsTrue(component.Find(".bit-ctp").ClassList.Contains(expectedClass));
        Assert.IsTrue(component.Find(".bit-ctp-cal").ClassList.Contains(expectedClass));
    }

    [TestMethod,
        DataRow(true, "bit-ctp-und"),
        DataRow(false, null)
    ]
    public void BitCircularTimePickerShouldRespectUnderlined(bool underlined, string expectedClass)
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.Underlined, underlined);
        });

        Assert.AreEqual(underlined, component.Find(".bit-ctp").ClassList.Contains("bit-ctp-und"));
    }

    [TestMethod,
        DataRow(true, false),
        DataRow(false, true)
    ]
    public void BitCircularTimePickerShouldRespectHasBorder(bool hasBorder, bool expectsNoBorderClass)
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.HasBorder, hasBorder);
        });

        Assert.AreEqual(expectsNoBorderClass, component.Find(".bit-ctp").ClassList.Contains("bit-ctp-nbd"));
    }

    [TestMethod]
    public void BitCircularTimePickerShouldRespectIconLocation()
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.IconLocation, BitIconLocation.Left);
        });

        Assert.IsTrue(component.Find(".bit-ctp").ClassList.Contains("bit-ctp-lic"));
    }

    [TestMethod]
    public void BitCircularTimePickerShouldCarryTheDirectionOntoTheCallout()
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Rtl);
        });

        // the callout is rendered outside the root element, so without a dir of its own it would fall back
        // to the direction of the page
        Assert.AreEqual("rtl", component.Find(".bit-ctp-cal").GetAttribute("dir"));
    }

    [TestMethod]
    public void BitCircularTimePickerShouldApplyTheClassStyleSlots()
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.ShowClearButton, true);
            parameters.Add(p => p.DefaultValue, new TimeSpan(9, 0, 0));
            parameters.Add(p => p.AllowedHours, h => h == 9);
            parameters.Add(p => p.Classes, new BitCircularTimePickerClassStyles
            {
                ClockNumber = "num",
                ClockSelectedNumber = "sel",
                ClockDisabledNumber = "dis",
                ClockPointer = "ptr",
                Actions = "act",
                ClearButton = "clr"
            });
        });

        Assert.AreEqual(24, component.FindAll(".bit-ctp-num.num").Count);
        Assert.AreEqual(1, component.FindAll(".bit-ctp-num.sel").Count);
        Assert.AreEqual(23, component.FindAll(".bit-ctp-num.dis").Count);
        Assert.AreEqual(1, component.FindAll(".bit-ctp-ptr.ptr").Count);
        Assert.AreEqual(1, component.FindAll(".bit-ctp-act.act").Count);
        Assert.AreEqual(1, component.FindAll(".bit-ctp-abn.clr").Count);

        // the pointer class must not leak into the style attribute of the hand
        StringAssert.DoesNotMatch(component.Find(".bit-ctp-ptr").GetAttribute("style") ?? string.Empty,
                                  new System.Text.RegularExpressions.Regex("ptr"));
    }

    [TestMethod]
    public void BitCircularTimePickerShouldApplyTheStyleSlots()
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.Styles, new BitCircularTimePickerClassStyles
            {
                ClockPointer = "opacity:0.5"
            });
        });

        StringAssert.Contains(component.Find(".bit-ctp-ptr").GetAttribute("style"), "opacity:0.5");
    }

    #endregion



    #region value

    [TestMethod]
    public void BitCircularTimePickerShouldFormatTheValueWithTheValueFormat()
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.ValueFormat, "hh-mm tt");
            parameters.Add(p => p.TimeFormat, BitTimeFormat.TwelveHours);
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.DefaultValue, new TimeSpan(13, 5, 0));
        });

        Assert.AreEqual("01-05 PM", component.Find(".bit-ctp-inp").GetAttribute("value"));
    }

    [TestMethod]
    public void BitCircularTimePickerShouldParseTheTypedTextWithTheValueFormat()
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.AllowTextInput, true);
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
        });

        component.Find(".bit-ctp-inp").Input("07:45");

        Assert.AreEqual(new TimeSpan(7, 45, 0), component.Instance.Value);
    }

    [TestMethod]
    public void BitCircularTimePickerShouldNotParseTheTypedTextWithoutAllowTextInput()
    {
        var component = RenderComponent<BitCircularTimePicker>();

        component.Find(".bit-ctp-inp").Input("07:45");

        Assert.IsNull(component.Instance.Value);
    }

    [TestMethod]
    public async Task BitCircularTimePickerShouldReportEveryChange()
    {
        TimeSpan? selected = null;
        TimeSpan? changed = null;

        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.Standalone, true);
            parameters.Add(p => p.OnSelectTime, v => selected = v);
            parameters.Add(p => p.OnChange, v => changed = v);
        });

        await Press(component, HourAngle(0), InnerRing);

        Assert.AreEqual(new TimeSpan(12, 0, 0), selected);
        Assert.AreEqual(new TimeSpan(12, 0, 0), changed);
    }

    [TestMethod]
    public async Task BitCircularTimePickerShouldSupportTwoWayBinding()
    {
        TimeSpan? bound = new TimeSpan(1, 0, 0);

        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.Standalone, true);
            parameters.Bind(p => p.Value, bound, v => bound = v);
        });

        await Press(component, HourAngle(0), InnerRing);

        Assert.AreEqual(new TimeSpan(12, 0, 0), bound);
    }

    [TestMethod]
    public async Task BitCircularTimePickerShouldNotChangeAnUnboundValue()
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.Standalone, true);
            parameters.Add(p => p.Value, new TimeSpan(1, 0, 0));
        });

        await Press(component, HourAngle(3), OuterRing);

        Assert.AreEqual(new TimeSpan(1, 0, 0), component.Instance.Value);
    }

    [TestMethod]
    public void BitCircularTimePickerShouldShowTheHourInTheFormatOfTheDial()
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.TimeFormat, BitTimeFormat.TwelveHours);
            parameters.Add(p => p.DefaultValue, new TimeSpan(0, 5, 0));
        });

        var buttons = component.FindAll("button.bit-ctp-txt");

        Assert.AreEqual("12", buttons[0].TextContent.Trim());
        Assert.AreEqual("05", buttons[1].TextContent.Trim());
    }

    #endregion



    #region open state

    [TestMethod]
    public void BitCircularTimePickerShouldOpenFromTheIsOpenParameter()
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
        });

        Assert.AreEqual("block", GetOverlayDisplay(component));

        component.Render(parameters => parameters.Add(p => p.IsOpen, false));

        Assert.AreEqual("none", GetOverlayDisplay(component));
    }

    [TestMethod]
    public async Task BitCircularTimePickerShouldReportOpenAndClose()
    {
        var opened = 0;
        var closed = 0;
        var isOpen = false;

        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.OnOpen, () => opened++);
            parameters.Add(p => p.OnClose, () => closed++);
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        await component.InvokeAsync(() => component.Instance.OpenCallout());
        Assert.AreEqual(1, opened);
        Assert.IsTrue(isOpen);

        await component.InvokeAsync(() => component.Instance.DismissCallout());
        Assert.AreEqual(1, closed);
        Assert.IsFalse(isOpen);
    }

    [TestMethod]
    public async Task BitCircularTimePickerShouldGoBackToTheStartViewOnEveryOpening()
    {
        var component = RenderComponent<BitCircularTimePicker>();

        await component.InvokeAsync(() => component.Instance.OpenCallout());
        await Press(component, HourAngle(3), OuterRing);

        Assert.AreEqual(BitCircularTimePickerView.Minute, component.Instance.View);

        await component.InvokeAsync(() => component.Instance.DismissCallout());
        await component.InvokeAsync(() => component.Instance.OpenCallout());

        Assert.AreEqual(BitCircularTimePickerView.Hour, component.Instance.View);
    }

    [TestMethod]
    public void BitCircularTimePickerShouldKeepTheFocusStateWhileTheCalloutIsOpen()
    {
        var component = RenderComponent<BitCircularTimePicker>();

        component.Find(".bit-ctp-inp").FocusIn();

        Assert.IsTrue(component.Find(".bit-ctp").ClassList.Contains("bit-ctp-foc"));

        // Opening hands the focus to the dial, which sits in the callout - a sibling of the root - so the input
        // reports a focusout even though the picker is still the control being operated.
        component.Find(".bit-ctp-wrp").Click();
        component.Find(".bit-ctp-inp").FocusOut();

        Assert.IsTrue(component.Find(".bit-ctp").ClassList.Contains("bit-ctp-foc"));

        component.Find(".bit-ctp-ovl").Click();

        Assert.IsFalse(component.Find(".bit-ctp").ClassList.Contains("bit-ctp-foc"));
    }

    [TestMethod]
    public async Task BitCircularTimePickerStandaloneShouldIgnoreTheCallout()
    {
        var opened = 0;

        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.Standalone, true);
            parameters.Add(p => p.OnOpen, () => opened++);
        });

        await component.InvokeAsync(() => component.Instance.OpenCallout());

        Assert.AreEqual(0, opened);
        Assert.IsFalse(component.Instance.IsOpen);
    }

    private static string GetOverlayDisplay(IRenderedComponent<BitCircularTimePicker> component)
    {
        var style = component.Find(".bit-ctp-ovl").GetAttribute("style") ?? string.Empty;

        return style.Contains("display:block") ? "block" : "none";
    }

    #endregion



    #region round trip

    [TestMethod]
    public async Task BitCircularTimePickerMeridiemShouldEnterTheHalfAtItsFirstSelectableHour()
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.TimeFormat, BitTimeFormat.TwelveHours);
            parameters.Add(p => p.MinTime, new TimeSpan(14, 0, 0));
        });

        // the morning half is entirely ruled out, so the PM button has to be able to move on its own
        await component.InvokeAsync(() => component.FindAll(".bit-ctp-apb")[1].Click());

        Assert.AreEqual(new TimeSpan(14, 0, 0), component.Instance.Value);
    }

    [TestMethod]
    public async Task BitCircularTimePickerMeridiemShouldDoNothingWhenTheWholeHalfIsRuledOut()
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.TimeFormat, BitTimeFormat.TwelveHours);
            parameters.Add(p => p.AllowedHours, h => h is >= 13 and <= 15);
            parameters.Add(p => p.DefaultValue, new TimeSpan(14, 0, 0));
        });

        await component.InvokeAsync(() => component.FindAll(".bit-ctp-apb")[0].Click());

        Assert.AreEqual(new TimeSpan(14, 0, 0), component.Instance.Value);
    }

    [TestMethod]
    public async Task BitCircularTimePickerOpenCalloutShouldNotReportAClick()
    {
        var clicked = 0;

        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.OnClick, () => clicked++);
        });

        await component.InvokeAsync(() => component.Instance.OpenCallout());

        Assert.AreEqual(0, clicked);

        component.Find(".bit-ctp-wrp").Click();

        Assert.AreEqual(1, clicked);
    }

    [TestMethod]
    public void BitCircularTimePickerShouldRespectTheActionTexts()
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.ShowNowButton, true);
            parameters.Add(p => p.ShowClearButton, true);
            parameters.Add(p => p.NowButtonText, "Maintenant");
            parameters.Add(p => p.ClearButtonText, "Effacer");
        });

        var buttons = component.FindAll(".bit-ctp-abn");

        Assert.AreEqual("Maintenant", buttons[0].TextContent.Trim());
        Assert.AreEqual("Effacer", buttons[1].TextContent.Trim());
    }

    [TestMethod]
    public void BitCircularTimePickerTwentyFourHourDialShouldMarkTheInnerRing()
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, new TimeSpan(5, 0, 0));
        });

        var inner = component.FindAll(".bit-ctp-num.bit-ctp-inn");

        Assert.AreEqual(12, inner.Count);
        CollectionAssert.AreEqual(new[] { "01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11", "12" },
                                  inner.Select(n => n.TextContent).ToArray());

        // an hour on the inner ring shortens the hand to that radius, one on the outer ring leaves it full length
        Assert.IsTrue(component.Find(".bit-ctp-ptr").ClassList.Contains("bit-ctp-pti"));

        var afternoon = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, new TimeSpan(21, 0, 0));
        });

        Assert.IsFalse(afternoon.Find(".bit-ctp-ptr").ClassList.Contains("bit-ctp-pti"));
    }

    [TestMethod]
    public void BitCircularTimePickerTwelveHourDialShouldHaveASingleRing()
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.TimeFormat, BitTimeFormat.TwelveHours);
        });

        var numbers = component.FindAll(".bit-ctp-num");

        Assert.AreEqual(12, numbers.Count);
        Assert.AreEqual(0, component.FindAll(".bit-ctp-inn").Count);
        CollectionAssert.AreEqual(Enumerable.Range(1, 12).Select(i => i.ToString()).ToArray(),
                                  numbers.Select(n => n.TextContent).ToArray());
    }

    [TestMethod]
    public void BitCircularTimePickerShouldTakeTheMeridiemDesignatorsFromTheCulture()
    {
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.DateTimeFormat.AMDesignator = "vm";
        culture.DateTimeFormat.PMDesignator = "nm";

        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.TimeFormat, BitTimeFormat.TwelveHours);
            parameters.Add(p => p.Culture, culture);
        });

        var buttons = component.FindAll(".bit-ctp-apb");

        Assert.AreEqual("vm", buttons[0].TextContent.Trim());
        Assert.AreEqual("nm", buttons[1].TextContent.Trim());
    }

    [TestMethod]
    public void BitCircularTimePickerShouldRenderTheIconTemplateInPlaceOfTheIcon()
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.IconTemplate, builder => builder.AddMarkupContent(0, "<span class=\"my-icon\"></span>"));
        });

        Assert.AreEqual(1, component.FindAll(".my-icon").Count);
        Assert.AreEqual(0, component.FindAll(".bit-ctp-ico").Count);
    }

    [TestMethod]
    public void BitCircularTimePickerShouldRespectTheCloseButtonIconAndTitle()
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.ShowCloseButton, true);
            parameters.Add(p => p.CloseButtonIconName, "ChromeClose");
            parameters.Add(p => p.CloseButtonTitle, "Dismiss");
        });

        var button = component.Find(".bit-ctp-cbn");

        Assert.AreEqual("Dismiss", button.GetAttribute("title"));
        Assert.AreEqual("Dismiss", button.GetAttribute("aria-label"));
        Assert.IsTrue(component.Find(".bit-ctp-cbi").ClassList.Contains("bit-icon--ChromeClose"));
    }

    [TestMethod]
    public async Task BitCircularTimePickerStandaloneShouldIgnoreAutoClose()
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.Standalone, true);
            parameters.Add(p => p.AutoClose, true);
            parameters.Add(p => p.EditMode, BitCircularTimePickerEditMode.OnlyMinutes);
        });

        await Press(component, MinuteAngle(30), OuterRing);

        Assert.IsFalse(component.Instance.IsOpen);
        Assert.AreEqual(new TimeSpan(0, 30, 0), component.Instance.Value);
    }

    #endregion



    #region validation

    [TestMethod]
    public void BitCircularTimePickerValidationShouldFlagAMissingRequiredValue()
    {
        var component = RenderComponent<BitCircularTimePickerValidationTest>();

        Assert.IsFalse(component.Find(".bit-ctp").ClassList.Contains("bit-inv"));

        component.Find("form").Submit();

        Assert.AreEqual(0, component.Instance.ValidCount);
        Assert.AreEqual(1, component.Instance.InvalidCount);
        Assert.IsTrue(component.Find(".bit-ctp").ClassList.Contains("bit-inv"));
        Assert.AreEqual("true", component.Find(".bit-ctp-inp").GetAttribute("aria-invalid"));
    }

    [TestMethod]
    public void BitCircularTimePickerValidationShouldPassOnceAValueIsSet()
    {
        var component = RenderComponent<BitCircularTimePickerValidationTest>(parameters =>
        {
            parameters.Add(p => p.TestModel, new BitCircularTimePickerTestModel { Time = new TimeSpan(9, 0, 0) });
        });

        component.Find("form").Submit();

        Assert.AreEqual(1, component.Instance.ValidCount);
        Assert.AreEqual(0, component.Instance.InvalidCount);
        Assert.IsFalse(component.Find(".bit-ctp").ClassList.Contains("bit-inv"));
    }

    [TestMethod]
    public void BitCircularTimePickerShouldUseTheCustomInvalidErrorMessage()
    {
        var component = RenderComponent<BitCircularTimePickerValidationTest>(parameters =>
        {
            parameters.Add(p => p.InvalidErrorMessage, "nope");
        });

        component.Find(".bit-ctp-inp").Input("not a time");

        StringAssert.Contains(component.Find(".validation-message").TextContent, "nope");
    }

    [TestMethod]
    public void BitCircularTimePickerShouldRejectAnOutOfRangeTypedTimeInsideAForm()
    {
        var component = RenderComponent<BitCircularTimePickerValidationTest>(parameters =>
        {
            parameters.Add(p => p.MinTime, new TimeSpan(8, 0, 0));
            parameters.Add(p => p.MaxTime, new TimeSpan(17, 0, 0));
            parameters.Add(p => p.InvalidErrorMessage, "out of range");
        });

        component.Find(".bit-ctp-inp").Input("19:00");

        Assert.IsNull(component.Instance.TestModel.Time);
        Assert.IsTrue(component.Find(".bit-ctp").ClassList.Contains("bit-inv"));

        component.Find(".bit-ctp-inp").Input("09:00");

        Assert.AreEqual(new TimeSpan(9, 0, 0), component.Instance.TestModel.Time);
        Assert.IsFalse(component.Find(".bit-ctp").ClassList.Contains("bit-inv"));
    }

    #endregion
}
