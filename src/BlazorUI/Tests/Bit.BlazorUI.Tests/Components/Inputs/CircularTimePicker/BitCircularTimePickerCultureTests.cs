using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.CircularTimePicker;

// A 12-hour dial names, writes, reads and takes from the keyboard the AM/PM designators of its culture - which for
// fa-IR and ar-SA are words of their own script rather than "AM" and "PM".
[TestClass]
public class BitCircularTimePickerCultureTests : BunitTestContext
{
    [TestMethod,
        DataRow("fa-IR"),
        DataRow("ar-SA")
    ]
    public void BitCircularTimePickerShouldShowTheDesignatorsOfTheCulture(string name)
    {
        var culture = CultureInfo.GetCultureInfo(name);

        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, culture);
            parameters.Add(p => p.TimeFormat, BitTimeFormat.TwelveHours);
            parameters.Add(p => p.Value, new TimeSpan(17, 30, 0));
        });

        var buttons = component.FindAll(".bit-ctp-apb");

        Assert.HasCount(2, buttons);
        Assert.AreEqual(culture.DateTimeFormat.AMDesignator, buttons[0].TextContent.Trim());
        Assert.AreEqual(culture.DateTimeFormat.AMDesignator, buttons[0].GetAttribute("title"));
        Assert.AreEqual(culture.DateTimeFormat.PMDesignator, buttons[1].TextContent.Trim());
        Assert.AreEqual(culture.DateTimeFormat.PMDesignator, buttons[1].GetAttribute("title"));
        Assert.AreEqual("true", buttons[1].GetAttribute("aria-pressed"));
    }

    [TestMethod,
        DataRow("fa-IR"),
        DataRow("ar-SA")
    ]
    public void BitCircularTimePickerShouldWriteTheValueWithTheDesignatorOfTheCulture(string name)
    {
        var culture = CultureInfo.GetCultureInfo(name);

        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, culture);
            parameters.Add(p => p.TimeFormat, BitTimeFormat.TwelveHours);
            parameters.Add(p => p.Value, new TimeSpan(13, 45, 0));
        });

        Assert.AreEqual($"01:45 {culture.DateTimeFormat.PMDesignator}", component.Find(".bit-ctp-inp").GetAttribute("value").NormalizeSpaces());
    }

    [TestMethod]
    public void BitCircularTimePickerShouldDropTheDesignatorOfATwelveHourCultureInTwentyFourHours()
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.GetCultureInfo("ar-SA"));
            parameters.Add(p => p.Value, new TimeSpan(13, 45, 0));
        });

        Assert.AreEqual("13:45", component.Find(".bit-ctp-inp").GetAttribute("value"));
        Assert.IsEmpty(component.FindAll(".bit-ctp-apb"));
    }

    [TestMethod,
        DataRow("fa-IR"),
        DataRow("ar-SA")
    ]
    public async Task BitCircularTimePickerMeridiemKeysShouldFollowTheDesignatorsOfTheCulture(string name)
    {
        var culture = CultureInfo.GetCultureInfo(name);

        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.Standalone, true);
            parameters.Add(p => p.TimeFormat, BitTimeFormat.TwelveHours);
            parameters.Add(p => p.Culture, culture);
            parameters.Add(p => p.DefaultValue, new TimeSpan(15, 0, 0));
        });

        await KeyDown(component, culture.DateTimeFormat.AMDesignator[..1]);
        Assert.AreEqual(3, component.Instance.Value!.Value.Hours);

        await KeyDown(component, culture.DateTimeFormat.PMDesignator[..1]);
        Assert.AreEqual(15, component.Instance.Value!.Value.Hours);
    }

    [TestMethod,
        DataRow("fa-IR"),
        DataRow("ar-SA")
    ]
    public void BitCircularTimePickerShouldReadATypedTimeWithTheDesignatorOfTheCulture(string name)
    {
        var culture = CultureInfo.GetCultureInfo(name);

        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.AllowTextInput, true);
            parameters.Add(p => p.Culture, culture);
            parameters.Add(p => p.TimeFormat, BitTimeFormat.TwelveHours);
        });

        component.Find(".bit-ctp-inp").Input($"02:30 {culture.DateTimeFormat.PMDesignator}");

        Assert.AreEqual(new TimeSpan(14, 30, 0), component.Instance.Value);
    }

    [TestMethod,
        DataRow("de-DE"),
        DataRow("")
    ]
    public void BitCircularTimePickerShouldWriteATwentyFourHourValueWithTheSeparatorOfTheCulture(string name)
    {
        // de-DE and the invariant culture both write a 24-hour time of day with a colon and a padded hour.
        var culture = CultureInfo.GetCultureInfo(name);

        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, culture);
            parameters.Add(p => p.Value, new TimeSpan(13, 45, 0));
        });

        Assert.AreEqual("13:45", component.Find(".bit-ctp-inp").GetAttribute("value"));
        Assert.IsEmpty(component.FindAll(".bit-ctp-apb"));

        var buttons = component.FindAll("button.bit-ctp-txt");

        Assert.AreEqual("13", buttons[0].TextContent.Trim());
        Assert.AreEqual("45", buttons[1].TextContent.Trim());
    }

    [TestMethod]
    public void BitCircularTimePickerShouldWriteATwelveHourValueWithTheDesignatorsOfAnEnUsCulture()
    {
        var culture = CultureInfo.GetCultureInfo("en-US");

        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, culture);
            parameters.Add(p => p.TimeFormat, BitTimeFormat.TwelveHours);
            parameters.Add(p => p.Value, new TimeSpan(13, 45, 0));
        });

        // en-US writes the hour narrow, which the field pads; the designator and the separator before it are the
        // culture's own (a narrow no-break space on some ICU versions, see TestStrings.NormalizeSpaces).
        Assert.AreEqual($"01:45 {culture.DateTimeFormat.PMDesignator}", component.Find(".bit-ctp-inp").GetAttribute("value").NormalizeSpaces());

        var buttons = component.FindAll(".bit-ctp-apb");

        Assert.AreEqual(culture.DateTimeFormat.AMDesignator, buttons[0].TextContent.Trim());
        Assert.AreEqual(culture.DateTimeFormat.PMDesignator, buttons[1].TextContent.Trim());
        Assert.AreEqual("true", buttons[1].GetAttribute("aria-pressed"));
    }

    [TestMethod,
        DataRow("de-DE", "07:15"),
        DataRow("en-US", "07:15"),
        DataRow("", "07:15")
    ]
    public void BitCircularTimePickerShouldReadATypedTwentyFourHourTimeOfTheCulture(string name, string typed)
    {
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.AllowTextInput, true);
            parameters.Add(p => p.Culture, CultureInfo.GetCultureInfo(name));
        });

        component.Find(".bit-ctp-inp").Input(typed);

        Assert.AreEqual(new TimeSpan(7, 15, 0), component.Instance.Value);
    }

    [TestMethod,
        DataRow("de-DE"),
        DataRow("en-US"),
        DataRow("fa-IR"),
        DataRow("")
    ]
    public void BitCircularTimePickerShouldNumberTheDialWithLatinDigitsWhateverTheCulture(string name)
    {
        // The dial is a clock face, which reads the same in every culture: the numbers are written with the digits
        // of the invariant culture rather than the native digits a culture may format its numbers with.
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.GetCultureInfo(name));
            parameters.Add(p => p.Value, new TimeSpan(9, 30, 0));
        });

        var numbers = component.FindAll(".bit-ctp-num").Select(n => n.TextContent).ToArray();

        Assert.AreEqual(24, numbers.Length);
        Assert.IsTrue(numbers.All(n => n.Length == 2 && n.All(char.IsAsciiDigit)));
        Assert.AreEqual("09", component.FindAll("button.bit-ctp-txt")[0].TextContent.Trim());
    }

    private static Task KeyDown(IRenderedComponent<BitCircularTimePicker> component, string key)
    {
        return component.InvokeAsync(() => component.Find(".bit-ctp-clf").KeyDown(new KeyboardEventArgs { Key = key }));
    }
}
