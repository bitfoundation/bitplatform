using TodoTemplate.Shared.Services.Implementations;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static void AddToDoTemplateSharedServices(this IServiceCollection services)
    {
        services.AddSingleton<IDateTimeProvider, ToDoTemplateDateTimeProvider>();
    }
}
