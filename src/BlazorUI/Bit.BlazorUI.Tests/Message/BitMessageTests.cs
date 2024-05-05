using System.Collections.Generic;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Message;

[TestClass]
public class BitMessageTests : BunitTestContext
{
    [DataTestMethod,
        DataRow(BitSeverity.Info),
        DataRow(BitSeverity.Error),
        DataRow(BitSeverity.SevereWarning),
        DataRow(BitSeverity.Success),
        DataRow(BitSeverity.Warning)
    ]
    public void BitMessageShouldTakeCorrectType(BitSeverity type)
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Severity, type);
        });

        var bitMessage = component.Find(".bit-msb");

        var typeClass = type switch
        {
            BitSeverity.Info => "bit-msb-info",
            BitSeverity.Success => "bit-msb-success",
            BitSeverity.Warning => "bit-msb-warning",
            BitSeverity.SevereWarning => "bit-msb-severe-warning",
            BitSeverity.Error => "bit-msb-error",
            _ => "bit-msb-info"
        };

        Assert.IsTrue(bitMessage.ClassList.Contains(typeClass));

        var icon = component.Find(".bit-msb-ict > i");

        Dictionary<BitSeverity, string> IconMap = new()
        {
            [BitSeverity.Info] = "Info",
            [BitSeverity.Warning] = "Info",
            [BitSeverity.Error] = "ErrorBadge",
            [BitSeverity.SevereWarning] = "Warning",
            [BitSeverity.Success] = "Completed"
        };

        Assert.IsTrue(icon.ClassList.Contains($"bit-icon--{IconMap[type]}"));
    }

    [DataTestMethod,
        DataRow("Emoji2"),
        DataRow("WordLogo")
    ]
    public void BitMessageShouldRespectCustomIcon(string iconName)
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.IconName, iconName);
        });

        var icon = component.Find(".bit-msb-ict > i");
        Assert.IsTrue(icon.ClassList.Contains($"bit-icon--{iconName}"));
    }

    [DataTestMethod,
        DataRow(true, true),
        DataRow(true, false),
        DataRow(false, true),
        DataRow(false, false)
    ]
    public void BitMessageShouldRespectMultiline(bool isMultiline, bool truncated)
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Multiline, isMultiline);
            parameters.Add(p => p.Truncate, truncated);
        });

        var bitMessage = component.Find(".bit-msb > div");

        var messageBarMultilineType = isMultiline ? "bit-msb-mul" : "bit-msb-sin";
        Assert.IsTrue(bitMessage.ClassList.Contains(messageBarMultilineType));

        if (isMultiline is false && truncated)
        {
            var truncateButton = component.Find(".bit-msb-trn > button");
            var icon = component.Find(".bit-msb-trn button span i");

            Assert.IsTrue(icon.ClassList.Contains("bit-icon--DoubleChevronDown"));

            truncateButton.Click();

            Assert.IsTrue(icon.ClassList.Contains("bit-icon--DoubleChevronUp"));

            truncateButton.Click();

            Assert.IsTrue(icon.ClassList.Contains("bit-icon--DoubleChevronDown"));
        }
    }

    [DataTestMethod]
    public void BitMessageDismissButtonShouldWorkCorrect()
    {
        int currentCount = 0;
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.OnDismiss, () => currentCount++);
        });

        var dismissButton = component.Find(".bit-msb-dim > button");

        dismissButton.Click();

        Assert.AreEqual(1, currentCount);
    }

    [DataTestMethod,
        DataRow("Emoji2"),
        DataRow("WordLogo")
    ]
    public void BitMessageShouldRespectCustomDismissIcon(string iconName)
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.DismissIconName, iconName);
            parameters.Add(p => p.OnDismiss, () => { });
        });

        var icon = component.Find(".bit-msb-dim button span i");

        Assert.IsTrue(icon.ClassList.Contains($"bit-icon--{iconName}"));
    }

    [DataTestMethod,
        DataRow("<div><button>Action</button></div>")
    ]
    public void BitMessageShouldRespectAction(string actions)
    {
        var component = RenderComponent<BitMessage>(parameter =>
        {
            parameter.Add(p => p.Actions, actions);
        });

        var actionsTemplate = component.Find(".bit-msb-act").ChildNodes;
        actionsTemplate.MarkupMatches(actions);
    }

    [DataTestMethod,
        DataRow("alert", BitSeverity.Info),
        DataRow("alert", BitSeverity.Success),
        DataRow("alert", BitSeverity.Warning),
        DataRow("alert", BitSeverity.SevereWarning),
        DataRow("alert", BitSeverity.Error),

        DataRow(null, BitSeverity.Info),
        DataRow(null, BitSeverity.Success),
        DataRow(null, BitSeverity.Warning),
        DataRow(null, BitSeverity.SevereWarning),
        DataRow(null, BitSeverity.Error),
    ]
    public void BitMessageRoleTest(string role, BitSeverity type)
    {
        var component = RenderComponent<BitMessage>(parameter =>
        {
            parameter.Add(p => p.Role, role);
            parameter.Add(p => p.Severity, type);
        });

        var textEl = component.Find(".bit-msb-txt");
        var expectedRole = role is not null ? role : GetRole(type);

        Assert.AreEqual(expectedRole, textEl.GetAttribute("role"));
    }

    private static string GetRole(BitSeverity type)
     => type switch
     {
         BitSeverity.Error or BitSeverity.SevereWarning or BitSeverity.Warning => "alert",
         _ => "status",
     };
}
