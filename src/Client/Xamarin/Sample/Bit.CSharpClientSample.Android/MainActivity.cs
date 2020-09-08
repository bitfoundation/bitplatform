using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Gms.Common;
using Android.OS;
using AndroidX.Core.App;
using Bit.Android;
using Bit.ViewModel.Implementations;
using Firebase.Messaging;
using System.Collections.Generic;
using System.Linq;
using WindowsAzure.Messaging;
using Xamarin.Forms;

namespace Bit.CSharpClientSample.Droid
{
    [Activity(Label = "Bit.CSharpClientSample", Icon = "@drawable/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : BitFormsAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            LocalTelemetryService.Current.Init();
            FirebaseTelemetryService.Current.Init(this);

            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            SQLitePCL.Batteries.Init();

            base.OnCreate(savedInstanceState);

            /*if (CanCreateNotificationHub())
                CreateNotificationChannel();*/

            UseDefaultConfiguration(savedInstanceState);
            Forms.SetFlags("StateTriggers_Experimental");
            Forms.Init(this, savedInstanceState);

            LoadApplication(new App(new SampleAppDroidInitializer(this)));
        }

        public bool CanCreateNotificationHub()
        {
            bool isPlayServicesAvailable;
            string playServicesAvailabilityErrorMessage = null;

            int resultCode = GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(this);

            if (resultCode != ConnectionResult.Success)
            {
                playServicesAvailabilityErrorMessage = GoogleApiAvailability.Instance.GetErrorString(resultCode);

                if (GoogleApiAvailability.Instance.IsUserResolvableError(resultCode))
                {
                    isPlayServicesAvailable = true;
                }
                else
                {
                    isPlayServicesAvailable = false;
                }
            }
            else
            {
                isPlayServicesAvailable = true;
            }

            bool versionIsOk = !(Build.VERSION.SdkInt < BuildVersionCodes.O); // Notification channels are new in API 26 (and not a part of the support library). There is no need to create a notification channel on older versions of Android.

            bool canCreateNotificationHub = isPlayServicesAvailable && versionIsOk;

            return canCreateNotificationHub;
        }

        public static readonly string CHANNEL_ID = "notification_channel";

        void CreateNotificationChannel()
        {
            string channelName = CHANNEL_ID;
            string channelDescription = string.Empty;

            NotificationChannel channel = new NotificationChannel(CHANNEL_ID, channelName, NotificationImportance.Default)
            {
                Description = channelDescription
            };

            ((NotificationManager)GetSystemService(NotificationService)).CreateNotificationChannel(channel);
        }
    }

    /// <summary>
    /// https://docs.microsoft.com/en-us/azure/notification-hubs/xamarin-notification-hubs-push-notifications-android-gcm
    /// </summary>
    public static class Constants
    {
        public const string ListenConnectionString = "Endpoint=sb://bitframework.servicebus.windows.net/;SharedAccessKeyName=DefaultListenSharedAccessSignature;SharedAccessKey=WVl36HhBsBPriKJU8gHCGCAjrxgwO5OIex04N/CWCPU=";
        public const string NotificationHubName = "bit";
    }

    [Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop)]
    [IntentFilter(new[] { Intent.ActionView }, Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable }, DataScheme = "test-oauth")]
    public class WebAuthenticationCallbackActivity : Xamarin.Essentials.WebAuthenticatorCallbackActivity
    {
    }

    public class SampleAppDroidInitializer : BitPlatformInitializer
    {
        public SampleAppDroidInitializer(Activity activity)
            : base(activity)
        {
        }
    }

    [Service]
    [IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
    [IntentFilter(new[] { "com.google.firebase.INSTANCE_ID_EVENT" })]
    public class DDSFirebaseMessagingService : FirebaseMessagingService
    {
        NotificationHub hub;

        public override void OnMessageReceived(RemoteMessage message)
        {
            if (message.GetNotification() != null)
            {
                SendNotification(message.GetNotification().Body);
            }
            else
            {
                SendNotification(message.Data.Values.First());
            }
        }

        void SendNotification(string messageBody)
        {
            var intent = new Intent(this, typeof(MainActivity));
            intent.AddFlags(ActivityFlags.ClearTop);
            var pendingIntent = PendingIntent.GetActivity(this, 0, intent, PendingIntentFlags.OneShot);

            var notificationBuilder = new NotificationCompat.Builder(this, MainActivity.CHANNEL_ID);

            notificationBuilder.SetContentTitle("Bit")
                        .SetSmallIcon(Resource.Drawable.icon)
                        .SetContentText(messageBody)
                        .SetAutoCancel(true)
                        .SetShowWhen(false)
                        .SetContentIntent(pendingIntent);

            var notificationManager = NotificationManager.FromContext(this);

            notificationManager.Notify(0, notificationBuilder.Build());
        }

        public override void OnNewToken(string token)
        {
            SendRegistrationToServer(token);
        }

        void SendRegistrationToServer(string token)
        {
            hub = new NotificationHub(Constants.NotificationHubName,
                                        Constants.ListenConnectionString, this);

            var tags = new List<string>() { };
            var regID = hub.Register(token, tags.ToArray()).RegistrationId;
        }
    }
}
