using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Boilerplate.Server.Api.Features.PersonalData;

/// <summary>
/// One feature's answer to "what do you hold about this person, and what happens to it when they leave".
/// <para>
/// Articles 15, 20 and 17 enumerate the same stores, so they are answered from one list rather than two that agree by
/// habit: a feature with a personal-data table and no source here is a visible omission in review.
/// <see cref="Identity.Services.UserErasureService"/> still owns the transaction, the retry and the <c>Users</c> row
/// itself, which has to be last by construction rather than by an integer.
/// </para>
/// </summary>
public interface IPersonalDataSource
{
    /// <summary>
    /// Web defaults so the names match the rest of the API's json, and enums as names because the reader is a person
    /// to whom <c>"Gender": 1</c> means nothing.
    /// </summary>
    static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>Section name in <c>data.json</c>, and the folder under <c>files/</c> when the source has any.</summary>
    string Key { get; }

    /// <summary>
    /// Orders the sections in the export. Erasure uses <see cref="ErasureOrder"/> instead: an export reads in
    /// whatever order reads best, a delete has to respect the foreign keys between stores.
    /// </summary>
    int Order => 100;

    /// <summary>
    /// Order of this source's delete inside <c>UserErasureService</c>'s transaction. A store whose rows can only be
    /// found <em>through</em> another goes first - see <c>PushNotificationsPersonalDataSource.ErasureOrder</c>.
    /// </summary>
    int ErasureOrder => 100;

    /// <summary>Art. 15(1)(a) - why this store exists, written for the person reading the export.</summary>
    string Purpose { get; }

    /// <summary>Art. 15(1)(d) - how long it is kept, and what removes it.</summary>
    string Retention { get; }

    /// <summary>Art. 15(1)(c) - who else receives it. Empty when it never leaves the application's own stores.</summary>
    string[] Recipients => [];

    /// <summary>
    /// What this section cannot tell the reader. An export that quietly omits a store is worse than one that names it.
    /// </summary>
    string? Notes => null;

    /// <summary>Which erasure path reaches this store.</summary>
    PersonalDataErasure Erasure { get; }

    /// <summary>
    /// The section's data - usually the projection a query already returns, since nothing outside reads the shape.
    /// </summary>
    Task<JsonNode?> Export(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Blobs that belong in the zip beside <c>data.json</c>. Opened while the entry is written, never held in memory.
    /// </summary>
    Task<PersonalDataFile[]> ExportFiles(Guid userId, CancellationToken cancellationToken) => Task.FromResult<PersonalDataFile[]>([]);

    /// <summary>
    /// Read whatever the rows about to be deleted are the only record of - a blob path, a live connection id. Runs
    /// once and outside the retry, so what it captures survives a replayed transaction.
    /// </summary>
    Task PrepareErase(PersonalDataErasureContext context, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Delete this store's rows, inside the transaction and in <see cref="ErasureOrder"/>. Has to be idempotent:
    /// the execution strategy replays the whole delegate.
    /// </summary>
    Task Erase(PersonalDataErasureContext context, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// What no transaction can roll back: a blob, a CDN purge, a message to a device. After the commit, and a failure
    /// is logged rather than thrown - the account is gone, so the caller's sign-out still has to run.
    /// </summary>
    Task ErasePublished(PersonalDataErasureContext context, CancellationToken cancellationToken) => Task.CompletedTask;
}

/// <param name="UserId">The account being erased.</param>
/// <param name="ExceptSessionId">
/// A session that must NOT be told to sign out because it is the one asking and is already signing itself out (See
/// <c>DeleteAccountTab</c>). Null for management calls, whose caller is a different user.
/// </param>
public record PersonalDataErasureContext(Guid UserId, Guid? ExceptSessionId);

/// <summary>How an Article 17 request reaches a store. Reported in the export, so it is not just a policy claim.</summary>
public enum PersonalDataErasure
{
    /// <summary><c>UserErasureService</c> deletes it explicitly.</summary>
    ErasureService,

    /// <summary>The database cascade from the <c>Users</c> row that <c>UserErasureService</c> deletes takes it.</summary>
    CascadeFromUser,

    /// <summary>
    /// Only a retention job removes it, on its own schedule - so the privacy notice has to describe it: pressing
    /// "delete my account" does not.
    /// </summary>
    RetentionJobOnly
}

/// <param name="Name">File name inside <c>files/{Key}/</c>.</param>
/// <param name="OpenRead">Called while the entry is being written - nothing is read before the zip needs it.</param>
public record PersonalDataFile(string Name, Func<CancellationToken, Task<Stream>> OpenRead);
