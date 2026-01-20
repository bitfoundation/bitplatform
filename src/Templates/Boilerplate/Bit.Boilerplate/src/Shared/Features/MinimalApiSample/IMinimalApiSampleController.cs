namespace Boilerplate.Shared.Features.MinimalApiSample;

public interface IMinimalApiSampleController : IAppController
{
    [HttpGet("api/minimal-api-sample/{routeParameter}{?queryStringParameter}")]
    Task<JsonElement> MinimalApiSample(string routeParameter, string? queryStringParameter, CancellationToken cancellationToken);
}
