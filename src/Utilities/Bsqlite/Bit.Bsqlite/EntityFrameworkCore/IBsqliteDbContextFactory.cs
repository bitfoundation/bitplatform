using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace Bit.Bsqlite;

/// <summary>
/// Creates contexts bound to the application's <see cref="BsqliteDatabase"/>, opening it on
/// first use and - when asked - applying migrations once per session.
/// </summary>
/// <typeparam name="TContext">A context with a constructor taking <see cref="DbContextOptions{TContext}"/>.</typeparam>
/// <remarks>
/// Opening a database is asynchronous in the browser, so the factory's main entry point is
/// <see cref="CreateDbContextAsync"/>. The synchronous <c>CreateDbContext</c> works only once an
/// earlier <see cref="CreateDbContextAsync"/> has opened the session and run the first-use step on
/// it, and says so otherwise.
/// </remarks>
public interface IBsqliteDbContextFactory<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] TContext> : IDbContextFactory<TContext>
    where TContext : DbContext
{
    /// <summary>The database every context this factory creates is bound to.</summary>
    BsqliteDatabase Database { get; }

    /// <summary>Opens the database if needed, runs the configured first-use step, and returns a context.</summary>
    new Task<TContext> CreateDbContextAsync(CancellationToken cancellationToken = default);
}
