namespace Boilerplate.Shared.Infrastructure.Dtos.SignalR;

public class BackgroundJobProgressDto
{
    public required string JobId { get; set; }
    public required string JobTitle { get; set; }
    public int SucceededItems { get; set; }
    public int TotalItems { get; set; }
    public int FailedItems { get; set; }
}
