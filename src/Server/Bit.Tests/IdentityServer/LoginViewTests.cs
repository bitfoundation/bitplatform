using System.Linq;
using Bit.Test;
using Bit.Test.Implementations;
using Bit.Test.Server;
using Bit.Tests.IdentityServer.Implementations;
using FakeItEasy;
using IdentityServer3.Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support.UI;
using System;
using System.Threading;
using System.Collections.Generic;
using Bit.Core.Contracts;

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
                using (RemoteWebDriver driver = testEnvironment.Server.BuildWebDriver(new RemoteWebDriverOptions { Uri = testEnvironment.Server.GetLoginUrl(), ClientSideTest = false }))
                {
                    new WebDriverWait(driver, TimeSpan.FromSeconds(3))
                        .Until(ExpectedConditions.ElementExists(By.Name("loginForm")));

                    driver.FindElementByName("username").SendKeys("ValidUserName");

                    driver.FindElementByName("password").SendKeys("ValidPassword");

                    driver.FindElementByName("login").Click();
                }

                bool foundAnyCorrectCall = false;

                foreach (TestUserService userService in testEnvironment.GetObjects<TestUserService>())
                {
                    try
                    {
                        A.CallTo(() => userService.AuthenticateLocalAsync(A<LocalAuthenticationContext>.That.Matches(cntx => cntx.UserName == "ValidUserName" && cntx.Password == "ValidPassword"), A<CancellationToken>.Ignored))
                            .MustHaveHappenedOnceExactly();

                        foundAnyCorrectCall = true;
                    }
                    catch (ExpectationException) { }
                }

                Assert.IsTrue(foundAnyCorrectCall);
            }
        }

        [TestMethod]
        [TestCategory("IdentityServer")]
        public virtual void InValidLoginMustShowsAnError()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                using (RemoteWebDriver driver = testEnvironment.Server.BuildWebDriver(new RemoteWebDriverOptions
                {
                    Uri = testEnvironment.Server.GetLoginUrl(acr_values: new Dictionary<string, string> { { "x", "1" }, { "y", "2" } }),
                    ClientSideTest = false
                }))
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

                bool foundAnyCorrectCall = false;

                foreach (TestUserService userService in testEnvironment.GetObjects<TestUserService>())
                {
                    try
                    {
                        A.CallTo(() => userService.AuthenticateLocalAsync(A<LocalAuthenticationContext>.That.Matches(cntx => cntx.UserName == "InValidUserName" && cntx.Password == "InValidPassword"), A<CancellationToken>.Ignored))
                            .MustHaveHappenedOnceExactly();

                        foundAnyCorrectCall = true;
                    }
                    catch (ExpectationException) { }
                }

                Assert.IsTrue(foundAnyCorrectCall);

                bool acr_values_are_logged = testEnvironment.GetObjects<ILogger>()
                     .Any(l => l.LogData.Any(ld => ld.Key == "AcrValues" && ((string[])ld.Value).SequenceEqual(new[] { "x:1", "y:2" })));

                Assert.IsTrue(acr_values_are_logged);
            }
        }
    }
}
