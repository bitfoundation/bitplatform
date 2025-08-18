﻿using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.Test;
using Bit.Tests.Api.ApiControllers;
using Bit.Tests.Core.Contracts;
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
    public class LogOperationInfoFilterAttributeTests
    {
        [TestMethod]
        [TestCategory("WebApi"), TestCategory("Logging")]
        public virtual async Task LogOperationArgsFilterAttributeShouldLogOperationArgs()
        {
            IEmailService emailService = A.Fake<IEmailService>();

            A.CallTo(() => emailService.SendEmail(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Throws<InvalidOperationException>();

            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs
            {
                AdditionalDependencies = (manager, services) =>
                {
                    manager.RegisterInstance(emailService);
                }
            }))
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword");

                IODataClient client = testEnvironment.BuildTestODataClient(token: token);

                try
                {
                    await client.TestModels()
                        .SendEmail(to: "Someone", title: "Email title", message: "Email message")
                        .ExecuteAsync();

                    Assert.Fail();
                }
                catch (WebRequestException)
                {
                    ILogStore logStore = testEnvironment.GetObjects<ILogStore>().Last();

                    A.CallTo(() => logStore.SaveLogAsync(A<LogEntry>.That.Matches(log => ((TestModelsController.EmailParameters)((KeyValuePair<string, object>[])log.LogData.Single(ld => ld.Key == "OperationArgs").Value)[0].Value).to == "Someone")))
                        .MustHaveHappenedOnceExactly();
                    A.CallTo(() => logStore.SaveLogAsync(A<LogEntry>.That.Matches(log => ((string)log.LogData.Single(ld => ld.Key == "OperationName").Value) == "SendEmail")))
                        .MustHaveHappenedOnceExactly();
                    A.CallTo(() => logStore.SaveLogAsync(A<LogEntry>.That.Matches(log => ((string)log.LogData.Single(ld => ld.Key == "ControllerName").Value) == "TestModels")))
                        .MustHaveHappenedOnceExactly();
                }
            }
        }
    }
}