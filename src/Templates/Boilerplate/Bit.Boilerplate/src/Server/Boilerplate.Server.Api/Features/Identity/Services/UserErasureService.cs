//+:cnd:noEmit
using FluentStorage.Storage;
using Boilerplate.Shared.Features.Attachments;

namespace Boilerplate.Server.Api.Features.Identity.Services;

/// <summary>
/// The one place an account is erased, so the user's own "delete my account" and an admin's "delete this user" cannot
/// cover different stores. <c>userManager.DeleteAsync</c> only reaches what cascades from the <c>Users</c> row; the
/// profile picture, its blob, the edge cache entry and the push subscription do not, and each survived it before.
/// </summary>
public partial class UserErasureService
{
    [AutoInject] private IStore blobStorage = default!;
    [AutoInject] private AppDbContext dbContext = default!;
    [AutoInject] private UserManager<User> userManager = default!;
    [AutoInject] private ILogger<UserErasureService> logger = default!;
    [AutoInject] private ResponseCacheService responseCacheService = default!;
    //#if (signalR == true)
    [AutoInject] private IHubContext<AppHub> appHubContext = default!;
    //#endif

    /// <summary>The kinds whose attachment id is the user id (See <c>AttachmentController.GetFilePath</c>).</summary>
    private static readonly AttachmentKind[] profileImageKinds = [AttachmentKind.UserProfileImageSmall, AttachmentKind.UserProfileImageOriginal];

    /// <summary>
    /// Removes the account, everything held under it and the live connections of its devices. The caller keeps only what
    /// concerns the current request: signing this principal out and clearing this response's cookie.
    /// </summary>
    /// <param name="exceptSessionId">
    /// A session that must NOT be told to sign out because it is the one asking and is already signing itself out (See
    /// <c>DeleteAccountTab</c>). Null for management calls, whose caller is a different user.
    /// </param>
    public async Task Erase(Guid userId, Guid? exceptSessionId, CancellationToken cancellationToken)
    {
        // Untracked: an instance tracked here would survive a retry of the delegate below in whatever state it left it.
        if (await dbContext.Users.AnyAsync(user => user.Id == userId, cancellationToken) is false)
            throw new ResourceNotFoundException().WithData("Reason", "User not found.");

        // Read before the rows go: the row is the only record of where the blob is.
        var blobPaths = await dbContext.Attachments
            .Where(att => att.Id == userId && profileImageKinds.Contains(att.Kind))
            .Select(att => att.Path)
            .ToArrayAsync(cancellationToken);

        //#if (signalR == true)
        // Same reason. WhereIf rather than an inline comparison: a null there is SQL NULL, which matches no rows.
        var signalRConnectionIds = await dbContext.UserSessions
            .Where(us => us.UserId == userId && us.SignalRConnectionId != null)
            .WhereIf(exceptSessionId is not null, us => us.Id != exceptSessionId)
            .Select(us => us.SignalRConnectionId!)
            .ToArrayAsync(cancellationToken);
        //#endif

        await dbContext.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            //#if (notification == true)
            // Ahead of the sessions: their SetNull cascade would leave no UserSessionId to find these rows by. An
            // EXISTS subquery rather than a navigation, which ExecuteDelete translates differently per provider.
            await dbContext.PushNotificationSubscriptions
                .Where(sub => dbContext.UserSessions.Any(us => us.Id == sub.UserSessionId && us.UserId == userId))
                .ExecuteDeleteAsync(cancellationToken);
            //#endif

            await dbContext.Attachments
                .Where(att => att.Id == userId && profileImageKinds.Contains(att.Kind))
                .ExecuteDeleteAsync(cancellationToken);

            await dbContext.UserSessions
                .Where(us => us.UserId == userId)
                .ExecuteDeleteAsync(cancellationToken);

            // Re-read inside the delegate: on a retry the instance from the failed attempt is still tracked, already
            // marked Deleted and carrying a stale concurrency stamp.
            var userToDelete = await userManager.FindByIdAsync(userId.ToString()) ?? throw new ResourceNotFoundException();

            var result = await userManager.DeleteAsync(userToDelete);

            if (result.Succeeded is false)
                throw new ResourceValidationException(result.Errors.Select(err => new LocalizedString(err.Code, err.Description)).ToArray());

            await transaction.CommitAsync(cancellationToken);
        });

        //#if (signalR == true)
        // Ahead of the blobs: this is what the user's other devices are waiting on. See AppHub's comments.
        await appHubContext.Clients.Clients(signalRConnectionIds).Publish(SharedAppMessages.SESSION_REVOKED, null, cancellationToken);
        //#endif

        await ErasePublishedBlobs(userId, blobPaths, cancellationToken);

        // The only evidence the request was carried out.
        logger.LogInformation("Erased user {UserId} along with {BlobCount} attachment blob(s).", userId, blobPaths.Length);
    }

    /// <summary>
    /// Blobs first, then the edge - purging first lets the next request re-populate it from the origin. Both after the
    /// commit, like <c>ProductController.Delete</c>: the other way round, a failed transaction leaves a live account
    /// whose picture is gone and whose <c>HasProfilePicture</c> is true, which nothing can then clear.
    /// <para>
    /// A failure is logged rather than thrown - the account is already gone, so the caller's sign-out still has to run
    /// and a retry could only answer 404. What is left is one file, named at Critical for an orphaned-blob sweep.
    /// </para>
    /// </summary>
    private async Task ErasePublishedBlobs(Guid userId, string?[] blobPaths, CancellationToken cancellationToken)
    {
        if (blobPaths.Length is 0)
            return;

        try
        {
            foreach (var blobPath in blobPaths.OfType<string>())
            {
                if (await blobStorage.ObjectExists(blobPath, cancellationToken) is false)
                    continue;

                await blobStorage.DeleteObject(blobPath, cancellationToken);
            }

            await responseCacheService.PurgeUserProfileImagesCache(userId);
        }
        catch (Exception exp)
        {
            logger.LogCritical(exp, "User {UserId} was erased, but the attachment blob(s) at {BlobPaths} could not be removed or purged from the edge cache. They are now referenced by no row and have to be deleted by hand.",
                               userId, string.Join(", ", blobPaths));
        }
    }
}
