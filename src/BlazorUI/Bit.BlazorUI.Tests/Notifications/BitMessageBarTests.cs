using System.Collections.Generic;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Notifications;

[TestClass]
public class BitMessageBarTests : BunitTestContext
{
    [DataTestMethod,
        DataRow(Visual.Fluent, BitMessageBarType.Info),
        DataRow(Visual.Fluent, BitMessageBarType.Blocked),
        DataRow(Visual.Fluent, BitMessageBarType.Error),
        DataRow(Visual.Fluent, BitMessageBarType.SevereWarning),
        DataRow(Visual.Fluent, BitMessageBarType.Success),
        DataRow(Visual.Fluent, BitMessageBarType.Warning),

        DataRow(Visual.Cupertino, BitMessageBarType.Info),
        DataRow(Visual.Cupertino, BitMessageBarType.Blocked),
        DataRow(Visual.Cupertino, BitMessageBarType.Error),
        DataRow(Visual.Cupertino, BitMessageBarType.SevereWarning),
        DataRow(Visual.Cupertino, BitMessageBarType.Success),
        DataRow(Visual.Cupertino, BitMessageBarType.Warning),

        DataRow(Visual.Material, BitMessageBarType.Info),
        DataRow(Visual.Material, BitMessageBarType.Blocked),
        DataRow(Visual.Material, BitMessageBarType.Error),
        DataRow(Visual.Material, BitMessageBarType.SevereWarning),
        DataRow(Visual.Material, BitMessageBarType.Success),
        DataRow(Visual.Material, BitMessageBarType.Warning)
    ]
    public void BitMessageBarShouldTakeCorrectTypeAndVisual(Visual visual, BitMessageBarType messageBarType)
    {
        var component = RenderComponent<BitMessageBarTest>(
            parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.MessageBarType, messageBarType);
            });

        var bitMessageBar = component.Find(".bit-msg-bar");

        var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";
        var messageBarTypeClass = messageBarType == BitMessageBarType.SevereWarning ? "severe-warning" : messageBarType.ToString().ToLower();

        Assert.IsTrue(bitMessageBar.ClassList.Contains($"bit-msg-bar-{messageBarTypeClass}-{visualClass}"));

        var icon = component.Find(".bit-msg-bar-icon > i");

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

        var icon = component.Find(".bit-msg-bar-icon > i");
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

        var bitMessageBar = component.Find(".bit-msg-bar > div");

        string messageBarMultilineType = isMultiline ? "multiline" : "singleline";
        Assert.IsTrue(bitMessageBar.ClassList.Contains($"bit-msg-bar-{messageBarMultilineType}"));

        if (!isMultiline && truncated)
        {
            var truncateButton = component.Find(".bit-msg-bar-truncate > button");

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

        var dismissButton = component.Find(".bit-msg-bar-dismiss > button");
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

        var icon = component.Find(".bit-msg-bar-dismiss button span i");
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

        var dismissButton = component.Find(".bit-msg-bar-dismiss button");

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

        var dismissButton = component.Find(".bit-msg-bar-truncate button");

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

        var actionsTemplate = component.Find(".bit-msg-bar-actions").ChildNodes;
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

        var textEl = component.Find(".bit-msg-bar-text");
        var expectedRole = role is not null ? role : GetRole(type);
        Assert.AreEqual(expectedRole, textEl.GetAttribute("role"));
    }

    private string GetRole(BitMessageBarType type)
    {
        switch (type)
        {
            case BitMessageBarType.Blocked:
            case BitMessageBarType.Error:
            case BitMessageBarType.SevereWarning:
                return "alert";
        }
        return "status";
    }
}
