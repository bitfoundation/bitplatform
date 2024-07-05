﻿namespace Boilerplate.Api.Services;

public class GoogleRecaptchaVerificationResponse
{
    public bool Success { get; set; }

    [JsonPropertyName("challenge_ts")]
    public string? ChallengeTimestamp { get; set; }

    public string? Hostname { get; set; }

    [JsonPropertyName("error_codes")]
    public string[]? ErrorCodes { get; set; }
}
