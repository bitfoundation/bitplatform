using System;
using System.Net;
using IdentityModel.Client;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Tests.Api.Middlewares.SignalR.Tests
{
    [TestClass]
    public class SignalRSecurityTests
    {
        [TestMethod]
        [TestCategory("SignalR"), TestCategory("Security")]
        public virtual void OnlyLoggedInUsersCanHaveAccessToSignalR()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                TokenResponse token = testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientName: "TestResOwner");

                testEnvironment.Server.BuildSignalRClient(token);
            }
        }

        [TestMethod]
        [TestCategory("SignalR"), TestCategory("Security")]
        public virtual void NotLoggedInUsersMustNotHaveAccessToSignalR()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
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
