using Bit.Test;
using IdentityModel.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Http;
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
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", "TestResOwner");

                Assert.AreEqual("test", await (await testEnvironment.Server.BuildHttpClient(token).GetAsync("api/customers/get-custom-data?api-version=1.0")).Content.ReadAsAsync<string>()); // see TestUserService

                Assert.IsFalse(token.IsError);
            }
        }

        [TestMethod]
        [TestCategory("IdentityServer")]
        public virtual async Task LoginWithInValidUserNameAndPasswordUsingCodeMayNotWorksFine()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = false }))
            {
                TokenResponse token = await testEnvironment.Server.Login("InValidUser", "InvalidPassword", "TestResOwner");

                Assert.IsTrue(token.IsError);
            }
        }
    }
}
