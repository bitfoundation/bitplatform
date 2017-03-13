/*using System.Threading.Tasks;
using Foundation.Api.Metadata;
using Foundation.Model.DomainModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Simple.OData.Client;
using Foundation.Api.ApiControllers;

namespace Foundation.Test.Api.ApiControllers.Tests
{
    [TestClass]
    public class JobsControllerTests
    {
        [Ignore]
        [TestMethod]
        [TestCategory("WebApi"), TestCategory("BackgroundJobs")]
        public virtual async Task JobsControllerMustThrowAnExceptionWhenJobIsNotPresents()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment())
            {
                OAuthToken token = testEnvironment.Server.Login("ValidUserName", "ValidPassword");

                ODataClient client = testEnvironment.Server.BuildODataClient(token: token, route: "Foundation");

                try
                {
                    await client.Controller<JobsInfoController, JobInfo>()
                        .Key("1")
                        .FindEntryAsync();

                    Assert.Fail();
                }
                catch (WebRequestException ex)
                {
                    Assert.IsTrue(ex.Response.Contains(FoundationMetadataBuilder.JobNotFound));
                }
            }
        }
    }
}
*/