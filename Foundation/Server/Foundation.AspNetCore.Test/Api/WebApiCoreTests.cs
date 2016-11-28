using Foundation.Test;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Foundation.AspNetCore.Test.Api
{
    public class WebApiCoreTests
    {
        [Fact]
        public virtual async Task WebApiCoreControllerShouldReturnOkStatusCode()
        {
            using (AspNetCoreTestEnvironment testEnvironment = new AspNetCoreTestEnvironment(new TestEnvironmentArgs { UseRealServer = false }))
            {
                HttpResponseMessage response = await testEnvironment.Server.GetHttpClient()
                    .GetAsync("api/People/GetData");

                response.EnsureSuccessStatusCode();

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }
    }
}
