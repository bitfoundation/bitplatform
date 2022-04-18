using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Bit.Android;
using Bit.ViewModel.Implementations;
using Firebase.Messaging;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Plugin.LocalNotification;
using System.Linq;
using Xamarin.Forms;

[assembly: Xamarin.Forms.Internals.Preserve]

namespace Bit.CSharpClientSample.Droid
{
    [Activity(Label = "Bit.CSharpClientSample", Icon = "@drawable/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation, Exported = true)]
    public class MainActivity : BitFormsAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            LocalTelemetryService.Current.Init();
            FirebaseTelemetryService.Current.Init(this);
            AppCenterTelemetryService.Current.Init("79771c81-f748-4649-8787-74508df44145",
                   typeof(Analytics), typeof(Crashes));
            ApplicationInsightsTelemetryService.Current.Init("55f4c3a7-8bd1-4ec0-92b3-717cb9ddde1d");

            SQLitePCL.Batteries.Init();

            base.OnCreate(savedInstanceState);

            /*if (CanCreateNotificationHub())
                CreateNotificationChannel();*/

            UseDefaultConfiguration(savedInstanceState);
            Forms.SetFlags("StateTriggers_Experimental");
            Forms.Init(this, savedInstanceState);

            NotificationCenter.CreateNotificationChannel();

            LoadApplication(new App(new SampleAppDroidInitializer(this)));

            NotificationCenter.NotifyNotificationTapped(Intent);
        }

        protected override void OnNewIntent(Intent intent)
        {
            NotificationCenter.NotifyNotificationTapped(intent);
            base.OnNewIntent(intent);
        }
    }

    [Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
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

    [Service(Exported = true)]
    [IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
    [IntentFilter(new[] { "com.google.firebase.INSTANCE_ID_EVENT" })]
    public class SampleAppFirebaseMessagingService : FirebaseMessagingService
    {
        public override void OnMessageReceived(RemoteMessage message)
        {
            string messageStr = message.GetNotification()?.Body ?? message.Data.Values.First();

            NotificationCenter.Current.Show(new NotificationRequest
            {
                NotificationId = 100,
                Title = "Bit",
                Description = messageStr
            });
        }
    }
}
