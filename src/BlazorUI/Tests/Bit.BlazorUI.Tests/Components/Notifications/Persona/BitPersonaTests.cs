using System.Collections.Generic;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Notifications.Persona;

[TestClass]
public class BitPersonaTests : BunitTestContext
{
    [TestMethod,
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

    [TestMethod,
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

    [TestMethod,
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

    [TestMethod,
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

    [TestMethod,
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

    [TestMethod]
    public void BitPersonaShouldRenderActionIconCssClassesFromBitIconInfo()
    {
        var actionIcon = new BitIconInfo("camera", "fa", "fa-");

        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.OnActionClick, EventCallback.Factory.Create<MouseEventArgs>(this, () => { }));
            parameters.Add(p => p.ActionIcon, actionIcon);
            parameters.Add(p => p.Size, BitPersonaSize.Size120);
        });

        var iconEl = component.Find(".bit-prs-aic");

        Assert.IsTrue(iconEl.ClassList.Contains("fa"));
        Assert.IsTrue(iconEl.ClassList.Contains("fa-camera"));
    }

    [TestMethod]
    public void BitPersonaActionIconShouldTakePrecedenceOverActionIconName()
    {
        var actionIcon = BitIconInfo.Bi("pencil-fill");

        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.OnActionClick, EventCallback.Factory.Create<MouseEventArgs>(this, () => { }));
            parameters.Add(p => p.ActionIcon, actionIcon);
            parameters.Add(p => p.ActionIconName, "Edit");
            parameters.Add(p => p.Size, BitPersonaSize.Size120);
        });

        var iconEl = component.Find(".bit-prs-aic");

        Assert.IsTrue(iconEl.ClassList.Contains("bi"));
        Assert.IsTrue(iconEl.ClassList.Contains("bi-pencil-fill"));
        Assert.IsFalse(iconEl.ClassList.Contains("bit-icon"));
    }

    [TestMethod,
        DataRow(BitPersonaPresence.Online, "check-circle-fill"),
        DataRow(BitPersonaPresence.Offline, "wifi-off"),
        DataRow(BitPersonaPresence.Away, "clock-fill"),
        DataRow(BitPersonaPresence.Dnd, "dash-circle-fill"),
        DataRow(BitPersonaPresence.Busy, "exclamation-circle-fill")
    ]
    public void BitPersonaShouldRenderPresenceIconCssClassesFromPresenceIcons(BitPersonaPresence presence, string iconName)
    {
        var iconsInfo = new Dictionary<BitPersonaPresence, BitIconInfo>
        {
            { BitPersonaPresence.Online, BitIconInfo.Bi("check-circle-fill") },
            { BitPersonaPresence.Offline, BitIconInfo.Bi("wifi-off") },
            { BitPersonaPresence.Away, BitIconInfo.Bi("clock-fill") },
            { BitPersonaPresence.Dnd, BitIconInfo.Bi("dash-circle-fill") },
            { BitPersonaPresence.Busy, BitIconInfo.Bi("exclamation-circle-fill") }
        };

        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.Presence, presence);
            parameters.Add(p => p.PresenceIcons, iconsInfo);
            parameters.Add(p => p.Size, BitPersonaSize.Size48);
        });

        var iconEl = component.Find(".bit-prs-pre i");

        Assert.IsTrue(iconEl.ClassList.Contains("bi"));
        Assert.IsTrue(iconEl.ClassList.Contains($"bi-{iconName}"));
    }

    [TestMethod]
    public void BitPersonaPresenceIconsShouldTakePrecedenceOverPresenceIconNames()
    {
        var iconsInfo = new Dictionary<BitPersonaPresence, BitIconInfo>
        {
            { BitPersonaPresence.Online, BitIconInfo.Bi("check-circle-fill") }
        };

        var icons = new Dictionary<BitPersonaPresence, string>
        {
            { BitPersonaPresence.Online, "SkypeCheck" }
        };

        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.Presence, BitPersonaPresence.Online);
            parameters.Add(p => p.PresenceIcons, iconsInfo);
            parameters.Add(p => p.PresenceIconNames, icons);
            parameters.Add(p => p.Size, BitPersonaSize.Size48);
        });

        var iconEl = component.Find(".bit-prs-pre i");

        Assert.IsTrue(iconEl.ClassList.Contains("bi"));
        Assert.IsTrue(iconEl.ClassList.Contains("bi-check-circle-fill"));
        Assert.IsFalse(iconEl.ClassList.Contains("bit-icon"));
    }

    [TestMethod]
    public void BitPersonaShouldFallbackToPresenceIconNamesWhenPresenceIconsDoesNotContainPresence()
    {
        var icons = new Dictionary<BitPersonaPresence, BitIconInfo>
        {
            // Only Online is defined in PresenceIcons; Offline is intentionally missing.
            { BitPersonaPresence.Online, BitIconInfo.Bi("check-circle-fill") }
        };

        var iconNames = new Dictionary<BitPersonaPresence, string>
        {
            // Offline is only defined in PresenceIconNames to verify the fallback behavior.
            { BitPersonaPresence.Offline, "SkypeMinus" }
        };

        var component = RenderComponent<BitPersona>(parameters =>
        {
            parameters.Add(p => p.Presence, BitPersonaPresence.Offline);
            parameters.Add(p => p.PresenceIcons, icons);
            parameters.Add(p => p.PresenceIconNames, iconNames);
            parameters.Add(p => p.Size, BitPersonaSize.Size48);
        });

        var iconEl = component.Find(".bit-prs-pre i");

        // When PresenceIcons has no mapping for the current Presence, the component
        // should fall back to PresenceIconNames, which uses the BitIcon-based rendering.
        Assert.IsTrue(iconEl.ClassList.Contains("bit-icon"));
        Assert.IsFalse(iconEl.ClassList.Contains("bi"));
    }

    [TestMethod]
    public void BitPersonaShouldRenderNoPresenceIconWhenNoMappingExists()
    {
        var icons = new Dictionary<BitPersonaPresence, BitIconInfo>
        {
            // Define a presence different from the one used in the test to keep it unmapped.
            { BitPersonaPresence.Online, BitIconInfo.Bi("check-circle-fill") }
        };

        var iconNames = new Dictionary<BitPersonaPresence, string>();
        
        var component = RenderComponent<BitPersona>(parameters =>
        {
            // Use a presence that is not present in either dictionary.
            parameters.Add(p => p.Presence, BitPersonaPresence.Busy);
            parameters.Add(p => p.PresenceIcons, icons);
            parameters.Add(p => p.PresenceIconNames, iconNames);
            parameters.Add(p => p.Size, BitPersonaSize.Size48);
        });
        
        var iconElements = component.FindAll(".bit-prs-pre i");
        
        // When neither PresenceIcons nor PresenceIconNames contains the current Presence,
        // no presence icon should be rendered.
        Assert.IsEmpty(iconElements);
    }
}
