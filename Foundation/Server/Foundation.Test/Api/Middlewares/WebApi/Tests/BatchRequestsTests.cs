using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web.OData;
using FakeItEasy;
using Foundation.Core.Contracts;
using Foundation.Test.Api.ApiControllers;
using Foundation.Test.Core.Contracts;
using Foundation.Test.Core.Implementations;
using Foundation.Test.Model.DomainModels;
using Simple.OData.Client;
using Foundation.DataAccess.Contracts;

namespace Foundation.Test.Api.Middlewares.WebApi.Tests
{
    [TestClass]
    public class BatchRequestsTests
    {
        [Ignore]
        [TestMethod]
        [TestCategory("WebApi")]
        public virtual async Task InsertAndUpdateToghether()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment())
            {
                OAuthToken token = testEnvironment.Server.Login("ValidUserName", "ValidPassword");

                ODataBatch batchClient = testEnvironment.Server.BuildODataBatchClient(token: token);

                ODataClient client = testEnvironment.Server.BuildODataClient(token: token);

                TestModel modelBeforeInsert = new TestModel
                {
                    StringProperty = "Test",
                    Version = 1
                };

                batchClient += bc => bc.Controller<TestModelsController, TestModel>()
                    .Set(modelBeforeInsert)
                    .InsertEntryAsync();

                long modelBeforeUpdateId = await client.Controller<TestModelsController, TestModel>()
                        .Top(1)
                        .Select(t => t.Id)
                        .FindScalarAsync<long>();

                batchClient += bc => bc.Controller<TestModelsController, TestModel>()
                     .Key(modelBeforeUpdateId)
                     .Set(new { StringProperty = "Test2" })
                     .UpdateEntryAsync();

                await batchClient.ExecuteAsync();

                IRepository<TestModel> testModelsRepository =
                    TestDependencyManager.CurrentTestDependencyManager.Objects
                        .OfType<IRepository<TestModel>>()
                        .Last();

                A.CallTo(() => testModelsRepository.UpdateAsync(
                            A<TestModel>.That.Matches(testModel => testModel.StringProperty == "Test2"),
                            A<CancellationToken>.Ignored))
                    .MustHaveHappened(Repeated.Exactly.Once);

                A.CallTo(() => testModelsRepository.AddAsync(A<TestModel>.That.Matches(testModel => testModel.StringProperty == "Test"), A<CancellationToken>.Ignored))
                    .MustHaveHappened(Repeated.Exactly.Once);
            }
        }

        [Ignore]
        [TestMethod]
        [TestCategory("WebApi")]
        public virtual async Task ServerMustStopRequestExecutionOfFirstException()
        {
            // This is based on client's request.

            IEmailService emailService = A.Fake<IEmailService>();

            A.CallTo(() => emailService.SendEmail(A<string>.That.Matches(s => s == "Exception"), A<string>.Ignored, A<string>.Ignored))
                .Throws<InvalidOperationException>();

            A.CallTo(() => emailService.SendEmail(A<string>.That.Matches(s => s == "Work"), A<string>.Ignored, A<string>.Ignored))
                .DoesNothing();

            using (TestEnvironment testEnvironment = new TestEnvironment(new TestEnvironmentArgs
            {
                AdditionalDependencies = manager =>
                {
                    manager.RegisterInstance(emailService);
                }
            }))
            {
                OAuthToken token = testEnvironment.Server.Login("ValidUserName", "ValidPassword");

                ODataBatch client = testEnvironment.Server.BuildODataBatchClient(token: token, afterResponse: message =>
                {
                    Assert.AreEqual(HttpStatusCode.OK, message.StatusCode);
                    // status code of batch request will be ok all the time, it will be something other than ok,
                    // when the request itself is invalid (BadRequestException).
                });

                try
                {
                    client += c => c.Controller<TestModelsController, TestModel>()
                        .Action(nameof(TestModelsController.SendEmail))
                        .Set(new { to = "Exception", title = "Email title", message = "Email message" })
                        .ExecuteAsync();

                    client += c => c.Controller<TestModelsController, TestModel>()
                                        .Action(nameof(TestModelsController.SendEmail))
                                        .Set(new { to = "Work", title = "Email title", message = "Email message" })
                                        .ExecuteAsync();

                    await client.ExecuteAsync();

                    Assert.Fail();
                }
                catch (WebRequestException)
                {
                    A.CallTo(() => emailService.SendEmail(A<string>.That.Matches(s => s == "Exception"), A<string>.Ignored, A<string>.Ignored))
                        .MustHaveHappened();

                    A.CallTo(() => emailService.SendEmail(A<string>.That.Matches(s => s == "Work"), A<string>.Ignored, A<string>.Ignored))
                                            .MustNotHaveHappened();

                    TestModelsController controller = TestDependencyManager.CurrentTestDependencyManager.Objects
                        .OfType<TestModelsController>().Single();

                    ILogger logger = TestDependencyManager.CurrentTestDependencyManager.Objects
                        .OfType<ILogger>().Last();

                    A.CallTo(() => controller.SendEmail(A<ODataActionParameters>.Ignored))
                                            .MustHaveHappened(Repeated.Exactly.Once);

                    A.CallTo(() => logger.LogFatalAsync(A<string>.That.Matches(msg => msg == "Scope was failed")))
                                            .MustHaveHappened(Repeated.Exactly.Once);
                }
            }
        }
    }
}
