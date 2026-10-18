using SQLitePCL;

namespace Bit.Bsqlite.EntityFrameworkCore;

/// <summary>
/// Satisfies <c>SQLitePCL.raw</c> on WASM so EF Core can read
/// <c>new SqliteConnection().ServerVersion</c> without a native e_sqlite3.
/// </summary>
/// <remarks>
/// EF Core 10's query translator does
/// <c>new Version(new SqliteConnection().ServerVersion) >= 3.38</c> before
/// compiling any query. <c>ServerVersion</c> calls <c>sqlite3_libversion()</c>,
/// which throws if no provider is set. Desktop tests already have the bundle;
/// the browser does not, and must not grow one. This stub answers version
/// probes and no-ops everything else.
/// </remarks>
internal static class BsqlitePcl
{
    private static int _initialized;

    internal static void EnsureInitialized()
    {
        if (!OperatingSystem.IsBrowser())
        {
            return;
        }

        if (Interlocked.Exchange(ref _initialized, 1) != 0)
        {
            return;
        }

        raw.SetProvider(new BsqlitePclProvider());
    }
}
