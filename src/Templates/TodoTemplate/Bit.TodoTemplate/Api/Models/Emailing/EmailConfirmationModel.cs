using TodoTemplate.Api.Resources;

namespace TodoTemplate.Api.Models.Emailing;

public class EmailConfirmationModel
{
    public string? ConfirmationLink { get; set; }

    public Uri? HostUri { get; set; }

    public IStringLocalizer<EmailStrings> EmailLocalizer { get; set; } = default!;
}
