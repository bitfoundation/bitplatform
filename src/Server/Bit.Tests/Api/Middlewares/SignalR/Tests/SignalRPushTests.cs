using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Simple.OData.Client;
using Foundation.Test.Api.ApiControllers;
using Bit.Tests.Api.ApiControllers;
using Bit.Tests.Model.DomainModels;
using IdentityModel.Client;
using Bit.Tests;

namespace Foundation.Test.Api.Middlewares.SignalR.Tests
{
    [TestClass]
    public class SignalRPushTests
    {
        [TestMethod]
        [TestCategory("SignalR"), TestCategory("WebApi")]
        public virtual async Task PushFromSomeUserToAnotherUserUsingWebApi()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                TokenResponse tokenOfUser1 = testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientName: "TestResOwner");

                TokenResponse tokenOfUser2 = testEnvironment.Server.Login("User2", "ValidPassword", clientName: "TestResOwner");

                ODataClient odataClientOfUser1 = testEnvironment.Server.BuildODataClient(token: tokenOfUser1);

                TaskCompletionSource<bool> onMessageReceivedCalled = new TaskCompletionSource<bool>();

                testEnvironment.Server.BuildSignalRClient(tokenOfUser2, (messageKey, messageArgs) =>
                {
                    onMessageReceivedCalled.SetResult(true);
                });

                await odataClientOfUser1.Controller<TestModelsController, TestModel>()
                    .Action(nameof(TestModelsController.PushSomeWordToAnotherUser))
                    .Set(new { to = "User2", word = "Some word" })
                    .ExecuteAsync();

                Assert.AreEqual(true, await onMessageReceivedCalled.Task);
            }
        }

        [TestMethod]
        [TestCategory("SignalR"), TestCategory("WebApi"), TestCategory("BackgroundJobs")]
        public virtual async Task PushFromSomeUserToAnotherUserUsingWebApiAndBackgroundJobWorker()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                TokenResponse tokenOfUser1 = testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientName: "TestResOwner");

                TokenResponse tokenOfUser2 = testEnvironment.Server.Login("User2", "ValidPassword", clientName: "TestResOwner");

                ODataClient odataClientOfUser1 = testEnvironment.Server.BuildODataClient(token: tokenOfUser1);

                TaskCompletionSource<bool> onMessageReceivedCalled = new TaskCompletionSource<bool>();

                testEnvironment.Server.BuildSignalRClient(tokenOfUser2, (messageKey, messageArgs) =>
                {
                    onMessageReceivedCalled.SetResult(true);
                });

                await odataClientOfUser1.Controller<TestModelsController, TestModel>()
                    .Action(nameof(TestModelsController.PushSomeWordToAnotherUsingBackgroundJobWorker))
                    .Set(new { to = "User2", word = "Some word" })
                    .ExecuteAsync();

                Assert.AreEqual(true, await onMessageReceivedCalled.Task);
            }
        }
    }
}