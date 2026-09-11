namespace ModelContextProtocol.Client;

/// <summary>How a deployment is actually running, as opposed to what its configuration file on disk says.</summary>
public static class McpDeploymentExtensions
{
    extension(McpClient mcp)
    {
        /// <summary>GetDeploymentInfo's payload: the effective configuration, with nothing per-request in it.</summary>
        public async Task<JsonNode> GetDeploymentInfo(CancellationToken cancellationToken)
        {
            var text = await mcp.CallText("GetDeploymentInfo", arguments: null, cancellationToken);

            return JsonNode.Parse(text) ?? throw new InvalidOperationException($"GetDeploymentInfo returned no json. Result: '{text}'.");
        }

        /// <summary>
        /// What the api makes of this very call, as plain text - the same report the /diagnostic page and the hub
        /// give, so the three can be compared.
        /// </summary>
        public Task<string> GetDiagnosticReport(CancellationToken cancellationToken)
        {
            return mcp.CallText("GetDiagnosticReport", arguments: null, cancellationToken);
        }
    }
}
