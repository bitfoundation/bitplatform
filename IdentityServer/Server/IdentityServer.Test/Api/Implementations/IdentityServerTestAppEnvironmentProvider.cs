using Foundation.Core.Contracts;
using Foundation.Core.Models;
using System.Collections.Generic;

namespace IdentityServer.Test.Api.Implementations
{
    public class IdentityServerTestAppEnvironmentProvider : IAppEnvironmentProvider
    {
        private AppEnvironment _activeAppEnvironment;

        public virtual AppEnvironment GetActiveAppEnvironment()
        {
            if (_activeAppEnvironment == null)
            {
                string version = "1";

                _activeAppEnvironment = new AppEnvironment
                {
                    Name = "Test",
                    IsActive = true,
                    DebugMode = true,
                    AppInfo = new EnvironmentAppInfo
                    {
                        Name = "TestIdentityServer",
                        Version = version,
                        DefaultTheme = "LightBlue",
                        DefaultCulture = "FaIr"
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
                                    Title = "Identity Server"
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
                                    Title = "سرور احراز هویت"
                                }
                            }
                        }
                    },
                    Configs = new List<EnvironmentConfig>
                    {
                        new EnvironmentConfig { Key = "StaticFilesRelativePath", Value = @"..\..\..\..\..\..\" },
                        new EnvironmentConfig { Key = "IdentityCertificatePassword" , Value = "P@ssw0rd" },
                        new EnvironmentConfig { Key = "IdentityServerSiteName" , Value = "Identity Server" },
                        new EnvironmentConfig { Key = "IdentityServerCertificatePath", Value = @"..\..\IdentityServerCertificate.pfx" },
                        new EnvironmentConfig { Key = "SsoPageTemplatePath" , Value = @"..\..\ssoPageTemplate.cshtml" }
                    }
                };
            }

            return _activeAppEnvironment;
        }
    }
}
