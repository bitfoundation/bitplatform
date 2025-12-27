using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Notifications.Message;

[TestClass]
public class BitMessageTests : BunitTestContext
{
    [DataTestMethod,
        DataRow(BitColor.Info),
        DataRow(BitColor.Error),
        DataRow(BitColor.SevereWarning),
        DataRow(BitColor.Success),
        DataRow(BitColor.Warning)
    ]
    public void BitMessageShouldTakeCorrectType(BitColor type)
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Color, type);
        });

        var bitMessage = component.Find(".bit-msg");

        var typeClass = type switch
        {
            BitColor.Info => "bit-msg-inf",
            BitColor.Success => "bit-msg-suc",
            BitColor.Warning => "bit-msg-wrn",
            BitColor.SevereWarning => "bit-msg-swr",
            BitColor.Error => "bit-msg-err",
            _ => "bit-msg-inf"
        };

        Assert.IsTrue(bitMessage.ClassList.Contains(typeClass));

        var icon = component.Find(".bit-msg-ict > i");

        Dictionary<BitColor, string> iconMap = new()
        {
            [BitColor.Info] = "Info",
            [BitColor.Warning] = "Info",
            [BitColor.Error] = "ErrorBadge",
            [BitColor.SevereWarning] = "Warning",
            [BitColor.Success] = "Completed"
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
            parameters.Add(p => p.DismissIcon, iconName);
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
        DataRow("alert", BitColor.Info),
        DataRow("alert", BitColor.Success),
        DataRow("alert", BitColor.Warning),
        DataRow("alert", BitColor.SevereWarning),
        DataRow("alert", BitColor.Error),

        DataRow(null, BitColor.Info),
        DataRow(null, BitColor.Success),
        DataRow(null, BitColor.Warning),
        DataRow(null, BitColor.SevereWarning),
        DataRow(null, BitColor.Error),
    ]
    public void BitMessageRoleTest(string role, BitColor type)
    {
        var component = RenderComponent<BitMessage>(parameter =>
        {
            parameter.Add(p => p.Role, role);
            parameter.Add(p => p.Color, type);
        });

        var textEl = component.Find(".bit-msg-cnc");
        var expectedRole = role is not null ? role : GetRole(type);

        Assert.AreEqual(expectedRole, textEl.GetAttribute("role"));
    }

    private static string GetRole(BitColor type)
     => type switch
     {
         BitColor.Error or BitColor.SevereWarning or BitColor.Warning => "alert",
         _ => "status",
     };
}
