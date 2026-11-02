using Microsoft.Extensions.Caching.Memory;

namespace Boilerplate.LandingPage.Server.Features.StoreListing;

public sealed partial class AppStoreListingService(HttpClient httpClient, IMemoryCache memoryCache, ILogger<AppStoreListingService> logger) : IStoreListingClient
{
    public async Task<Client.Infrastructure.Models.StoreListing?> GetAppStoreListingAsync(string appleAppId, CancellationToken cancellationToken)
    {
        if (appleAppId.All(char.IsAsciiDigit) is false)
            return null;

        return await memoryCache.GetOrCreateAsync($"app-store:{appleAppId}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6);

            try
            {
                using var document = await JsonDocument.ParseAsync(await httpClient.GetStreamAsync($"lookup?id={appleAppId}", cancellationToken), cancellationToken: cancellationToken);

                if (document.RootElement.GetProperty("results") is not { ValueKind: JsonValueKind.Array } results || results.GetArrayLength() is 0)
                    return null;

                var app = results[0];

                return new Client.Infrastructure.Models.StoreListing
                {
                    Name = app.TryGetProperty("trackName", out var name) ? name.GetString() : null,
                    AverageRating = app.TryGetProperty("averageUserRating", out var rating) ? Math.Round(rating.GetDouble(), 1) : null,
                    RatingCount = app.TryGetProperty("userRatingCount", out var count) ? count.GetInt32() : null,
                    Version = app.TryGetProperty("version", out var version) ? version.GetString() : null,
                    ReleasedOn = app.TryGetProperty("currentVersionReleaseDate", out var released) && released.TryGetDateTimeOffset(out var date) ? date : null,
                    SizeInBytes = app.TryGetProperty("fileSizeBytes", out var size) && long.TryParse(size.GetString(), out var bytes) ? bytes : null,
                    MinimumOsVersion = app.TryGetProperty("minimumOsVersion", out var os) ? os.GetString() : null,
                    Url = app.TryGetProperty("trackViewUrl", out var url) ? url.GetString() : null
                };
            }
            catch (Exception exp) when (exp is HttpRequestException or JsonException or KeyNotFoundException or InvalidOperationException or TaskCanceledException)
            {
                LogLookupFailed(logger, exp, appleAppId);
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                return null;
            }
        });
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Couldn't read the App Store listing of {AppleAppId}")]
    private static partial void LogLookupFailed(ILogger logger, Exception exception, string appleAppId);
}

public static class StoreListingEndpoints
{
    public static IServiceCollection AddLandingStoreListings(this IServiceCollection services)
    {
        services.AddMemoryCache();

        services.AddHttpClient<AppStoreListingService>(client =>
        {
            client.BaseAddress = new Uri("https://itunes.apple.com/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddScoped<IStoreListingClient>(sp => sp.GetRequiredService<AppStoreListingService>());

        return services;
    }

    public static void MapLandingStoreListings(this WebApplication app)
    {
        app.MapGet("/api/store-listing/{appleAppId}", async (string appleAppId, AppStoreListingService service, HttpContext context, CancellationToken cancellationToken) =>
        {
            var listing = await service.GetAppStoreListingAsync(appleAppId, cancellationToken);

            context.Response.GetTypedHeaders().CacheControl = new() { Public = true, MaxAge = TimeSpan.FromHours(1) };

            return listing is null ? Results.NotFound() : Results.Json(listing, LandingJsonContext.Default.StoreListing);
        }).ExcludeFromDescription();
    }
}
