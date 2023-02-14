using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Notifications;

[TestClass]
public class BitMessageBarTests : BunitTestContext
{
    [DataTestMethod,
        DataRow(BitMessageBarType.Info),
        DataRow(BitMessageBarType.Blocked),
        DataRow(BitMessageBarType.Error),
        DataRow(BitMessageBarType.SevereWarning),
        DataRow(BitMessageBarType.Success),
        DataRow(BitMessageBarType.Warning)
    ]
    public void BitMessageBarShouldTakeCorrectTypeAndVisual(BitMessageBarType messageBarType)
    {
        var component = RenderComponent<BitMessageBarTest>(
            parameters =>
            {
                parameters.Add(p => p.MessageBarType, messageBarType);
            });

        var bitMessageBar = component.Find(".bit-msb");

        var messageBarTypeClass = messageBarType switch
        {
            BitMessageBarType.Info => "info",
            BitMessageBarType.Warning => "warning",
            BitMessageBarType.Error => "error",
            BitMessageBarType.Blocked => "blocked",
            BitMessageBarType.SevereWarning => "severe-warning",
            _ => "success"
        };

        Assert.IsTrue(bitMessageBar.ClassList.Contains(messageBarTypeClass));

        var icon = component.Find(".icon > i");

        Dictionary<BitMessageBarType, BitIconName> IconMap = new()
        {
            [BitMessageBarType.Info] = BitIconName.Info,
            [BitMessageBarType.Warning] = BitIconName.Info,
            [BitMessageBarType.Error] = BitIconName.ErrorBadge,
            [BitMessageBarType.Blocked] = BitIconName.Blocked2,
            [BitMessageBarType.SevereWarning] = BitIconName.Warning,
            [BitMessageBarType.Success] = BitIconName.Completed
        };

        Assert.IsTrue(icon.ClassList.Contains($"bit-icon--{IconMap[messageBarType].GetName()}"));
    }

    [DataTestMethod,
        DataRow(BitIconName.Emoji2)
    ]
    public void BitMessageBarShouldRespectCustomIcon(BitIconName iconName)
    {
        var component = RenderComponent<BitMessageBarTest>(
            parameters =>
            {
                parameters.Add(p => p.MessageBarIconName, iconName);
            });

        var icon = component.Find(".icon > i");
        Assert.IsTrue(icon.ClassList.Contains($"bit-icon--{iconName.GetName()}"));
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

        var bitMessageBar = component.Find(".bit-msb > div");

        var messageBarMultilineType = isMultiline ? "multiline" : "singleline";
        Assert.IsTrue(bitMessageBar.ClassList.Contains(messageBarMultilineType));

        if (!isMultiline && truncated)
        {
            var truncateButton = component.Find(".truncate > button");

            Assert.IsTrue(truncateButton.FirstElementChild.ClassList.Contains("bit-icon--DoubleChevronDown"));

            truncateButton.Click();

            Assert.IsTrue(truncateButton.FirstElementChild.ClassList.Contains("bit-icon--DoubleChevronUp"));

            truncateButton.Click();

            Assert.IsTrue(truncateButton.FirstElementChild.ClassList.Contains("bit-icon--DoubleChevronDown"));
        }
    }

    [DataTestMethod]
    public void BitMessageBarDismissButtonShouldWorkCorrect()
    {
        var component = RenderComponent<BitMessageBarTest>();

        var dismissButton = component.Find(".dismiss > button");

        dismissButton.Click();

        Assert.AreEqual(1, component.Instance.CurrentCount);
    }

    [DataTestMethod,
        DataRow(BitIconName.Emoji2)
    ]
    public void BitMessageBarShouldRespectCustomDismissIcon(BitIconName iconName)
    {
        var component = RenderComponent<BitMessageBarTest>(
            parameters =>
            {
                parameters.Add(p => p.DismissIconName, iconName);
            });

        var icon = component.Find(".dismiss button span i");

        Assert.IsTrue(icon.ClassList.Contains($"bit-icon--{iconName.GetName()}"));
    }

    [DataTestMethod,
        DataRow("test dismiss aria label", false),
        DataRow("test dismiss aria label", true)
        ]
    public void BitMessageBarDismissButtonAriaLabelTest(string ariaLabel, bool isMultiline)
    {
        var component = RenderComponent<BitMessageBarTest>(
            parameters =>
            {
                parameters.Add(p => p.IsMultiline, isMultiline);
                parameters.Add(p => p.DismissButtonAriaLabel, ariaLabel);
            });

        var dismissButton = component.Find(".dismiss button");

        Assert.IsTrue(dismissButton.HasAttribute("aria-label"));
        Assert.AreEqual(ariaLabel, dismissButton.GetAttribute("aria-label"));

        Assert.IsTrue(dismissButton.HasAttribute("title"));
        Assert.AreEqual(ariaLabel, dismissButton.GetAttribute("title"));
    }

    [DataTestMethod,
        DataRow("test overflow aria label")
    ]
    public void BitMessageBarOverflowButtonAriaLabelTest(string ariaLabel)
    {
        var component = RenderComponent<BitMessageBarTest>(
            parameters =>
            {
                parameters.Add(p => p.IsMultiline, false);
                parameters.Add(p => p.Truncated, true);
                parameters.Add(p => p.OverflowButtonAriaLabel, ariaLabel);
            });

        var dismissButton = component.Find(".truncate button");

        Assert.IsTrue(dismissButton.HasAttribute("aria-label"));
        Assert.AreEqual(ariaLabel, dismissButton.GetAttribute("aria-label"));
    }

    [DataTestMethod,
        DataRow("<div><button>Action</button></div>")
    ]
    public void BitMessageBarShouldRespectAction(string actions)
    {
        var component = RenderComponent<BitMessageBar>(parameter =>
        {
            parameter.Add(p => p.Actions, actions);
        });

        var actionsTemplate = component.Find(".actions").ChildNodes;
        actionsTemplate.MarkupMatches(actions);
    }

    [DataTestMethod,
        DataRow("alert", BitMessageBarType.Info),
        DataRow("alert", BitMessageBarType.Error),
        DataRow("alert", BitMessageBarType.SevereWarning),
        DataRow("alert", BitMessageBarType.Success),
        DataRow("alert", BitMessageBarType.Warning),
        DataRow("alert", BitMessageBarType.Blocked),

        DataRow(null, BitMessageBarType.Info),
        DataRow(null, BitMessageBarType.Blocked),
        DataRow(null, BitMessageBarType.Error),
        DataRow(null, BitMessageBarType.SevereWarning),
        DataRow(null, BitMessageBarType.Success),
        DataRow(null, BitMessageBarType.Warning),
    ]
    public void BitMessageBarRoleTest(string role, BitMessageBarType type)
    {
        var component = RenderComponent<BitMessageBarTest>(parameter =>
        {
            parameter.Add(p => p.Role, role);
            parameter.Add(p => p.MessageBarType, type);
        });

        var textEl = component.Find(".text");
        var expectedRole = role is not null ? role : BitMessageBarTests.GetRole(type);

        Assert.AreEqual(expectedRole, textEl.GetAttribute("role"));
    }

    private static string GetRole(BitMessageBarType type)
    {
        return type switch
        {
            BitMessageBarType.Blocked or BitMessageBarType.Error or BitMessageBarType.SevereWarning => "alert",
            _ => "status",
        };
    }
}
