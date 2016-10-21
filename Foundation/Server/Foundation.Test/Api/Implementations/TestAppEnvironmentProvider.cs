using System;
using Foundation.Core.Contracts;
using Foundation.Core.Models;
using System.Collections.Generic;

namespace Foundation.Test.Api.Implementations
{
    public class TestAppEnvironmentProvider : IAppEnvironmentProvider
    {
        protected TestAppEnvironmentProvider()
        {

        }

        private readonly Action<AppEnvironment> _activeAppEnvironmentCustomizer;

        public TestAppEnvironmentProvider(Action<AppEnvironment> activeAppEnvironmentCustomizer = null)
        {
            _activeAppEnvironmentCustomizer = activeAppEnvironmentCustomizer;
        }

        private AppEnvironment _activeAppEnvironment;

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
                        SSOServerUrl = $"http://127.0.0.1:8080/core/",
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
                        new EnvironmentConfig { Key = "TestDbConnectionString", Value = @"TestDbConnectionString" + Guid.NewGuid().ToString() },
                        new EnvironmentConfig { Key = "FoundationReadDbConnectionString", Value = @"TestDbConnectionString" + Guid.NewGuid().ToString() },
                        new EnvironmentConfig { Key = "IdentityCertificatePassword" , Value = "P@ssw0rd" },
                        new EnvironmentConfig { Key = "ClientSideAccessibleConfigTest", Value = true, AccessibleInClientSide = true},
                        new EnvironmentConfig { Key = "ClientHostBaseUri", Value = "http://127.0.0.1" , AccessibleInClientSide = true },
                        new EnvironmentConfig { Key = "ClientHostVirtualPath", Value = "/" , AccessibleInClientSide = true }
                    }
                };
            }

            if (_activeAppEnvironmentCustomizer != null)
                _activeAppEnvironmentCustomizer(_activeAppEnvironment);

            return _activeAppEnvironment;
        }
    }
}
