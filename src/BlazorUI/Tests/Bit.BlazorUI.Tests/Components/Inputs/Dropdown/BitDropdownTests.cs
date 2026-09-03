using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
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

        var textSpan = component.Find(".bit-drp-tdp");
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

        var textSpan = component.Find(".bit-drp-tdp");
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

        var textSpan = component.Find(".bit-drp-tdp");
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

        var textSpan = component.Find(".bit-drp-tdp");
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

        var targetSpan = component.Find(".bit-drp-tdp");
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
      DataRow(true, true),
      DataRow(true, false),
      DataRow(false, true),
      DataRow(false, false)
    ]
    public void BitDropdownAriaRequiredAndDisabledShouldRenderTokenValues(bool required, bool isEnabled)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Required, required);
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.Items, BitDropdownTests.GetShortDropdownItems());
        });

        var wrapper = component.Find(".bit-drp-wrp");

        // The ARIA state attributes take explicit true/false tokens; a bool-valued Blazor
        // attribute would render an empty string, which assistive technologies do not honor.
        // Each attribute follows its own parameter, so neither can borrow the state of the other.
        if (required)
        {
            Assert.AreEqual("true", wrapper.GetAttribute("aria-required"));
        }
        else
        {
            Assert.IsFalse(wrapper.HasAttribute("aria-required"));
        }

        if (isEnabled)
        {
            Assert.IsFalse(wrapper.HasAttribute("aria-disabled"));
        }
        else
        {
            Assert.AreEqual("true", wrapper.GetAttribute("aria-disabled"));
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

        Assert.AreEqual("Broccoli, Apple", component.Find(".bit-drp-tdp").InnerHtml);
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
    public void BitDropdownSearchIgnoreDiacriticsShouldTolerateAstralCharacters()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        // An emoji is a surrogate pair, whose halves are not valid strings of their own; folding the
        // text one character at a time must pass them through instead of normalizing (and throwing).
        var items = new List<BitDropdownItem<string>>
        {
            new() { Text = "🍎 Pomme", Value = "f-pom" },
            new() { Text = "🍌 Banane", Value = "f-ban" },
            new() { Text = "Pêche 🍑", Value = "f-pec" }
        };

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.ShowSearchBox, true);
            parameters.Add(p => p.SearchIgnoreDiacritics, true);
        });

        component.Find(".bit-drp-wrp").Click();
        component.Find(".bit-drp-sin").Change("Peche");

        var options = component.FindAll(".bit-drp-itm");
        Assert.AreEqual(1, options.Count);
        Assert.AreEqual("Pêche 🍑", options[0].TextContent.Trim());
    }

    [TestMethod]
    public void BitDropdownKeyboardOpenShouldHonorAutoFocusSearchBox()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetShortDropdownItems());
            parameters.Add(p => p.ShowSearchBox, true);
            parameters.Add(p => p.AutoFocusSearchBox, true);
        });

        // A search-first dropdown hands the focus to the search box on open, so an Enter open must
        // not pull it onto an option like the plain open does.
        component.Find(".bit-drp-wrp").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.IsTrue(component.Instance.IsOpen);
        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Dropdowns.focusItem"].Count);
    }

    [TestMethod]
    public void BitDropdownBoundOpenShouldHonorAutoFocusSearchBox()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var isOpen = false;
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetShortDropdownItems());
            parameters.Add(p => p.ShowSearchBox, true);
            parameters.Add(p => p.AutoFocusSearchBox, true);
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        // An open pushed through the binding is the third way of opening the callout, next to the
        // pointer and the keyboard, so it hands the focus to the search box the same way they do.
        component.Render(parameters =>
        {
            parameters.Add(p => p.Items, GetShortDropdownItems());
            parameters.Add(p => p.ShowSearchBox, true);
            parameters.Add(p => p.AutoFocusSearchBox, true);
            parameters.Bind(p => p.IsOpen, true, v => isOpen = v);
        });

        Assert.IsTrue(component.Instance.IsOpen);

        var focused = Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"].ToList();

        Assert.AreEqual(1, focused.Count);
        Assert.AreEqual(component.Instance.SearchInputElement!.Value.Id, ((ElementReference)focused[0].Arguments[0]!).Id);
    }

    [TestMethod]
    public void BitDropdownInitiallyOpenShouldHonorAutoFocusSearchBox()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        // A dropdown that starts out open reaches the hook before there is a callout to show, so the
        // first render applies the open state - and with it the focus the search box would have taken.
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetShortDropdownItems());
            parameters.Add(p => p.ShowSearchBox, true);
            parameters.Add(p => p.AutoFocusSearchBox, true);
            parameters.Add(p => p.IsOpen, true);
        });

        var focused = Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"].ToList();

        Assert.AreEqual(1, focused.Count);
        focused[0].Arguments[0].ShouldBeElementReferenceTo(component.Find(".bit-drp-sin"));
    }

    [TestMethod]
    public void BitDropdownComboInputShouldBeNamedAfterTheLabel()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Combo, true);
            parameters.Add(p => p.Label, "Fruit");
            parameters.Add(p => p.Items, GetShortDropdownItems());
        });

        // The input does not inherit the accessible name of the combobox element around it, so
        // without an AriaLabel it has to be tied to the label by hand.
        var input = component.Find(".bit-drp-inp");
        var labelId = component.Find(".bit-drp-lbl").Id;
        Assert.AreEqual(labelId, input.GetAttribute("aria-labelledby"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Combo, true);
            parameters.Add(p => p.Label, "Fruit");
            parameters.Add(p => p.AriaLabel, "Pick a fruit");
            parameters.Add(p => p.Items, GetShortDropdownItems());
        });

        // An explicit AriaLabel names the input itself, and a labelledby reference would override it.
        input = component.Find(".bit-drp-inp");
        Assert.AreEqual("Pick a fruit", input.GetAttribute("aria-label"));
        Assert.IsFalse(input.HasAttribute("aria-labelledby"));
    }

    [TestMethod]
    public void BitDropdownTokenSeparatorsShouldCommitEachTerm()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var added = new List<BitDropdownItem<string>>();
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Combo, true);
            parameters.Add(p => p.Dynamic, true);
            parameters.Add(p => p.MultiSelect, true);
            parameters.Add(p => p.Items, GetShortDropdownItems());
            parameters.Add(p => p.TokenSeparators, new[] { ',' });
            parameters.Add(p => p.OnDynamicAdd, (BitDropdownItem<string> i) => added.Add(i));
            parameters.Add(p => p.DynamicValueGenerator, (BitDropdownItem<string>? item) => item?.Text ?? string.Empty);
        });

        // A pasted list splits on the separators and each term is committed like Enter would commit
        // it: the ones naming existing items select them, the rest become new (dynamic) items.
        component.Find(".bit-drp-inp").Input("Apple, Cherry, Banana");

        CollectionAssert.AreEqual(
            new[] { "Apple", "Cherry", "Banana" },
            component.Instance.SelectedItems.Select(i => i.Text).ToArray());
        CollectionAssert.AreEqual(new[] { "Cherry" }, added.Select(i => i.Text).ToArray());

        // The committed terms must leave the input, or the leftover text would filter the next pick.
        Assert.IsTrue(component.Find(".bit-drp-inp").GetAttribute("value").HasNoValue());
    }

    [TestMethod]
    public void BitDropdownOpenOnFocusShouldOpenTheCalloutButNotReopenAfterADismissal()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetShortDropdownItems());
            parameters.Add(p => p.OpenOnFocus, true);
        });

        component.Find(".bit-drp-wrp").FocusIn();
        Assert.IsTrue(component.Instance.IsOpen);

        // A dismissal moves the focus back to the trigger itself; the focus event that move produces
        // must not reopen the callout the user just closed.
        component.Find(".bit-drp-cal").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.IsFalse(component.Instance.IsOpen);

        component.Find(".bit-drp-wrp").FocusIn();
        Assert.IsFalse(component.Instance.IsOpen);

        // The suppression only stands for that one internal move; the next focus is the user again.
        component.Find(".bit-drp-wrp").FocusIn();
        Assert.IsTrue(component.Instance.IsOpen);
    }

    [TestMethod]
    public void BitDropdownOpenOnFocusShouldNotSwallowTheFocusWorkOfTheClickBesideIt()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.OpenOnFocus, true);
            parameters.Add(p => p.DefaultValue, "f-ban");
            parameters.Add(p => p.Items, GetShortDropdownItems());
        });

        var wrapper = component.Find(".bit-drp-wrp");

        // Clicking the dropdown focuses it first, so with OpenOnFocus the callout is already open by the
        // time the click itself is handled - which used to end the click there and leave the focus at the
        // top of the page instead of on the selected item.
        wrapper.FocusIn();
        wrapper.Click();

        Assert.IsTrue(component.Instance.IsOpen);
        var invocations = Context.JSInterop.Invocations["BitBlazorUI.Dropdowns.focusItem"];
        Assert.AreEqual(1, invocations.Count);
        Assert.AreEqual("selected", invocations[^1].Arguments[1]);

        // The callout is only toggled once for the two events: a second toggle would hide what the
        // focus just opened.
        Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Callouts.toggle"].Count);

        // A click that comes long after the focus (with the callout dismissed in between) opens the
        // callout itself again, so the note the focus left cannot outlive the interaction it belongs to.
        component.Find(".bit-drp-cal").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.IsFalse(component.Instance.IsOpen);

        wrapper.Click();
        Assert.IsTrue(component.Instance.IsOpen);
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
    public void BitDropdownAutoSelectFirstMatchShouldWaitForMinSearchLength()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        Context.JSInterop.Setup<string>("BitBlazorUI.Utils.getProperty", _ => true).SetResult("b");

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Combo, true);
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MinSearchLength, 3);
            parameters.Add(p => p.AutoSelectFirstMatch, true);
            parameters.Add(p => p.Items, GetShortDropdownItems());
        });

        var comboInput = component.Find(".bit-drp-inp");
        comboInput.Input("b");
        comboInput.KeyDown(new KeyboardEventArgs { Key = "Enter" });

        // The list is not filtered by a term shorter than MinSearchLength, so its first item is not a
        // match of anything the user typed and must not be committed.
        Assert.IsNull(component.Instance.Value);
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

    [TestMethod]
    public void BitDropdownSearchShouldKeepTheHeadersOfTheGroupsItLeaves()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.ShowSearchBox, true);
            parameters.Add(p => p.Items, GetDropdownItems());
        });

        component.Find(".bit-drp-wrp").Click();

        Assert.AreEqual(2, component.FindAll(".bit-drp-ihd").Count);

        component.Find(".bit-drp-sin").Input("app");

        // A group is part of what an item is, so a search that flattened the result into a bare list
        // would take that away exactly when the list is hardest to read: the group of the one match
        // stays, and every option still points at the header that names it.
        var headers = component.FindAll(".bit-drp-ihd");
        var options = component.FindAll("[role=option]");
        Assert.AreEqual(1, headers.Count);
        Assert.AreEqual("Fruits", headers[0].TextContent.Trim());
        Assert.AreEqual(1, options.Count);
        Assert.AreEqual(headers[0].GetAttribute("id"), options[0].GetAttribute("aria-describedby"));

        // The other group has nothing left under it, and the divider between the two has lost the
        // items on one of its sides, so neither is left standing for nothing.
        Assert.AreEqual(0, component.FindAll(".bit-drp-sep").Count);

        // Clearing the term brings the whole list back, groups and all.
        component.Find(".bit-drp-sin").Input(string.Empty);
        Assert.AreEqual(2, component.FindAll(".bit-drp-ihd").Count);
        Assert.AreEqual(1, component.FindAll(".bit-drp-sep").Count);
    }

    [TestMethod]
    public void BitDropdownSearchWithNoResultShouldLeaveNoGroupBehind()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.ShowSearchBox, true);
            parameters.Add(p => p.Items, GetDropdownItems());
        });

        component.Find(".bit-drp-wrp").Click();
        component.Find(".bit-drp-sin").Input("zzz");

        // Nothing matched, so the callout says so instead of showing the group names of an empty list.
        Assert.AreEqual(0, component.FindAll(".bit-drp-ihd").Count);
        Assert.AreEqual(0, component.FindAll(".bit-drp-sep").Count);
        Assert.AreEqual(1, component.FindAll(".bit-drp-emp").Count);
    }

    [TestMethod]
    public void BitDropdownHiddenItemShouldStayHiddenBesideACustomStyle()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var items = new List<BitDropdownItem<string>>
        {
            new() { Text = "Apple", Value = "f-app" },
            new() { Text = "Orange", Value = "f-ora", IsHidden = true }
        };
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Styles, new BitDropdownClassStyles { ItemButton = "color:red" });
        });

        // The two declarations have to be separated: "display:none color:red" is one invalid
        // declaration rather than two, and the item that was meant to be hidden would stay on screen.
        var style = component.FindAll("[role=option]")[1].GetAttribute("style");
        Assert.AreEqual("display:none;color:red", style);
    }

    [TestMethod]
    public void BitDropdownComboChangeShouldNotReopenTheCallout()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Combo, true);
            parameters.Add(p => p.Items, GetShortDropdownItems());
        });

        component.Find(".bit-drp-wrp").Click();
        Assert.IsTrue(component.Instance.IsOpen);

        // Tab dismisses the callout, and the browser then raises the change event of the input the
        // focus is leaving. That commit must not reveal the popup again behind the user.
        component.Find(".bit-drp-wrp").KeyDown(new KeyboardEventArgs { Key = "Tab" });
        Assert.IsFalse(component.Instance.IsOpen);

        component.Find(".bit-drp-inp").Change("app");

        Assert.IsFalse(component.Instance.IsOpen);
    }

    [TestMethod]
    public void BitDropdownSelectAllShouldNotRenderWithoutCandidates()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.MultiSelect, true);
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.ShowSelectAll, true);
            parameters.Add(p => p.ShowSearchBox, true);
            parameters.Add(p => p.Items, GetShortDropdownItems());
        });

        component.Find(".bit-drp-wrp").Click();
        Assert.AreEqual(1, component.FindAll(".bit-drp-sab").Count);

        // A search that matched nothing leaves the item with nothing to select, so it goes with the
        // list instead of topping the empty state with a control that cannot do anything at all.
        component.Find(".bit-drp-sin").Input("zzz");
        Assert.AreEqual(0, component.FindAll(".bit-drp-sab").Count);

        component.Find(".bit-drp-sin").Input(string.Empty);
        Assert.AreEqual(1, component.FindAll(".bit-drp-sab").Count);
    }

    [TestMethod]
    public void BitDropdownUnderlinedShouldTakeTheCorrectClass()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetShortDropdownItems());
        });

        Assert.IsFalse(component.Find(".bit-drp").ClassList.Contains("bit-drp-und"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Underlined, true);
            parameters.Add(p => p.Items, GetShortDropdownItems());
        });

        Assert.IsTrue(component.Find(".bit-drp").ClassList.Contains("bit-drp-und"));
    }

    [TestMethod]
    public void BitDropdownDescriptionShouldDescribeTheDropdown()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetShortDropdownItems());
        });

        // Nothing is described, so nothing points at an element that is not there.
        Assert.IsFalse(component.Find("[role=combobox]").HasAttribute("aria-describedby"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Combo, true);
            parameters.Add(p => p.Items, GetShortDropdownItems());
            parameters.Add(p => p.Description, "Pick the one you like");
        });

        var description = component.Find(".bit-drp-des");
        Assert.AreEqual("Pick the one you like", description.TextContent.Trim());

        // Both the combobox and the editable input it wraps are described by it, so the text is read
        // along with the dropdown instead of being left as text that only happens to sit under it.
        Assert.AreEqual(description.GetAttribute("id"), component.Find("[role=combobox]").GetAttribute("aria-describedby"));
        Assert.AreEqual(description.GetAttribute("id"), component.Find(".bit-drp-inp").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitDropdownDescriptionTemplateShouldReplaceTheDescription()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetShortDropdownItems());
            parameters.Add(p => p.Description, "Plain text");
            parameters.Add(p => p.DescriptionTemplate, "<b>Templated</b>");
        });

        var description = component.Find(".bit-drp-des");

        Assert.AreEqual("Templated", description.TextContent.Trim());
        Assert.AreEqual(description.GetAttribute("id"), component.Find("[role=combobox]").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitDropdownChipTemplateShouldReplaceTheChipContent()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Chips, true);
            parameters.Add(p => p.MultiSelect, true);
            parameters.Add(p => p.Items, GetShortDropdownItems());
            parameters.Add(p => p.DefaultValues, new[] { "f-app" });
            parameters.Add(p => p.ChipTemplate, (BitDropdownItem<string> item) =>
                builder => builder.AddContent(0, $"[{item.Text}]"));
        });

        var chip = component.Find(".bit-drp-chp");

        // The template draws the content of the chip; the remove button is still rendered after it, so
        // a chip stays removable however it is drawn.
        Assert.IsTrue(chip.TextContent.Contains("[Apple]"));
        Assert.AreEqual(1, component.FindAll(".bit-drp-crb").Count);
    }

    [TestMethod]
    public void BitDropdownSelectionShouldFollowAValuesCollectionMutatedInPlace()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var values = new List<string?> { "f-app" };
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.MultiSelect, true);
            parameters.Add(p => p.Values, values);
            parameters.Add(p => p.Items, GetShortDropdownItems());
        });

        Assert.AreEqual(1, component.Instance.SelectedItems.Count);

        // The selection is looked up in a set built from Values, which a parameter set has to rebuild:
        // a consumer that mutates the same collection instead of replacing it would otherwise keep
        // seeing the selection it had when the set was built.
        values.Add("f-ban");
        component.Render(parameters =>
        {
            parameters.Add(p => p.MultiSelect, true);
            parameters.Add(p => p.Values, values);
            parameters.Add(p => p.Items, GetShortDropdownItems());
        });

        Assert.AreEqual(2, component.Instance.SelectedItems.Count);
    }

    [TestMethod]
    public void BitDropdownHomeAndEndShouldJumpFromTheClosedTrigger()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetShortDropdownItems());
        });

        var wrapper = component.Find(".bit-drp-wrp");

        // Reaching the last item of a long list should not take opening it and then pressing a second
        // key, so Home and End jump to the ends of the list from the closed dropdown as well.
        wrapper.KeyDown(new KeyboardEventArgs { Key = "End" });

        Assert.IsTrue(component.Instance.IsOpen);
        var invocations = Context.JSInterop.Invocations["BitBlazorUI.Dropdowns.focusItem"];
        Assert.AreEqual("last", invocations[^1].Arguments[1]);

        wrapper.KeyDown(new KeyboardEventArgs { Key = "Escape" });
        wrapper.KeyDown(new KeyboardEventArgs { Key = "Home" });

        invocations = Context.JSInterop.Invocations["BitBlazorUI.Dropdowns.focusItem"];
        Assert.AreEqual("first", invocations[^1].Arguments[1]);
    }

    [TestMethod]
    public void BitDropdownMaxHeightShouldReachTheCallout()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.MaxHeight, 180);
            parameters.Add(p => p.Items, GetShortDropdownItems());
        });

        component.Find(".bit-drp-wrp").Click();

        // The callout sets the height of its scrollable list from the viewport with an inline style, so
        // a cap the consumer asks for can only be applied there - a stylesheet rule would never win.
        var toggle = Context.JSInterop.Invocations["BitBlazorUI.Callouts.toggle"];
        // The cap is the 18th argument of the positioning call; the ones after it are the optional extras
        // (the arrow, the gap and the dismissal opt-out) that only the callout component itself passes.
        Assert.AreEqual(180, toggle[^1].Arguments[17]);
    }

    [TestMethod]
    public void BitDropdownVirtualizeShouldScrollToTheSelectedItem()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Virtualize, true);
            parameters.Add(p => p.ItemSize, 40);
            parameters.Add(p => p.DefaultValue, "300");
            parameters.Add(p => p.Items, GetRangeDropdownItems(500));
        });

        component.Find(".bit-drp-wrp").Click();

        // Only the items around the visible window exist in the DOM, so a selection further down cannot
        // be found - let alone focused - before the list has been scrolled to it. The index it sits at,
        // and the height of a row, are what the scroll is computed from.
        var invocations = Context.JSInterop.Invocations["BitBlazorUI.Dropdowns.focusItem"];
        Assert.AreEqual("selected", invocations[^1].Arguments[1]);
        Assert.AreEqual(299, invocations[^1].Arguments[4]);
        Assert.AreEqual(40, invocations[^1].Arguments[5]);
    }

    [TestMethod]
    public void BitDropdownSelectedItemIndexShouldNotBeReportedWithoutVirtualization()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, "f-ban");
            parameters.Add(p => p.Items, GetShortDropdownItems());
        });

        component.Find(".bit-drp-wrp").Click();

        // Every item is in the DOM here, so the selected one is found by its own markup and there is
        // nothing to scroll to first.
        var invocations = Context.JSInterop.Invocations["BitBlazorUI.Dropdowns.focusItem"];
        Assert.AreEqual("selected", invocations[^1].Arguments[1]);
        Assert.AreEqual(-1, invocations[^1].Arguments[4]);
    }

    [TestMethod]
    public void BitDropdownValueComparerShouldDecideWhichItemAValueSelects()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var items = GetShortDropdownItems();
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, "F-APP");
            parameters.Add(p => p.Items, items);
        });

        // The default equality of the value type is case sensitive, so a value that only differs in case
        // names no item at all.
        Assert.AreEqual(0, component.Instance.SelectedItems.Count);
        Assert.IsNull(component.Instance.SelectedItem);

        component.Render(parameters =>
        {
            parameters.Add(p => p.DefaultValue, "F-APP");
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.ValueComparer, StringComparer.OrdinalIgnoreCase);
        });

        // A comparer of your own is what a value arriving from a form, a query string or a fresh fetch
        // needs to be recognized as the item it names.
        Assert.AreEqual("Apple", component.Instance.SelectedItem?.Text);
    }

    [TestMethod]
    public void BitDropdownValueComparerShouldGovernTheMultiSelectLookup()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.MultiSelect, true);
            parameters.Add(p => p.Items, GetShortDropdownItems());
            parameters.Add(p => p.ValueComparer, StringComparer.OrdinalIgnoreCase);
            parameters.Add(p => p.DefaultValues, new[] { "F-APP", "V-BRO" });
        });

        // The set the selection is looked up in has to answer exactly what a scan of Values would, so
        // the fast path is built with the same comparer as every other comparison.
        Assert.AreEqual(2, component.Instance.SelectedItems.Count);
        CollectionAssert.AreEqual(new[] { "Apple", "Broccoli" }, component.Instance.SelectedItems.Select(i => i.Text).ToArray());

        var options = component.FindAll("[role=option]");
        Assert.AreEqual("true", options[0].GetAttribute("aria-selected"));
        Assert.AreEqual("false", options[1].GetAttribute("aria-selected"));
        Assert.AreEqual("true", options[3].GetAttribute("aria-selected"));
    }

    [TestMethod]
    public void BitDropdownComboShouldShowWhichItemEnterWouldTake()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        // Enter reads what the input actually holds rather than the (possibly debounced) search term.
        Context.JSInterop.Setup<string>("BitBlazorUI.Utils.getProperty", _ => true).SetResult("b");

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Combo, true);
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.AutoSelectFirstMatch, true);
            parameters.Add(p => p.Items, GetShortDropdownItems());
        });

        component.Find(".bit-drp-wrp").Click();

        // Nothing is typed yet, so there is nothing Enter would take and nothing to point at.
        Assert.AreEqual(0, component.FindAll(".bit-drp-ctg").Count);
        Assert.IsFalse(component.Find(".bit-drp-inp").HasAttribute("aria-activedescendant"));

        component.Find(".bit-drp-inp").Input("b");

        // A partially typed term stands for the first item it still matches, and the list says so before
        // Enter is pressed rather than only after - by sight, and through aria-activedescendant for the
        // users who have none.
        var target = component.Find(".bit-drp-ctg");
        Assert.AreEqual("Banana", target.TextContent.Trim());
        Assert.AreEqual(target.GetAttribute("id"), component.Find(".bit-drp-inp").GetAttribute("aria-activedescendant"));

        component.Find(".bit-drp-inp").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.AreEqual("f-ban", component.Instance.Value);
    }

    [TestMethod]
    public void BitDropdownComboShouldPointAtTheExactMatchWithoutAutoSelect()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Combo, true);
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.Items, GetShortDropdownItems());
        });

        component.Find(".bit-drp-wrp").Click();

        // Without AutoSelectFirstMatch only a term that names an item exactly commits to anything, so a
        // partial one points at nothing...
        component.Find(".bit-drp-inp").Input("ban");
        Assert.AreEqual(0, component.FindAll(".bit-drp-ctg").Count);

        // ...while the full name of an item is a commit that stands, and is shown as one.
        component.Find(".bit-drp-inp").Input("Banana");
        Assert.AreEqual("Banana", component.Find(".bit-drp-ctg").TextContent.Trim());
    }

    [TestMethod]
    public void BitDropdownShouldAnnounceThatTheSelectionLimitIsReached()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.MultiSelect, true);
            parameters.Add(p => p.MaxSelectedItems, 2);
            parameters.Add(p => p.Items, GetShortDropdownItems());
            parameters.Add(p => p.DefaultValues, new[] { "f-app" });
        });

        var liveRegion = component.Find(".bit-drp-lvr");
        Assert.AreEqual(string.Empty, liveRegion.TextContent.Trim());

        component.FindAll("[role=option]")[1].Click();

        // Reaching the limit disables every item that is not selected yet, which is a change only a
        // sighted user notices on their own.
        Assert.AreEqual("Maximum of 2 items selected", component.Find(".bit-drp-lvr").TextContent.Trim());

        component.FindAll("[role=option]")[1].Click();

        // ...and it goes quiet again as soon as there is room, so nothing keeps saying a limit that no
        // longer holds.
        Assert.AreEqual(string.Empty, component.Find(".bit-drp-lvr").TextContent.Trim());
    }

    [TestMethod]
    public void BitDropdownMaxSelectedItemsTextShouldBeCustomizable()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.MultiSelect, true);
            parameters.Add(p => p.MaxSelectedItems, 1);
            parameters.Add(p => p.Items, GetShortDropdownItems());
            parameters.Add(p => p.DefaultValues, new[] { "f-app" });
            parameters.Add(p => p.MaxSelectedItemsText, "Höchstens {0} Einträge");
        });

        Assert.AreEqual("Höchstens 1 Einträge", component.Find(".bit-drp-lvr").TextContent.Trim());
    }

    [TestMethod]
    public void BitDropdownReselectShouldFollowTheValueAndNotTheInstance()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var selected = 0;
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, "f-app");
            parameters.Add(p => p.Items, GetShortDropdownItems());
            parameters.Add(p => p.OnSelectItem, (BitDropdownItem<string> _) => selected++);
        });

        // The items are rebuilt on every render here, so the option carrying the current value is a
        // different instance each time - and picking it is still a reselection, which only Reselectable
        // reports.
        component.FindAll("[role=option]")[0].Click();
        Assert.AreEqual(0, selected);

        component.FindAll("[role=option]")[1].Click();
        Assert.AreEqual(1, selected);
    }

    private void HandleValueChanged(string value)
    {
        _bitDropdownValue = value;
    }

    private void HandleValuesChanged(IEnumerable<string?>? values)
    {
        _bitDropdownValues = values;
    }

    // The ComboBox mode filters through the input of the dropdown itself, so the search box is not
    // rendered there and the reference to its input is never assigned. Opening the callout used to
    // focus that reference all the same, which throws rather than doing nothing.
    [TestMethod]
    public void BitDropdownShowSearchBoxShouldStayInertInTheComboMode()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Combo, true);
            parameters.Add(p => p.ShowSearchBox, true);
            parameters.Add(p => p.AutoFocusSearchBox, true);
            parameters.Add(p => p.Items, GetShortDropdownItems());
        });

        component.Find(".bit-drp-wrp").Click();

        Assert.AreEqual(0, component.FindAll(".bit-drp-sin").Count);
        Assert.IsNull(component.Instance.SearchInputElement);

        // The same key press that opens a non-combo dropdown onto its search box.
        component.Find(".bit-drp-wrp").KeyDown(new KeyboardEventArgs { Key = "Enter" });
    }

    // The trigger is named after the element that shows the selection. An input inside that element
    // would contribute its value to the name, making the trigger report back whatever is being typed.
    [TestMethod]
    public void BitDropdownComboInputShouldStayOutOfTheTriggerAccessibleName()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Combo, true);
            parameters.Add(p => p.Items, GetShortDropdownItems());
            parameters.Add(p => p.Value, "f-app");
        });

        var labelledBy = component.Find(".bit-drp-wrp").GetAttribute("aria-labelledby");
        var namedBy = component.Find($"#{labelledBy}");

        Assert.AreEqual("Apple", namedBy.TextContent.Trim());
        Assert.AreEqual(0, namedBy.QuerySelectorAll("input").Length);
        Assert.AreEqual(1, component.FindAll(".bit-drp-inp").Count);
    }

    // A live region only announces while it is in the accessibility tree, and the callout is hidden
    // whenever it is closed - which is exactly when the loading state has to be announced.
    [TestMethod]
    public void BitDropdownLiveRegionShouldLiveOutsideTheCallout()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.IsLoading, true);
            parameters.Add(p => p.LoadingText, "Fetching");
            parameters.Add(p => p.Items, GetShortDropdownItems());
        });

        var liveRegion = component.Find(".bit-drp-lvr");

        Assert.AreEqual("Fetching", liveRegion.TextContent.Trim());
        Assert.IsNull(liveRegion.Closest(".bit-drp-cal"));
        Assert.IsNotNull(liveRegion.Closest(".bit-drp"));
    }

    // The placeholder and the chips display sit in the same element, so the one must not swallow the
    // other while nothing is selected yet.
    [TestMethod]
    public void BitDropdownChipsShouldStillShowThePlaceholderWhenNothingIsSelected()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Chips, true);
            parameters.Add(p => p.MultiSelect, true);
            parameters.Add(p => p.Placeholder, "Select options");
            parameters.Add(p => p.Items, GetShortDropdownItems());
        });

        Assert.AreEqual("Select options", component.Find(".bit-drp-tdp").TextContent.Trim());

        component.Render(parameters =>
        {
            parameters.Add(p => p.Chips, true);
            parameters.Add(p => p.MultiSelect, true);
            parameters.Add(p => p.Placeholder, "Select options");
            parameters.Add(p => p.Items, GetShortDropdownItems());
            parameters.Add(p => p.Values, new[] { "f-app" });
        });

        Assert.AreEqual(1, component.FindAll(".bit-drp-chp").Count);
        Assert.AreEqual("Apple", component.Find(".bit-drp-tdp").TextContent.Trim());
    }

    [TestMethod]
    public void BitDropdownDynamicShouldOfferToCreateTheTypedTextAsAnItem()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Combo, true);
            parameters.Add(p => p.Dynamic, true);
            parameters.Add(p => p.Items, GetShortDropdownItems());
        });

        component.Find(".bit-drp-wrp").Click();

        // Nothing is typed, so there is nothing to create out of.
        Assert.AreEqual(0, component.FindAll(".bit-drp-dyn").Count);

        component.Find(".bit-drp-inp").Input("Kiwi");

        // A term that names no item is one the list offers to create, and the ComboBox input points at
        // that offer, because it is exactly what Enter would take.
        var dynamicItem = component.Find(".bit-drp-dyn");
        Assert.AreEqual("Add \"Kiwi\"", dynamicItem.TextContent.Trim());
        Assert.AreEqual("option", dynamicItem.GetAttribute("role"));
        Assert.AreEqual(dynamicItem.GetAttribute("id"), component.Find(".bit-drp-inp").GetAttribute("aria-activedescendant"));

        // The empty state is not shown beside it: a list that says "no results" while offering to create
        // the very thing that was searched for contradicts itself.
        Assert.AreEqual(0, component.FindAll(".bit-drp-emp").Count);

        // A term that names an existing item is a selection rather than a creation, so the offer goes.
        component.Find(".bit-drp-inp").Input("Apple");
        Assert.AreEqual(0, component.FindAll(".bit-drp-dyn").Count);
    }

    [TestMethod]
    public void BitDropdownDynamicItemShouldCommitTheTypedTextWhenItIsClicked()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        BitDropdownItem<string>? added = null;
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Combo, true);
            parameters.Add(p => p.Dynamic, true);
            parameters.Add(p => p.MultiSelect, true);
            parameters.Add(p => p.Items, GetShortDropdownItems());
            parameters.Add(p => p.OnDynamicAdd, item => added = item);
            parameters.Add(p => p.DynamicValueGenerator, (BitDropdownItem<string>? item) => item?.Text ?? string.Empty);
        });

        component.Find(".bit-drp-wrp").Click();
        component.Find(".bit-drp-inp").Input("Kiwi");

        component.Find(".bit-drp-dyn").Click();

        // Picking the row is the pointer equivalent of pressing Enter: the item is created, reported and
        // selected, and the term it was created from is gone from the input along with the row itself.
        Assert.IsNotNull(added);
        Assert.AreEqual("Kiwi", added!.Text);
        CollectionAssert.AreEqual(new[] { "Kiwi" }, component.Instance.Values!.ToArray());
        Assert.AreEqual(0, component.FindAll(".bit-drp-dyn").Count);

        // A multi select callout stays open so the next term can be typed right away.
        Assert.IsTrue(component.Instance.IsOpen);
    }

    [TestMethod]
    public void BitDropdownDynamicItemTextFormatShouldBeCustomizable()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Combo, true);
            parameters.Add(p => p.Dynamic, true);
            parameters.Add(p => p.Items, GetShortDropdownItems());
            parameters.Add(p => p.DynamicItemTextFormat, "Create the new tag '{0}'");
        });

        component.Find(".bit-drp-wrp").Click();
        component.Find(".bit-drp-inp").Input("Kiwi");

        Assert.AreEqual("Create the new tag 'Kiwi'", component.Find(".bit-drp-dyn").TextContent.Trim());
    }

    [TestMethod]
    public void BitDropdownDynamicItemShouldNotOfferWhatTheCommitWouldRefuse()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Combo, true);
            parameters.Add(p => p.Dynamic, true);
            parameters.Add(p => p.MultiSelect, true);
            parameters.Add(p => p.MaxSelectedItems, 1);
            parameters.Add(p => p.Items, GetShortDropdownItems());
            parameters.Add(p => p.Values, new[] { "f-app" });
        });

        component.Find(".bit-drp-wrp").Click();
        component.Find(".bit-drp-inp").Input("Kiwi");

        // The selection limit leaves no room for another item, so the row would offer an addition the
        // commit refuses.
        Assert.AreEqual(0, component.FindAll(".bit-drp-dyn").Count);

        // A term that names one of the selections already is the same story from the other side.
        component.Render(parameters =>
        {
            parameters.Add(p => p.Combo, true);
            parameters.Add(p => p.Dynamic, true);
            parameters.Add(p => p.MultiSelect, true);
            parameters.Add(p => p.Items, GetShortDropdownItems());
            parameters.Add(p => p.Values, new[] { "f-app" });
        });

        component.Find(".bit-drp-inp").Input("apple");
        Assert.AreEqual(0, component.FindAll(".bit-drp-dyn").Count);
    }

    [TestMethod]
    public void BitDropdownDynamicItemShouldNotBeOfferedInAReadOnlyDropdown()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Combo, true);
            parameters.Add(p => p.Dynamic, true);
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.Items, GetShortDropdownItems());
        });

        component.Find(".bit-drp-wrp").Click();
        component.Find(".bit-drp-inp").Input("Kiwi");

        Assert.AreEqual(0, component.FindAll(".bit-drp-dyn").Count);
    }

    [TestMethod]
    public void BitDropdownComboTypingShouldRevealTheCalloutWithoutImmediate()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var searches = new List<string?>();
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Combo, true);
            parameters.Add(p => p.Items, GetShortDropdownItems());
            parameters.Add(p => p.OnSearch, v => searches.Add(v));
        });

        Assert.IsFalse(component.Instance.IsOpen);

        // A combo box filters as it is typed whatever Immediate says, so the list it filters has to be
        // on the screen: typing at a closed dropdown filtered a list nobody could see.
        component.Find(".bit-drp-inp").Input("app");

        Assert.IsTrue(component.Instance.IsOpen);
        Assert.AreEqual(1, component.FindAll(".bit-drp-itm").Count);
        CollectionAssert.AreEqual(new[] { "app" }, searches);

        // The commit of the very term the input handler already searched for reports nothing new.
        component.Find(".bit-drp-inp").Change("app");
        CollectionAssert.AreEqual(new[] { "app" }, searches);
    }

    [TestMethod]
    public void BitDropdownDividerShouldNotBeExposedAsAListboxChild()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetDropdownItems());
        });

        // The only children a listbox may have are its options and their groups, and a rule between two
        // groups carries nothing a screen reader has to hear, so it is drawn for the eye alone.
        var divider = component.Find(".bit-drp-sep");
        Assert.AreEqual("presentation", divider.GetAttribute("role"));
        Assert.AreEqual("true", divider.GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public void BitDropdownLoadingShouldMarkTheListboxAsBusy()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.IsLoading, true);
            parameters.Add(p => p.Items, GetShortDropdownItems());
        });

        Assert.AreEqual("true", component.Find(".bit-drp-scn").GetAttribute("aria-busy"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.IsLoading, false);
            parameters.Add(p => p.Items, GetShortDropdownItems());
        });

        Assert.IsFalse(component.Find(".bit-drp-scn").HasAttribute("aria-busy"));
    }

    [TestMethod]
    public async Task BitDropdownFocusHelpersShouldDoNothingWithoutTheirElement()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.ShowSearchBox, true);
            parameters.Add(p => p.Items, GetShortDropdownItems());
        });

        // The search box lives in the callout, so while the callout is closed there is no element to
        // report - and none to focus either, which used to throw on a reference that was never assigned.
        Assert.IsNull(component.Instance.SearchInputElement);
        Assert.IsNull(component.Instance.ComboInputElement);

        await component.InvokeAsync(async () => await component.Instance.FocusSearchInputAsync());
        await component.InvokeAsync(async () => await component.Instance.FocusComboInputAsync());
    }

    [TestMethod]
    public void BitDropdownNoWrapNavigationShouldReachTheFocusHelper()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetShortDropdownItems());
        });

        component.Find(".bit-drp-wrp").Click();

        // Whether the arrow keys wrap around is decided where the focus is actually moved, which is the
        // JS side, so the parameter has to be part of every call that moves it.
        var invocations = Context.JSInterop.Invocations["BitBlazorUI.Dropdowns.focusItem"];
        Assert.AreEqual(false, invocations[^1].Arguments[6]);

        component.Render(parameters =>
        {
            parameters.Add(p => p.NoWrapNavigation, true);
            parameters.Add(p => p.Items, GetShortDropdownItems());
        });

        component.Find(".bit-drp-wrp").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });

        invocations = Context.JSInterop.Invocations["BitBlazorUI.Dropdowns.focusItem"];
        Assert.AreEqual(true, invocations[^1].Arguments[6]);
    }

    [TestMethod]
    public void BitDropdownMinSearchLengthShouldSayHowManyCharactersAreMissing()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.ShowSearchBox, true);
            parameters.Add(p => p.MinSearchLength, 3);
            parameters.Add(p => p.Items, GetShortDropdownItems());
        });

        component.Find(".bit-drp-wrp").Click();

        // Nothing typed yet, so the full list under the search box needs no explaining.
        Assert.AreEqual(0, component.FindAll(".bit-drp-shn").Count);

        component.Find(".bit-drp-sin").Input("a");

        // The list is still the whole list, which is the one state where what is on the screen has
        // nothing to do with what was typed - so it is said, on the screen and to a screen reader alike.
        Assert.AreEqual("Type 2 more characters to search", component.Find(".bit-drp-shn").TextContent.Trim());
        Assert.AreEqual("Type 2 more characters to search", component.Find(".bit-drp-lvr").TextContent.Trim());
        Assert.AreEqual(4, component.FindAll("[role=option]").Count);

        component.Find(".bit-drp-sin").Input("ap");

        Assert.AreEqual("Type 1 more character to search", component.Find(".bit-drp-shn").TextContent.Trim());

        component.Find(".bit-drp-sin").Input("app");

        // The term filters the list now, so the hint gives way to the result count.
        Assert.AreEqual(0, component.FindAll(".bit-drp-shn").Count);
        Assert.AreEqual("1 result available", component.Find(".bit-drp-lvr").TextContent.Trim());
    }

    [TestMethod]
    public void BitDropdownMinSearchLengthTextShouldBeLocalizable()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.ShowSearchBox, true);
            parameters.Add(p => p.MinSearchLength, 4);
            parameters.Add(p => p.MinSearchLengthText, "Noch {0} Zeichen");
            parameters.Add(p => p.Items, GetShortDropdownItems());
        });

        component.Find(".bit-drp-wrp").Click();
        component.Find(".bit-drp-sin").Input("ap");

        Assert.AreEqual("Noch 2 Zeichen", component.Find(".bit-drp-shn").TextContent.Trim());
    }

    [TestMethod]
    public void BitDropdownMinSearchLengthHintShouldFollowTheComboInput()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Combo, true);
            parameters.Add(p => p.MinSearchLength, 3);
            parameters.Add(p => p.Items, GetShortDropdownItems());
        });

        // The ComboBox mode has no search box: the input of the dropdown itself is what the items are
        // filtered by, so it is the text the hint has to be measured against.
        component.Find(".bit-drp-inp").Input("a");

        Assert.AreEqual("Type 2 more characters to search", component.Find(".bit-drp-shn").TextContent.Trim());
    }

    [TestMethod]
    public void BitDropdownSearchResultCountShouldFollowTheRenderedList()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MultiSelect, true);
            parameters.Add(p => p.ShowSearchBox, true);
            parameters.Add(p => p.HideSelectedItems, true);
            parameters.Add(p => p.Values, new[] { "f-app" });
            parameters.Add(p => p.Items, GetShortDropdownItems());
        });

        component.Find(".bit-drp-wrp").Click();
        component.Find(".bit-drp-sin").Input("a");

        // Apple, Orange and Banana match, but Apple is already selected and HideSelectedItems takes it
        // out of the list - so counting it would announce a result nobody can reach.
        Assert.AreEqual(2, component.FindAll("[role=option]").Count);
        Assert.AreEqual("2 results available", component.Find(".bit-drp-lvr").TextContent.Trim());
    }

    [TestMethod]
    public void BitDropdownListboxShouldBeNamedByTheAriaLabelWithoutALabel()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, "Fruits");
            parameters.Add(p => p.Items, GetShortDropdownItems());
        });

        // Without a label element there is nothing for the listbox to point at, so the name given to the
        // dropdown stands in - a listbox that reports no name at all leaves its options unattributed.
        var listbox = component.Find(".bit-drp-scn");
        Assert.AreEqual("Fruits", listbox.GetAttribute("aria-label"));
        Assert.IsFalse(listbox.HasAttribute("aria-labelledby"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Label, "Fruit");
            parameters.Add(p => p.AriaLabel, "Fruits");
            parameters.Add(p => p.Items, GetShortDropdownItems());
        });

        listbox = component.Find(".bit-drp-scn");
        Assert.IsFalse(listbox.HasAttribute("aria-label"));
        Assert.IsTrue(listbox.HasAttribute("aria-labelledby"));
    }

    [TestMethod]
    public void BitDropdownCalloutShouldCarryTheDirectionOfTheDropdown()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Rtl);
            parameters.Add(p => p.Items, GetShortDropdownItems());
        });

        // The callout is rendered outside the root element (and reparented to the body while open), so
        // it never inherits the direction the root declares - an RTL dropdown on an LTR page would
        // otherwise open a list whose text starts against the wrong edge.
        Assert.AreEqual("rtl", component.Find(".bit-drp-cal").GetAttribute("dir"));

        var undirected = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetShortDropdownItems());
        });

        // Without a direction of its own the callout follows the page, like every other element.
        Assert.IsFalse(undirected.Find(".bit-drp-cal").HasAttribute("dir"));
    }

    [TestMethod]
    public void BitDropdownSearchBoxClearButtonShouldFollowTheTypedTextWithoutImmediate()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.ShowSearchBox, true);
            parameters.Add(p => p.MinSearchLength, 3);
            parameters.Add(p => p.Items, GetShortDropdownItems());
        });

        component.Find(".bit-drp-wrp").Click();
        Assert.AreEqual(0, component.FindAll(".bit-drp-sbc").Count);

        component.Find(".bit-drp-sin").Input("ap");

        // The search itself waits for the input to be committed here, but the parts that describe the
        // field - the clear button and the too-short hint - are about what is in it, so they follow the
        // typing. The list is untouched, since no term has been applied yet.
        Assert.AreEqual(1, component.FindAll(".bit-drp-sbc").Count);
        Assert.AreEqual("Type 1 more character to search", component.Find(".bit-drp-shn").TextContent.Trim());
        Assert.AreEqual(4, component.FindAll("[role=option]").Count);

        component.Find(".bit-drp-sbc").Click();

        // Clearing an uncommitted term only empties the input: there is no search behind it to re-run.
        Assert.AreEqual(0, component.FindAll(".bit-drp-sbc").Count);
        Assert.AreEqual(0, component.FindAll(".bit-drp-shn").Count);
        Assert.IsNull(component.Find(".bit-drp-sin").GetAttribute("value"));
    }

    [TestMethod]
    public void BitDropdownSearchBoxShouldStillClearACommittedTerm()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var searches = new List<string?>();
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.ShowSearchBox, true);
            parameters.Add(p => p.Items, GetShortDropdownItems());
            parameters.Add(p => p.OnSearch, (string? text) => searches.Add(text));
        });

        component.Find(".bit-drp-wrp").Click();
        component.Find(".bit-drp-sin").Change("app");

        Assert.AreEqual(1, component.FindAll("[role=option]").Count);
        CollectionAssert.AreEqual(new[] { "app" }, searches);

        component.Find(".bit-drp-sbc").Click();

        // A term that was applied is a search to take back, so the list comes back and the callback is
        // told about it - which is what tells an ItemsProvider to stop serving the abandoned term.
        Assert.AreEqual(4, component.FindAll("[role=option]").Count);
        CollectionAssert.AreEqual(new string?[] { "app", null }, searches);
    }

    [TestMethod]
    public void BitDropdownItemsProviderShouldNotKeepTheItemOfAReplacedValue()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        static ValueTask<BitDropdownItemsProviderResult<BitDropdownItem<string>>> provider(BitDropdownItemsProviderRequest<BitDropdownItem<string>> _)
            => ValueTask.FromResult(BitDropdownItemsProviderResult.From<BitDropdownItem<string>>([], 0));

        var initial = new List<BitDropdownItem<string>> { new() { Text = "Apple", Value = "f-app" } };

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Virtualize, true);
            parameters.Add(p => p.ItemsProvider, provider);
            parameters.Add(p => p.InitialSelectedItems, initial);
        });

        Assert.AreEqual("Apple", component.Find(".bit-drp-tdp").TextContent.Trim());

        component.Render(parameters =>
        {
            parameters.Add(p => p.Virtualize, true);
            parameters.Add(p => p.ItemsProvider, provider);
            parameters.Add(p => p.InitialSelectedItems, initial);
            parameters.Add(p => p.Value, "f-ban");
        });

        // The item of a value the provider has not handed over yet is kept, since it usually only means
        // the window it lives in was never fetched - but only while it still stands for the value the
        // dropdown holds, or the trigger goes on naming the selection before it.
        Assert.AreEqual(string.Empty, component.Find(".bit-drp-tdp").TextContent.Trim());
    }

    [TestMethod]
    public void BitDropdownComboShouldNotCommitADisabledItem()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        Context.JSInterop.Setup<string>("BitBlazorUI.Utils.getProperty", _ => true).SetResult("Orange");

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Combo, true);
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.Items, GetDropdownItemsWithDisabled());
        });

        var comboInput = component.Find(".bit-drp-inp");
        comboInput.Input("Orange");

        // The item the list shows as unavailable is not one a commit may take, so it is not marked as
        // the one Enter is about to select either.
        Assert.AreEqual(0, component.FindAll(".bit-drp-ctg").Count);
        Assert.IsNull(comboInput.GetAttribute("aria-activedescendant"));

        comboInput.KeyDown(new KeyboardEventArgs { Key = "Enter" });

        // Typing the exact text of a disabled item must not select what clicking it would refuse.
        Assert.IsNull(component.Instance.Value);
    }

    [TestMethod]
    public void BitDropdownComboShouldNotCommitAGroupHeader()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        Context.JSInterop.Setup<string>("BitBlazorUI.Utils.getProperty", _ => true).SetResult("Fruits");

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Combo, true);
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.Items, GetDropdownItems());
        });

        var comboInput = component.Find(".bit-drp-inp");
        comboInput.Input("Fruits");
        comboInput.KeyDown(new KeyboardEventArgs { Key = "Enter" });

        // A header names the items under it and is not one of them, so its text cannot be committed.
        Assert.IsNull(component.Instance.Value);
        Assert.IsNull(component.Instance.SelectedItem);
    }

    [TestMethod]
    public void BitDropdownComboShouldNotCommitAHiddenItem()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        Context.JSInterop.Setup<string>("BitBlazorUI.Utils.getProperty", _ => true).SetResult("Banana");

        var items = GetShortDropdownItems();
        items[2].IsHidden = true;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Combo, true);
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.Items, items);
        });

        var comboInput = component.Find(".bit-drp-inp");
        comboInput.Input("Banana");
        comboInput.KeyDown(new KeyboardEventArgs { Key = "Enter" });

        // An item that is not on the screen cannot be clicked, so it cannot be typed into either.
        Assert.IsNull(component.Instance.Value);
    }

    [TestMethod]
    public void BitDropdownShouldResyncTheSelectionWhenItemsAreMutatedInPlace()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var items = new List<BitDropdownItem<string>> { new() { Text = "Apple", Value = "f-app" } };

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Value, "f-ban");
        });

        // The value names an item the list does not hold yet, so the trigger has nothing to show.
        Assert.AreEqual(string.Empty, component.Find(".bit-drp-tdp").TextContent.Trim());

        items.Add(new() { Text = "Banana", Value = "f-ban" });
        component.Render();

        // A list the consumer adds to in place keeps its reference, so the item of an already selected
        // value would otherwise never reach the trigger.
        Assert.AreEqual("Banana", component.Find(".bit-drp-tdp").TextContent.Trim());
        Assert.AreEqual(2, component.FindAll("[role=option]").Count);
    }

    [TestMethod]
    public void BitDropdownClearOnEscapeShouldOnlyClearWithNothingLeftToDismiss()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var clearCount = 0;
        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.ClearOnEscape, true);
            parameters.Add(p => p.DefaultValue, "f-app");
            parameters.Add(p => p.Items, GetShortDropdownItems());
            parameters.Add(p => p.OnClear, () => clearCount++);
        });

        var wrapper = component.Find(".bit-drp-wrp");
        wrapper.KeyDown(new KeyboardEventArgs { Key = "Enter" });
        Assert.IsTrue(component.Instance.IsOpen);

        wrapper.KeyDown(new KeyboardEventArgs { Key = "Escape" });

        // The callout is what the first press dismisses; the selection is left alone.
        Assert.IsFalse(component.Instance.IsOpen);
        Assert.AreEqual("f-app", component.Instance.Value);
        Assert.AreEqual(0, clearCount);

        wrapper.KeyDown(new KeyboardEventArgs { Key = "Escape" });

        // With nothing left to dismiss the press reaches the selection, through the very same clear the
        // clear button goes through - so it reports itself the same way.
        Assert.IsNull(component.Instance.Value);
        Assert.AreEqual(1, clearCount);
    }

    [TestMethod]
    public void BitDropdownShouldKeepTheSelectionOnEscapeWithoutClearOnEscape()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, "f-app");
            parameters.Add(p => p.Items, GetShortDropdownItems());
        });

        component.Find(".bit-drp-wrp").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.AreEqual("f-app", component.Instance.Value);
    }

    [TestMethod]
    public void BitDropdownComboClearOnEscapeShouldDropTheTypedTermFirst()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Combo, true);
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.ClearOnEscape, true);
            parameters.Add(p => p.DefaultValue, "f-app");
            parameters.Add(p => p.Items, GetShortDropdownItems());
        });

        var comboInput = component.Find(".bit-drp-inp");
        comboInput.FocusIn();
        comboInput.Input("ban");
        comboInput.KeyDown(new KeyboardEventArgs { Key = "Escape" });

        // The typed term (and the callout it revealed) is what this press takes back. The keydown of
        // the input bubbles through the trigger, which must not act on the same press a second time.
        Assert.IsFalse(component.Instance.IsOpen);
        Assert.AreEqual("f-app", component.Instance.Value);

        component.Find(".bit-drp-inp").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.IsNull(component.Instance.Value);
    }

    [TestMethod]
    public void BitDropdownSelectTextOnFocusShouldSelectTheComboInputText()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Combo, true);
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.SelectTextOnFocus, true);
            parameters.Add(p => p.Items, GetShortDropdownItems());
        });

        var comboInput = component.Find(".bit-drp-inp");
        comboInput.FocusIn();

        // An empty input has nothing to select.
        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.selectText"].Count);

        comboInput.Input("ban");
        component.Find(".bit-drp-inp").FocusIn();

        Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.selectText"].Count);
    }

    [TestMethod]
    public void BitDropdownShouldNotSelectTheComboInputTextWithoutTheParameter()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Combo, true);
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.Items, GetShortDropdownItems());
        });

        var comboInput = component.Find(".bit-drp-inp");
        comboInput.Input("ban");
        component.Find(".bit-drp-inp").FocusIn();

        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.selectText"].Count);
    }

    [TestMethod]
    public void BitDropdownComboEscapeOnTheTriggerShouldStillCloseTheCallout()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Combo, true);
            parameters.Add(p => p.Items, GetShortDropdownItems());
        });

        var wrapper = component.Find(".bit-drp-wrp");
        wrapper.KeyDown(new KeyboardEventArgs { Key = "ArrowDown", AltKey = true });
        Assert.IsTrue(component.Instance.IsOpen);

        // The trigger around the ComboBox input answers the keys the input did not: the focus never
        // reached it here, so nothing else is going to dismiss the callout.
        wrapper.KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.IsFalse(component.Instance.IsOpen);
    }

    [TestMethod]
    public async Task BitDropdownShouldSurviveAnItemsProviderReturningTheDefaultResult()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        static ValueTask<BitDropdownItemsProviderResult<BitDropdownItem<string>>> provider(BitDropdownItemsProviderRequest<BitDropdownItem<string>> _)
            => ValueTask.FromResult(default(BitDropdownItemsProviderResult<BitDropdownItem<string>>));

        var component = RenderComponent<BitDropdown<BitDropdownItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Virtualize, true);
            parameters.Add(p => p.ItemsProvider, provider);
            parameters.Add(p => p.ItemsProviderDebounceTime, 0);
        });

        component.Find(".bit-drp-wrp").Click();

        // The result is a struct, so a provider that hands back the default one carries no Items
        // collection at all - which must be an empty window rather than a crash.
        await component.InvokeAsync(() => component.Instance.RefreshItemsAsync());

        Assert.AreEqual(0, component.FindAll("[role=option]").Count);
    }

    private static List<BitDropdownItem<string>> GetDropdownItemsWithDisabled() => new()
    {
        new() { Text = "Apple", Value = "f-app" },
        new() { Text = "Orange", Value = "f-ora", IsEnabled = false },
        new() { Text = "Banana", Value = "f-ban" }
    };

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
