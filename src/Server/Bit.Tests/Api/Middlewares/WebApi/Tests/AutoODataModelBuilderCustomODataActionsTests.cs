using System.Threading.Tasks;
using Bit.Tests.Api.ApiControllers;
using Bit.Tests.Model.DomainModels;
using IdentityModel.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Simple.OData.Client;

namespace Bit.Tests.Api.Middlewares.WebApi.Tests
{
    [TestClass]
    public class AutoODataModelBuilderCustomODataActionsTests
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
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                IODataClient client = testEnvironment.Server.BuildODataClient(token: token);

                await client.TestModels()
                    .CustomActionMethodWithSingleDtoReturnValueTest()
                    .ExecuteAsSingleAsync();
            }
        }

        [TestMethod]
        [TestCategory("OData"), TestCategory("WebApi")]
        public virtual async Task CustomActionMethodWithArrayOfEntitiesReturnValueTest()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                IODataClient client = testEnvironment.Server.BuildODataClient(token: token);

                await client.TestModels()
                    .CustomActionMethodWithArrayOfEntitiesReturnValueTest()
                    .ExecuteAsEnumerableAsync();
            }
        }

        [TestMethod]
        [TestCategory("OData"), TestCategory("WebApi")]
        public virtual async Task CustomActionMethodWithQueryableOfEntitiesReturnValueTest()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                IODataClient client = testEnvironment.Server.BuildODataClient(token: token);

                await client.TestModels()
                    .CustomActionMethodWithQueryableOfEntitiesReturnValueTest()
                    .ExecuteAsEnumerableAsync();
            }
        }
    }
}