namespace Boilerplate.Server.Models.Emailing;

public class ResetPasswordTokenTemplateModel
{
    public string? DisplayName { get; set; }

    public required string Token { get; set; }
}
