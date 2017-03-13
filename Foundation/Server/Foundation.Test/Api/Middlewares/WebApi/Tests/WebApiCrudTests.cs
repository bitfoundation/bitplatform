using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.OData;
using FakeItEasy;
using Foundation.Test.Api.ApiControllers;
using Foundation.Test.Core.Contracts;
using Foundation.Test.Core.Implementations;
using Foundation.Test.Model.DomainModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Simple.OData.Client;
using Foundation.DataAccess.Contracts;

namespace Foundation.Test.Api.Middlewares.WebApi.Tests
{
    [TestClass]
    public class WebApiCrudTests
    {
        /*[Ignore]
        [TestMethod]
        [TestCategory("WebApi"), TestCategory("OData")]
        public async Task TestInsert()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment())
            {
                OAuthToken token = testEnvironment.Server.Login("ValidUserName", "ValidPassword");

                ODataClient client = testEnvironment.Server.BuildODataClient(token: token);

                TestModel modelBeforeInsert = new TestModel
                {
                    StringProperty = "Test",
                    Version = 1
                };

                TestModel modelAfterInsert = await client.Controller<TestModelsController, TestModel>()
                    .Set(modelBeforeInsert)
                    .InsertEntryAsync();

                Assert.AreNotEqual(0, modelAfterInsert.Id);

                TestModelsController testModelsController = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<TestModelsController>()
                    .Last();

                A.CallTo(() => testModelsController.Create(A<TestModel>.That.Matches(testModel => testModel.StringProperty == "Test"),
                            A<CancellationToken>.Ignored))
                    .MustHaveHappened(Repeated.Exactly.Once);

                IRepository<TestModel> testModelsRepository =
                    TestDependencyManager.CurrentTestDependencyManager.Objects
                        .OfType<IRepository<TestModel>>()
                        .Single();

                A.CallTo(() => testModelsRepository.AddAsync(A<TestModel>.That.Matches(testModel => testModel.StringProperty == "Test"), A<CancellationToken>.Ignored))
                    .MustHaveHappened(Repeated.Exactly.Once);
            }
        }

        [Ignore]
        [TestMethod]
        [TestCategory("WebApi"), TestCategory("OData")]
        public async Task TestPartialUpdate()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment())
            {
                OAuthToken token = testEnvironment.Server.Login("ValidUserName", "ValidPassword");

                ODataClient client = testEnvironment.Server.BuildODataClient(token: token);

                long modelBeforeUpdateId = await client.Controller<TestModelsController, TestModel>()
                    .Top(1)
                    .Select(t => t.Id)
                    .FindScalarAsync<long>();

                TestModel modelAfterUpdate = await client.Controller<TestModelsController, TestModel>()
                    .Key(modelBeforeUpdateId)
                    .Set(new { StringProperty = "Test2" })
                    .UpdateEntryAsync();

                Assert.AreEqual("Test2", modelAfterUpdate.StringProperty);

                TestModelsController testModelsController = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<TestModelsController>()
                    .Last();

                A.CallTo(() => testModelsController.PartialUpdate(modelBeforeUpdateId,
                    A<Delta<TestModel>>.That.Matches(
                        testModelDelta =>
                            testModelDelta.GetChangedPropertyNames().Single() == nameof(TestModel.StringProperty)),
                    A<CancellationToken>.Ignored))
                    .MustHaveHappened(Repeated.Exactly.Once);

                IRepository<TestModel> testModelsRepository =
                    TestDependencyManager.CurrentTestDependencyManager.Objects
                        .OfType<IRepository<TestModel>>()
                        .Last();

                A.CallTo(() => testModelsRepository.UpdateAsync(
                            A<TestModel>.That.Matches(testModel => testModel.StringProperty == "Test2"),
                            A<CancellationToken>.Ignored))
                    .MustHaveHappened(Repeated.Exactly.Once);
            }
        }

        [Ignore]
        [TestMethod]
        [TestCategory("WebApi"), TestCategory("OData")]
        public async Task TestDelete()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment())
            {
                OAuthToken token = testEnvironment.Server.Login("ValidUserName", "ValidPassword");

                ODataClient client = testEnvironment.Server.BuildODataClient(token: token);

                long modelIdForDelete = await client.Controller<TestModelsController, TestModel>()
                    .Top(1)
                    .Select(t => t.Id)
                    .FindScalarAsync<long>();

                await client.Controller<TestModelsController, TestModel>()
                    .Key(modelIdForDelete)
                    .DeleteEntryAsync();

                TestModelsController testModelsController = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<TestModelsController>()
                    .Last();

                A.CallTo(() => testModelsController.Delete(modelIdForDelete, A<CancellationToken>.Ignored))
                    .MustHaveHappened(Repeated.Exactly.Once);

                IRepository<TestModel> testModelsRepository =
                    TestDependencyManager.CurrentTestDependencyManager.Objects
                        .OfType<IRepository<TestModel>>()
                        .Last();

                A.CallTo(() => testModelsRepository.DeleteAsync(
                            A<TestModel>.That.Matches(testModel => testModel.Id == modelIdForDelete),
                            A<CancellationToken>.Ignored))
                    .MustHaveHappened(Repeated.Exactly.Once);
            }
        }

        [Ignore]
        [TestMethod]
        [TestCategory("WebApi"), TestCategory("OData")]
        public async Task TestSimpleFilter()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment())
            {
                OAuthToken token = testEnvironment.Server.Login("ValidUserName", "ValidPassword");

                IRequestValidator requestValidator = A.Fake<IRequestValidator>();

                ODataClient client = testEnvironment.Server.BuildODataClient(token: token, beforeRequest: message =>
                {
                    requestValidator.ValidateRequestByUri(message.RequestUri);
                });

                IEnumerable<ParentEntity> parentEntities = await client.Controller<ParentEntitiesController, ParentEntity>()
                    .Filter(p => p.Name == "A")
                    .FindEntriesAsync();

                Assert.AreEqual(1, parentEntities.Count());

                A.CallTo(() => requestValidator.ValidateRequestByUri(A<Uri>.That.Matches(uri => uri.ToString().Contains("$filter=Name eq 'A'"))))
                    .MustHaveHappened(Repeated.Exactly.Once);
            }
        }*/

        [TestMethod]
        [TestCategory("WebApi"), TestCategory("OData")]
        public async Task TestComplexProjection()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment())
            {
                OAuthToken token = testEnvironment.Server.Login("ValidUserName", "ValidPassword");

                HttpResponseMessage response = await testEnvironment.Server.GetHttpClient(token)
                            .GetAsync("/odata/Test/ChildEntities?$top=1&$select=Id,Name&$expand=ParentEntity($select=Id,Name)");

                response.EnsureSuccessStatusCode();
            }
        }
    }
}