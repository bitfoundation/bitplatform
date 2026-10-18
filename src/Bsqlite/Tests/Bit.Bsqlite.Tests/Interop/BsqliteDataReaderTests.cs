using System.Data.Common;
using Bit.Bsqlite.Data;

namespace Bit.Bsqlite.Interop.Tests;

/// <summary>
/// The reader is the surface EF materializes through, so the ADO.NET contract matters here more
/// than anywhere else: schema questions must be answerable before the first row, and no answer may
/// be <see cref="DBNull"/>.
/// </summary>
[TestClass]
public sealed class BsqliteDataReaderTests
{
    [TestMethod]
    public void GetFieldType_AnswersBeforeTheFirstRead()
    {
        using var reader = Reader(
            ["id", "name"],
            ["INTEGER", "TEXT"],
            [[1L, "Widget"]]);

        Assert.AreEqual(typeof(long), reader.GetFieldType(0));
        Assert.AreEqual(typeof(string), reader.GetFieldType(1));
    }

    /// <summary>
    /// A NULL cell has no type of its own. Answering <c>DBNull</c> - which is what asking the
    /// current row would give - is not a CLR type any caller can materialize into.
    /// </summary>
    [TestMethod]
    public void GetFieldType_NeverAnswersDBNull_ForAColumnThatStartsNull()
    {
        using var reader = Reader(
            ["name"],
            ["TEXT"],
            [[null], ["Widget"]]);

        Assert.IsTrue(reader.Read());
        Assert.IsTrue(reader.IsDBNull(0));
        Assert.AreEqual(typeof(string), reader.GetFieldType(0));
    }

    /// <summary>
    /// With no data to go on the column's reported type decides, by SQLite's affinity rules - which
    /// have to work for the worker's storage-class spellings as well as declared types.
    /// </summary>
    [TestMethod]
    [DataRow("INTEGER", typeof(long))]
    [DataRow("int", typeof(long))]
    [DataRow("BIGINT", typeof(long))]
    [DataRow("TEXT", typeof(string))]
    [DataRow("nvarchar(50)", typeof(string))]
    [DataRow("CLOB", typeof(string))]
    [DataRow("BLOB", typeof(byte[]))]
    [DataRow("REAL", typeof(double))]
    [DataRow("decimal(18,2)", typeof(double))]
    [DataRow(null, typeof(object))]
    public void GetFieldType_FallsBackToAffinity_ForAnEmptyResult(string? declared, Type expected)
    {
        using var reader = Reader(["v"], [declared], []);

        Assert.AreEqual(expected, reader.GetFieldType(0));
    }

    /// <summary>
    /// Reading past the end of a blob is how a caller finds the end, so it yields nothing rather
    /// than an exception out of the copy.
    /// </summary>
    [TestMethod]
    public void GetBytes_ReturnsZero_WhenTheOffsetIsPastTheEnd()
    {
        using var reader = Reader(["b"], ["BLOB"], [[new byte[] { 1, 2, 3 }]]);
        Assert.IsTrue(reader.Read());

        var buffer = new byte[8];

        Assert.AreEqual(3, reader.GetBytes(0, 0, null, 0, 0));
        Assert.AreEqual(2, reader.GetBytes(0, 1, buffer, 0, 8));
        Assert.AreEqual(0, reader.GetBytes(0, 3, buffer, 0, 8));
    }

    [TestMethod]
    public void GetChars_ReturnsZero_WhenTheOffsetIsPastTheEnd()
    {
        using var reader = Reader(["s"], ["TEXT"], [["abc"]]);
        Assert.IsTrue(reader.Read());

        var buffer = new char[8];

        Assert.AreEqual(3, reader.GetChars(0, 0, null, 0, 0));
        Assert.AreEqual(0, reader.GetChars(0, 3, buffer, 0, 8));
    }

    private static DbDataReader Reader(
        string[] columnNames,
        string?[] columnTypes,
        object?[][] rows)
        => new BsqliteDataReader(
        [
            new BsqliteCommandResult
            {
                ColumnNames = columnNames,
                ColumnTypes = columnTypes,
                Rows = rows,
            },
        ]);
}
