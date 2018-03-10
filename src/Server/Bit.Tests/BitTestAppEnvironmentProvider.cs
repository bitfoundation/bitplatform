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
                        new EnvironmentConfig { Key = "IndexPagePath", Value = @"./bit-framework/src/Server/Bit.Tests/indexPage.html" },
                        new EnvironmentConfig { Key = "IdentityServerCertificatePath", Value = @"../../../IdentityServerCertificate.pfx" },
                        new EnvironmentConfig { Key = "StaticFilesRelativePath", Value = @"../../../../../../../" },
                        new EnvironmentConfig { Key = "TestDbConnectionString", Value = string.Format(Settings.Default.TestDbConnectionString, Guid.NewGuid())  },
                        new EnvironmentConfig { Key = "IdentityCertificatePassword" , Value = "P@ssw0rd" },
                        new EnvironmentConfig { Key = "ClientSideAccessibleConfigTest", Value = true, AccessibleInClientSide = true},
                        new EnvironmentConfig { Key = "HostVirtualPath", Value = "/" , AccessibleInClientSide = true },
                        new EnvironmentConfig { Key = "LoginPagePath" , Value = @"bit-framework/src/Server/Bit.Tests/loginPage.html" },
                        new EnvironmentConfig { Key = "GoogleClientId" , Value = "563799658385-5vv2jcqml5dv7fosup10e57unahfufdd.apps.googleusercontent.com" },
                        new EnvironmentConfig { Key = "GoogleSecret" , Value = "v8zVdioz7K-rnYjz7MdxL_fQ" }
                    }
                };
            }

            _args?.ActiveAppEnvironmentCustomizer?.Invoke(_activeAppEnvironment);

            return _activeAppEnvironment;
        }
    }
}
