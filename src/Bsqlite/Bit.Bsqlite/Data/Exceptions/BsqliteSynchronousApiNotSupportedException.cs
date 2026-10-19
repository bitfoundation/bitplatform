namespace Bit.Bsqlite.Data;

/// <summary>
/// Thrown when a synchronous ADO.NET or EF Core API is used against the default (asynchronous)
/// tier, where the browser main thread cannot block on the transport.
/// </summary>
public sealed class BsqliteSynchronousApiNotSupportedException : NotSupportedException
{
    private BsqliteSynchronousApiNotSupportedException(string message) : base(message)
    {
    }

    /// <summary>
    /// Builds the exception for <paramref name="synchronousMember"/>, pointing the caller at
    /// <paramref name="asynchronousMember"/>.
    /// </summary>
    /// <remarks>
    /// Public because the EF integration and third-party storage providers ship in their own
    /// assemblies and must fail with this exact, recognisable error rather than one of their own.
    /// </remarks>
    public static BsqliteSynchronousApiNotSupportedException ForMember(
        string synchronousMember,
        string asynchronousMember)
        => new($"""
            '{synchronousMember}' is not supported by Bsqlite's default tier, because the SQLite
            engine runs asynchronously and the browser main thread cannot block waiting for it.

            Use '{asynchronousMember}' instead. In EF Core, prefer the async overloads:
            SaveChangesAsync, ToListAsync, FirstOrDefaultAsync, and so on.
            """);
}
