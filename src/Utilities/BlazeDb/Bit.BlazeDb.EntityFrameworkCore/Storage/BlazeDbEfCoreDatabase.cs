using System.Linq.Expressions;
using Bit.BlazeDb.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;

namespace Bit.BlazeDb.EntityFrameworkCore.Storage;

/// <summary>
/// Applies a <c>SaveChanges</c> to the engine. Every entry goes into one BlazeDb transaction, so
/// the whole save either lands in memory and in the write-ahead log or leaves both untouched.
///
/// Rows are the live objects the queries handed out, which makes an update a subtlety rather than
/// a copy: by the time <c>SaveChanges</c> runs, the row inside the table has already been mutated
/// and its index entries describe values that no longer exist on it. The change tracker still
/// holds the original values, so the provider rebuilds them and hands both versions to the engine,
/// which retracts the old index entries before adding the new ones.
/// </summary>
internal sealed class BlazeDbEfCoreDatabase : IDatabase
{
    private readonly IBlazeDbTableCache _tables;
    private readonly IDbContextTransactionManager _transactions;
    private readonly ICurrentDbContext _currentContext;

    public BlazeDbEfCoreDatabase(IBlazeDbTableCache tables, IDbContextTransactionManager transactions, ICurrentDbContext currentContext)
    {
        _tables = tables;
        _transactions = transactions;
        _currentContext = currentContext;
    }

    public int SaveChanges(IList<IUpdateEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        if (entries.Count == 0)
        {
            return 0;
        }

        // The engine's transactions are ambient - while one is open every table write on the
        // database joins it - so a save made inside a transaction this context opened, or one the
        // application opened on the engine directly, becomes part of it instead of failing as a
        // nested one. It is still atomic: the save runs behind a savepoint, so one that fails
        // part-way takes back the entries it had already applied and leaves the outer transaction
        // as it found it, as a relational provider does with the savepoint it sets around each
        // SaveChanges. A transaction another context opened is a different matter: joining it
        // would report this save as done while leaving its fate to that context's commit or
        // rollback, with this tracker none the wiser. That is refused. The check, the join and the
        // save itself run under the engine's write lock, as RunInTransaction does, so a unit another
        // thread opens in the same transaction cannot interleave with this one; and when the save
        // fails, its exception - not one from undoing it - is the one EF sees.
        //
        // The tables are looked up first, outside that lock: finding the engine may wait on the engine
        // cache's gate for this store, whose holder - a context opening the store - goes on to take the
        // engine's write lock, so waiting on the gate while holding the lock could deadlock.
        var bindings = new IBlazeDbTableBinding[entries.Count];
        for (var i = 0; i < bindings.Length; i++)
        {
            bindings[i] = _tables.GetBinding(entries[i].EntityType);
        }
        var database = _tables.Database;
        var count = database.RunInTransactionCore(
            (Entries: entries, Bindings: bindings),
            static state => Apply(state.Entries, state.Bindings),
            joiner: _transactions);

        try
        {
            database.DeliverChanges();
        }
        catch (BlazeDbChangeHandlerException ex)
        {
            // The save committed; only a Changed handler failed while it was reported. Failing the save
            // here would leave the tracker believing nothing was written - entities still Added, and a
            // retry refused as a duplicate. So the save completes, EF accepts its changes, and the
            // handler's exception is rethrown once it has (see OnSavedChanges).
            DeferUntilSaved(ex);
        }
        return count;
    }

    private BlazeDbChangeHandlerException? _deferredHandlerError;

    // Subscribed for the one save that needs it rather than once for good: a pooled context clears its
    // events when it goes back to the pool, while this service - and a flag saying it had subscribed -
    // lives on into the next lease.
    private void DeferUntilSaved(BlazeDbChangeHandlerException error)
    {
        var context = _currentContext.Context;
        _deferredHandlerError = error;
        context.SavedChanges -= OnSavedChanges;
        context.SaveChangesFailed -= OnSaveChangesFailed;
        context.SavedChanges += OnSavedChanges;
        context.SaveChangesFailed += OnSaveChangesFailed;
    }

    private void OnSavedChanges(object? sender, SavedChangesEventArgs e)
    {
        if (TakeDeferredHandlerError() is { } error)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw(error);
        }
    }

    // A save that fails after the engine committed reports its own failure; the handler's error belongs to
    // that save and is dropped with it, rather than surfacing from a later, unrelated one.
    private void OnSaveChangesFailed(object? sender, SaveChangesFailedEventArgs e) => TakeDeferredHandlerError();

    private BlazeDbChangeHandlerException? TakeDeferredHandlerError()
    {
        var context = _currentContext.Context;
        context.SavedChanges -= OnSavedChanges;
        context.SaveChangesFailed -= OnSaveChangesFailed;
        return Interlocked.Exchange(ref _deferredHandlerError, null);
    }

    private static int Apply(IList<IUpdateEntry> entries, IBlazeDbTableBinding[] bindings)
    {
        var count = 0;
        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            var binding = bindings[i];
            var entity = entry.ToEntityEntry().Entity;
            try
            {
                switch (entry.EntityState)
                {
                    case EntityState.Added:
                        binding.Insert(entity);
                        break;
                    case EntityState.Modified:
                        binding.UpdateInPlace(entity, BlazeDbOriginalValueFactory.Create(entry));
                        break;
                    case EntityState.Deleted:
                        binding.Delete(entity, BlazeDbOriginalValueFactory.Create(entry));
                        break;
                    default:
                        continue;
                }
            }
            catch (BlazeDbStaleRowException ex)
            {
                // The originals this tracker holds describe a state the row has moved past - another
                // context saved it in between - which is what a concurrency exception is for.
                throw new DbUpdateConcurrencyException(
                    "The row was changed by another context after this one loaded it, so its original " +
                    "values no longer describe the database. Reload the entity and apply the change again.",
                    ex, [entry]);
            }
            catch (KeyNotFoundException ex)
            {
                // An update or delete of a row that is no longer there: someone else deleted it after
                // this context loaded it. Relational providers report "0 rows affected" the same way.
                throw new DbUpdateConcurrencyException(
                    "The row this entity describes no longer exists in the database; another context " +
                    "deleted it after this one loaded it.",
                    ex, [entry]);
            }
            catch (Exception ex) when (ex is BlazeDbDuplicateKeyException or BlazeDbUniqueConstraintViolationException)
            {
                // Constraint violations are what DbUpdateException is for, and what callers catch;
                // the engine's own exception stays available as InnerException.
                throw new DbUpdateException(
                    "Saving the entity violated a BlazeDb constraint. See the inner exception for details.",
                    ex, [entry]);
            }
            count++;
        }
        return count;
    }

    /// <summary>
    /// Saves synchronously and returns a completed task. The engine's writes are memory operations;
    /// persistence is the write-ahead log's job and happens on its own schedule, so there is
    /// nothing here to await. Call <c>BlazeDbDatabase.FlushAsync</c> for a hard durability point.
    /// </summary>
    public Task<int> SaveChangesAsync(IList<IUpdateEntry> entries, CancellationToken cancellationToken = default) =>
        cancellationToken.IsCancellationRequested
            ? Task.FromCanceled<int>(cancellationToken)
            : Task.FromResult(SaveChanges(entries));

    public Func<QueryContext, TResult> CompileQuery<TResult>(Expression query, bool async) => throw NoPipeline();

    public Expression<Func<QueryContext, TResult>> CompileQueryExpression<TResult>(Expression query, bool async) =>
        throw NoPipeline();

#if NET9_0
    // EF Core 9 alone passes the precompiled-query parameter set; 8 and 10 have only the overload above.
    public Expression<Func<QueryContext, TResult>> CompileQueryExpression<TResult>(Expression query, bool async, IReadOnlySet<string> nonNullableReferenceTypeParameters) =>
        throw NoPipeline();
#endif

    private static InvalidOperationException NoPipeline() =>
        new("BlazeDb executes queries through its own compiler and never builds an EF query pipeline. " +
            "Reaching this method means the provider's IQueryCompiler was replaced.");
}
