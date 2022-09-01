using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Persona;

[TestClass]
public class BitPersonaTests : BunitTestContext
{
    [DataTestMethod,
     DataRow(Visual.Fluent, true),
     DataRow(Visual.Fluent, false),

     DataRow(Visual.Cupertino, true),
     DataRow(Visual.Cupertino, false),

     DataRow(Visual.Material, true),
     DataRow(Visual.Material, false)
   ]
    public void BitPersonaShouldTakeCorrectVisual(Visual visual, bool isEnabled)
    {
        var component = RenderComponent<BitPersonaTest>(parameters =>
        {
            parameters.Add(p => p.Visual, visual);
            parameters.Add(p => p.IsEnable, isEnabled);
        });

        var persona = component.Find(".bit-prs");

        var enabledClass = isEnabled ? "enabled" : "disabled";
        var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

        Assert.IsTrue(persona.ClassList.Contains($"bit-prs-{enabledClass}-{visualClass}"));
    }

    [DataTestMethod,
        DataRow("Text", "SecondaryText", "TertiaryText", "OptionalText"),
        DataRow(null, null, null, null)]
    public void BitPersonaShouldCurrectWorkText(string text, string secondaryText, string tertiaryText, string optionalText)
    {
        var component = RenderComponent<BitPersonaTest>(
               parameters =>
               {
                   parameters.Add(p => p.Text, text);
                   parameters.Add(p => p.SecondaryText, secondaryText);
                   parameters.Add(p => p.TertiaryText, tertiaryText);
                   parameters.Add(p => p.OptionalText, optionalText);
               });

        var textClassName = component.Find(".bit-prs-primary-text");
        var secendryTextClassName = component.Find(".bit-prs-secondary-text");
        var tertiaryTextClassName = component.Find(".bit-prs-tertiary-text");
        var optionalTextClassName = component.Find(".bit-prs-optional-text");

        Assert.AreEqual(text, textClassName.TextContent.HasValue() ? textClassName.TextContent : null);
        Assert.AreEqual(secondaryText, secendryTextClassName.TextContent.HasValue() ? secendryTextClassName.TextContent : null);
        Assert.AreEqual(tertiaryText, tertiaryTextClassName.TextContent.HasValue() ? tertiaryTextClassName.TextContent : null);
        Assert.AreEqual(optionalText, optionalTextClassName.TextContent.HasValue() ? optionalTextClassName.TextContent : null);
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
        DataRow(BitPersonaPresenceStatus.DND, true),
        DataRow(BitPersonaPresenceStatus.DND, false),]
    public void BitPersonaPresenceStatusClassNameTest(BitPersonaPresenceStatus presenceStatus, bool isOutOfOffice)
    {
        var component = RenderComponent<BitPersonaTest>(
              parameters =>
              {
                  parameters.Add(p => p.Presence, presenceStatus);
                  parameters.Add(p => p.IsOutOfOffice, isOutOfOffice);
              });

        var presenceStatusClassName = DetermineIcon(presenceStatus, isOutOfOffice);
        var personaStatus = component.Find(".bit-prs-presence > i");

        Assert.AreEqual($"bit-prs-icon bit-icon--{presenceStatusClassName}", personaStatus.GetAttribute("class"));
    }

    [DataTestMethod,
        DataRow(BitPersonaSize.Size20),
        DataRow(BitPersonaSize.Size32),
        DataRow(BitPersonaSize.Size40),
        DataRow(BitPersonaSize.Size48),
        DataRow(BitPersonaSize.Size56),
        DataRow(BitPersonaSize.Size72),
        DataRow(BitPersonaSize.Size100),
        DataRow(BitPersonaSize.Size120)]
    public void BitPersonaSizeClassNameTest(string size)
    {
        var component = RenderComponent<BitPersonaTest>(
             parameters =>
             {
                 parameters.Add(p => p.Size, size);
             });

        var persona = component.Find(".bit-prs");
        var personaSizeClass = $"bit-prs-{size}";

        Assert.IsTrue(persona.ClassList.Contains(personaSizeClass));
    }

    [DataTestMethod,
        DataRow("Image url"),
        DataRow(null)]
    public void BitPersonaImageTest(string imageurl)
    {
        var component = RenderComponent<BitPersonaTest>(
             parameters =>
             {
                 parameters.Add(p => p.ImageUrl, imageurl);
             });

        if (imageurl.HasValue())
        {
            var personaImage = component.Find(".bit-prs-img");
            var imageSrc = personaImage.GetAttribute("src");

            Assert.AreEqual(imageurl, imageSrc);
        }
    }

    [DataTestMethod,
        DataRow("Presence Title", BitPersonaPresenceStatus.Blocked),
        DataRow("Presence Title", BitPersonaPresenceStatus.Away),
        DataRow("Presence Title", BitPersonaPresenceStatus.Offline),
        DataRow("Presence Title", BitPersonaPresenceStatus.Online),
        DataRow("Presence Title", BitPersonaPresenceStatus.DND),
        DataRow("Presence Title", BitPersonaPresenceStatus.Busy),]
    public void BitPersonaPresenceTitleTest(string presenceTitle, BitPersonaPresenceStatus presenceStatus)
    {
        var component = RenderComponent<BitPersonaTest>(
            parameters =>
            {
                parameters.Add(p => p.PresenceTitle, presenceTitle);
                parameters.Add(p => p.Presence, presenceStatus);
            });

        var precenseTitleClassName = component.Find(".bit-prs-presence");
        var title = precenseTitleClassName.GetAttribute("title");

        Assert.AreEqual(presenceTitle, title);
    }
    private string DetermineIcon(BitPersonaPresenceStatus presence, bool isOutofOffice)
    {
        string oofIcon = "presence_oof";

        return presence switch
        {
            BitPersonaPresenceStatus.Online => "presence_available",
            BitPersonaPresenceStatus.Busy => "presence_busy",
            BitPersonaPresenceStatus.Away => isOutofOffice ? oofIcon : "presence_away",
            BitPersonaPresenceStatus.DND => "presence_dnd",
            BitPersonaPresenceStatus.Offline => isOutofOffice ? oofIcon : "presence_offline",
            _ => "presence_unknown",
        };
    }
}
