namespace Bit.Sales.WebSite.App.Services.Implementations;

public class AppHttpClientHandler : HttpClientHandler
{

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode && response.Content.Headers.ContentType?.MediaType == "application/json")
        {
            if (response.Headers.TryGetValues("Request-ID", out IEnumerable<string>? values) && values is not null && values.Any())
            {
                RestExceptionPayload restError = await response.Content.ReadFromJsonAsync(AppJsonContext.Default.RestExceptionPayload);

                Type exceptionType = typeof(RestExceptionPayload).Assembly.GetType(restError.ExceptionType) ?? typeof(UnknownException);

                Exception exp = (Exception)Activator.CreateInstance(exceptionType, args: new object[] { restError.Message });

                if (exp is ResourceValidationException resValidationException)
                {
                    resValidationException.Details = restError.Details;
                }

                throw exp;
            }
        }

        response.EnsureSuccessStatusCode();

        return response;
    }
}
