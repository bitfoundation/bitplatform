using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.OData;
using FakeItEasy;
using Foundation.Core.Contracts;
using Foundation.Core.Models;
using Foundation.Test.Core.Contracts;
using Foundation.Test.Core.Implementations;
using Foundation.Test.Model.DomainModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Simple.OData.Client;
using Foundation.Test.Api.ApiControllers;

namespace Foundation.Test.Api.Middlewares.WebApi.Tests
{
    [TestClass]
    public class LogActionArgsFilterAttributeTests
    {
        [Ignore]
        [TestMethod]
        [TestCategory("WebApi"), TestCategory("Logging")]
        public virtual async Task LogActionArgsFilterAttributeShouldLogActionArgs()
        {
            IEmailService emailService = A.Fake<IEmailService>();

            A.CallTo(() => emailService.SendEmail(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Throws<InvalidOperationException>();

            using (TestEnvironment testEnvironment = new TestEnvironment(new TestEnvironmentArgs
            {
                AdditionalDependencies = manager =>
                {
                    manager.RegisterInstance(emailService);
                }
            }))
            {
                OAuthToken token = testEnvironment.Server.Login("ValidUserName", "ValidPassword");

                ODataClient client = testEnvironment.Server.BuildODataClient(token: token);

                try
                {
                    await client.Controller<TestModelsController, TestModel>()
                        .Action(nameof(TestModelsController.SendEmail))
                        .Set(new { to = "Someone", title = "Email title", message = "Email message" })
                        .ExecuteAsync();

                    Assert.Fail();
                }
                catch (WebRequestException)
                {
                    ILogStore logStore = TestDependencyManager.CurrentTestDependencyManager.Objects
                        .OfType<ILogStore>().Last();

                    A.CallTo(() => logStore.SaveLogAsync(A<LogEntry>.That.Matches(log => (string)((ODataActionParameters)((KeyValuePair<string, object>[])log.LogData.Single(ld => ld.Key == "ActionArgs").Value)[0].Value)["to"] == "Someone")))
                        .MustHaveHappened(Repeated.Exactly.Once);
                }
            }
        }
    }
}
