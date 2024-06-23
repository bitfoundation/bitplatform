namespace Boilerplate.Server.Models.Emailing;

public class EmailTokenTemplateModel
{
    public string? Email { get; set; }
    public string? Token { get; set; }
    public Uri? Link { get; set; }
}
