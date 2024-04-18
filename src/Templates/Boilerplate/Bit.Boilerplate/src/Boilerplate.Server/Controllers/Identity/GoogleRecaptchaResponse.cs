namespace Boilerplate.Server.Controllers.Identity;

public class GoogleRecaptchaResponse
{
    public bool Success { get; set; }
    public string? Challenge_ts { get; set; }
    public string? Hostname { get; set; }
}
