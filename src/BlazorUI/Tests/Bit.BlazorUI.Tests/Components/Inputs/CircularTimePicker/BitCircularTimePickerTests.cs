using System.Collections.Generic;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.CircularTimePicker;

[TestClass]
public class BitCircularTimePickerTests : BunitTestContext
{
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

        if (isEnabled)
        {
            Assert.IsFalse(bitCircularTimePicker.ClassList.Contains("bit-dis"));
        }
        else
        {
            Assert.IsTrue(bitCircularTimePicker.ClassList.Contains("bit-dis"));
        }
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
        var ariaLabel = callout.GetAttribute("aria-label");

        Assert.AreEqual(calloutAriaLabel, ariaLabel);
    }

    [TestMethod,
        DataRow(true, 1),
        DataRow(false, 0)
    ]
    public void BitCircularTimePickerShouldHandleOnClickEvent(bool isEnabled, int count)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var clickedValue = 0;

        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.OnClick, () => clickedValue++);
        });

        var bitCircularTimePickerWrapper = component.Find(".bit-ctp-wrp");
        bitCircularTimePickerWrapper.Click();

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

        var input = component.Find("input.bit-input-hidden");
        Assert.IsNotNull(input);
    }

    [TestMethod]
    public void BitCircularTimePickerCalloutHtmlAttributesTest()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var calloutHtmlAttributes = new Dictionary<string, object>
        {
            { "data-test", "data-value" }
        };

        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.CalloutHtmlAttributes, calloutHtmlAttributes);
        });

        var callout = component.Find(".bit-ctp-cac");
        var calloutStyle = callout.GetAttribute("data-test");

        Assert.AreEqual("data-value", calloutStyle);
    }
}
