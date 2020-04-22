using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.Owin.Contracts;
using Microsoft.Owin.Security.Jwt;
using Owin;
using System;

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

            JwtBearerAuthenticationOptions jwtBearerAuthenticationOptions = new JwtBearerAuthenticationOptions
            {
                AllowedAudiences = new[] { $"{issuerName}/resources" },
                IssuerSecurityKeyProviders = new[]
                {
                    new X509CertificateSecurityKeyProvider(issuerName, AppCertificatesProvider.GetSingleSignOnCertificate())
                }
            };

            owinApp.UseJwtBearerAuthentication(jwtBearerAuthenticationOptions);
        }
    }
}
