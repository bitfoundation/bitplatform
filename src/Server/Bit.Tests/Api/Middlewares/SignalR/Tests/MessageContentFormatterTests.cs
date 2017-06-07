/*using System;
using System.Linq;
using System.Threading.Tasks;
using FakeItEasy;
using Foundation.Core.Contracts;
using Foundation.Core.Models;
using Foundation.Test.Core.Implementations;
using Foundation.Test.Model.DomainModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Simple.OData.Client;
using Foundation.Test.Api.ApiControllers;

namespace Foundation.Test.Api.Middlewares.SignalR.Tests
{
    [TestClass]
    public class MessageContentFormatterTests
    {
        [Ignore]
        [TestMethod]
        [TestCategory("SignalR")]
        public virtual async Task SignalRMessageContentFormatterMustThrowAnExceptionIfMessageArgsContainsDateTimeOffset()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment())
            {
                TokenResponse token = testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientName: "TestResOwner");

                ODataClient client = testEnvironment.Server.BuildODataClient(token: token);

                try
                {
                    await client.Controller<TestModelsController, TestModel>()
                        .Action(nameof(TestModelsController.PushSomethingWithDateTimeOffset))
                        .ExecuteAsync();

                    Assert.Fail();
                }
                catch (WebRequestException)
                {
                    ILogStore logStore = TestDependencyManager.CurrentTestDependencyManager
                        .Objects.OfType<ILogStore>().Last();

                    A.CallTo(() => logStore.SaveLogAsync(A<LogEntry>.That.Matches(log =>
                                      log.LogData.Any(logData => logData.Key == "WebException" &&
                                              ((InvalidOperationException)logData.Value).Message == "You may not use date time values in task push content formatter"))))
                                              .MustHaveHappened(Repeated.Exactly.Once);
                }
            }
        }
    }
}
*/