﻿using Bit.Http.Contracts;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Threading.Tasks;

namespace Bit.Tests.Api.Middlewares.SignalR.Tests
{
    [TestClass]
    public class SignalRSecurityTests
    {
        [TestMethod]
        [TestCategory("SignalR"), TestCategory("Security")]
        public virtual async Task OnlyLoggedInUsersCanHaveAccessToSignalR()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword");

                await testEnvironment.Server.BuildSignalRClient(token);
            }
        }

        [TestMethod]
        [TestCategory("SignalR"), TestCategory("Security")]
        public virtual async Task NotLoggedInUsersMustNotHaveAccessToSignalR()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                try
                {
                    await testEnvironment.Server.BuildSignalRClient();
                    Assert.Fail();
                }
                catch (HttpClientException ex)
                {
                    Assert.AreEqual(HttpStatusCode.Unauthorized, ex.Response?.StatusCode);
                }
            }
        }
    }
}
