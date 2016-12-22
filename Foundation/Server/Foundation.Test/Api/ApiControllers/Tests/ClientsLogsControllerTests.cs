using FakeItEasy;
using Foundation.Api.ApiControllers;
using Foundation.Core.Contracts;
using Foundation.Model.Dtos;
using Foundation.Test.Core.Implementations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Simple.OData.Client;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Foundation.Test.Api.ApiControllers.Tests
{
    [TestClass]
    public class ClientsLogsControllerTests
    {
        [Ignore]
        [TestMethod]
        [TestCategory("WebApi"), TestCategory("Logging")]
        public virtual async Task ClientsLogsControllerMustSaveLogsUsingLogger()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment())
            {
                OAuthToken token = testEnvironment.Server.Login("ValidUserName", "ValidPassword");

                ODataClient client = testEnvironment.Server.BuildODataClient(token: token, route: "Foundation");

                await client.Controller<ClientsLogsController, ClientLogDto>()
                    .Action(nameof(ClientsLogsController.StoreClientLogs))
                    .Set(new { clientLogs = new[] { new ClientLogDto { Message = "1", Route = "R" } } })
                    .ExecuteAsync();

                ILogger logger = TestDependencyManager.CurrentTestDependencyManager
                    .Objects.OfType<ILogger>()
                    .Last();

                A.CallTo(() => logger.LogWarningAsync("Client-Log"))
                    .MustHaveHappened(Repeated.Exactly.Once);

                A.CallTo(() => logger.AddLogData("ClientLogs", A<IEnumerable<ClientLogDto>>.That.Matches(logs => logs.Single().Message == "1")))
                    .MustHaveHappened(Repeated.Exactly.Once);
            }
        }
    }
}
