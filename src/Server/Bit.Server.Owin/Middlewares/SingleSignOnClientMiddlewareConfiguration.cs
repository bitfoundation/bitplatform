using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.Owin.Contracts;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin.Security.Jwt;
using Owin;
using System;
using System.Security.Cryptography;

namespace Bit.Owin.Middlewares
{
    public class SingleSignOnClientMiddlewareConfiguration : IOwinMiddlewareConfiguration
    {
        public virtual IAppCertificatesProvider AppCertificatesProvider { get; set; } = default!;

        public virtual AppEnvironment AppEnvironment { get; set; } = default!;

        public virtual void Configure(IAppBuilder owinApp)
        {
            if (owinApp == null)
                throw new ArgumentNullException(nameof(owinApp));

            UseJwtBearerAuthentication(owinApp);
        }

        protected virtual void UseJwtBearerAuthentication(IAppBuilder owinApp)
        {
            string issuerName = AppEnvironment.GetSsoIssuerName();

            RsaSecurityKey issuerSigningKey = new RsaSecurityKey(AppCertificatesProvider.GetSingleSignOnClientRsaKey());

            JwtBearerAuthenticationOptions jwtBearerAuthenticationOptions = new JwtBearerAuthenticationOptions
            {
                AllowedAudiences = new[] { $"{issuerName}/resources" },
                TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    ValidateTokenReplay = true,
                    ValidateLifetime = true,
                    ValidateActor = true,
                    ValidateAudience = true,
                    ValidateIssuer = true,
                    ValidIssuer = issuerName,
                    ValidAudience = $"{issuerName}/resources",
                    IssuerSigningKey = issuerSigningKey
                }
            };

            owinApp.UseJwtBearerAuthentication(jwtBearerAuthenticationOptions);
        }
    }
}
