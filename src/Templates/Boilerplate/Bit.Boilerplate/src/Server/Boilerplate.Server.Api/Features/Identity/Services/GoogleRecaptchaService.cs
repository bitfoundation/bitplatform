namespace Boilerplate.Server.Api.Features.Identity.Services;

public partial class GoogleRecaptchaService
{
    [AutoInject] protected ServerApiSettings AppSettings = default!;

    [AutoInject] protected HttpClient httpClient = default!;

    [AutoInject] protected JsonSerializerOptions jsonSerializerOptions = default!;

    public virtual async ValueTask<bool> Verify(string? googleRecaptchaResponse, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(googleRecaptchaResponse)) return false;

        using var payload = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "secret", AppSettings.GoogleRecaptchaSecretKey! },
            { "response", googleRecaptchaResponse }
        });

        var response = await httpClient.PostAsync("api/siteverify", payload, cancellationToken);

        if (response.IsSuccessStatusCode is false)
            return false;

        var result = await response.Content.ReadFromJsonAsync(jsonSerializerOptions.GetTypeInfo<GoogleRecaptchaVerificationResponse>(), cancellationToken);

        return result?.Success is true;
    }
}

public partial class GoogleRecaptchaVerificationResponse
{
    public bool Success { get; set; }

    [JsonPropertyName("challenge_ts")]
    public string? ChallengeTimestamp { get; set; }

    public string? Hostname { get; set; }

    [JsonPropertyName("error_codes")]
    public string[]? ErrorCodes { get; set; }
}
