using Bit.Test;
using Bit.Tests.Core.Contracts;
using Bit.Tests.Model.DomainModels;
using FakeItEasy;
using Bit.Http.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Simple.OData.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bit.Tests.Api.Middlewares.WebApi.Tests
{
    [TestClass]
    public class TimeZoneTests
    {
        [TestMethod]
        [TestCategory("WebApi")]
        public virtual async Task TestTimeZonesInUrlWithoutClientDemand()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                IODataClient client = testEnvironment.BuildTestODataClient(token: token);

                IEnumerable<TestModel> testModels = await client.TestModels()
                     .Where(tm => tm.DateProperty == new DateTimeOffset(2016, 1, 1, 10, 30, 0, TimeSpan.Zero))
                     .FindEntriesAsync();

                Assert.AreEqual(1, testModels.Count());
            }
        }

        [TestMethod]
        [TestCategory("WebApi")]
        public virtual async Task TestTimeZone()
        {
            var thrDate = new DateTimeOffset(2022, 5, 28, 10, 30, 0, TimeSpan.FromHours(4.5)).DateTime;

            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs
            {
                ClientArgs = new TestClientArgs
                {
                    CurrentTimeZone = "Asia/Ashgabat",
                    DesiredTimeZone = "Asia/Tehran"
                }
            }))
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                IODataClient client = testEnvironment.BuildTestODataClient(token: token);

                var date = await client.TestModels().GetDateTimeOffset().FindScalarAsync<DateTimeOffset>();

                Assert.AreEqual(date, new DateTimeOffset(2022, 5, 28, 5, 30, 0, TimeSpan.Zero));

                Assert.AreEqual(thrDate, date.ToOffset(TimeZoneInfo.FindSystemTimeZoneById("Asia/Ashgabat").GetUtcOffset(date)).DateTime);
            }

            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs
            {
                ClientArgs = new TestClientArgs
                {
                    CurrentTimeZone = "Asia/Istanbul",
                    DesiredTimeZone = "Asia/Tehran"
                }
            }))
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                IODataClient client = testEnvironment.BuildTestODataClient(token: token);

                var date = await client.TestModels().GetDateTimeOffset().FindScalarAsync<DateTimeOffset>();

                Assert.AreEqual(date, new DateTimeOffset(2022, 5, 28, 7, 30, 0, TimeSpan.Zero));

                Assert.AreEqual(thrDate, date.ToOffset(TimeZoneInfo.FindSystemTimeZoneById("Asia/Istanbul").GetUtcOffset(date)).DateTime);
            }

            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs
            {
                ClientArgs = new TestClientArgs
                {
                    CurrentTimeZone = "America/Vancouver",
                    DesiredTimeZone = "Asia/Tehran"
                }
            }))
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                IODataClient client = testEnvironment.BuildTestODataClient(token: token);

                var date = await client.TestModels().GetDateTimeOffset().FindScalarAsync<DateTimeOffset>();

                Assert.AreEqual(date, new DateTimeOffset(2022, 5, 28, 17, 30, 0, TimeSpan.Zero));

                Assert.AreEqual(thrDate, date.ToOffset(TimeZoneInfo.FindSystemTimeZoneById("America/Vancouver").GetUtcOffset(date)).DateTime);
            }
        }
    }
}
