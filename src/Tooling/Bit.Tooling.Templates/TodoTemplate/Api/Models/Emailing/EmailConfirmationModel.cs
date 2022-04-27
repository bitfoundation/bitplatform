namespace TodoTemplate.Api.Models.Emailing;

public class EmailConfirmationModel
{
    public string? ConfirmationLink { get; set; }

    public string? ProjectIconInBase64 { get; set; }

    public string? ProjectLink { get; set; }
}
