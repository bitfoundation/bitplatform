namespace Boilerplate.Client.Core.Components.Layout;

public partial class AppJsBridge
{
    private DotNetObjectReference<AppJsBridge>? dotnetObj;
    private ISmsOtpReceiver? smsOtpReceiver;
    private CancellationTokenSource? smsOtpCts;

    /// <summary>
    /// at the rendering time of this component (the component is added to the `RootLayout`)
    /// it registers an instance of the `DotNetObjectReference` into the js code (look at the `App.ts` file), 
    /// so we can later use it to call .net methods.
    /// </summary>
    protected override async Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        dotnetObj = DotNetObjectReference.Create(this);

        // Optional: only the Android app registers one, and Scripts/SmsOtp.ts provides WebOTP only when it is there and supported.
        smsOtpReceiver = ScopedServices.GetService<ISmsOtpReceiver>();

        await JSRuntime.InvokeVoidAsync("App.registerJsBridge", dotnetObj, smsOtpReceiver?.IsSupported is true);
    }


    /// <summary>
    /// you can add any other method like this to utilize the bridge between js and .net code.
    /// </summary>
    [JSInvokable(nameof(ShowDiagnostic))]
    public async Task ShowDiagnostic()
    {
        PubSubService.Publish(ClientAppMessages.SHOW_DIAGNOSTIC_MODAL);
    }

    [JSInvokable(nameof(PublishMessage))]
    public async Task PublishMessage(string message, string? payload)
    {
        PubSubService.Publish(message, payload);
    }

    [JSInvokable(nameof(ReceiveSmsOtp))]
    public async Task<string?> ReceiveSmsOtp()
    {
        // One wait at a time: a newer BitOtpInput (e.g. the second one on ConfirmPage) replaces the pending request.
        CancelSmsOtp();

        using var cts = smsOtpCts = new CancellationTokenSource();

        try
        {
            return await smsOtpReceiver!.ReceiveOtp(cts.Token);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception exp)
        {
            ExceptionHandler.Handle(exp, ExceptionDisplayKind.None);
            return null;
        }
        finally
        {
            if (smsOtpCts == cts)
            {
                smsOtpCts = null;
            }
        }
    }

    [JSInvokable(nameof(CancelSmsOtp))]
    public void CancelSmsOtp()
    {
        smsOtpCts?.Cancel();
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        dotnetObj?.Dispose();
        CancelSmsOtp();

        await base.DisposeAsync(disposing);
    }
}
