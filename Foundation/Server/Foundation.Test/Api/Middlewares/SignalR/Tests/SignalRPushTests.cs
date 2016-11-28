using System.Threading.Tasks;
using Foundation.Test.Model.DomainModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Simple.OData.Client;
using Foundation.Test.Api.ApiControllers;

namespace Foundation.Test.Api.Middlewares.SignalR.Tests
{
    [TestClass]
    public class SignalRPushTests
    {
        [Ignore]
        [TestMethod]
        [TestCategory("SignalR"), TestCategory("WebApi")]
        public virtual async Task PushFromSomeUserToAnotherUserUsingWebApi()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment())
            {
                OAuthToken tokenOfUser1 = testEnvironment.Server.Login("ValidUserName", "ValidPassword");

                OAuthToken tokenOfUser2 = testEnvironment.Server.Login("User2", "ValidPassword");

                ODataClient odataClientOfUser1 = testEnvironment.Server.BuildODataClient(token: tokenOfUser1);

                TaskCompletionSource<bool> onMessageRecievedCalled = new TaskCompletionSource<bool>();

                testEnvironment.Server.BuildSignalRClient(tokenOfUser2, (messageKey, messageArgs) =>
                {
                    onMessageRecievedCalled.SetResult(true);
                });

                await odataClientOfUser1.Controller<TestModelsController, TestModel>()
                    .Action(nameof(TestModelsController.PushSomeWordToAnotherUser))
                    .Set(new { to = "User2", word = "Some word" })
                    .ExecuteAsync();

                Assert.AreEqual(true, await onMessageRecievedCalled.Task);
            }
        }

        [Ignore]
        [TestMethod]
        [TestCategory("SignalR"), TestCategory("WebApi"), TestCategory("BackgroundJobs")]
        public virtual async Task PushFromSomeUserToAnotherUserUsingWebApiAndBackgroundJobWorker()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment())
            {
                OAuthToken tokenOfUser1 = testEnvironment.Server.Login("ValidUserName", "ValidPassword");

                OAuthToken tokenOfUser2 = testEnvironment.Server.Login("User2", "ValidPassword");

                ODataClient odataClientOfUser1 = testEnvironment.Server.BuildODataClient(token: tokenOfUser1);

                TaskCompletionSource<bool> onMessageRecievedCalled = new TaskCompletionSource<bool>();

                testEnvironment.Server.BuildSignalRClient(tokenOfUser2, (messageKey, messageArgs) =>
                {
                    onMessageRecievedCalled.SetResult(true);
                });

                await odataClientOfUser1.Controller<TestModelsController, TestModel>()
                    .Action(nameof(TestModelsController.PushSomeWordToAnotherUsingBackgroundJobWorker))
                    .Set(new { to = "User2", word = "Some word" })
                    .ExecuteAsync();

                Assert.AreEqual(true, await onMessageRecievedCalled.Task);
            }
        }
    }
}
