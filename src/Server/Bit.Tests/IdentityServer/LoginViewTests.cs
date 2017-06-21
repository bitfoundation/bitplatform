using System.Linq;
using Bit.Test;
using Bit.Test.Core.Implementations;
using Bit.Test.Server;
using Bit.Tests.IdentityServer.Implementations;
using FakeItEasy;
using IdentityServer3.Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;

namespace Bit.Tests.IdentityServer
{
    [TestClass]
    public class LoginViewTests
    {
        [TestMethod]
        [TestCategory("IdentityServer")]
        public virtual void ValidLoginMustBeRedirectedToClientUrl()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                using (RemoteWebDriver driver = testEnvironment.Server.GetWebDriver(new RemoteWebDriverOptions { Uri = @"core/connect/authorize?scope=openid profile user_info&client_id=Test&redirect_uri=http://localhost/SignIn&response_type=id_token token&state={}&nonce=SgPoeilE1Tub", ClientSideTest = false }))
                {
                    driver.GetElementById("username").SendKeys("ValidUserName");

                    driver.GetElementById("password").SendKeys("ValidPassword");

                    driver.GetElementById("login").Click();

                    driver.WaitForControlReady();
                }

                TestUserService testUserService = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<TestUserService>()
                    .Single();

                A.CallTo(() => testUserService.AuthenticateLocalAsync(A<LocalAuthenticationContext>.That.Matches(cntx => cntx.UserName == "ValidUserName" && cntx.Password == "ValidPassword")))
                    .MustHaveHappened(Repeated.Exactly.Once);
            }
        }

        [TestMethod]
        [TestCategory("IdentityServer")]
        public virtual void InValidLoginMustShowsAnError()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                using (RemoteWebDriver driver = testEnvironment.Server.GetWebDriver(new RemoteWebDriverOptions { Uri = @"core/connect/authorize?scope=openid profile user_info&client_id=Test&redirect_uri=http://localhost/SignIn&response_type=id_token token&state={}&nonce=SgPoeilE1Tub", ClientSideTest = false }))
                {
                    driver.GetElementById("username").SendKeys("InValidUserName");
                    driver.GetElementById("password").SendKeys("InValidPassword");
                    driver.GetElementById("login").Click();
                    driver.WaitForControlReady();
                    Assert.AreEqual("Login failed", driver.GetElementById("error").GetAttribute("innerText"));
                }

                TestUserService testUserService = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<TestUserService>()
                    .Single();

                A.CallTo(() => testUserService.AuthenticateLocalAsync(A<LocalAuthenticationContext>.That.Matches(cntx => cntx.UserName == "InValidUserName" && cntx.Password == "InValidPassword")))
                    .MustHaveHappened(Repeated.Exactly.Once);
            }
        }
    }
}
