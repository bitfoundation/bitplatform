using FakeItEasy;
using Foundation.Test;
using Foundation.Test.Core.Implementations;
using IdentityServer3.Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium.Remote;
using System.Linq;

namespace IdentityServer.Test.ViewsTests
{
    [TestClass]
    public class LoginViewTests
    {
        [TestMethod]
        [TestCategory("IdentityServer")]
        public virtual void ValidLoginMustBeRedirectedToClientUrl()
        {
            using (IdentityServerTestEnvironment testEnvironment = new IdentityServerTestEnvironment())
            {
                using (RemoteWebDriver driver = testEnvironment.Server.GetWebDriver(new RemoteWebDriverOptions { Uri = @"core/connect/authorize?scope=openid profile user_info&client_id=Test&redirect_uri=http://127.0.0.1/SignIn&response_type=id_token token&state={}&nonce=SgPoeilE1Tub", ClientSideTest = false }))
                {
                    driver.GetElementById("username").SendKeys("ValidUser1");

                    driver.GetElementById("password").SendKeys("ValidUser1");

                    driver.GetElementById("login").Click();

                    driver.WaitForControlReady();
                }

                Api.Implementations.TestUserService testUserService = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<Api.Implementations.TestUserService>()
                    .Single();

                A.CallTo(() => testUserService.AuthenticateLocalAsync(A<LocalAuthenticationContext>.That.Matches(cntx => cntx.UserName == "ValidUser1" && cntx.Password == "ValidUser1")))
                    .MustHaveHappened(Repeated.Exactly.Once);

                // TODO
            }
        }

        [TestMethod]
        [TestCategory("IdentityServer")]
        public virtual void InValidLoginMustShowsAnError()
        {
            using (IdentityServerTestEnvironment testEnvironment = new IdentityServerTestEnvironment())
            {
                using (RemoteWebDriver driver = testEnvironment.Server.GetWebDriver(new RemoteWebDriverOptions { Uri = @"core/connect/authorize?scope=openid profile user_info&client_id=Test&redirect_uri=http://127.0.0.1/SignIn&response_type=id_token token&state={}&nonce=SgPoeilE1Tub", ClientSideTest = false }))
                {
                    driver.GetElementById("username").SendKeys("InValidUser1");
                    driver.GetElementById("password").SendKeys("InValidUser1");
                    driver.GetElementById("login").Click();
                    driver.WaitForControlReady();
                    Assert.AreEqual("ورود ناموفق", driver.GetElementById("error").GetAttribute("innerText"));
                }

                Api.Implementations.TestUserService testUserService = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<Api.Implementations.TestUserService>()
                    .Single();

                A.CallTo(() => testUserService.AuthenticateLocalAsync(A<LocalAuthenticationContext>.That.Matches(cntx => cntx.UserName == "InValidUser1" && cntx.Password == "InValidUser1")))
                    .MustHaveHappened(Repeated.Exactly.Once);
            }
        }
    }
}
