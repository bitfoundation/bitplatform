using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Bit.Core.Models;
using Bit.Test;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Tests.Api.Middlewares.Tests
{
    [TestClass]
    public class SignInPageMiddlewareTests
    {
        [TestMethod]
        [TestCategory("SignInPage")]
        public async Task ReturnSignInPage()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                HttpResponseMessage getSignInPage = await testEnvironment.Server.BuildHttpClient()
                    .GetAsync("/SignIn");

                Assert.AreEqual(HttpStatusCode.OK, getSignInPage.StatusCode);
            }
        }

        [TestMethod]
        [TestCategory("SignInPage"), TestCategory("Caching")]
        public async Task SignInPageResponseMustNotBeCacheable()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                HttpResponseMessage getSignInPage = await testEnvironment.Server.BuildHttpClient()
                    .GetAsync("/SignIn");

                Assert.AreEqual(false, getSignInPage.Headers.CacheControl.Public);

                Assert.AreEqual(null, getSignInPage.Headers.CacheControl.MaxAge);

                Assert.AreEqual(true, getSignInPage.Headers.CacheControl.NoStore);
            }
        }

        [TestMethod]
        [TestCategory("SignInPage"), TestCategory("Security")]
        public async Task SignInPageResponseMustHaveSecurityHeaders()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                HttpResponseMessage getSignInPage = await testEnvironment.Server.BuildHttpClient()
                    .GetAsync("/SignIn");

                Assert.AreEqual(true, getSignInPage.Headers.Contains("X-Frame-Options"));
                Assert.AreEqual(true, getSignInPage.Headers.Contains("X-Content-Type-Options"));
                Assert.AreEqual(true, getSignInPage.Headers.Contains("X-Download-Options"));
                Assert.AreEqual(true, getSignInPage.Headers.Contains("X-XSS-Protection"));
            }
        }

        [TestMethod]
        [TestCategory("SignInPage"), TestCategory("Security")]
        public async Task SignInPageResponseMustHaveStrictTransportSecurityHeadersInCaseOfSslRequired()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs
            {
                UseHttps = true,
                ActiveAppEnvironmentCustomizer = environment =>
                {
                    environment.AddOrReplace(new EnvironmentConfig { Key = AppEnvironment.KeyValues.RequireSsl, Value = true });
                }
            }))
            {
                HttpResponseMessage getSignInPage = await testEnvironment.Server.BuildHttpClient()
                    .GetAsync($"{testEnvironment.Server.Uri}SignIn");

                Assert.AreEqual(true, getSignInPage.Headers.Contains("X-Frame-Options"));
                Assert.AreEqual(true, getSignInPage.Headers.Contains("X-Content-Type-Options"));
                Assert.AreEqual(true, getSignInPage.Headers.Contains("X-Download-Options"));
                Assert.AreEqual(true, getSignInPage.Headers.Contains("X-XSS-Protection"));
                Assert.AreEqual(true, getSignInPage.Headers.Contains("Strict-Transport-Security"));
            }
        }
    }
}
