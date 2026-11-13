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

    private static readonly ConcurrentDictionary<Guid, (byte[] Content, string ContentType, int Uploads)> uploaded = [];

    /// <summary>How many pictures this user has uploaded, so the view's account notices a new one.</summary>
    public static int Uploads(Guid userId) => uploaded.TryGetValue(userId, out var picture) ? picture.Uploads : 0;

    public static void Keep(Guid userId, byte[] content, string contentType)
        => uploaded.AddOrUpdate(userId, _ => (content, contentType, 1), (_, previous) => (content, contentType, previous.Uploads + 1));

    /// <summary>The picture uploaded for this user, if any.</summary>
    public static (byte[] Content, string ContentType)? Find(Guid userId) => uploaded.TryGetValue(userId, out var picture) ? (picture.Content, picture.ContentType) : null;

    /// <summary>The demo user's own photo, as a path in the Workbench's wwwroot, or null for an id that is no demo user's.</summary>
    public static string? SampleOf(Guid userId) => WorkbenchClaims.UserOf(userId) is { } user ? WorkbenchPhotos.Find(user.NameKey) : null;
}
