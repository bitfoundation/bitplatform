using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace Bit.Bsqlite;

/// <summary>EF entry point for table-level live queries.</summary>
/// <remarks>
/// <para>
/// Every run of the query - the first <c>RefreshAsync</c> and each refresh a write sets off - happens
/// in a context of its own, built like the one the query came from and disposed once the rows are
/// read. Two things depend on that. A refresh arrives whenever another tab writes, so on the
/// application's context it would collide with whatever the application starts there next - EF
/// allows one operation per context at a time. And a tracking query re-run on a context that already
/// tracks the rows hands back the instances it holds without their new values, so a live view would
/// pick up inserts and deletes but never an update.
/// </para>
/// <para>
/// The rows a live query returns are therefore current, and are not tracked by the application's
/// context. To change one, load it - or attach it - in the context that saves it.
/// </para>
/// </remarks>
public static partial class BsqliteLiveQueryExtensions
{
    /// <summary>
    /// Re-executes <paramref name="query"/> whenever a table it reads is written.
    /// </summary>
    public static IBsqliteLiveQuery<IReadOnlyList<T>> AsLiveQuery<T>(this IQueryable<T> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        return AsLiveQuery(query, ConnectionOf(query));
    }

    /// <summary>
    /// Re-executes <paramref name="query"/> whenever a table it reads is written.
    /// </summary>
    public static IBsqliteLiveQuery<IReadOnlyList<T>> AsLiveQuery<T>(
        this IQueryable<T> query,
        BsqliteConnection connection)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(connection);

        // The write notification arrives while SaveChanges is still consuming its result, inside the
        // transaction it wrote in. The gate defers the re-read until the save has finished, so it
        // never reads rows that may yet roll back. Null only for a query whose provider hides its
        // context, in which case the query also runs as given - there is no context to copy.
        var context = DbContextOf(query);
        var tables = TablesOf(query.ToQueryString(), context);
        var gate = context is null ? null : new BsqliteSaveChangesGate(context);
        var contexts = context is null ? null : new RefreshContexts(context);

        return new BsqliteLiveQuery<IReadOnlyList<T>>(
            connection,
            async ct =>
            {
                if (gate is not null)
                {
                    await gate.WaitForIdleAsync(ct).ConfigureAwait(false);
                }

                await using var own = contexts?.Create();
                if (own is null)
                {
                    return await query.ToListAsync(ct).ConfigureAwait(false);
                }

                // Query roots name an entity type, not a context, so the same expression runs on the
                // fresh context's provider - which is how a compiled query runs on any context.
                return await own.GetService<IAsyncQueryProvider>()
                    .CreateQuery<T>(query.Expression)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);
            },
            tables,
            onDispose: gate is null ? null : gate.Dispose);
    }

    /// <summary>
    /// The tables a write to which re-runs the query, or none - every write re-runs it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A write is reported by the tables it changed, as SQLite's update hook names them: base tables. A name the
    /// query reads that is no table of the context's model - a view (<c>ToView</c>), a view or table a raw
    /// <c>FromSql</c> query reads - is never among them, so a query reading one would never re-run. What such a
    /// name reads is not known here, so the query re-runs on every write instead.
    /// </para>
    /// <para>
    /// The names come from a coarse extractor, so what is plainly no table is set aside first: the text of
    /// string literals and comments (<c>'picked up from store'</c>), a table-valued function
    /// (<c>json_each(@ids)</c>, which EF reads a primitive collection with), and a common table expression,
    /// whose own body names the tables it reads. A common table expression named like a table of the model leaves
    /// it unknown whether the query also reads that table (<c>main."Products"</c>), so it re-runs on every write.
    /// </para>
    /// </remarks>
    internal static IReadOnlySet<string> TablesOf(string sql, DbContext? context)
    {
        var code = WithoutLiteralsAndComments(sql);
        var tables = BsqliteTableNames.Extract(code);
        if (tables.Count == 0)
        {
            return tables;
        }

        // Matched where the text of quoted identifiers is masked, so a keyword inside one - a column named
        // "From Lines (old)" - is not taken for a FROM calling a function; the name is read back from the code.
        var masked = WithQuotedIdentifiersMasked(code);

        var commonTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in CommonTableExpression().Matches(masked))
        {
            commonTables.Add(code.Substring(match.Groups[1].Index, match.Groups[1].Length));
        }

        var notTables = new HashSet<string>(commonTables, StringComparer.OrdinalIgnoreCase);
        foreach (Match match in TableFunction().Matches(masked))
        {
            notTables.Add(code.Substring(match.Groups[1].Index, match.Groups[1].Length));
        }

        var read = tables.Where(name => !notTables.Contains(name)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (read.Count == 0)
        {
            return read;
        }

        // Without the model there is no telling a table from anything else the extractor picked up.
        if (context is null)
        {
            return new HashSet<string>();
        }

        var mapped = context.Model.GetRelationalModel().Tables
            .Select(table => table.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // A common table expression named like a table hides it by that name, yet the query may still read the
        // table itself (main."Products") - the extractor sees one name either way, so which it reads is unknown.
        if (commonTables.Any(mapped.Contains))
        {
            return new HashSet<string>();
        }

        return read.All(mapped.Contains) ? read : new HashSet<string>();
    }

    /// <summary>
    /// <paramref name="sql"/> with the text of every string literal and comment blanked out, so a word in one is
    /// not taken for a table. Quoted identifiers are kept: they are the names.
    /// </summary>
    internal static string WithoutLiteralsAndComments(string sql)
    {
        var chars = sql.ToCharArray();
        var i = 0;
        while (i < chars.Length)
        {
            switch (chars[i])
            {
                case '\'':
                    // Up to the quote that ends it: '' inside a literal is a quote, blanked like the rest.
                    i++;
                    while (i < chars.Length)
                    {
                        if (chars[i] == '\'')
                        {
                            if (i + 1 < chars.Length && chars[i + 1] == '\'')
                            {
                                chars[i++] = ' ';
                                chars[i++] = ' ';
                                continue;
                            }

                            break;
                        }

                        chars[i++] = ' ';
                    }

                    i++;
                    break;

                case '"' or '`' or '[':
                    var close = chars[i] == '[' ? ']' : chars[i];
                    i++;
                    while (i < chars.Length && chars[i] != close)
                    {
                        i++;
                    }

                    i++;
                    break;

                case '-' when i + 1 < chars.Length && chars[i + 1] == '-':
                    while (i < chars.Length && chars[i] != '\n')
                    {
                        chars[i++] = ' ';
                    }

                    break;

                case '/' when i + 1 < chars.Length && chars[i + 1] == '*':
                    var end = sql.IndexOf("*/", i + 2, StringComparison.Ordinal);
                    end = end < 0 ? chars.Length : end + 2;
                    while (i < end)
                    {
                        chars[i++] = ' ';
                    }

                    break;

                default:
                    i++;
                    break;
            }
        }

        return new string(chars);
    }

    /// <summary>
    /// <paramref name="code"/> - already without literals and comments - with the text inside every quoted
    /// identifier replaced by <c>_</c>, so it holds no keyword, parenthesis or comma. The length and the quotes
    /// stay, so a match's position names the same text in <paramref name="code"/>.
    /// </summary>
    internal static string WithQuotedIdentifiersMasked(string code)
    {
        var chars = code.ToCharArray();
        var i = 0;
        while (i < chars.Length)
        {
            if (chars[i] is '"' or '`' or '[')
            {
                var close = chars[i] == '[' ? ']' : chars[i];
                i++;
                while (i < chars.Length && chars[i] != close)
                {
                    chars[i++] = '_';
                }
            }

            i++;
        }

        return new string(chars);
    }

    // A name a FROM or JOIN calls rather than reads: a table-valued function such as json_each or pragma_table_info.
    [GeneratedRegex(
        @"\b(?:from|join)\s+(?:[""`\[]?[A-Za-z_]\w*[""`\]]?\s*\.\s*)?[""`\[]?([A-Za-z_]\w*)[""`\]]?\s*\(",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex TableFunction();

    // The name a WITH clause gives a common table expression: WITH [RECURSIVE] x [(columns)] AS [[NOT] MATERIALIZED] (.
    [GeneratedRegex(
        @"(?:\bwith\s+(?:recursive\s+)?|,\s*)[""`\[]?([A-Za-z_]\w*)[""`\]]?\s*(?:\([^()]*\)\s*)?as\s+(?:(?:not\s+)?materialized\s+)?\(",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CommonTableExpression();

    /// <summary>
    /// Builds contexts of the same type and options as the one a live query came from, for its
    /// runs to happen in. See the class remarks.
    /// </summary>
    /// <remarks>
    /// Everything is captured up front, so a refresh never touches the application's context, which
    /// may be disposed by then. A context type that cannot be constructed from its options - one
    /// configured only in <c>OnConfiguring</c>, with no constructor taking them - is tried without
    /// them; failing that, the query runs on the application's context as given.
    /// </remarks>
    private sealed class RefreshContexts(DbContext context)
    {
        private readonly Type _contextType = context.GetType();
        private readonly IDbContextOptions _options = context.GetService<IDbContextOptions>();
        private readonly IServiceProvider _services =
            context.GetService<IDbContextOptions>().FindExtension<CoreOptionsExtension>()?.ApplicationServiceProvider
            ?? NoServices.Instance;

        private Func<DbContext>? _create;
        private bool _unavailable;

        public DbContext? Create()
        {
            if (_create is not null)
            {
                return _create();
            }

            if (_unavailable)
            {
                return null;
            }

            foreach (var candidate in new Func<DbContext>[] { WithOptions, WithoutOptions })
            {
                try
                {
                    var created = candidate();
                    _create = candidate;
                    return created;
                }
                catch (InvalidOperationException)
                {
                    // No constructor this way round; try the next.
                }
            }

            _unavailable = true;
            return null;
        }

        // The context type is the application's, and its constructors survive trimming because the
        // application builds it through them - directly, or through a factory or AddDbContext, all
        // of which demand public constructors be kept.
        [UnconditionalSuppressMessage("Trimming", "IL2077",
            Justification = "The context type's public constructors are kept by whatever built the context this one copies.")]
        private DbContext WithOptions()
            => (DbContext)ActivatorUtilities.CreateInstance(_services, _contextType, _options);

        [UnconditionalSuppressMessage("Trimming", "IL2077",
            Justification = "The context type's public constructors are kept by whatever built the context this one copies.")]
        private DbContext WithoutOptions()
            => (DbContext)ActivatorUtilities.CreateInstance(_services, _contextType);
    }

    private sealed class NoServices : IServiceProvider
    {
        public static readonly NoServices Instance = new();

        public object? GetService(Type serviceType) => null;
    }

    /// <summary>
    /// <see cref="DbSet{TEntity}"/> implements <c>IInfrastructure&lt;IServiceProvider&gt;</c>.
    /// <c>Where</c>/<c>OrderBy</c> replace it with <c>EntityQueryable</c>, whose provider holds
    /// the compiler that still knows the context.
    /// </summary>
    private static BsqliteConnection ConnectionOf<T>(IQueryable<T> query)
    {
        var context = DbContextOf(query)
            ?? throw new InvalidOperationException(
                "AsLiveQuery() needs an EF query whose provider exposes the current DbContext.");

        if (context.Database.GetDbConnection() is BsqliteConnection connection)
        {
            return connection;
        }

        throw new InvalidOperationException(
            "AsLiveQuery() requires the context to be using a BsqliteConnection.");
    }

    private static DbContext? DbContextOf(IQueryable query)
        => ContextFromInfrastructure(query as IInfrastructure<IServiceProvider>)
            ?? ContextFromInfrastructure(query.Provider as IInfrastructure<IServiceProvider>)
            ?? ContextFromQueryProvider(query.Provider);

    private static DbContext? ContextFromInfrastructure(IInfrastructure<IServiceProvider>? accessor)
        => accessor?.GetService<ICurrentDbContext>()?.Context;

    // The last resort: reach through EF's query provider for the context. EF1001 is suppressed here
    // and nowhere else, so the rest of the assembly still fails the build if it touches an internal.
#pragma warning disable EF1001
    private static DbContext? ContextFromQueryProvider(IQueryProvider provider)
    {
        if (provider is not EntityQueryProvider entityProvider)
        {
            return null;
        }

        var compiler = GetQueryCompiler(entityProvider);
        if (compiler is QueryCompiler queryCompiler)
        {
            return GetQueryContextFactory(queryCompiler).Create().Context;
        }

        return null;
    }

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_queryCompiler")]
    private static extern ref IQueryCompiler GetQueryCompiler(EntityQueryProvider provider);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_queryContextFactory")]
    private static extern ref IQueryContextFactory GetQueryContextFactory(QueryCompiler compiler);
#pragma warning restore EF1001
}
