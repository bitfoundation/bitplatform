namespace Boilerplate.Shared.Features.Attachments;

public static class ImageUploadContentTypes
{
    public static readonly string[] All = ["image/jpeg", "image/png", "image/webp", "image/gif", "image/avif"];

    public static readonly string Accept = string.Join(',', All);
}
