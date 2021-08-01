﻿using Bit.Test;
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
        public virtual async Task TestTimeZonesInCustomActionsWithoutClientsDemand()
        {
            IValueChecker valueChecker = A.Fake<IValueChecker>();

            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs
            {
                AdditionalDependencies = (manager, services) =>
                {
                    manager.RegisterInstance(valueChecker);
                }
            }))
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                IODataClient client = testEnvironment.BuildTestODataClient(token: token);

                DateTimeOffset date = new DateTimeOffset(2016, 1, 1, 10, 30, 0, TimeSpan.Zero);

                await client.TestModels()
                    .TimeZoneTests(simpleDate: date,
                        datesArray: new[] { date, date },
                        simpleDto: new TestModel { StringProperty = " ", DateProperty = date, Id = 1, Version = 1 },
                        entitiesArray: new[]
                        {
                            new TestModel
                            {
                                StringProperty = " ", DateProperty = date, Id = 2, Version = 2
                            },
                            new TestModel
                            {
                                StringProperty = " ", DateProperty = date, Id = 3, Version = 3
                            }
                        }).ExecuteAsync();

                A.CallTo(() => valueChecker.CheckValue(A<DateTimeOffset>.That.Matches(dt => dt.Year == 2016)))
                    .MustHaveHappenedOnceExactly();

                A.CallTo(() => valueChecker.CheckValue(A<List<DateTimeOffset>>.That.Matches(dates => dates.SequenceEqual(new List<DateTimeOffset> { date, date }))))
                    .MustHaveHappenedOnceExactly();

                A.CallTo(() => valueChecker.CheckValue(A<TestModel>.That.Matches(tm => tm.DateProperty == date)))
                    .MustHaveHappenedOnceExactly();

                A.CallTo(() => valueChecker.CheckValue(A<List<TestModel>>.That.Matches(tms => tms.First().DateProperty == date && tms.Last().DateProperty == date)))
                    .MustHaveHappenedOnceExactly();
            }
        }

        [TestMethod]
        [TestCategory("WebApi")]
        public virtual async Task TestTimeZonesInCustomActionsWithClientDemand()
        {
            IValueChecker valueChecker = A.Fake<IValueChecker>();

            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs
            {
                AdditionalDependencies = (manager, services) =>
                {
                    manager.RegisterInstance(valueChecker);
                },
                ClientArgs = new TestClientArgs
                {
                    CurrentTimeZone = "Afghanistan Standard Time",
                    DesiredTimeZone = "Iran Standard Time"
                }
            }))
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                IODataClient client = testEnvironment.BuildTestODataClient(token: token);

                DateTimeOffset date = new DateTimeOffset(2016, 1, 1, 10, 30, 0, TimeSpan.Zero);

                await client.TestModels()
                    .TimeZoneTests(simpleDate: date,
                        datesArray: new[] { date, date },
                        simpleDto: new TestModel { StringProperty = " ", DateProperty = date, Id = 1, Version = 1 },
                        entitiesArray: new[]
                        {
                            new TestModel
                            {
                                StringProperty = " ", DateProperty = date, Id = 2, Version = 2
                            },
                            new TestModel
                            {
                                StringProperty = " ", DateProperty = date, Id = 3, Version = 3
                            }
                        }).ExecuteAsync();

                A.CallTo(() => valueChecker.CheckValue(A<DateTimeOffset>.That.Matches(dt => dt.Year == 2016)))
                    .MustHaveHappenedOnceExactly();

                A.CallTo(() => valueChecker.CheckValue(A<List<DateTimeOffset>>.That.Matches(dates => dates.SequenceEqual(new List<DateTimeOffset> { date.AddHours(1), date.AddHours(1) }))))
                    .MustHaveHappenedOnceExactly();

                A.CallTo(() => valueChecker.CheckValue(A<TestModel>.That.Matches(tm => tm.DateProperty == date.AddHours(1))))
                    .MustHaveHappenedOnceExactly();

                A.CallTo(() => valueChecker.CheckValue(A<List<TestModel>>.That.Matches(tms => tms.First().DateProperty == date.AddHours(1) && tms.Last().DateProperty == date.AddHours(1))))
                    .MustHaveHappenedOnceExactly();
            }
        }

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
        public virtual async Task TestTimeZonesInUrlWithClientDemand()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs
            {
                ClientArgs = new TestClientArgs
                {
                    CurrentTimeZone = "Asia/Kabul",
                    DesiredTimeZone = "Asia/Tehran"
                }
            }))
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                IODataClient client = testEnvironment.BuildTestODataClient(token: token);

                IEnumerable<TestModel> testModels = await client.TestModels()
                     .Where(tm => tm.DateProperty == new DateTimeOffset(2016, 1, 1, 9, 30, 0, TimeSpan.Zero))
                     .FindEntriesAsync();

                Assert.AreEqual(1, testModels.Count());
            }
        }
    }
}
