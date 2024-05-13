namespace Boilerplate.Server.Models.Emailing;

public class SendTwoFactorTokenModel
{
    public required string DisplayName { get; set; }

    public required string Token { get; set; }
}
