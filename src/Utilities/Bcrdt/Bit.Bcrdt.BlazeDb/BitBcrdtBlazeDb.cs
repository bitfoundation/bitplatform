using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bit.Bcrdt.BlazeDb;

/// <summary>Configures bit Bcrdt to keep its documents in a bit BlazeDb database.</summary>
public static class BitBcrdtBlazeDb
{
    /// <summary>
    /// Stores documents in an open BlazeDb database. See <see cref="BcrdtBlazeDbDocumentStore"/>.
    /// </summary>
    /// <param name="builder">The collaboration being configured.</param>
    /// <param name="database">
    /// The database to keep documents in, opened with <see cref="BcrdtBlazeDbDocumentStore.Table"/> registered.
    /// </param>
    /// <param name="options">How writes are made durable and serialized.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static BcrdtCollaborationBuilder UseBlazeDbStore(
        this BcrdtCollaborationBuilder builder,
        BlazeDbDatabase database,
        BcrdtBlazeDbDocumentStoreOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(database);

        return builder.UseStore(new BcrdtBlazeDbDocumentStore(database, options));
    }

    /// <summary>
    /// Stores documents in the application's BlazeDb database, resolved from its services when the store is first
    /// used. See <see cref="BcrdtBlazeDbDocumentStore"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The shape for a database that is opened asynchronously, the way a WebAssembly application opens one on
    /// OPFS - once, by a service it shares - and that the store therefore cannot be handed while the services
    /// are being registered:
    /// </para>
    /// <code>
    /// builder.Services.AddSingleton&lt;AppDatabase&gt;();
    /// builder.Services.AddBitBcrdtServices(collaboration => collaboration
    ///     .UseBlazeDbStore((services, _) => new(services.GetRequiredService&lt;AppDatabase&gt;().GetAsync())));
    /// </code>
    /// <para>
    /// Registered per scope, like the collaboration service that uses it, so the delegate may resolve a scoped
    /// database as well as a shared one. Every store on the same database shares one lock and one record
    /// counter, however many scopes create them.
    /// </para>
    /// </remarks>
    /// <param name="builder">The collaboration being configured.</param>
    /// <param name="database">
    /// Returns the database to keep documents in, opened with <see cref="BcrdtBlazeDbDocumentStore.Table"/>
    /// registered.
    /// </param>
    /// <param name="options">How writes are made durable and serialized.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static BcrdtCollaborationBuilder UseBlazeDbStore(
        this BcrdtCollaborationBuilder builder,
        Func<IServiceProvider, CancellationToken, ValueTask<BlazeDbDatabase>> database,
        BcrdtBlazeDbDocumentStoreOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(database);

        builder.Services.TryAddScoped<IBcrdtDocumentStore>(services =>
            new BcrdtBlazeDbDocumentStore(cancellationToken => database(services, cancellationToken), options));
        return builder;
    }
}
