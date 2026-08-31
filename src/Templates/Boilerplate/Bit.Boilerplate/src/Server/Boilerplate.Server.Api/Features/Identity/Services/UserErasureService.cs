using Boilerplate.Server.Api.Features.PersonalData;

namespace Boilerplate.Server.Api.Features.Identity.Services;

/// <summary>
/// The one place an account is erased, so the user's own "delete my account" and an admin's "delete this user" cannot
/// cover different stores. Each store's own deletes live in its <see cref="IPersonalDataSource"/> - the same list the
/// export is built from - leaving here only what no single feature can decide: the order, the shared transaction, and
/// the <c>Users</c> row itself, which has to go after everything hanging off it.
/// </summary>
public partial class UserErasureService
{
    [AutoInject] private AppDbContext dbContext = default!;
    [AutoInject] private UserManager<User> userManager = default!;
    [AutoInject] private ILogger<UserErasureService> logger = default!;
    [AutoInject] private IEnumerable<IPersonalDataSource> personalDataSources = default!;

    /// <summary>
    /// Removes the account, everything held under it and the live connections of its devices. The caller keeps only what
    /// concerns the current request: signing this principal out and clearing this response's cookie.
    /// </summary>
    /// <param name="userId">The account to erase.</param>
    /// <param name="exceptSessionId"><inheritdoc cref="PersonalDataErasureContext" path="/param[@name='ExceptSessionId']"/></param>
    /// <param name="cancellationToken">Cancels the reads and the transaction; the post-commit work is not undone by it.</param>
    public async Task Erase(Guid userId, Guid? exceptSessionId, CancellationToken cancellationToken)
    {
        // Untracked: an instance tracked here would survive a retry of the delegate below in whatever state it left it.
        if (await dbContext.Users.AnyAsync(user => user.Id == userId, cancellationToken) is false)
            throw new ResourceNotFoundException().WithData("Reason", "User not found.");

        PersonalDataErasureContext context = new(userId, exceptSessionId);

        var sources = personalDataSources.OrderBy(source => source.ErasureOrder).ThenBy(source => source.Key).ToArray();

        // Outside the execution strategy: what a source captures here has to outlive a replayed transaction, and the
        // row it reads is gone once the delete has run.
        foreach (var source in sources)
        {
            await source.PrepareErase(context, cancellationToken);
        }

        await dbContext.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            foreach (var source in sources)
            {
                await source.Erase(context, cancellationToken);
            }

            // Last, and re-read inside the delegate: on a retry the instance from the failed attempt is still tracked,
            // already marked Deleted and carrying a stale concurrency stamp.
            var userToDelete = await userManager.FindByIdAsync(userId.ToString()) ?? throw new ResourceNotFoundException();

            var result = await userManager.DeleteAsync(userToDelete);

            if (result.Succeeded is false)
                throw new ResourceValidationException(result.Errors.Select(err => new LocalizedString(err.Code, err.Description)).ToArray());

            await transaction.CommitAsync(cancellationToken);
        });

        foreach (var source in sources)
        {
            // One source's unreachable blob store must not stop the next from telling a device to sign out, and none
            // may throw: the account is already gone. See IPersonalDataSource.ErasePublished.
            try
            {
                // Not the request's token: the rows are already committed, so a caller that walks away mid-cleanup
                // would leave blobs behind that nothing references and nothing will come back for.
                await source.ErasePublished(context, CancellationToken.None);
            }
            catch (Exception exp)
            {
                logger.LogCritical(exp, "User {UserId} was erased, but the '{SourceKey}' source could not finish what happens after the commit. Whatever it names in its own log line is now referenced by no row.",
                                   userId, source.Key);
            }
        }

        // The only evidence the request was carried out.
        logger.LogInformation("Erased user {UserId} across {SourceCount} personal data source(s).", userId, sources.Length);
    }
}
