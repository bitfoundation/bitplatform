namespace TodoTemplate.Api.Models.Emailing;

public class EmailConfirmationModel
{
    public string? ConfirmationLink { get; set; }

    public string? HostUri { get; set; }
}
