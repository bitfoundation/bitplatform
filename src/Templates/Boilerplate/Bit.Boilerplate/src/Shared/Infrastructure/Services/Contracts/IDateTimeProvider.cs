namespace Boilerplate.Shared.Infrastructure.Services.Contracts;

public interface IDateTimeProvider
{
    DateTimeOffset GetCurrentDateTime();
}
