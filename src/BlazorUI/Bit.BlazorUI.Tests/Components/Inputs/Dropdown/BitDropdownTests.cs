using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Inputs.Dropdown;

[TestClass]
public class BitDropdownTests : BunitTestContext
{
    private string _bitDropdownValue;
    private IEnumerable<string> _bitDropdownValues;

    [DataTestMethod,
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

    [DataTestMethod,
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
        //    Assert.ThrowsException<ElementNotFoundException>(() => component.Find(".bit-drp-rlc"));
        //}
    }

    [DataTestMethod,
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
            Assert.ThrowsException<ElementNotFoundException>(() => component.Find(".bit-drp-rlc > label"));
        }
        else
        {
            Assert.AreEqual(labelFragment, component.Find(".bit-drp-rlc > label").InnerHtml);
        }
    }

    [DataTestMethod,
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
            Assert.ThrowsException<ElementNotFoundException>(() => component.Find(".bit-drp-rlc > label"));
        }
        else
        {
            var labelChild = component.Find(".bit-drp-rlc > :first-child");
            labelChild.MarkupMatches(labelFragment);
        }
    }

    [DataTestMethod,
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

    [DataTestMethod,
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
            Assert.ThrowsException<ElementNotFoundException>(() => component.Find(".bit-drp-iwr"));
        }
    }

    [DataTestMethod,
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

    [DataTestMethod,
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
        var expectedText = items.Find(i => i.Value == defaultValue && i.ItemType == BitDropdownItemType.Normal).Text;

        Assert.AreEqual(expectedText, textSpan.InnerHtml);
    }

    [DataTestMethod,
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

    [DataTestMethod,
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
        var expectedText = items.Find(i => i.Value == value && i.ItemType == BitDropdownItemType.Normal).Text;

        Assert.AreEqual(expectedText, textSpan.InnerHtml);
    }

    [DataTestMethod,
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

    [DataTestMethod,
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
                expectedText.Append(items.Find(i => i.Value == value).Text);
            }
            else
            {
                expectedText.Append(placeholder);
            }
        }

        Assert.AreEqual(expectedText.ToString(), targetSpan.InnerHtml);
    }

    [DataTestMethod,
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
            Assert.ThrowsException<ElementNotFoundException>(() => component.Find("label"));
        }
    }

    [DataTestMethod,
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

    [DataTestMethod,
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

    [DataTestMethod,
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

    [DataTestMethod,
        DataRow(true, true),
        DataRow(false, true),

        DataRow(true, false),
        DataRow(false, false)
    ]
    public void BitDropdownEnableItemSelectionShouldWorkCorrect(bool itemIsEnabled, bool isMultiSelect)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var itemsSelected = 0;
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
            parameters.Add(p => p.OnValuesChange, () => itemsSelected++);
        });

        if (isMultiSelect)
        {
            var drpItems = component.FindAll(".bit-drp-iwr", true);
            drpItems[0].GetElementsByTagName("button").First().Click();
            drpItems[1].GetElementsByTagName("button").First().Click();
            var expectedResult = itemIsEnabled ? 2 : 0;
            Assert.AreEqual(expectedResult, itemsSelected);
        }
        else
        {
            var drpItems = component.FindAll(".bit-drp-itm");
            drpItems[0].Click();
            var expectedResult = itemIsEnabled ? 1 : 0;
            Assert.AreEqual(expectedResult, itemsSelected);
        }
    }

    [DataTestMethod,
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

    [DataTestMethod,
        DataRow("f-ban,v-bro"),
        DataRow("f-ora")
    ]
    public void BitDropdownMultiSelectTwoWayBoundWithCustomHandlerShouldWorkCorrect(string values)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var isOpen = true;

        _bitDropdownValues = values.Split(",").ToArray();
        var initialValuesCount = _bitDropdownValues.Count();
        var items = BitDropdownTests.GetShortDropdownItems();
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
        if (values.Contains(items[3].Value))
        {
            expectedResult = initialValuesCount - 1;
        }
        else
        {
            expectedResult = initialValuesCount + 1;
        }

        Assert.AreEqual(expectedResult, _bitDropdownValues.Count());
    }

    [DataTestMethod,
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

    [DataTestMethod,
        DataRow(null),
        DataRow("f-ban,v-bro"),
        DataRow("f-ora")
    ]
    public void BitDropdownMultiSelectValidationFormTest(string values)
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

    [DataTestMethod,
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

    [DataTestMethod,
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

    [DataTestMethod,
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

    [DataTestMethod,
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

    [DataTestMethod,
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
            parameters.Add(p => p.MultiSelect, isMultiSelect);
            parameters.Add(p => p.Items, items);
        });

        var bitDropdown = component.Find(".bit-drp-wrp");
        bitDropdown.Click();

        var drpItems = component.FindAll(isMultiSelect ? ".bit-drp-iwr" : ".bit-drp-itm", true);

        Assert.AreEqual(items.Count, drpItems.Count);

        var searchInput = component.Find(".bit-drp-sin");
        searchInput.Input(search);

        var itemCount = string.IsNullOrEmpty(search) ? items.Count : items.Count(i => i.Text.Contains(search, StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual(itemCount, drpItems.Count);

        if (string.IsNullOrEmpty(search) is false)
        {
            searchInput.Input(string.Empty);
            Assert.AreEqual(items.Count, drpItems.Count);
        }
    }

    [DataTestMethod,
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

        // To ensure a consistent display structure in the Virtualize component across .NET 9 and .NET 8,
        // we've set the default value of MaxItemCount to 100. This means that even if a higher value is specified,
        // only a maximum of 100 items will be rendered by default.
        AppContext.SetData("Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize.MaxItemCount", 100);

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
            //When virtualize is true, number of rendered items is greater than number of items show in the list + 2 * overScanCount.
            var expectedRenderedItemCount = Math.Ceiling((decimal)(viewportHeight / component.Instance.ItemSize)) + (2 * component.Instance.OverscanCount);
            expectedRenderedItemCount = Math.Min(expectedRenderedItemCount, 100);

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
    }

    [DataTestMethod,
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

    [DataTestMethod,
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

    [DataTestMethod,
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

    private void HandleValueChanged(string value)
    {
        _bitDropdownValue = value;
    }

    private void HandleValuesChanged(IEnumerable<string> values)
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


    private static ICollection<BitDropdownItem<string>> GetRangeDropdownItems(int count) =>
        Enumerable.Range(1, count).Select(item => new BitDropdownItem<string>
        {
            ItemType = BitDropdownItemType.Normal,
            Value = item.ToString(),
            Text = $"Item {item}"
        }).ToArray();
}
