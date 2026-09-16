namespace Boilerplate.Server.Api.Features.Identity.Services;

public partial class GoogleRecaptchaService
{
    [AutoInject] protected ServerApiSettings AppSettings = default!;

    [AutoInject] protected HttpClient httpClient = default!;

    [AutoInject] protected JsonSerializerOptions jsonSerializerOptions = default!;

    public virtual async ValueTask<bool> Verify(string? googleRecaptchaResponse, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(googleRecaptchaResponse)) return false;

        var result = await SiteVerify(googleRecaptchaResponse, cancellationToken);

        return result?.Success is true;
    }

    /// <summary>
    /// Verifies a response that can never be valid. Google checks the response before the secret, so this proves only
    /// that siteverify is reachable and answering, not that the secret is right.
    /// </summary>
    public virtual async ValueTask EnsureReachable(CancellationToken cancellationToken)
    {
        var result = await SiteVerify("health-check", cancellationToken)
            ?? throw new InvalidOperationException("siteverify answered with an unrecognized error.");

        if (result.ErrorCodes?.Contains("invalid-input-response") is not true)
            throw new InvalidOperationException($"siteverify answered with unexpected errors: {string.Join(", ", result.ErrorCodes ?? [])}");
    }

    private async ValueTask<GoogleRecaptchaVerificationResponse?> SiteVerify(string googleRecaptchaResponse, CancellationToken cancellationToken)
    {
        using var payload = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "secret", AppSettings.GoogleRecaptchaSecretKey! },
            { "response", googleRecaptchaResponse }
        });

        using var response = await httpClient.PostAsync("api/siteverify", payload, cancellationToken);

        if (response.IsSuccessStatusCode is false)
            return null;

        return await response.Content.ReadFromJsonAsync(jsonSerializerOptions.GetTypeInfo<GoogleRecaptchaVerificationResponse>(), cancellationToken);
    }
}

public partial class GoogleRecaptchaVerificationResponse
{
    public bool Success { get; set; }

    [JsonPropertyName("challenge_ts")]
    public string? ChallengeTimestamp { get; set; }

    public string? Hostname { get; set; }

    [JsonPropertyName("error-codes")]
    public string[]? ErrorCodes { get; set; }
}
