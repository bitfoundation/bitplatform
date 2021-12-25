namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddTodoTemplateServices(this IServiceCollection services)
    {
        services.AddScoped<IToastService, ToastService>();


#if Android
        services.AddSingleton<IContactsService, TodoTemplate.App.Platforms.Android.Implementations.ContactsService>();
#elif iOS
        services.AddSingleton<IContactsService, TodoTemplate.App.Platforms.iOS.Implementations.ContactsService>();
#elif Windows
        services.AddSingleton<IContactsService, TodoTemplate.App.Platforms.Windows.Implementations.ContactsService>();
#elif Mac
        services.AddSingleton<IContactsService, TodoTemplate.App.Platforms.MacCatalyst.Implementations.ContactsService>();
#else
        services.AddSingleton<IContactsService, TodoTemplate.App.Platforms.Web.Implementations.ContactsService>();
#endif

        return services;
    }
}
