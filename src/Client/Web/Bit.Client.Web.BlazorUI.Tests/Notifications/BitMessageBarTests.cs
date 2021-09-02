using System.Collections.Generic;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Client.Web.BlazorUI.Tests.Notifications
{
    [TestClass]
    public class BitMessageBarTests : BunitTestContext
    {
        [DataTestMethod,
            DataRow(Visual.Fluent, true, BitMessageBarType.Info),
            DataRow(Visual.Fluent, true, BitMessageBarType.Blocked),
            DataRow(Visual.Fluent, true, BitMessageBarType.Error),
            DataRow(Visual.Fluent, true, BitMessageBarType.SevereWarning),
            DataRow(Visual.Fluent, true, BitMessageBarType.Success),
            DataRow(Visual.Fluent, true, BitMessageBarType.Warning),

            DataRow(Visual.Fluent, false, BitMessageBarType.Info),
            DataRow(Visual.Fluent, false, BitMessageBarType.Blocked),
            DataRow(Visual.Fluent, false, BitMessageBarType.Error),
            DataRow(Visual.Fluent, false, BitMessageBarType.SevereWarning),
            DataRow(Visual.Fluent, false, BitMessageBarType.Success),
            DataRow(Visual.Fluent, false, BitMessageBarType.Warning),

            DataRow(Visual.Cupertino, true, BitMessageBarType.Info),
            DataRow(Visual.Cupertino, true, BitMessageBarType.Blocked),
            DataRow(Visual.Cupertino, true, BitMessageBarType.Error),
            DataRow(Visual.Cupertino, true, BitMessageBarType.SevereWarning),
            DataRow(Visual.Cupertino, true, BitMessageBarType.Success),
            DataRow(Visual.Cupertino, true, BitMessageBarType.Warning),

            DataRow(Visual.Cupertino, false, BitMessageBarType.Info),
            DataRow(Visual.Cupertino, false, BitMessageBarType.Blocked),
            DataRow(Visual.Cupertino, false, BitMessageBarType.Error),
            DataRow(Visual.Cupertino, false, BitMessageBarType.SevereWarning),
            DataRow(Visual.Cupertino, false, BitMessageBarType.Success),
            DataRow(Visual.Cupertino, false, BitMessageBarType.Warning),

            DataRow(Visual.Material, true, BitMessageBarType.Info),
            DataRow(Visual.Material, true, BitMessageBarType.Blocked),
            DataRow(Visual.Material, true, BitMessageBarType.Error),
            DataRow(Visual.Material, true, BitMessageBarType.SevereWarning),
            DataRow(Visual.Material, true, BitMessageBarType.Success),
            DataRow(Visual.Material, true, BitMessageBarType.Warning),

            DataRow(Visual.Material, false, BitMessageBarType.Info),
            DataRow(Visual.Material, false, BitMessageBarType.Blocked),
            DataRow(Visual.Material, false, BitMessageBarType.SevereWarning),
            DataRow(Visual.Material, false, BitMessageBarType.Warning),
            DataRow(Visual.Material, false, BitMessageBarType.Success),
            DataRow(Visual.Material, false, BitMessageBarType.Warning)
        ]
        public void BitMessageBarShouldTakeCorrectTypeAndVisual(Visual visual, bool isEnabled, BitMessageBarType messageBarType)
        {
            var component = RenderComponent<BitMessageBarTest>(
                parameters =>
                {
                    parameters.Add(p => p.Visual, visual);
                    parameters.Add(p => p.IsEnabled, isEnabled);
                    parameters.Add(p => p.MessageBarType, messageBarType);
                });

            var bitMessageBar = component.Find(".bit-msg-bar");

            var isEnabledClass = isEnabled ? "enabled" : "disabled";
            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";
            var messageBarTypeClass = messageBarType == BitMessageBarType.SevereWarning ? "severe-warning" : messageBarType.ToString().ToLower();

            Assert.IsTrue(bitMessageBar.ClassList.Contains($"bit-msg-bar-{isEnabledClass}-{visualClass}"));
            Assert.AreEqual(isEnabled, bitMessageBar.ClassList.Contains($"bit-msg-bar-{messageBarTypeClass}-{visualClass}"));
            
            
            
            var icon = component.Find(".bit-msg-bar .bit-msg-bar-icon ");
            
            Dictionary<BitMessageBarType, string> IconMap = new()
            {
                [BitMessageBarType.Info] = "Info",
                [BitMessageBarType.Warning] = "Info",
                [BitMessageBarType.Error] = "ErrorBadge",
                [BitMessageBarType.Blocked] = "Blocked2",
                [BitMessageBarType.SevereWarning] = "Warning",
                [BitMessageBarType.Success] = "Completed"
            };
            Assert.IsTrue(icon.FirstElementChild.ClassList.Contains ($"bit-icon--{IconMap[messageBarType]}"));
        }

        [DataTestMethod,
            DataRow(true, true),
            DataRow(true, false),
            DataRow(false, true),
            DataRow(false, false)
        ]
        public void BitMessageBarShouldRespectMultiline(bool isMultiline, bool truncated)
        {
            var component = RenderComponent<BitMessageBarTest>(
                parameters =>
                {
                    parameters.Add(p => p.IsMultiline, isMultiline);
                    parameters.Add(p => p.Truncated, truncated);
                });

            var bitMessageBar = component.Find(".bit-msg-bar > div");

            string messageBarMultilineType = isMultiline ? "multiline" : "singleline";
            Assert.IsTrue(bitMessageBar.ClassList.Contains($"bit-msg-bar-{messageBarMultilineType}"));

            if (!isMultiline && truncated)
            {
                var truncateButton = component.Find(".bit-msg-bar .bit-msg-bar-truncate button");

                Assert.IsTrue(truncateButton.FirstElementChild.ClassList.Contains("bit-icon--DoubleChevronDown"));
                truncateButton.Click();
                Assert.IsTrue(truncateButton.FirstElementChild.ClassList.Contains("bit-icon--DoubleChevronUp"));
                truncateButton.Click();
                Assert.IsTrue(truncateButton.FirstElementChild.ClassList.Contains("bit-icon--DoubleChevronDown"));
            }
        }

        [DataTestMethod,
            DataRow(true),
            DataRow(false),
        ]
        public void BitMessageBarShouldRespectAction(bool isEnable)
        {
            var component = RenderComponent<BitMessageBarTest>(
                parameters =>
                {
                    parameters.Add(p => p.IsEnabled, isEnable);
                });

            var bitMessageBarActionContainer = component.Find(".bit-msg-bar:nth-child(2) div div:nth-child(3)");
            Assert.IsTrue(bitMessageBarActionContainer.ClassList.Contains("bit-msg-bar-actions"));
            Assert.AreEqual("BUTTON", bitMessageBarActionContainer.FirstElementChild.TagName);
        }

        [DataTestMethod,
            DataRow(true),
            DataRow(false),
        ]
        public void BitMessageBarShouldRespectCustomIcon(bool hasDismiss)
        {
            var component = RenderComponent<BitMessageBarTest>(
                parameters =>
                {
                    parameters.Add(p => p.Dissmi, isEnable);
                    parameters.Add(p => p.IsEnabled, isEnable);
                });

            var bitMessageBarActionContainer = component.Find(".bit-msg-bar:nth-child(2) div div:nth-child(2)");
            Assert.IsTrue(bitMessageBarActionContainer.ClassList.Contains("bit-msg-bar-actions"));
            Assert.AreEqual("BUTTON", bitMessageBarActionContainer.FirstElementChild.TagName);
        }
        
    }
}
