using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Bit.Bsqlite.Data;
using Bit.Bsqlite.Interop;

namespace Bit.Bsqlite.Interop.Tests;

/// <summary>
/// The wire format is the only thing standing between a 64-bit key and a silently rounded one, so
/// every storage class and the exact byte layout are pinned here rather than left to a browser test
/// that would also be testing the engine. <see cref="ReplyBuilder"/> writes replies the way
/// <c>bit-bsqlite-wire.js</c> does; a layout change must be made in both places.
/// </summary>
[TestClass]
public sealed class SqliteWireFormatTests
{
    [TestMethod]
    public void EncodeBatch_WritesTheLayoutTheWorkerReads()
    {
        var bytes = BsqliteWireFormat.EncodeBatch(
        [
            new BsqliteCommandRequest
            {
                CommandText = "INSERT INTO t (n) VALUES (@n)",
                ResultKind = BsqliteResultKind.Reader,
                RowLimit = 5,
                Parameters = [new BsqliteParameterValue("@n", 7)],
            },
        ]);

        var reader = new SpanReader(bytes);
        Assert.AreEqual(BsqliteWireFormat.Magic, reader.Byte());
        Assert.AreEqual(BsqliteWireFormat.Version, reader.Byte());
        Assert.AreEqual(1u, reader.UInt32());
        Assert.AreEqual("INSERT INTO t (n) VALUES (@n)", reader.String());
        Assert.AreEqual(2, reader.Byte());                 // reader
        Assert.AreEqual(5, reader.Int32());                // row limit
        Assert.AreEqual(1, reader.UInt16());               // parameter count
        Assert.AreEqual("@n", reader.String());
        Assert.AreEqual(BsqliteWireFormat.TypeCode.Integer, reader.Byte());
        Assert.AreEqual(7L, reader.Int64());
        Assert.AreEqual(0, reader.Remaining);
    }

    [TestMethod]
    public void EncodeBatch_WritesMinusOne_WhenThereIsNoRowLimit()
    {
        var bytes = BsqliteWireFormat.EncodeBatch([new BsqliteCommandRequest { CommandText = "SELECT 1" }]);

        var reader = new SpanReader(bytes);
        reader.Skip(6);
        reader.String();
        Assert.AreEqual(0, reader.Byte());
        Assert.AreEqual(-1, reader.Int32());
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(-1)]
    [DataRow(9007199254740993)]
    [DataRow(long.MaxValue)]
    [DataRow(long.MinValue)]
    public void Integers_TravelAsEightBytes_WhateverTheirMagnitude(long value)
    {
        var (type, encoded) = BsqliteWireFormat.EncodeValue(value, "p");

        Assert.AreEqual(BsqliteWireFormat.TypeCode.Integer, type);
        Assert.AreEqual(value, Assert.IsInstanceOfType<long>(encoded));

        var bytes = BsqliteWireFormat.EncodeBatch(
            [new BsqliteCommandRequest { CommandText = "x", Parameters = [new BsqliteParameterValue("p", value)] }]);
        Assert.AreEqual(value, BinaryPrimitives.ReadInt64LittleEndian(bytes.AsSpan(bytes.Length - 8)));
    }

    [TestMethod]
    public void Decode_RestoresALargeInteger_Exactly()
    {
        var reply = new ReplyBuilder()
            .Result(["v"], [BsqliteWireFormat.TypeCode.Integer])
            .Row(long.MaxValue)
            .End()
            .Build();

        var results = BsqliteWireFormat.DecodeReply(reply);

        Assert.AreEqual(long.MaxValue, Assert.ContainsSingle(Assert.ContainsSingle(Assert.ContainsSingle(results).Rows)));
    }

    [TestMethod]
    public void Blob_RoundTripsAsRawBytes()
    {
        byte[] blob = [0, 1, 250, 255];
        var encoded = BsqliteWireFormat.EncodeBatch(
        [
            new BsqliteCommandRequest
            {
                CommandText = "INSERT INTO t (b) VALUES (@b)",
                Parameters = [new BsqliteParameterValue("@b", blob)],
            },
        ]);

        // The last nine bytes are the blob tag, its length, and the bytes themselves.
        Assert.AreEqual(BsqliteWireFormat.TypeCode.Blob, encoded[^9]);
        Assert.AreEqual(4u, BinaryPrimitives.ReadUInt32LittleEndian(encoded.AsSpan(encoded.Length - 8)));
        CollectionAssert.AreEqual(blob, encoded[^4..]);

        var results = BsqliteWireFormat.DecodeReply(new ReplyBuilder()
            .Result(["b"], [BsqliteWireFormat.TypeCode.Blob])
            .Row(blob)
            .End()
            .Build());

        CollectionAssert.AreEqual(blob, (byte[]?)Assert.ContainsSingle(Assert.ContainsSingle(Assert.ContainsSingle(results).Rows)));
    }

    [TestMethod]
    public void EveryStorageClass_DecodesToItsClrType()
    {
        var results = BsqliteWireFormat.DecodeReply(new ReplyBuilder()
            .Result(
                ["i", "r", "t", "b", "n"],
                [BsqliteWireFormat.TypeCode.Integer, BsqliteWireFormat.TypeCode.Real, BsqliteWireFormat.TypeCode.Text, BsqliteWireFormat.TypeCode.Blob, 0])
            .Row(42L, 1.5d, "héllo wörld", new byte[] { 9 }, null)
            .End()
            .Build());

        var result = Assert.ContainsSingle(results);
        var row = Assert.ContainsSingle(result.Rows);
        AssertSequence.Equal(["INTEGER", "REAL", "TEXT", "BLOB", null], result.ColumnTypes);
        Assert.AreEqual(42L, row[0]);
        Assert.AreEqual(1.5d, row[1]);
        Assert.AreEqual("héllo wörld", row[2]);
        CollectionAssert.AreEqual(new byte[] { 9 }, (byte[]?)row[3]);
        Assert.IsNull(row[4]);
    }

    [TestMethod]
    public void Decode_CarriesTheCounters()
    {
        var results = BsqliteWireFormat.DecodeReply(new ReplyBuilder()
            .Result([], [], recordsAffected: 3, lastInsertRowId: 77)
            .End()
            .Build());

        var result = Assert.ContainsSingle(results);
        Assert.AreEqual(3, result.RecordsAffected);
        Assert.AreEqual(77L, result.LastInsertRowId);
    }

    [TestMethod]
    public void Decode_ReturnsOneResultPerRequest_InOrder()
    {
        var results = BsqliteWireFormat.DecodeReply(new ReplyBuilder()
            .Result([], [], recordsAffected: 1).End()
            .Result(["n"], [BsqliteWireFormat.TypeCode.Text]).Row("First").Row("Second").End()
            .Build());

        Assert.AreEqual(2, results.Count);
        Assert.AreEqual(1, results[0].RecordsAffected);
        Assert.AreEqual(2, results[1].Rows.Count);
        Assert.AreEqual("Second", results[1].Rows[1][0]);
    }

    [TestMethod]
    public void NullBoolDecimalAndText_HaveOneObviousEncodingEach()
    {
        Assert.AreEqual((BsqliteWireFormat.TypeCode.Null, (object?)null), BsqliteWireFormat.EncodeValue(null, "p"));
        Assert.AreEqual((BsqliteWireFormat.TypeCode.Null, (object?)null), BsqliteWireFormat.EncodeValue(DBNull.Value, "p"));
        Assert.AreEqual((BsqliteWireFormat.TypeCode.Integer, 1L), BsqliteWireFormat.EncodeValue(true, "p"));
        Assert.AreEqual((BsqliteWireFormat.TypeCode.Integer, 0L), BsqliteWireFormat.EncodeValue(false, "p"));
        Assert.AreEqual((BsqliteWireFormat.TypeCode.Text, "1.25"), BsqliteWireFormat.EncodeValue(1.250m, "p"));
        Assert.AreEqual((BsqliteWireFormat.TypeCode.Text, "hi"), BsqliteWireFormat.EncodeValue("hi", "p"));
        Assert.AreEqual((BsqliteWireFormat.TypeCode.Real, 1.5d), BsqliteWireFormat.EncodeValue(1.5d, "p"));
    }

    /// <summary>
    /// The oracle is Microsoft.Data.Sqlite: whatever it would have stored for a decimal parameter is
    /// what the worker has to store, or a database written in the browser stops matching one written
    /// on the server - and EF's decimal equality, which is a plain TEXT comparison against a literal
    /// in this same form, starts returning nothing.
    /// </summary>
    [TestMethod]
    [DataRow("10", "10.0")]
    [DataRow("1.50", "1.5")]
    [DataRow("100.000", "100.0")]
    [DataRow("0", "0.0")]
    [DataRow("-3", "-3.0")]
    [DataRow("12.34", "12.34")]
    [DataRow("0.3333333333333333333333333333", "0.3333333333333333333333333333")]
    public void ADecimal_TravelsInMicrosoftDataSqlitesCanonicalTextForm(string clr, string expected)
    {
        var value = decimal.Parse(clr, CultureInfo.InvariantCulture);

        Assert.AreEqual((BsqliteWireFormat.TypeCode.Text, expected), BsqliteWireFormat.EncodeValue(value, "p"));
        Assert.AreEqual(expected, StoredByMicrosoftDataSqlite(value));
    }

    /// <summary>
    /// SQLite stores NaN as NULL, so that is what it becomes on the way in; the infinities are
    /// ordinary IEEE doubles and cross the binary format as eight bytes like any other REAL.
    /// </summary>
    [TestMethod]
    public void InfinityAndNaN_SurviveTheWire()
    {
        Assert.AreEqual((BsqliteWireFormat.TypeCode.Real, double.PositiveInfinity), BsqliteWireFormat.EncodeValue(double.PositiveInfinity, "p"));
        Assert.AreEqual((BsqliteWireFormat.TypeCode.Real, double.NegativeInfinity), BsqliteWireFormat.EncodeValue(float.NegativeInfinity, "p"));
        Assert.AreEqual((BsqliteWireFormat.TypeCode.Null, (object?)null), BsqliteWireFormat.EncodeValue(double.NaN, "p"));
        Assert.AreEqual((BsqliteWireFormat.TypeCode.Null, (object?)null), BsqliteWireFormat.EncodeValue(float.NaN, "p"));

        var results = BsqliteWireFormat.DecodeReply(new ReplyBuilder()
            .Result(["v", "w"], [BsqliteWireFormat.TypeCode.Real, BsqliteWireFormat.TypeCode.Real])
            .Row(double.PositiveInfinity, double.NegativeInfinity)
            .End()
            .Build());

        AssertSequence.Equal([double.PositiveInfinity, double.NegativeInfinity], Assert.ContainsSingle(Assert.ContainsSingle(results).Rows));
    }

    /// <summary>What Microsoft.Data.Sqlite actually writes for a decimal parameter.</summary>
    private static string StoredByMicrosoftDataSqlite(decimal value)
    {
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT @v";
        command.Parameters.AddWithValue("@v", value);
        return (string)command.ExecuteScalar()!;
    }

    [TestMethod]
    public void DatesAndGuids_AreRejected_SoTheTypeMappingOwnsTheChoice()
    {
        var date = Assert.ThrowsExactly<NotSupportedException>(
            () => BsqliteWireFormat.EncodeValue(new DateTime(2026, 1, 1), "when"));
        var guid = Assert.ThrowsExactly<NotSupportedException>(
            () => BsqliteWireFormat.EncodeValue(Guid.Empty, "id"));

        Assert.Contains("when", date.Message);
        Assert.Contains("id", guid.Message);
        Assert.Contains("ADO.NET binder", date.Message);
    }

    [TestMethod]
    public void AnUnsignedIntegerPastInt64_IsRejectedRatherThanWrapped()
    {
        var error = Assert.ThrowsExactly<NotSupportedException>(
            () => BsqliteWireFormat.EncodeValue(ulong.MaxValue, "p"));

        Assert.Contains("64-bit signed INTEGER", error.Message);
    }

    [TestMethod]
    public void DecodeReply_ThrowsBsqliteException_CarryingTheResultCode()
    {
        var error = Assert.ThrowsExactly<BsqliteConstraintException>(
            () => BsqliteWireFormat.DecodeReply(ReplyBuilder.Error("UNIQUE constraint failed", 19, "SQLiteError")));

        Assert.AreEqual("UNIQUE constraint failed", error.Message);
        Assert.AreEqual(19, error.SqliteErrorCode);
    }

    [TestMethod]
    public void DecodeReply_ThrowsWithoutACode_WhenTheFailureIsTheTransports()
    {
        var error = Assert.ThrowsExactly<BsqliteException>(
            () => BsqliteWireFormat.DecodeReply(ReplyBuilder.Error("No database is open in this worker.", -1, "Error")));

        Assert.IsNull(error.SqliteErrorCode);
    }

    [TestMethod]
    [DataRow(5, typeof(BsqliteConcurrencyException))]
    [DataRow(6, typeof(BsqliteConcurrencyException))]
    [DataRow(11, typeof(BsqliteCorruptDatabaseException))]
    [DataRow(13, typeof(BsqliteQuotaExceededException))]
    [DataRow(19, typeof(BsqliteConstraintException))]
    [DataRow(1, typeof(BsqliteException))]
    public void ResultCodes_MapToTypedExceptions(int code, Type expected)
    {
        var error = BsqliteWireFormat.CreateException("x", code, null);

        Assert.AreEqual(expected, error.GetType());
        Assert.AreEqual(code, error.SqliteErrorCode);
    }

    [TestMethod]
    public void AQuotaExceededError_IsRecognisedByName_WhenTheBrowserThrewIt()
    {
        var error = BsqliteWireFormat.CreateException("The quota has been exceeded.", null, "QuotaExceededError");

        Assert.IsInstanceOfType<BsqliteQuotaExceededException>(error);
    }

    [TestMethod]
    public void DecodeReply_RejectsTheWrongMagic()
    {
        Assert.ThrowsExactly<FormatException>(() => BsqliteWireFormat.DecodeReply([0x00, 0x01, 0x00]));
    }

    [TestMethod]
    public void DecodeReply_RejectsAnotherVersion_NamingTheMismatch()
    {
        var error = Assert.ThrowsExactly<FormatException>(
            () => BsqliteWireFormat.DecodeReply([BsqliteWireFormat.Magic, (byte)(BsqliteWireFormat.Version + 1), 0]));

        Assert.Contains("different release", error.Message);
    }

    [TestMethod]
    public void DecodeReply_RejectsATruncatedMessage()
    {
        var whole = new ReplyBuilder().Result(["v"], [BsqliteWireFormat.TypeCode.Text]).Row("abc").End().Build();

        Assert.ThrowsExactly<FormatException>(() => BsqliteWireFormat.DecodeReply(whole.AsSpan(0, whole.Length - 2)));
    }

    [TestMethod]
    public void DecodeCall_ReturnsTheResult_WhenTheWorkerSucceeded()
    {
        var result = BsqliteWireFormat.DecodeCall(Parse("""{ "ok": true, "result": [1, 2] }"""));

        Assert.AreEqual(2, result.GetArrayLength());
    }

    [TestMethod]
    public void DecodeCall_ThrowsBsqliteException_CarryingTheResultCode()
    {
        var error = Assert.ThrowsExactly<BsqliteConstraintException>(
            () => BsqliteWireFormat.DecodeCall(Parse(
                """{ "ok": false, "error": { "message": "UNIQUE constraint failed", "sqliteCode": 19 } }""")));

        Assert.AreEqual("UNIQUE constraint failed", error.Message);
        Assert.AreEqual(19, error.SqliteErrorCode);
    }

    [TestMethod]
    public void DecodeCall_RejectsABareArray_SoAMissingEnvelopeCannotBeMistakenForSuccess()
    {
        Assert.ThrowsExactly<FormatException>(() => BsqliteWireFormat.DecodeCall(Parse("[1]")));
    }

    /// <summary>
    /// A reply that does not match the envelope is a wire problem and is reported as one, rather
    /// than as whatever <c>System.Text.Json</c> happens to raise when a property is missing or the
    /// wrong kind.
    /// </summary>
    [TestMethod]
    [DataRow("""{ "result": 1 }""")]
    [DataRow("""{ "ok": "yes", "result": 1 }""")]
    [DataRow("[]")]
    public void DecodeCall_ReportsAMalformedEnvelope_AsAFormatException(string json)
        => Assert.ThrowsExactly<FormatException>(() => BsqliteWireFormat.DecodeCall(Parse(json)));

    private static JsonElement Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    /// <summary>Walks a batch the way the worker's `BinaryReader` does.</summary>
    private ref struct SpanReader(ReadOnlySpan<byte> data)
    {
        private readonly ReadOnlySpan<byte> _data = data;
        private int _offset = 0;

        public readonly int Remaining => _data.Length - _offset;

        public void Skip(int count) => _offset += count;

        public byte Byte() => _data[_offset++];

        public ushort UInt16()
        {
            var value = BinaryPrimitives.ReadUInt16LittleEndian(_data[_offset..]);
            _offset += 2;
            return value;
        }

        public int Int32()
        {
            var value = BinaryPrimitives.ReadInt32LittleEndian(_data[_offset..]);
            _offset += 4;
            return value;
        }

        public uint UInt32()
        {
            var value = BinaryPrimitives.ReadUInt32LittleEndian(_data[_offset..]);
            _offset += 4;
            return value;
        }

        public long Int64()
        {
            var value = BinaryPrimitives.ReadInt64LittleEndian(_data[_offset..]);
            _offset += 8;
            return value;
        }

        public string String()
        {
            var length = (int)UInt32();
            var value = Encoding.UTF8.GetString(_data.Slice(_offset, length));
            _offset += length;
            return value;
        }
    }
}

/// <summary>Writes replies exactly as <c>bit-bsqlite-wire.js</c> does, so the decoder is tested against the real layout.</summary>
internal sealed class ReplyBuilder
{
    private readonly MemoryStream _stream = new();
    private readonly BinaryWriter _writer;
    private int _resultCount;
    private long _rowCountPosition;
    private int _rowCount;

    public ReplyBuilder()
    {
        _writer = new BinaryWriter(_stream, Encoding.UTF8, leaveOpen: true);
        _writer.Write(BsqliteWireFormat.Magic);
        _writer.Write(BsqliteWireFormat.Version);
        _writer.Write((byte)0);
        _writer.Write(0u); // result count, patched by Build
    }

    public static byte[] Error(string message, int code, string name)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8);
        writer.Write(BsqliteWireFormat.Magic);
        writer.Write(BsqliteWireFormat.Version);
        writer.Write((byte)1);
        WriteString(writer, message);
        writer.Write(code);
        WriteString(writer, name);
        writer.Flush();
        return stream.ToArray();
    }

    public ReplyBuilder Result(string[] columns, int[] storageClasses, int recordsAffected = 0, long lastInsertRowId = 0)
    {
        _resultCount++;
        _writer.Write((ushort)columns.Length);
        for (var i = 0; i < columns.Length; i++)
        {
            WriteString(_writer, columns[i]);
            _writer.Write((byte)storageClasses[i]);
        }

        _writer.Write(recordsAffected);
        _writer.Write(lastInsertRowId);
        _rowCountPosition = _stream.Position;
        _writer.Write(0u);
        _rowCount = 0;
        return this;
    }

    public ReplyBuilder Row(params object?[] cells)
    {
        _rowCount++;
        foreach (var cell in cells)
        {
            switch (cell)
            {
                case null:
                    _writer.Write((byte)BsqliteWireFormat.TypeCode.Null);
                    break;
                case long value:
                    _writer.Write((byte)BsqliteWireFormat.TypeCode.Integer);
                    _writer.Write(value);
                    break;
                case double value:
                    _writer.Write((byte)BsqliteWireFormat.TypeCode.Real);
                    _writer.Write(value);
                    break;
                case string value:
                    _writer.Write((byte)BsqliteWireFormat.TypeCode.Text);
                    WriteString(_writer, value);
                    break;
                case byte[] value:
                    _writer.Write((byte)BsqliteWireFormat.TypeCode.Blob);
                    _writer.Write((uint)value.Length);
                    _writer.Write(value);
                    break;
                default:
                    throw new ArgumentException($"Unsupported cell {cell.GetType()}.");
            }
        }

        return this;
    }

    public ReplyBuilder End()
    {
        var end = _stream.Position;
        _stream.Position = _rowCountPosition;
        _writer.Write((uint)_rowCount);
        _stream.Position = end;
        return this;
    }

    public byte[] Build()
    {
        _writer.Flush();
        var bytes = _stream.ToArray();
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(3), (uint)_resultCount);
        return bytes;
    }

    private static void WriteString(BinaryWriter writer, string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        writer.Write((uint)bytes.Length);
        writer.Write(bytes);
    }
}
