using Foundation.Test;
using IdentityModel.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IdentityServer.Test.Api
{
    [TestClass]
    public class CodeBasedLoginTests
    {
        [TestMethod]
        [TestCategory("IdentityServer")]
        public virtual void LoginWithValidUserNameAndPasswordUsingCodeShouldWorksFine()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment(new TestEnvironmentArgs { UseRealServer = false }))
            {
                TokenClient tokenClient = testEnvironment.Server.BuildTokenClient("TestResOwner", "secret");
                TokenResponse tokenResponse = tokenClient.RequestResourceOwnerPasswordAsync("ValidUserName", "ValidPassword", scope: "openid profile user_info").Result;

                Assert.IsFalse(tokenResponse.IsError);
            }
        }

        [TestMethod]
        [TestCategory("IdentityServer")]
        public virtual void LoginWithInValidUserNameAndPasswordUsingCodeMayNotWorksFine()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment(new TestEnvironmentArgs { UseRealServer = false }))
            {
                TokenClient tokenClient = testEnvironment.Server.BuildTokenClient("TestResOwner", "secret");
                TokenResponse tokenResponse = tokenClient.RequestResourceOwnerPasswordAsync("InValidUser", "InvalidPassword", scope: "openid profile user_info").Result;

                Assert.IsTrue(tokenResponse.IsError);
            }
        }
    }
}
