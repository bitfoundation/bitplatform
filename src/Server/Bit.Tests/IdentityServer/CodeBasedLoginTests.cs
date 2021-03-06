using Bit.Core.Contracts;
using Bit.Test;
using Bit.Test.Server;
using Bit.Http.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Bit.Core.Exceptions;

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
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", "TestResOwner");

                Assert.AreEqual("test", await (await testEnvironment.Server.BuildHttpClient(token).GetAsync("api/customers/get-custom-data?api-version=1.0")).Content.ReadAsAsync<string>()); // see TestUserService
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
                    await testEnvironment.Server.LoginWithCredentials("InValidUser", "InvalidPassword", "TestResOwner");
                    Assert.Fail();
                }
                catch (LoginFailureException exp) when (exp.Message == "LoginFailed") { }
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
                    await testEnvironment.Server.LoginWithCredentials("InValidUser", "InvalidPassword", "TestResOwner", acr_values: new Dictionary<string, string>
                    {
                        { "x",  "1" },
                        { "y",  "2" }
                    });

                    Assert.Fail();
                }
                catch (LoginFailureException exp) when (exp.Message == "LoginFailed") { }

                bool acr_values_are_logged = testEnvironment.GetObjects<ILogger>()
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
                    Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                    using (RemoteWebDriver driver = testEnvironment.Server.BuildWebDriver(new RemoteWebDriverOptions { Token = token }))
                    {
                        await driver.ExecuteTest("testAcrValuesAndIdentityServerLoggingTogether");
                    }
                }
                catch { }

                bool acr_values_are_logged = testEnvironment.GetObjects<ILogger>()
                     .Any(l => l.LogData.Any(ld => ld.Key == "AcrValues" && ((List<string>)ld.Value).SequenceEqual(new[] { "x:1", "y:2" })));

                Assert.IsTrue(acr_values_are_logged);
            }
        }
    }
}
