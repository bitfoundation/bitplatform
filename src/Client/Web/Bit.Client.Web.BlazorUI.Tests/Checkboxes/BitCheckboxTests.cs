using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Client.Web.BlazorUI.Tests.Checkboxes
{
    [TestClass]
    public class BitCheckboxTests : BunitTestContext
    {
        private bool BitCheckBoxIsChecked;
        private bool BitCheckBoxIsIndeterminate = true;

        [DataTestMethod,
            DataRow(Visual.Fluent, true, true),
            DataRow(Visual.Fluent, false, true),
            DataRow(Visual.Fluent, true, false),
            DataRow(Visual.Fluent, false, false),

            DataRow(Visual.Cupertino, true, true),
            DataRow(Visual.Cupertino, false, true),
            DataRow(Visual.Cupertino, true, false),
            DataRow(Visual.Cupertino, false, false),

            DataRow(Visual.Material, true, true),
            DataRow(Visual.Material, false, true),
            DataRow(Visual.Material, true, false),
            DataRow(Visual.Material, false, false),
        ]
        public void BitCheckboxOnClickShouldWorkIfIsEnabled(Visual visual, bool defaultIsChecked, bool isEnabled)
        {
            var component = RenderComponent<BitCheckboxTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.IsEnabled, isEnabled);
                parameters.Add(p => p.DefaultIsChecked, defaultIsChecked);
            });

            var chb = component.Find(".bit-chb");
            var chbCheckbox = component.Find(".bit-chb-checkbox");
            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

            Assert.AreEqual(defaultIsChecked, chb.ClassList.Contains($"bit-chb-checked-{visualClass}"));
            chbCheckbox.Click();
            if (isEnabled)
            {
                Assert.AreEqual(1, component.Instance.ClickCounter);
                Assert.IsTrue(component.Instance.IsCheckedChanged);
            }
        }

        [DataTestMethod,
            DataRow(Visual.Fluent, BitCheckBoxSide.Start),
            DataRow(Visual.Fluent, BitCheckBoxSide.End),

            DataRow(Visual.Cupertino, BitCheckBoxSide.Start),
            DataRow(Visual.Cupertino, BitCheckBoxSide.End),

            DataRow(Visual.Material, BitCheckBoxSide.Start),
            DataRow(Visual.Material, BitCheckBoxSide.End),
        ]
        public void BitCheckboxBoxSideTest(Visual visual, BitCheckBoxSide boxSide)
        {
            var component = RenderComponent<BitCheckboxTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.BoxSide, boxSide);
            });

            var chb = component.Find(".bit-chb");
            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

            if (boxSide is BitCheckBoxSide.End)
            {
                Assert.IsTrue(chb.ClassList.Contains($"bit-chb-end-{visualClass}"));
            }
            else
            {
                Assert.IsFalse(chb.ClassList.Contains($"bit-chb-end-{visualClass}"));
            }
        }

        [DataTestMethod,
            DataRow(true),
            DataRow(false)
        ]
        public void IndeterminatedBitCheckboxShouldHaveCorrectClassNameIfIsEnabled(bool isEnabled)
        {
            Context.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = RenderComponent<BitCheckbox>(parameters =>
            {
                parameters.Add(p => p.DefaultIsIndeterminate, true);
                parameters.Add(p => p.IsEnabled, isEnabled);
            });

            var chb = component.Find(".bit-chb");
            var chbCheckbox = component.Find(".bit-chb-checkbox");
            chbCheckbox.Click();

            if (isEnabled)
            {
                Assert.IsTrue(!chb.ClassList.Contains("bit-chb-indeterminate-fluent"));
            }
        }

        [DataTestMethod,
            DataRow("Detailed label"),
            DataRow(null)
        ]
        public void BitCheckboxAriaLabelTest(string ariaLabel)
        {
            var component = RenderComponent<BitCheckboxTest>(parameters =>
            {
                parameters.Add(p => p.AriaLabel, ariaLabel);
            });

            var chbInput = component.Find("input");

            if (ariaLabel is not null)
            {
                Assert.IsTrue(chbInput.GetAttribute("aria-label").Equals(ariaLabel));
            }
            else
            {
                Assert.IsNull(chbInput.GetAttribute("aria-label"));
            }
        }

        [DataTestMethod,
            DataRow("Detailed description"),
            DataRow(null)
        ]
        public void BitCheckboxAriaDescriptionTest(string ariaDescription)
        {
            var component = RenderComponent<BitCheckboxTest>(parameters =>
            {
                parameters.Add(p => p.AriaDescription, ariaDescription);
            });

            var chbInput = component.Find("input");

            if (ariaDescription is not null)
            {
                Assert.IsTrue(chbInput.GetAttribute("aria-describedby").Equals(ariaDescription));
            }
            else
            {
                Assert.IsNull(chbInput.GetAttribute("aria-describedby"));
            }
        }

        [DataTestMethod,
            DataRow("Detailed label"),
            DataRow(null)
        ]
        public void BitCheckboxAriaLabelledbyTest(string ariaLabelledby)
        {
            var component = RenderComponent<BitCheckboxTest>(parameters =>
            {
                parameters.Add(p => p.AriaLabelledby, ariaLabelledby);
            });

            var chbInput = component.Find("input");

            if (ariaLabelledby is not null)
            {
                Assert.IsTrue(chbInput.GetAttribute("aria-labelledby").Equals(ariaLabelledby));
            }
            else
            {
                Assert.IsNull(chbInput.GetAttribute("aria-labelledby"));
            }
        }

        [DataTestMethod,
            DataRow("Title"),
            DataRow(null)
        ]
        public void BitCheckboxTitleTest(string title)
        {
            var component = RenderComponent<BitCheckboxTest>(parameters =>
            {
                parameters.Add(p => p.Title, title);
            });

            var chbInput = component.Find("input");

            if (title is not null)
            {
                Assert.IsTrue(chbInput.GetAttribute("title").Equals(title));
            }
            else
            {
                Assert.IsNull(chbInput.GetAttribute("title"));
            }
        }

        [DataTestMethod,
            DataRow("Name"),
            DataRow(null)
        ]
        public void BitCheckboxNameTest(string name)
        {
            var component = RenderComponent<BitCheckboxTest>(parameters =>
            {
                parameters.Add(p => p.Name, name);
            });

            var chbInput = component.Find("input");

            if (name is not null)
            {
                Assert.IsTrue(chbInput.GetAttribute("name").Equals(name));
            }
            else
            {
                Assert.IsNull(chbInput.GetAttribute("name"));
            }
        }

        [DataTestMethod,
            DataRow(3),
            DataRow(null)
        ]
        public void BitCheckboxAriaPostionInSetTest(int? ariaPosInSet)
        {
            var component = RenderComponent<BitCheckboxTest>(parameters =>
            {
                parameters.Add(p => p.AriaPositionInSet, ariaPosInSet);
            });

            var chbInput = component.Find("input");

            if (ariaPosInSet is not null)
            {
                Assert.IsTrue(chbInput.GetAttribute("aria-posinset").Equals(ariaPosInSet.ToString()));
            }
            else
            {
                Assert.IsNull(chbInput.GetAttribute("aria-posinset"));
            }
        }

        [DataTestMethod,
            DataRow(3),
            DataRow(null)
        ]
        public void BitCheckboxAriaSetSizeTest(int? ariaSetSize)
        {
            var component = RenderComponent<BitCheckboxTest>(parameters =>
            {
                parameters.Add(p => p.AriaSetSize, ariaSetSize);
            });

            var chbInput = component.Find("input");

            if (ariaSetSize is not null)
            {
                Assert.IsTrue(chbInput.GetAttribute("aria-setsize").Equals(ariaSetSize.ToString()));
            }
            else
            {
                Assert.IsNull(chbInput.GetAttribute("aria-setsize"));
            }
        }

        [DataTestMethod,
            DataRow(BitIcon.Emoji2)
        ]
        public void BitCheckboxCustomCheckmarkIconTest(BitIcon checkmarkIconName)
        {
            var component = RenderComponent<BitCheckboxTest>(parameters =>
            {
                parameters.Add(p => p.CheckmarkIconName, checkmarkIconName);
            });

            var icon = component.Find(".bit-chb-checkmark");

            Assert.IsTrue(icon.ClassList.Contains($"bit-icon--{checkmarkIconName.GetName()}"));
        }

        [DataTestMethod,
            DataRow("Icon aria-label"),
            DataRow(null)
        ]
        public void BitCheckboxCheckmarkIconAriaLabelTest(string ariaLabel)
        {
            var component = RenderComponent<BitCheckboxTest>(parameters =>
            {
                parameters.Add(p => p.CheckmarkIconAriaLabel, ariaLabel);
            });

            var icon = component.Find(".bit-chb-checkmark");

            if (ariaLabel is not null)
            {
                Assert.IsTrue(icon.GetAttribute("aria-label").Equals(ariaLabel));
            }
            else
            {
                Assert.IsNull(icon.GetAttribute("aria-label"));
            }
        }

        [DataTestMethod,
            DataRow("Child with label tag <label>This is the label</label>"),
            DataRow("BitCheckBox (controlled)")
        ]
        public void BitCheckboxCustomChildContentTest(string childContent)
        {
            var component = RenderComponent<BitCheckboxTest>(parameters =>
            {
                parameters.Add(p => p.ChildContent, childContent);
            });

            var chbChildContent = component.Find(".bit-chb-txt").ChildNodes;
            chbChildContent.MarkupMatches(childContent);
        }

        [DataTestMethod,
            DataRow(true),
            DataRow(false)
        ]
        public void BitCheckBoxIsCheckedTwoWayBoundWithCustomHandlerTest(bool isChecked)
        {
            BitCheckBoxIsChecked = isChecked;

            var component = RenderComponent<BitCheckbox>(parameters =>
            {
                parameters.Add(p => p.IsChecked, isChecked);
                parameters.Add(p => p.IsCheckedChanged, HandleIsCheckedChanged);
            });

            var chb = component.Find(".bit-chb-checkbox");
            chb.Click();

            var expectedValue = !isChecked;

            Assert.AreEqual(expectedValue, BitCheckBoxIsChecked);
        }

        [DataTestMethod]
        public void BitCheckBoxIsIndeterminateTwoWayBoundWithCustomHandlerTest()
        {
            var component = RenderComponent<BitCheckbox>(parameters =>
            {
                parameters.Add(p => p.IsIndeterminate, true);
                parameters.Add(p => p.IsIndeterminateChanged, HandleIsIndeterminateChanged);
            });

            var chb = component.Find(".bit-chb-checkbox");
            chb.Click();

            Assert.IsFalse(BitCheckBoxIsIndeterminate);
        }

        private void HandleIsCheckedChanged(bool isChecked)
        {
            BitCheckBoxIsChecked = isChecked;
        }

        private void HandleIsIndeterminateChanged(bool isIndeterminate)
        {
            BitCheckBoxIsIndeterminate = isIndeterminate;
        }
    }
}
