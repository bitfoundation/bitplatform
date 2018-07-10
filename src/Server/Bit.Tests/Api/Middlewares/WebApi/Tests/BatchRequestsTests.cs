using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Bit.Core.Contracts;
using Bit.Data.Contracts;
using Bit.Test;
using Bit.Test.Core.Implementations;
using Bit.Tests.Api.ApiControllers;
using Bit.Tests.Core.Contracts;
using Bit.Tests.Model.DomainModels;
using FakeItEasy;
using IdentityModel.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Simple.OData.Client;

namespace Bit.Tests.Api.Middlewares.WebApi.Tests
{
    [TestClass]
    public class BatchRequestsTests
    {
        [TestMethod]
        [TestCategory("WebApi")]
        public virtual async Task InsertAndUpdateTogether()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                ODataBatch batchClient = testEnvironment.Server.BuildODataBatchClient(token: token);

                IODataClient client = testEnvironment.Server.BuildODataClient(token: token);

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

        [TestMethod]
        [TestCategory("WebApi")]
        public virtual async Task ServerMustStopRequestExecutionOnFirstException()
        {
            // This is based on client's request.

            IEmailService emailService = A.Fake<IEmailService>();

            A.CallTo(() => emailService.SendEmail(A<string>.That.Matches(s => s == "Exception"), A<string>.Ignored, A<string>.Ignored))
                .Throws<InvalidOperationException>();

            A.CallTo(() => emailService.SendEmail(A<string>.That.Matches(s => s == "Work"), A<string>.Ignored, A<string>.Ignored))
                .DoesNothing();

            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs
            {
                AdditionalDependencies = (manager, services) =>
                {
                    manager.RegisterInstance(emailService);
                }
            }))
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

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
                        .Set(new TestModelsController.EmailParameters { to = "Exception", title = "Email title", message = "Email message" })
                        .ExecuteAsync();

                    client += c => c.Controller<TestModelsController, TestModel>()
                                        .Action(nameof(TestModelsController.SendEmail))
                                        .Set(new TestModelsController.EmailParameters { to = "Work", title = "Email title", message = "Email message" })
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

                    A.CallTo(() => controller.SendEmail(A<TestModelsController.EmailParameters>.Ignored))
                                            .MustHaveHappened(Repeated.Exactly.Once);

                    A.CallTo(() => logger.LogFatalAsync(A<string>.That.Matches(msg => msg == "Scope was failed: Operation is not valid due to the current state of the object.")))
                                            .MustHaveHappened(Repeated.Exactly.Once);
                }
            }
        }
    }
}
