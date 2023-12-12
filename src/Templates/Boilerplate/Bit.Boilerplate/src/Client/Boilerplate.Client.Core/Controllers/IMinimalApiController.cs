using System.Text.Json;

namespace Boilerplate.Client.Core.Controllers;

public interface IMinimalApiController : IAppController
{
    [HttpGet("api/minimal-api-sample/{routeParameter}{?queryStringParameter}")]
    Task<JsonDocument> MinimalApiSample(string routeParameter, string queryStringParameter, CancellationToken cancellationToken = default);
}
