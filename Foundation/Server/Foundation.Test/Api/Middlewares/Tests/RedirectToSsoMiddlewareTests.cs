using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FakeItEasy;
using Foundation.Core.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Foundation.Test.Api.Middlewares.Tests
{
    [TestClass]
    public class RedirectToSsoMiddlewareTests
    {
        [TestMethod]
        [TestCategory("RedirectToSso"), TestCategory("Security")]
        public async Task RedirectToSsoIfNotLoggedInOnRootUrl()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment(new TestEnvironmentArgs
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

                Assert.AreEqual(new Uri($@"http://127.0.0.1:8080/core/connect/authorize?scope=openid profile user_info&client_id=Test&redirect_uri=http://127.0.0.1/SignIn&response_type=id_token token&state={{""pathname"":""/some-page""}}&nonce=RandomString"),
                    getDefaultPageResponse.Headers.Location);
            }
        }

        [TestMethod]
        [TestCategory("RedirectToSso"), TestCategory("Security")]
        public async Task DontRedirectToSsoIfLoggedIn()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment())
            {
                OAuthToken token = testEnvironment.Server.Login("ValidUserName", "ValidPassword");

                HttpResponseMessage getDefaultPageResponse = await testEnvironment.Server.GetHttpClient(token)
                    .GetAsync("/");

                Assert.AreNotEqual(HttpStatusCode.Redirect, getDefaultPageResponse.StatusCode);
            }
        }
    }
}
