using Microsoft.Agents.AI;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Boilerplate.Server.Api.Infrastructure.HealthChecks;

/// <summary>
/// Gets a real answer from an agent built on the app's <see cref="IChatClient"/>, which also catches exhausted quota.
/// Not the chatbot's SupportAgent, whose prompt and tools would make every check expensive.
/// </summary>
public partial class AIChatAgentHealthCheck : IHealthCheck
{
    /// <summary>The smallest output limit every OpenAI api accepts (the Responses api rejects less).</summary>
    public const int MaxOutputTokens = 16;

    [AutoInject] private IChatClient chatClient = default!;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var agent = chatClient.AsAIAgent(instructions: "Reply with OK.", name: "HealthCheckAgent");

            var response = await agent.RunAsync("Ping",
                options: new ChatClientAgentRunOptions(new ChatOptions { MaxOutputTokens = MaxOutputTokens }),
                cancellationToken: cancellationToken);

            return HealthCheckResult.Healthy("Chat agent answered", data: new Dictionary<string, object>
            {
                ["TotalTokens"] = response.Usage?.TotalTokenCount ?? 0
            });
        }
        catch (Exception exp)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "Chat agent did not answer", exp);
        }
    }
}
