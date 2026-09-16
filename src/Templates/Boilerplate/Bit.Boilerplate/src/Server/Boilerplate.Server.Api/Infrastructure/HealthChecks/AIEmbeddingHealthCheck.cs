using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Boilerplate.Server.Api.Infrastructure.HealthChecks;

/// <summary>
/// Embeds one word with the app's own generator, so it covers whichever provider is configured (OpenAI compatible or
/// HuggingFace), including exhausted quota.
/// </summary>
public partial class AIEmbeddingHealthCheck : IHealthCheck
{
    [AutoInject] private IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator = default!;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var embeddings = await embeddingGenerator.GenerateAsync(["ping"], cancellationToken: cancellationToken);

            if (embeddings is not [{ Vector.Length: > 0 } embedding])
                throw new InvalidOperationException("The embedding provider returned no vector.");

            return HealthCheckResult.Healthy("Embedding generator answered", data: new Dictionary<string, object> { ["Dimensions"] = embedding.Vector.Length });
        }
        catch (Exception exp)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "Embedding generator did not answer", exp);
        }
    }
}
