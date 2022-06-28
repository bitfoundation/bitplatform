namespace TodoTemplate.Shared.Services.Implementations;

public class TodoTemplateDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset GetCurrentDateTime()
    {
        return DateTimeOffset.UtcNow;
    }
}
