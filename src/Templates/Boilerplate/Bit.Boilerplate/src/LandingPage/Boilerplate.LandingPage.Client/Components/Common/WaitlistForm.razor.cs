using System.Diagnostics;
using Bit.Butil;

namespace Boilerplate.LandingPage.Client.Components.Common;

public partial class WaitlistForm
{
    [AutoInject] private IWaitlistClient waitlistClient = default!;
    [AutoInject] private Document document = default!;

    [Parameter, EditorRequired] public LandingSite Site { get; set; } = default!;

    [Parameter] public IReadOnlyList<LandingPlatform>? Platforms { get; set; }

    [Parameter] public bool ShowName { get; set; }

    [Parameter] public bool ShowUpdates { get; set; } = true;

    [Parameter] public string? SubmitText { get; set; }

    [Parameter] public string? Id { get; set; }

    [Parameter] public string? Class { get; set; }

    private string? email;
    private string? name;
    private string? website;
    private bool wantsUpdates;
    private bool isSubmitting;
    private string? error;
    private WaitlistJoinResult? result;
    private List<LandingPlatform> offeredPlatforms = [];
    private readonly HashSet<LandingPlatform> selectedPlatforms = [];
    private readonly Stopwatch stopwatch = new();

    protected override void OnParametersSet()
    {
        offeredPlatforms = [.. Platforms ?? Site.Platforms.Where(p => p.Status is LandingPlatformStatus.ComingSoon).Select(p => p.Platform)];

        if (selectedPlatforms.Count is 0)
        {
            selectedPlatforms.UnionWith(offeredPlatforms);
        }

        base.OnParametersSet();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            stopwatch.Start();
        }

        base.OnAfterRender(firstRender);
    }

    private void Toggle(LandingPlatform platform, bool selected)
    {
        if (selected)
        {
            selectedPlatforms.Add(platform);
        }
        else
        {
            selectedPlatforms.Remove(platform);
        }
    }

    private async Task Submit()
    {
        if (isSubmitting)
            return;

        error = null;

        if (string.IsNullOrWhiteSpace(email) || email.Contains('@') is false)
        {
            error = Localizer[nameof(LandingStrings.WaitlistInvalidEmail)];
            return;
        }

        isSubmitting = true;

        try
        {
            var query = NavigationManager.ToAbsoluteUri(NavigationManager.Uri).Query.TrimStart('?')
                .Split('&', StringSplitOptions.RemoveEmptyEntries)
                .Select(pair => pair.Split('=', 2))
                .ToDictionary(pair => pair[0], pair => pair.Length > 1 ? Uri.UnescapeDataString(pair[1]) : string.Empty, StringComparer.OrdinalIgnoreCase);

            result = await waitlistClient.JoinAsync(new()
            {
                Site = Site.Key,
                Email = email.Trim(),
                Name = name,
                Platforms = [.. selectedPlatforms.Select(p => p.ToString())],
                WantsUpdates = wantsUpdates,
                Culture = CurrentCulture.Name,
                Source = query.GetValueOrDefault("utm_source"),
                Medium = query.GetValueOrDefault("utm_medium"),
                Campaign = query.GetValueOrDefault("utm_campaign"),
                Referrer = await document.GetReferrer(),
                Website = website,
                ElapsedMilliseconds = stopwatch.ElapsedMilliseconds
            }, CancellationToken.None);
        }
        catch (WaitlistException exp)
        {
            error = exp.Message;
        }
        finally
        {
            isSubmitting = false;
        }
    }
}
