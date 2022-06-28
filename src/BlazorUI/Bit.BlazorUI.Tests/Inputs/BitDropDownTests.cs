using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Inputs
{
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

            var component = RenderComponent<BitDropDownTest>(parameters =>
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
          DataRow(true),
          DataRow(false)
        ]
        public void BitDropDownOnClickShouldWorkCorrect(bool isEnabled)
        {
            Context.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = RenderComponent<BitDropDownTest>(parameters =>
            {
                parameters.Add(p => p.IsEnabled, isEnabled);
            });

            var bitDrpWrapper = component.Find(".bit-drp-wrapper");
            bitDrpWrapper.Click();
            var expectedResult = isEnabled ? 1 : 0;

            Assert.AreEqual(expectedResult, component.Instance.Counter);
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
            var component = RenderComponent<BitDropDownTest>(parameters =>
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
            var component = RenderComponent<BitDropDownTest>(parameters =>
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
                Assert.AreEqual(items.FindAll(i => i.ItemType == BitDropDownItemType.Normal).Count, component.FindAll("button").Count);
            }
        }

        [DataTestMethod,
          DataRow(true, "-"),
          DataRow(false, null)
        ]
        public void BitDropDownTextWithSelectedItemsShouldInitCorrect(bool isMultiSelect, string multiSelectDelimiter)
        {
            Context.JSInterop.Mode = JSRuntimeMode.Loose;

            var items = GetDropdownItems();
            var component = RenderComponent<BitDropDownTest>(parameters =>
            {
                parameters.Add(p => p.Items, items);
                parameters.Add(p => p.IsMultiSelect, isMultiSelect);
                parameters.Add(p => p.MultiSelectDelimiter, multiSelectDelimiter);
            });

            var textSpan = component.Find(".bit-drp-wrapper-txt");
            var expectedText = "";

            if (isMultiSelect)
            {
                items.ForEach(i =>
                {
                    if (i.IsSelected && i.ItemType == BitDropDownItemType.Normal)
                    {
                        if (expectedText.HasValue())
                        {
                            expectedText += multiSelectDelimiter;
                        }

                        expectedText += i.Text;
                    }
                });
            }
            else
            {
                var firstSelectedItem = items.FirstOrDefault(i => i.IsSelected);
                expectedText = firstSelectedItem is null ? "" : firstSelectedItem.Text;
            }

            Assert.AreEqual(expectedText, textSpan.InnerHtml);
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

            var component = RenderComponent<BitDropDownTest>(parameters =>
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

            var component = RenderComponent<BitDropDownTest>(parameters =>
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
            var component = RenderComponent<BitDropDownTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.Items, items);
                parameters.Add(p => p.IsOpen, true);
                parameters.Add(p => p.IsEnabled, true);
                parameters.Add(p => p.NotifyOnReselect, notifyOnReselect);
                parameters.Add(p => p.DefaultValue, defaultValue);
            });

            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";
            var selectedItem = component.Find($".bit-drp-slc-{visualClass}");
            selectedItem.Click();

            int expectedResult;
            if (notifyOnReselect)
            {
                expectedResult = 1;
            }
            else
            {
                expectedResult = 0;
            }

            Assert.AreEqual(expectedResult, component.Instance.SelectItemCounter);
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

            var items = new List<BitDropDownItem>()
            {
                new BitDropDownItem() {Value = "Apple", Text = "f-app", IsEnabled = itemIsEnabled },
                new BitDropDownItem() {Value = "Banana", Text = "f-ban", IsEnabled = itemIsEnabled }
            };
            var component = RenderComponent<BitDropDownTest>(parameters =>
            {
                parameters.Add(p => p.Items, items);
                parameters.Add(p => p.IsOpen, true);
                parameters.Add(p => p.IsEnabled, true);
                parameters.Add(p => p.IsMultiSelect, isMultiSelect);
            });

            if (isMultiSelect)
            {
                var drpItems = component.FindAll(".bit-drp-chb", true);
                drpItems[0].GetElementsByTagName("label").First().Click();
                drpItems[1].GetElementsByTagName("label").First().Click();
                var expectedResult = itemIsEnabled ? 2 : 0;
                Assert.AreEqual(expectedResult, component.Instance.SelectItemCounter);
            }
            else
            {
                var drpItems = component.FindAll("button");
                drpItems[0].Click();
                var expectedResult = itemIsEnabled ? 1 : 0;
                Assert.AreEqual(expectedResult, component.Instance.SelectItemCounter);
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

            var drpItems = component.FindAll("button");
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
                var drpItems = component.FindAll(".bit-drp-items-wrapper button");
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
                var drpItemFirst = component.Find(".bit-drp-chb:first-child");
                drpItemFirst.Children[0].Click();

                var drpItemLast = component.Find(".bit-drp-chb:last-child");
                drpItemLast.Children[0].Click();

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
                var drpItems = component.FindAll(".bit-drp-items-wrapper button");
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
                var drpItemFirst = component.Find(".bit-drp-chb:first-child");
                drpItemFirst.Children[0].Click();

                var drpItemLast = component.Find(".bit-drp-chb:last-child");
                drpItemLast.Children[0].Click();

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
                var drpItems = component.FindAll(".bit-drp-items-wrapper button");
                drpItems.First().Click();
            }

            Assert.IsFalse(bitDropDown.ClassList.Contains($"bit-drp-invalid-{visualClass}"));
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
                IsEnabled = false
            });
            items.Add(new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Banana",
                Value = "f-ban",
                IsSelected = true
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
                IsSelected = true
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
    }
}
