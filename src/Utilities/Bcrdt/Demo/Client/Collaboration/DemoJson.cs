using System.Globalization;
using System.Text;

namespace Bit.Bcrdt.Demo.Client.Collaboration;

/// <summary>
/// Renders the plain-object projection returned by <c>ToJson()</c> as readable JSON text.
/// </summary>
/// <remarks>
/// Hand-written rather than delegated to <c>System.Text.Json</c> because the projection is a tree of
/// <c>object?</c> whose leaves are whatever the collaborative value model held. A few lines of
/// switch is more predictable here than reflection-based serialization, and it lets binary content
/// be summarized rather than dumped as base64.
/// </remarks>
public static class DemoJson
{
    /// <summary>Formats a projection as indented JSON.</summary>
    public static string Format(object? value)
    {
        var builder = new StringBuilder();
        Write(builder, value, 0);
        return builder.ToString();
    }

    private static void Write(StringBuilder builder, object? value, int depth)
    {
        switch (value)
        {
            case null:
                builder.Append("null");
                break;

            case string text:
                WriteString(builder, text);
                break;

            case bool flag:
                builder.Append(flag ? "true" : "false");
                break;

            case byte[] bytes:
                builder.Append('"').Append(bytes.Length).Append(" bytes\"");
                break;

            case IDictionary<string, object?> map:
                WriteObject(builder, map, depth);
                break;

            case IEnumerable<object?> list:
                WriteArray(builder, list, depth);
                break;

            default:
                builder.Append(Convert.ToString(value, CultureInfo.InvariantCulture) ?? "null");
                break;
        }
    }

    private static void WriteObject(StringBuilder builder, IDictionary<string, object?> map, int depth)
    {
        if (map.Count == 0)
        {
            builder.Append("{}");
            return;
        }

        builder.Append("{\n");
        int remaining = map.Count;

        foreach ((string key, object? item) in map)
        {
            Indent(builder, depth + 1);
            WriteString(builder, key);
            builder.Append(": ");
            Write(builder, item, depth + 1);
            if (--remaining > 0) builder.Append(',');
            builder.Append('\n');
        }

        Indent(builder, depth);
        builder.Append('}');
    }

    private static void WriteArray(StringBuilder builder, IEnumerable<object?> list, int depth)
    {
        List<object?> items = [.. list];
        if (items.Count == 0)
        {
            builder.Append("[]");
            return;
        }

        builder.Append("[\n");

        for (int index = 0; index < items.Count; index++)
        {
            Indent(builder, depth + 1);
            Write(builder, items[index], depth + 1);
            if (index < items.Count - 1) builder.Append(',');
            builder.Append('\n');
        }

        Indent(builder, depth);
        builder.Append(']');
    }

    private static void Indent(StringBuilder builder, int depth) => builder.Append(' ', depth * 2);

    private static void WriteString(StringBuilder builder, string text)
    {
        builder.Append('"');
        foreach (char character in text)
        {
            switch (character)
            {
                case '"': builder.Append("\\\""); break;
                case '\\': builder.Append("\\\\"); break;
                case '\n': builder.Append("\\n"); break;
                case '\r': builder.Append("\\r"); break;
                case '\t': builder.Append("\\t"); break;
                default:
                    if (char.IsControl(character)) builder.Append("\\u").Append(((int)character).ToString("x4"));
                    else builder.Append(character);
                    break;
            }
        }
        builder.Append('"');
    }
}
