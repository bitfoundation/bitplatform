using Foundation.Core.Contracts;
using Foundation.Core.Models;
using Foundation.Test;
using System;
using System.Collections.Generic;

namespace Foundation.AspNetCore.Test.Api.Implementations
{
    public class FoundationAspNetCoreAppEnvironmentPrvider : IAppEnvironmentProvider
    {
        protected FoundationAspNetCoreAppEnvironmentPrvider()
        {

        }

        public FoundationAspNetCoreAppEnvironmentPrvider(TestEnvironmentArgs args)
        {
            _args = args;
        }

        private AppEnvironment _activeAppEnvironment;
        private readonly TestEnvironmentArgs _args;

        public virtual AppEnvironment GetActiveAppEnvironment()
        {
            if (_activeAppEnvironment == null)
            {
                _activeAppEnvironment = new AppEnvironment
                {
                    Name = "Test",
                    IsActive = true,
                    DebugMode = true,
                    AppInfo = new EnvironmentAppInfo
                    {
                        Name = "Test",
                        Version = "1",
                        DefaultTheme = "LightBlue"
                    },
                    Security = new EnvironmentSecurity
                    {
                        SSOServerUrl = "http://127.0.0.1:8080/core",
                        Scopes = new[] { "openid", "profile", "user_info" },
                        ClientName = "Test",
                        ClientSecret = "secret"
                    },
                    Cultures = new[]
                    {
                        new EnvironmentCulture
                        {
                            Name = "EnUs",
                            Values = new List<EnvironmentCultureValue>
                            {
                                new EnvironmentCultureValue
                                {
                                    Name = "AppTitle",
                                    Title = "Test"
                                }
                            }
                        },
                        new EnvironmentCulture
                        {
                            Name = "FaIr",
                            Values = new List<EnvironmentCultureValue>
                            {
                                new EnvironmentCultureValue
                                {
                                    Name = "AppTitle",
                                    Title = "تست"
                                }
                            }
                        }
                    },
                    Configs = new List<EnvironmentConfig>
                    {
                        new EnvironmentConfig { Key = "DefaultPageTemplatePath", Value = @"..\..\defaultPageTemplate.cshtml" },
                        new EnvironmentConfig { Key = "IdentityServerCertificatePath", Value = @"..\..\IdentityServerCertificate.pfx" },
                        new EnvironmentConfig { Key = "StaticFilesRelativePath", Value = @"..\..\..\..\..\..\" },
                        new EnvironmentConfig { Key = "TestDbConnectionString", Value = @"TestDbConnectionString" + Guid.NewGuid() },
                        new EnvironmentConfig { Key = "FoundationReadDbConnectionString", Value = @"TestDbConnectionString" + Guid.NewGuid() },
                        new EnvironmentConfig { Key = "IdentityCertificatePassword" , Value = "P@ssw0rd" },
                        new EnvironmentConfig { Key = "ClientSideAccessibleConfigTest", Value = true, AccessibleInClientSide = true},
                        new EnvironmentConfig { Key = "ClientHostBaseUri", Value = "http://127.0.0.1" , AccessibleInClientSide = true },
                        new EnvironmentConfig { Key = "ClientHostVirtualPath", Value = "/" , AccessibleInClientSide = true }
                    }
                };
            }

            if (_args?.ActiveAppEnvironmentCustomizer != null)
                _args?.ActiveAppEnvironmentCustomizer(_activeAppEnvironment);

            return _activeAppEnvironment;
        }
    }
}
