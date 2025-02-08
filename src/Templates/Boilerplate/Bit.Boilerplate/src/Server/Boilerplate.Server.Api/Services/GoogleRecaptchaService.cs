namespace Boilerplate.Server.Api.Services;

public partial class GoogleRecaptchaService
{
    [AutoInject] protected ServerApiSettings AppSettings = default!;

    [AutoInject] protected HttpClient httpClient = default!;

    public virtual async ValueTask<bool> Verify(string? googleRecaptchaResponse, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(googleRecaptchaResponse)) return false;

        var url = $"api/siteverify?secret={AppSettings.GoogleRecaptchaSecretKey}&response={googleRecaptchaResponse}";
        var response = await httpClient.PostAsync(url, null, cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync(ServerJsonContext.Default.GoogleRecaptchaVerificationResponse, cancellationToken: cancellationToken);

        return result?.Success is true;
    }
}
