using Bit.Core.Contracts;
using Bit.Core.Models;
using FakeItEasy;
using IdentityModel.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Simple.OData.Client;
using System.Linq;
using System.Threading.Tasks;

namespace Bit.Tests.Api.Middlewares.SignalR.Tests
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
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                IODataClient client = testEnvironment.BuildTestODataClient(token: token);

                try
                {
                    await client.TestModels()
                        .PushSomethingWithDateTimeOffset()
                        .ExecuteAsync();

                    Assert.Fail();
                }
                catch (WebRequestException)
                {
                    ILogStore logStore = testEnvironment.GetObjects<ILogStore>().Last();

                    A.CallTo(() => logStore.SaveLogAsync(A<LogEntry>.That.Matches(log =>
                                      log.LogData.Any(logData => logData.Key == "WebException" &&
                                              ((string)logData.Value).Contains("You may not use date time values in signalr content formatter")))))
                                              .MustHaveHappenedOnceExactly();
                }
            }
        }
    }
}