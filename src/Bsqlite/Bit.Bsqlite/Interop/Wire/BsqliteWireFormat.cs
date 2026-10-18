using System.Buffers;
using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Bit.Bsqlite.Data;

namespace Bit.Bsqlite.Interop;

/// <summary>
/// Translates between the ADO.NET layer's types and the bytes the browser transport carries.
/// </summary>
/// <remarks>
/// <para>
/// A wire format exists because JSON cannot represent two things SQLite returns routinely: integers
/// outside the range a JavaScript number holds exactly, and blobs. Left to a plain JSON projection,
/// a 64-bit key silently loses its low bits and a blob arrives as an array of numbers. Both failures
/// are quiet, which is why the format is explicit rather than incidental.
/// </para>
/// <para>
/// The format is binary and little-endian. A batch is one byte array, a reply is one byte array, and
/// Blazor marshals byte arrays across JS interop without base64 or JSON parsing, so a result set
/// costs one allocation and a linear scan rather than a document object model. Every value travels
/// with its SQLite storage class, using SQLite's own type codes so the JavaScript half can label
/// values from <c>sqlite3_column_type</c> without a translation table.
/// <c>bit-bsqlite-wire.js</c> is the other half of this file and must agree with it byte for byte.
/// </para>
/// <para>
/// Layout of a batch: magic, version, request count, then per request the SQL (length-prefixed
/// UTF-8), a result-kind byte, a signed 32-bit row limit (-1 for none), and the parameters as
/// name, type tag, and payload. Layout of a reply: magic, version, status; on failure a message, a
/// result code (-1 for none), and an error name; on success a result count and per result the
/// columns (name and storage class), records affected, last insert rowid, and the rows as tagged
/// cells.
/// </para>
/// </remarks>
public static class BsqliteWireFormat
{
    /// <summary>First byte of every message, so a stray buffer is rejected rather than misread.</summary>
    public const byte Magic = 0xB5;

    /// <summary>Bumped whenever the layout changes; the decoder refuses anything else.</summary>
    public const byte Version = 1;

    private const byte StatusOk = 0;
    private const byte StatusError = 1;

    /// <summary>SQLite's storage class codes, used verbatim as the wire type tags.</summary>
    public static class TypeCode
    {
        /// <summary><c>SQLITE_INTEGER</c>.</summary>
        public const int Integer = 1;

        /// <summary><c>SQLITE_FLOAT</c>.</summary>
        public const int Real = 2;

        /// <summary><c>SQLITE_TEXT</c>.</summary>
        public const int Text = 3;

        /// <summary><c>SQLITE_BLOB</c>.</summary>
        public const int Blob = 4;

        /// <summary><c>SQLITE_NULL</c>.</summary>
        public const int Null = 5;
    }

    private static readonly string[] StorageClassNames = [string.Empty, "INTEGER", "REAL", "TEXT", "BLOB"];

    // ---------------------------------------------------------------------------------------------
    // Encoding a batch
    // ---------------------------------------------------------------------------------------------

    /// <summary>Encodes a batch for the worker.</summary>
    public static byte[] EncodeBatch(IReadOnlyList<BsqliteCommandRequest> batch)
    {
        ArgumentNullException.ThrowIfNull(batch);

        var writer = new WireWriter(EstimateBatchSize(batch));
        writer.WriteByte(Magic);
        writer.WriteByte(Version);
        writer.WriteUInt32((uint)batch.Count);

        for (var i = 0; i < batch.Count; i++)
        {
            var request = batch[i];
            writer.WriteString(request.CommandText);
            writer.WriteByte(EncodeResultKind(request.ResultKind));
            writer.WriteInt32(request.RowLimit is { } limit && limit >= 0 ? limit : -1);

            var parameters = request.Parameters;
            if (parameters.Count > ushort.MaxValue)
            {
                throw new NotSupportedException(
                    $"A request carries {parameters.Count} parameters; the wire format allows {ushort.MaxValue}.");
            }

            writer.WriteUInt16((ushort)parameters.Count);
            for (var p = 0; p < parameters.Count; p++)
            {
                var parameter = parameters[p];
                writer.WriteString(parameter.Name);
                WriteValue(ref writer, parameter.Value, parameter.Name);
            }
        }

        return writer.ToArray();
    }

    private static int EstimateBatchSize(IReadOnlyList<BsqliteCommandRequest> batch)
    {
        var size = 16;
        for (var i = 0; i < batch.Count; i++)
        {
            var request = batch[i];
            size += 16 + request.CommandText.Length * 2;
            for (var p = 0; p < request.Parameters.Count; p++)
            {
                size += 24 + request.Parameters[p].Value switch
                {
                    string text => text.Length * 2,
                    byte[] blob => blob.Length,
                    _ => 8,
                };
            }
        }

        return size;
    }

    private static byte EncodeResultKind(BsqliteResultKind kind) => kind switch
    {
        BsqliteResultKind.NonQuery => 0,
        BsqliteResultKind.Scalar => 1,
        BsqliteResultKind.Reader => 2,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown result kind."),
    };

    private static void WriteValue(ref WireWriter writer, object? value, string parameterName)
    {
        var (type, normalized) = EncodeValue(value, parameterName);
        writer.WriteByte((byte)type);

        switch (type)
        {
            case TypeCode.Null:
                break;

            case TypeCode.Integer:
                writer.WriteInt64((long)normalized!);
                break;

            case TypeCode.Real:
                writer.WriteDouble((double)normalized!);
                break;

            case TypeCode.Text:
                writer.WriteString((string)normalized!);
                break;

            case TypeCode.Blob:
                writer.WriteBytes((ReadOnlyMemory<byte>)normalized!);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(value), type, "Unknown wire type.");
        }
    }

    /// <summary>
    /// Maps a parameter value onto a storage class and the single representation the wire carries
    /// for it: <see cref="long"/>, <see cref="double"/>, <see cref="string"/>,
    /// <see cref="ReadOnlyMemory{T}"/> of <see cref="byte"/>, or <see langword="null"/>.
    /// </summary>
    /// <remarks>
    /// Only conversions with one obvious answer are performed here. Dates, times, and GUIDs have more
    /// than one reasonable SQLite representation, and choosing silently would put this layer in
    /// disagreement with the ADO.NET binder that owns the decision, so they are rejected with a message
    /// saying so. <see cref="BsqliteParameterBinding"/> converts them before a parameter reaches the
    /// transport.
    /// </remarks>
    public static (int Type, object? Value) EncodeValue(object? value, string parameterName)
    {
        switch (value)
        {
            case null or DBNull:
                return (TypeCode.Null, null);

            // SQLite has no boolean storage class; it stores 0 and 1, as every SQLite provider does.
            case bool flag:
                return (TypeCode.Integer, flag ? 1L : 0L);

            case long int64:
                return (TypeCode.Integer, int64);

            case int int32:
                return (TypeCode.Integer, (long)int32);

            case byte or sbyte or short or ushort or uint:
                return (TypeCode.Integer, Convert.ToInt64(value, CultureInfo.InvariantCulture));

            case ulong unsigned when unsigned <= long.MaxValue:
                return (TypeCode.Integer, (long)unsigned);

            case ulong tooLarge:
                throw new NotSupportedException(
                    $"Parameter '{parameterName}' is {tooLarge}, which exceeds the range of SQLite's "
                    + "64-bit signed INTEGER.");

            // SQLite stores NaN as NULL, so it is sent as one; infinities are ordinary REALs.
            case double real:
                return double.IsNaN(real) ? (TypeCode.Null, null) : (TypeCode.Real, real);

            case float single:
                return float.IsNaN(single) ? (TypeCode.Null, null) : (TypeCode.Real, (double)single);

            // Text, so that no precision is lost. Comparisons and arithmetic over these columns are
            // what the ef_* function set exists to make correct.
            case decimal money:
                return (TypeCode.Text, FormatDecimal(money));

            case string text:
                return (TypeCode.Text, text);

            case char character:
                return (TypeCode.Text, character.ToString());

            case byte[] blob:
                return (TypeCode.Blob, new ReadOnlyMemory<byte>(blob));

            case ReadOnlyMemory<byte> memory:
                return (TypeCode.Blob, memory);

            case Memory<byte> memory:
                return (TypeCode.Blob, (ReadOnlyMemory<byte>)memory);

            case ArraySegment<byte> segment:
                return (TypeCode.Blob, (ReadOnlyMemory<byte>)segment);

            default:
                throw new NotSupportedException(
                    $"Parameter '{parameterName}' is of type {value.GetType()}, which the transport does "
                    + "not convert. Values with more than one sensible SQLite representation - dates, "
                    + "times, and GUIDs among them - must be converted by the ADO.NET binder before "
                    + "reaching the transport, so that one component owns the choice.");
        }
    }

    /// <summary>
    /// Writes a decimal the way Microsoft.Data.Sqlite writes one, which is the canonical TEXT form
    /// for the whole SQLite/EF stack.
    /// </summary>
    /// <remarks>
    /// Not <c>ToString(InvariantCulture)</c>, which preserves the CLR value's trailing zeros: that
    /// renders <c>10m</c> as <c>"10"</c> and <c>1.50m</c> as <c>"1.50"</c>, while the rest of the
    /// stack writes <c>"10.0"</c> and <c>"1.5"</c>. The difference is not cosmetic. EF Core compiles
    /// decimal equality to a plain TEXT comparison against a literal it renders with this same
    /// format - <c>WHERE "Price" = '10.0'</c> - so a row the browser wrote as <c>"10"</c> would
    /// never match, silently. It also makes a database written in the browser differ byte-for-byte
    /// from the same one written on the server, which is the promise this library exists to keep.
    /// </remarks>
    internal static string FormatDecimal(decimal value)
        => value.ToString(DecimalFormat, CultureInfo.InvariantCulture);

    /// <summary>Microsoft.Data.Sqlite's decimal format string, reproduced exactly.</summary>
    private const string DecimalFormat = "0.0###########################";

    // ---------------------------------------------------------------------------------------------
    // Decoding a reply
    // ---------------------------------------------------------------------------------------------

    /// <summary>Decodes the worker's reply into results the ADO.NET layer can read.</summary>
    /// <exception cref="BsqliteException">The worker reported a failure.</exception>
    /// <exception cref="FormatException">The reply did not match the wire format.</exception>
    public static IReadOnlyList<BsqliteCommandResult> DecodeReply(ReadOnlySpan<byte> reply)
    {
        var reader = new WireReader(reply);

        var magic = reader.ReadByte();
        if (magic != Magic)
        {
            throw new FormatException($"Not a Bsqlite wire message (leading byte 0x{magic:X2}).");
        }

        var version = reader.ReadByte();
        if (version != Version)
        {
            throw new FormatException(
                $"Unsupported wire version {version}; this build speaks version {Version}. "
                + "The browser is running Bsqlite JavaScript from a different release than this "
                + "assembly - typically a cached copy; reload without the cache.");
        }

        var status = reader.ReadByte();
        if (status == StatusError)
        {
            var message = reader.ReadString();
            var code = reader.ReadInt32();
            var name = reader.ReadString();
            throw CreateException(message, code < 0 ? null : code, name);
        }

        if (status != StatusOk)
        {
            throw new FormatException($"Unknown reply status {status}.");
        }

        var count = checked((int)reader.ReadUInt32());
        var results = new BsqliteCommandResult[count];
        for (var i = 0; i < count; i++)
        {
            results[i] = ReadResult(ref reader);
        }

        if (reader.Remaining != 0)
        {
            throw new FormatException($"The reply carries {reader.Remaining} unexpected trailing byte(s).");
        }

        return results;
    }

    private static BsqliteCommandResult ReadResult(ref WireReader reader)
    {
        var columnCount = reader.ReadUInt16();
        var columnNames = columnCount == 0 ? [] : new string[columnCount];
        var columnTypes = columnCount == 0 ? [] : new string?[columnCount];

        for (var c = 0; c < columnCount; c++)
        {
            columnNames[c] = reader.ReadString();
            var storageClass = reader.ReadByte();
            columnTypes[c] = storageClass is >= 1 and <= 4 ? StorageClassNames[storageClass] : null;
        }

        var recordsAffected = reader.ReadInt32();
        var lastInsertRowId = reader.ReadInt64();
        var rowCount = checked((int)reader.ReadUInt32());
        var rows = rowCount == 0 ? [] : new object?[rowCount][];

        for (var r = 0; r < rowCount; r++)
        {
            var cells = columnCount == 0 ? [] : new object?[columnCount];
            for (var c = 0; c < columnCount; c++)
            {
                cells[c] = ReadValue(ref reader);
            }

            rows[r] = cells;
        }

        return new BsqliteCommandResult
        {
            ColumnNames = columnNames,
            ColumnTypes = columnTypes,
            Rows = rows,
            RecordsAffected = recordsAffected,
            LastInsertRowId = lastInsertRowId,
        };
    }

    private static object? ReadValue(ref WireReader reader)
    {
        var type = reader.ReadByte();
        return type switch
        {
            TypeCode.Null => null,
            TypeCode.Integer => reader.ReadInt64(),
            TypeCode.Real => reader.ReadDouble(),
            TypeCode.Text => reader.ReadString(),
            TypeCode.Blob => reader.ReadBytes(),
            _ => throw new FormatException($"Unknown wire type tag {type}."),
        };
    }

    // ---------------------------------------------------------------------------------------------
    // The JSON envelope used by control requests (open, close, version)
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// Reads the envelope returned by the host's <c>call</c> method, which never throws across JS
    /// interop - Blazor would otherwise reduce a JavaScript error to its message and drop the SQLite
    /// result code.
    /// </summary>
    /// <exception cref="BsqliteException">The worker reported a failure.</exception>
    /// <exception cref="FormatException">The reply did not match the envelope.</exception>
    public static JsonElement DecodeCall(JsonElement envelope)
    {
        if (envelope.ValueKind != JsonValueKind.Object
            || !envelope.TryGetProperty("ok", out var ok)
            || ok.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            throw new FormatException(
                "Expected a { ok, result } or { ok, error } envelope from the worker.");
        }

        if (ok.GetBoolean())
        {
            return envelope.TryGetProperty("result", out var result)
                ? result
                : default;
        }

        throw ReadError(envelope);
    }

    private static BsqliteException ReadError(JsonElement envelope)
    {
        if (!envelope.TryGetProperty("error", out var error)
            || error.ValueKind != JsonValueKind.Object)
        {
            throw new BsqliteException("The SQLite worker reported a failure without a message.");
        }

        var message = error.TryGetProperty("message", out var text) && text.ValueKind == JsonValueKind.String
            ? text.GetString()
            : null;

        int? code = error.TryGetProperty("sqliteCode", out var sqliteCode)
            && sqliteCode.ValueKind == JsonValueKind.Number
                ? sqliteCode.GetInt32()
                : null;

        var name = error.TryGetProperty("name", out var nameElement)
            && nameElement.ValueKind == JsonValueKind.String
                ? nameElement.GetString()
                : null;

        return CreateException(message, code, name);
    }

    /// <summary>
    /// Builds the typed exception for a worker failure, so every path - binary reply, JSON envelope,
    /// or a transport's own error - classifies result codes the same way.
    /// </summary>
    public static BsqliteException CreateException(string? message, int? sqliteErrorCode, string? errorName)
    {
        var text = string.IsNullOrWhiteSpace(message)
            ? "The SQLite worker reported a failure."
            : message;

        if (sqliteErrorCode is 13 || string.Equals(errorName, "QuotaExceededError", StringComparison.Ordinal))
        {
            return new BsqliteQuotaExceededException(text, sqliteErrorCode ?? 13);
        }

        return sqliteErrorCode switch
        {
            11 or 26 => new BsqliteCorruptDatabaseException(text, sqliteErrorCode),
            5 or 6 => new BsqliteConcurrencyException(text, sqliteErrorCode),
            19 => new BsqliteConstraintException(text, sqliteErrorCode),
            _ => new BsqliteException(text, sqliteErrorCode),
        };
    }

    // ---------------------------------------------------------------------------------------------
    // Primitives
    // ---------------------------------------------------------------------------------------------

    private ref struct WireWriter
    {
        private byte[] _buffer;
        private int _length;

        public WireWriter(int initialCapacity)
        {
            _buffer = ArrayPool<byte>.Shared.Rent(Math.Max(initialCapacity, 256));
            _length = 0;
        }

        private Span<byte> Reserve(int count)
        {
            if (_length + count > _buffer.Length)
            {
                var grown = ArrayPool<byte>.Shared.Rent(Math.Max(_buffer.Length * 2, _length + count));
                _buffer.AsSpan(0, _length).CopyTo(grown);
                ArrayPool<byte>.Shared.Return(_buffer);
                _buffer = grown;
            }

            var span = _buffer.AsSpan(_length, count);
            _length += count;
            return span;
        }

        public void WriteByte(byte value) => Reserve(1)[0] = value;

        public void WriteUInt16(ushort value) => BinaryPrimitives.WriteUInt16LittleEndian(Reserve(2), value);

        public void WriteInt32(int value) => BinaryPrimitives.WriteInt32LittleEndian(Reserve(4), value);

        public void WriteUInt32(uint value) => BinaryPrimitives.WriteUInt32LittleEndian(Reserve(4), value);

        public void WriteInt64(long value) => BinaryPrimitives.WriteInt64LittleEndian(Reserve(8), value);

        public void WriteDouble(double value) => BinaryPrimitives.WriteDoubleLittleEndian(Reserve(8), value);

        public void WriteBytes(ReadOnlyMemory<byte> bytes)
        {
            WriteUInt32((uint)bytes.Length);
            bytes.Span.CopyTo(Reserve(bytes.Length));
        }

        public void WriteString(string text)
        {
            var byteCount = Encoding.UTF8.GetByteCount(text);
            WriteUInt32((uint)byteCount);
            Encoding.UTF8.GetBytes(text, Reserve(byteCount));
        }

        public byte[] ToArray()
        {
            var result = _buffer.AsSpan(0, _length).ToArray();
            ArrayPool<byte>.Shared.Return(_buffer);
            _buffer = [];
            return result;
        }
    }

    private ref struct WireReader(ReadOnlySpan<byte> data)
    {
        private readonly ReadOnlySpan<byte> _data = data;
        private int _position = 0;

        public readonly int Remaining => _data.Length - _position;

        private ReadOnlySpan<byte> Take(int count)
        {
            if (count < 0 || _position + count > _data.Length)
            {
                throw new FormatException(
                    $"Truncated wire message: needed {count} byte(s) at offset {_position} of {_data.Length}.");
            }

            var span = _data.Slice(_position, count);
            _position += count;
            return span;
        }

        public byte ReadByte() => Take(1)[0];

        public ushort ReadUInt16() => BinaryPrimitives.ReadUInt16LittleEndian(Take(2));

        public int ReadInt32() => BinaryPrimitives.ReadInt32LittleEndian(Take(4));

        public uint ReadUInt32() => BinaryPrimitives.ReadUInt32LittleEndian(Take(4));

        public long ReadInt64() => BinaryPrimitives.ReadInt64LittleEndian(Take(8));

        public double ReadDouble() => BinaryPrimitives.ReadDoubleLittleEndian(Take(8));

        public byte[] ReadBytes()
        {
            var length = checked((int)ReadUInt32());
            return Take(length).ToArray();
        }

        public string ReadString()
        {
            var length = checked((int)ReadUInt32());
            return length == 0 ? string.Empty : Encoding.UTF8.GetString(Take(length));
        }
    }
}
