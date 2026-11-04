namespace Boilerplate.LandingPage.Client.Components.Common;

public partial class Countdown : IDisposable
{
    [Parameter, EditorRequired] public DateTimeOffset Target { get; set; }

    [Parameter, EditorRequired] public string[] Units { get; set; } = default!;

    [Parameter] public string? Label { get; set; }

    [Parameter] public string? Class { get; set; }

    private PeriodicTimer? timer;

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender && RendererInfo.IsInteractive)
        {
            _ = Tick();
        }

        base.OnAfterRender(firstRender);
    }

    private async Task Tick()
    {
        timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        while (await timer.WaitForNextTickAsync())
        {
            await InvokeAsync(StateHasChanged);
        }
    }

    private IEnumerable<(int Value, string Unit)> Parts()
    {
        var left = Target - DateTimeOffset.UtcNow;

        if (left < TimeSpan.Zero)
            left = TimeSpan.Zero;

        yield return ((int)left.TotalDays, Units[0]);
        yield return (left.Hours, Units[1]);
        yield return (left.Minutes, Units[2]);
        yield return (left.Seconds, Units[3]);
    }

    public void Dispose() => timer?.Dispose();
}
