using System.Data.Common;
using Bit.Bsqlite.Data;

namespace Bit.Bsqlite.Interop.Tests;

/// <summary>
/// Code shared between a server and the browser catches <see cref="DbException"/>, as it does for
/// Microsoft.Data.Sqlite. A Bsqlite failure that slipped past those handlers would surface only in
/// the browser.
/// </summary>
[TestClass]
public sealed class BsqliteExceptionTests
{
    /// <summary>What the worker transport throws for a failure the engine reported.</summary>
    [TestMethod]
    public void AFailureFromTheWorker_IsCaughtAsADbException()
    {
        DbException? caught = null;
        try
        {
            throw BsqliteWireFormat.CreateException("UNIQUE constraint failed: product.name", 19, "Error");
        }
        catch (DbException ex)
        {
            caught = ex;
        }

        Assert.IsInstanceOfType<BsqliteConstraintException>(caught);
    }

    [TestMethod]
    public void EveryBsqliteFailure_IsADbException()
    {
        Assert.IsInstanceOfType<DbException>(new BsqliteConstraintException("UNIQUE constraint failed"));
        Assert.IsInstanceOfType<DbException>(new BsqliteConcurrencyException("database is locked"));
        Assert.IsInstanceOfType<DbException>(new BsqliteCorruptDatabaseException("file is not a database"));
        Assert.IsInstanceOfType<DbException>(new BsqliteQuotaExceededException("database or disk is full"));
    }

    [TestMethod]
    [DataRow(5, true)]
    [DataRow(6, true)]
    [DataRow(261, true)]   // SQLITE_BUSY_RECOVERY: the extended code keeps the primary in its low byte.
    [DataRow(19, false)]
    [DataRow(null, false)]
    public void Busy_And_Locked_AreTransient(int? code, bool transient)
        => Assert.AreEqual(transient, new BsqliteException("failure", code).IsTransient);
}
