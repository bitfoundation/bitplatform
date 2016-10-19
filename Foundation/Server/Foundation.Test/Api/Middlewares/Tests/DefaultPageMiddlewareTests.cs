using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using OpenQA.Selenium.Remote;
using Foundation.Core.Models;

namespace Foundation.Test.Api.Middlewares.Tests
{
    [TestClass]
    public class DefaultPageMiddlewareTests
    {
        [TestMethod]
        [TestCategory("DefaultPage"), TestCategory("Security")]
        public async Task ReturnDefaultPageForLoggedInUsersOnly()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment())
            {
                OAuthToken token = testEnvironment.Server.Login("ValidUserName", "ValidPassword");

                HttpResponseMessage getDefaultPageResponse = await testEnvironment.Server.GetHttpClient(token)
                    .GetAsync("/");

                Assert.AreEqual(HttpStatusCode.OK, getDefaultPageResponse.StatusCode);

                Assert.AreEqual("text/html", getDefaultPageResponse.Content.Headers.ContentType.MediaType);

                Assert.AreEqual("utf-8", getDefaultPageResponse.Content.Headers.ContentType.CharSet);
            }
        }

        [TestMethod]
        [TestCategory("DefaultPage"), TestCategory("Security")]
        public async Task DontReturnDefaultPageForNotLoggedInUsers()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment())
            {
                HttpResponseMessage getDefaultPageResponse = await testEnvironment.Server.GetHttpClient()
                    .GetAsync("/");

                Assert.AreNotEqual(HttpStatusCode.OK, getDefaultPageResponse.StatusCode);
            }
        }

        [TestMethod]
        [TestCategory("DefaultPage"), TestCategory("Caching")]
        public async Task DefaultPageResponseMustNotBeCacheable()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment())
            {
                OAuthToken token = testEnvironment.Server.Login("ValidUserName", "ValidPassword");

                HttpResponseMessage getDefaultPageResponse = await testEnvironment.Server.GetHttpClient(token)
                    .GetAsync("/");

                Assert.AreEqual(false, getDefaultPageResponse.Headers.CacheControl.Public);

                Assert.AreEqual(null, getDefaultPageResponse.Headers.CacheControl.MaxAge);

                Assert.AreEqual(true, getDefaultPageResponse.Headers.CacheControl.NoCache);

                Assert.AreEqual(true, getDefaultPageResponse.Headers.CacheControl.NoStore);

                Assert.AreEqual(true, getDefaultPageResponse.Headers.CacheControl.MustRevalidate);
            }
        }

        [TestMethod]
        [TestCategory("DefaultPage"), TestCategory("Security")]
        public async Task DefaultPageResponseMustHaveSecurityHeaders()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment())
            {
                OAuthToken token = testEnvironment.Server.Login("ValidUserName", "ValidPassword");

                HttpResponseMessage getDefaultPageResponse = await testEnvironment.Server.GetHttpClient(token)
                    .GetAsync("/");

                Assert.AreEqual(true, getDefaultPageResponse.Headers.Contains("X-Content-Type-Options"));
                Assert.AreEqual(true, getDefaultPageResponse.Headers.Contains("X-Download-Options"));
                Assert.AreEqual(true, getDefaultPageResponse.Headers.Contains("X-XSS-Protection"));
            }
        }

        [TestMethod]
        [TestCategory("DefaultPage"), TestCategory("Security")]
        public async Task DefaultPageResponseMustHaveStrictTransportSecurityHeadersInCaseOfSslRequired()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment(new TestEnvironmentArgs
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
                OAuthToken token = testEnvironment.Server.Login("ValidUserName", "ValidPassword");

                HttpResponseMessage getDefaultPageResponse = await testEnvironment.Server.GetHttpClient(token)
                    .GetAsync(testEnvironment.Server.Uri);

                Assert.AreEqual(true, getDefaultPageResponse.Headers.Contains("X-Content-Type-Options"));
                Assert.AreEqual(true, getDefaultPageResponse.Headers.Contains("X-Download-Options"));
                Assert.AreEqual(true, getDefaultPageResponse.Headers.Contains("X-XSS-Protection"));
                Assert.AreEqual(true, getDefaultPageResponse.Headers.Contains("Strict-Transport-Security"));
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("DefaultPage")]
        public virtual void TestDesiredEnvironmentsConfigsArePresentInClientSide()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                OAuthToken token = testEnvironment.Server.Login("ValidUserName", "ValidPassword");

                using (RemoteWebDriver driver = testEnvironment.Server.GetWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    driver.ExecuteTest("testDesiredEnvironmentsConfigsArePresentInClientSide");
                }
            }
        }
    }
}