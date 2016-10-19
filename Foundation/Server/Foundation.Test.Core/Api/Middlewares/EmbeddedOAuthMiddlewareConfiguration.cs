using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Foundation.Api.Contracts;
using Microsoft.Owin;
using Microsoft.Owin.Security.OAuth;
using Owin;

namespace Foundation.Test.Api.Middlewares
{
    public interface ITestUserService
    {
        Task<bool> IsValidAsync(string userName, string password);

        Task<IEnumerable<Claim>> GetClaimsAsync(string userName, string password);
    }

    public class TestUserService : ITestUserService
    {
        public virtual Task<bool> IsValidAsync(string userName, string password)
        {
            if (password == "ValidPassword")
                return Task.FromResult(true);
            return Task.FromResult(false);
        }

        public virtual Task<IEnumerable<Claim>> GetClaimsAsync(string userName, string password)
        {
            return Task.FromResult(new[]
            {
                new Claim("sub", userName),
                new Claim("primary_sid", userName),
                new Claim("upn", userName),
                new Claim("name",userName),
                new Claim("given_name", userName)
            }.AsEnumerable());
        }
    }

    public class TestAuthorizationServerProvider : OAuthAuthorizationServerProvider
    {
        private readonly ITestUserService _testUserService;

        public TestAuthorizationServerProvider(ITestUserService testUserService)
        {
            _testUserService = testUserService;
        }

        public override Task ValidateClientAuthentication(OAuthValidateClientAuthenticationContext context)
        {
            context.Validated();
            return Task.FromResult<object>(null);
        }

        public override async Task GrantResourceOwnerCredentials(OAuthGrantResourceOwnerCredentialsContext context)
        {
            bool isValid = await _testUserService.IsValidAsync(context.UserName, context.Password);

            if (isValid == false)
            {
                context.SetError("invalid_grant", "The username or password is incorrect.");
                return;
            }

            ClaimsIdentity identity = new ClaimsIdentity(context.Options.AuthenticationType);

            identity.AddClaims(await _testUserService.GetClaimsAsync(context.UserName, context.Password));

            context.Validated(identity);
        }
    }

    public class EmbeddedOAuthMiddlewareConfiguration : IOwinMiddlewareConfiguration
    {
        private readonly ITestUserService _testUserService;

        protected EmbeddedOAuthMiddlewareConfiguration()
        {

        }

        public EmbeddedOAuthMiddlewareConfiguration(ITestUserService testUserService)
        {
            if (testUserService == null)
                throw new ArgumentNullException(nameof(testUserService));

            _testUserService = testUserService;
        }

        public virtual void Configure(IAppBuilder owinApp)
        {
            OAuthAuthorizationServerOptions oAuthServerOptions = new OAuthAuthorizationServerOptions
            {
                AllowInsecureHttp = true,
                TokenEndpointPath = new PathString("/token"),
                AccessTokenExpireTimeSpan = TimeSpan.FromDays(1),
                Provider = new TestAuthorizationServerProvider(_testUserService)
            };

            owinApp.UseOAuthAuthorizationServer(oAuthServerOptions);

            owinApp.UseOAuthBearerAuthentication(new OAuthBearerAuthenticationOptions());
        }
    }
}
