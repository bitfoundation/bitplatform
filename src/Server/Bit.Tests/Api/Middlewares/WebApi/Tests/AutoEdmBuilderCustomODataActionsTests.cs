using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using Simple.OData.Client;
using Bit.Tests;
using IdentityModel.Client;
using Bit.Tests.Api.ApiControllers;
using Bit.Tests.Model.DomainModels;

namespace Foundation.Test.Api.Middlewares.WebApi.Tests
{
    [TestClass]
    public class AutoEdmBuilderCustomODataActionsTests
    {
        /*It's all about action arguments and methods in custom odata actions
        Arguments:
                    1- Single primitive type: TestModelsController.SendEmailUsingBackgroundJobService
                    2- Single entity: TestModelsController.StringFormattersTests
                    3- Array of entities: TestModelsController.StringFormattersTests
                    4- Array of primitives: TestModelsController.StringFormattersTests
                    5- IDictionar<string,object> : We're not going to support that for now
                    6- Single complex type: We're not going to support that for now
                    7- Array of complex values: We're not going to support that for now.
                    8- No argument: TestModelsController.PushSomethingWithDateTimeOffset

        Return values:
                    1- Single primitive value: TestModelsController.SendEmailUsingBackgroundJobServiceAndPushAfterThat
                    2- Single entity: See tests
                    3- Single complex value: We're not going to support that for now
                    4- IDictionary<string,object>: We're not going to support that for now
                    3- Array of primitive values: We're not going to support that for now
                    4- Array of entities: See tests
                    5- Array of complex values: We're not going to support that for now
                    6- Queryable of primitives: We're not going to support that for now
                    7- Queryable of entities: See tests
                    8- Queryable of complex values: We're not going to support that for now
                    9- No return value: TestModelsController.PushSomethingWithDateTimeOffset*/

        [TestMethod]
        [TestCategory("OData"), TestCategory("WebApi")]
        public virtual async Task CustomActionMethodWithSingleDtoReturnValueTest()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                TokenResponse token = testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientName: "TestResOwner");

                ODataClient client = testEnvironment.Server.BuildODataClient(token: token);

                await client.Controller<TestModelsController, TestModel>()
                    .Function(nameof(TestModelsController.CustomActionMethodWithSingleDtoReturnValueTest))
                    .FindEntryAsync();
            }
        }

        [TestMethod]
        [TestCategory("OData"), TestCategory("WebApi")]
        public virtual async Task CustomActionMethodWithArrayOfEntitiesReturnValueTest()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                TokenResponse token = testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientName: "TestResOwner");

                ODataClient client = testEnvironment.Server.BuildODataClient(token: token);

                await client.Controller<TestModelsController, TestModel>()
                    .Function(nameof(TestModelsController.CustomActionMethodWithArrayOfEntitiesReturnValueTest))
                    .FindEntriesAsync();
            }
        }

        [TestMethod]
        [TestCategory("OData"), TestCategory("WebApi")]
        public virtual async Task CustomActionMethodWithQueryableOfEntitiesReturnValueTest()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                TokenResponse token = testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientName: "TestResOwner");

                ODataClient client = testEnvironment.Server.BuildODataClient(token: token);

                await client.Controller<TestModelsController, TestModel>()
                    .Function(nameof(TestModelsController.CustomActionMethodWithQueryableOfEntitiesReturnValueTest))
                    .FindEntriesAsync();
            }
        }
    }
}