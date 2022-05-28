using Bit.Core.Contracts;
using Bit.Core.Exceptions;
using Bit.OData.ActionFilters;
using Bit.OData.Contracts;
using Bit.OData.ODataControllers;
using Bit.Tests.Core.Contracts;
using Bit.Tests.Data.Contracts;
using Bit.Tests.Model.DomainModels;
using Bit.Tests.Model.Dto;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Query;
using Microsoft.EntityFrameworkCore;
using Microsoft.Owin;
using Newtonsoft.Json.Converters;
using Swashbuckle.Examples;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace Bit.Tests.Api.ApiControllers
{
    public class TestModelsController : DtoController<TestModel>
    {
        public virtual Lazy<ITestModelsRepository> TestModelsRepository { get; set; }
        public virtual Lazy<IBackgroundJobWorker> BackgroundJobWorker { get; set; }
        public virtual Lazy<IMessageSender> MessageSender { get; set; }
        public virtual Lazy<IDateTimeProvider> DateTimeProvider { get; set; }

        [Get]
        [AllowAnonymous]
        public virtual async Task<IQueryable<TestModel>> Get(CancellationToken cancellationToken)
        {
            return await TestModelsRepository.Value
                .GetAllAsync(cancellationToken);
        }

        [Get]
        public virtual async Task<SingleResult<TestModel>> Get(long key, CancellationToken cancellationToken)
        {
            return SingleResult((await TestModelsRepository.Value
                .GetAllAsync(cancellationToken))
                .Where(t => t.Id == key));
        }

        [Create]
        public virtual async Task<SingleResult<TestModel>> Create(TestModel model, CancellationToken cancellationToken)
        {
            model = await TestModelsRepository.Value.AddAsync(model, cancellationToken);

            return SingleResult(model);
        }

        [PartialUpdate]
        public virtual async Task<SingleResult<TestModel>> PartialUpdate(long key, Delta<TestModel> modelDelta,
            CancellationToken cancellationToken)
        {
            TestModel model = await (await TestModelsRepository.Value.GetAllAsync(cancellationToken))
                .FirstOrDefaultAsync(m => m.Id == key, cancellationToken);

            if (model == null)
                throw new ResourceNotFoundException();

            modelDelta.Patch(model);

            model = await TestModelsRepository.Value.UpdateAsync(model, cancellationToken);

            return SingleResult(model);
        }

        [Update]
        public virtual async Task<SingleResult<TestModel>> Update(long key, TestModel model,
            CancellationToken cancellationToken)
        {
            model = await TestModelsRepository.Value.UpdateAsync(model, cancellationToken);

            if (model.Id != key)
                throw new BadRequestException();

            return SingleResult(model);
        }

        [Delete]
        public virtual async Task Delete(long key, CancellationToken cancellationToken)
        {
            TestModel model = await (await TestModelsRepository.Value.GetAllAsync(cancellationToken))
                .FirstOrDefaultAsync(m => m.Id == key, cancellationToken);

            if (model == null)
                throw new ResourceNotFoundException();

            await TestModelsRepository.Value.DeleteAsync(model, cancellationToken);
        }

        public class EmailParameters
        {
            public string title { get; set; }

            public string message { get; set; }

            public string to { get; set; }
        }

        [Action]
        public virtual async Task<Guid> SendEmailUsingBackgroundJobService(EmailParameters actionParameters)
        {
            string title = actionParameters.title;
            string message = actionParameters.message;
            string to = actionParameters.to;

            string jobId = await BackgroundJobWorker.Value
                .PerformBackgroundJobAsync<IEmailService>(emailService => emailService.SendEmail(to, title, message));

            return Guid.Parse(jobId);
        }

        [Action]
        public virtual async Task<Guid> SendEmailUsingBackgroundJobServiceAndPushAfterThat(EmailParameters actionParameters)
        {
            string title = actionParameters.title;
            string message = actionParameters.message;
            string to = actionParameters.to;

            string jobId = await BackgroundJobWorker.Value
                .PerformBackgroundJobAsync<IEmailService>(emailService => emailService.SendEmail(to, title, message));

            string secondJobId = await BackgroundJobWorker.Value
                .PerformBackgroundJobWhenAnotherJobSucceededAsync<IMessageSender>(jobId,
                    messageSender => messageSender.SendMessageToUsersAsync("OnEmailSent", new { Title = title }, new[] { to }));

            return Guid.Parse(secondJobId);
        }

        [Action]
        public virtual void SendEmail(EmailParameters actionParameters)
        {
            string title = actionParameters.title;
            string message = actionParameters.message;
            string to = actionParameters.to;

            Request.GetOwinContext().GetDependencyResolver().Resolve<IEmailService>().SendEmail(to, title, message);
        }

        [Action]
        public virtual async Task PushSomethingWithDateTimeOffset()
        {
            await MessageSender.Value.SendMessageToUsersAsync("TestTask", new { Date = DateTimeProvider.Value.GetCurrentUtcDateTime() }, new[] { "SomeUser" });
        }

        public class WordParameters
        {
            public string to { get; set; }

            public string word { get; set; }
        }

        [Action]
        public virtual async Task PushSomeWordToAnotherUser(WordParameters parameters, CancellationToken cancellationToken)
        {
            string to = parameters.to;
            string word = parameters.word;

            await MessageSender.Value.SendMessageToUsersAsync("NewWord", new { Word = word }, new[] { to });
        }

        [Action]
        public virtual async Task PushSomeWordToAnotherUsingBackgroundJobWorker(WordParameters parameters, CancellationToken cancellationToken)
        {
            string to = parameters.to;
            string word = parameters.word;

            await BackgroundJobWorker.Value.PerformBackgroundJobAsync<IMessageSender>(messageSender =>
                        messageSender.SendMessageToUsersAsync("NewWord", new { Word = word }, new[] { to }));
        }

        public class StringFormattersTestsParameters
        {
            public string simpleString { get; set; }

            public IEnumerable<string> stringsArray { get; set; }

            public IEnumerable<string> stringsArray2 { get; set; }

            public TestModel simpleDto { get; set; }

            public IEnumerable<TestModel> entitiesArray { get; set; }
        }

        [Action]
        public virtual void StringFormattersTests(StringFormattersTestsParameters actionParameters)
        {
            string simpleString = actionParameters.simpleString;
            List<string> stringsArray = actionParameters.stringsArray.ToList();
            TestModel simpleDto = actionParameters.simpleDto;
            List<TestModel> entitiesArray = actionParameters.entitiesArray.ToList();
            IValueChecker valueChecker = Request.GetOwinContext().GetDependencyResolver().Resolve<IValueChecker>();
            valueChecker.CheckValue(simpleString);
            valueChecker.CheckValue(stringsArray);
            valueChecker.CheckValue(simpleDto);
            valueChecker.CheckValue(entitiesArray);
        }

        [Function]
        public virtual async Task<SingleResult<TestModel>> CustomActionMethodWithSingleDtoReturnValueTest(CancellationToken cancellationToken)
        {
            return SingleResult(await (await TestModelsRepository.Value
                .GetAllAsync(cancellationToken))
                .FirstAsync(cancellationToken));
        }

        [Function]
        public virtual async Task<IEnumerable<TestModel>> CustomActionMethodWithArrayOfEntitiesReturnValueTest(CancellationToken cancellationToken)
        {
            return await (await TestModelsRepository.Value
                .GetAllAsync(cancellationToken))
                .ToListAsync(cancellationToken);
        }

        [Function]
        public virtual async Task<IQueryable<TestModel>> CustomActionMethodWithQueryableOfEntitiesReturnValueTest(CancellationToken cancellationToken)
        {
            return await TestModelsRepository.Value
                .GetAllAsync(cancellationToken);
        }

        [Function]
        public virtual TestModel[] GetTestModelsByStringPropertyValue(long val)
        {
            return new[]
            {
                new TestModel { Id = 1, DateProperty = DateTimeOffset.Now, StringProperty = "String1", Version = 1 },
                new TestModel { Id = 2, DateProperty = DateTimeOffset.Now, StringProperty = "String2", Version = 2 }
            };
        }

        public class FirstSecondParameters
        {
            public int firstValue { get; set; }

            public int secondValue { get; set; }
        }

        [Action]
        public virtual bool AreEqual(FirstSecondParameters parameters)
        {
            return parameters.firstValue == parameters.secondValue;
        }

        public class SumRequestExample : IExamplesProvider
        {
            public object GetExamples()
            {
                return new FirstSecondParameters { firstValue = 1, secondValue = 2 };
            }
        }

        [SwaggerRequestExample(typeof(FirstSecondParameters), typeof(SumRequestExample), jsonConverter: typeof(StringEnumConverter))]
        [Action]
        public virtual int Sum(FirstSecondParameters parameters)
        {
            return parameters.firstValue + parameters.secondValue;
        }

        [Function]
        public virtual TestModel[] GetSomeTestModelsForTest()
        {
            return new[] {
                new TestModel
                {
                    StringProperty = "Test",
                    DateProperty = new DateTimeOffset(2016, 1, 1, 10, 30, 0, TimeSpan.Zero),
                    Version = 1
                }
            };
        }

        public class TestIEEE754CompatibilityParameters
        {
            public decimal val { get; set; }
        }

        [Action]
        public virtual decimal TestIEEE754Compatibility(TestIEEE754CompatibilityParameters parameters)
        {
            decimal val = parameters.val;
            return val;
        }

        public class TestIEEE754Compatibility2Parameters
        {
            public int val { get; set; }
        }

        [Action]
        public virtual int TestIEEE754Compatibility2(TestIEEE754Compatibility2Parameters parameters)
        {
            int val = parameters.val;
            return val;
        }

        public class TestIEEE754Compatibility3Parameters
        {
            public long val { get; set; }
        }

        [Action]
        public virtual long TestIEEE754Compatibility3(TestIEEE754Compatibility3Parameters parameters)
        {
            long val = parameters.val;
            return val;
        }

        public class FirstSecondValueDecimalParameters
        {
            public decimal firstValue { get; set; }

            public decimal secondValue { get; set; }
        }

        [Action]
        public virtual decimal TestDecimalSum(FirstSecondValueDecimalParameters parameters)
        {
            return parameters.firstValue + parameters.secondValue;
        }

        public class ActionForNullArgParameters
        {
            public string nullSimpleProp { get; set; }

            public TestModel nullDto { get; set; }

            public ComplexObj nullComplex { get; set; }

            public IEnumerable<string> nullSimpleProps { get; set; }

            public IEnumerable<TestModel> nullDtos { get; set; }

            public IEnumerable<ComplexObj> nullComplexes { get; set; }

            public string notNullSimpleProp { get; set; }
        }

        [Action]
        public virtual string ActionForNullArg(ActionForNullArgParameters parameters)
        {
            if (parameters.nullDto != null)
                throw new InvalidOperationException();

            if (parameters.nullSimpleProp != null)
                throw new InvalidOperationException();

            if (parameters.nullComplex != null)
                throw new InvalidOperationException();

            if (parameters.nullDtos.Any())
                throw new InvalidOperationException();

            if (parameters.nullSimpleProps.Any())
                throw new InvalidOperationException();

            if (parameters.nullComplexes.Any())
                throw new InvalidOperationException();

            if (parameters.notNullSimpleProp == null)
                throw new InvalidOperationException();

            return null;
        }

        [Function]
        public virtual string FunctionForNullArg(string nullValue, string notNullValue)
        {
            if (nullValue != null)
                throw new InvalidOperationException();

            if (notNullValue == null)
                throw new InvalidOperationException();

            return null;
        }

        [Function]
        [IgnoreODataEnableQuery]
        public virtual IQueryable<TestModel> TestSqlBuilder(ODataQueryOptions<TestModel> odataQuery)
        {
            IDependencyResolver dependencyResolver = Request.GetOwinContext().GetDependencyResolver();
            IValueChecker valueChecker = dependencyResolver.Resolve<IValueChecker>();
            ODataSqlQueryParts sqlParts = odataQuery;
            valueChecker.CheckValue(sqlParts.WhereClause);
            valueChecker.CheckValue(sqlParts.OrderByClause);
            valueChecker.CheckValue(sqlParts.Top);
            valueChecker.CheckValue(sqlParts.Skip);
            valueChecker.CheckValue(sqlParts.Parameters.Values.ToArray());
            IODataSqlBuilder odataSqlBuilder = dependencyResolver.Resolve<IODataSqlBuilder>();
            ODataSqlQuery sql = odataSqlBuilder.BuildSqlQuery(GetODataQueryOptions(), tableName: "Test.TestModels");
            valueChecker.CheckValue(sql.SelectQuery);
            valueChecker.CheckValue(sql.SelectTotalCountQuery);
            valueChecker.CheckValue(sql.Parts.GetTotalCountFromDb);

            return new TestModel[] { }.AsQueryable();
        }

        [Function]
        public virtual string CreateODataLinkSample()
        {
            return CreateODataLink(action: "SumFunction", routeValues: new { n1 = 1, n2 = 2 });
        }

        [Function]
        public virtual int SumFunction(int n1, int n2)
        {
            return n1 + n2;
        }

        [Action]
        public virtual void JustToTestCodeGenerator(IEnumerable<TestModel> testModels)
        {

        }

        [Function]
        public virtual DateTimeOffset GetDateTimeOffset()
        {
            return new DateTimeOffset(2022, 05, 28, 06, 00, 00, TimeSpan.Zero);
        }
    }
}
