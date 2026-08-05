using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Inputs.Dropdown;

[TestClass]
public class BitDropdownTests : BunitTestContext
{
    private string? _bitDropdownValue;
    private IEnumerable<string?>? _bitDropdownValues;

    [TestMethod,
      DataRow(true),
      DataRow(false)
    ]
    public void BitDropdownTest(bool isEnabled)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var bitDropdown = component.Find(".bit-drp");

        if (isEnabled)
        {
            Assert.IsFalse(bitDropdown.ClassList.Contains("bit-dis"));
        }
        else
        {
            Assert.IsTrue(bitDropdown.ClassList.Contains("bit-dis"));
        }
    }

    [TestMethod,
      DataRow(true),
      DataRow(false)
    ]
    public void ResponsiveDropdownShouldTakeCorrectClassNameAndRenderElements(bool isResponsiveModeEnabled)
    {
        //// since it's now handled in the JS this test needs to be changed!

        //Context.JSInterop.Mode = JSRuntimeMode.Loose;

        //var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        //{
        //    parameters.Add(p => p.IsResponsive, isResponsiveModeEnabled);
        //});

        //var callout = component.Find(".bit-drp-cal");

        //if (isResponsiveModeEnabled)
        //{
        //    Assert.IsTrue(callout.ClassList.Contains("bit-drp-res"));

        //    var lblContainer = component.Find(".bit-drp-rlc");
        //    Assert.IsNotNull(lblContainer);
        //}
        //else
        //{
        //    Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-drp-rlc"));
        //}
    }

    [TestMethod,
      DataRow(null),
      DataRow("BitDrop")
    ]
    public void ResponsiveDropdownShouldRenderLabel(string labelFragment)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Responsive, true);
            parameters.Add(p => p.Label, labelFragment);
        });

        if (string.IsNullOrEmpty(labelFragment))
        {
            Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-drp-rlc > label"));
        }
        else
        {
            Assert.AreEqual(labelFragment, component.Find(".bit-drp-rlc > label").InnerHtml);
        }
    }

    [TestMethod,
      DataRow(null),
      DataRow("<div>This is labelFragment</div>"),
    ]
    public void ResponsiveDropdownShouldRenderLabelFragment(string labelFragment)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Responsive, true);

            if (string.IsNullOrEmpty(labelFragment) is false)
            {
                parameters.Add(p => p.LabelTemplate, labelFragment);
            }
        });

        if (string.IsNullOrEmpty(labelFragment))
        {
            Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-drp-rlc > label"));
        }
        else
        {
            var labelChild = component.Find(".bit-drp-rlc > :first-child");
            labelChild.MarkupMatches(labelFragment);
        }
    }

    [TestMethod,
      DataRow(true),
      DataRow(false)
    ]
    public void BitDropdownOnClickShouldWorkCorrect(bool isEnabled)
    {
        var clicked = false;
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.OnClick, () => clicked = true);
        });

        var wrapper = component.Find(".bit-drp-wrp");
        wrapper.Click();

        Assert.AreEqual(isEnabled, clicked);
    }

    [TestMethod,
      DataRow(true),
      DataRow(false)
    ]
    public void BitDropdownIsMultiSelectShouldWorkCorrect(bool isMultiSelect)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var isOpen = true;

        var items = BitDropdownTests.GetDropdownItems();
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.IsOpen, isOpen);
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.MultiSelect, isMultiSelect);
        });

        var bitDropdown = component.Find(".bit-drp");

        if (isMultiSelect)
        {
            Assert.AreEqual(items.FindAll(i => i.ItemType == BitDropdownItemType.Normal).Count, component.FindAll(".bit-drp-iwr").Count);
        }
        else
        {
            Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-drp-iwr"));
        }
    }

    [TestMethod,
      DataRow(true),
      DataRow(false)
    ]
    public void BitDropdownItemsShouldRenderCorrect(bool isMultiSelect)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var isOpen = true;
        var items = BitDropdownTests.GetDropdownItems();
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.IsOpen, isOpen);
            parameters.Add(p => p.IsOpenChanged, v => isOpen = v);
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.MultiSelect, isMultiSelect);
        });

        Assert.AreEqual(items.FindAll(i => i.ItemType == BitDropdownItemType.Header).Count, component.FindAll(".bit-drp-ihd").Count);
        Assert.AreEqual(items.FindAll(i => i.ItemType == BitDropdownItemType.Divider).Count, component.FindAll(".bit-drp-sep").Count);

        if (isMultiSelect)
        {
            Assert.AreEqual(items.FindAll(i => i.ItemType == BitDropdownItemType.Normal).Count, component.FindAll(".bit-drp-iwr").Count);
        }
        else
        {
            Assert.AreEqual(items.FindAll(i => i.ItemType == BitDropdownItemType.Normal).Count, component.FindAll(".bit-drp-itm").Count);
        }
    }

    [TestMethod,
      DataRow("f-ban"),
      DataRow("f-app")
    ]
    public void BitDropdownTextWithDefaultValueShouldInitCorrect(string defaultValue)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var items = BitDropdownTests.GetDropdownItems();
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.DefaultValue, defaultValue);
        });

        var textSpan = component.Find(".bit-drp-tcn");
        var expectedText = items?.Find(i => i.Value == defaultValue && i.ItemType == BitDropdownItemType.Normal)?.Text;

        Assert.AreEqual(expectedText, textSpan.InnerHtml);
    }

    [TestMethod,
      DataRow("f-ban"),
      DataRow("f-app,f-ban")
    ]
    public void BitDropdownTextWithDefaultValuesShouldInitCorrect(string defaultValues)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var items = GetDropdownItems();
        var defaultSelectedMultipleValueList = defaultValues.Split(",").ToArray();
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.MultiSelect, true);
            parameters.Add(p => p.DefaultValues, defaultSelectedMultipleValueList);
        });

        var textSpan = component.Find(".bit-drp-tcn");
        var defaultSelectedItems = component.Instance.SelectedItems.ToList();
        var expectedText = "";

        defaultSelectedItems.ForEach(i =>
        {
            if (expectedText.HasValue())
            {
                expectedText += component.Instance.MultiSelectDelimiter;
            }

            expectedText += i.Text;
        });

        Assert.AreEqual(expectedText, textSpan.InnerHtml);
    }

    [TestMethod,
      DataRow("f-ban", "f-app"),
      DataRow("f-app", null)
    ]
    public void BitDropdownTextWithValueShouldInitCorrect(string value, string defaultValue)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var items = GetDropdownItems();
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.DefaultValue, defaultValue);
            parameters.Add(p => p.Value, value);
        });

        var textSpan = component.Find(".bit-drp-tcn");
        var expectedText = items?.Find(i => i.Value == value && i.ItemType == BitDropdownItemType.Normal)?.Text;

        Assert.AreEqual(expectedText, textSpan.InnerHtml);
    }

    [TestMethod,
      DataRow("f-ban", "f-app,f-ban"),
      DataRow("f-app,f-ban", "f-ban")
    ]
    public void BitDropdownTextWithValuesAndDefaultValuesShouldInitCorrect(string defaultValues, string values)
    {
        var items = GetDropdownItems();
        var defaultSelectedMultipleValueList = defaultValues.Split(",").ToArray();
        var selectedMultipleValueList = values.Split(",").ToArray();
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.MultiSelect, true);
            parameters.Add(p => p.DefaultValues, defaultSelectedMultipleValueList);
            parameters.Add(p => p.Values, selectedMultipleValueList);
        });

        var textSpan = component.Find(".bit-drp-tcn");
        var selectedItems = component.Instance.SelectedItems.ToList();
        var expectedText = new StringBuilder();

        selectedItems.ForEach(i =>
        {
            if (expectedText.Length > 0)
            {
                expectedText.Append(component.Instance.MultiSelectDelimiter);
            }

            expectedText.Append(i.Text);
        });

        Assert.AreEqual(expectedText.ToString(), textSpan.InnerHtml);
    }

    [TestMethod,
      DataRow(null, "f-app,f-ban", true, "Select options"),
      DataRow(null, null, true, "Select options"),
      DataRow("f-ban", null, false, "Select option"),
      DataRow(null, null, false, "Select option")
    ]
    public void BitDropdownPlaceholderShouldWorkCorrect(string value, string values, bool isMultiSelect, string placeholder)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var items = BitDropdownTests.GetShortDropdownItems();
        var selectedMultipleValueList = values is not null ? values.Split(",").ToArray() : Array.Empty<string>();
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.MultiSelect, isMultiSelect);
            parameters.Add(p => p.Values, selectedMultipleValueList);
            parameters.Add(p => p.Value, value);
            parameters.Add(p => p.Placeholder, placeholder);
        });

        var targetSpan = component.Find(".bit-drp-tcn");
        var expectedText = new StringBuilder();

        if (isMultiSelect)
        {
            if (values is not null)
            {
                var selectedItems = items.FindAll(i => selectedMultipleValueList.Contains(i.Value) && i.ItemType == BitDropdownItemType.Normal);
                selectedItems.ForEach(item =>
                {
                    if (expectedText.Length > 0)
                    {
                        expectedText.Append(component.Instance.MultiSelectDelimiter);
                    }

                    expectedText.Append(item.Text);
                });
            }
            else
            {
                expectedText.Append(placeholder);
            }
        }
        else
        {
            if (value is not null)
            {
                expectedText.Append(items?.Find(i => i.Value == value)?.Text);
            }
            else
            {
                expectedText.Append(placeholder);
            }
        }

        Assert.AreEqual(expectedText.ToString(), targetSpan.InnerHtml);
    }

    [TestMethod,
        DataRow("Drop down"),
        DataRow(null)
    ]
    public void BitDropdownLabelShouldWorkCorrect(string label)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Label, label);
        });

        if (label is not null)
        {
            Assert.AreEqual(label, component.Find("label").InnerHtml);
        }
        else
        {
            Assert.Throws<ElementNotFoundException>(() => component.Find("label"));
        }
    }

    [TestMethod,
        DataRow("<div>This is labelFragment</div>")
    ]
    public void BitDropdownLabelFragmentTest(string labelFragment)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.LabelTemplate, labelFragment);
        });

        var drpLabelChild = component.Find("div > label > :first-child");
        drpLabelChild.MarkupMatches(labelFragment);
    }

    [TestMethod,
        DataRow("Drop Down"),
        DataRow(null)
    ]
    public void BitDropdownTitleTest(string title)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Title, title);
        });

        var drpWrapper = component.Find(".bit-drp-wrp");

        Assert.AreEqual(title, drpWrapper.GetAttribute("title"));
    }

    [TestMethod,
        DataRow(true, "f-app"),
        DataRow(false, "f-app"),
    ]
    public void BitDropdownNotifyOnReselectShouldWorkCorrect(bool notifyOnReselect, string defaultValue)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var items = BitDropdownTests.GetShortDropdownItems();
        var itemSelected = false;
        var isOpen = true;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.IsOpen, isOpen);
            parameters.Add(p => p.IsOpenChanged, v => isOpen = v);
            parameters.Add(p => p.IsEnabled, true);
            parameters.Add(p => p.Reselectable, notifyOnReselect);
            parameters.Add(p => p.DefaultValue, defaultValue);
            parameters.Add(p => p.OnSelectItem, () => itemSelected = true);
        });

        var selectedItem = component.Find(".bit-drp-sel");
        selectedItem.Click();

        Assert.AreEqual(notifyOnReselect, itemSelected);
    }

    [TestMethod,
        DataRow(true, true),
        DataRow(false, true),

        DataRow(true, false),
        DataRow(false, false)
    ]
    public void BitDropdownEnableItemSelectionShouldWorkCorrect(bool itemIsEnabled, bool isMultiSelect)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var itemsSelected = 0;
        var valuesChanged = 0;
        var isOpen = true;

        var items = new List<BitDropdownItem<string>>()
        {
            new() { Value = "Apple", Text = "f-app", IsEnabled = itemIsEnabled },
            new() { Value = "Banana", Text = "f-ban", IsEnabled = itemIsEnabled }
        };
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.IsOpen, isOpen);
            parameters.Add(p => p.IsOpenChanged, v => isOpen = v);
            parameters.Add(p => p.IsEnabled, true);
            parameters.Add(p => p.MultiSelect, isMultiSelect);
            parameters.Add(p => p.OnSelectItem, () => itemsSelected++);
            parameters.Add(p => p.OnValuesChange, () => valuesChanged++);
        });

        if (isMultiSelect)
        {
            var drpItems = component.FindAll(".bit-drp-iwr");
            drpItems[0].GetElementsByTagName("button").First().Click();
            drpItems[1].GetElementsByTagName("button").First().Click();
            var expectedResult = itemIsEnabled ? 2 : 0;
            Assert.AreEqual(expectedResult, itemsSelected);
            Assert.AreEqual(expectedResult, valuesChanged);
        }
        else
        {
            var drpItems = component.FindAll(".bit-drp-itm");
            drpItems[0].Click();
            var expectedResult = itemIsEnabled ? 1 : 0;
            Assert.AreEqual(expectedResult, itemsSelected);
            // Values only exists in multi select mode, so OnValuesChange stays quiet here.
            Assert.AreEqual(0, valuesChanged);
        }
    }

    [TestMethod,
        DataRow("f-ban"),
        DataRow("f-ora"),
        DataRow("v-bro")
    ]
    public void BitDropdownTwoWayBoundWithCustomHandlerShouldWorkCorrect(string value)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        _bitDropdownValue = value;
        var isOpen = true;

        var items = BitDropdownTests.GetShortDropdownItems();
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.IsOpen, isOpen);
            parameters.Add(p => p.IsOpenChanged, v => isOpen = v);
            parameters.Add(p => p.IsEnabled, true);
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Value, _bitDropdownValue);
            parameters.Add(p => p.ValueChanged, HandleValueChanged);
        });

        var drpItems = component.FindAll(".bit-drp-itm");
        drpItems[3].Click();

        var expectedValue = items[3].Value;

        Assert.AreEqual(expectedValue, _bitDropdownValue);
    }

    [TestMethod,
        DataRow("f-ban,v-bro"),
        DataRow("f-ora")
    ]
    public void BitDropdownMultiSelectTwoWayBoundWithCustomHandlerShouldWorkCorrect(string values)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var isOpen = true;

        _bitDropdownValues = [.. values.Split(",")];
        var initialValuesCount = _bitDropdownValues.Count();
        var items = GetShortDropdownItems();
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.IsOpen, isOpen);
            parameters.Add(p => p.IsOpenChanged, v => isOpen = v);
            parameters.Add(p => p.IsEnabled, true);
            parameters.Add(p => p.MultiSelect, true);
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Values, _bitDropdownValues);
            parameters.Add(p => p.ValuesChanged, HandleValuesChanged);
        });

        var drpItems = component.FindAll(".bit-drp-iwr");
        drpItems[3].Children[0].Children[0].Click();

        int expectedResult;
        if (values.Contains(items[3].Value!))
        {
            expectedResult = initialValuesCount - 1;
        }
        else
        {
            expectedResult = initialValuesCount + 1;
        }

        Assert.AreEqual(expectedResult, _bitDropdownValues.Count());
    }

    [TestMethod,
        DataRow(null),
        DataRow("f-ora")
    ]
    public void BitDropdownValidationFormTest(string value)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var items = BitDropdownTests.GetShortDropdownItems();
        var component = RenderComponent<BitDropdownValidationTest>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, true);
            parameters.Add(p => p.IsMultiSelect, false);
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.TestModel, new BitDropdownTestModel { Value = value });
        });

        var isValid = value.HasValue();

        var form = component.Find("form");
        form.Submit();

        Assert.AreEqual(component.Instance.ValidCount, isValid ? 1 : 0);
        Assert.AreEqual(component.Instance.InvalidCount, isValid ? 0 : 1);

        if (isValid is false)
        {
            // open dropdown
            var drp = component.Find(".bit-drp-wrp");
            drp.Click();

            // select item
            var drpItems = component.FindAll(".bit-drp-itm");
            drpItems[0].Click();

            form.Submit();

            Assert.AreEqual(1, component.Instance.ValidCount);
            Assert.AreEqual(1, component.Instance.InvalidCount);
            Assert.AreEqual(component.Instance.ValidCount, component.Instance.InvalidCount);
        }
    }

    [TestMethod,
        DataRow(null),
        DataRow("f-ban,v-bro"),
        DataRow("f-ora")
    ]
    public void BitDropdownMultiSelectValidationFormTest(string values)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        _bitDropdownValues = values.HasValue() ? values.Split(",").ToArray() : null;
        var items = GetShortDropdownItems();
        var component = RenderComponent<BitDropdownMultiSelectValidationTest>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, true);
            parameters.Add(p => p.IsMultiSelect, true);
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.TestModel, new BitDropdownMultiSelectTestModel { Values = _bitDropdownValues });
        });

        var isValid = (_bitDropdownValues?.Count() ?? 0) == 2;

        var form = component.Find("form");
        form.Submit();

        Assert.AreEqual(isValid ? 1 : 0, component.Instance.ValidCount);
        Assert.AreEqual(isValid ? 0 : 1, component.Instance.InvalidCount);

        if (isValid is false)
        {
            // open dropdown
            var drp = component.Find(".bit-drp-wrp");
            drp.Click();

            // select items
            //var drpItemFirst = component.Find(".bit-drp-iwr:first-child");
            //drpItemFirst.Children[0].Click();

            //var drpItemLast = component.Find(".bit-drp-iwr:last-child");
            //drpItemLast.Children[0].Click();

            form.Submit();

            //TODO: bypassed - BUnit 2-way bound parameters issue
            //Assert.AreEqual(component.Instance.ValidCount, 1);
            //Assert.AreEqual(component.Instance.InvalidCount, 1);
            //Assert.AreEqual(component.Instance.ValidCount, component.Instance.InvalidCount);
        }
    }

    [TestMethod,
        DataRow(null),
        DataRow("f-ora")
    ]
    public void BitDropdownValidationInvalidHtmlAttributeTest(string value)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var items = BitDropdownTests.GetShortDropdownItems();
        var component = RenderComponent<BitDropdownValidationTest>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, true);
            parameters.Add(p => p.IsMultiSelect, false);
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.TestModel, new BitDropdownTestModel { Value = value });
        });

        var isInvalid = value.HasNoValue();

        var selectTag = component.Find("select");
        Assert.IsFalse(selectTag.HasAttribute("aria-invalid"));

        var form = component.Find("form");
        form.Submit();

        Assert.AreEqual(isInvalid, selectTag.HasAttribute("aria-invalid"));
        if (selectTag.HasAttribute("aria-invalid"))
        {
            Assert.AreEqual("true", selectTag.GetAttribute("aria-invalid"));
        }

        if (isInvalid)
        {
            // open dropdown
            var drp = component.Find(".bit-drp-wrp");
            drp.Click();

            // select item
            var drpItems = component.FindAll(".bit-drp-itm");
            drpItems[0].Click();

            Assert.IsFalse(selectTag.HasAttribute("aria-invalid"));
        }
    }

    [TestMethod,
        DataRow(null),
        DataRow("f-ban,v-bro"),
        DataRow("f-ora")
    ]
    public void BitDropdownMultiSelectValidationInvalidHtmlAttributeTest(string values)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        _bitDropdownValues = values.HasValue() ? values.Split(",").ToArray() : null;
        var items = BitDropdownTests.GetShortDropdownItems();
        var component = RenderComponent<BitDropdownMultiSelectValidationTest>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, true);
            parameters.Add(p => p.IsMultiSelect, true);
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.TestModel, new BitDropdownMultiSelectTestModel { Values = _bitDropdownValues });
        });

        var isInvalid = (_bitDropdownValues?.Count() ?? 0) != 2;

        var selectTag = component.Find("select");
        Assert.IsFalse(selectTag.HasAttribute("aria-invalid"));

        var form = component.Find("form");
        form.Submit();

        Assert.AreEqual(selectTag.HasAttribute("aria-invalid"), isInvalid);
        if (selectTag.HasAttribute("aria-invalid"))
        {
            Assert.AreEqual("true", selectTag.GetAttribute("aria-invalid"));
        }

        if (isInvalid)
        {
            // open dropdown
            var drp = component.Find(".bit-drp-wrp");
            drp.Click();

            // select items
            //var drpItemFirst = component.Find(".bit-drp-iwr:first-child");
            //drpItemFirst.Children[0].Click();

            //var drpItemLast = component.Find(".bit-drp-iwr:last-child");
            //drpItemLast.Children[0].Click();

            form.Submit();

            //TODO: bypassed - BUnit 2-way bound parameters issue
            //Assert.IsFalse(selectTag.HasAttribute("aria-invalid"));
        }
    }

    [TestMethod,
        DataRow(null),
        DataRow("f-ora")
    ]
    public void BitDropdownValidationInvalidCssClassTest(string value)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var items = BitDropdownTests.GetShortDropdownItems();
        var component = RenderComponent<BitDropdownValidationTest>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, true);
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.TestModel, new BitDropdownTestModel { Value = value });
        });

        var isInvalid = value.HasNoValue();

        var bitDropdown = component.Find(".bit-drp");

        Assert.IsFalse(bitDropdown.ClassList.Contains("bit-inv"));

        var form = component.Find("form");
        form.Submit();

        Assert.AreEqual(bitDropdown.ClassList.Contains("bit-inv"), isInvalid);

        if (isInvalid)
        {
            // open dropdown
            var drp = component.Find(".bit-drp-wrp");
            drp.Click();

            // select item
            var drpItems = component.FindAll(".bit-drp-itm");
            drpItems[0].Click();
        }

        Assert.IsFalse(bitDropdown.ClassList.Contains("bit-inv"));
    }

    [TestMethod,
        DataRow(true, null),
        DataRow(true, "Search items"),
        DataRow(false, null),
        DataRow(false, "Search items")
    ]
    public void BitDropdownShowSearchBoxTest(bool showSearchBox, string searchBoxPlaceholder)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var items = BitDropdownTests.GetShortDropdownItems();
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, true);
            parameters.Add(p => p.ShowSearchBox, showSearchBox);
            parameters.Add(p => p.SearchBoxPlaceholder, searchBoxPlaceholder);
            parameters.Add(p => p.Items, items);
        });

        var bitDropdown = component.Find(".bit-drp-wrp");
        bitDropdown.Click();

        var searchBox = component.FindAll(".bit-drp-cal .bit-drp-sb");
        if (showSearchBox)
        {
            Assert.AreEqual(1, searchBox.Count);

            var searchInput = component.Find(".bit-drp-sin");
            var inputPlaceholder = searchInput.GetAttribute("placeholder");

            Assert.AreEqual(searchBoxPlaceholder, inputPlaceholder);
        }
        else
        {
            Assert.AreEqual(0, searchBox.Count);
        }
    }

    [TestMethod,
        DataRow(null, false),
        DataRow("app", false),
        DataRow(null, true),
        DataRow("app", true)
    ]
    public void BitDropdownSearchItemTest(string search, bool isMultiSelect)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var items = BitDropdownTests.GetShortDropdownItems();
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, true);
            parameters.Add(p => p.ShowSearchBox, true);
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MultiSelect, isMultiSelect);
            parameters.Add(p => p.Items, items);
        });

        var bitDropdown = component.Find(".bit-drp-wrp");
        bitDropdown.Click();

        var drpItems = component.FindAll(isMultiSelect ? ".bit-drp-iwr" : ".bit-drp-itm");

        Assert.AreEqual(items.Count, drpItems.Count);

        var searchInput = component.Find(".bit-drp-sin");
        searchInput.Input(search);

        var itemCount = string.IsNullOrEmpty(search) ? items.Count : items.Count(item => item.Text?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false);
        // bUnit v2 removed auto-refreshing FindAll collections, so re-query after the DOM changes.
        drpItems = component.FindAll(isMultiSelect ? ".bit-drp-iwr" : ".bit-drp-itm");
        Assert.AreEqual(itemCount, drpItems.Count);

        if (string.IsNullOrEmpty(search) is false)
        {
            searchInput.Input(string.Empty);
            drpItems = component.FindAll(isMultiSelect ? ".bit-drp-iwr" : ".bit-drp-itm");
            Assert.AreEqual(items.Count, drpItems.Count);
        }
    }

    [TestMethod,
        DataRow(null, false),
        DataRow("app", false),
        DataRow(null, true),
        DataRow("app", true)
    ]
    public void BitDropdownSearchItemOnChangeTest(string search, bool isMultiSelect)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var items = BitDropdownTests.GetShortDropdownItems();
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, true);
            parameters.Add(p => p.ShowSearchBox, true);
            parameters.Add(p => p.MultiSelect, isMultiSelect);
            parameters.Add(p => p.Items, items);
        });

        var bitDropdown = component.Find(".bit-drp-wrp");
        bitDropdown.Click();

        var drpItems = component.FindAll(isMultiSelect ? ".bit-drp-iwr" : ".bit-drp-itm");

        Assert.AreEqual(items.Count, drpItems.Count);

        var searchInput = component.Find(".bit-drp-sin");

        // Without Immediate, typing (oninput) should not filter the items.
        searchInput.Input(search);
        // bUnit v2 removed auto-refreshing FindAll collections, so re-query after the DOM changes.
        drpItems = component.FindAll(isMultiSelect ? ".bit-drp-iwr" : ".bit-drp-itm");
        Assert.AreEqual(items.Count, drpItems.Count);

        // Filtering happens on the change event instead.
        searchInput.Change(search);

        var itemCount = string.IsNullOrEmpty(search) ? items.Count : items.Count(item => item.Text?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false);
        drpItems = component.FindAll(isMultiSelect ? ".bit-drp-iwr" : ".bit-drp-itm");
        Assert.AreEqual(itemCount, drpItems.Count);
    }

    [TestMethod,
        DataRow(null, false),
        DataRow("app", false),
        DataRow(null, true),
        DataRow("app", true)
    ]
    public void BitDropdownComboSearchItemTest(string search, bool isMultiSelect)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var items = BitDropdownTests.GetShortDropdownItems();
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, true);
            parameters.Add(p => p.Combo, true);
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MultiSelect, isMultiSelect);
            parameters.Add(p => p.Items, items);
        });

        var bitDropdown = component.Find(".bit-drp-wrp");
        bitDropdown.Click();

        var drpItems = component.FindAll(isMultiSelect ? ".bit-drp-iwr" : ".bit-drp-itm");

        Assert.AreEqual(items.Count, drpItems.Count);

        var comboInput = component.Find(".bit-drp-inp");
        comboInput.Input(search);

        var itemCount = string.IsNullOrEmpty(search) ? items.Count : items.Count(item => item.Text?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false);
        // bUnit v2 removed auto-refreshing FindAll collections, so re-query after the DOM changes.
        drpItems = component.FindAll(isMultiSelect ? ".bit-drp-iwr" : ".bit-drp-itm");
        Assert.AreEqual(itemCount, drpItems.Count);

        if (string.IsNullOrEmpty(search) is false)
        {
            comboInput.Input(string.Empty);
            drpItems = component.FindAll(isMultiSelect ? ".bit-drp-iwr" : ".bit-drp-itm");
            Assert.AreEqual(items.Count, drpItems.Count);
        }
    }

    [TestMethod,
        DataRow(null, false),
        DataRow("app", false),
        DataRow(null, true),
        DataRow("app", true)
    ]
    public void BitDropdownComboSearchItemOnChangeTest(string search, bool isMultiSelect)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var items = BitDropdownTests.GetShortDropdownItems();
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, true);
            parameters.Add(p => p.Combo, true);
            parameters.Add(p => p.MultiSelect, isMultiSelect);
            parameters.Add(p => p.Items, items);
        });

        var bitDropdown = component.Find(".bit-drp-wrp");
        bitDropdown.Click();

        var drpItems = component.FindAll(isMultiSelect ? ".bit-drp-iwr" : ".bit-drp-itm");

        Assert.AreEqual(items.Count, drpItems.Count);

        var comboInput = component.Find(".bit-drp-inp");

        // The non-Immediate combo commits its search on the change event (HandleOnComboChange).
        comboInput.Change(search);

        var itemCount = string.IsNullOrEmpty(search) ? items.Count : items.Count(item => item.Text?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false);
        // bUnit v2 removed auto-refreshing FindAll collections, so re-query after the DOM changes.
        drpItems = component.FindAll(isMultiSelect ? ".bit-drp-iwr" : ".bit-drp-itm");
        Assert.AreEqual(itemCount, drpItems.Count);
    }

    [TestMethod,
        DataRow(false, null, null, false),
        DataRow(false, 3_000_000, null, false),
        DataRow(false, null, 4, false),
        DataRow(false, 3_000_000, 4, false),

        DataRow(true, null, null, false),
        DataRow(true, 3_000_000, null, false),
        DataRow(true, null, 4, false),
        DataRow(true, 3_000_000, 4, false),

        DataRow(false, null, null, true),
        DataRow(false, 3_000_000, null, true),
        DataRow(false, null, 4, true),
        DataRow(false, 3_000_000, 4, true),

        DataRow(true, null, null, true),
        DataRow(true, 3_000_000, null, true),
        DataRow(true, null, 4, true),
        DataRow(true, 3_000_000, 4, true)
    ]
    public void BitDropdownVirtualizeTest(bool virtualize, int? itemSize, int? overscanCount, bool isMultiSelect)
    {
        //https://bunit.dev/docs/test-doubles/emulating-ijsruntime.html#-jsinterop-emulation
        const double viewportHeight = 1_000_000_000;
        var items = GetRangeDropdownItems(500);
        var maxItemCount = 100;

        // To ensure a consistent display structure in the Virtualize component across .NET 8, .NET 9, and .NET 10,
        // we've set the default value of MaxItemCount to 100. This means that even if a higher value is specified,
        // only a maximum of 100 items will be rendered by default.
        AppContext.SetData("Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize.MaxItemCount", maxItemCount);

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, true);
            parameters.Add(p => p.Virtualize, virtualize);
            parameters.Add(p => p.MultiSelect, isMultiSelect);
            parameters.Add(p => p.Items, items);

            if (itemSize.HasValue)
            {
                parameters.Add(p => p.ItemSize, itemSize.Value);
            }

            if (overscanCount.HasValue)
            {
                parameters.Add(p => p.OverscanCount, overscanCount.Value);
            }
        });

        var bitDropdown = component.Find(".bit-drp-wrp");
        bitDropdown.Click();

        var drpItems = component.FindAll(isMultiSelect ? ".bit-drp-iwr" : ".bit-drp-itm");
        var actualRenderedItemCount = drpItems.Count;

        if (virtualize)
        {
            //When virtualize is true, number of rendered items is greater than number of items shown in the list by "2 * OverScanCount".
            var overscanItemsCount = 2 * component.Instance.OverscanCount;

#if NET10_0
            maxItemCount += overscanItemsCount;
#endif

            var expectedRenderedItemCount = Math.Ceiling((decimal)(viewportHeight / component.Instance.ItemSize)) + overscanItemsCount;
            expectedRenderedItemCount = Math.Min(expectedRenderedItemCount, maxItemCount);

            //When actualRenderedItemCount is smaller than expectedRenderedItemCount, so show all items in viewport then actualRenderedItemCount equals total items count
            if (actualRenderedItemCount < expectedRenderedItemCount)
            {
                Assert.AreEqual(items.Count, actualRenderedItemCount);
            }
            else
            {
                Assert.AreEqual(expectedRenderedItemCount, actualRenderedItemCount);
            }
        }
        else
        {
            Assert.AreEqual(items.Count, actualRenderedItemCount);
        }

        AppContext.SetData("Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize.MaxItemCount", null);
    }

    [TestMethod,
        DataRow("WindowsLogo"),
        DataRow("ChevronUp"),
        DataRow(null)
    ]
    public void BitDropdownCaretDownIconNameTest(string? iconName)
    {
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            if (iconName is not null)
            {
                parameters.Add(p => p.CaretDownIconName, iconName);
            }
        });

        if (iconName is not null)
        {
            Assert.IsTrue(component.Find(".bit-drp-wrp > .bit-drp-icn > i").ClassList.Contains($"bit-icon--{iconName}"));
        }
        else
        {
            Assert.IsTrue(component.Find(".bit-drp-wrp > .bit-drp-icn > i").ClassList.Contains("bit-ico-r90"));
            Assert.IsTrue(component.Find(".bit-drp-wrp > .bit-drp-icn > i").ClassList.Contains("bit-icon--ChevronRight"));
        }
    }

    [TestMethod,
        DataRow("<i>This is CaretDownTemplate</div>"),
        DataRow(null)
    ]
    public void BitDropdownCaretDownTemplateTest(string iconFragment)
    {
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            if (string.IsNullOrEmpty(iconFragment) is false)
            {
                parameters.Add(p => p.CaretDownTemplate, iconFragment);
            }
        });

        if (string.IsNullOrEmpty(iconFragment))
        {
            Assert.IsTrue(component.Find(".bit-drp-wrp > .bit-drp-icn > i").ClassList.Contains("bit-ico-r90"));
            Assert.IsTrue(component.Find(".bit-drp-wrp > .bit-drp-icn > i").ClassList.Contains("bit-icon--ChevronRight"));
        }
        else
        {
            var drpCaretDownChild = component.Find(".bit-drp-wrp > .bit-drp-icn").ChildNodes;
            drpCaretDownChild.MarkupMatches(iconFragment);
        }
    }

    [TestMethod,
        DataRow("WindowsLogo"),
        DataRow("ChevronUp"),
        DataRow(null)
    ]
    public void BitDropdownChipsRemoveIconNameTest(string? iconName)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var items = BitDropdownTests.GetShortDropdownItems();
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Chips, true);
            parameters.Add(p => p.MultiSelect, true);
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.DefaultValues, new[] { "f-app" });

            if (iconName is not null)
            {
                parameters.Add(p => p.ChipsRemoveIconName, iconName);
            }
        });

        var chipIcon = component.Find(".bit-drp-chp > .bit-drp-crb > i");

        if (iconName is not null)
        {
            Assert.IsTrue(chipIcon.ClassList.Contains($"bit-icon--{iconName}"));
        }
        else
        {
            Assert.IsTrue(chipIcon.ClassList.Contains("bit-icon--Cancel"));
        }
    }

    [TestMethod,
        DataRow("WindowsLogo"),
        DataRow("ChevronUp"),
        DataRow(null)
    ]
    public void BitDropdownClearButtonIconNameTest(string? iconName)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var items = BitDropdownTests.GetShortDropdownItems();
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.ShowClearButton, true);
            parameters.Add(p => p.DefaultValue, "f-app");
            parameters.Add(p => p.Items, items);

            if (iconName is not null)
            {
                parameters.Add(p => p.ClearButtonIconName, iconName);
            }
        });

        var clearIcon = component.Find(".bit-drp-clr > i");

        if (iconName is not null)
        {
            Assert.IsTrue(clearIcon.ClassList.Contains($"bit-icon--{iconName}"));
        }
        else
        {
            Assert.IsTrue(clearIcon.ClassList.Contains("bit-icon--Cancel"));
        }
    }

    [TestMethod,
        DataRow("WindowsLogo"),
        DataRow("ChevronUp"),
        DataRow(null)
    ]
    public void BitDropdownResponsiveCloseIconNameTest(string? iconName)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Responsive, true);

            if (iconName is not null)
            {
                parameters.Add(p => p.ResponsiveCloseIconName, iconName);
            }
        });

        var closeIcon = component.Find(".bit-drp-cls > span > i");

        if (iconName is not null)
        {
            Assert.IsTrue(closeIcon.ClassList.Contains($"bit-icon--{iconName}"));
        }
        else
        {
            Assert.IsTrue(closeIcon.ClassList.Contains("bit-icon--Cancel"));
        }
    }

    [TestMethod,
        DataRow("WindowsLogo"),
        DataRow("ChevronUp"),
        DataRow(null)
    ]
    public void BitDropdownSearchBoxIconNameTest(string? iconName)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var items = BitDropdownTests.GetShortDropdownItems();
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.ShowSearchBox, true);
            parameters.Add(p => p.Items, items);

            if (iconName is not null)
            {
                parameters.Add(p => p.SearchBoxIconName, iconName);
            }
        });

        var searchIcon = component.Find(".bit-drp-sic > i");

        if (iconName is not null)
        {
            Assert.IsTrue(searchIcon.ClassList.Contains($"bit-icon--{iconName}"));
        }
        else
        {
            Assert.IsTrue(searchIcon.ClassList.Contains("bit-icon--Search"));
        }
    }

    [TestMethod,
        DataRow("WindowsLogo"),
        DataRow("ChevronUp"),
        DataRow(null)
    ]
    public void BitDropdownSearchBoxClearIconNameTest(string? iconName)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var items = BitDropdownTests.GetShortDropdownItems();
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.ShowSearchBox, true);
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.Items, items);

            if (iconName is not null)
            {
                parameters.Add(p => p.SearchBoxClearIconName, iconName);
            }
        });

        component.Find(".bit-drp-wrp").Click();
        component.Find(".bit-drp-sin").Input("app");

        var clearIcon = component.Find(".bit-drp-sbc > span > i");

        if (iconName is not null)
        {
            Assert.IsTrue(clearIcon.ClassList.Contains($"bit-icon--{iconName}"));
        }
        else
        {
            Assert.IsTrue(clearIcon.ClassList.Contains("bit-icon--Cancel"));
        }
    }

    [TestMethod,
        DataRow(BitDir.Rtl),
        DataRow(BitDir.Ltr)
    ]
    public void BitDropdownDirTest(BitDir dir)
    {
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Dir, dir);
        });

        var bitDrp = component.Find(".bit-drp");

        if (dir is BitDir.Rtl)
        {
            Assert.IsTrue(bitDrp.ClassList.Contains("bit-rtl"));
        }
        else
        {
            Assert.IsFalse(bitDrp.ClassList.Contains("bit-rtl"));
        }
    }

    [TestMethod]
    [DataRow(BitColor.Primary, "bit-drp-pri")]
    [DataRow(BitColor.Secondary, "bit-drp-sec")]
    [DataRow(BitColor.Tertiary, "bit-drp-ter")]
    [DataRow(BitColor.Info, "bit-drp-inf")]
    [DataRow(BitColor.Success, "bit-drp-suc")]
    [DataRow(BitColor.Warning, "bit-drp-wrn")]
    [DataRow(BitColor.SevereWarning, "bit-drp-swr")]
    [DataRow(BitColor.Error, "bit-drp-err")]
    [DataRow(BitColor.PrimaryBackground, "bit-drp-pbg")]
    [DataRow(BitColor.SecondaryBackground, "bit-drp-sbg")]
    [DataRow(BitColor.TertiaryBackground, "bit-drp-tbg")]
    [DataRow(BitColor.PrimaryForeground, "bit-drp-pfg")]
    [DataRow(BitColor.SecondaryForeground, "bit-drp-sfg")]
    [DataRow(BitColor.TertiaryForeground, "bit-drp-tfg")]
    [DataRow(BitColor.PrimaryBorder, "bit-drp-pbr")]
    [DataRow(BitColor.SecondaryBorder, "bit-drp-sbr")]
    [DataRow(BitColor.TertiaryBorder, "bit-drp-tbr")]
    [DataRow(null, "bit-drp-pri")]
    public void BitDropdownColorTest(BitColor? color, string expectedClass)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            if (color.HasValue)
            {
                parameters.Add(p => p.Color, color.Value);
            }
        });

        var bitDrp = component.Find(".bit-drp");

        Assert.IsTrue(bitDrp.ClassList.Contains(expectedClass));
    }

    [TestMethod]
    [DataRow(BitColor.Primary, "bit-drp-pri")]
    [DataRow(BitColor.Secondary, "bit-drp-sec")]
    [DataRow(BitColor.Tertiary, "bit-drp-ter")]
    [DataRow(BitColor.Info, "bit-drp-inf")]
    [DataRow(BitColor.Success, "bit-drp-suc")]
    [DataRow(BitColor.Warning, "bit-drp-wrn")]
    [DataRow(BitColor.SevereWarning, "bit-drp-swr")]
    [DataRow(BitColor.Error, "bit-drp-err")]
    [DataRow(BitColor.PrimaryBackground, "bit-drp-pbg")]
    [DataRow(BitColor.SecondaryBackground, "bit-drp-sbg")]
    [DataRow(BitColor.TertiaryBackground, "bit-drp-tbg")]
    [DataRow(BitColor.PrimaryForeground, "bit-drp-pfg")]
    [DataRow(BitColor.SecondaryForeground, "bit-drp-sfg")]
    [DataRow(BitColor.TertiaryForeground, "bit-drp-tfg")]
    [DataRow(BitColor.PrimaryBorder, "bit-drp-pbr")]
    [DataRow(BitColor.SecondaryBorder, "bit-drp-sbr")]
    [DataRow(BitColor.TertiaryBorder, "bit-drp-tbr")]
    [DataRow(null, "bit-drp-pri")]
    public void BitDropdownColorCalloutTest(BitColor? color, string expectedClass)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            if (color.HasValue)
            {
                parameters.Add(p => p.Color, color.Value);
            }
        });

        var callout = component.Find(".bit-drp-cal");

        Assert.IsTrue(callout.ClassList.Contains(expectedClass));
    }

    [TestMethod]
    public void BitDropdownComboTypingWhileClosedShouldOpenAndKeepFilter()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var items = BitDropdownTests.GetShortDropdownItems();
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Combo, true);
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.Items, items);
        });

        // Typing into the combo input while the callout is closed opens it, and the very term that
        // opened it must stay as the filter (the open used to clear the search text and lose it).
        var comboInput = component.Find(".bit-drp-inp");
        comboInput.Input("app");

        var drpItems = component.FindAll(".bit-drp-itm");
        Assert.AreEqual(1, drpItems.Count);
        Assert.AreEqual("Apple", drpItems[0].TextContent.Trim());
    }

    [TestMethod]
    public void BitDropdownCtrlAShouldToggleSelectAll()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var items = BitDropdownTests.GetShortDropdownItems();
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.MultiSelect, true);
            parameters.Add(p => p.Items, items);
        });

        component.Find(".bit-drp-wrp").Click();

        var callout = component.Find(".bit-drp-cal");
        callout.KeyDown(new KeyboardEventArgs { Key = "a", CtrlKey = true });

        Assert.AreEqual(items.Count, component.FindAll(".bit-drp-iwr.bit-drp-chd").Count);

        // A second Ctrl+A clears the selection, since every item is selected already.
        callout.KeyDown(new KeyboardEventArgs { Key = "a", CtrlKey = true });

        Assert.AreEqual(0, component.FindAll(".bit-drp-iwr.bit-drp-chd").Count);
    }

    [TestMethod]
    public async Task BitDropdownUnselectItemShouldIgnoreUnselectedItems()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var items = BitDropdownTests.GetShortDropdownItems();
        var values = new[] { items[0].Value };
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.MultiSelect, true);
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Values, values);
            parameters.Add(p => p.ValuesChanged, v => values = v?.ToArray());
        });

        // Unselecting an item that is not selected must be a no-op instead of selecting it.
        await component.InvokeAsync(() => component.Instance.UnselectItem(items[1]));
        Assert.AreEqual(1, values!.Length);
        Assert.AreEqual(items[0].Value, values[0]);

        await component.InvokeAsync(() => component.Instance.UnselectItem(items[0]));
        Assert.AreEqual(0, values!.Length);
    }

    [TestMethod]
    public async Task BitDropdownUnselectItemShouldOnlyClearTheSelectedItem()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var items = BitDropdownTests.GetShortDropdownItems();
        var value = items[0].Value;
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Value, value);
            parameters.Add(p => p.ValueChanged, v => value = v);
        });

        // Unselecting an item other than the selected one must not clear the selection.
        await component.InvokeAsync(() => component.Instance.UnselectItem(items[1]));
        Assert.AreEqual(items[0].Value, value);

        await component.InvokeAsync(() => component.Instance.UnselectItem(items[0]));
        Assert.IsNull(value);
    }

    [TestMethod,
      DataRow(null),
      DataRow("Favorite fruit")
    ]
    public void BitDropdownAriaLabelShouldOverrideAriaLabelledby(string ariaLabel)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Label, "Fruits");
            parameters.Add(p => p.Items, BitDropdownTests.GetShortDropdownItems());

            if (ariaLabel is not null)
            {
                parameters.Add(p => p.AriaLabel, ariaLabel);
            }
        });

        var wrapper = component.Find(".bit-drp-wrp");

        if (ariaLabel is null)
        {
            Assert.IsFalse(wrapper.HasAttribute("aria-label"));
            Assert.IsTrue(wrapper.HasAttribute("aria-labelledby"));
        }
        else
        {
            Assert.AreEqual(ariaLabel, wrapper.GetAttribute("aria-label"));
            Assert.IsFalse(wrapper.HasAttribute("aria-labelledby"));
        }
    }

    [TestMethod]
    public void BitDropdownProgrammaticIsOpenShouldToggleCallout()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var opened = false;
        var closed = false;
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, BitDropdownTests.GetShortDropdownItems());
            parameters.Add(p => p.OnOpen, () => opened = true);
            parameters.Add(p => p.OnClose, () => closed = true);
        });

        int CountToggles() => Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.Callouts.toggle");

        Assert.AreEqual(0, CountToggles());

        // Opening the callout from the outside through the IsOpen parameter has no internal flow that
        // toggles the callout, so the parameter hook itself has to reach the JS side.
        component.Render(parameters => parameters.Add(p => p.IsOpen, true));

        Assert.AreEqual(1, CountToggles());
        Assert.IsTrue(opened);

        component.Render(parameters => parameters.Add(p => p.IsOpen, false));

        Assert.AreEqual(2, CountToggles());
        Assert.IsTrue(closed);
    }

    [TestMethod]
    public void BitDropdownInitialIsOpenShouldToggleCalloutAfterRender()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        // An initial IsOpen fires the parameter hook before the first render, when no callout element
        // exists to toggle (and during prerendering no JS runtime is available at all), so the open
        // state has to be applied after the first render instead.
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Items, BitDropdownTests.GetShortDropdownItems());
        });

        Assert.AreEqual(1, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.Callouts.toggle"));
    }

    [TestMethod]
    public void BitDropdownOneWayIsOpenShouldNotBlockSelection()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        BitDropdownItem<string>? selectedItem = null;
        var items = BitDropdownTests.GetShortDropdownItems();
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            // A one-way bound IsOpen (no IsOpenChanged) keeps the callout state controlled by the
            // parent, but must not block selecting items.
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.OnSelectItem, (BitDropdownItem<string> i) => selectedItem = i);
        });

        component.Find(".bit-drp-itm").Click();

        Assert.IsNotNull(selectedItem);
        Assert.AreEqual(items[0].Value, selectedItem.Value);
    }

    [TestMethod]
    public void BitDropdownDynamicItemsShouldSurviveSelectionChanges()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        Context.JSInterop.Setup<string>("BitBlazorUI.Utils.getProperty", _ => true).SetResult("New Item");

        var items = BitDropdownTests.GetShortDropdownItems();
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Combo, true);
            parameters.Add(p => p.Dynamic, true);
            parameters.Add(p => p.MultiSelect, true);
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.DynamicValueGenerator, (BitDropdownItem<string>? item) => item?.Text ?? string.Empty);
        });

        var comboInput = component.Find(".bit-drp-inp");
        comboInput.Input("New Item");
        comboInput.KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.AreEqual(1, component.Instance.SelectedItems.Count);
        Assert.AreEqual("New Item", component.Instance.SelectedItems[0].Text);

        // Selecting a normal item afterwards rebuilds the selection from Items; the dynamic item is
        // not part of Items but its value is still selected, so it must survive the rebuild.
        component.Find(".bit-drp-wrp").Click();
        component.Find(".bit-drp-iwr button").Click();

        Assert.AreEqual(2, component.Instance.SelectedItems.Count);
        Assert.IsTrue(component.Instance.SelectedItems.Any(i => i.Text == "New Item"));
        Assert.IsTrue(component.Instance.SelectedItems.Any(i => i.Value == items[0].Value));
    }

    [TestMethod,
      DataRow(true),
      DataRow(false)
    ]
    public void BitDropdownAriaRequiredAndDisabledShouldRenderTokenValues(bool state)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Required, state);
            parameters.Add(p => p.IsEnabled, state is false);
            parameters.Add(p => p.Items, BitDropdownTests.GetShortDropdownItems());
        });

        var wrapper = component.Find(".bit-drp-wrp");

        if (state)
        {
            // The ARIA state attributes take explicit true/false tokens; a bool-valued Blazor
            // attribute would render an empty string, which assistive technologies do not honor.
            Assert.AreEqual("true", wrapper.GetAttribute("aria-required"));
            Assert.AreEqual("true", wrapper.GetAttribute("aria-disabled"));
        }
        else
        {
            Assert.IsFalse(wrapper.HasAttribute("aria-required"));
            Assert.IsFalse(wrapper.HasAttribute("aria-disabled"));
        }
    }

    [TestMethod]
    public void BitDropdownNameShouldRenderOnTheHiddenSelect()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var items = BitDropdownTests.GetShortDropdownItems();
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Name, "fruit");
            parameters.Add(p => p.DefaultValue, items[0].Value);
        });

        // Without a name the mirrored select cannot take part in a plain HTML form post at all.
        var select = component.Find("select.bit-input-hidden");
        Assert.AreEqual("fruit", select.GetAttribute("name"));

        // It duplicates the selection, so it stays out of the accessibility tree and the tab order.
        Assert.AreEqual("true", select.GetAttribute("aria-hidden"));
        Assert.AreEqual("-1", select.GetAttribute("tabindex"));

        var option = component.Find("select.bit-input-hidden option");
        Assert.AreEqual("f-app", option.GetAttribute("value"));
    }

    [TestMethod]
    public void BitDropdownDisabledShouldDisableTheHiddenSelect()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.Items, BitDropdownTests.GetShortDropdownItems());
        });

        // A disabled control must not contribute its value to a form post.
        Assert.IsTrue(component.Find("select.bit-input-hidden").HasAttribute("disabled"));
    }

    [TestMethod,
      DataRow(null, "0"),
      DataRow("3", "3")
    ]
    public void BitDropdownTabIndexShouldWorkCorrect(string? tabIndex, string expectedTabIndex)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.TabIndex, tabIndex);
            parameters.Add(p => p.Items, BitDropdownTests.GetShortDropdownItems());
        });

        Assert.AreEqual(expectedTabIndex, component.Find(".bit-drp-wrp").GetAttribute("tabindex"));

        // A disabled dropdown is out of the tab order whatever the requested index is.
        component.Render(parameters => parameters.Add(p => p.IsEnabled, false));

        Assert.AreEqual("-1", component.Find(".bit-drp-wrp").GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitDropdownOptionsShouldStayOutOfTheTabOrder()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.MultiSelect, true);
            parameters.Add(p => p.ShowSelectAll, true);
            parameters.Add(p => p.Items, BitDropdownTests.GetShortDropdownItems());
        });

        // The options are reached with the arrow keys; Tab closes the callout instead of walking them.
        foreach (var option in component.FindAll("[role=option]"))
        {
            Assert.AreEqual("-1", option.GetAttribute("tabindex"));
        }

        Assert.AreEqual("-1", component.Find(".bit-drp-sab").GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitDropdownAriaLabelsShouldBeCustomizable()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var items = BitDropdownTests.GetShortDropdownItems();
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Chips, true);
            parameters.Add(p => p.MultiSelect, true);
            parameters.Add(p => p.ShowSearchBox, true);
            parameters.Add(p => p.ShowClearButton, true);
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.DefaultValues, new[] { items[0].Value });
            parameters.Add(p => p.ChipsRemoveButtonAriaLabel, "Weg mit {0}");
            parameters.Add(p => p.ClearButtonAriaLabel, "Alles löschen");
            parameters.Add(p => p.SearchBoxAriaLabel, "Suchtext");
        });

        Assert.AreEqual("Weg mit Apple", component.Find(".bit-drp-crb").GetAttribute("aria-label"));
        Assert.AreEqual("Alles löschen", component.Find(".bit-drp-clr").GetAttribute("aria-label"));
        Assert.AreEqual("Suchtext", component.Find(".bit-drp-sin").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitDropdownOverflowChipShouldNameTheItemsItHides()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var items = BitDropdownTests.GetShortDropdownItems();
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Chips, true);
            parameters.Add(p => p.MultiSelect, true);
            parameters.Add(p => p.MaxDisplayedItems, 1);
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.DefaultValues, items.Select(i => i.Value).ToArray());
        });

        // The count alone says nothing about what got collapsed, so the hidden items are named.
        var overflow = component.Find(".bit-drp-ovf");
        Assert.AreEqual("+3", overflow.TextContent.Trim());
        Assert.AreEqual("Orange, Banana, Broccoli", overflow.GetAttribute("title"));
        Assert.AreEqual("Orange, Banana, Broccoli", overflow.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitDropdownAltArrowUpShouldNotOpenTheCallout()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var isOpen = false;
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, BitDropdownTests.GetShortDropdownItems());
            parameters.Add(p => p.IsOpenChanged, v => isOpen = v);
        });

        var wrapper = component.Find(".bit-drp-wrp");

        // Alt+ArrowUp dismisses the popup per the APG combobox pattern, so it must not open it.
        wrapper.KeyDown(new KeyboardEventArgs { Key = "ArrowUp", AltKey = true });
        Assert.IsFalse(isOpen);

        wrapper.KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });
        Assert.IsTrue(isOpen);
    }

    [TestMethod]
    public void BitDropdownItemsShouldBeDescribedByTheirHeader()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, BitDropdownTests.GetDropdownItems());
        });

        // A flat listbox cannot nest the items inside their header, so each item points at the header
        // that names it and a screen reader reads the group along with the item.
        var headers = component.FindAll(".bit-drp-ihd");
        var options = component.FindAll("[role=option]");

        Assert.AreEqual(2, headers.Count);
        Assert.IsTrue(headers[0].GetAttribute("id").HasValue());
        Assert.AreNotEqual(headers[0].GetAttribute("id"), headers[1].GetAttribute("id"));

        // Apple, Orange and Banana belong to the first header, Broccoli to the second one.
        Assert.AreEqual(headers[0].GetAttribute("id"), options[0].GetAttribute("aria-describedby"));
        Assert.AreEqual(headers[0].GetAttribute("id"), options[2].GetAttribute("aria-describedby"));
        Assert.AreEqual(headers[1].GetAttribute("id"), options[3].GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitDropdownItemHeaderIdShouldPreferTheOneOfTheItem()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var items = new List<BitDropdownItem<string>>
        {
            new() { Text = "Fruits", Id = "my-fruits", ItemType = BitDropdownItemType.Header },
            new() { Text = "Apple", Value = "f-app" }
        };
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        Assert.AreEqual("my-fruits", component.Find(".bit-drp-ihd").GetAttribute("id"));
        Assert.AreEqual("my-fruits", component.Find("[role=option]").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitDropdownComboInputShouldRaiseOnSearch()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var searches = new List<string?>();
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Combo, true);
            parameters.Add(p => p.Items, BitDropdownTests.GetShortDropdownItems());
            parameters.Add(p => p.OnSearch, v => searches.Add(v));
        });

        // OnSearch covers the ComboBox input as well as the search box, so a consumer loading its own
        // data does not have to know which of the two the user is typing in.
        component.Find(".bit-drp-inp").Change("app");

        CollectionAssert.AreEqual(new[] { "app" }, searches);
    }

    [TestMethod]
    public void BitDropdownComboEscapeShouldClearTheSearch()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var searches = new List<string?>();
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Combo, true);
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.Items, BitDropdownTests.GetShortDropdownItems());
            parameters.Add(p => p.OnSearch, v => searches.Add(v));
        });

        var comboInput = component.Find(".bit-drp-inp");
        comboInput.Input("app");
        Assert.AreEqual(1, component.FindAll(".bit-drp-itm").Count);

        // Escape abandons the term, which has to reach the filtering (and its subscribers) rather than
        // only blanking the input: an ItemsProvider would otherwise keep serving the abandoned term.
        comboInput.KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.AreEqual(4, component.FindAll(".bit-drp-itm").Count);
        Assert.IsTrue(component.Find(".bit-drp-inp").GetAttribute("value").HasNoValue());
        CollectionAssert.AreEqual(new string?[] { "app", null }, searches);
    }

    [TestMethod,
      DataRow(true),
      DataRow(false)
    ]
    public void BitDropdownStickyHeadersShouldWorkCorrect(bool stickyHeaders)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.StickyHeaders, stickyHeaders);
            parameters.Add(p => p.Items, BitDropdownTests.GetDropdownItems());
        });

        // The wrapper of a header, and not the header element itself, is the direct child of the
        // scroll container, so it is the one that carries the class the sticky rule applies to.
        Assert.AreEqual(2, component.FindAll(".bit-drp-ihw").Count);
        Assert.AreEqual(stickyHeaders, component.Find(".bit-drp-cal").ClassList.Contains("bit-drp-sth"));
    }

    [TestMethod]
    public void BitDropdownItemClassShouldSurviveTheHeaderWrapperClass()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var items = new List<BitDropdownItem<string>>
        {
            new() { Text = "Fruits", Class = "my-header", ItemType = BitDropdownItemType.Header },
            new() { Text = "Apple", Value = "f-app", Class = "my-item" }
        };
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        var wrappers = component.FindAll(".bit-drp-scn > div");

        Assert.IsTrue(wrappers[0].ClassList.Contains("bit-drp-ihw"));
        Assert.IsTrue(wrappers[0].ClassList.Contains("my-header"));
        Assert.IsFalse(wrappers[1].ClassList.Contains("bit-drp-ihw"));
        Assert.IsTrue(wrappers[1].ClassList.Contains("my-item"));
    }

    [TestMethod]
    public void BitDropdownSelectedItemsShouldFollowTheOrderOfTheValues()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var items = GetShortDropdownItems();
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.MultiSelect, true);
            // Deliberately not the order of the item list, so the assertions below cannot pass by accident.
            parameters.Add(p => p.Values, new[] { "v-bro", "f-app" });
        });

        CollectionAssert.AreEqual(new[] { "Broccoli", "Apple" },
                                  component.Instance.SelectedItems.Select(i => i.Text).ToArray());

        Assert.AreEqual("Broccoli, Apple", component.Find(".bit-drp-tcn").InnerHtml);
    }

    [TestMethod]
    public void BitDropdownSelectedItemsShouldFollowTheOrderTheyWerePickedIn()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var items = GetShortDropdownItems();
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.MultiSelect, true);
            parameters.Add(p => p.DefaultValues, Array.Empty<string>());
        });

        var options = component.FindAll(".bit-drp-scn button");

        // The last item first, then the first one: the selection has to report that order, not the list's.
        options[3].Click();
        options[0].Click();

        CollectionAssert.AreEqual(new[] { "Broccoli", "Apple" },
                                  component.Instance.SelectedItems.Select(i => i.Text).ToArray());
    }

    [TestMethod]
    public void BitDropdownHideSelectedItemsShouldDropTheHeadersItEmpties()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var items = GetDropdownItems();
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.MultiSelect, true);
            parameters.Add(p => p.HideSelectedItems, true);
            parameters.Add(p => p.DefaultValues, Array.Empty<string>());
        });

        Assert.AreEqual(2, component.FindAll(".bit-drp-ihd").Count);

        // Broccoli is the only item of the Vegetables group, so hiding it leaves that header naming nothing.
        component.Render(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.MultiSelect, true);
            parameters.Add(p => p.HideSelectedItems, true);
            parameters.Add(p => p.Values, new[] { "v-bro" });
        });

        var headers = component.FindAll(".bit-drp-ihd");

        Assert.AreEqual(1, headers.Count);
        Assert.AreEqual("Fruits", headers[0].TextContent);

        // The divider between the two groups now has the Vegetables side missing, so it separates
        // nothing and goes with the header it framed.
        Assert.AreEqual(0, component.FindAll(".bit-drp-sep").Count);
    }

    [TestMethod]
    public void BitDropdownHideSelectedItemsShouldDropTheDividerItLeavesLeading()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var items = new List<BitDropdownItem<string>>
        {
            new() { Text = "Apple", Value = "f-app" },
            new() { ItemType = BitDropdownItemType.Divider },
            new() { Text = "Broccoli", Value = "v-bro" }
        };
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.MultiSelect, true);
            parameters.Add(p => p.HideSelectedItems, true);
            parameters.Add(p => p.DefaultValues, Array.Empty<string>());
        });

        Assert.AreEqual(1, component.FindAll(".bit-drp-sep").Count);

        // Hiding the only item above the divider would leave the list opening with a rule.
        component.Render(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.MultiSelect, true);
            parameters.Add(p => p.HideSelectedItems, true);
            parameters.Add(p => p.Values, new[] { "f-app" });
        });

        Assert.AreEqual(0, component.FindAll(".bit-drp-sep").Count);
        Assert.AreEqual(1, component.FindAll("[role=option]").Count);
    }

    [TestMethod]
    public void BitDropdownOnDeselectItemShouldOnlyReportTheUnselectedOnes()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var items = GetShortDropdownItems();
        List<string?> selected = [];
        List<string?> deselected = [];

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.MultiSelect, true);
            parameters.Add(p => p.DefaultValues, Array.Empty<string>());
            parameters.Add(p => p.OnSelectItem, (BitDropdownItem<string> item) => selected.Add(item.Value));
            parameters.Add(p => p.OnDeselectItem, (BitDropdownItem<string> item) => deselected.Add(item.Value));
        });

        var option = component.FindAll(".bit-drp-scn button")[0];

        option.Click();
        option.Click();

        CollectionAssert.AreEqual(new[] { "f-app", "f-app" }, selected.ToArray());
        CollectionAssert.AreEqual(new[] { "f-app" }, deselected.ToArray());
    }

    [TestMethod]
    public void BitDropdownOnClickShouldFireEvenWhenTheOpenIsRefused()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var clicks = 0;
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetShortDropdownItems());
            // A one-way bound IsOpen refuses the opening, which must not swallow the click callback.
            parameters.Add(p => p.IsOpen, false);
            parameters.Add(p => p.OnClick, () => clicks++);
        });

        component.Find(".bit-drp-wrp").Click();

        Assert.AreEqual(1, clicks);
        Assert.IsFalse(component.Instance.IsOpen);
    }

    [TestMethod]
    public void BitDropdownAltArrowDownShouldOpenWithoutMovingTheFocus()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetShortDropdownItems());
        });

        var wrapper = component.Find(".bit-drp-wrp");

        // Alt+ArrowDown reveals the popup without moving the focus into it, per the APG combobox pattern.
        wrapper.KeyDown(new KeyboardEventArgs { Key = "ArrowDown", AltKey = true });

        Assert.IsTrue(component.Instance.IsOpen);
        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Dropdowns.focusItem"].Count);

        // The plain ArrowDown keeps opening the popup with the focus on the selected (or first) option.
        wrapper.KeyDown(new KeyboardEventArgs { Key = "ArrowUp", AltKey = true });
        wrapper.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });

        Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Dropdowns.focusItem"].Count);
    }

    [TestMethod]
    public void BitDropdownComboAriaAutocompleteShouldBeOnTheComboboxElement()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetShortDropdownItems());
        });

        Assert.IsFalse(component.Find(".bit-drp-wrp").HasAttribute("aria-autocomplete"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Items, GetShortDropdownItems());
            parameters.Add(p => p.Combo, true);
        });

        Assert.AreEqual("list", component.Find(".bit-drp-wrp").GetAttribute("aria-autocomplete"));
    }

    [TestMethod]
    public void BitDropdownClearButtonShouldAlsoClearTheComboInput()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Combo, true);
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.ShowClearButton, true);
            parameters.Add(p => p.Items, GetShortDropdownItems());
            parameters.Add(p => p.DefaultValue, "f-app");
        });

        component.Find(".bit-drp-inp").Input("ban");
        Assert.AreEqual("ban", component.Find(".bit-drp-inp").GetAttribute("value"));

        // The button says it clears the selection, so it must not leave a filter behind in the input.
        component.Find(".bit-drp-clr").Click();

        Assert.IsTrue(component.Find(".bit-drp-inp").GetAttribute("value").HasNoValue());
        Assert.AreEqual(4, component.FindAll(".bit-drp-itm").Count);
    }

    [TestMethod]
    public void BitDropdownSearchIgnoreDiacriticsShouldMatchTheFoldedText()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var items = GetAccentedDropdownItems();
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.ShowSearchBox, true);
            parameters.Add(p => p.HighlightSearch, true);
        });

        component.Find(".bit-drp-wrp").Click();

        // Without the folding an unaccented term finds nothing, which is exactly what makes a search
        // box useless to anyone typing on a keyboard that has no accented keys.
        component.Find(".bit-drp-sin").Change("Jose");
        Assert.AreEqual(0, component.FindAll(".bit-drp-itm").Count);

        component.Render(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.ShowSearchBox, true);
            parameters.Add(p => p.HighlightSearch, true);
            parameters.Add(p => p.SearchIgnoreDiacritics, true);
        });

        var options = component.FindAll(".bit-drp-itm");
        Assert.AreEqual(1, options.Count);
        Assert.AreEqual("José", options[0].TextContent.Trim());

        // Folding keeps one character per character, so the highlighted range still lines up with the
        // original (accented) text rather than sliding along it.
        Assert.AreEqual("José", component.Find(".bit-drp-hlt").TextContent);

        component.Find(".bit-drp-sin").Change("Muller");

        options = component.FindAll(".bit-drp-itm");
        Assert.AreEqual(1, options.Count);
        Assert.AreEqual("Müller", options[0].TextContent.Trim());
    }

    [TestMethod]
    public void BitDropdownCloseOnSelectShouldOverrideTheDefaultOfEachMode()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        // A single select pick closes the callout by default, because the pick is the whole
        // interaction; CloseOnSelect=false keeps it open so the user can try another option.
        var single = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetShortDropdownItems());
            parameters.Add(p => p.CloseOnSelect, false);
        });

        single.Find(".bit-drp-wrp").Click();
        Assert.IsTrue(single.Instance.IsOpen);

        single.FindAll("[role=option]")[1].Click();

        Assert.IsTrue(single.Instance.IsOpen);
        Assert.AreEqual("f-ora", single.Instance.Value);

        // A multi select callout stays open by default so the next item can be picked right away;
        // CloseOnSelect=true turns each pick into a complete interaction of its own.
        var multi = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetShortDropdownItems());
            parameters.Add(p => p.MultiSelect, true);
            parameters.Add(p => p.CloseOnSelect, true);
        });

        multi.Find(".bit-drp-wrp").Click();
        Assert.IsTrue(multi.Instance.IsOpen);

        multi.FindAll("[role=option]")[0].Click();

        Assert.IsFalse(multi.Instance.IsOpen);
        Assert.AreEqual(1, multi.Instance.SelectedItems.Count);
    }

    [TestMethod]
    public void BitDropdownCloseOnSelectShouldKeepTheDefaultsWhenNotSet()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var single = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetShortDropdownItems());
        });

        single.Find(".bit-drp-wrp").Click();
        single.FindAll("[role=option]")[0].Click();

        Assert.IsFalse(single.Instance.IsOpen);

        var multi = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetShortDropdownItems());
            parameters.Add(p => p.MultiSelect, true);
        });

        multi.Find(".bit-drp-wrp").Click();
        multi.FindAll("[role=option]")[0].Click();

        Assert.IsTrue(multi.Instance.IsOpen);
    }

    [TestMethod]
    public void BitDropdownAutoSelectFirstMatchShouldCommitAPartiallyTypedTerm()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        Context.JSInterop.Setup<string>("BitBlazorUI.Utils.getProperty", _ => true).SetResult("ban");

        var items = GetShortDropdownItems();
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Combo, true);
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.Items, items);
        });

        var comboInput = component.Find(".bit-drp-inp");
        comboInput.Input("ban");
        comboInput.KeyDown(new KeyboardEventArgs { Key = "Enter" });

        // Without the parameter a term that matches no item exactly commits to nothing at all.
        Assert.IsNull(component.Instance.Value);

        component.Render(parameters =>
        {
            parameters.Add(p => p.Combo, true);
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.AutoSelectFirstMatch, true);
        });

        comboInput = component.Find(".bit-drp-inp");
        comboInput.Input("ban");
        comboInput.KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.AreEqual("f-ban", component.Instance.Value);
    }

    [TestMethod]
    public void BitDropdownAutoSelectFirstMatchShouldTakePrecedenceOverDynamic()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        Context.JSInterop.Setup<string>("BitBlazorUI.Utils.getProperty", _ => true).SetResult("ora");

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Combo, true);
            parameters.Add(p => p.Dynamic, true);
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.AutoSelectFirstMatch, true);
            parameters.Add(p => p.Items, GetShortDropdownItems());
        });

        var comboInput = component.Find(".bit-drp-inp");
        comboInput.Input("ora");
        comboInput.KeyDown(new KeyboardEventArgs { Key = "Enter" });

        // A term that names an item the list already has selects that item instead of creating a
        // second one that only looks like it.
        Assert.AreEqual("f-ora", component.Instance.Value);
        Assert.AreEqual("Orange", component.Instance.SelectedItem?.Text);
    }

    [TestMethod]
    public void BitDropdownComboDebounceShouldAlsoDelayTheFiltering()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Combo, true);
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.DebounceTime, 3000);
            parameters.Add(p => p.Items, GetShortDropdownItems());
        });

        component.Find(".bit-drp-wrp").Click();
        component.Find(".bit-drp-inp").Input("app");

        // The input shows the typed text at once - it is what the user is looking at...
        Assert.AreEqual("app", component.Find(".bit-drp-inp").GetAttribute("value"));

        // ...while the list is only filtered once the debounce elapses, which is the whole point of
        // configuring one. Applying the term as it was typed used to leave the rate limit governing
        // nothing but the OnSearch callback and the ItemsProvider.
        Assert.AreEqual(4, component.FindAll(".bit-drp-itm").Count);
    }

    [TestMethod]
    public void BitDropdownTabOnTheTriggerShouldCloseTheCallout()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetShortDropdownItems());
        });

        var wrapper = component.Find(".bit-drp-wrp");

        // Alt+ArrowDown reveals the popup without moving the focus into it, so the Tab that leaves the
        // dropdown is seen by the trigger and not by the callout - and must still dismiss the popup,
        // which is rendered at the end of the document and would otherwise be left behind, open.
        wrapper.KeyDown(new KeyboardEventArgs { Key = "ArrowDown", AltKey = true });
        Assert.IsTrue(component.Instance.IsOpen);

        wrapper.KeyDown(new KeyboardEventArgs { Key = "Tab" });

        Assert.IsFalse(component.Instance.IsOpen);
    }

    [TestMethod]
    public async Task BitDropdownSelectItemShouldMirrorUnselectItem()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var items = GetShortDropdownItems();
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.MultiSelect, true);
            parameters.Add(p => p.Items, items);
        });

        await component.InvokeAsync(() => component.Instance.SelectItem(items[1]));
        Assert.AreEqual(1, component.Instance.SelectedItems.Count);

        // An item that is already selected is left alone: picking it again would unselect it, which
        // is what UnselectItem is for.
        await component.InvokeAsync(() => component.Instance.SelectItem(items[1]));
        Assert.AreEqual(1, component.Instance.SelectedItems.Count);

        await component.InvokeAsync(() => component.Instance.SelectItem(items[2]));
        Assert.AreEqual(2, component.Instance.SelectedItems.Count);

        await component.InvokeAsync(() => component.Instance.UnselectItem(items[1]));
        Assert.AreEqual(1, component.Instance.SelectedItems.Count);
        Assert.AreEqual("f-ban", component.Instance.SelectedItems[0].Value);
    }

    [TestMethod]
    public void BitDropdownTemplatedHeaderShouldStillCarryTheGroupIdAndHiddenState()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var items = new List<BitDropdownItem<string>>
        {
            new() { Text = "Fruits", ItemType = BitDropdownItemType.Header },
            new() { Text = "Apple", Value = "f-app" },
            new() { Text = "Vegetables", IsHidden = true, ItemType = BitDropdownItemType.Header },
            new() { Text = "Broccoli", Value = "v-bro" }
        };
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.HeaderTemplate, (BitDropdownItem<string> item) =>
                builder => builder.AddContent(0, item.Text));
        });

        var headers = component.FindAll(".bit-drp-ihw");
        var options = component.FindAll("[role=option]");

        // A custom template renders whatever it likes, so the generated id its items point at (and the
        // hidden state) have to land on the wrapper, or the reference dangles on a missing element.
        Assert.AreEqual(2, headers.Count);
        Assert.IsTrue(headers[0].GetAttribute("id").HasValue());
        Assert.AreEqual(headers[0].GetAttribute("id"), options[0].GetAttribute("aria-describedby"));

        // A hidden header names nothing, so it is not rendered and its items are left without a group.
        Assert.IsTrue(headers[1].GetAttribute("style")?.Contains("display:none") is true);
        Assert.IsFalse(options[1].HasAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitDropdownOnDynamicAddShouldNotFireForARefusedSelection()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        Context.JSInterop.Setup<string>("BitBlazorUI.Utils.getProperty", _ => true).SetResult("New Item");

        var added = new List<BitDropdownItem<string>>();
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Combo, true);
            parameters.Add(p => p.Dynamic, true);
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MultiSelect, true);
            parameters.Add(p => p.MaxSelectedItems, 1);
            parameters.Add(p => p.Items, GetShortDropdownItems());
            parameters.Add(p => p.OnDynamicAdd, (BitDropdownItem<string> i) => added.Add(i));
            parameters.Add(p => p.DynamicValueGenerator, (BitDropdownItem<string>? item) => item?.Text ?? string.Empty);
        });

        var comboInput = component.Find(".bit-drp-inp");
        comboInput.Input("New Item");
        comboInput.KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.AreEqual(1, added.Count);
        Assert.AreEqual(1, component.Instance.SelectedItems.Count);

        // The limit is reached, so the second item is refused - and a refused selection is not an item
        // that was added, however much it was typed.
        Context.JSInterop.Setup<string>("BitBlazorUI.Utils.getProperty", _ => true).SetResult("Another Item");

        comboInput = component.Find(".bit-drp-inp");
        comboInput.Input("Another Item");
        comboInput.KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.AreEqual(1, added.Count);
        Assert.AreEqual(1, component.Instance.SelectedItems.Count);
    }

    [TestMethod]
    public void BitDropdownSelectAllShouldClearWhenTheSelectionLimitIsReached()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.MultiSelect, true);
            parameters.Add(p => p.ShowSelectAll, true);
            parameters.Add(p => p.MaxSelectedItems, 2);
            parameters.Add(p => p.Items, GetShortDropdownItems());
        });

        component.Find(".bit-drp-wrp").Click();

        // The limit stops the select all halfway, which is as far as it can go.
        component.Find(".bit-drp-sab").Click();
        Assert.AreEqual(2, component.Instance.SelectedItems.Count);
        Assert.AreEqual("mixed", component.Find(".bit-drp-sab").GetAttribute("aria-checked"));

        // It can never reach "all selected", so without this the item would be a control that does
        // nothing at all from here on; clearing is the only move left, and the list below it says the
        // same thing, since every unselected item is disabled while the limit holds.
        component.Find(".bit-drp-sab").Click();
        Assert.AreEqual(0, component.Instance.SelectedItems.Count);
    }

    private void HandleValueChanged(string value)
    {
        _bitDropdownValue = value;
    }

    private void HandleValuesChanged(IEnumerable<string?>? values)
    {
        _bitDropdownValues = values;
    }

    private static List<BitDropdownItem<string>> GetDropdownItems() => new()
    {
        new() { Text = "Fruits", ItemType = BitDropdownItemType.Header },
        new() { Text = "Apple", Value = "f-app" },
        new() { Text = "Orange", Value = "f-ora" },
        new() { Text = "Banana", Value = "f-ban" },
        new() { ItemType = BitDropdownItemType.Divider },
        new() { Text = "Vegetables", ItemType = BitDropdownItemType.Header },
        new() { Text = "Broccoli", Value = "v-bro" }
    };

    private static List<BitDropdownItem<string>> GetShortDropdownItems() => new()
    {
        new() { Text = "Apple", Value = "f-app" },
        new() { Text = "Orange", Value = "f-ora" },
        new() { Text = "Banana", Value = "f-ban" },
        new() { Text = "Broccoli", Value = "v-bro" }
    };


    private static List<BitDropdownItem<string>> GetAccentedDropdownItems() => new()
    {
        new() { Text = "José", Value = "n-jos" },
        new() { Text = "Müller", Value = "n-mul" },
        new() { Text = "Smith", Value = "n-smi" }
    };


    private static ICollection<BitDropdownItem<string>> GetRangeDropdownItems(int count) =>
        Enumerable.Range(1, count).Select(item => new BitDropdownItem<string>
        {
            ItemType = BitDropdownItemType.Normal,
            Value = item.ToString(),
            Text = $"Item {item}"
        }).ToArray();
}
