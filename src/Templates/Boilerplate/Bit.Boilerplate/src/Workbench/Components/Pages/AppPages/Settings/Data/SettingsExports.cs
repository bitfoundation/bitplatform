//+:cnd:noEmit
using System.IO.Compression;
using System.Text.Json;
using Boilerplate.Shared.Features.Identity.Dtos;

namespace Boilerplate.Workbench.Components.Pages.AppPages.Settings.Data;

/// <summary>
/// The demo user's personal data export, the zip the privacy section downloads, laid out as the server's
/// (PersonalDataExportService): a data.json of sections, each saying why the data is kept and for how long, and the files.
/// It holds the view's account as it is now, with what the page changed, as the server's holds what it has stored.
/// </summary>
public static class SettingsExports
{
    private static readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public static string FileNameOf(Guid userId, DateTimeOffset exportedOn) => $"personal-data-{userId}-{exportedOn:yyyy-MM-dd}.zip";

    public static byte[] PersonalData(UserDto user, IEnumerable<UserSessionDto> sessions, DateTimeOffset exportedOn, string? photoPath)
    {
        // photoPath is the demo user's own photo on disk, which the profile shows until a picture is uploaded.
        var picture = user.HasProfilePicture ? WorkbenchProfilePictures.Find(user.Id) : null;
        var pictureFile = user.HasProfilePicture is false ? null
                        : picture is { } uploaded ? $"files/attachments/profile-picture{ExtensionOf(uploaded.ContentType)}"
                        : photoPath is null ? null : "files/attachments/profile-picture.svg";

        var export = new
        {
            exportedOn,
            subjectUserId = user.Id,
            notice = "Personal data held by this application about the account named in subjectUserId. Credentials (password hash, two-factor secret, push encryption keys) are deliberately excluded: handing them out would weaken the account without telling the reader anything about themselves.",
            sections = new Dictionary<string, object>
            {
                ["account"] = new
                {
                    purpose = "Identifying you, letting you sign in, and contacting you about your account.",
                    retention = "For as long as the account exists. An account that is never confirmed is deleted 48 hours after it was created.",
                    recipients = Array.Empty<string>(),
                    erasedOnAccountDeletion = true,
                    erasurePath = "ErasureService",
                    data = new { user.Id, user.UserName, user.Email, user.PhoneNumber, user.FullName, user.Gender, user.BirthDate },
                    files = Array.Empty<string>()
                },
                ["sessions"] = new
                {
                    purpose = "Keeping you signed in on each device, showing you where your account is signed in, and letting you sign a device out remotely. An external application you authorized holds one of these too, which is how you can see it and take its access away.",
                    retention = "14 days after a session was last renewed - 7 for one held by an external application - then deleted by a daily job. Signing a device out, or revoking an application, deletes its session immediately.",
                    //#if (cloudflare == true)
                    recipients = new[] { "Cloudflare - sits in front of the application and supplies the country and city recorded below." },
                    //#else
                    //#if (IsInsideProjectTemplate == true)
                    /*
                    //#endif
                    recipients = Array.Empty<string>(),
                    //#if (IsInsideProjectTemplate == true)
                    */
                    //#endif
                    //#endif
                    erasedOnAccountDeletion = true,
                    erasurePath = "ErasureService",
                    data = sessions.Select(session => new { session.Id, session.IP, session.Address, session.DeviceInfo, session.RenewedOnDateTimeOffset, session.OAuthClientName }),
                    files = Array.Empty<string>()
                },
                ["attachments"] = new
                {
                    purpose = "Showing your profile picture in the application.",
                    retention = "For as long as the account exists. Deleting your profile picture, or your account, removes both the row and the stored image.",
                    recipients = Array.Empty<string>(),
                    erasedOnAccountDeletion = true,
                    erasurePath = "ErasureService",
                    data = new { hasProfilePicture = pictureFile is not null },
                    files = pictureFile is null ? [] : new[] { pictureFile }
                }
            }
        };

        using var buffer = new MemoryStream();

        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            using (var data = archive.CreateEntry("data.json", CompressionLevel.Optimal).Open())
            {
                JsonSerializer.Serialize(data, export, jsonOptions);
            }

            if (pictureFile is not null)
            {
                using var file = archive.CreateEntry(pictureFile, CompressionLevel.NoCompression).Open();
                file.Write(picture?.Content ?? File.ReadAllBytes(photoPath!));
            }
        }

        return buffer.ToArray();
    }

    private static string ExtensionOf(string contentType) => contentType switch
    {
        "image/png" => ".png",
        "image/webp" => ".webp",
        "image/gif" => ".gif",
        "image/avif" => ".avif",
        "image/bmp" => ".bmp",
        _ => ".jpg"
    };
}
