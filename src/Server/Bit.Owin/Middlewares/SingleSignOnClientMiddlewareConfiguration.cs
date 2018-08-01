using Bit.Core.Contracts;
using Bit.Core.Extensions;
using Bit.Core.Models;
using Bit.Owin.Contracts;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin.Security.Jwt;
using Owin;
using System;
using System.IdentityModel.Tokens.Jwt;

namespace Bit.Owin.Middlewares
{
    public class SingleSignOnClientMiddlewareConfiguration : IOwinMiddlewareConfiguration
    {
        public virtual IAppCertificatesProvider AppCertificatesProvider { get; set; }

        public virtual AppEnvironment AppEnvironment { get; set; }

        public virtual void Configure(IAppBuilder owinApp)
        {
            if (owinApp == null)
                throw new ArgumentNullException(nameof(owinApp));

            UseJwtBearerAuthentication(owinApp);
        }

        protected virtual void UseJwtBearerAuthentication(IAppBuilder owinApp)
        {
            string issuerName = AppEnvironment.GetSsoIssuerName();

            JwtBearerAuthenticationOptions jwtBearerAuthenticationOptions = new JwtBearerAuthenticationOptions
            {
                AllowedAudiences = new string[] { $"{issuerName}/resources" },
                IssuerSecurityKeyProviders = new[]
                {
                    new X509CertificateSecurityKeyProvider(issuerName, AppCertificatesProvider.GetSingleSignOnCertificate())
                }
            };

            if (PlatformUtilities.IsRunningOnMono)
            {
                jwtBearerAuthenticationOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = false,
                    ValidIssuer = issuerName,
                    ValidAudience = $"{issuerName}/resources",
                    IssuerSigningKey = new X509SecurityKey(AppCertificatesProvider.GetSingleSignOnCertificate()),
                    SignatureValidator = (token, parameters) =>
                    {
                        JwtSecurityToken jwt = new JwtSecurityToken(token);

                        return jwt;
                    }
                };
            }

            owinApp.UseJwtBearerAuthentication(jwtBearerAuthenticationOptions);
        }
    }
}