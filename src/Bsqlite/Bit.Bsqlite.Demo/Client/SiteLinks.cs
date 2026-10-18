using System.Reflection;
using Bit.Bsqlite.Data;

namespace Bit.Bsqlite.Demo.Client;

/// <summary>Where the site points when it leaves itself, plus the version it is showing off.</summary>
internal static class SiteLinks
{
    /// <summary>
    /// The Bit.Bsqlite build this site runs. The informational version's build metadata - everything after
    /// '+', usually the commit hash - is dropped: it identifies nothing a reader could act on.
    /// </summary>
    public static readonly string Version = (
        typeof(BsqliteConnection).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(BsqliteConnection).Assembly.GetName().Version?.ToString()
        ?? "unknown").Split('+')[0];

    public const string Repository = "https://github.com/bitfoundation/bitplatform/tree/develop/src/Bsqlite";
    public const string Issues = "https://github.com/bitfoundation/bitplatform/issues";
    public const string License = "https://github.com/bitfoundation/bitplatform/blob/develop/LICENSE";
    public const string NuGet = "https://www.nuget.org/packages/Bit.Bsqlite";
    public const string Readme = "https://github.com/bitfoundation/bitplatform/tree/develop/src/Bsqlite#readme";
}
