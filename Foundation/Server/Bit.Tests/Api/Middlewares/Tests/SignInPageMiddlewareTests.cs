using System.Linq;
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
                HttpResponseMessage getSignInPage = await testEnvironment.Server.GetHttpClient()
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
                HttpResponseMessage getSignInPage = await testEnvironment.Server.GetHttpClient()
                    .GetAsync("/SignIn");

                Assert.AreEqual(false, getSignInPage.Headers.CacheControl.Public);

                Assert.AreEqual(null, getSignInPage.Headers.CacheControl.MaxAge);

                Assert.AreEqual(true, getSignInPage.Headers.CacheControl.NoCache);

                Assert.AreEqual(true, getSignInPage.Headers.CacheControl.NoStore);

                Assert.AreEqual(true, getSignInPage.Headers.CacheControl.MustRevalidate);
            }
        }

        [TestMethod]
        [TestCategory("SignInPage"), TestCategory("Security")]
        public async Task SignInPageResponseMustHaveSecurityHeaders()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                HttpResponseMessage getSignInPage = await testEnvironment.Server.GetHttpClient()
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
                    if (!environment.Configs.Any(c =>
                    {
                        bool isRequireSslConfig = c.Key == "RequireSsl";

                        if (isRequireSslConfig == true)
                            c.Value = true;

                        return isRequireSslConfig;
                    }))
                    {
                        environment.Configs.Add(new EnvironmentConfig { Key = "RequireSsl", Value = true });
                    }
                }
            }))
            {
                HttpResponseMessage getSignInPage = await testEnvironment.Server.GetHttpClient()
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
