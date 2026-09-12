using ImageMagick;

namespace Boilerplate.Tests.E2E.Infrastructure;

/// <summary>
/// The pictures a test uploads. Generated for this suite, so no run ever puts a real person's photograph on a live
/// deployment - which matters most for the tests that then read it back out of a personal data export.
/// </summary>
public static class TestImages
{
    /// <summary>
    /// A 320 x 320 PNG. At least 256 x 256: UploadAttachment turns a smaller profile picture away with ImageTooSmall
    /// rather than blowing it up. AiChatImage resizes ShrinkOnly, so the same bytes serve there too.
    /// </summary>
    private const string pngBase64 = "iVBORw0KGgoAAAANSUhEUgAAAUAAAAFACAIAAABC8jL9AAACtklEQVR42u3TQQkAAAgEwYtjRCMayw7+hIFJsLCpHuCpSAAGBgwMGBgMDBgYMDBgYDAwYGDAwGBgwMCAgQEDg4EBAwMGBgwMBgYMDBgYDAwYGDAwYGAwMGBgwMCAgcHAgIEBA4OBAQMDBgYMDAYGDAwYGAysAhgYMDBgYDAwYGDAwICBwcCAgQEDg4EBAwMGBgwMBgYMDBgYMDAYGDAwYGAwMGBgwMCAgcHAgIEBAwMGBgMDBgYMDAYGDAwYGDAwGBgwMGBgMDBgYMDAgIHBwICBAQMDBgYDAwYGDAwGBgwMGBgwMBgYMDBgYMDAYGDAwICBwcCAgQEDAwYGAwMGBgwMGBgMDBgYMDAYGDAwYGDAwGBgwMCAgcHAgIEBAwMGBgMDBgYMDBgYDAwYGDAwGBgwMGBgwMBgYMDAgIEBA4OBAQMDBgYDAwYGDAwYGAwMGBgwMBhYBTAwYGDAwGBgwMCAgQEDg4EBAwMGBgMDBgYMDBgYDAwYGDAwYGAwMGBgwMBgYMDAgIEBA4OBAQMDBgYMDAYGDAwYGAwMGBgwMGBgMDBgYMDAYGDAwICBAQODgQEDAwYGDAwGBgwMGBgMDBgYMDBgYDAwYGDAwICBwcCAgQEDg4EBAwMGBgwMBgYMDBgYMDAYGDAwYGAwMGBgwMCAgcHAgIEBA4OBAQMDBgYMDAYGDAwYGDAwGBgwMGBgMDBgYMDAgIHBwICBAQMDBgYDAwYGDAwGBgwMGBgwMBgYMDBgYDCwBGBgwMCAgcHAgIEBAwMGBgMDBgYMDAYGDAwYGDAwGBgwMGBgwMBgYMDAgIHBwICBAQMDBgYDAwYGDAwYGAwMGBgwMBgYMDBgYMDAYGDAwICBwcAqgIEBAwMGBgMDBgYMDBgYDAwYGDAwGBgwMGBgwMBgYMDAgIEBA4OBAQMDNwsofaDvEBH6IQAAAABJRU5ErkJggg==";

    /// <summary>A new array per call: an upload hands its buffer to something that may take ownership of it.</summary>
    public static byte[] ProfilePicturePng() => Convert.FromBase64String(pngBase64);

    /// <summary>
    /// A 512 x 512 JPEG carrying what a phone photo arrives with: a GPS reference, the capture position and the
    /// software that wrote it. A JPEG because that is what a camera produces, and over 256 x 256 so the small kind
    /// does not turn it away.
    /// </summary>
    public static byte[] PhotoWithExifJpeg()
    {
        using var image = new MagickImage(MagickColors.Teal, 512, 512);

        var exif = new ExifProfile();
        exif.SetValue(ExifTag.Software, ExifSoftware);
        exif.SetValue(ExifTag.GPSLatitudeRef, "N");
        exif.SetValue(ExifTag.GPSLatitude, [new Rational(51), new Rational(30), new Rational(0)]);
        image.SetProfile(exif);

        return image.ToByteArray(MagickFormat.Jpeg);
    }

    /// <summary>The tag a test looks for in the bytes a deployment serves back, to name what it is that must be gone.</summary>
    public const string ExifSoftware = "boilerplate-e2e";
}
