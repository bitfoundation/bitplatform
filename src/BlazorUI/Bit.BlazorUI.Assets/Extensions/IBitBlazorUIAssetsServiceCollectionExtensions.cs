using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bit.BlazorUI;

public static class IBitBlazorUIAssetsServiceCollectionExtensions
{
    public static IServiceCollection AddBitBlazorUIAssetsServices(this IServiceCollection services, IFileProvider fileProvider)
    {
        services.TryAddScoped(sp => new BitFileVersionProvider(fileProvider));

        return services;
    }
}
