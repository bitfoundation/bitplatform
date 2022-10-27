using AdminPanel.Server.Api.Resources;

namespace AdminPanel.Server.Api.Models.Emailing;

public class ResetPasswordModel
{
    public string? DisplayName { get; set; }

    public string? ResetPasswordLink { get; set; }

    public Uri? HostUri { get; set; }

    public IStringLocalizer<EmailStrings> EmailLocalizer { get; set; } = default!;
}
