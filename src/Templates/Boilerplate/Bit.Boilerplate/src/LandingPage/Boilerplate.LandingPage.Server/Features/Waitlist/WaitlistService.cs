using System.Net.Mail;
using System.Security.Cryptography;
using Boilerplate.LandingPage.Server.Features.Email;
using Boilerplate.LandingPage.Server.Features.Email.Templates;

namespace Boilerplate.LandingPage.Server.Features.Waitlist;

public sealed partial class WaitlistService(
    IWaitlistStore store,
    LandingEmailService emailService,
    IStringLocalizer<LandingStrings> localizer,
    TimeProvider timeProvider,
    ILogger<WaitlistService> logger)
{
    private static readonly TimeSpan minimumFillTime = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan confirmationResendInterval = TimeSpan.FromMinutes(10);

    public async Task<WaitlistJoinResult> JoinAsync(WaitlistJoinRequest request, Uri siteUrl, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.Website) is false || TimeSpan.FromMilliseconds(request.ElapsedMilliseconds) < minimumFillTime)
        {
            LogBotRejected(logger, request.Site);
            return new(Position: Random.Shared.Next(200, 900), AlreadyJoined: false, ConfirmationSent: true);
        }

        var culture = LandingCultures.Find(request.Culture) ?? LandingCultures.Default;

        var site = LandingSites.Find(request.Site);

        if (site is null || MailAddress.TryCreate(request.Email.Trim(), out var address) is false || address.Address.Length > 254 || address.Host.Contains('.') is false)
            throw new WaitlistException(localizer.In(culture, nameof(LandingStrings.WaitlistInvalidEmail)));

        var (entry, alreadyJoined) = await store.AddAsync(new()
        {
            Site = site.Key,
            Email = address.Address,
            Name = Trim(request.Name, 100),
            Platforms = [.. request.Platforms.Where(p => Enum.TryParse<LandingPlatform>(p, out _)).Distinct().Take(5)],
            WantsUpdates = request.WantsUpdates,
            Culture = culture.Name,
            Source = Trim(request.Source, 100),
            Medium = Trim(request.Medium, 100),
            Campaign = Trim(request.Campaign, 100),
            Referrer = Trim(request.Referrer, 500),
            Token = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(24)),
            JoinedOn = timeProvider.GetUtcNow()
        }, cancellationToken);

        var now = timeProvider.GetUtcNow();

        if (entry.ConfirmedOn is null && (entry.ConfirmationSentOn is not { } sentOn || now - sentOn >= confirmationResendInterval))
        {
            await SendConfirmationAsync(entry, site, culture, siteUrl, cancellationToken);
            await store.UpdateAsync(entry with { ConfirmationSentOn = now }, cancellationToken);
        }

        return new(entry.Position, alreadyJoined, ConfirmationSent: entry.ConfirmedOn is null);
    }

    public async Task<WaitlistConfirmResult> ConfirmAsync(string token, CancellationToken cancellationToken)
    {
        var entry = await store.FindByTokenAsync(token, cancellationToken) ?? throw new KeyNotFoundException();

        if (entry.ConfirmedOn is null)
        {
            entry = entry with { ConfirmedOn = timeProvider.GetUtcNow(), UnsubscribedOn = null };
            await store.UpdateAsync(entry, cancellationToken);
        }

        return new(Confirmed: true, entry.Position);
    }

    public async Task UnsubscribeAsync(string token, CancellationToken cancellationToken)
    {
        var entry = await store.FindByTokenAsync(token, cancellationToken) ?? throw new KeyNotFoundException();

        if (entry.UnsubscribedOn is null)
        {
            await store.UpdateAsync(entry with { UnsubscribedOn = timeProvider.GetUtcNow(), WantsUpdates = false }, cancellationToken);
        }
    }

    public async Task SendLaunchAnnouncementAsync(WaitlistEntry entry, Uri siteUrl, CancellationToken cancellationToken)
    {
        var site = LandingSites.Find(entry.Site)!;
        var culture = LandingCultures.Find(entry.Culture) ?? LandingCultures.Default;

        (string Label, string Url)[] links = [.. site.Platforms
            .Where(p => p.Status is LandingPlatformStatus.Live && p.Url is not null)
            .Select(p => (p.Platform.ToString(), p.Url!))];

        await emailService.SendAsync<LaunchAnnouncementEmail>(entry.Email, localizer.In(culture, nameof(LandingStrings.LaunchEmailSubject), site.Name), culture, new()
        {
            [nameof(LaunchAnnouncementEmail.SiteName)] = site.Name,
            [nameof(LaunchAnnouncementEmail.AccentColor)] = site.AccentColor,
            [nameof(LaunchAnnouncementEmail.Links)] = links,
            [nameof(LaunchAnnouncementEmail.UnsubscribeUrl)] = Link(siteUrl, site, culture, "unsubscribe", entry.Token)
        }, cancellationToken);
    }

    private async Task SendConfirmationAsync(WaitlistEntry entry, LandingSite site, CultureInfo culture, Uri siteUrl, CancellationToken cancellationToken)
    {
        await emailService.SendAsync<WaitlistConfirmationEmail>(entry.Email, localizer.In(culture, nameof(LandingStrings.WaitlistEmailSubject), site.Name), culture, new()
        {
            [nameof(WaitlistConfirmationEmail.SiteName)] = site.Name,
            [nameof(WaitlistConfirmationEmail.AccentColor)] = site.AccentColor,
            [nameof(WaitlistConfirmationEmail.Position)] = entry.Position,
            [nameof(WaitlistConfirmationEmail.ConfirmUrl)] = Link(siteUrl, site, culture, "confirm", entry.Token),
            [nameof(WaitlistConfirmationEmail.UnsubscribeUrl)] = Link(siteUrl, site, culture, "unsubscribe", entry.Token)
        }, cancellationToken);
    }

    private static Uri Link(Uri siteUrl, LandingSite site, CultureInfo culture, string action, string token)
    {
        return new Uri(siteUrl, $"{LandingCultures.Localize($"{site.Path}/waitlist/{action}", culture).TrimStart('/')}?token={token}");
    }

    private static string? Trim(string? value, int maxLength)
    {
        value = value?.Trim();

        return string.IsNullOrEmpty(value) ? null : value.Length > maxLength ? value[..maxLength] : value;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "A sign-up for {Site} looked automated and was dropped")]
    private static partial void LogBotRejected(ILogger logger, string site);
}

public static class WaitlistEndpoints
{
    public static IServiceCollection AddLandingWaitlist(this IServiceCollection services)
    {
        services.AddSingleton<IWaitlistStore, InMemoryWaitlistStore>();
        services.AddScoped<WaitlistService>();
        services.AddScoped<IWaitlistClient, DirectWaitlistClient>();

        return services;
    }

    public static void MapLandingWaitlist(this WebApplication app)
    {
        var group = app.MapGroup("/api/waitlist").RequireRateLimiting(LandingRateLimits.Waitlist).ExcludeFromDescription();

        group.MapPost("/", async (WaitlistJoinRequest request, WaitlistService service, HttpContext context, CancellationToken cancellationToken) =>
        {
            try
            {
                return Results.Json(await service.JoinAsync(request, context.GetSiteUrl(), cancellationToken), LandingJsonContext.Default.WaitlistJoinResult);
            }
            catch (WaitlistException)
            {
                return Results.BadRequest();
            }
        });

        group.MapPost("/confirm", async (WaitlistTokenRequest request, WaitlistService service, CancellationToken cancellationToken) =>
        {
            try
            {
                return Results.Json(await service.ConfirmAsync(request.Token, cancellationToken), LandingJsonContext.Default.WaitlistConfirmResult);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        });

        group.MapPost("/unsubscribe", async (WaitlistTokenRequest request, WaitlistService service, CancellationToken cancellationToken) =>
        {
            try
            {
                await service.UnsubscribeAsync(request.Token, cancellationToken);
                return Results.NoContent();
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        });
    }
}

public sealed partial class DirectWaitlistClient(WaitlistService service, NavigationManager navigationManager, LandingServerSettings settings) : IWaitlistClient
{
    public Task<WaitlistJoinResult> JoinAsync(WaitlistJoinRequest request, CancellationToken cancellationToken)
        => service.JoinAsync(request, settings.Site.Url ?? new Uri(navigationManager.BaseUri), cancellationToken);

    public Task<WaitlistConfirmResult> ConfirmAsync(string token, CancellationToken cancellationToken)
        => Wrap(() => service.ConfirmAsync(token, cancellationToken));

    public Task UnsubscribeAsync(string token, CancellationToken cancellationToken)
        => Wrap(async () => { await service.UnsubscribeAsync(token, cancellationToken); return true; });

    private static async Task<T> Wrap<T>(Func<Task<T>> action)
    {
        try
        {
            return await action();
        }
        catch (KeyNotFoundException)
        {
            throw new WaitlistException(string.Empty);
        }
    }
}
