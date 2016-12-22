using System.Net;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Foundation.Test.Api.Middlewares.SignalR.Tests
{
    [TestClass]
    public class SignalRSecurityTests
    {
        [TestMethod]
        [TestCategory("SignalR"), TestCategory("Security")]
        public virtual void OnlyLoggedInUsersCanHaveAccessToSignalR()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment())
            {
                OAuthToken token = testEnvironment.Server.Login("ValidUserName", "ValidPassword");

                testEnvironment.Server.BuildSignalRClient(token);
            }
        }

        [TestMethod]
        [TestCategory("SignalR"), TestCategory("Security")]
        public virtual void NotLoggedInUsersMustNotHaveAccessToSignalR()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment())
            {
                try
                {
                    testEnvironment.Server.BuildSignalRClient();
                    Assert.Fail();
                }
                catch (AggregateException ex)
                {
                    Assert.AreEqual(HttpStatusCode.Unauthorized, ((HttpClientException)ex.InnerException)?.Response?.StatusCode);
                }
            }
        }
    }
}
