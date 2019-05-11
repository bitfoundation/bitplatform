using Bit.Test;
using Bit.Test.Implementations;
using Bit.Test.Server;
using Bit.Tests.Api.ApiControllers;
using Bit.Tests.Model.DomainModels;
using Bit.Tests.Model.Dto;
using FakeItEasy;
using IdentityModel.Client;
using Microsoft.AspNet.OData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using Simple.OData.Client;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
                    .MustHaveHappenedOnceExactly();
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
                    .MustHaveHappenedOnceExactly();

                A.CallTo(() => controllerForUpdate.PartialUpdate(1, A<Delta<TestComplexDto>>.That.Matches(dto => dto.GetInstance().ComplexObj.Name == "Test?"), A<CancellationToken>.Ignored))
                    .MustHaveHappenedOnceExactly();

                A.CallTo(() => controllerForAction.DoSomeThingWithComplexObj(A<TestComplexDto>.That.Matches(complexDto => complexDto.ComplexObj.Name == "Test??")))
                    .MustHaveHappenedOnceExactly();
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

                A.CallTo(() => controllerForSimpleValuesArray.GetValues(A<IEnumerable<int>>.That.Matches(values => values != null), A<CancellationToken>.Ignored))
                    .MustHaveHappenedOnceExactly();
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
                    .MustHaveHappenedOnceExactly();

                A.CallTo(() => secondCallController.GetDtoWithEnumsByGender2(TestGender2.Man))
                    .MustHaveHappenedOnceExactly();
            }
        }

        [TestMethod]
        [TestCategory("JayDataContextOData")]
        public virtual async Task EnumTest_CSClient()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { }))
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                IODataClient client = testEnvironment.Server.BuildODataClient(token: token);

                DtoWithEnum dtoWithEnum = await client.DtoWithEnum()
                    .GetDtoWithEnumsByGender(TestGender.Man)
                    .ExecuteAsSingleAsync();

                Assert.AreEqual(TestGender.Man, dtoWithEnum.Gender);

                Assert.AreEqual(true, await client.DtoWithEnum()
                    .PostDtoWithEnum(dtoWithEnum)
                    .ExecuteAsScalarAsync<bool>());

                ODataBatch batchClient = testEnvironment.Server.BuildODataBatchClient(token: token);

                batchClient += bc => bc.DtoWithEnum()
                    .GetDtoWithEnumsByGender2(TestGender2.Man)
                    .ExecuteAsEnumerableAsync();

                batchClient += bc => bc.DtoWithEnum()
                    .GetDtoWithEnumsByGender(TestGender.Man)
                    .ExecuteAsEnumerableAsync();

                await batchClient.ExecuteAsync();

                /*Assert.AreEqual(true, await client.DtoWithEnum()
                    .TestEnumsArray(new[] { TestGender2.Man, TestGender2.Woman })
                    .ExecuteAsScalarAsync<bool>());*/

                DtoWithEnumController firstCallController = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<DtoWithEnumController>()
                    .First();

                DtoWithEnumController secondCallController = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<DtoWithEnumController>()
                    .ElementAt(2);

                A.CallTo(() => firstCallController.GetDtoWithEnumsByGender(TestGender.Man))
                    .MustHaveHappenedOnceExactly();

                A.CallTo(() => secondCallController.GetDtoWithEnumsByGender2(TestGender2.Man))
                    .MustHaveHappenedOnceExactly();
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
                    .MustHaveHappenedOnceExactly();
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
                    .MustHaveHappenedOnceExactly();

                TestModelsController testModelsController = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<TestModelsController>()
                    .Single();

                A.CallTo(() => testModelsController.Get(A<CancellationToken>.Ignored))
                    .MustHaveHappenedOnceExactly();
            }
        }
    }
}
