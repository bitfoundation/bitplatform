using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Bit.Belemetry;

/// <summary>
/// Reads the <c>partial_success</c> of an OTLP export response - <c>Export{Trace,Logs,Metrics}ServiceResponse</c>,
/// whose field 1 holds the number of rejected items (field 1) and an error message (field 2). The specification says a
/// client must not retry it, but the rejection is worth saying: without it data vanishes with a 200.
/// </summary>
internal static class OtlpPartialSuccess
{
    /// <summary>"N rejected: message", or null when the response reports nothing rejected and no warning.</summary>
    public static string? Describe(ReadOnlySpan<byte> body, bool json) => Describe(body, json, out _);

    /// <summary>"N rejected: message", or null when the response reports nothing rejected and no warning; <paramref name="rejected"/> is N.</summary>
    public static string? Describe(ReadOnlySpan<byte> body, bool json, out long rejected)
    {
        rejected = 0;
        try
        {
            var (count, message) = json ? ReadJson(body) : ReadProtobuf(body);
            if (count <= 0 && string.IsNullOrEmpty(message)) return null;
            rejected = Math.Max(0, count);
            return count > 0
                ? $"{count.ToString(CultureInfo.InvariantCulture)} rejected{(string.IsNullOrEmpty(message) ? string.Empty : ": " + message)}"
                : "warning: " + message;
        }
        catch (Exception)
        {
            // Whatever is wrong with the body (JSON, a varint, an overflowing count), it only informs: a response that
            // is not what it should be must never make a delivered export look failed - that would resend it.
            return null;
        }
    }

    private static (long Rejected, string? Message) ReadJson(ReadOnlySpan<byte> body)
    {
        // A byte order mark is not JSON; a server may still send one.
        if (body.StartsWith(Encoding.UTF8.Preamble)) body = body[Encoding.UTF8.Preamble.Length..];
        using var document = JsonDocument.Parse(body.ToArray());
        if (document.RootElement.ValueKind != JsonValueKind.Object || document.RootElement.TryGetProperty("partialSuccess", out var partial) is false
            || partial.ValueKind != JsonValueKind.Object)
        {
            return (0, null);
        }

        long rejected = 0;
        string? message = null;
        foreach (var property in partial.EnumerateObject())
        {
            if (property.Name == "errorMessage" && property.Value.ValueKind == JsonValueKind.String)
            {
                message = property.Value.GetString();
            }
            else if (property.Name.StartsWith("rejected", StringComparison.Ordinal))
            {
                // int64 travels as a decimal string in OTLP/JSON, though some servers send a number.
                rejected = property.Value.ValueKind == JsonValueKind.Number ? property.Value.GetInt64()
                         : long.Parse(property.Value.GetString() ?? "0", NumberStyles.Integer, CultureInfo.InvariantCulture);
            }
        }

        return (rejected, message);
    }

    private static (long Rejected, string? Message) ReadProtobuf(ReadOnlySpan<byte> body)
    {
        var position = 0;
        while (position < body.Length)
        {
            var tag = ReadVarint(body, ref position);
            if (tag >> 3 == 1 && (tag & 7) == 2)
            {
                var length = ReadLength(body, ref position);
                return ReadPartial(body.Slice(position, length));
            }
            Skip(body, ref position, (int)(tag & 7));
        }

        return (0, null);
    }

    private static (long Rejected, string? Message) ReadPartial(ReadOnlySpan<byte> message)
    {
        long rejected = 0;
        string? text = null;
        var position = 0;
        while (position < message.Length)
        {
            var tag = ReadVarint(message, ref position);
            switch (tag >> 3, tag & 7)
            {
                case (1, 0):
                    rejected = (long)ReadVarint(message, ref position);
                    break;
                case (2, 2):
                    var length = ReadLength(message, ref position);
                    text = Encoding.UTF8.GetString(message.Slice(position, length));
                    position += length;
                    break;
                default:
                    Skip(message, ref position, (int)(tag & 7));
                    break;
            }
        }

        return (rejected, text);
    }

    private static ulong ReadVarint(ReadOnlySpan<byte> data, ref int position)
    {
        ulong result = 0;
        for (var shift = 0; shift < 64; shift += 7)
        {
            if (position >= data.Length) throw new FormatException("A truncated varint.");
            var b = data[position++];
            result |= (ulong)(b & 0x7F) << shift;
            if ((b & 0x80) == 0) return result;
        }

        throw new FormatException("A varint longer than 10 bytes.");
    }

    /// <summary>A length prefix, checked against what is left: a hostile or broken one must never move the reader backwards (which would loop forever) or past the end.</summary>
    private static int ReadLength(ReadOnlySpan<byte> data, ref int position)
    {
        var length = ReadVarint(data, ref position);
        if (length > (ulong)(data.Length - position)) throw new FormatException("A length runs past the end of the message.");
        return (int)length;
    }

    private static void Skip(ReadOnlySpan<byte> data, ref int position, int wireType)
    {
        var skip = wireType switch
        {
            0 => 0,
            1 => 8,
            2 => ReadLength(data, ref position),
            5 => 4,
            _ => throw new FormatException($"Wire type {wireType} cannot be skipped."),
        };

        if (wireType == 0) ReadVarint(data, ref position);
        else if (skip > data.Length - position) throw new FormatException("A field runs past the end of the message.");
        else position += skip;
    }
}
