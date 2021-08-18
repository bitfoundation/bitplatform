using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Client.Web.BlazorUI.Tests.Pickers
{
    [TestClass]
    public class BitDatePickerTests : BunitTestContext
    {
        [DataTestMethod,
          DataRow(Visual.Fluent, true),
          DataRow(Visual.Fluent, false),

          DataRow(Visual.Cupertino, true),
          DataRow(Visual.Cupertino, false),

          DataRow(Visual.Material, true),
          DataRow(Visual.Material, false)
      ]
        public void BitDatePickerShouldTakeCorrectVisual(Visual visual, bool isEnabled)
        {
            var component = RenderComponent<BitDatePickerTest>(
                parameters =>
                {
                    parameters.Add(p => p.Visual, visual);
                    parameters.Add(p => p.IsEnabled, isEnabled);
                });

            var bitDatePicker = component.Find(".bit-dtp");
            var datePickerIsEnabledClass = isEnabled ? "enabled" : "disabled";
            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

            Assert.IsTrue(bitDatePicker.ClassList.Contains($"bit-dtp-{datePickerIsEnabledClass}-{visualClass}"));
        }

        [DataTestMethod, DataRow("go to today text")]
        public void BitDatePickerShouldGiveValueToGoToToday(string goToToday)
        {
            var component = RenderComponent<BitDatePickerTest>(
                parameters =>
                {
                    parameters.Add(p => p.GoToToday, goToToday);
                    parameters.Add(p => p.IsOpen, true);
                });

            var goToTodayButton = component.Find(".bit-dtp>div:nth-child(2)>div>div>div:nth-child(2)>div:nth-child(4)>button");

            goToTodayButton.MarkupMatches($"<button type=\"button\">{goToToday}</button>");
        }

        [DataTestMethod,
          DataRow(true),
          DataRow(false),
      ]
        public void BitDatePickerShouldRespondToClickEvent(bool isEnabled)
        {
            var component = RenderComponent<BitDatePickerTest>(
                parameters =>
                {
                    parameters.Add(p => p.IsEnabled, isEnabled);
                });

            var bitDatePicker = component.Find(".bit-dtp");
            bitDatePicker.Click();
        }
        /*
                [DataTestMethod,
                   DataRow(true, "value1"),
                   DataRow(false, "value2")
                ]
                public void BitChoiceOptionMustRespondToTheClickAndChangeValueEvent(bool isEnabled, string value)
                {
                    var component = RenderComponent<BitChoiceGroupTest>(
                        parameters =>
                        {
                            parameters.Add(p => p.Value, value);
                            parameters.Add(p => p.ChoiceGroupIsEnabled, isEnabled);
                        });

                    var input = component.Find(".bit-chg .bit-cho input");

                    input.Click();

                    //TODO: bypassed - BUnit onchange event issue
                    //Assert.AreEqual(isEnabled ? 1 : 0, component.Instance.ChoiceOptionClickedValue);
                    //Assert.AreEqual(isEnabled ? value : "", component.Instance.ChoiceGroupChangedValue);
                }

                [DataTestMethod, DataRow("test value")]
                public void BitChoiceOptionShouldTakeCorrectValue(string value)
                {
                    var component = RenderComponent<BitChoiceGroupTest>(
                        parameters =>
                        {
                            parameters.Add(p => p.Value, value);
                        });

                    var input = component.Find(".bit-cho input");

                    Assert.IsTrue(input.HasAttribute("value"));
                    Assert.AreEqual(value, input.GetAttribute("value"));
                }

                [DataTestMethod, DataRow("hello world")]
                public void BitChoiceOptionShouldUseTextInLabel(string text)
                {
                    var component = RenderComponent<BitChoiceGroupTest>(
                        parameters =>
                        {
                            parameters.Add(p => p.Text, text);
                        });

                    var bitChoiceOptionLabelText = component.Find(".bit-cho label span");

                    Assert.AreEqual(text, bitChoiceOptionLabelText.TextContent);
                }

                [DataTestMethod,
                  DataRow(true),
                  DataRow(false)
                ]
                public void BitChoiceOptionShouldAcceptGivenCheckStatus(bool isChecked)
                {
                    var component = RenderComponent<BitChoiceGroupTest>(
                        parameters =>
                        {
                            parameters.Add(p => p.IsChecked, isChecked);
                        });

                    var input = component.Find(".bit-cho input");

                    Assert.AreEqual(isChecked, input.HasAttribute("checked"));

                }

                [DataTestMethod,
                   DataRow(true, true),
                   DataRow(true, false),
                   DataRow(false, true),
                   DataRow(false, false),
                ]
                public void BitChoiceOptionMustRespondToTheClickEvent(bool groupIsEnabled, bool optionIsEnabled)
                {
                    var component = RenderComponent<BitChoiceGroupTest>(
                        parameters =>
                        {
                            parameters.Add(p => p.ChoiceGroupIsEnabled, groupIsEnabled);
                            parameters.Add(p => p.ChoiceOptionIsEnabled, optionIsEnabled);
                        });

                    var input1 = component.Find(".bit-chg .bit-cho:nth-child(1) input");
                    input1.Click();

                    if (groupIsEnabled && optionIsEnabled)
                    {
                        //TODO: bypassed - BUnit onchange event issue
                        //Assert.IsTrue(input1.HasAttribute("checked"));
                    }

                    var input2 = component.Find($".bit-chg .bit-cho:nth-child(2) input");
                    input2.Click();

                    if (groupIsEnabled && optionIsEnabled)
                    {
                        //TODO: bypassed - BUnit onchange event issue
                        //Assert.IsTrue(input2.HasAttribute("checked"));
                    }

                    //TODO: bypassed - BUnit onchange event issue
                    //Assert.AreEqual(groupIsEnabled && optionIsEnabled ? 1 : 0, component.Instance.ChoiceOptionClickedValue);
                }

                [DataTestMethod,
                  DataRow(true, true, 2),
                   DataRow(true, false, 2),
                   DataRow(false, true, 2),
                   DataRow(false, false, 2),
               ]
                public void BitChoiceOptionMustRespondToTheChangeEvent(bool groupIsEnabled, bool optionIsEnabled, int count)
                {
                    var component = RenderComponent<BitChoiceGroupTest>(
                        parameters =>
                        {
                            parameters.Add(p => p.ChoiceGroupIsEnabled, groupIsEnabled);
                            parameters.Add(p => p.ChoiceOptionIsEnabled, optionIsEnabled);
                        });

                    var input = component.Find(".bit-cho input");

                    input.Click();

                    if (groupIsEnabled && optionIsEnabled)
                    {
                        //TODO: bypassed - BUnit onchange event issue
                        //Assert.IsTrue(input.HasAttribute("checked"));
                    }

                    //TODO: bypassed - BUnit onchange event issue
                    //Assert.AreEqual(groupIsEnabled && optionIsEnabled ? count.ToString() : null, component.Instance.Value);
                    //Assert.AreEqual(groupIsEnabled && optionIsEnabled ? 1 : 0, component.Instance.ChoiceOptionChangedValue);
                }*/
    }
}
