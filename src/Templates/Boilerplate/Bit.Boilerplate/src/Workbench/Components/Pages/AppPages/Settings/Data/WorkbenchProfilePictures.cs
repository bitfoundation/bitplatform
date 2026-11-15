namespace Boilerplate.Workbench.Components.Pages.AppPages.Settings.Data;

/// <summary>
/// The demo users' profile pictures, served at the attachment address the app's pages ask for: the one uploaded on the
/// settings page since the Workbench started, or else the demo user's own photo. An upload is the Workbench's, not the
/// view's, so a screenshot of another view of the same user shows it too.
/// </summary>
public static class WorkbenchProfilePictures
{
    /// <summary>The settings page's own limit (ProfileSection's BitFileUpload).</summary>
    public const int MaxBytes = 10 * 1024 * 1024;

    private static readonly ConcurrentDictionary<Guid, (byte[] Content, string ContentType, string Upload)> uploaded = [];

    /// <summary>
    /// The type of an upload by its first bytes, as the server identifies the format rather than trusting what the upload
    /// says it is: one of the images the page accepts (ImageUploadContentTypes), or null for anything else.
    /// </summary>
    public static string? ImageTypeOf(ReadOnlySpan<byte> content) => content switch
    {
        [0x89, 0x50, 0x4E, 0x47, ..] => "image/png",
        [0xFF, 0xD8, 0xFF, ..] => "image/jpeg",
        [0x47, 0x49, 0x46, 0x38, ..] => "image/gif",
        [0x52, 0x49, 0x46, 0x46, _, _, _, _, 0x57, 0x45, 0x42, 0x50, ..] => "image/webp",
        [_, _, _, _, 0x66, 0x74, 0x79, 0x70, 0x61, 0x76, 0x69, 0x66 or 0x73, ..] => "image/avif",
        [0x42, 0x4D, ..] => "image/bmp",
        _ => null
    };

    /// <summary>
    /// The id of this user's latest upload, or null with none since the Workbench started, so a view's account notices one it
    /// hasn't shown, whichever view uploaded it and whatever the view saved before a restart.
    /// </summary>
    public static string? LastUpload(Guid userId) => uploaded.TryGetValue(userId, out var picture) ? picture.Upload : null;

    public static void Keep(Guid userId, byte[] content, string contentType) => uploaded[userId] = (content, contentType, Guid.NewGuid().ToString("N"));

    /// <summary>The picture uploaded for this user, if any.</summary>
    public static (byte[] Content, string ContentType)? Find(Guid userId) => uploaded.TryGetValue(userId, out var picture) ? (picture.Content, picture.ContentType) : null;

    /// <summary>The demo user's own photo, as a path in the Workbench's wwwroot, or null for an id that is no demo user's.</summary>
    public static string? SampleOf(Guid userId) => WorkbenchClaims.UserOf(userId) is { } user ? WorkbenchPhotos.Find(user.NameKey) : null;
}
