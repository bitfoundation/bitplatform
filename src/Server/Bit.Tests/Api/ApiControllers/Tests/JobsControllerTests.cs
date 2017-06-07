using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Simple.OData.Client;
using Bit.Tests;
using IdentityModel.Client;
using Bit.Api.ApiControllers;
using Bit.Model.Dtos;
using Bit.Owin.Metadata;

namespace Foundation.Test.Api.ApiControllers.Tests
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
                TokenResponse token = testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientName: "TestResOwner");

                ODataClient client = testEnvironment.Server.BuildODataClient(token: token, route: "Bit");

                try
                {
                    await client.Controller<JobsInfoController, JobInfoDto>()
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
