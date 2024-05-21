namespace Boilerplate.Server.Models.Emailing;

public class OtpTokenTemplateModel
{
    public string? DisplayName { get; set; }

    public required string Token { get; set; }
}
