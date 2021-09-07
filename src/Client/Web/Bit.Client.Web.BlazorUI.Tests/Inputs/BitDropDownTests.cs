using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Client.Web.BlazorUI.Tests.Inputs
{
    [TestClass]
    public class BitDropDownTests : BunitTestContext
    {
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

            var textSpan = component.Find(".bit-drp-wrapper > span");
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
          DataRow("f-app"),
        ]
        public void BitDropDownTextWithDefaultSelectedKeyShouldInitCorrect(string defaultSelectedKey)
        {
            var items = GetDropdownItems();
            var component = RenderComponent<BitDropDown>(parameters =>
            {
                parameters.Add(p => p.Items, items);
                parameters.Add(p => p.DefaultSelectedKey, defaultSelectedKey);
            });

            var textSpan = component.Find(".bit-drp-wrapper > span");
            var expectedText = items.Find(i => i.Value == defaultSelectedKey && i.ItemType == BitDropDownItemType.Normal).Text;

            Assert.AreEqual(expectedText, textSpan.InnerHtml);
        }

        [DataTestMethod,
          DataRow("f-ban"),
          DataRow("f-app,f-ban"),
        ]
        public void BitDropDownTextWithDefaultSelectedMultipleKeysShouldInitCorrect(string defaultSelectedMultipleKey)
        {
            var items = GetDropdownItems();
            var defaultSelectedMultipleKeyList = defaultSelectedMultipleKey.Split(",").ToList();
            var component = RenderComponent<BitDropDown>(parameters =>
            {
                parameters.Add(p => p.Items, items);
                parameters.Add(p => p.IsMultiSelect, true);
                parameters.Add(p => p.DefaultSelectedMultipleKeys, defaultSelectedMultipleKeyList);
            });

            var textSpan = component.Find(".bit-drp-wrapper > span");
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
    }
}
