using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Inputs;

[TestClass]
public class BitDropDownTests : BunitTestContext
{
    private string BitDropDownValue;
    private List<string> BitDropDownValues;

    [DataTestMethod,
      DataRow(true),
      DataRow(false)
    ]
    public void BitDropDownTest(bool isEnabled)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropDown>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var bitDropDown = component.Find(".bit-drp");

        if (isEnabled)
        {
            Assert.IsFalse(bitDropDown.ClassList.Contains("bit-dis"));
        }
        else
        {
            Assert.IsTrue(bitDropDown.ClassList.Contains("bit-dis"));
        }
    }

    [DataTestMethod,
      DataRow(true),
      DataRow(false)
    ]
    public void ResponsiveDropDownShouldTakeCorrectClassNameAndRenderElements(bool isResponsiveModeEnabled)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropDown>(parameters =>
        {
            parameters.Add(p => p.IsResponsiveModeEnabled, isResponsiveModeEnabled);
        });

        var bitDropDown = component.Find(".bit-drp");

        if (isResponsiveModeEnabled)
        {
            Assert.IsTrue(bitDropDown.ClassList.Contains("responsive"));

            var lblContainer = component.Find(".responsive-label-container");
            Assert.IsNotNull(lblContainer);
        }
        else
        {
            Assert.ThrowsException<ElementNotFoundException>(() => component.Find(".responsive-label-container"));
        }
    }

    [DataTestMethod,
      DataRow(null),
      DataRow("BitDrop")
    ]
    public void ResponsiveDropDownShouldRenderLabel(string labelFragment)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropDown>(parameters =>
        {
            parameters.Add(p => p.IsResponsiveModeEnabled, true);
            parameters.Add(p => p.Label, labelFragment);
        });

        if (string.IsNullOrEmpty(labelFragment))
        {
            Assert.ThrowsException<ElementNotFoundException>(() => component.Find(".responsive-label-container > label"));
        }
        else
        {
            Assert.AreEqual(labelFragment, component.Find(".responsive-label-container > label").InnerHtml);
        }
    }

    [DataTestMethod,
      DataRow(null),
      DataRow("<div>This is labelFragment</div>"),
    ]
    public void ResponsiveDropDownShouldRenderLabelFragment(string labelFragment)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropDown>(parameters =>
        {
            parameters.Add(p => p.IsResponsiveModeEnabled, true);

            if (string.IsNullOrEmpty(labelFragment) is false)
            {
                parameters.Add(p => p.LabelTemplate, labelFragment);
            }
        });

        if (string.IsNullOrEmpty(labelFragment))
        {
            Assert.ThrowsException<ElementNotFoundException>(() => component.Find(".responsive-label-container > label"));
        }
        else
        {
            var labelChild = component.Find(".responsive-label-container > label").ChildNodes;
            labelChild.MarkupMatches(labelFragment);
        }
    }

    [DataTestMethod,
      DataRow(true),
      DataRow(false)
    ]
    public void BitDropDownOnClickShouldWorkCorrect(bool isEnabled)
    {
        var clicked = false;
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDropDown>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.OnClick, () => clicked = true);
        });

        var wrapper = component.Find(".wrapper");
        wrapper.Click();

        Assert.AreEqual(isEnabled, clicked);
    }

    [DataTestMethod,
      DataRow(true),
      DataRow(false)
    ]
    public void BitDropDownIsMultiSelectShouldWorkCorrect(bool isMultiSelect)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var isOpen = true;

        var items = GetDropdownItems();
        var component = RenderComponent<BitDropDown>(parameters =>
        {
            parameters.Add(p => p.IsOpen, isOpen);
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.IsMultiSelect, isMultiSelect);
        });

        var bitDropDown = component.Find(".bit-drp");

        if (isMultiSelect)
        {
            Assert.IsTrue(bitDropDown.ClassList.Contains("multi"));
            Assert.AreEqual(items.FindAll(i => i.ItemType == BitDropDownItemType.Normal).Count, component.FindAll(".checkbox-wrapper").Count);
        }
        else
        {
            Assert.IsFalse(bitDropDown.ClassList.Contains("multi"));
            Assert.ThrowsException<ElementNotFoundException>(() => component.Find(".checkbox-wrapper"));
        }
    }

    [DataTestMethod,
      DataRow(true),
      DataRow(false)
    ]
    public void BitDropDownItemsShouldRenderCorrect(bool isMultiSelect)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var isOpen = true;
        var items = GetDropdownItems();
        var component = RenderComponent<BitDropDown>(parameters =>
        {
            parameters.Add(p => p.IsOpen, isOpen);
            parameters.Add(p => p.IsOpenChanged, v => isOpen = v);
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.IsMultiSelect, isMultiSelect);
        });

        Assert.AreEqual(items.FindAll(i => i.ItemType == BitDropDownItemType.Header).Count, component.FindAll(".item-header").Count);
        Assert.AreEqual(items.FindAll(i => i.ItemType == BitDropDownItemType.Divider).Count, component.FindAll(".item-divider").Count);

        if (isMultiSelect)
        {
            Assert.AreEqual(items.FindAll(i => i.ItemType == BitDropDownItemType.Normal).Count, component.FindAll(".checkbox-wrapper").Count);
        }
        else
        {
            Assert.AreEqual(items.FindAll(i => i.ItemType == BitDropDownItemType.Normal).Count, component.FindAll(".item").Count);
        }
    }

    [DataTestMethod,
      DataRow("f-ban"),
      DataRow("f-app")
    ]
    public void BitDropDownTextWithDefaultValueShouldInitCorrect(string defaultValue)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var items = GetDropdownItems();
        var component = RenderComponent<BitDropDown>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.DefaultValue, defaultValue);
        });

        var textSpan = component.Find(".text-container");
        var expectedText = items.Find(i => i.Value == defaultValue && i.ItemType == BitDropDownItemType.Normal).Text;

        Assert.AreEqual(expectedText, textSpan.InnerHtml);
    }

    [DataTestMethod,
      DataRow("f-ban"),
      DataRow("f-app,f-ban")
    ]
    public void BitDropDownTextWithDefaultValuesShouldInitCorrect(string defaultValues)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var items = GetDropdownItems();
        var defaultSelectedMultipleValueList = defaultValues.Split(",").ToList();
        var component = RenderComponent<BitDropDown>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.IsMultiSelect, true);
            parameters.Add(p => p.DefaultValues, defaultSelectedMultipleValueList);
        });

        var textSpan = component.Find(".text-container");
        var defaultSelectedItems = items.FindAll(i => defaultSelectedMultipleValueList.Contains(i.Value) && i.ItemType == BitDropDownItemType.Normal);
        var expectedText = "";

        defaultSelectedItems.ForEach(i =>
        {
            if (i.IsSelected && i.ItemType == BitDropDownItemType.Normal)
            {
                if (expectedText.HasValue())
                {
                    expectedText += component.Instance.MultiSelectDelimiter;
                }

                expectedText += i.Text;
            }
        });

        Assert.AreEqual(expectedText, textSpan.InnerHtml);
    }

    [DataTestMethod,
      DataRow("f-ban", "f-app"),
      DataRow("f-app", null)
    ]
    public void BitDropDownTextWithValueShouldInitCorrect(string value, string defaultValue)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var items = GetDropdownItems();
        var component = RenderComponent<BitDropDown>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.DefaultValue, defaultValue);
            parameters.Add(p => p.Value, value);
        });

        var textSpan = component.Find(".text-container");
        var expectedText = items.Find(i => i.Value == value && i.ItemType == BitDropDownItemType.Normal).Text;

        Assert.AreEqual(expectedText, textSpan.InnerHtml);
    }

    [DataTestMethod,
      DataRow("f-ban", "f-app,f-ban"),
      DataRow("f-app,f-ban", "f-ban")
    ]
    public void BitDropDownTextWithValuesAndDefaultValuesShouldInitCorrect(string defaultValues, string values)
    {
        var items = GetDropdownItems();
        var defaultSelectedMultipleValueList = defaultValues.Split(",").ToList();
        var selectedMultipleValueList = values.Split(",").ToList();
        var component = RenderComponent<BitDropDown>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.IsMultiSelect, true);
            parameters.Add(p => p.DefaultValues, defaultSelectedMultipleValueList);
            parameters.Add(p => p.Values, selectedMultipleValueList);
        });

        var textSpan = component.Find(".text-container");
        var selectedItems = items.FindAll(i => selectedMultipleValueList.Contains(i.Value) && i.ItemType == BitDropDownItemType.Normal);
        var expectedText = new StringBuilder();

        selectedItems.ForEach(i =>
        {
            if (i.IsSelected && i.ItemType == BitDropDownItemType.Normal)
            {
                if (expectedText.Length > 0)
                {
                    expectedText.Append(component.Instance.MultiSelectDelimiter);
                }

                expectedText.Append(i.Text);
            }
        });

        Assert.AreEqual(expectedText.ToString(), textSpan.InnerHtml);
    }

    [DataTestMethod,
      DataRow(null, "f-app,f-ban", true, "Select options"),
      DataRow(null, null, true, "Select options"),
      DataRow("f-ban", null, false, "Select option"),
      DataRow(null, null, false, "Select option")
    ]
    public void BitDropDownPlaceholderShouldWorkCorrect(string value, string values, bool isMultiSelect, string placeholder)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var items = GetRawDropdownItems();
        var selectedMultipleValueList = values is not null ? values.Split(",").ToList() : new List<string>();
        var component = RenderComponent<BitDropDown>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.IsMultiSelect, isMultiSelect);
            parameters.Add(p => p.Values, selectedMultipleValueList);
            parameters.Add(p => p.Value, value);
            parameters.Add(p => p.Placeholder, placeholder);
        });

        var targetSpan = component.Find(".text-container");
        var expectedText = new StringBuilder();

        if (isMultiSelect)
        {
            if (values is not null)
            {
                var selectedItems = items.FindAll(i => selectedMultipleValueList.Contains(i.Value) && i.ItemType == BitDropDownItemType.Normal);
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
    public void BitDropDownLabelShouldWorkCorrect(string label)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropDown>(parameters =>
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
    public void BitDropDownLabelFragmentTest(string labelFragment)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropDown>(parameters =>
        {
            parameters.Add(p => p.LabelTemplate, labelFragment);
        });

        var drpLabelChild = component.Find("label").ChildNodes;
        drpLabelChild.MarkupMatches(labelFragment);
    }

    [DataTestMethod,
        DataRow("Drop Down"),
        DataRow(null)
    ]
    public void BitDropDownTitleTest(string title)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropDown>(parameters =>
        {
            parameters.Add(p => p.Title, title);
        });

        var drpWrapper = component.Find(".wrapper");

        Assert.AreEqual(title, drpWrapper.GetAttribute("title"));
    }

    [DataTestMethod,
        DataRow(true, "f-app"),
        DataRow(false, "f-app"),
    ]
    public void BitDropDownNotifyOnReselectShouldWorkCorrect(bool notifyOnReselect, string defaultValue)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var items = GetRawDropdownItems();
        var itemSelected = false;
        var isOpen = true;

        var component = RenderComponent<BitDropDown>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.IsOpen, isOpen);
            parameters.Add(p => p.IsOpenChanged, v => isOpen = v);
            parameters.Add(p => p.IsEnabled, true);
            parameters.Add(p => p.NotifyOnReselect, notifyOnReselect);
            parameters.Add(p => p.DefaultValue, defaultValue);
            parameters.Add(p => p.OnSelectItem, () => itemSelected = true);
        });

        var selectedItem = component.Find(".selected");
        selectedItem.Click();

        Assert.AreEqual(notifyOnReselect, itemSelected);
    }

    [DataTestMethod,
        DataRow(true, true),
        DataRow(false, true),

        DataRow(true, false),
        DataRow(false, false)
    ]
    public void BitDropDownEnableItemSelectionShouldWorkCorrect(bool itemIsEnabled, bool isMultiSelect)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var itemsSelected = 0;
        var isOpen = true;

        var items = new List<BitDropDownItem>()
        {
            new() { Value = "Apple", Text = "f-app", IsEnabled = itemIsEnabled },
            new() { Value = "Banana", Text = "f-ban", IsEnabled = itemIsEnabled }
        };
        var component = RenderComponent<BitDropDown>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.IsOpen, isOpen);
            parameters.Add(p => p.IsOpenChanged, v => isOpen = v);
            parameters.Add(p => p.IsEnabled, true);
            parameters.Add(p => p.IsMultiSelect, isMultiSelect);
            parameters.Add(p => p.OnSelectItem, () => itemsSelected++);
        });

        if (isMultiSelect)
        {
            var drpItems = component.FindAll(".checkbox-wrapper", true);
            drpItems[0].GetElementsByTagName("label").First().Click();
            drpItems[1].GetElementsByTagName("label").First().Click();
            var expectedResult = itemIsEnabled ? 2 : 0;
            Assert.AreEqual(expectedResult, itemsSelected);
        }
        else
        {
            var drpItems = component.FindAll(".item");
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
    public void BitDropDownTwoWayBoundWithCustomHandlerShouldWorkCorrect(string value)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        BitDropDownValue = value;
        var isOpen = true;

        var items = GetRawDropdownItems();
        var component = RenderComponent<BitDropDown>(parameters =>
        {
            parameters.Add(p => p.IsOpen, isOpen);
            parameters.Add(p => p.IsOpenChanged, v => isOpen = v);
            parameters.Add(p => p.IsEnabled, true);
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Value, BitDropDownValue);
            parameters.Add(p => p.ValueChanged, HandleValueChanged);
        });

        var drpItems = component.FindAll(".item");
        drpItems[3].Click();

        var expectedValue = items[3].Value;

        Assert.AreEqual(expectedValue, BitDropDownValue);
    }

    [DataTestMethod,
        DataRow("f-ban,v-bro"),
        DataRow("f-ora")
    ]
    public void BitDropDownMultiSelectTwoWayBoundWithCustomHandlerShouldWorkCorrect(string values)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var isOpen = true;

        BitDropDownValues = values.Split(",").ToList();
        var initialValuesCount = BitDropDownValues.Count;
        var items = GetRawDropdownItems();
        var component = RenderComponent<BitDropDown>(parameters =>
        {
            parameters.Add(p => p.IsOpen, isOpen);
            parameters.Add(p => p.IsOpenChanged, v => isOpen = v);
            parameters.Add(p => p.IsEnabled, true);
            parameters.Add(p => p.IsMultiSelect, true);
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Values, BitDropDownValues);
            parameters.Add(p => p.ValuesChanged, HandleValuesChanged);
        });

        var drpItems = component.FindAll(".checkbox-wrapper");
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

        Assert.AreEqual(expectedResult, BitDropDownValues.Count);
    }

    [DataTestMethod,
        DataRow("Apple", "f-app"),
        DataRow("Orange", "f-ora"),
        DataRow("Banana", "f-ban"),
        DataRow("Broccoli", "v-bro")
    ]
    public void BitDropDownTwoWayBoundWithForSelectedItem(string text, string value)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        BitDropDownItem? selectedItem = null;
        var isOpen = true;

        var items = GetRawDropdownItems();
        var component = RenderComponent<BitDropDown>(parameters =>
        {
            parameters.Add(p => p.IsOpen, isOpen);
            parameters.Add(p => p.IsOpenChanged, v => isOpen = v);
            parameters.Add(p => p.IsEnabled, true);
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.SelectedItem, selectedItem);
            parameters.Add(p => p.SelectedItemChanged, (value) => selectedItem = value);
        });

        var drpItems = component.FindAll(".item");
        drpItems.Single(i => i.TextContent.Contains(text)).Click();

        Assert.IsNotNull(selectedItem);
        Assert.AreEqual(value, selectedItem.Value);
        Assert.AreEqual(text, selectedItem.Text);
        Assert.IsTrue(selectedItem.IsSelected);
    }

    [DataTestMethod,
        DataRow("Banana,Broccoli", "f-ban,v-bro"),
        DataRow("Orange", "f-ora"),
        DataRow("Orange,Apple,Banana", "f-ora,f-app,f-ban")
    ]
    public void BitDropDownMultiSelectTwoWayBoundForSelectedItems(string text, string value)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        List<BitDropDownItem>? selectedItems = null;
        var isOpen = true;

        var items = GetRawDropdownItems();
        var component = RenderComponent<BitDropDown>(parameters =>
        {
            parameters.Add(p => p.IsOpen, isOpen);
            parameters.Add(p => p.IsOpenChanged, v => isOpen = v);
            parameters.Add(p => p.IsEnabled, true);
            parameters.Add(p => p.IsMultiSelect, true);
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.SelectedItems, selectedItems);
            parameters.Add(p => p.SelectedItemsChanged, v => selectedItems = v);
        });

        var textList = text.Split(",").ToList();
        var drpItems = component.FindAll(".checkbox-wrapper", enableAutoRefresh: true);
        foreach (var txt in textList)
        {
            drpItems.Single(i => i.Children[0].Children[1].TextContent.Contains(txt)).Children[0].Click();
        }

        var valueList = value.Split(",").ToList();

        Assert.IsNotNull(selectedItems);
        Assert.AreEqual(valueList.Count, selectedItems.Count);
        Assert.IsTrue(selectedItems.Select(i => i.Value).OrderBy(o => o).SequenceEqual(valueList.OrderBy(o => o)));
        Assert.IsTrue(selectedItems.Select(i => i.Text).OrderBy(o => o).SequenceEqual(textList.OrderBy(o => o)));
        Assert.IsFalse(selectedItems.Any(i => i.IsSelected is false));
    }

    [DataTestMethod,
        DataRow(null),
        DataRow("f-ora")
    ]
    public void BitDropDownValidationFormTest(string value)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var items = GetRawDropdownItems();
        var component = RenderComponent<BitDropDownValidationTest>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, true);
            parameters.Add(p => p.IsMultiSelect, false);
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.TestModel, new BitDropDownTestModel { Value = value });
        });

        var isValid = value.HasValue();

        var form = component.Find("form");
        form.Submit();

        Assert.AreEqual(component.Instance.ValidCount, isValid ? 1 : 0);
        Assert.AreEqual(component.Instance.InvalidCount, isValid ? 0 : 1);

        if (isValid is false)
        {
            // open dropdown
            var drp = component.Find(".wrapper");
            drp.Click();

            // select item
            var drpItems = component.FindAll(".item");
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
    public void BitDropDownMultiSelectValidationFormTest(string values)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        BitDropDownValues = values.HasValue() ? values.Split(",").ToList() : null;
        var items = GetRawDropdownItems();
        var component = RenderComponent<BitDropDownMultiSelectValidationTest>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, true);
            parameters.Add(p => p.IsMultiSelect, true);
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.TestModel, new BitDropDownMultiSelectTestModel { Values = BitDropDownValues });
        });

        var isValid = (BitDropDownValues?.Count ?? 0) == 2;

        var form = component.Find("form");
        form.Submit();

        Assert.AreEqual(isValid ? 1 : 0, component.Instance.ValidCount);
        Assert.AreEqual(isValid ? 0 : 1, component.Instance.InvalidCount);

        if (isValid is false)
        {
            // open dropdown
            var drp = component.Find(".wrapper");
            drp.Click();

            // select items
            //var drpItemFirst = component.Find(".checkbox-wrapper:first-child");
            //drpItemFirst.Children[0].Click();

            //var drpItemLast = component.Find(".checkbox-wrapperb:last-child");
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
    public void BitDropDownValidationInvalidHtmlAttributeTest(string value)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var items = GetRawDropdownItems();
        var component = RenderComponent<BitDropDownValidationTest>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, true);
            parameters.Add(p => p.IsMultiSelect, false);
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.TestModel, new BitDropDownTestModel { Value = value });
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
            var drp = component.Find(".wrapper");
            drp.Click();

            // select item
            var drpItems = component.FindAll(".item");
            drpItems[0].Click();

            Assert.IsFalse(selectTag.HasAttribute("aria-invalid"));
        }
    }

    [DataTestMethod,
        DataRow(null),
        DataRow("f-ban,v-bro"),
        DataRow("f-ora")
    ]
    public void BitDropDownMultiSelectValidationInvalidHtmlAttributeTest(string values)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        BitDropDownValues = values.HasValue() ? values.Split(",").ToList() : null;
        var items = GetRawDropdownItems();
        var component = RenderComponent<BitDropDownMultiSelectValidationTest>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, true);
            parameters.Add(p => p.IsMultiSelect, true);
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.TestModel, new BitDropDownMultiSelectTestModel { Values = BitDropDownValues });
        });

        var isInvalid = (BitDropDownValues?.Count ?? 0) != 2;

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
            var drp = component.Find(".wrapper");
            drp.Click();

            // select items
            //var drpItemFirst = component.Find(".checkbox-wrapper:first-child");
            //drpItemFirst.Children[0].Click();

            //var drpItemLast = component.Find(".checkbox-wrapper:last-child");
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
    public void BitDropDownValidationInvalidCssClassTest(string value)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var items = GetRawDropdownItems();
        var component = RenderComponent<BitDropDownValidationTest>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, true);
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.TestModel, new BitDropDownTestModel { Value = value });
        });

        var isInvalid = value.HasNoValue();

        var bitDropDown = component.Find(".bit-drp");

        Assert.IsFalse(bitDropDown.ClassList.Contains("bit-inv"));

        var form = component.Find("form");
        form.Submit();

        Assert.AreEqual(bitDropDown.ClassList.Contains("bit-inv"), isInvalid);

        if (isInvalid)
        {
            // open dropdown
            var drp = component.Find(".wrapper");
            drp.Click();

            // select item
            var drpItems = component.FindAll(".item");
            drpItems[0].Click();
        }

        Assert.IsFalse(bitDropDown.ClassList.Contains("bit-inv"));
    }

    [DataTestMethod,
        DataRow(true, null),
        DataRow(true, "Search items"),
        DataRow(false, null),
        DataRow(false, "Search items")
    ]
    public void BitDropDownShowSearchBoxTest(bool showSearchBox, string searchBoxPlaceholder)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var items = GetRawDropdownItems();
        var component = RenderComponent<BitDropDown>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, true);
            parameters.Add(p => p.ShowSearchBox, showSearchBox);
            parameters.Add(p => p.SearchBoxPlaceholder, searchBoxPlaceholder);
            parameters.Add(p => p.Items, items);
        });

        var bitDropDown = component.Find(".wrapper");
        bitDropDown.Click();

        var searchBox = component.FindAll(".items-wrapper .search-box");
        if (showSearchBox)
        {
            Assert.AreEqual(1, searchBox.Count);

            var searchInput = component.Find(".search-input");
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
    public void BitDropDownSearchItemTest(string search, bool isMultiSelect)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var items = GetRawDropdownItems();
        var component = RenderComponent<BitDropDown>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, true);
            parameters.Add(p => p.ShowSearchBox, true);
            parameters.Add(p => p.IsMultiSelect, isMultiSelect);
            parameters.Add(p => p.Items, items);
        });

        var bitDropDown = component.Find(".wrapper");
        bitDropDown.Click();

        var drpItems = component.FindAll(isMultiSelect ? ".checkbox-wrapper" : ".item", true);

        Assert.AreEqual(items.Count, drpItems.Count);

        var searchInput = component.Find(".search-input");
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
    public void BitDropDownVirtualizeTest(bool virtualize, int? itemSize, int? overscanCount, bool isMultiSelect)
    {
        //https://bunit.dev/docs/test-doubles/emulating-ijsruntime.html#-jsinterop-emulation
        const double viewportHeight = 1_000_000_000;
        var items = GetRawDropdownItems(500);
        var component = RenderComponent<BitDropDown>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, true);
            parameters.Add(p => p.Virtualize, virtualize);
            parameters.Add(p => p.IsMultiSelect, isMultiSelect);
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

        var bitDropDown = component.Find(".wrapper");
        bitDropDown.Click();

        var drpItems = component.FindAll(isMultiSelect ? ".checkbox-wrapper" : ".item");
        var actualRenderedItemCount = drpItems.Count;

        if (virtualize)
        {
            //When virtualize is true, number of rendered items is greater than number of items show in the list + 2 * overScanCount.
            var expectedRenderedItemCount = Math.Ceiling((decimal)(viewportHeight / component.Instance.ItemSize)) + (2 * component.Instance.OverscanCount);

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
        DataRow(BitIconName.WindowsLogo),
        DataRow(BitIconName.ChevronUp),
        DataRow(null)
    ]
    public void BitDropDownCaretDownIconNameTest(BitIconName? iconName)
    {
        var component = RenderComponent<BitDropDown>(parameters =>
        {
            if (iconName.HasValue)
            {
                parameters.Add(p => p.CaretDownIconName, iconName.Value);
            }
        });

        if (iconName.HasValue)
        {
            Assert.IsTrue(component.Find(".wrapper > .icon-container > i").ClassList.Contains($"bit-icon--{iconName.GetDisplayName()}"));
        }
        else
        {
            Assert.IsTrue(component.Find(".wrapper > .icon-container > i").ClassList.Contains($"bit-icon--{BitIconName.ChevronDown.GetDisplayName()}"));
        }
    }

    [DataTestMethod,
        DataRow("<i>This is CaretDownFragment</div>"),
        DataRow(null)
    ]
    public void BitDropDownCaretDownFragmentTest(string iconFragment)
    {
        var component = RenderComponent<BitDropDown>(parameters =>
        {
            if (string.IsNullOrEmpty(iconFragment) is false)
            {
                parameters.Add(p => p.CaretDownTemplate, iconFragment);
            }
        });

        if (string.IsNullOrEmpty(iconFragment))
        {
            Assert.IsTrue(component.Find(".wrapper > .icon-container > i").ClassList.Contains($"bit-icon--{BitIconName.ChevronDown.GetDisplayName()}"));
        }
        else
        {
            var drpCaretDownChild = component.Find(".wrapper > .icon-container").ChildNodes;
            drpCaretDownChild.MarkupMatches(iconFragment);
        }
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitDropDownIsRtlTest(bool isRtl)
    {
        var component = RenderComponent<BitDropDown>(parameters =>
        {
            parameters.Add(p => p.IsRtl, isRtl);
        });

        var bitDrp = component.Find(".bit-drp");

        if (isRtl)
        {
            Assert.IsTrue(bitDrp.ClassList.Contains("rtl"));
        }
        else
        {
            Assert.IsFalse(bitDrp.ClassList.Contains("rtl"));
        }
    }

    private void HandleValueChanged(string value)
    {
        BitDropDownValue = value;
    }

    private void HandleValuesChanged(List<string> values)
    {
        BitDropDownValues = values;
    }

    private List<BitDropDownItem> GetDropdownItems()
    {
        return new()
        {
            new()
            {
                ItemType = BitDropDownItemType.Header,
                Text = "Fruits"
            },
            new()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Apple",
                Value = "f-app"
            },
            new()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Orange",
                Value = "f-ora",
            },
            new()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Banana",
                Value = "f-ban",
            },
            new()
            {
                ItemType = BitDropDownItemType.Divider,
            },
            new()
            {
                ItemType = BitDropDownItemType.Header,
                Text = "Vegetables"
            },
            new()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Broccoli",
                Value = "v-bro",
            }
        };
    }

    private List<BitDropDownItem> GetRawDropdownItems()
    {
        return new()
        {
            new()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Apple",
                Value = "f-app"
            },
            new()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Orange",
                Value = "f-ora"
            },
            new()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Banana",
                Value = "f-ban"
            },
            new()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Broccoli",
                Value = "v-bro"
            }
        };
    }

    private List<BitDropDownItem> GetRawDropdownItems(int count)
    {
        return Enumerable.Range(1, count).Select(item => new BitDropDownItem
        {
            ItemType = BitDropDownItemType.Normal,
            Value = item.ToString(),
            Text = $"Item {item}"
        }).ToList();
    }
}
