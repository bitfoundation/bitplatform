using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using AngleSharp.Dom;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Client.Web.BlazorUI.Tests.RadioButtonList
{
    [TestClass]
    public class BitRadioButtonListTests : BunitTestContext
    {
        [DataTestMethod,
          DataRow(Visual.Fluent, true),
          DataRow(Visual.Cupertino, true),
          DataRow(Visual.Material, true),

          DataRow(Visual.Fluent, false),
          DataRow(Visual.Cupertino, false),
          DataRow(Visual.Material, false),
        ]
        public void BitRadioButtonListShouldTakeCorrectVisualStyle(Visual visual, bool isEnabled)
        {
            var component = RenderComponent<BitRadioButtonListTest>(parameters =>
            {
                parameters.Add(p => p.Items, GetRadioButtonListItems());
                parameters.Add(p => p.TextField, nameof(Gender.GenderText));
                parameters.Add(p => p.ValueField, nameof(Gender.GenderId));
                parameters.Add(p => p.Visual, visual);
            });

            var bitRadioButtonList = component.Find(".bit-rbl");
            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

            if (isEnabled)
            {
                Assert.IsTrue(bitRadioButtonList.ClassList.Contains($"bit-rbl-{visualClass}"));
            }
            else
            {
                Assert.IsTrue(bitRadioButtonList.ClassList.Contains($"bit-rbl-disabled-{visualClass}"));
            }
        }

        [DataTestMethod]
        public void BitRadioButtonListShouldGenerateAllItems()
        {
            var radioButtonListItems = GetRadioButtonListItems();

            var component = RenderComponent<BitRadioButtonListTest>(parameters =>
            {
                parameters.Add(p => p.Items, radioButtonListItems);
                parameters.Add(p => p.TextField, nameof(Gender.GenderText));
                parameters.Add(p => p.ValueField, nameof(Gender.GenderId));
            });

            var bitRadioButtonList = component.FindAll(".bit-rbli");

            Assert.AreEqual(bitRadioButtonList.Count, radioButtonListItems.Count);
        }

        [DataTestMethod]
        public void BitRadioButtonListShouldTakeCorrectIconName()
        {
            var radioButtonListItems = GetRadioButtonListItems();

            var component = RenderComponent<BitRadioButtonListTest>(parameters =>
            {
                parameters.Add(p => p.Items, radioButtonListItems);
                parameters.Add(p => p.TextField, nameof(Gender.GenderText));
                parameters.Add(p => p.ValueField, nameof(Gender.GenderId));
                parameters.Add(p => p.IconNameField, nameof(Gender.IconName));
            });

            var bitRadioButtonListIcons = component.FindAll(".bit-rbl .bit-icon");

            foreach ((IElement item, int index) element in bitRadioButtonListIcons.Select((item, index) => (item, index)))
            {
                Assert.IsTrue(element.item.ClassList.Contains($"bit-icon--{radioButtonListItems[element.index].IconName}"));
            }
        }

        [DataTestMethod,
          DataRow(Visual.Fluent, false),
          DataRow(Visual.Cupertino, false),
          DataRow(Visual.Material, false),
          DataRow(Visual.Fluent, true),
          DataRow(Visual.Cupertino, true),
          DataRow(Visual.Material, true),
        ]
        public void BitRadioButtonListShouldRespectInputClick(Visual visual, bool isEnabled)
        {
            var radioButtonListItems = GetRadioButtonListItems();

            var component = RenderComponent<BitRadioButtonListTest>(parameters =>
            {
                parameters.Add(p => p.Items, radioButtonListItems);
                parameters.Add(p => p.TextField, nameof(Gender.GenderText));
                parameters.Add(p => p.ValueField, nameof(Gender.GenderId));
                parameters.Add(p => p.IsEnabled, isEnabled);
                parameters.Add(p => p.Visual, visual);
            });

            var bitRadioButtonListImages = component.FindAll(".bit-rbli", true);

            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

            foreach (var element in bitRadioButtonListImages)
            {
                if (isEnabled)
                {
                    var bitRadioButtonList = component.Find(".bit-rbli");
                    var bitRadioButtonListInput = bitRadioButtonList.GetElementsByTagName("input").First();

                    bitRadioButtonListInput.Click();

                    // TODO: bypassed - BUnit 2-way bound parameters issue
                    // Assert.IsTrue(element.ClassList.Contains($"bit-rbli-checked-{visualClass}"));
                }
                else
                {
                    Assert.IsTrue(element.ClassList.Contains($"bit-rbli-disabled-{visualClass}"));
                }
            }
        }

        [DataTestMethod]
        public void BitRadioButtonListShouldTakeCorrectImageName()
        {
            var radioButtonListItems = GetRadioButtonListItems();

            var component = RenderComponent<BitRadioButtonListTest>(parameters =>
            {
                parameters.Add(p => p.Items, radioButtonListItems);
                parameters.Add(p => p.TextField, nameof(Gender.GenderText));
                parameters.Add(p => p.ValueField, nameof(Gender.GenderId));
                parameters.Add(p => p.ImageAltField, nameof(Gender.ImageAlt));
                parameters.Add(p => p.ImageSrcField, value: nameof(Gender.ImageName));
                parameters.Add(p => p.SelectedImageSrcField, nameof(Gender.SelectedImageName));
            });

            var bitRadioButtonListImages = component.FindAll(".bit-rbl img", true);

            foreach ((IElement item, int index) element in bitRadioButtonListImages.Select((item, index) => (item, index)))
            {
                Assert.AreEqual(element.item.GetAttribute("src"), radioButtonListItems[element.index].ImageName);
                Assert.AreEqual(element.item.GetAttribute("alt"), radioButtonListItems[element.index].ImageAlt);

                var bitRadioButtonList = component.Find(".bit-rbli");
                var bitRadioButtonListInput = bitRadioButtonList.GetElementsByTagName("input").First();

                bitRadioButtonListInput.Click();

                // TODO: bypassed - BUnit 2-way bound parameters issue
                // Assert.AreEqual(element.item.GetAttribute("src"), radioButtonListItems[element.index].SelectedImageName);
            }
        }

        [DataTestMethod,
          DataRow(32, 32)
        ]
        public void BitRadioButtonListShouldTakeSize(int width, int height)
        {
            var component = RenderComponent<BitRadioButtonListTest>(parameters =>
            {
                parameters.Add(p => p.Items, GetRadioButtonListItems());
                parameters.Add(p => p.TextField, nameof(Gender.GenderText));
                parameters.Add(p => p.ValueField, nameof(Gender.GenderId));
                parameters.Add(p => p.ImageAltField, nameof(Gender.ImageAlt));
                parameters.Add(p => p.ImageSrcField, nameof(Gender.ImageName));
                parameters.Add(p => p.SelectedImageSrcField, nameof(Gender.SelectedImageName));
                parameters.Add(p => p.ImageSize, new Size(width, height));
            });

            var bitRadioButtonListImages = component.FindAll(".bit-rbli-img");

            foreach (var item in bitRadioButtonListImages)
            {
                Assert.IsTrue(item.GetAttribute("style").Contains($"width:{width}px; height:{height}px;"));
            }
        }

        [DataTestMethod,
          DataRow("Detailed label")
        ]
        public void BitRadioButtonListShouldTakeCorrectLabel(string label)
        {
            var component = RenderComponent<BitRadioButtonListTest>(parameters =>
            {
                parameters.Add(p => p.Items, GetRadioButtonListItems());
                parameters.Add(p => p.TextField, nameof(Gender.GenderText));
                parameters.Add(p => p.ValueField, nameof(Gender.GenderId));
                parameters.Add(p => p.Label, label);
            });

            var bitRadioButtonListLabel = component.Find(".bit-rbl label");

            Assert.IsTrue(bitRadioButtonListLabel.InnerHtml.Contains(label));
        }

        [DataTestMethod,
          DataRow("<span>I am a span</span>")
        ]
        public void BitRadioButtonListShouldTakeCorrectLabelContent(string labelContent)
        {
            var component = RenderComponent<BitRadioButtonListTest>(parameters =>
            {
                parameters.Add(p => p.Items, GetRadioButtonListItems());
                parameters.Add(p => p.TextField, nameof(Gender.GenderText));
                parameters.Add(p => p.ValueField, nameof(Gender.GenderId));
                parameters.Add(p => p.LabelFragment, labelContent);
            });

            var bitRadioButtonListLabelContent = component.Find(".bit-rbl label").ChildNodes;
            bitRadioButtonListLabelContent.MarkupMatches(labelContent);
        }

        [DataTestMethod,
          DataRow("This is a AriaLabel", "This is a AriaLabelledBy")
        ]
        public void BitRadioButtonListShouldTakeCorrectArials(string ariaLabel, string ariaLabelledBy)
        {
            var component = RenderComponent<BitRadioButtonListTest>(parameters =>
            {
                parameters.Add(p => p.Items, GetRadioButtonListItems());
                parameters.Add(p => p.TextField, nameof(Gender.GenderText));
                parameters.Add(p => p.ValueField, nameof(Gender.GenderId));
                parameters.Add(p => p.AriaLabel, ariaLabel);
                parameters.Add(p => p.AriaLabelledBy, ariaLabelledBy);
            });

            var bitRadioButtonList = component.Find(".bit-rbl").FirstElementChild;
            Assert.AreEqual(bitRadioButtonList.GetAttribute("aria-labelledby"), ariaLabelledBy);

            var bitRadioButtonListInputs = component.FindAll(".bit-rbl input");

            foreach (var item in bitRadioButtonListInputs)
            {
                Assert.AreEqual(item.GetAttribute("aria-label"), ariaLabel);
            }
        }

        [DataTestMethod,
          DataRow("color:red;")
        ]
        public void BitRadioButtonListShouldTakeCustomStyle(string customStyle)
        {
            var component = RenderComponent<BitRadioButtonListTest>(parameters =>
            {
                parameters.Add(p => p.Items, GetRadioButtonListItems());
                parameters.Add(p => p.TextField, nameof(Gender.GenderText));
                parameters.Add(p => p.ValueField, nameof(Gender.GenderId));
                parameters.Add(p => p.Style, customStyle);
            });

            var bitRadioButtonList = component.Find(".bit-rbl");

            Assert.IsTrue(bitRadioButtonList.GetAttribute("style").Contains(customStyle));
        }

        [DataTestMethod,
          DataRow("custom-class")
        ]
        public void BitRadioButtonListShouldTakeCustomClass(string customClass)
        {
            var component = RenderComponent<BitRadioButtonListTest>(parameters =>
            {
                parameters.Add(p => p.Items, GetRadioButtonListItems());
                parameters.Add(p => p.TextField, nameof(Gender.GenderText));
                parameters.Add(p => p.ValueField, nameof(Gender.GenderId));
                parameters.Add(p => p.Class, customClass);
            });

            var bitRadioButtonList = component.Find(".bit-rbl");

            Assert.IsTrue(bitRadioButtonList.ClassList.Contains(customClass));
        }

        [DataTestMethod,
          DataRow(BitComponentVisibility.Visible),
          DataRow(BitComponentVisibility.Hidden),
          DataRow(BitComponentVisibility.Collapsed),
        ]
        public void BitRadioButtonListShouldTakeCustomVisibility(BitComponentVisibility visibility)
        {
            var component = RenderComponent<BitRadioButtonListTest>(parameters =>
            {
                parameters.Add(p => p.Items, GetRadioButtonListItems());
                parameters.Add(p => p.TextField, nameof(Gender.GenderText));
                parameters.Add(p => p.ValueField, nameof(Gender.GenderId));
                parameters.Add(p => p.Visibility, visibility);
            });

            var bitRadioButtonList = component.Find($".bit-rbl");

            switch (visibility)
            {
                case BitComponentVisibility.Visible:
                    Assert.IsTrue(bitRadioButtonList.GetAttribute("style").Contains(""));
                    break;
                case BitComponentVisibility.Hidden:
                    Assert.IsTrue(bitRadioButtonList.GetAttribute("style").Contains("visibility:hidden"));
                    break;
                case BitComponentVisibility.Collapsed:
                    Assert.IsTrue(bitRadioButtonList.GetAttribute("style").Contains("display:none"));
                    break;
            }
        }

        [DataTestMethod,
          DataRow(2),
          DataRow(0)
        ]
        public void BitRadioButtonListShouldValidateForm(int value)
        {
            var component = RenderComponent<BitRadioButtonListValidationTest>(parameters =>
            {
                parameters.Add(p => p.TestModel, new BitRadioButtonListTestModel { Value = value });
                parameters.Add(p => p.Items, GetRadioButtonListItems());
                parameters.Add(p => p.TextField, nameof(Gender.GenderText));
                parameters.Add(p => p.ValueField, nameof(Gender.GenderId));
            });

            var hasValue = value > 0;

            var form = component.Find("form");
            form.Submit();

            Assert.AreEqual(component.Instance.ValidCount, hasValue ? 1 : 0);
            Assert.AreEqual(component.Instance.InvalidCount, hasValue ? 0 : 1);

            if (hasValue is false)
            {
                var bitRadioButtonList = component.Find(".bit-rbli");
                var bitRadioButtonListInput = bitRadioButtonList.GetElementsByTagName("input").First();

                bitRadioButtonListInput.Click();

                form.Submit();

                // TODO: bypassed - BUnit 2-way bound parameters issue
                // Assert.AreEqual(component.Instance.ValidCount, 1);
                // Assert.AreEqual(component.Instance.InvalidCount, 1);
                // Assert.AreEqual(component.Instance.ValidCount, component.Instance.InvalidCount);
            }
        }

        [DataTestMethod,
          DataRow(2),
          DataRow(0)
        ]
        public void BitRadioButtonListValidationInvalidHtmlAttributeTest(int value)
        {
            var component = RenderComponent<BitRadioButtonListValidationTest>(parameters =>
            {
                parameters.Add(p => p.TestModel, new BitRadioButtonListTestModel { Value = value });
                parameters.Add(p => p.Items, GetRadioButtonListItems());
                parameters.Add(p => p.TextField, nameof(Gender.GenderText));
                parameters.Add(p => p.ValueField, nameof(Gender.GenderId));
            });

            var hasValue = value > 0;

            var bitRadioButtonListItemsBeforeSubmit = component.Find(".bit-rbli");
            var bitRadioButtonListInputBeforeSubmit = bitRadioButtonListItemsBeforeSubmit.GetElementsByTagName("input").First();

            Assert.IsFalse(bitRadioButtonListInputBeforeSubmit.HasAttribute("aria-invalid"));

            var form = component.Find("form");
            form.Submit();

            var bitRadioButtonListItemsAfterSubmit = component.Find(".bit-rbli");
            var bitRadioButtonListInputAfterSubmit = bitRadioButtonListItemsAfterSubmit.GetElementsByTagName("input").First();

            Assert.AreNotEqual(bitRadioButtonListInputAfterSubmit.HasAttribute("aria-invalid"), hasValue);

            bitRadioButtonListInputAfterSubmit.Click();

            // TODO: bypassed - BUnit 2-way bound parameters issue
            // Assert.IsFalse(bitRadioButtonListInputAfterSubmit.HasAttribute("aria-invalid"));
        }

        [DataTestMethod,
          DataRow(Visual.Fluent,2),
          DataRow(Visual.Fluent,0),

          DataRow(Visual.Cupertino,2),
          DataRow(Visual.Cupertino,0),

          DataRow(Visual.Cupertino,2),
          DataRow(Visual.Material,0),
        ]
        public void BitRadioButtonListValidationInvalidCssClassTest(Visual visual, int value)
        {
            var component = RenderComponent<BitRadioButtonListValidationTest>(parameters =>
            {
                parameters.Add(p => p.TestModel, new BitRadioButtonListTestModel { Value = value });
                parameters.Add(p => p.Items, GetRadioButtonListItems());
                parameters.Add(p => p.TextField, nameof(Gender.GenderText));
                parameters.Add(p => p.ValueField, nameof(Gender.GenderId));
                parameters.Add(p => p.Visual, visual);
            });

            var hasValue = value > 0;

            var bitRadioButtonList = component.Find(".bit-rbl");
            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

            Assert.IsFalse(bitRadioButtonList.ClassList.Contains($"bit-rbl-invalid-{visualClass}"));

            var form = component.Find("form");
            form.Submit();

            Assert.AreNotEqual(bitRadioButtonList.ClassList.Contains($"bit-rbl-invalid-{visualClass}"), hasValue);

            var bitRadioButtonListItems = component.Find(".bit-rbli");
            var bitRadioButtonListInput = bitRadioButtonListItems.GetElementsByTagName("input").First();

            bitRadioButtonListInput.Click();

            // TODO: bypassed - BUnit 2-way bound parameters issue
            // Assert.IsFalse(bitRadioButtonListInput.ClassList.Contains($"bit-rbl-invalid-{visualClass}"));
        }

        private List<Gender> GetRadioButtonListItems()
        {
            return new List<Gender>()
            {
                new Gender
                {
                    GenderId = 1,
                    GenderText = "Female",
                    IconName = BitIconName.ContactHeart,
                    ImageName = "https://bit.com/female_icon.svg.png",
                    SelectedImageName = "https://bit.com/selected-female_icon.svg.png",
                    ImageAlt = "female-icon",
                },
                new Gender
                {
                    GenderId = 2,
                    GenderText = "Male",
                    IconName = BitIconName.FrontCamera,
                    ImageName = "https://bit.com/male_icon.svg.png",
                    SelectedImageName = "https://bit.com/selected-male_icon.svg.png",
                    ImageAlt = "male-icon",
                },
                new Gender
                {
                    GenderId = 3,
                    GenderText = "Other",
                    IconName = BitIconName.Group,
                    ImageName = "https://bit.com/other_icon.svg.png",
                    SelectedImageName = "https://bit.com/selected-other_icon.svg.png",
                    ImageAlt = "other-icon",
                },
                new Gender
                {
                    GenderId = 4,
                    GenderText = "Prefer not to say",
                    IconName = BitIconName.Emoji2,
                    ImageName = "https://bit.com/nottosay_icon.svg.png",
                    SelectedImageName = "https://bit.com/selected-nottosay_icon.svg.png",
                    ImageAlt = "nottosay-icon",
                },
            };
        }
    }
}
