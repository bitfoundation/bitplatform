using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Bit.Core.Contracts;
using Bit.Http.Contracts;
using Microsoft.AspNetCore.Components.Authorization;
using Prism.Events;

namespace Bit.MauiAppSample.Implementations
{
    public class UserPolicyChangedEvent : PubSubEvent<UserPolicyChangedEvent>
    {
    }

    public class SampleAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly ISecurityService _securityService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IExceptionHandler _exceptionHandler;

        public SampleAuthenticationStateProvider(ISecurityService securityService,
                                                     IEventAggregator eventAggregator,
                                                     IExceptionHandler exceptionHandler)
        {
            _securityService = securityService;
            _eventAggregator = eventAggregator;
            _exceptionHandler = exceptionHandler;
        }

        public void StateHasChanged()
        {
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

            _eventAggregator.GetEvent<UserPolicyChangedEvent>().Publish(new UserPolicyChangedEvent { });
        }

        private static AuthenticationState NoUser() => new AuthenticationState(user: new ClaimsPrincipal());

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                bool isLoggedIn = await _securityService.IsLoggedInAsync();

                if (isLoggedIn is false) return NoUser();

                var claims = new List<Claim>
                {
                    new Claim("UserId", await _securityService.GetUserIdAsync(default))
                };

                var identity = new ClaimsIdentity(claims, authenticationType: "Bearer");

                var user = new ClaimsPrincipal(identity);

                return new AuthenticationState(user);
            }
            catch (Exception exp)
            {
                _exceptionHandler.OnExceptionReceived(exp);

                return NoUser();
            }
        }
    }
}
