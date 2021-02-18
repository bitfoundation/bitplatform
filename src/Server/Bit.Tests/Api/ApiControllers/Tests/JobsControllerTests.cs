using Bit.Owin.Metadata;
using Bit.Http.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Simple.OData.Client;
using System.Threading.Tasks;

namespace Bit.Tests.Api.ApiControllers.Tests
{
    [TestClass]
    public class JobsControllerTests
    {
        [TestMethod]
        [TestCategory("WebApi"), TestCategory("BackgroundJobs")]
        public virtual async Task JobsControllerMustThrowAnExceptionWhenJobIsNotPresents()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                IODataClient client = testEnvironment.BuildBitODataClient(token: token);

                try
                {
                    await client.JobsInfo()
                        .Key("1")
                        .FindEntryAsync();

                    Assert.Fail();
                }
                catch (WebRequestException ex)
                {
                    Assert.IsTrue(ex.Response.Contains(BitMetadataBuilder.JobNotFound));
                }
            }
        }
    }
}
