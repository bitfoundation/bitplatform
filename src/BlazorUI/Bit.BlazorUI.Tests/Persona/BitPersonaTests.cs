﻿using System;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Persona;

[TestClass]
public class BitPersonaTests : BunitTestContext
{
    [DataTestMethod,
         DataRow(true),
         DataRow(false)
    ]
    public void BitPersonaTest(bool isEnabled)
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var persona = component.Find(".bit-prs");

        if (isEnabled)
        {
            Assert.IsFalse(persona.ClassList.Contains("bit-dis"));
        }
        else
        {
            Assert.IsTrue(persona.ClassList.Contains("bit-dis"));
        }
    }

    [DataTestMethod,
        DataRow("Text", "SecondaryText", "TertiaryText", "OptionalText"),
        DataRow(null, null, null, null)
    ]
    public void BitPersonaShouldAddCorrectDetailsText(string text, string secondaryText, string tertiaryText, string optionalText)
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.Text, text);
            parameters.Add(p => p.SecondaryText, secondaryText);
            parameters.Add(p => p.TertiaryText, tertiaryText);
            parameters.Add(p => p.OptionalText, optionalText);
        });

        var primaryEl = component.Find(".bit-prs-ptx");
        var secondaryEl = component.Find(".bit-prs-stx");
        var tertiaryTextEl = component.Find(".bit-prs-ttx");
        var optionalTextEl = component.Find(".bit-prs-otx");

        Assert.AreEqual(text, primaryEl.TextContent.HasValue() ? primaryEl.TextContent : null);
        Assert.AreEqual(secondaryText, secondaryEl.TextContent.HasValue() ? secondaryEl.TextContent : null);
        Assert.AreEqual(tertiaryText, tertiaryTextEl.TextContent.HasValue() ? tertiaryTextEl.TextContent : null);
        Assert.AreEqual(optionalText, optionalTextEl.TextContent.HasValue() ? optionalTextEl.TextContent : null);
    }

    [DataTestMethod,
        DataRow(BitPersonaPresenceStatus.Blocked, true),
        DataRow(BitPersonaPresenceStatus.Blocked, false),
        DataRow(BitPersonaPresenceStatus.Offline, true),
        DataRow(BitPersonaPresenceStatus.Offline, false),
        DataRow(BitPersonaPresenceStatus.Away, true),
        DataRow(BitPersonaPresenceStatus.Away, false),
        DataRow(BitPersonaPresenceStatus.Online, true),
        DataRow(BitPersonaPresenceStatus.Online, false),
        DataRow(BitPersonaPresenceStatus.Busy, true),
        DataRow(BitPersonaPresenceStatus.Busy, false),
        DataRow(BitPersonaPresenceStatus.Dnd, true),
        DataRow(BitPersonaPresenceStatus.Dnd, false)
    ]
    public void BitPersonaPresenceStatusClassNameTest(BitPersonaPresenceStatus presenceStatus, bool isOutOfOffice)
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.Presence, presenceStatus);
            parameters.Add(p => p.IsOutOfOffice, isOutOfOffice);
        });

        var presenceStatusClassName = presenceStatus switch
        {
            BitPersonaPresenceStatus.Online => "presence_available",
            BitPersonaPresenceStatus.Busy => "presence_busy",
            BitPersonaPresenceStatus.Away => isOutOfOffice ? "presence_oof" : "presence_away",
            BitPersonaPresenceStatus.Dnd => "presence_dnd",
            BitPersonaPresenceStatus.Offline => isOutOfOffice ? "presence_oof" : "presence_offline",
            _ => "presence_unknown",
        };

        var personaStatus = component.Find(".bit-prs-pre > i");

        Assert.AreEqual($"bit-icon bit-icon--{presenceStatusClassName}", personaStatus.GetAttribute("class"));
    }

    [DataTestMethod,
        DataRow(BitPersonaSize.Size20),
        DataRow(BitPersonaSize.Size32),
        DataRow(BitPersonaSize.Size40),
        DataRow(BitPersonaSize.Size48),
        DataRow(BitPersonaSize.Size56),
        DataRow(BitPersonaSize.Size72),
        DataRow(BitPersonaSize.Size100),
        DataRow(BitPersonaSize.Size120)
    ]
    public void BitPersonaSizeClassNameTest(string size)
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.Size, size);
        });

        var persona = component.Find(".bit-prs");
        var personaSizeClass = $"bit-prs-{size}";

        Assert.IsTrue(persona.ClassList.Contains(personaSizeClass));
    }

    [DataTestMethod,
        DataRow("Image url"),
        DataRow(null)
    ]
    public void BitPersonaImageTest(string imageUrl)
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.ImageUrl, imageUrl);
        });

        if (imageUrl.HasValue())
        {
            var personaImage = component.Find(".bit-prs-img");
            var imageSrc = personaImage.GetAttribute("src");

            Assert.AreEqual(imageUrl, imageSrc);
        }
    }

    [DataTestMethod,
        DataRow("Presence Title", BitPersonaPresenceStatus.Blocked),
        DataRow("Presence Title", BitPersonaPresenceStatus.Away),
        DataRow("Presence Title", BitPersonaPresenceStatus.Offline),
        DataRow("Presence Title", BitPersonaPresenceStatus.Online),
        DataRow("Presence Title", BitPersonaPresenceStatus.Dnd),
        DataRow("Presence Title", BitPersonaPresenceStatus.Busy)
    ]
    public void BitPersonaPresenceTitleTest(string presenceTitle, BitPersonaPresenceStatus presenceStatus)
    {
        var component = RenderComponent<BitPersona>(
            parameters =>
            {
                parameters.Add(p => p.PresenceTitle, presenceTitle);
                parameters.Add(p => p.Presence, presenceStatus);
            });

        var presenceTitleClassName = component.Find(".bit-prs-pre");
        var title = presenceTitleClassName.GetAttribute("title");

        Assert.AreEqual(presenceTitle, title);
    }
}
