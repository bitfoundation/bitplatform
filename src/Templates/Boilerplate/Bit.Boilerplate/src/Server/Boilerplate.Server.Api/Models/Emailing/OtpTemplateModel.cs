namespace Boilerplate.Server.Api.Models.Emailing;

public partial class OtpTemplateModel
{
    public string? DisplayName { get; set; }
    public required string Token { get; set; }
    public required Uri Link { get; set; }
}
