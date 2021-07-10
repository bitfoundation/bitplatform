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
                    await testEnvironment.Server.LoginWithCredentials("+9891255447788", "سلام به معنی Hello است", "TestResOwner", acr_values: new Dictionary<string, string>
                    {
                        { "x",  "1:11111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111" },
                        { "y",  "test test:test" }
                    });

                    Assert.Fail();
                }
                catch (LoginFailureException exp) when (exp.Message == "LoginFailed") { }

                var logger = testEnvironment.GetObjects<ILogger>();

                bool x_is_logged = logger
                     .Any(l => l.LogData.Any(ld => ld.Key == "x" && ((string)ld.Value).Equals("1:11111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111")));

                bool y_is_logged = logger
                     .Any(l => l.LogData.Any(ld => ld.Key == "y" && ((string)ld.Value).Equals("test test:test")));

                bool password_is_logged = logger
                    .Any(l => l.LogData.Any(ld => ld.Key == "password" && ((string)ld.Value) == "سلام به معنی Hello است"));

                bool username_is_logged = logger
                    .Any(l => l.LogData.Any(ld => ld.Key == "username" && ((string)ld.Value) == "+9891255447788"));

                Assert.IsTrue(x_is_logged);
                Assert.IsTrue(y_is_logged);
                Assert.IsTrue(password_is_logged);
                Assert.IsTrue(username_is_logged);
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

                var logger = testEnvironment.GetObjects<ILogger>();

                bool x_is_logged = logger
                     .Any(l => l.LogData.Any(ld => ld.Key == "x" && ((string)ld.Value).Equals("1:11111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111")));

                bool y_is_logged = logger
                     .Any(l => l.LogData.Any(ld => ld.Key == "y" && ((string)ld.Value).Equals("test test:test")));

                bool password_is_logged = logger
                    .Any(l => l.LogData.Any(ld => ld.Key == "password" && ((string)ld.Value) == "سلام به معنی Hello است"));
                
                bool username_is_logged = logger
                    .Any(l => l.LogData.Any(ld => ld.Key == "username" && ((string)ld.Value) == "+9891255447788"));

                Assert.IsTrue(x_is_logged);
                Assert.IsTrue(y_is_logged);
                Assert.IsTrue(password_is_logged);
                Assert.IsTrue(username_is_logged);
            }
        }
    }
}
