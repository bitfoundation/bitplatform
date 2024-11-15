namespace Boilerplate.Shared.Controllers;

[AnonymousApi]
public interface IMinimalApiController : IAppController
{
    [HttpGet("api/minimal-api-sample/{routeParameter}{?queryStringParameter}")]
    Task<JsonElement> MinimalApiSample(string routeParameter, string? queryStringParameter, CancellationToken cancellationToken);
}
