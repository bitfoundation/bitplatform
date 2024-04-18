namespace Boilerplate.Server.Controllers.Identity;

public class GoogleRecaptchaVerificationResponse
{
    public bool Success { get; set; }
    public string? Challenge_ts { get; set; }
    public string? Hostname { get; set; }
    public string[]? Error_codes { get; set; }
}
