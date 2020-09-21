using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.OData.Contracts;
using Bit.Test;
using Bit.Tests.Properties;
using System;
using System.Collections.Generic;

[assembly: ODataModule("v1")]
[assembly: ODataModule("Test")]

namespace Bit.Tests
{
    public class BitTestAppEnvironmentsProvider : IAppEnvironmentsProvider
    {
        protected BitTestAppEnvironmentsProvider()
        {

        }

        public BitTestAppEnvironmentsProvider(TestEnvironmentArgs args)
        {
            _args = args;
        }

        private AppEnvironment _activeAppEnvironment;
        private readonly TestEnvironmentArgs _args;

        public virtual AppEnvironment GetActiveAppEnvironment()
        {
            var (success, message) = TryGetActiveAppEnvironment(out AppEnvironment activeAppEnvironment);
            if (success == true)
                return activeAppEnvironment;
            throw new InvalidOperationException(message);
        }

        public virtual (bool success, string message) TryGetActiveAppEnvironment(out AppEnvironment activeAppEnvironment)
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
                        new EnvironmentConfig { Key = AppEnvironment.KeyValues.IndexPagePath, Value = @"./src/Server/Bit.Tests/indexPage.html" },
                        new EnvironmentConfig { Key = AppEnvironment.KeyValues.IdentityServerCertificatePath, Value = @"../../../IdentityServerCertificate.pfx" },
                        new EnvironmentConfig { Key = AppEnvironment.KeyValues.StaticFilesRelativePath, Value = @"../../../../../../" },
                        new EnvironmentConfig { Key = "TestDbConnectionString", Value = string.Format(Settings.Default.TestDbConnectionString, Guid.NewGuid())  },
                        new EnvironmentConfig { Key = AppEnvironment.KeyValues.IdentityCertificatePassword , Value = "P@ssw0rd" },
                        new EnvironmentConfig { Key = "ClientSideAccessibleConfigTest", Value = true, AccessibleInClientSide = true},
                        new EnvironmentConfig { Key = AppEnvironment.KeyValues.HostVirtualPath, Value = AppEnvironment.KeyValues.HostVirtualPathDefaultValue , AccessibleInClientSide = true },
                        new EnvironmentConfig { Key = AppEnvironment.KeyValues.IdentityServer.LoginPagePath , Value = @"src/Server/Bit.Tests/loginPage.html" },
                        new EnvironmentConfig { Key = AppEnvironment.KeyValues.IdentityServer.GoogleClientId , Value = "563799658385-5vv2jcqml5dv7fosup10e57unahfufdd.apps.googleusercontent.com" },
                        new EnvironmentConfig { Key = AppEnvironment.KeyValues.IdentityServer.GoogleSecret , Value = "v8zVdioz7K-rnYjz7MdxL_fQ" }
                    }
                };
            }

            _args?.ActiveAppEnvironmentCustomizer?.Invoke(_activeAppEnvironment);

            activeAppEnvironment = _activeAppEnvironment;

            return (true, null);
        }
    }
}
