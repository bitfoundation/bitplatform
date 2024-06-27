using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Notifications.Persona;

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
        DataRow("PrimaryText", "SecondaryText", "TertiaryText", "OptionalText"),
        DataRow(null, null, null, null)
    ]
    public void BitPersonaShouldAddCorrectDetailsText(string primaryText, string secondaryText, string tertiaryText, string optionalText)
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.PrimaryText, primaryText);
            parameters.Add(p => p.SecondaryText, secondaryText);
            parameters.Add(p => p.TertiaryText, tertiaryText);
            parameters.Add(p => p.OptionalText, optionalText);
        });

        var primaryEl = component.Find(".bit-prs-ptx");
        var secondaryEl = component.Find(".bit-prs-stx");
        var tertiaryTextEl = component.Find(".bit-prs-ttx");
        var optionalTextEl = component.Find(".bit-prs-otx");

        Assert.AreEqual(primaryText, primaryEl.TextContent.HasValue() ? primaryEl.TextContent : null);
        Assert.AreEqual(secondaryText, secondaryEl.TextContent.HasValue() ? secondaryEl.TextContent : null);
        Assert.AreEqual(tertiaryText, tertiaryTextEl.TextContent.HasValue() ? tertiaryTextEl.TextContent : null);
        Assert.AreEqual(optionalText, optionalTextEl.TextContent.HasValue() ? optionalTextEl.TextContent : null);
    }

    [DataTestMethod,
        DataRow(BitPersonaSize.Size8),
        DataRow(BitPersonaSize.Size32),
        DataRow(BitPersonaSize.Size40),
        DataRow(BitPersonaSize.Size48),
        DataRow(BitPersonaSize.Size56),
        DataRow(BitPersonaSize.Size72),
        DataRow(BitPersonaSize.Size100),
        DataRow(BitPersonaSize.Size120)
    ]
    public void BitPersonaSizeClassNameTest(BitPersonaSize size)
    {
        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.Size, size);
        });

        var persona = component.Find(".bit-prs");
        var personaSizeClass = $"bit-prs-{size.ToString().ToLower().Replace("size", "s")}";

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
        DataRow("Presence Title", BitPersonaPresence.Blocked),
        DataRow("Presence Title", BitPersonaPresence.Away),
        DataRow("Presence Title", BitPersonaPresence.Offline),
        DataRow("Presence Title", BitPersonaPresence.Online),
        DataRow("Presence Title", BitPersonaPresence.Dnd),
        DataRow("Presence Title", BitPersonaPresence.Busy)
    ]
    public void BitPersonaPresenceTitleTest(string presenceTitle, BitPersonaPresence presenceStatus)
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
