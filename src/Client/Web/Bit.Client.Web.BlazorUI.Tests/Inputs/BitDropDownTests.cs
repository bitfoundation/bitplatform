using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Client.Web.BlazorUI.Tests.Inputs
{
    [TestClass]
    public class BitDropDownTests : BunitTestContext
    {
        private string BitDropDownSelectedKey;
        private List<string> BitDropDownSelectedMultipleKeys;

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
                Assert.AreEqual(items.FindAll(i => i.ItemType == BitDropDownItemType.Normal).Count, component.FindAll(".bit-chb").Count);
            }
            else
            {
                Assert.ThrowsException<ElementNotFoundException>(() => component.Find(".bit-chb"));
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
                Assert.AreEqual(items.FindAll(i => i.ItemType == BitDropDownItemType.Normal).Count, component.FindAll(".bit-chb").Count);
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
        public void BitDropDownTextWithDefaultSelectedKeyShouldInitCorrect(string defaultSelectedKey)
        {
            var items = GetDropdownItems();
            var component = RenderComponent<BitDropDown>(parameters =>
            {
                parameters.Add(p => p.Items, items);
                parameters.Add(p => p.DefaultSelectedKey, defaultSelectedKey);
            });

            var textSpan = component.Find(".bit-drp-wrapper-txt");
            var expectedText = items.Find(i => i.Value == defaultSelectedKey && i.ItemType == BitDropDownItemType.Normal).Text;

            Assert.AreEqual(expectedText, textSpan.InnerHtml);
        }

        [DataTestMethod,
          DataRow("f-ban"),
          DataRow("f-app,f-ban")
        ]
        public void BitDropDownTextWithDefaultSelectedMultipleKeysShouldInitCorrect(string defaultSelectedMultipleKeys)
        {
            var items = GetDropdownItems();
            var defaultSelectedMultipleKeyList = defaultSelectedMultipleKeys.Split(",").ToList();
            var component = RenderComponent<BitDropDown>(parameters =>
            {
                parameters.Add(p => p.Items, items);
                parameters.Add(p => p.IsMultiSelect, true);
                parameters.Add(p => p.DefaultSelectedMultipleKeys, defaultSelectedMultipleKeyList);
            });

            var textSpan = component.Find(".bit-drp-wrapper-txt");
            var defaultSelectedItems = items.FindAll(i => defaultSelectedMultipleKeyList.Contains(i.Value) && i.ItemType == BitDropDownItemType.Normal);
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
        public void BitDropDownTextWithSelectedKeyShouldInitCorrect(string selectedKey, string defaultSelectedKey)
        {
            var items = GetDropdownItems();
            var component = RenderComponent<BitDropDown>(parameters =>
            {
                parameters.Add(p => p.Items, items);
                parameters.Add(p => p.DefaultSelectedKey, defaultSelectedKey);
                parameters.Add(p => p.SelectedKey, selectedKey);
            });

            var textSpan = component.Find(".bit-drp-wrapper-txt");
            var expectedText = items.Find(i => i.Value == selectedKey && i.ItemType == BitDropDownItemType.Normal).Text;

            Assert.AreEqual(expectedText, textSpan.InnerHtml);
        }

        [DataTestMethod,
          DataRow("f-ban", "f-app,f-ban"),
          DataRow("f-app,f-ban", "f-ban")
        ]
        public void BitDropDownTextWithDefaultSelectedMultipleKeysShouldInitCorrect(string defaultSelectedMultipleKeys, string selectedMultipleKeys)
        {
            var items = GetDropdownItems();
            var defaultSelectedMultipleKeyList = defaultSelectedMultipleKeys.Split(",").ToList();
            var selectedMultipleKeyList = selectedMultipleKeys.Split(",").ToList();
            var component = RenderComponent<BitDropDown>(parameters =>
            {
                parameters.Add(p => p.Items, items);
                parameters.Add(p => p.IsMultiSelect, true);
                parameters.Add(p => p.DefaultSelectedMultipleKeys, defaultSelectedMultipleKeyList);
                parameters.Add(p => p.SelectedMultipleKeys, selectedMultipleKeyList);
            });

            var textSpan = component.Find(".bit-drp-wrapper-txt");
            var selectedItems = items.FindAll(i => selectedMultipleKeyList.Contains(i.Value) && i.ItemType == BitDropDownItemType.Normal);
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
        public void BitDropDownPlaceholderShouldWorkCorrect(string selectedKey, string selectedMultipleKeys, bool isMultiSelect, string placeholder)
        {
            var items = GetRawDropdownItems();
            var selectedMultipleKeyList = selectedMultipleKeys is not null ? selectedMultipleKeys.Split(",").ToList() : new List<string>();
            var component = RenderComponent<BitDropDown>(parameters =>
            {
                parameters.Add(p => p.Items, items);
                parameters.Add(p => p.IsMultiSelect, isMultiSelect);
                parameters.Add(p => p.SelectedMultipleKeys, selectedMultipleKeyList);
                parameters.Add(p => p.SelectedKey, selectedKey);
                parameters.Add(p => p.Placeholder, placeholder);
            });

            var targetSpan = component.Find(".bit-drp-wrapper-txt");
            var expectedText = "";

            if (isMultiSelect)
            {
                if (selectedMultipleKeys is not null)
                {
                    var selectedItems = items.FindAll(i => selectedMultipleKeyList.Contains(i.Value) && i.ItemType == BitDropDownItemType.Normal);
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
                if (selectedKey is not null)
                {
                    expectedText = items.Find(i => i.Value == selectedKey).Text;
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
        public void BitDropDownNotifyOnReselectShouldWorkCorrect(Visual visual, bool notifyOnReselect, string defaultSelectedKey)
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
                parameters.Add(p => p.DefaultSelectedKey, defaultSelectedKey);
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
                var drpItems = component.FindAll(".bit-chb");
                drpItems[0].Children[1].Children[0].Click();
                drpItems[1].Children[1].Children[0].Click();
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
        public void BitDropDownTwoWayBoundWithCustomHandlerShouldWorkCorrect(string selectedKey)
        {
            Context.JSInterop.Mode = JSRuntimeMode.Loose;
            BitDropDownSelectedKey = selectedKey;

            var items = GetRawDropdownItems();
            var component = RenderComponent<BitDropDown>(parameters =>
            {
                parameters.Add(p => p.IsOpen, true);
                parameters.Add(p => p.IsEnabled, true);
                parameters.Add(p => p.Items, items);
                parameters.Add(p => p.SelectedKey, BitDropDownSelectedKey);
                parameters.Add(p => p.SelectedKeyChanged, HandleSelectedKeyChanged);
            });

            var drpItems = component.FindAll("button");
            drpItems[3].Click();

            var expectedValue = items[3].Value;

            Assert.AreEqual(expectedValue, BitDropDownSelectedKey);
        }

        [DataTestMethod,
            DataRow("f-ban,v-bro"),
            DataRow("f-ora")
        ]
        public void BitDropDownMultiSelectTwoWayBoundWithCustomHandlerShouldWorkCorrect(string selectedMultipleKeys)
        {
            BitDropDownSelectedMultipleKeys = selectedMultipleKeys.Split(",").ToList();
            var initialSelectedKeysCount = BitDropDownSelectedMultipleKeys.Count;
            var items = GetRawDropdownItems();
            var component = RenderComponent<BitDropDown>(parameters =>
            {
                parameters.Add(p => p.IsOpen, true);
                parameters.Add(p => p.IsEnabled, true);
                parameters.Add(p => p.IsMultiSelect, true);
                parameters.Add(p => p.Items, items);
                parameters.Add(p => p.SelectedMultipleKeys, BitDropDownSelectedMultipleKeys);
                parameters.Add(p => p.SelectedMultipleKeysChanged, HandleSelectedMultipleKeysChanged);
            });

            var drpItems = component.FindAll(".bit-chb");
            drpItems[3].Children[1].Children[0].Click();

            int expectedResult;
            if (selectedMultipleKeys.Contains(items[3].Value))
            {
                expectedResult = --initialSelectedKeysCount;
            }
            else
            {
                expectedResult = ++initialSelectedKeysCount;
            }

            Assert.AreEqual(expectedResult, BitDropDownSelectedMultipleKeys.Count);
        }

        private void HandleSelectedKeyChanged(string selectedKey)
        {
            BitDropDownSelectedKey = selectedKey;
        }

        private void HandleSelectedMultipleKeysChanged(List<string> selectedKeys)
        {
            BitDropDownSelectedMultipleKeys = selectedKeys;
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
