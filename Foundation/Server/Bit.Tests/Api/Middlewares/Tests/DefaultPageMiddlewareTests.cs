using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Bit.Core.Models;
using Bit.Test;
using Bit.Test.Server;
using IdentityModel.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;

namespace Bit.Tests.Api.Middlewares.Tests
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
                TokenResponse token = testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientName: "TestResOwner");

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
                TokenResponse token = testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientName: "TestResOwner");

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
                TokenResponse token = testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientName: "TestResOwner");

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

                    environment.Configs.Find(c => c.Key == "ClientHostBaseUri").Value = "https://127.0.0.1";
                    environment.Security.SSOServerUrl = "https://127.0.0.1/core";
                }
            }))
            {
                TokenResponse token = testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientName: "TestResOwner");

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
                TokenResponse token = testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientName: "TestResOwner");

                using (RemoteWebDriver driver = testEnvironment.Server.GetWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    driver.ExecuteTest("testDesiredEnvironmentsConfigsArePresentInClientSide");
                }
            }
        }
    }
}