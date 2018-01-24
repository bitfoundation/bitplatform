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
using OpenQA.Selenium.Support.UI;
using System;

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
                using (RemoteWebDriver driver = testEnvironment.Server.BuildWebDriver(new RemoteWebDriverOptions { Uri = @"core/connect/authorize?scope=openid profile user_info&client_id=Test&redirect_uri=http://localhost/SignIn&response_type=id_token token&state={}&nonce=SgPoeilE1Tub", ClientSideTest = false }))
                {
                    new WebDriverWait(driver, TimeSpan.FromSeconds(3))
                        .Until(ExpectedConditions.ElementExists(By.Name("loginForm")));

                    driver.FindElementByName("username").SendKeys("ValidUserName");

                    driver.FindElementByName("password").SendKeys("ValidPassword");

                    driver.FindElementByName("login").Click();
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
                using (RemoteWebDriver driver = testEnvironment.Server.BuildWebDriver(new RemoteWebDriverOptions { Uri = @"core/connect/authorize?scope=openid profile user_info&client_id=Test&redirect_uri=http://localhost/SignIn&response_type=id_token token&state={}&nonce=SgPoeilE1Tub", ClientSideTest = false }))
                {
                    new WebDriverWait(driver, TimeSpan.FromSeconds(3))
                        .Until(ExpectedConditions.ElementExists(By.Name("loginForm")));

                    driver.FindElementByName("username").SendKeys("InValidUserName");
                    driver.FindElementByName("password").SendKeys("InValidPassword");
                    driver.FindElementByName("login").Click();

                    new WebDriverWait(driver, TimeSpan.FromSeconds(3))
                        .Until(ExpectedConditions.ElementExists(By.Name("error")));

                    Assert.AreEqual("Login failed", driver.FindElementByName("error").GetAttribute("innerText"));
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
