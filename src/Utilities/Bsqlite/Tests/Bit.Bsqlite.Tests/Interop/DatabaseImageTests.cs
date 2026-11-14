using System.Buffers.Binary;
using Bit.Bsqlite.Data;
using Microsoft.Data.Sqlite;

namespace Bit.Bsqlite.Interop.Tests;

/// <summary>
/// What an import is checked against before it is written over a database: a file that is not a whole
/// SQLite database is far cheaper to refuse up front than to bind and then fail every open with.
/// </summary>
[TestClass]
public sealed class DatabaseImageTests
{
    [TestMethod]
    public void AnEmptyImage_IsAnEmptyDatabase()
        => BsqliteDatabaseImage.Verify([]);

    [TestMethod]
    public void AWholeImage_Passes()
        => BsqliteDatabaseImage.Verify(SqliteImages.Valid(pages: 5, pageSize: 4096));

    [TestMethod]
    [DataRow(512)]
    [DataRow(65536)]
    public void EveryPageSizeSqliteCanHave_Passes(int pageSize)
        => BsqliteDatabaseImage.Verify(SqliteImages.Valid(pageSize: pageSize));

    [TestMethod]
    public void AFileThatIsNotADatabase_IsRefused()
    {
        var error = Assert.ThrowsExactly<BsqliteCorruptDatabaseException>(
            () => BsqliteDatabaseImage.Verify("{\"not\":\"a database\"}"u8, "The upload"));

        StringAssert.StartsWith(error.Message, "The upload is not a SQLite database");
    }

    [TestMethod]
    public void AHeaderNamingAnImpossiblePageSize_IsRefused()
    {
        var image = SqliteImages.Valid();
        BinaryPrimitives.WriteUInt16BigEndian(image.AsSpan(16), 1000);

        Assert.ThrowsExactly<BsqliteCorruptDatabaseException>(() => BsqliteDatabaseImage.Verify(image));
    }

    [TestMethod]
    public void AnImageCutPartWayThroughAPage_IsRefused()
        => Assert.ThrowsExactly<BsqliteCorruptDatabaseException>(
            () => BsqliteDatabaseImage.Verify(SqliteImages.Valid(pages: 2)[..1500]));

    [TestMethod]
    public void AnImageMissingPagesItsHeaderCounts_IsRefused()
    {
        var error = Assert.ThrowsExactly<BsqliteCorruptDatabaseException>(
            () => BsqliteDatabaseImage.Verify(SqliteImages.Valid(pages: 3)[..2048]));

        StringAssert.Contains(error.Message, "3 pages");
    }

    /// <summary>
    /// The header's page count is only current when the change counter it was written with matches; a file
    /// from a SQLite older than 3.7.0 may leave it stale, and SQLite then goes by the file's length.
    /// </summary>
    [TestMethod]
    public void AStalePageCount_IsNotHeldAgainstTheImage()
    {
        var image = SqliteImages.Valid(pages: 3)[..2048];
        BinaryPrimitives.WriteUInt32BigEndian(image.AsSpan(92), 7);

        BsqliteDatabaseImage.Verify(image);
    }

    [TestMethod]
    public void Prepare_LeavesARollbackJournalImageAsItIs()
    {
        ReadOnlyMemory<byte> image = SqliteImages.Valid();

        Assert.IsTrue(BsqliteDatabaseImage.Prepare(image).Span == image.Span);
    }

    /// <summary>
    /// The real thing: a database Microsoft.Data.Sqlite wrote in WAL mode, checkpointed and closed, opens as
    /// a rollback-journal database once prepared - with its rows, which the checkpoint put in the file.
    /// </summary>
    [TestMethod]
    public void Prepare_MarksAWalModeDatabase_SoItOpensWithoutItsLog()
    {
        var path = Path.Combine(Path.GetTempPath(), $"bsqlite-wal-{Guid.NewGuid():N}.db");
        var copy = path + ".copy";
        try
        {
            using (var connection = new SqliteConnection($"Data Source={path};Pooling=False"))
            {
                connection.Open();
                Run(connection, "PRAGMA journal_mode = WAL; CREATE TABLE t (x); INSERT INTO t VALUES (42); PRAGMA wal_checkpoint(TRUNCATE);");
            }

            var image = File.ReadAllBytes(path);
            Assert.IsTrue(BsqliteDatabaseImage.IsWalMode(image));

            var prepared = BsqliteDatabaseImage.Prepare(image).ToArray();
            Assert.IsFalse(BsqliteDatabaseImage.IsWalMode(prepared));
            File.WriteAllBytes(copy, prepared);

            using var reopened = new SqliteConnection($"Data Source={copy};Mode=ReadOnly;Pooling=False");
            reopened.Open();
            using var command = reopened.CreateCommand();
            command.CommandText = "SELECT x FROM t";
            Assert.AreEqual(42L, command.ExecuteScalar());
            command.CommandText = "PRAGMA journal_mode";
            Assert.AreEqual("delete", command.ExecuteScalar());
        }
        finally
        {
            File.Delete(path);
            File.Delete(copy);
        }
    }

    private static void Run(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}
