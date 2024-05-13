namespace Boilerplate.Server.Models.Emailing;

public class SendResetPasswordTokenModel
{
    public string? DisplayName { get; set; }

    public Uri? ResetPasswordLink { get; set; }
}
