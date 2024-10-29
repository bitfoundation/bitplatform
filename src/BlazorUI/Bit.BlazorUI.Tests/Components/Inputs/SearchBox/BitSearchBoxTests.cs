using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Inputs.SearchBox;

[TestClass]
public class BitSearchBoxTests : BunitTestContext
{
    [DataTestMethod,
        DataRow("Search"),
        DataRow("Filter")]
    public void SearchBoxPlaceholderMeetEnteredValue(string componentPlaceholder)
    {
        var component = RenderComponent<BitSearchBox>(parameter => parameter.Add(p => p.Placeholder, componentPlaceholder));

        var input = component.Find(".bit-srb-inp");
        var inputPlaceholder = input.GetAttribute("placeholder");

        Assert.AreEqual(componentPlaceholder, inputPlaceholder);
    }

    [DataTestMethod,
        DataRow("Search"),
        DataRow("Closed Issue"),
        DataRow("fake value")]
    public void SearchBoxDefaultValueMeetEnteredValue(string value)
    {
        var component = RenderComponent<BitSearchBox>(parameter => parameter.Add(p => p.Value, value));

        var input = component.Find(".bit-srb-inp");
        var inputValue = input.GetAttribute("value");

        Assert.AreEqual(value, inputValue);
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)]
    public void SearchBoxNoAnimationShouldHaveClassName(bool disableAnimation)
    {
        var component = RenderComponent<BitSearchBox>(parameter => parameter.Add(p => p.DisableAnimation, disableAnimation));

        var searchBox = component.Find(".bit-srb");

        Assert.AreEqual(disableAnimation, searchBox.ClassList.Contains("bit-srb-nan"));
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)]
    public void SearchBoxUnderlinedShouldHaveClassName(bool isUnderlined)
    {
        var component = RenderComponent<BitSearchBox>(parameter => parameter.Add(p => p.Underlined, isUnderlined));

        var searchBox = component.Find(".bit-srb");

        Assert.AreEqual(isUnderlined, searchBox.ClassList.Contains("bit-srb-und"));
    }

    [DataTestMethod,
        DataRow("Detailed label")]
    public void BitSearchBoxAriaLabelTest(string ariaLabel)
    {
        var com = RenderComponent<BitSearchBox>(parameters => parameters.Add(p => p.AriaLabel, ariaLabel));

        var bitSearchBox = com.Find(".bit-srb-inp");

        Assert.IsTrue(bitSearchBox.GetAttribute("aria-label").Equals(ariaLabel));
    }

    [DataTestMethod,
        DataRow("hello world", "hello bit"),
        DataRow(null, "hello bit"),
        DataRow("hello world", null)
    ]
    public void BitSearchBoxShouldTakeDefaultValue(string value, string defaultValue)
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Bind(p => p.Value, value, v => value = v);
            parameters.Add(p => p.DefaultValue, defaultValue);
        });

        var input = component.Find(".bit-srb-inp");
        var actualValue = string.IsNullOrEmpty(value) ? defaultValue : value;

        Assert.AreEqual(input.GetAttribute("value"), actualValue);
    }

    [DataTestMethod,
        DataRow("hello world", true),
        DataRow("hello world", false)
    ]
    public void BitSearchBoxedMustShowSearchIconEvenHasValueWhenShowIconTrue(string value, bool fixedIcon)
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Value, value);
            parameters.Add(p => p.FixedIcon, fixedIcon);
        });

        var bitSearchBox = component.Find(".bit-srb");

        Assert.AreEqual(fixedIcon, bitSearchBox.ClassList.Contains("bit-srb-fic-hvl"));
    }

    [Ignore("bypassed - BUnit oninput event issue")]
    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitSearchBoxMustRespondToTheChangeEvent(bool isEnabled)
    {
        int currentCount = 0;
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.OnChange, () => currentCount++);
        });

        var input = component.Find(".bit-srb-inp");

        //TODO: bypassed - BUnit oninput event issue
        input.KeyDown("a");
        Assert.AreEqual(isEnabled ? 1 : 0, currentCount);
    }

    [DataTestMethod,
        DataRow(null),
        DataRow("off"),
        DataRow("email")
    ]
    public void BitSearchBoxAutoCompleteTest(string autoComplete)
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.AutoComplete, autoComplete);
            parameters.Add(p => p.IsEnabled, true);
        });

        var input = component.Find(".bit-srb-inp");

        if (autoComplete.HasValue())
        {
            Assert.IsTrue(input.HasAttribute("autocomplete"));
            Assert.AreEqual(autoComplete, input.GetAttribute("autocomplete"));
        }
        else
        {
            Assert.IsFalse(input.HasAttribute("autocomplete"));
        }
    }

    [DataTestMethod,
        DataRow(null),
        DataRow("abc123"),
        DataRow("test@bit-components.com"),
        DataRow("test@bit.com"),
    ]
    public void BitSearchBoxValidationFormTest(string value)
    {
        var component = RenderComponent<BitSearchBoxValidationTest>(parameters =>
        {
            parameters.Add(p => p.TestModel, new BitSearchBoxTestModel { Value = value });
            parameters.Add(p => p.IsEnabled, true);
            parameters.Add(p => p.Immediate, true);
        });

        var isValid = value == "test@bit.com" || value == "test@bit-components.com";

        var form = component.Find("form");
        form.Submit();

        Assert.AreEqual(component.Instance.ValidCount, isValid ? 1 : 0);
        Assert.AreEqual(component.Instance.InvalidCount, isValid ? 0 : 1);

        var input = component.Find("input.bit-srb-inp");
        if (isValid)
        {
            input.Input("bit.com");
        }
        else
        {
            input.Input("test@bit.com");
        }

        form.Submit();

        Assert.AreEqual(1, component.Instance.ValidCount);
        Assert.AreEqual(1, component.Instance.InvalidCount);
        Assert.AreEqual(component.Instance.ValidCount, component.Instance.InvalidCount);
    }

    [DataTestMethod,
        DataRow(null),
        DataRow("abc123"),
        DataRow("test@bit-components.com"),
        DataRow("test@bit.com"),
    ]
    public void BitSearchBoxValidationInvalidHtmlAttributeTest(string value)
    {
        var component = RenderComponent<BitSearchBoxValidationTest>(parameters =>
        {
            parameters.Add(p => p.TestModel, new BitSearchBoxTestModel { Value = value });
            parameters.Add(p => p.IsEnabled, true);
            parameters.Add(p => p.Immediate, true);
        });

        var isInvalid = value != "test@bit.com" && value != "test@bit-components.com";

        var input = component.Find("input");
        Assert.IsFalse(input.HasAttribute("aria-invalid"));

        var form = component.Find("form");
        form.Submit();

        Assert.AreEqual(isInvalid, input.HasAttribute("aria-invalid"));
        if (input.HasAttribute("aria-invalid"))
        {
            Assert.AreEqual("true", input.GetAttribute("aria-invalid"));
        }

        if (isInvalid)
        {
            input.Input("test@bit.com");
            Assert.IsFalse(input.HasAttribute("aria-invalid"));
        }
        else
        {
            input.Input("bit-components");
            Assert.IsTrue(input.HasAttribute("aria-invalid"));
        }
    }

    [DataTestMethod,
        DataRow("abc123"),
        DataRow("test@bit.com")
    ]
    public void BitSearchBoxValidationInvalidCssClassTest(string value)
    {
        var component = RenderComponent<BitSearchBoxValidationTest>(parameters =>
        {
            parameters.Add(p => p.TestModel, new BitSearchBoxTestModel { Value = value });
            parameters.Add(p => p.IsEnabled, true);
            parameters.Add(p => p.Immediate, true);
        });

        var isInvalid = value != "test@bit.com";

        var bitSearchBox = component.Find(".bit-srb");

        Assert.IsFalse(bitSearchBox.ClassList.Contains("bit-inv"));

        var form = component.Find("form");
        form.Submit();

        Assert.AreEqual(isInvalid, bitSearchBox.ClassList.Contains("bit-inv"));

        var input = component.Find("input");
        if (isInvalid)
        {
            input.Input("test@bit.com");
        }
        else
        {
            input.Input("abc123");
        }

        Assert.AreEqual(isInvalid is false, bitSearchBox.ClassList.Contains("bit-inv"));
    }
}
