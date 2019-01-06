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
                TokenResponse tokenResponse = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", "TestResOwner");

                Assert.IsFalse(tokenResponse.IsError);
            }
        }

        [TestMethod]
        [TestCategory("IdentityServer")]
        public virtual async Task LoginWithInValidUserNameAndPasswordUsingCodeMayNotWorksFine()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = false }))
            {
                TokenResponse tokenResponse = await testEnvironment.Server.Login("InValidUser", "InvalidPassword", "TestResOwner");

                Assert.IsTrue(tokenResponse.IsError);
            }
        }
    }
}
