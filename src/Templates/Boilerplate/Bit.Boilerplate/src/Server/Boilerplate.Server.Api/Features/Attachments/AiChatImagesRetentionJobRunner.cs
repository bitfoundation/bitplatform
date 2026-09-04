//+:cnd:noEmit
using FluentStorage.Storage;
using Boilerplate.Shared.Features.Attachments;

namespace Boilerplate.Server.Api.Features.Attachments;

/// <summary>
/// Deletes images attached to AI chat messages once past <c>AiChatImagesRetention</c>.
/// </summary>
public partial class AiChatImagesRetentionJobRunner
{
    public const string RecurringJobId = nameof(AiChatImagesRetentionJobRunner);

    /// <summary>Bounds the work of one run; the job is hourly, so a backlog drains over successive runs.</summary>
    private const int MaxDeletionsPerRun = 100;

    [AutoInject] private IStore blobStorage = default!;
    [AutoInject] private AppDbContext dbContext = default!;
    [AutoInject] private TimeProvider timeProvider = default!;
    [AutoInject] private ServerApiSettings serverApiSettings = default!;
    [AutoInject] private ILogger<AiChatImagesRetentionJobRunner> logger = default!;

    public async Task EnforceRetention(CancellationToken cancellationToken)
    {
        var retention = serverApiSettings.AiChatImagesRetention;

        // Zero would expire every picture on upload; a misconfiguration, not a policy.
        if (retention <= TimeSpan.Zero)
            throw new InvalidOperationException($"{nameof(ServerApiSettings.AiChatImagesRetention)} must be greater than zero.");

        var expiredBefore = timeProvider.GetUtcNow() - retention;

        var expiredAttachments = await dbContext.Attachments
            .Where(att => att.Kind == AttachmentKind.AiChatImage && att.CreatedOn < expiredBefore)
            .OrderBy(att => att.CreatedOn)
            .Take(MaxDeletionsPerRun)
            .AsNoTracking()
            .ToArrayAsync(cancellationToken);

        if (expiredAttachments.Length is 0)
            return;

        var deletedIds = new List<Guid>(expiredAttachments.Length);

        foreach (var attachment in expiredAttachments)
        {
            // Blob first, row second - the opposite order to UserErasureService, because here the row exists ONLY to
            // say where the blob is, so dropping it first strands a file nothing can name again.
            try
            {
                if (attachment.Path is not null && await blobStorage.ObjectExists(attachment.Path, cancellationToken))
                {
                    await blobStorage.DeleteObject(attachment.Path, cancellationToken);
                }
            }
            catch (Exception exp)
            {
                // The row is kept on purpose: the pointer is what lets the next run retry. Dropping it would turn a
                // transient storage failure into a file kept forever.
                logger.LogError(exp, "Could not delete the expired AI chat image at {BlobPath}; its row is kept so the next run retries it.", attachment.Path);
                continue;
            }

            // Reached for an already-missing blob too, which is what makes the pass idempotent.
            deletedIds.Add(attachment.Id);
        }

        if (deletedIds.Count is 0)
            return;

        // ExecuteDelete rather than tracked removes: another worker may have read the same batch, and removing a row
        // that is already gone fails the whole run on a concurrency check.
        var deletedCount = await dbContext.Attachments
            .Where(att => att.Kind == AttachmentKind.AiChatImage && deletedIds.Contains(att.Id))
            .ExecuteDeleteAsync(cancellationToken);

        if (deletedCount > 0)
        {
            logger.LogInformation("Deleted {DeletedCount} expired AI chat image(s).", deletedCount);
        }
    }
}
