using Bit.Bsqlite.Data;

namespace Bit.Bsqlite.Interop.Tests;

[TestClass]
public sealed class SqliteTableNamesTests
{
    [TestMethod]
    [DataRow("SELECT * FROM product", "product")]
    [DataRow("INSERT INTO product (name) VALUES ('x')", "product")]
    [DataRow("UPDATE product SET name = 'x'", "product")]
    [DataRow("DELETE FROM product", "product")]
    [DataRow("SELECT * FROM product JOIN category ON 1=1", "product", "category")]
    [DataRow("SELECT * FROM \"Products\" AS p", "Products")]
    [DataRow("SELECT * FROM [Products]", "Products")]
    [DataRow("SELECT * FROM `Products`", "Products")]
    [DataRow("INSERT INTO main.Products (Name) VALUES ('x')", "Products")]
    [DataRow("INSERT INTO \"main\".\"Products\" (Name) VALUES ('x')", "Products")]
    [DataRow("CREATE TABLE IF NOT EXISTS \"Orders\" (Id INTEGER)", "Orders")]
    [DataRow("CREATE INDEX IX_Orders_Date ON Orders (Date)", "Orders")]
    [DataRow("DROP VIEW OpenOrders", "OpenOrders")]
    [DataRow("SELECT * FROM (SELECT 1)")]
    // The quoting SQLite accepts, a schema prefix, and UPDATE's conflict clause: each of these hid a
    // table from the pattern once, and a hidden table is a live query that never re-runs.
    [DataRow("SELECT * FROM \"Products\" AS \"p\"", "Products")]
    [DataRow("INSERT INTO [Products] (Name) VALUES ('x')", "Products")]
    [DataRow("DELETE FROM `Products` WHERE Id = 1", "Products")]
    [DataRow("SELECT * FROM main.\"Products\"", "Products")]
    [DataRow("UPDATE OR REPLACE Products SET Name = 'x'", "Products")]
    [DataRow("UPDATE OR IGNORE \"Products\" SET Name = 'x'", "Products")]
    [DataRow("CREATE TABLE IF NOT EXISTS \"Products\" (Id INTEGER)", "Products")]
    [DataRow("SELECT * FROM sqlite_master")]
    public void ExtractsTables(string sql, params string[] expected)
        => AssertSequence.Equal(
            expected.Order(StringComparer.OrdinalIgnoreCase),
            BsqliteTableNames.Extract(sql).Order(StringComparer.OrdinalIgnoreCase));

    [TestMethod]
    public void SkipsSqliteInternalTables()
        => Assert.IsEmpty(BsqliteTableNames.Extract("SELECT * FROM sqlite_master"));

    [TestMethod]
    [DataRow("INSERT INTO product (name) VALUES ('from')", true)]
    [DataRow("SELECT * FROM product", false)]
    [DataRow("BEGIN; INSERT INTO product (name) VALUES ('x'); COMMIT;", true)]
    [DataRow("PRAGMA foreign_keys = ON; DELETE FROM product", true)]
    [DataRow("-- comment\nUPDATE product SET name = 'x'", true)]
    [DataRow("/* comment */ DELETE FROM product", true)]
    [DataRow("WITH gone AS (SELECT id FROM product) DELETE FROM product WHERE id IN gone", true)]
    [DataRow("SELECT 'insert into' FROM product", false)]
    [DataRow("CREATE TABLE t (id INTEGER)", true)]
    // A write is a write wherever it sits in the batch. Missing one of these is a live query that
    // never re-runs, which is the failure this heuristic exists to prevent.
    [DataRow("SELECT 1; UPDATE product SET name = 'x'", true)]
    [DataRow("BEGIN; SELECT * FROM product; COMMIT;", false)]
    [DataRow("  \n  DELETE FROM product", true)]
    [DataRow("SELECT * FROM product; SELECT * FROM category", false)]
    // A common table expression in front of a write is still a write.
    [DataRow("WITH ids AS (SELECT 1 AS id) INSERT INTO product (id) SELECT id FROM ids", true)]
    [DataRow("WITH ids AS (SELECT 1 AS id) UPDATE product SET name = 'x' WHERE id IN (SELECT id FROM ids)", true)]
    [DataRow("WITH ids AS (SELECT 1 AS id) SELECT * FROM product WHERE id IN (SELECT id FROM ids)", false)]
    [DataRow("SELECT * FROM withdrawals", false)]
    public void DistinguishesWrites(string sql, bool write)
        => Assert.AreEqual(write, BsqliteTableNames.LooksLikeWrite(sql));
}
