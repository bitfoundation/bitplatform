using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Client.Web.BlazorUI.Tests.Persona
{
    [TestClass]
    public class BitPersonaTests : BunitTestContext
    {
        [DataTestMethod,
            DataRow("secondaryText", "TertiaryText", "Text", "OptionalText"),
            DataRow("secondaryText", "TertiaryText", "Text", "OptionalText"),
            DataRow("secondaryText", "TertiaryText", "Text", "OptionalText"),
            DataRow("secondaryText", "TertiaryText", "Text", "OptionalText")]
        public void BitPersonaShouldCurrectWorkText(string secondaryText, string tertiaryText, string primarytext, string optionalText)
        {
            var component = RenderComponent<BitPersonaTest>(
                   parameters =>
                   {
                       parameters.Add(p => p.OptionalText, optionalText);
                       parameters.Add(p => p.Text, primarytext);
                       parameters.Add(p => p.TertiaryText, tertiaryText);
                       parameters.Add(p => p.SecondaryText, secondaryText);
                   });

            var secendryTextClassName = component.Find(".bit-Persona-secondaryText");
            var optionalTextClassName = component.Find(".bit-Persona-optionalText");
            var tertiaryTextClassName = component.Find(".bit-Persona-tertiaryText");
            var primaryTextClassName = component.Find(".bit-Persona-primaryTex");

            Assert.AreEqual(secondaryText, secendryTextClassName.TextContent);
            Assert.AreEqual(tertiaryText, tertiaryTextClassName.TextContent);
            Assert.AreEqual(optionalText, optionalTextClassName.TextContent);
            Assert.AreEqual(primarytext, primaryTextClassName.TextContent);
        }

        [DataTestMethod,
            DataRow(BitPersonaPresenceStatus.Blocked, true),
            DataRow(BitPersonaPresenceStatus.Offline, false),
            DataRow(BitPersonaPresenceStatus.Offline, true),
            DataRow(BitPersonaPresenceStatus.Away, false),
            DataRow(BitPersonaPresenceStatus.Away, true),
            DataRow(BitPersonaPresenceStatus.Online, false),
            DataRow(BitPersonaPresenceStatus.Busy, true),
            DataRow(BitPersonaPresenceStatus.DND, true),]
        public void BitPersonaPresenceStatusClassNameTest(BitPersonaPresenceStatus presenceStatus, bool isOutOfOffice)
        {
            var component = RenderComponent<BitPersonaTest>(
                  parameters =>
                  {
                      parameters.Add(p => p.Presence, presenceStatus);
                      parameters.Add(p => p.IsOutOfOffice, isOutOfOffice);
                  });

            var presenceStatusClassName = presenceStatus == BitPersonaPresenceStatus.Online ?
                "bit-persona-icon bit-icon--presence_available" :
                presenceStatus == BitPersonaPresenceStatus.Blocked ?
                "bit-persona-icon bit-icon--presence_unknown" :
                (presenceStatus == BitPersonaPresenceStatus.Offline || presenceStatus == BitPersonaPresenceStatus.Away) && isOutOfOffice == true ?
                "bit-persona-icon bit-icon--presence_oof" : $"bit-persona-icon bit-icon--presence_{presenceStatus.ToString().ToLower()}";
            var personaStatus = component.Find(".bit-Persona-presence > i");

            Assert.AreEqual(presenceStatusClassName, personaStatus.GetAttribute("class"));
        }

        [DataTestMethod,
            DataRow("20px"),
            DataRow("32px"),
            DataRow("40px"),
            DataRow("48px"),
            DataRow("56px"),
            DataRow("72px"),
            DataRow("100px"),
            DataRow("120px")]
        public void BitPersonaSizeClassNameTest(string size)
        {
            var component = RenderComponent<BitPersonaTest>(
                 parameters =>
                 {
                     parameters.Add(p => p.Size, size);
                 });

            var persona = component.Find(".bit-persona");
            var personaSizeClass = $"bit-persona-{size}";

            Assert.IsTrue(persona.ClassList.Contains(personaSizeClass));
        }

        [DataTestMethod, DataRow("Image url")]
        public void BitPersonaImageTest(string imageurl)
        {
            var component = RenderComponent<BitPersonaTest>(
                 parameters =>
                 {
                     parameters.Add(p => p.ImageUrl, imageurl);
                 });

            var personaImage = component.Find(".bit-persona-img-container > img");
            var imageSrc = personaImage.GetAttribute("src");

            Assert.AreEqual(imageurl, imageSrc);
        }
    }
}
