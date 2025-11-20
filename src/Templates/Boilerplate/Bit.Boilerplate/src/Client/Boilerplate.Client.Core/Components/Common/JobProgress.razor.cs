using Boilerplate.Shared.Dtos.SignalR;

namespace Boilerplate.Client.Core.Components.Common;

public partial class JobProgress
{
    private bool isVisible;

    // This component shows progress for only one job at a time.
    private string? currentJobTitle;
    private double currentJobProgressPercentage;
    private BackgroundJobProgressDto? currentlyBeingShownJobProgress;
    private string? toBeIgnoredJobId;

    private Action? disposable;

    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        if (InPrerenderSession is false)
        {
            disposable = PubSubService.Subscribe(ClientAppMessages.BACKGROUND_JOB_PROGRESS, async payload =>
            {
                if (payload is null) return;

                var jobProgress = payload is JsonElement jsonDocument
                    ? jsonDocument.Deserialize(JsonSerializerOptions.GetTypeInfo<BackgroundJobProgressDto>())! /* Message gets published from server through SignalR */
                    : (BackgroundJobProgressDto)payload;

                await InvokeAsync(async () =>
                {
                    currentlyBeingShownJobProgress = jobProgress;

                    currentJobTitle = Localizer[jobProgress.JobTitle];

                    currentJobProgressPercentage = jobProgress.TotalItems == 0
                        ? 0
                        : (double)(jobProgress.SucceededItems + jobProgress.FailedItems) / jobProgress.TotalItems * 100;

                    isVisible = jobProgress.JobId != toBeIgnoredJobId;

                    StateHasChanged();

                    await RestartHideTimer();
                });
            });
        }
    }

    private async Task RestartHideTimer()
    {
        await Abort();
        _ = HideAfterDelay();
    }

    private async Task HideAfterDelay()
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(5), CurrentCancellationToken);

            await InvokeAsync(() =>
            {
                isVisible = false;
                StateHasChanged();
            });
        }
        catch (TaskCanceledException)
        {

        }
    }

    private async Task Ignore()
    {
        toBeIgnoredJobId = currentlyBeingShownJobProgress?.JobId;
        isVisible = false;
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        disposable?.Invoke();

        await base.DisposeAsync(disposing);
    }
}
