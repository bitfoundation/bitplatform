using System;
using System.Linq;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Simple.OData.Client;
using Bit.Core.Contracts;
using Bit.Tests.Api.ApiControllers;
using Bit.Tests.Model.DomainModels;
using Bit.Tests;
using IdentityModel.Client;
using Bit.Test.Core.Implementations;
using Bit.Core.Models;

namespace Foundation.Test.Api.Middlewares.SignalR.Tests
{
    [TestClass]
    public class MessageContentFormatterTests
    {
        [TestMethod]
        [TestCategory("SignalR")]
        public virtual async Task SignalRMessageContentFormatterMustThrowAnExceptionIfMessageArgsContainsDateTimeOffset()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
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