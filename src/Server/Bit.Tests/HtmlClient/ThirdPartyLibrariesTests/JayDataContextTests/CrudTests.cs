using Bit.Test;
using Bit.Test.Core.Implementations;
using Bit.Test.Server;
using Bit.Tests.Api.ApiControllers;
using Bit.Tests.Model.DomainModels;
using Bit.Tests.Model.Dto;
using FakeItEasy;
using IdentityModel.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using Simple.OData.Client;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.OData;

namespace Bit.Tests.HtmlClient.ThirdPartyLibrariesTests.JayDataContextTests
{
    [TestClass]
    public class CrudTests
    {
        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("JayDataContextOData")]
        public virtual async Task TestInsert()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (RemoteWebDriver driver = testEnvironment.Server.BuildWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    await driver.ExecuteTest("testInsert");
                }

                TestModelsController testModelsController = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<TestModelsController>()
                    .Single();

                A.CallTo(() => testModelsController.Create(A<TestModel>.That.Matches(m => m.StringProperty == "Test"), A<CancellationToken>.Ignored))
                    .MustHaveHappened(Repeated.Exactly.Once);
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("JayDataContextOData")]
        public virtual async Task TestComplexTypeWithOData()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (RemoteWebDriver driver = testEnvironment.Server.BuildWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    await driver.ExecuteTest("testComplexTypeWithOData");
                }

                TestComplexController controllerForInsert = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<TestComplexController>()
                    .ElementAt(0);

                TestComplexController controllerForUpdate = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<TestComplexController>()
                    .ElementAt(2);

                TestComplexController controllerForAction = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<TestComplexController>()
                    .ElementAt(3);

                A.CallTo(() => controllerForInsert.Create(A<TestComplexDto>.That.Matches(dto => dto.ComplexObj.Name == "Test?"), A<CancellationToken>.Ignored))
                    .MustHaveHappened(Repeated.Exactly.Once);

                A.CallTo(() => controllerForUpdate.PartialUpdate(1, A<Delta<TestComplexDto>>.That.Matches(dto => dto.GetInstance().ComplexObj.Name == "Test?"), A<CancellationToken>.Ignored))
                    .MustHaveHappened(Repeated.Exactly.Once);

                A.CallTo(() => controllerForAction.DoSomeThingWithComplexObj(A<TestComplexController.DoSomeThingWithComplexObjParameters>.That.Matches(parameters => parameters.complexDto.ComplexObj.Name == "Test??")))
                    .MustHaveHappened(Repeated.Exactly.Once);
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("JayDataContextOData")]
        public virtual async Task SimpleArrayValuesTest()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (RemoteWebDriver driver = testEnvironment.Server.BuildWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    await driver.ExecuteTest("simpleArrayValuesTest");
                }

                TestComplexController controllerForSimpleValuesArray = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<TestComplexController>()
                    .Single();

                A.CallTo(() => controllerForSimpleValuesArray.GetValues(A<TestComplexController.GetValuesParameters>.That.Matches(parameters => parameters.values != null), A<CancellationToken>.Ignored))
                    .MustHaveHappened(Repeated.Exactly.Once);
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("JayDataContextOData")]
        public virtual async Task EnumTest_HtmlClient()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (RemoteWebDriver driver = testEnvironment.Server.BuildWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    await driver.ExecuteTest("enumTest");
                }

                DtoWithEnumController firstCallController = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<DtoWithEnumController>()
                    .First();

                DtoWithEnumController secondCallController = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<DtoWithEnumController>()
                    .ElementAt(1);

                A.CallTo(() => firstCallController.GetDtoWithEnumsByGender(TestGender.Man))
                    .MustHaveHappened(Repeated.Exactly.Once);

                A.CallTo(() => secondCallController.GetDtoWithEnumsByGender2(TestGender2.Man))
                    .MustHaveHappened(Repeated.Exactly.Once);
            }
        }

        [TestMethod]
        [TestCategory("JayDataContextOData")]
        public virtual async Task EnumTest_CSClient()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { }))
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                ODataClient client = testEnvironment.Server.BuildODataClient(token: token);

                DtoWithEnum dtoWithEnum = await client.Controller<DtoWithEnumController, DtoWithEnum>()
                    .Function(nameof(DtoWithEnumController.GetDtoWithEnumsByGender))
                    .Set(new { gender = TestGender.Man })
                    .ExecuteAsSingleAsync();

                Assert.AreEqual(TestGender.Man, dtoWithEnum.Gender);

                Assert.AreEqual(true, await client.Controller<DtoWithEnumController, DtoWithEnum>()
                    .Action(nameof(DtoWithEnumController.PostDtoWithEnum))
                    .Set(new DtoWithEnumController.PostDtoWithEnumParameters { dto = dtoWithEnum })
                    .ExecuteAsScalarAsync<bool>());

                ODataBatch batchClient = testEnvironment.Server.BuildODataBatchClient(token: token);

                batchClient += bc => bc.Controller<DtoWithEnumController, DtoWithEnum>()
                    .Function(nameof(DtoWithEnumController.GetDtoWithEnumsByGender2))
                    .Set(new { gender = TestGender2.Man })
                    .FindEntriesAsync();

                batchClient += bc => bc.Controller<DtoWithEnumController, DtoWithEnum>()
                    .Function(nameof(DtoWithEnumController.GetDtoWithEnumsByGender))
                    .Set(new { gender = TestGender.Man })
                    .FindEntriesAsync();

                await batchClient.ExecuteAsync();

                /*Assert.AreEqual(true, await client.Controller<DtoWithEnumController, DtoWithEnum>()
                    .Action(nameof(DtoWithEnumController.TestEnumsArray))
                    .Set(new DtoWithEnumController.TestEnumsArrayParameters { enums = new[] { TestGender2.Man, TestGender2.Woman } })
                    .ExecuteAsScalarAsync<bool>());*/

                DtoWithEnumController firstCallController = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<DtoWithEnumController>()
                    .First();

                DtoWithEnumController secondCallController = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<DtoWithEnumController>()
                    .ElementAt(2);

                A.CallTo(() => firstCallController.GetDtoWithEnumsByGender(TestGender.Man))
                    .MustHaveHappened(Repeated.Exactly.Once);

                A.CallTo(() => secondCallController.GetDtoWithEnumsByGender2(TestGender2.Man))
                    .MustHaveHappened(Repeated.Exactly.Once);
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("JayDataContextOData")]
        public virtual async Task TestComplexTypeWithOfflineDb()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (RemoteWebDriver driver = testEnvironment.Server.BuildWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    await driver.ExecuteTest("testComplexTypeWithOfflineDb");
                }
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("JayDataContextOData")]
        public virtual async Task TestGetAllAndFilter()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (RemoteWebDriver driver = testEnvironment.Server.BuildWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    await driver.ExecuteTest(@"testGetAllAndFilter");
                }

                ParentEntitiesController parentEntitiesController = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<ParentEntitiesController>()
                    .Single();

                A.CallTo(() => parentEntitiesController.Get(A<CancellationToken>.Ignored))
                    .MustHaveHappened(Repeated.Exactly.Once);
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("JayDataContextOData")]
        public virtual async Task TestBatchReadRequest()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (RemoteWebDriver driver = testEnvironment.Server.BuildWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    await driver.ExecuteTest(@"testBatchReadRequest");
                }

                ParentEntitiesController parentEntitiesController = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<ParentEntitiesController>()
                    .Single();

                A.CallTo(() => parentEntitiesController.Get(A<CancellationToken>.Ignored))
                    .MustHaveHappened(Repeated.Exactly.Once);

                TestModelsController testModelsController = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<TestModelsController>()
                    .Single();

                A.CallTo(() => testModelsController.Get(A<CancellationToken>.Ignored))
                    .MustHaveHappened(Repeated.Exactly.Once);
            }
        }
    }
}
