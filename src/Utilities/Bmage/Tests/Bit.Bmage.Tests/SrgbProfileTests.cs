using System.Buffers.Binary;
using System.Text;
using Bit.Bmage.Input;
using Bit.Bmage.Metadata;

namespace Bit.Bmage.Tests;

/// <summary>
/// Over a Server circuit, a JPEG or PNG with an sRGB profile is decoded on the server, and any other profile goes to
/// the browser, which converts its colours. Mistaking another profile for sRGB loads wrong colours, so only a profile
/// whose whole description is a known sRGB name counts.
/// </summary>
[TestClass]
public sealed class SrgbProfileTests
{
    [TestMethod]
    [DataRow("sRGB IEC61966-2.1", true)]
    [DataRow("sRGB built-in", true)]
    [DataRow("sRGB", true)]
    [DataRow("sRGB-elle-V2-g10.icc", false)]
    [DataRow("Linear sRGB", false)]
    [DataRow("Display P3", false)]
    [DataRow("Adobe RGB (1998)", false)]
    public void AVersion2ProfileIsKnownByItsWholeDescription(string description, bool srgb)
        => Assert.AreEqual(srgb, ImageProcessorFactory.IsSrgbProfile(Profile("RGB ", Version2Description(description))));

    [TestMethod]
    public void AVersion4ProfileIsKnownByAnyOfItsTranslations()
    {
        Assert.IsTrue(ImageProcessorFactory.IsSrgbProfile(Profile("RGB ", Version4Description("sRGB IEC61966-2.1"))));
        Assert.IsTrue(ImageProcessorFactory.IsSrgbProfile(Profile("RGB ", Version4Description("sRGB-Profil", "sRGB IEC61966-2.1"))));
        Assert.IsFalse(ImageProcessorFactory.IsSrgbProfile(Profile("RGB ", Version4Description("Display P3"))));
        Assert.IsFalse(ImageProcessorFactory.IsSrgbProfile(Profile("RGB ", Version4Description("sRGB linear"))));
    }

    [TestMethod]
    public void AProfileThatIsNotRgbOrIsCutShortIsNotSrgb()
    {
        Assert.IsFalse(ImageProcessorFactory.IsSrgbProfile(Profile("GRAY", Version2Description("sRGB IEC61966-2.1"))), "a grey profile");
        var whole = Profile("RGB ", Version2Description("sRGB IEC61966-2.1"));
        Assert.IsTrue(ImageProcessorFactory.IsSrgbProfile(whole));
        for (var length = 0; length < whole.Length; length++)
            Assert.IsFalse(ImageProcessorFactory.IsSrgbProfile(whole.AsSpan(0, length)), $"cut at {length} bytes");
        Assert.IsFalse(ImageProcessorFactory.IsSrgbProfile(Encoding.ASCII.GetBytes(new string('x', 40) + "sRGB IEC61966-2.1")), "not a profile");
    }

    /// <summary>A PNG profile past the size the reader inflates reads as none, but is still no sRGB profile.</summary>
    [TestMethod]
    public void APngProfileTooLargeToReadStillGoesToTheBrowser()
    {
        using var image = TestImages.Gradient(8, 8);
        var png = Codecs.Png.PngEncoder.Encode(image).Data;
        var srgb = PngContainer.WriteMetadata(png, new ImageMetadata(null, Profile("RGB ", Version2Description("sRGB IEC61966-2.1")), default, ImageFormat.Png));
        var oversized = PngContainer.WriteMetadata(png, new ImageMetadata(null, new byte[5 << 20], default, ImageFormat.Png));

        Assert.IsFalse(ImageProcessorFactory.HasWideGamutProfile(ImageFormat.Png, png), "no profile");
        Assert.IsFalse(ImageProcessorFactory.HasWideGamutProfile(ImageFormat.Png, srgb), "an sRGB profile");
        Assert.IsTrue(PngContainer.ReadMetadata(oversized).IccProfile.IsEmpty, "the profile is past the cap");
        Assert.IsTrue(ImageProcessorFactory.HasWideGamutProfile(ImageFormat.Png, oversized), "a profile too large to read");
    }

    /// <summary>A profile holding only its header and the description tag.</summary>
    private static byte[] Profile(string colourSpace, byte[] description)
    {
        const int tagOffset = 132 + 12;
        var profile = new byte[tagOffset + description.Length];
        BinaryPrimitives.WriteUInt32BigEndian(profile, (uint)profile.Length);
        Encoding.ASCII.GetBytes(colourSpace).CopyTo(profile, 16);
        Encoding.ASCII.GetBytes("acsp").CopyTo(profile, 36);
        BinaryPrimitives.WriteUInt32BigEndian(profile.AsSpan(128), 1);
        Encoding.ASCII.GetBytes("desc").CopyTo(profile, 132);
        BinaryPrimitives.WriteUInt32BigEndian(profile.AsSpan(136), tagOffset);
        BinaryPrimitives.WriteUInt32BigEndian(profile.AsSpan(140), (uint)description.Length);
        description.CopyTo(profile, tagOffset);
        return profile;
    }

    /// <summary>
    /// ICC version 2's textDescriptionType: the signature, 4 reserved bytes, the length of the ASCII text with its
    /// terminator and the text, then empty Unicode and ScriptCode parts.
    /// </summary>
    private static byte[] Version2Description(string text)
    {
        var tag = new byte[12 + text.Length + 1 + 8 + 3 + 67];
        Encoding.ASCII.GetBytes("desc").CopyTo(tag, 0);
        BinaryPrimitives.WriteUInt32BigEndian(tag.AsSpan(8), (uint)text.Length + 1);
        Encoding.ASCII.GetBytes(text).CopyTo(tag, 12);
        return tag;
    }

    /// <summary>
    /// ICC version 4's multiLocalizedUnicodeType: the signature, 4 reserved bytes, the record count and size, a record
    /// per translation (language, country, length and offset of its text), then the texts in big-endian UTF-16.
    /// </summary>
    private static byte[] Version4Description(params string[] translations)
    {
        var texts = translations.Select(Encoding.BigEndianUnicode.GetBytes).ToArray();
        var tag = new byte[16 + 12 * texts.Length + texts.Sum(t => t.Length)];
        Encoding.ASCII.GetBytes("mluc").CopyTo(tag, 0);
        BinaryPrimitives.WriteUInt32BigEndian(tag.AsSpan(8), (uint)texts.Length);
        BinaryPrimitives.WriteUInt32BigEndian(tag.AsSpan(12), 12);
        var offset = 16 + 12 * texts.Length;
        for (var i = 0; i < texts.Length; i++)
        {
            var record = tag.AsSpan(16 + 12 * i, 12);
            Encoding.ASCII.GetBytes("enUS").CopyTo(record);
            BinaryPrimitives.WriteUInt32BigEndian(record[4..], (uint)texts[i].Length);
            BinaryPrimitives.WriteUInt32BigEndian(record[8..], (uint)offset);
            texts[i].CopyTo(tag, offset);
            offset += texts[i].Length;
        }
        return tag;
    }
}
