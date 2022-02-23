using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Client.Web.BlazorUI.Tests.Modal
{
    [TestClass]
    public class BitModalTests : BunitTestContext
    {
        private bool isModalOpen = true;

        [DataTestMethod,
            DataRow(Visual.Fluent, null),
            DataRow(Visual.Fluent, false),
            DataRow(Visual.Fluent, true),

            DataRow(Visual.Cupertino, null),
            DataRow(Visual.Cupertino, false),
            DataRow(Visual.Cupertino, true),

            DataRow(Visual.Material, null),
            DataRow(Visual.Material, false),
            DataRow(Visual.Material, true),
        ]
        public void BitModalIsAlertTest(Visual visual, bool? isAlert)
        {
            var com = RenderComponent<BitModalTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.IsAlert, isAlert);
                parameters.Add(p => p.IsOpen, true);
            });

            var element = com.Find(".bit-mdl > div");
            Assert.AreEqual(element.Attributes["role"].Value, isAlert.HasValue && isAlert.Value ? "alertdialog" : "dialog");
        }

        [DataTestMethod,
            DataRow(Visual.Fluent, false),
            DataRow(Visual.Fluent, true),

            DataRow(Visual.Cupertino, false),
            DataRow(Visual.Cupertino, true),

            DataRow(Visual.Material, false),
            DataRow(Visual.Material, true),
        ]
        public void BitModalIsBlockingTest(Visual visual, bool isBlocking)
        {
            var com = RenderComponent<BitModal>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.IsBlocking, isBlocking);
                parameters.Add(p => p.IsOpen, isModalOpen);
                parameters.Add(p => p.IsOpenChanged, HandleIsOpenChanged);
            });

            var bitModel = com.FindAll(".bit-mdl");
            Assert.AreEqual(bitModel.Count, 1);

            var overlayElement = com.Find(".bit-mdl-overlay");
            overlayElement.Click();

            bitModel = com.FindAll(".bit-mdl");
            Assert.AreEqual(bitModel.Count, isBlocking ? 1 : 0);
        }

        [DataTestMethod,
            DataRow(Visual.Fluent, false),
            DataRow(Visual.Fluent, true),

            DataRow(Visual.Cupertino, false),
            DataRow(Visual.Cupertino, true),

            DataRow(Visual.Material, false),
            DataRow(Visual.Material, true),
        ]
        public void BitModalIsModelessTest(Visual visual, bool isModeless)
        {
            var com = RenderComponent<BitModalTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.IsModeless, isModeless);
                parameters.Add(p => p.IsOpen, true);
            });

            var element = com.Find(".bit-mdl > div");
            Assert.AreEqual(element.Attributes["aria-model"].Value, (!isModeless).ToString());

            var elementOverlay = com.FindAll(".bit-mdl-overlay");
            Assert.AreEqual(elementOverlay.Count, isModeless ? 0 : 1);
        }

        [DataTestMethod,
            DataRow(Visual.Fluent, false),
            DataRow(Visual.Fluent, true),

            DataRow(Visual.Cupertino, false),
            DataRow(Visual.Cupertino, true),

            DataRow(Visual.Material, false),
            DataRow(Visual.Material, true),
        ]
        public void BitModalIsOpenTest(Visual visual, bool isOpen)
        {
            var com = RenderComponent<BitModalTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.IsOpen, isOpen);
            });

            var bitModel = com.FindAll(".bit-mdl");
            Assert.AreEqual(bitModel.Count, isOpen ? 1 : 0);
        }

        [DataTestMethod,
            DataRow(Visual.Fluent, null),
            DataRow(Visual.Fluent, ""),
            DataRow(Visual.Fluent, "Test-S-A-Id"),

            DataRow(Visual.Cupertino, null),
            DataRow(Visual.Cupertino, ""),
            DataRow(Visual.Cupertino, "Test-S-A-Id"),

            DataRow(Visual.Material, null),
            DataRow(Visual.Material, ""),
            DataRow(Visual.Material, "Test-S-A-Id"),
        ]
        public void BitModalSubtitleAriaIdTest(Visual visual, string subtitleAriaId)
        {
            var com = RenderComponent<BitModalTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.SubtitleAriaId, subtitleAriaId);
                parameters.Add(p => p.IsOpen, true);
            });

            var element = com.Find(".bit-mdl > div");

            if (subtitleAriaId == null)
            {
                Assert.IsFalse(element.HasAttribute("aria-describedby"));
            }
            else if (subtitleAriaId == string.Empty)
            {
                Assert.AreEqual(element.Attributes["aria-describedby"].Value, string.Empty);
            }
            else
            {
                Assert.AreEqual(element.Attributes["aria-describedby"].Value, subtitleAriaId);
            }
        }

        [DataTestMethod,
            DataRow(Visual.Fluent, null),
            DataRow(Visual.Fluent, ""),
            DataRow(Visual.Fluent, "Test-T-A-Id"),

            DataRow(Visual.Cupertino, null),
            DataRow(Visual.Cupertino, ""),
            DataRow(Visual.Cupertino, "Test-T-A-Id"),

            DataRow(Visual.Material, null),
            DataRow(Visual.Material, ""),
            DataRow(Visual.Material, "Test-T-A-Id"),
        ]
        public void BitModalTitleAriaIdTest(Visual visual, string titleAriaId)
        {
            var com = RenderComponent<BitModalTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.TitleAriaId, titleAriaId);
                parameters.Add(p => p.IsOpen, true);
            });

            var element = com.Find(".bit-mdl > div");

            if (titleAriaId == null)
            {
                Assert.IsFalse(element.HasAttribute("aria-labelledby"));
            }
            else if (titleAriaId == string.Empty)
            {
                Assert.AreEqual(element.Attributes["aria-labelledby"].Value, string.Empty);
            }
            else
            {
                Assert.AreEqual(element.Attributes["aria-labelledby"].Value, titleAriaId);
            }
        }

        [DataTestMethod,
            DataRow(Visual.Fluent),

            DataRow(Visual.Cupertino),

            DataRow(Visual.Material),
        ]
        public void BitModalContentTest(Visual visual)
        {
            var com = RenderComponent<BitModalTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.IsOpen, true);
            });

            var elementContent = com.Find(".bit-mdl-scrl-cnt");

            elementContent.MarkupMatches("<div class=\"bit-mdl-scrl-cnt\"><div>Test Content</div></div>");
        }

        [DataTestMethod,
            DataRow(Visual.Fluent),

            DataRow(Visual.Cupertino),

            DataRow(Visual.Material),
        ]
        public void BitModalCloseWhenClickOutOfModalTest(Visual visual)
        {
            var com = RenderComponent<BitModal>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.IsOpen, isModalOpen);
                parameters.Add(p => p.IsOpenChanged, HandleIsOpenChanged);
            });

            var bitModel = com.FindAll(".bit-mdl");
            Assert.AreEqual(bitModel.Count, 1);

            var overlayElement = com.Find(".bit-mdl-overlay");
            overlayElement.Click();

            bitModel = com.FindAll(".bit-mdl");
            Assert.AreEqual(bitModel.Count, 0);
        }

        [DataTestMethod,
            DataRow(Visual.Fluent),

            DataRow(Visual.Cupertino),

            DataRow(Visual.Material),
        ]
        public void BitModalOnDismissShouldWorkCorrect(Visual visual)
        {
            var com = RenderComponent<BitModalTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.IsOpen, true);
            });

            var overlayElement = com.Find(".bit-mdl-overlay");
            overlayElement.Click();
            Assert.AreEqual(1, com.Instance.CurrentCount);
        }

        private void HandleIsOpenChanged(bool isOpen)
        {
            isModalOpen = isOpen;
        }
    }
}
