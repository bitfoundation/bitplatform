using ModelContextProtocol.Protocol;

namespace ModelContextProtocol.Client;

/// <summary>
/// The one call every <c>/dev-mcp</c> read goes through, so an error answer fails the test rather than being parsed
/// as data.
/// </summary>
public static class McpToolExtensions
{
    extension(McpClient mcp)
    {
        public async Task<string> CallText(string tool, IReadOnlyDictionary<string, object?>? arguments, CancellationToken cancellationToken)
        {
            var result = await mcp.CallToolAsync(tool, arguments, cancellationToken: cancellationToken);
            var text = result.Content.OfType<TextContentBlock>().FirstOrDefault()?.Text ?? "";
            Assert.AreNotEqual(true, result.IsError, $"Tool '{tool}' returned an error. Result: '{text}'.");
            return text;
        }
    }
}
