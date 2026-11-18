namespace Bit.Bsqlite;

/// <summary>Where the application's .NET code runs, which decides where Bsqlite runs the engine.</summary>
internal enum BsqliteHostKind
{
    /// <summary>
    /// Anywhere else: a server rendering or prerendering pages, whose users' databases live in their
    /// browsers, or a desktop process such as a test run.
    /// </summary>
    Other,

    /// <summary>Blazor WebAssembly: the engine runs in a dedicated worker in the same tab.</summary>
    Browser,

    /// <summary>
    /// Blazor Hybrid: .NET runs natively next to a web view, and the engine is the native SQLite the app
    /// brings, opened in-process on a file of the app's own.
    /// </summary>
    BlazorHybrid,
}
