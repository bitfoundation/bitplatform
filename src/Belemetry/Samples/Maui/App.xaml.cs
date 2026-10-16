namespace Bit.Belemetry.Samples.Maui;

public partial class App : Application
{
    private readonly IServiceProvider _services;

    public App(IServiceProvider services)
    {
        InitializeComponent();

        _services = services;
    }

    protected override Window CreateWindow(IActivationState? activationState) => new(new MainPage());

    /// <summary>
    /// A mobile OS may suspend a backgrounded app and kill it later without another callback, so whatever is still
    /// queued is exported as soon as the app goes to the background.
    /// </summary>
    protected override async void OnSleep()
    {
        base.OnSleep();

        try
        {
            // IBelemetry is scoped (one per BlazorWebView); the queue it flushes is the app-wide pipeline.
            await using var scope = _services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<IBelemetry>().FlushAsync();
        }
        catch (Exception ex)
        {
            // async void: an exception here would crash the app on its way to the background.
            System.Diagnostics.Debug.WriteLine($"Flushing telemetry on sleep failed: {ex}");
        }
    }
}
