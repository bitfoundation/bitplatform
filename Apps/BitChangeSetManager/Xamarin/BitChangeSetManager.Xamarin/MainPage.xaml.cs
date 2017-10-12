using BitChangeSetManager.Xamarin.BitChangeSetManager;
using BitChangeSetManager.Xamarin.BitChangeSetManager.Dto;
using IdentityModel.Client;
using Microsoft.OData.Client;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BitChangeSetManager.Xamarin
{
    public partial class MainPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        public const string BaseAddress = "http://10.0.2.2:9092";

        private TokenResponse _tokenResponse = null;

        private async void Login_Clicked(object sender, EventArgs e)
        {
            using (TokenClient identityClient = new TokenClient($"{BaseAddress}/core/connect/token", "BitChangeSetManager-ResOwner", "secret"))
            {
                _tokenResponse = await identityClient.RequestResourceOwnerPasswordAsync(UserName.Text, Password.Text, "openid profile user_info");

                if (_tokenResponse.IsError)
                {
                    LoadData.IsEnabled = false;
                }
                else
                {
                    LoadData.IsEnabled = true;
                }
            }
        }

        private async void LoadData_Clicked(object sender, EventArgs e)
        {
            BitChangeSetManagerContext context = new BitChangeSetManagerContext(new Uri($"{BaseAddress}/odata/BitChangeSetManager/"));

            context.SetBearerToken(_tokenResponse);

            DataServiceCollection<ChangeSetDto> changeSets = new DataServiceCollection<ChangeSetDto>(context);

            ChangeSetsListView.ItemsSource = changeSets;

            await changeSets.LoadQueryAsync(context.ChangeSets);
        }
    }

    public static class Extensions
    {
        public static void SetBearerToken(this DataServiceContext context, TokenResponse token)
        {
            context.BuildingRequest += (sender, e) =>
            {
                if (!e.Headers.ContainsKey("Authorization"))
                    e.Headers.Add("Authorization", $"{token.TokenType} {token.AccessToken}");
            };
        }

        public static Task LoadQueryAsync<T>(this DataServiceCollection<T> collection, IQueryable<T> query)
        {
            TaskCompletionSource<object> taskCompletationSource = new TaskCompletionSource<object>();

            EventHandler<LoadCompletedEventArgs> onCompleted = null;

            onCompleted = (e, sender) =>
            {
                collection.LoadCompleted -= onCompleted;

                if (sender.Cancelled)
                    taskCompletationSource.SetCanceled();
                else if (sender.Error != null)
                    taskCompletationSource.SetException(sender.Error);
                else
                    taskCompletationSource.SetResult(null);
            };

            collection.LoadCompleted += onCompleted;

            collection.LoadAsync(query);

            return taskCompletationSource.Task;
        }
    }
}
