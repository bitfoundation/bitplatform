using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Bit.Core.Contracts;
using Bit.Test;
using FakeItEasy;
using IdentityModel.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Tests.Api.Middlewares.Tests
{
    [TestClass]
    public class RedirectToSsoMiddlewareTests
    {
        [TestMethod]
        [TestCategory("RedirectToSso"), TestCategory("Security")]
        public async Task RedirectToSsoIfNotLoggedInOnRootUrl()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs
            {
                AdditionalDependencies = manager =>
                {
                    IRandomStringProvider randomStringProvider = A.Fake<IRandomStringProvider>();

                    A.CallTo(() => randomStringProvider.GetRandomNonSecureString(12))
                    .Returns("RandomString");

                    manager.RegisterInstance(randomStringProvider);
                }
            }))
            {
                HttpResponseMessage getDefaultPageResponse = await testEnvironment.Server.GetHttpClient()
                    .GetAsync("/some-page", HttpCompletionOption.ResponseHeadersRead);

                Assert.AreEqual(HttpStatusCode.Redirect, getDefaultPageResponse.StatusCode);

                Assert.AreEqual(@"/core/connect/authorize?scope=openid profile user_info&client_id=Test&redirect_uri=http://localhost/SignIn&response_type=id_token token&state={""pathname"":""/some-page""}&nonce=RandomString", getDefaultPageResponse.Headers.Location.ToString());
            }
        }

        [TestMethod]
        [TestCategory("RedirectToSso"), TestCategory("Security")]
        public async Task DontRedirectToSsoIfLoggedIn()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                HttpResponseMessage getDefaultPageResponse = await testEnvironment.Server.GetHttpClient(token)
                    .GetAsync("/");

                Assert.AreNotEqual(HttpStatusCode.Redirect, getDefaultPageResponse.StatusCode);
            }
        }
    }
}
