using Bit.Test;
using IdentityModel.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
                TokenClient tokenClient = testEnvironment.Server.BuildTokenClient("TestResOwner", "secret");
                TokenResponse tokenResponse = await tokenClient.RequestResourceOwnerPasswordAsync("ValidUserName", "ValidPassword", scope: "openid profile user_info");

                Assert.IsFalse(tokenResponse.IsError);
            }
        }

        [TestMethod]
        [TestCategory("IdentityServer")]
        public virtual async Task LoginWithInValidUserNameAndPasswordUsingCodeMayNotWorksFine()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = false }))
            {
                TokenClient tokenClient = testEnvironment.Server.BuildTokenClient("TestResOwner", "secret");
                TokenResponse tokenResponse = await tokenClient.RequestResourceOwnerPasswordAsync("InValidUser", "InvalidPassword", scope: "openid profile user_info");

                Assert.IsTrue(tokenResponse.IsError);
            }
        }
    }
}
