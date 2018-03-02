using Bit.ViewModel.Contracts;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Auth;
using Xamarin.Auth.Presenters;

namespace Bit.ViewModel.Implementations
{
    public class BitOAuth2Authenticator : OAuth2Authenticator
    {
        private readonly IClientAppProfile _clientAppProfile;
        private readonly AccountStore _accountStore;
        private readonly OAuthLoginPresenter _oAuthLoginPresenter;

        public BitOAuth2Authenticator(IClientAppProfile clientAppProfile,
            AccountStore accountStore,
            OAuthLoginPresenter oAuthLoginPresenter)
            : base(clientId: "Temp",
                  scope: "openid profile user_info",
                  authorizeUrl: new Uri("https://temp-uri.org"),
                  redirectUrl: new Uri("https://temp-uri.org"),
                  getUsernameAsync: null,
                  isUsingNativeUI: true)
        {
            _clientAppProfile = clientAppProfile;
            _accountStore = accountStore;
            _oAuthLoginPresenter = oAuthLoginPresenter;
        }

        public override async Task<Uri> GetInitialUrlAsync()
        {
            return Url;
        }

        protected Uri Url { get; set; }
        protected TaskCompletionSource<Token> CurrentLoginTaskCompletionSource { get; set; }
        protected string CurrentAction { get; set; }
        protected TaskCompletionSource<object> CurrentLogoutTaskCompletionSource { get; set; }

        public virtual Task Logout(Uri url)
        {
            CurrentAction = "Logout";
            Url = url;
            CurrentLogoutTaskCompletionSource = new TaskCompletionSource<object>();
            _oAuthLoginPresenter.Login(this);
            return CurrentLogoutTaskCompletionSource.Task;
        }

        public virtual Task<Token> Login(Uri url)
        {
            CurrentAction = "Login";
            Url = url;
            CurrentLoginTaskCompletionSource = new TaskCompletionSource<Token>();
            _oAuthLoginPresenter.Login(this);
            return CurrentLoginTaskCompletionSource.Task;
        }

        protected override async void OnPageEncountered(Uri url, IDictionary<string, string> query, IDictionary<string, string> fragment)
        {
            if (CurrentAction == "Login")
            {
                Token token = (Dictionary<string, string>)fragment;

                Account account = Token.FromTokenToAccount(token);

                await _accountStore.SaveAsync(account, _clientAppProfile.AppName).ConfigureAwait(false);

                CurrentLoginTaskCompletionSource.SetResult(account);
            }
            else
            {
                CurrentLogoutTaskCompletionSource.SetResult(null);
            }

            base.OnPageEncountered(url, query, fragment);
        }
    }
}
