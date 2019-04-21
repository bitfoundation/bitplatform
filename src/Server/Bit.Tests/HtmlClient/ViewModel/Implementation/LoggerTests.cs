using System.Linq;
using Bit.OData.ODataControllers;
using Bit.Model.Dtos;
using Bit.Test;
using Bit.Test.Implementations;
using Bit.Test.Server;
using FakeItEasy;
using IdentityModel.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using System.Threading.Tasks;

namespace Bit.Tests.HtmlClient.ViewModel.Implementation
{
    [TestClass]
    public class LoggerTests
    {
        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("Logging")]
        public virtual async Task LogException()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                try
                {
                    using (RemoteWebDriver driver = testEnvironment.Server.BuildWebDriver(new RemoteWebDriverOptions { Token = token }))
                    {
                        try
                        {
                            await driver.ExecuteTest("LoggerTests.logException");
                        }
                        catch { }
                    }

                    Assert.Fail();
                }
                catch
                {
                    ClientsLogsController clientsLogsController = TestDependencyManager.CurrentTestDependencyManager
                        .Objects.OfType<ClientsLogsController>().Single();

                    A.CallTo(() => clientsLogsController.Create(A<ClientLogDto>.That.Matches(cl => cl.ErrorName == "TypeError")))
                        .MustHaveHappenedOnceExactly();
                }
            }
        }
    }
}