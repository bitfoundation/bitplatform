using Android.App;
using Android.Content;
using Android.Gms.Common;
using Android.Gms.Extensions;
using Android.Gms.Common.Apis;
using Android.Gms.Auth.Api.Phone;
using AndroidX.Activity;
using AndroidX.Activity.Result;
using AndroidX.Activity.Result.Contract;
using AndroidX.Core.Content;
using System.Text.RegularExpressions;

namespace Boilerplate.Client.Maui.Platforms.Android.Services;

public partial class AndroidSmsOtpReceiver : ISmsOtpReceiver
{
    public bool IsSupported => GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(Platform.AppContext) is ConnectionResult.Success;

    public async Task<string?> ReceiveOtp(CancellationToken cancellationToken)
    {
        var context = Platform.AppContext;
        var message = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var receiver = new SmsConsentReceiver(message);

        ContextCompat.RegisterReceiver(context, receiver, new IntentFilter(SmsRetriever.SmsRetrievedAction), SmsRetriever.SendPermission, null, ContextCompat.ReceiverExported);

        try
        {
            await SmsRetriever.GetClient(context).StartSmsUserConsent(null);

            using var registration = cancellationToken.Register(() => message.TrySetCanceled(cancellationToken));

            return await message.Task is string sms && WebOtpLine().Match(sms) is { Success: true } match
                ? match.Groups[1].Value
                : null;
        }
        finally
        {
            context.UnregisterReceiver(receiver);
        }
    }

    [GeneratedRegex(@"^@\S+ #(\S+)", RegexOptions.Multiline)]
    private static partial Regex WebOtpLine();

    private sealed class SmsConsentReceiver(TaskCompletionSource<string?> message) : BroadcastReceiver
    {
        public override void OnReceive(Context? context, Intent? intent)
        {
            if (intent is null || message.Task.IsCompleted) return;

            var status = IntentCompat.GetParcelableExtra(intent, SmsRetriever.ExtraStatus, Java.Lang.Class.FromType(typeof(Statuses))) as Statuses;
            var consentIntent = IntentCompat.GetParcelableExtra(intent, SmsRetriever.ExtraConsentIntent, Java.Lang.Class.FromType(typeof(Intent))) as Intent;

            if (status?.StatusCode is not CommonStatusCodes.Success || consentIntent is null || Platform.CurrentActivity is not ComponentActivity activity)
            {
                message.TrySetResult(null);
                return;
            }

            ActivityResultLauncher? launcher = null;
            launcher = activity.ActivityResultRegistry.Register(nameof(AndroidSmsOtpReceiver), new ActivityResultContracts.StartActivityForResult(), new ConsentResultCallback(result =>
            {
                launcher?.Unregister();
                message.TrySetResult(result?.ResultCode is (int)Result.Ok ? result.Data?.GetStringExtra(SmsRetriever.ExtraSmsMessage) : null);
            }));
            launcher.Launch(consentIntent);
        }
    }

    private sealed class ConsentResultCallback(Action<ActivityResult?> onResult) : Java.Lang.Object, IActivityResultCallback
    {
        public void OnActivityResult(Java.Lang.Object? result) => onResult(result as ActivityResult);
    }
}
