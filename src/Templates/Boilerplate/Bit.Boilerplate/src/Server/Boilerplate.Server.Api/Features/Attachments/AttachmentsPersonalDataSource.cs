//+:cnd:noEmit
using System.Text.Json;
using System.Text.Json.Nodes;
using FluentStorage.Storage;
using Boilerplate.Shared.Features.Attachments;
using Boilerplate.Server.Api.Features.PersonalData;

namespace Boilerplate.Server.Api.Features.Attachments;

/// <summary>
/// The profile photograph: its row, and the image itself as a file in the zip.
/// </summary>
public partial class AttachmentsPersonalDataSource : IPersonalDataSource
{
    [AutoInject] private IStore blobStorage = default!;
    [AutoInject] private AppDbContext dbContext = default!;
    [AutoInject] private ResponseCacheService responseCacheService = default!;
    [AutoInject] private ILogger<AttachmentsPersonalDataSource> logger = default!;

    /// <summary>Read by <see cref="PrepareErase"/>, because after the delete the row that names them is gone.</summary>
    private string[] blobPathsToErase = [];

    /// <summary>The kinds whose attachment id is the user id (See <c>AttachmentController.GetFilePath</c>).</summary>
    private static readonly AttachmentKind[] profileImageKinds = [AttachmentKind.UserProfileImageSmall, AttachmentKind.UserProfileImageOriginal];

    public string Key => "attachments";

    public int Order => 30;

    /// <summary>Before the sessions, only so the blob paths are read and the rows gone in one predictable pass.</summary>
    public int ErasureOrder => 20;

    public string Purpose => "Showing your profile picture in the application.";

    public string Retention => "For as long as the account exists. Deleting your profile picture, or your account, removes both the row and the stored image.";

    //#if (signalR == true)
    public string? Notes => "Images you attached to a message in the AI chat panel are not listed here: they are stored under a random identifier with no column recording who uploaded them, so they cannot be looked up by account. They are deleted 3 hours after upload by an hourly job.";
    //#endif

    public PersonalDataErasure Erasure => PersonalDataErasure.ErasureService;

    /// <summary>
    /// Metadata only: the bytes travel as files, since base64 in json is a third larger and opens in nothing.
    /// </summary>
    public async Task<JsonNode?> Export(Guid userId, CancellationToken cancellationToken)
    {
        // Materialised before mapping: the file name is built with Path.GetExtension, which no provider translates.
        var attachments = await dbContext.Attachments
            .AsNoTracking()
            .Where(attachment => attachment.Id == userId && profileImageKinds.Contains(attachment.Kind))
            .OrderBy(attachment => attachment.Kind)
            .Select(attachment => new { attachment.Kind, attachment.CreatedOn, attachment.Path })
            .ToArrayAsync(cancellationToken);

        var export = attachments.Select(attachment => new
        {
            attachment.Kind,
            attachment.CreatedOn,
            // Name of the image inside files/attachments/, or null when the stored file could not be found.
            File = attachment.Path is null ? null : BuildFileName(attachment.Kind, attachment.Path)
        });

        return JsonSerializer.SerializeToNode(export, IPersonalDataSource.SerializerOptions);
    }

    public async Task<PersonalDataFile[]> ExportFiles(Guid userId, CancellationToken cancellationToken)
    {
        var blobs = await dbContext.Attachments
            .AsNoTracking()
            .Where(attachment => attachment.Id == userId && profileImageKinds.Contains(attachment.Kind) && attachment.Path != null)
            .Select(attachment => new { attachment.Kind, Path = attachment.Path! })
            .ToArrayAsync(cancellationToken);

        List<PersonalDataFile> files = [];

        foreach (var blob in blobs)
        {
            // A missing blob is the residue of a failed post-commit erasure; skipped rather than failing the export,
            // and the row still appears in the metadata above.
            if (await blobStorage.ObjectExists(blob.Path, cancellationToken) is false)
                continue;

            files.Add(new(BuildFileName(blob.Kind, blob.Path), ct => blobStorage.OpenRead(blob.Path, ct)));
        }

        return [.. files];
    }

    /// <summary>The kind is the name, so the two profile images do not arrive as two unrelated files.</summary>
    private static string BuildFileName(AttachmentKind kind, string blobPath) => $"{kind}{Path.GetExtension(blobPath)}";

    public async Task PrepareErase(PersonalDataErasureContext context, CancellationToken cancellationToken)
    {
        blobPathsToErase = await dbContext.Attachments
            .Where(attachment => attachment.Id == context.UserId && profileImageKinds.Contains(attachment.Kind) && attachment.Path != null)
            .Select(attachment => attachment.Path!)
            .ToArrayAsync(cancellationToken);
    }

    public async Task Erase(PersonalDataErasureContext context, CancellationToken cancellationToken)
    {
        await dbContext.Attachments
            .Where(attachment => attachment.Id == context.UserId && profileImageKinds.Contains(attachment.Kind))
            .ExecuteDeleteAsync(cancellationToken);
    }

    /// <summary>
    /// Blobs then the edge, both after the commit like <c>ProductController.Delete</c>: the other way round, a failed
    /// transaction leaves a live account whose picture is gone and whose <c>HasProfilePicture</c> is still true.
    /// Caught here rather than by the caller, because the line that names the paths is the only record of what is now
    /// referenced by no row.
    /// </summary>
    public async Task ErasePublished(PersonalDataErasureContext context, CancellationToken cancellationToken)
    {
        if (blobPathsToErase.Length is 0)
            return;

        try
        {
            foreach (var blobPath in blobPathsToErase)
            {
                if (await blobStorage.ObjectExists(blobPath, cancellationToken) is false)
                    continue;

                await blobStorage.DeleteObject(blobPath, cancellationToken);
            }

            await responseCacheService.PurgeAttachmentCache(context.UserId);
        }
        catch (Exception exp)
        {
            logger.LogCritical(exp, "User {UserId} was erased, but the attachment blob(s) at {BlobPaths} could not be removed or purged from the edge cache. They are now referenced by no row and have to be deleted by hand.",
                               context.UserId, string.Join(", ", blobPathsToErase));
        }
    }
}
