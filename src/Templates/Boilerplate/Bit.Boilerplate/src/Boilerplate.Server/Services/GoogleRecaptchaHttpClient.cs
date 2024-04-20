namespace Boilerplate.Server.Services;

public partial class GoogleRecaptchaHttpClient
{
    [AutoInject] protected AppSettings AppSettings = default!;

    [AutoInject] protected HttpClient httpClient = default!;

    public async ValueTask<bool> Verify(string? googleRecaptchaResponse)
    {
        if (string.IsNullOrWhiteSpace(googleRecaptchaResponse)) return false;

        var url = $"api/siteverify?secret={AppSettings.GoogleRecaptchaSecretKey}&response={googleRecaptchaResponse}";
        var response = await httpClient.PostAsync(url, null);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync(ServerJsonContext.Default.GoogleRecaptchaVerificationResponse);

        return result?.Success is true;
    }
}
