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
            using (IdentityServerTestEnvironment testEnvironment = new IdentityServerTestEnvironment(useRealServer: false))
            {
                TokenClient tokenClient = testEnvironment.Server.BuildTokenClient("Test2", "secret");
                TokenResponse tokenResponse = tokenClient.RequestResourceOwnerPasswordAsync("ValidUser1", "ValidUser1", scope: "openid profile user_info").Result;

                Assert.IsFalse(tokenResponse.IsError);
            }
        }

        [TestMethod]
        [TestCategory("IdentityServer")]
        public virtual void LoginWithInValidUserNameAndPasswordUsingCodeMayNotWorksFine()
        {
            using (IdentityServerTestEnvironment testEnvironment = new IdentityServerTestEnvironment(useRealServer: false))
            {
                TokenClient tokenClient = testEnvironment.Server.BuildTokenClient("Test2", "secret");
                TokenResponse tokenResponse = tokenClient.RequestResourceOwnerPasswordAsync("InValidUser1", "InValidUser1", scope: "openid profile user_info").Result;

                Assert.IsTrue(tokenResponse.IsError);
            }
        }
    }
}
