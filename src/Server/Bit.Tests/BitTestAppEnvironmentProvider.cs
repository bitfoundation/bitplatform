using System;
using System.Collections.Generic;
using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.Test;
using Bit.Tests.Properties;

namespace Bit.Tests
{
    public class BitTestAppEnvironmentProvider : IAppEnvironmentProvider
    {
        protected BitTestAppEnvironmentProvider()
        {

        }

        public BitTestAppEnvironmentProvider(TestEnvironmentArgs args)
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
                    Name = "Development",
                    IsActive = true,
                    DebugMode = true,
                    AppInfo = new EnvironmentAppInfo
                    {
                        Name = "Test",
                        Version = "1",
                        DefaultTheme = "LightBlue",
                        DefaultCulture = "EnUs"
                    },
                    Security = new EnvironmentSecurity
                    {
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
                        new EnvironmentConfig { Key = "DefaultPageTemplatePath", Value = @".\bit-framework\src\Server\Bit.Tests\defaultPageTemplate.cshtml" },
                        new EnvironmentConfig { Key = "IdentityServerCertificatePath", Value = @"..\..\IdentityServerCertificate.pfx" },
                        new EnvironmentConfig { Key = "StaticFilesRelativePath", Value = @"..\..\..\..\..\..\" },
                        new EnvironmentConfig { Key = "TestDbConnectionString", Value = string.Format(Settings.Default.TestDbConnectionString, Guid.NewGuid())  },
                        new EnvironmentConfig { Key = "IdentityCertificatePassword" , Value = "P@ssw0rd" },
                        new EnvironmentConfig { Key = "ClientSideAccessibleConfigTest", Value = true, AccessibleInClientSide = true},
                        new EnvironmentConfig { Key = "HostVirtualPath", Value = "/" , AccessibleInClientSide = true },
                        new EnvironmentConfig { Key = "KendoUILoadMode" , Value = "Web" /*Web|Core*/ , AccessibleInClientSide = true },
                        new EnvironmentConfig { Key = "SsoPageTemplatePath" , Value = @"bit-framework\src\Server\Bit.Tests\ssoPageTemplate.cshtml" }
                    }
                };
            }

            if (_args?.ActiveAppEnvironmentCustomizer != null)
                _args?.ActiveAppEnvironmentCustomizer(_activeAppEnvironment);

            return _activeAppEnvironment;
        }
    }
}
