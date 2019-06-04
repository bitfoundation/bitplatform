using Bit.Core.Contracts;
using Bit.Test;
using Bit.Test.Implementations;
using Bit.Test.Server;
using IdentityModel.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Bit.Tests.IdentityServer
{
    [TestClass]
    public class CodeBasedLoginTests
    {
        [TestMethod]
        [TestCategory("IdentityServer")]
        public virtual async Task LoginWithValidUserNameAndPasswordUsingCodeShouldWorksFine()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = false }))
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", "TestResOwner");

                Assert.AreEqual("test", await (await testEnvironment.Server.BuildHttpClient(token).GetAsync("api/customers/get-custom-data?api-version=1.0")).Content.ReadAsAsync<string>()); // see TestUserService

                Assert.IsFalse(token.IsError);
            }
        }

        [TestMethod]
        [TestCategory("IdentityServer")]
        public virtual async Task LoginWithInValidUserNameAndPasswordUsingCodeMayNotWorksFine()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = false }))
            {
                try
                {
                    await testEnvironment.Server.Login("InValidUser", "InvalidPassword", "TestResOwner");
                    Assert.Fail();
                }
                catch (Exception exp) when (exp.Message == "invalid_grant {\"error\":\"invalid_grant\",\"error_description\":\"LoginFailed\"}") { }
            }
        }

        [TestMethod]
        [TestCategory("IdentityServer")]
        public virtual async Task TestAcrValuesAndIdentityServerLoggingTogether_CSharpClient()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = false }))
            {
                try
                {
                    await testEnvironment.Server.Login("InValidUser", "InvalidPassword", "TestResOwner", parameters: new Dictionary<string, string>
                    {
                        { "acr_values", "x:1 y:2" }
                    });

                    Assert.Fail();
                }
                catch (Exception exp) when (exp.Message == "invalid_grant {\"error\":\"invalid_grant\",\"error_description\":\"LoginFailed\"}") { }

                bool acr_values_are_logged = TestDependencyManager.CurrentTestDependencyManager
                     .Objects
                     .OfType<ILogger>()
                     .Any(l => l.LogData.Any(ld => ld.Key == "AcrValues" && ((List<string>)ld.Value).SequenceEqual(new[] { "x:1", "y:2" })));

                Assert.IsTrue(acr_values_are_logged);
            }
        }

        [TestMethod]
        [TestCategory("IdentityServer")]
        public virtual async Task TestAcrValuesAndIdentityServerLoggingTogether_TypeScriptClient()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                try
                {
                    TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                    using (RemoteWebDriver driver = testEnvironment.Server.BuildWebDriver(new RemoteWebDriverOptions { Token = token }))
                    {
                        await driver.ExecuteTest("testAcrValuesAndIdentityServerLoggingTogether");
                    }
                }
                catch { }

                bool acr_values_are_logged = TestDependencyManager.CurrentTestDependencyManager
                     .Objects
                     .OfType<ILogger>()
                     .Any(l => l.LogData.Any(ld => ld.Key == "AcrValues" && ((List<string>)ld.Value).SequenceEqual(new[] { "x:1", "y:2" })));

                Assert.IsTrue(acr_values_are_logged);
            }
        }
    }
}
