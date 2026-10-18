using Bit.Bsqlite.Data;

namespace Bit.Bsqlite.Interop.Tests;

[TestClass]
public sealed class SqliteFeatureGuardsTests
{
    [TestMethod]
    [DataRow("PRAGMA journal_mode=WAL")]
    [DataRow("pragma journal_mode = wal")]
    [DataRow("PRAGMA journal_mode='wal'")]
    [DataRow("PRAGMA journal_mode=\"WAL\"")]
    public void RejectsWalInAnyReasonableSpelling(string sql)
    {
        var error = Assert.ThrowsExactly<BsqliteException>(() => BsqliteFeatureGuards.EnsureSupported(sql));

        Assert.Contains("WAL", error.Message);
    }

    [TestMethod]
    [DataRow("PRAGMA journal_mode=DELETE")]
    [DataRow("PRAGMA journal_mode=TRUNCATE")]
    [DataRow("SELECT journal_mode FROM pragma_journal_mode")]
    [DataRow("INSERT INTO t (name) VALUES ('walrus')")]
    [DataRow("ATTACH 'other.db' AS other")]
    [DataRow("PRAGMA page_size=4096")]
    public void DoesNotRejectUnrelatedSql(string sql)
        => BsqliteFeatureGuards.EnsureSupported(sql);

    [TestMethod]
    [DataRow("ATTACH 'other.db' AS other")]
    [DataRow("attach database 'other.db' as other")]
    public void RejectsAttach_WhenTheBackendCannotSpanDatabases(string sql)
    {
        var limits = BsqliteRuntimeLimits.Unrestricted with { SupportsMultiDatabaseTransactions = false };
        var error = Assert.ThrowsExactly<BsqliteException>(() => BsqliteFeatureGuards.EnsureSupported(sql, limits));

        Assert.Contains("ATTACH", error.Message);
    }

    [TestMethod]
    public void RejectsPageSizeAssignment_WhenTheBackendPinsThePageSize()
    {
        var limits = BsqliteRuntimeLimits.Unrestricted with { CanChangePageSize = false };
        var error = Assert.ThrowsExactly<BsqliteException>(
            () => BsqliteFeatureGuards.EnsureSupported("PRAGMA page_size=8192", limits));

        Assert.Contains("page_size", error.Message);
    }

    [TestMethod]
    [DataRow("INSERT INTO t (name) VALUES ('attach')")]
    [DataRow("SELECT page_size FROM pragma_page_size")]
    [DataRow("PRAGMA page_size")]
    public void DoesNotFalsePositive_OnAttachOrPageSizeWords(string sql)
    {
        var limits = new BsqliteRuntimeLimits
        {
            SupportsMultiDatabaseTransactions = false,
            CanChangePageSize = false,
        };

        BsqliteFeatureGuards.EnsureSupported(sql, limits);
    }
}
