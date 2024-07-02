using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Notifications.Message;

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

        var bitMessage = component.Find(".bit-msg");

        var typeClass = type switch
        {
            BitSeverity.Info => "bit-msg-inf",
            BitSeverity.Success => "bit-msg-suc",
            BitSeverity.Warning => "bit-msg-wrn",
            BitSeverity.SevereWarning => "bit-msg-swr",
            BitSeverity.Error => "bit-msg-err",
            _ => "bit-msg-inf"
        };

        Assert.IsTrue(bitMessage.ClassList.Contains(typeClass));

        var icon = component.Find(".bit-msg-ict > i");

        Dictionary<BitSeverity, string> iconMap = new()
        {
            [BitSeverity.Info] = "Info",
            [BitSeverity.Warning] = "Info",
            [BitSeverity.Error] = "ErrorBadge",
            [BitSeverity.SevereWarning] = "Warning",
            [BitSeverity.Success] = "Completed"
        };

        Assert.IsTrue(icon.ClassList.Contains($"bit-icon--{iconMap[type]}"));
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

        var icon = component.Find(".bit-msg-ict > i");
        Assert.IsTrue(icon.ClassList.Contains($"bit-icon--{iconName}"));
    }

    [DataTestMethod]
    public void BitMessageDismissButtonShouldWorkCorrect()
    {
        var currentCount = 0;
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.OnDismiss, () => currentCount++);
        });

        var dismissButton = component.Find(".bit-msg-dmb");

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

        var icon = component.Find(".bit-msg-dmi");

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

        var actionsTemplate = component.Find(".bit-msg-act").ChildNodes;
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

        var textEl = component.Find(".bit-msg-cnc");
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
