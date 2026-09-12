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
}
