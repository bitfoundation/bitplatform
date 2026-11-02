using System.Data.Common;
using Bit.Bsqlite.Data;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Bit.Bsqlite.EntityFrameworkCore;

/// <summary>
/// Marks the commands EF Core creates as enlisting explicitly, so a context outside the open
/// transaction waits for it instead of joining it.
/// </summary>
/// <remarks>
/// Every context of a session shares one <see cref="BsqliteConnection"/>. EF gives a command its
/// context's transaction whenever the context has one, so an EF command without a transaction is
/// another context's - one that must not run inside, and roll back with, a transaction it never
/// began. ADO.NET code that leaves <c>Transaction</c> unset still joins the open transaction, as it
/// always has; only EF's commands say enough to be told apart.
/// </remarks>
internal sealed class BsqliteCommandEnlistmentInterceptor : DbCommandInterceptor
{
    public static readonly BsqliteCommandEnlistmentInterceptor Instance = new();

    private BsqliteCommandEnlistmentInterceptor()
    {
    }

    public override DbCommand CommandCreated(CommandEndEventData eventData, DbCommand result)
    {
        if (result is BsqliteCommand command)
        {
            command.EnlistsExplicitly = true;
        }

        return result;
    }
}
