using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Inputs;

[TestClass]
public class BitDropDownTests : BunitTestContext
{
    private string BitDropDownValue;
    private List<string> BitDropDownValues;

    [DataTestMethod,
      DataRow(Visual.Fluent, true),
      DataRow(Visual.Fluent, false),

      DataRow(Visual.Cupertino, true),
      DataRow(Visual.Cupertino, false),

      DataRow(Visual.Material, true),
      DataRow(Visual.Material, false)
    ]
    public void BitDropDownShouldTakeCorrectVisual(Visual visual, bool isEnabled)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropDown>(parameters =>
        {
            parameters.Add(p => p.Visual, visual);
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var bitDrp = component.Find(".bit-drp");

        var enabledClass = isEnabled ? "enabled" : "disabled";
        var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

        Assert.IsTrue(bitDrp.ClassList.Contains($"bit-drp-{enabledClass}-{visualClass}"));
    }

    [DataTestMethod,
      DataRow(Visual.Fluent, true),
      DataRow(Visual.Fluent, false),

      DataRow(Visual.Cupertino, true),
      DataRow(Visual.Cupertino, false),

      DataRow(Visual.Material, true),
      DataRow(Visual.Material, false)
    ]
    public void ResponsiveDropDownShouldTakeCorrectClassName(Visual visual, bool isResponsiveModeEnabled)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropDown>(parameters =>
        {
            parameters.Add(p => p.Visual, visual);
            parameters.Add(p => p.IsResponsiveModeEnabled, isResponsiveModeEnabled);
        });

        var bitDrp = component.Find(".bit-drp");

        if (isResponsiveModeEnabled)
        {
            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

            Assert.IsTrue(bitDrp.ClassList.Contains($"bit-drp-responsive-{visualClass}"));
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

        var bitDrpWrapper = component.Find(".bit-drp-wrapper");
        bitDrpWrapper.Click();

        Assert.AreEqual(isEnabled, clicked);
    }

    [DataTestMethod,
      DataRow(Visual.Fluent, true),
      DataRow(Visual.Fluent, false),

      DataRow(Visual.Cupertino, true),
      DataRow(Visual.Cupertino, false),

      DataRow(Visual.Material, true),
      DataRow(Visual.Material, false)
    ]
    public void BitDropDownIsMultiSelectShouldWorkCorrect(Visual visual, bool isMultiSelect)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var items = GetDropdownItems();
        var component = RenderComponent<BitDropDown>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Visual, visual);
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.IsMultiSelect, isMultiSelect);
        });

        var bitDrp = component.Find(".bit-drp");
        var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";
        var multiSelectCalss = isMultiSelect ? "multi-" : null;

        if (isMultiSelect)
        {
            Assert.IsTrue(bitDrp.ClassList.Contains($"bit-drp-{multiSelectCalss}{visualClass}"));
            Assert.AreEqual(items.FindAll(i => i.ItemType == BitDropDownItemType.Normal).Count, component.FindAll(".bit-drp-chb").Count);
        }
        else
        {
            Assert.ThrowsException<ElementNotFoundException>(() => component.Find(".bit-drp-chb"));
        }
    }

    [DataTestMethod,
      DataRow(Visual.Fluent, true),
      DataRow(Visual.Fluent, false),

      DataRow(Visual.Cupertino, true),
      DataRow(Visual.Cupertino, false),

      DataRow(Visual.Material, true),
      DataRow(Visual.Material, false)
    ]
    public void BitDropDownItemsShouldRenderCorrect(Visual visual, bool isMultiSelect)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var items = GetDropdownItems();
        var component = RenderComponent<BitDropDown>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.IsMultiSelect, isMultiSelect);
            parameters.Add(p => p.Visual, visual);
        });

        var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

        Assert.AreEqual(items.FindAll(i => i.ItemType == BitDropDownItemType.Header).Count, component.FindAll($".bit-drp-head-{visualClass}").Count);
        Assert.AreEqual(items.FindAll(i => i.ItemType == BitDropDownItemType.Divider).Count, component.FindAll($".bit-drp-div-{visualClass}").Count);

        if (isMultiSelect)
        {
            Assert.AreEqual(items.FindAll(i => i.ItemType == BitDropDownItemType.Normal).Count, component.FindAll(".bit-drp-chb").Count);
        }
        else
        {
            Assert.AreEqual(items.FindAll(i => i.ItemType == BitDropDownItemType.Normal).Count, component.FindAll(".bit-drp-item").Count);
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

        var textSpan = component.Find(".bit-drp-wrapper-txt");
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

        var textSpan = component.Find(".bit-drp-wrapper-txt");
        var defaultSelectedItems = items.FindAll(i => defaultSelectedMultipleValueList.Contains(i.Value) && i.ItemType == BitDropDownItemType.Normal);
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

        var textSpan = component.Find(".bit-drp-wrapper-txt");
        var expectedText = items.Find(i => i.Value == value && i.ItemType == BitDropDownItemType.Normal).Text;

        Assert.AreEqual(expectedText, textSpan.InnerHtml);
    }

    [DataTestMethod,
      DataRow("f-ban", "f-app,f-ban"),
      DataRow("f-app,f-ban", "f-ban")
    ]
    public void BitDropDownTextWithDefaultValuesShouldInitCorrect(string defaultValues, string values)
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

        var textSpan = component.Find(".bit-drp-wrapper-txt");
        var selectedItems = items.FindAll(i => selectedMultipleValueList.Contains(i.Value) && i.ItemType == BitDropDownItemType.Normal);
        var expectedText = "";

        selectedItems.ForEach(i =>
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

        var targetSpan = component.Find(".bit-drp-wrapper-txt");
        var expectedText = "";

        if (isMultiSelect)
        {
            if (values is not null)
            {
                var selectedItems = items.FindAll(i => selectedMultipleValueList.Contains(i.Value) && i.ItemType == BitDropDownItemType.Normal);
                selectedItems.ForEach(item =>
                {
                    if (expectedText.HasValue())
                    {
                        expectedText += component.Instance.MultiSelectDelimiter;
                    }

                    expectedText += item.Text;
                });
            }
            else
            {
                expectedText = placeholder;
            }
        }
        else
        {
            if (value is not null)
            {
                expectedText = items.Find(i => i.Value == value).Text;
            }
            else
            {
                expectedText = placeholder;
            }
        }

        Assert.AreEqual(expectedText, targetSpan.InnerHtml);
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
            parameters.Add(p => p.LabelFragment, labelFragment);
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

        var drpWrapper = component.Find(".bit-drp-wrapper");

        Assert.AreEqual(title, drpWrapper.GetAttribute("title"));
    }

    [DataTestMethod,
        DataRow(Visual.Fluent, true, "f-app"),
        DataRow(Visual.Fluent, false, "f-app"),

        DataRow(Visual.Cupertino, true, "f-app"),
        DataRow(Visual.Cupertino, false, "f-app"),

        DataRow(Visual.Material, true, "f-app"),
        DataRow(Visual.Material, false, "f-app"),
    ]
    public void BitDropDownNotifyOnReselectShouldWorkCorrect(Visual visual, bool notifyOnReselect, string defaultValue)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var items = GetRawDropdownItems();
        var itemSelecetd = false;
        var component = RenderComponent<BitDropDown>(parameters =>
        {
            parameters.Add(p => p.Visual, visual);
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.IsEnabled, true);
            parameters.Add(p => p.NotifyOnReselect, notifyOnReselect);
            parameters.Add(p => p.DefaultValue, defaultValue);
            parameters.Add(p => p.OnSelectItem, () => itemSelecetd = true);
        });

        var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";
        var selectedItem = component.Find($".bit-drp-slc-{visualClass}");
        selectedItem.Click();

        Assert.AreEqual(notifyOnReselect, itemSelecetd);
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

        var items = new List<BitDropDownItem>()
        {
            new() {Value = "Apple", Text = "f-app", IsEnabled = itemIsEnabled },
            new() {Value = "Banana", Text = "f-ban", IsEnabled = itemIsEnabled }
        };
        var component = RenderComponent<BitDropDown>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.IsEnabled, true);
            parameters.Add(p => p.IsMultiSelect, isMultiSelect);
            parameters.Add(p => p.OnSelectItem, () => itemsSelected++);
        });

        if (isMultiSelect)
        {
            var drpItems = component.FindAll(".bit-drp-chb", true);
            drpItems[0].GetElementsByTagName("label").First().Click();
            drpItems[1].GetElementsByTagName("label").First().Click();
            var expectedResult = itemIsEnabled ? 2 : 0;
            Assert.AreEqual(expectedResult, itemsSelected);
        }
        else
        {
            var drpItems = component.FindAll(".bit-drp-item");
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

        var items = GetRawDropdownItems();
        var component = RenderComponent<BitDropDown>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.IsEnabled, true);
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Value, BitDropDownValue);
            parameters.Add(p => p.ValueChanged, HandleValueChanged);
        });

        var drpItems = component.FindAll(".bit-drp-item");
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

        BitDropDownValues = values.Split(",").ToList();
        var initialValuesCount = BitDropDownValues.Count;
        var items = GetRawDropdownItems();
        var component = RenderComponent<BitDropDown>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.IsEnabled, true);
            parameters.Add(p => p.IsMultiSelect, true);
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Values, BitDropDownValues);
            parameters.Add(p => p.ValuesChanged, HandleValuesChanged);
        });

        var drpItems = component.FindAll(".bit-drp-chb");
        drpItems[3].Children[0].Children[0].Click();

        int expectedResult;
        if (values.Contains(items[3].Value))
        {
            expectedResult = --initialValuesCount;
        }
        else
        {
            expectedResult = ++initialValuesCount;
        }

        Assert.AreEqual(expectedResult, BitDropDownValues.Count);
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
            var drp = component.Find(".bit-drp-wrapper");
            drp.Click();

            // select item
            var drpItems = component.FindAll(".bit-drp-item");
            drpItems.First().Click();

            form.Submit();

            Assert.AreEqual(component.Instance.ValidCount, 1);
            Assert.AreEqual(component.Instance.InvalidCount, 1);
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

        Assert.AreEqual(component.Instance.ValidCount, isValid ? 1 : 0);
        Assert.AreEqual(component.Instance.InvalidCount, isValid ? 0 : 1);

        if (isValid is false)
        {
            // open dropdown
            var drp = component.Find(".bit-drp-wrapper");
            drp.Click();

            // select items
            //var drpItemFirst = component.Find(".bit-drp-chb:first-child");
            //drpItemFirst.Children[0].Click();

            //var drpItemLast = component.Find(".bit-drp-chb:last-child");
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

        Assert.AreEqual(selectTag.HasAttribute("aria-invalid"), isInvalid);
        if (selectTag.HasAttribute("aria-invalid"))
        {
            Assert.AreEqual(selectTag.GetAttribute("aria-invalid"), "true");
        }

        if (isInvalid)
        {
            // open dropdown
            var drp = component.Find(".bit-drp-wrapper");
            drp.Click();

            // select item
            var drpItems = component.FindAll(".bit-drp-item");
            drpItems.First().Click();

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
            Assert.AreEqual(selectTag.GetAttribute("aria-invalid"), "true");
        }

        if (isInvalid)
        {
            // open dropdown
            var drp = component.Find(".bit-drp-wrapper");
            drp.Click();

            // select items
            //var drpItemFirst = component.Find(".bit-drp-chb:first-child");
            //drpItemFirst.Children[0].Click();

            //var drpItemLast = component.Find(".bit-drp-chb:last-child");
            //drpItemLast.Children[0].Click();

            form.Submit();

            //TODO: bypassed - BUnit 2-way bound parameters issue
            //Assert.IsFalse(selectTag.HasAttribute("aria-invalid"));
        }
    }

    [DataTestMethod,
        DataRow(Visual.Fluent, null),
        DataRow(Visual.Fluent, "f-ora"),
        DataRow(Visual.Cupertino, null),
        DataRow(Visual.Cupertino, "f-ora"),
        DataRow(Visual.Material, null),
        DataRow(Visual.Material, "f-ora")
    ]
    public void BitDropDownValidationInvalidCssClassTest(Visual visual, string value)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var items = GetRawDropdownItems();
        var component = RenderComponent<BitDropDownValidationTest>(parameters =>
        {
            parameters.Add(p => p.Visual, visual);
            parameters.Add(p => p.IsEnabled, true);
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.TestModel, new BitDropDownTestModel { Value = value });
        });

        var isInvalid = value.HasNoValue();

        var bitDropDown = component.Find(".bit-drp");
        var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

        Assert.IsFalse(bitDropDown.ClassList.Contains($"bit-drp-invalid-{visualClass}"));

        var form = component.Find("form");
        form.Submit();

        Assert.AreEqual(bitDropDown.ClassList.Contains($"bit-drp-invalid-{visualClass}"), isInvalid);

        if (isInvalid)
        {
            // open dropdown
            var drp = component.Find(".bit-drp-wrapper");
            drp.Click();

            // select item
            var drpItems = component.FindAll(".bit-drp-item");
            drpItems.First().Click();
        }

        Assert.IsFalse(bitDropDown.ClassList.Contains($"bit-drp-invalid-{visualClass}"));
    }

    [DataTestMethod,
        DataRow(Visual.Fluent, true, null),
        DataRow(Visual.Fluent, true, "Search items"),
        DataRow(Visual.Fluent, false, null),
        DataRow(Visual.Fluent, false, "Search items"),

        DataRow(Visual.Cupertino, true, null),
        DataRow(Visual.Cupertino, true, "Search items"),
        DataRow(Visual.Cupertino, false, null),
        DataRow(Visual.Cupertino, false, "Search items"),

        DataRow(Visual.Material, true, null),
        DataRow(Visual.Material, true, "Search items"),
        DataRow(Visual.Material, false, null),
        DataRow(Visual.Material, false, "Search items")
    ]
    public void BitDropDownShowSearchBoxTest(Visual visual, bool showSearchBox, string searchBoxPlaceholder)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var items = GetRawDropdownItems();
        var component = RenderComponent<BitDropDown>(parameters =>
        {
            parameters.AddCascadingValue(visual);
            parameters.Add(p => p.IsEnabled, true);
            parameters.Add(p => p.ShowSearchBox, showSearchBox);
            parameters.Add(p => p.SearchBoxPlaceholder, searchBoxPlaceholder);
            parameters.Add(p => p.Items, items);
        });

        var bitDropDown = component.Find(".bit-drp-wrapper");
        bitDropDown.Click();

        var searchBox = component.FindAll(".bit-drp-items-wrapper .search-box");
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
        DataRow(Visual.Fluent, null, false),
        DataRow(Visual.Fluent, "app", false),
        DataRow(Visual.Fluent, null, true),
        DataRow(Visual.Fluent, "app", true),

        DataRow(Visual.Cupertino, null, false),
        DataRow(Visual.Cupertino, "app", false),
        DataRow(Visual.Cupertino, null, true),
        DataRow(Visual.Cupertino, "app", true),

        DataRow(Visual.Material, null, false),
        DataRow(Visual.Material, "app", false),
        DataRow(Visual.Material, null, true),
        DataRow(Visual.Material, "app", true),
    ]
    public void BitDropDownSearchItemTest(Visual visual, string search, bool isMultiSelect)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var items = GetRawDropdownItems();
        var component = RenderComponent<BitDropDown>(parameters =>
        {
            parameters.AddCascadingValue(visual);
            parameters.Add(p => p.IsEnabled, true);
            parameters.Add(p => p.ShowSearchBox, true);
            parameters.Add(p => p.IsMultiSelect, isMultiSelect);
            parameters.Add(p => p.Items, items);
        });

        var bitDropDown = component.Find(".bit-drp-wrapper");
        bitDropDown.Click();

        var drpItems = component.FindAll(isMultiSelect ? ".bit-drp-chb" : ".bit-drp-item", true);

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
        DataRow(Visual.Fluent, false, null, null, false),
        DataRow(Visual.Fluent, false, 3_000_000, null, false),
        DataRow(Visual.Fluent, false, null, 4, false),
        DataRow(Visual.Fluent, false, 3_000_000, 4, false),

        DataRow(Visual.Fluent, true, null, null, false),
        DataRow(Visual.Fluent, true, 3_000_000, null, false),
        DataRow(Visual.Fluent, true, null, 4, false),
        DataRow(Visual.Fluent, true, 3_000_000, 4, false),

        DataRow(Visual.Fluent, false, null, null, true),
        DataRow(Visual.Fluent, false, 3_000_000, null, true),
        DataRow(Visual.Fluent, false, null, 4, true),
        DataRow(Visual.Fluent, false, 3_000_000, 4, true),

        DataRow(Visual.Fluent, true, null, null, true),
        DataRow(Visual.Fluent, true, 3_000_000, null, true),
        DataRow(Visual.Fluent, true, null, 4, true),
        DataRow(Visual.Fluent, true, 3_000_000, 4, true),

        DataRow(Visual.Cupertino, false, null, null, false),
        DataRow(Visual.Cupertino, false, 3_000_000, null, false),
        DataRow(Visual.Cupertino, false, null, 4, false),
        DataRow(Visual.Cupertino, false, 3_000_000, 4, false),

        DataRow(Visual.Cupertino, true, null, null, false),
        DataRow(Visual.Cupertino, true, 3_000_000, null, false),
        DataRow(Visual.Cupertino, true, null, 4, false),
        DataRow(Visual.Cupertino, true, 3_000_000, 4, false),

        DataRow(Visual.Cupertino, false, null, null, true),
        DataRow(Visual.Cupertino, false, 3_000_000, null, true),
        DataRow(Visual.Cupertino, false, null, 4, true),
        DataRow(Visual.Cupertino, false, 3_000_000, 4, true),

        DataRow(Visual.Cupertino, true, null, null, true),
        DataRow(Visual.Cupertino, true, 3_000_000, null, true),
        DataRow(Visual.Cupertino, true, null, 4, true),
        DataRow(Visual.Cupertino, true, 3_000_000, 4, true),

        DataRow(Visual.Material, false, null, null, false),
        DataRow(Visual.Material, false, 3_000_000, null, false),
        DataRow(Visual.Material, false, null, 4, false),
        DataRow(Visual.Material, false, 3_000_000, 4, false),

        DataRow(Visual.Material, true, null, null, false),
        DataRow(Visual.Material, true, 3_000_000, null, false),
        DataRow(Visual.Material, true, null, 4, false),
        DataRow(Visual.Material, true, 3_000_000, 4, false),

        DataRow(Visual.Material, false, null, null, true),
        DataRow(Visual.Material, false, 3_000_000, null, true),
        DataRow(Visual.Material, false, null, 4, true),
        DataRow(Visual.Material, false, 3_000_000, 4, true),

        DataRow(Visual.Material, true, null, null, true),
        DataRow(Visual.Material, true, 3_000_000, null, true),
        DataRow(Visual.Material, true, null, 4, true),
        DataRow(Visual.Material, true, 3_000_000, 4, true),
    ]
    public void BitDropDownVirtualizeTest(Visual visual, bool virtualize, int? itemSize, int? overscanCount, bool isMultiSelect)
    {
        //https://bunit.dev/docs/test-doubles/emulating-ijsruntime.html#-jsinterop-emulation
        const double viewportHeight = 1_000_000_000;
        var items = GetRawDropdownItems(500);
        var component = RenderComponent<BitDropDown>(parameters =>
        {
            parameters.AddCascadingValue(visual);
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

        var bitDropDown = component.Find(".bit-drp-wrapper");
        bitDropDown.Click();

        var drpItems = component.FindAll(isMultiSelect ? ".bit-drp-chb" : ".bit-drp-item");
        var actualRenderedItemCount = drpItems.Count;

        if (virtualize)
        {
            //When virtualize is true, number of rendered items is greater than number of items showm in the list + 2 * overScanCount.
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
        List<BitDropDownItem> items = new();
        items.Add(new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Header,
            Text = "Fruits"
        });
        items.Add(new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = "Apple",
            Value = "f-app"
        });
        items.Add(new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = "Orange",
            Value = "f-ora",
        });
        items.Add(new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = "Banana",
            Value = "f-ban",
        });
        items.Add(new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Divider,
        });
        items.Add(new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Header,
            Text = "Vegetables"
        });
        items.Add(new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = "Broccoli",
            Value = "v-bro",
        });

        return items;
    }

    private List<BitDropDownItem> GetRawDropdownItems()
    {
        List<BitDropDownItem> items = new();
        items.Add(new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = "Apple",
            Value = "f-app"
        });
        items.Add(new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = "Orange",
            Value = "f-ora"
        });
        items.Add(new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = "Banana",
            Value = "f-ban"
        });
        items.Add(new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = "Broccoli",
            Value = "v-bro"
        });

        return items;
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
