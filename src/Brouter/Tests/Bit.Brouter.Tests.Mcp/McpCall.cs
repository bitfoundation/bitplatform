using System.Text.Json;
using ModelContextProtocol.Protocol;

namespace Bit.Brouter.Tests.Mcp;

/// <summary>
/// Calls a tool the way a client does and hands back what a client would actually get.
/// <para>
/// Every call goes over the connected session rather than into the controller, so a tool that
/// throws, answers with nothing, or returns something that does not serialize fails here - with
/// the tool's name in the message - instead of being read past by the test that called it.
/// </para>
/// </summary>
internal static class McpCall
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    /// <summary>The raw result, for the tests that are about the failure rather than the answer.</summary>
    public static Task<CallToolResult> RawAsync(string tool, Dictionary<string, object?>? arguments = null)
        => McpTestHost.Client.CallToolAsync(tool, arguments).AsTask();

    /// <summary>The text of a prose tool's answer.</summary>
    public static async Task<string> TextAsync(string tool, Dictionary<string, object?>? arguments = null)
    {
        var result = await RawAsync(tool, arguments);

        AssertSucceeded(tool, result);

        var text = string.Join('\n', result.Content.OfType<TextContentBlock>().Select(block => block.Text));

        Assert.IsFalse(string.IsNullOrWhiteSpace(text), $"'{tool}' answered with no text at all.");

        return text;
    }

    /// <summary>
    /// A structured tool's answer, deserialized into the DTO the tool declares. This is the round
    /// trip that matters for a client: the object is rebuilt out of the JSON that went over the
    /// wire, under the output schema the tool published, not out of the instance the method made.
    /// </summary>
    public static async Task<T> StructuredAsync<T>(string tool, Dictionary<string, object?>? arguments = null)
    {
        var result = await RawAsync(tool, arguments);

        AssertSucceeded(tool, result);

        Assert.IsNotNull(result.StructuredContent,
            $"'{tool}' returned no structured content, so a client has nothing to validate against its output schema.");

        var value = result.StructuredContent.Value.Deserialize<T>(_json);

        Assert.IsNotNull(value, $"'{tool}' returned structured content that does not deserialize into {typeof(T).Name}.");

        return value;
    }

    /// <summary>The text of a tool call that is expected to answer, rather than to fail.</summary>
    private static void AssertSucceeded(string tool, CallToolResult result)
    {
        if (result.IsError is not true) return;

        var text = string.Join('\n', result.Content.OfType<TextContentBlock>().Select(block => block.Text));

        Assert.Fail($"'{tool}' answered with an error: {text}");
    }
}
