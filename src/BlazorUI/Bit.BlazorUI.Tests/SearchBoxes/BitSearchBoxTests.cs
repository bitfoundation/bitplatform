using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.SearchBoxes;

[TestClass]
public class BitSearchBoxTests : BunitTestContext
{
    [DataTestMethod,
        DataRow("Search"),
        DataRow("Filter")]
    public void SearchBoxPlaceholderMeetEnteredValue(string componentPlaceholder)
    {
        var component = RenderComponent<BitSearchBoxTest>(parameter => parameter.Add(p => p.Placeholder, componentPlaceholder));

        var input = component.Find(".input");

        var inputPlaceholder = input.GetAttribute("placeholder");

        Assert.AreEqual(componentPlaceholder, inputPlaceholder);
    }

    [DataTestMethod,
        DataRow("Search"),
        DataRow("Closed Issue"),
        DataRow("fake value")]
    public void SearchBoxDefaultValueMeetEnteredValue(string value)
    {
        var component = RenderComponent<BitSearchBoxTest>(parameter => parameter.Add(p => p.Value, value));

        var input = component.Find(".input");

        var inputValue = input.GetAttribute("value");

        Assert.AreEqual(value, inputValue);
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)]
    public void SearchBoxNoAnimationShouldHaveClassName(bool disableAnimation)
    {
        var component = RenderComponent<BitSearchBoxTest>(parameter => parameter.Add(p => p.DisableAnimation, disableAnimation));

        var searchBox = component.Find(".bit-srb");

        Assert.AreEqual(disableAnimation, searchBox.ClassList.Contains("no-animation"));
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)]
    public void SearchBoxUnderlinedShouldHaveClassName(bool isUnderlined)
    {
        var component = RenderComponent<BitSearchBoxTest>(parameter => parameter.Add(p => p.IsUnderlined, isUnderlined));

        var searchBox = component.Find(".bit-srb");

        Assert.AreEqual(isUnderlined, searchBox.ClassList.Contains("underlined"));
    }

    [DataTestMethod,
        DataRow("Detailed label")]
    public void BitSearchBoxAriaLabelTest(string ariaLabel)
    {
        var com = RenderComponent<BitSearchBoxTest>(parameters => parameters.Add(p => p.AriaLabel, ariaLabel));

        var bitSearchBox = com.Find(".input");

        Assert.IsTrue(bitSearchBox.GetAttribute("aria-label").Equals(ariaLabel));
    }

    [DataTestMethod,
        DataRow("hello world", "hello bit"),
        DataRow(null, "hello bit"),
        DataRow("hello world", null)
    ]
    public void BitSearchBoxShouldTakeDefaultValue(string value, string defaultValue)
    {
        var component = RenderComponent<BitSearchBoxTest>(parameters =>
        {
            parameters.Add(p => p.Value, value);
            parameters.Add(p => p.DefaultValue, defaultValue);
        });

        var input = component.Find(".input");
        var actualValue = string.IsNullOrEmpty(value) ? defaultValue : value;

        Assert.AreEqual(input.GetAttribute("value"), actualValue);
    }

    [DataTestMethod,
        DataRow("hello world", true),
        DataRow("hello world", false)
    ]
    public void BitSearchBoxedMustShowSearchIconEvenHasValueWhenShowIconTrue(string value, bool showIcon)
    {
        var component = RenderComponent<BitSearchBoxTest>(parameters =>
        {
            parameters.Add(p => p.Value, value);
            parameters.Add(p => p.ShowIcon, showIcon);
        });

        var bitSearchBox = component.Find(".bit-srb");

        Assert.AreEqual(showIcon, bitSearchBox.ClassList.Contains("fixed-icon-has-value"));
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitSearchBoxMustRespondToTheChangeEvent(bool isEnabled)
    {
        var component = RenderComponent<BitSearchBoxTest>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var input = component.Find(".input");

        //TODO: bypassed - BUnit oninput event issue
        //input.KeyDown("a");
        //Assert.AreEqual(isEnabled ? 1 : 0, component.Instance.CurrentCount);
    }

    [DataTestMethod,
        DataRow(null),
        DataRow("off"),
        DataRow("email")
    ]
    public void BitSearchBoxAutoCompleteTest(string autoComplete)
    {
        var component = RenderComponent<BitSearchBoxTest>(parameters =>
        {
            parameters.Add(p => p.AutoComplete, autoComplete);
            parameters.Add(p => p.IsEnabled, true);
        });

        var input = component.Find(".input");

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
        });

        var isValid = value == "test@bit.com" || value == "test@bit-components.com";

        var form = component.Find("form");
        form.Submit();

        Assert.AreEqual(component.Instance.ValidCount, isValid ? 1 : 0);
        Assert.AreEqual(component.Instance.InvalidCount, isValid ? 0 : 1);

        var input = component.Find("input.input");
        if (isValid)
        {
            input.Input("bit.com");
        }
        else
        {
            input.Input("test@bit.com");
        }

        form.Submit();

        Assert.AreEqual(component.Instance.ValidCount, 1);
        Assert.AreEqual(component.Instance.InvalidCount, 1);
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
        });

        var isInvalid = value != "test@bit.com" && value != "test@bit-components.com";

        var input = component.Find("input");
        Assert.IsFalse(input.HasAttribute("aria-invalid"));

        var form = component.Find("form");
        form.Submit();

        Assert.AreEqual(input.HasAttribute("aria-invalid"), isInvalid);
        if (input.HasAttribute("aria-invalid"))
        {
            Assert.AreEqual(input.GetAttribute("aria-invalid"), "true");
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
        });

        var isInvalid = value != "test@bit.com";

        var bitSearchBox = component.Find(".bit-srb");

        Assert.IsFalse(bitSearchBox.ClassList.Contains("invalid"));

        var form = component.Find("form");
        form.Submit();

        Assert.AreEqual(bitSearchBox.ClassList.Contains("invalid"), isInvalid);

        var input = component.Find("input");
        if (isInvalid)
        {
            input.Input("test@bit.com");
        }
        else
        {
            input.Input("abc123");
        }

        Assert.AreEqual(bitSearchBox.ClassList.Contains("invalid"), !isInvalid);
    }
}
