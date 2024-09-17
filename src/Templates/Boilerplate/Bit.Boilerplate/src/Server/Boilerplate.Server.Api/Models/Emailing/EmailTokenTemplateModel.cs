namespace Boilerplate.Server.Api.Models.Emailing;

public partial class EmailTokenTemplateModel
{
    public string? Email { get; set; }
    public string? Token { get; set; }
    public Uri? Link { get; set; }
}
