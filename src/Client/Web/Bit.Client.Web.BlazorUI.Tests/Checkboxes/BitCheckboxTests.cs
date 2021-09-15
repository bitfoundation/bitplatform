﻿using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Client.Web.BlazorUI.Tests.Checkboxes
{
    [TestClass]
    public class BitCheckboxTests : BunitTestContext
    {
        [DataTestMethod,
            DataRow(true, true),
            DataRow(false, true),
            DataRow(true, false),
            DataRow(false, false)
        ]
        public void BitCheckboxOnClickShouldWorkIfIsEnabled(bool defaultIsChecked, bool isEnabled)
        {
            Context.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = RenderComponent<BitCheckboxTest>(parameters =>
            {
                parameters.Add(p => p.IsEnabled, isEnabled);
                parameters.Add(p => p.DefaultIsChecked, defaultIsChecked);
            });

            var chb = component.Find(".bit-chb");
            var chbCheckbox = component.Find(".bit-chb-checkbox");
            chbCheckbox.Click();

            Assert.AreEqual(isEnabled, chb.ClassList.Contains("bit-chb-checked-fluent"));
            if (isEnabled)
            {
                Assert.AreEqual(1, component.Instance.ClickCounter);
                Assert.IsTrue(component.Instance.IsCheckedChanged);
            }
        }

        [DataTestMethod,
            DataRow(Visual.Fluent, BitBoxSide.Start),
            DataRow(Visual.Fluent, BitBoxSide.End),

            DataRow(Visual.Cupertino, BitBoxSide.Start),
            DataRow(Visual.Cupertino, BitBoxSide.End),

            DataRow(Visual.Material, BitBoxSide.Start),
            DataRow(Visual.Material, BitBoxSide.End),
        ]
        public void BitCheckboxBoxSideTest(Visual visual, BitBoxSide boxSide)
        {
            var component = RenderComponent<BitCheckboxTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.BoxSide, boxSide);
            });

            var chb = component.Find(".bit-chb");
            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

            if (boxSide is BitBoxSide.End)
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
        public void BitCheckboxAriaLabelTest(string? ariaLabel)
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
        public void BitCheckboxAriaDescriptionTest(string? ariaDescription)
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
        public void BitCheckboxAriaLabelledbyTest(string? ariaLabelledby)
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
        public void BitCheckboxTitleTest(string? title)
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
        public void BitCheckboxNameTest(string? name)
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
            DataRow("Emoji2")
        ]
        public void BitCheckboxCustomCheckmarkIconTest(string checkmarkIconName)
        {
            var component = RenderComponent<BitCheckboxTest>(parameters =>
            {
                parameters.Add(p => p.CheckmarkIconName, checkmarkIconName);
            });

            var icon = component.Find(".bit-chb-checkmark");

            Assert.IsTrue(icon.ClassList.Contains($"bit-icon--{checkmarkIconName}"));
        }

        [DataTestMethod,
            DataRow("Icon aria-label"),
            DataRow(null)
        ]
        public void BitCheckboxCheckmarkIconAriaLabelTest(string? ariaLabel)
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
    }
}
