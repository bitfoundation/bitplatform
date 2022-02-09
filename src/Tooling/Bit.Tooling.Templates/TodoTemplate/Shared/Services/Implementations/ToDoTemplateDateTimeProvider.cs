namespace TodoTemplate.Shared.Services.Implementations;

public class ToDoTemplateDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset GetCurrentDateTime()
    {
        return DateTimeOffset.UtcNow;
    }
}
