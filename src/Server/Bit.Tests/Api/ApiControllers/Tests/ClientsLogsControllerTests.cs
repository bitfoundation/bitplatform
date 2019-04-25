using Bit.Core.Contracts;
using Bit.Model.Dtos;
using Bit.Test.Implementations;
using FakeItEasy;
using IdentityModel.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Simple.OData.Client;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bit.Tests.Api.ApiControllers.Tests
{
    [TestClass]
    public class ClientsLogsControllerTests
    {
        [TestMethod]
        [TestCategory("WebApi"), TestCategory("Logging")]
        public virtual async Task ClientsLogsControllerMustSaveLogsUsingLogger()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                IODataClient client = testEnvironment.Server.BuildODataClient(token: token, odataRouteName: "Bit");

                await client.ClientsLogs()
                    .StoreClientLogs(new[] { new ClientLogDto { Message = "1", Route = "R" } })
                    .ExecuteAsync();

                ILogger logger = TestDependencyManager.CurrentTestDependencyManager
                    .Objects.OfType<ILogger>()
                    .Last();

                A.CallTo(() => logger.LogWarningAsync("Client-Log"))
                    .MustHaveHappenedOnceExactly();

                A.CallTo(() => logger.AddLogData("ClientLogs", A<IEnumerable<ClientLogDto>>.That.Matches(logs => logs.Single().Message == "1")))
                    .MustHaveHappenedOnceExactly();
            }
        }
    }
}
