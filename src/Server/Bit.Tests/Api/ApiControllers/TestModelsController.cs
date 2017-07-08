using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.OData;
using Bit.OData.ODataControllers;
using Bit.Core.Contracts;
using Bit.Data.Contracts;
using Bit.Owin.Exceptions;
using Bit.Tests.Core.Contracts;
using Bit.Tests.Model.DomainModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Owin;
using System.Web.OData.Query;
using Bit.OData.ActionFilters;
using Bit.OData.Contracts;
using Bit.Tests.Model.Dto;

namespace Bit.Tests.Api.ApiControllers
{
    public class TestModelsController : DtoController<TestModel>
    {
        private readonly Lazy<IRepository<TestModel>> _testModelsRepository;
        private readonly Lazy<IBackgroundJobWorker> _backgroundJobWorker;
        private readonly Lazy<IMessageSender> _messageSender;
        private readonly Lazy<IDateTimeProvider> _dateTimeProvider;

        public TestModelsController(Lazy<IRepository<TestModel>> testModelsRepository, Lazy<IBackgroundJobWorker> backgroundJobWorker, Lazy<IMessageSender> messageSender, Lazy<IDateTimeProvider> dateTimeProvider)
        {
            if (testModelsRepository == null)
                throw new ArgumentNullException(nameof(testModelsRepository));

            _testModelsRepository = testModelsRepository;

            if (backgroundJobWorker == null)
                throw new ArgumentNullException(nameof(backgroundJobWorker));

            _backgroundJobWorker = backgroundJobWorker;

            if (messageSender == null)
                throw new ArgumentNullException(nameof(messageSender));

            _messageSender = messageSender;

            if (dateTimeProvider == null)
                throw new ArgumentNullException(nameof(dateTimeProvider));

            _dateTimeProvider = dateTimeProvider;
        }

        protected TestModelsController()
        {

        }

        [Get]
        [AllowAnonymous]
        public virtual async Task<IQueryable<TestModel>> Get(CancellationToken cancellationToken)
        {
            return await _testModelsRepository.Value
                .GetAllAsync(cancellationToken);
        }

        [Get]
        public virtual async Task<TestModel> Get(long key, CancellationToken cancellationToken)
        {
            TestModel testModel = await (await _testModelsRepository.Value
                .GetAllAsync(cancellationToken))
                .FirstOrDefaultAsync(t => t.Id == key, cancellationToken);

            if (testModel == null)
                throw new ResourceNotFoundException();

            return testModel;
        }

        [Create]
        public virtual async Task<TestModel> Create(TestModel model, CancellationToken cancellationToken)
        {
            model = await _testModelsRepository.Value.AddAsync(model, cancellationToken);

            return model;
        }

        [PartialUpdate]
        public virtual async Task<TestModel> PartialUpdate(long key, Delta<TestModel> modelDelta,
            CancellationToken cancellationToken)
        {
            TestModel model = await (await _testModelsRepository.Value.GetAllAsync(cancellationToken))
                .FirstOrDefaultAsync(m => m.Id == key, cancellationToken);

            if (model == null)
                throw new ResourceNotFoundException();

            modelDelta.Patch(model);

            model = await _testModelsRepository.Value.UpdateAsync(model, cancellationToken);

            return model;
        }

        [Update]
        public virtual async Task<TestModel> Update(long key, TestModel model,
            CancellationToken cancellationToken)
        {
            model = await _testModelsRepository.Value.UpdateAsync(model, cancellationToken);

            if (model.Id != key)
                throw new BadRequestException();

            return model;
        }

        [Delete]
        public virtual async Task Delete(long key, CancellationToken cancellationToken)
        {
            TestModel model = await (await _testModelsRepository.Value.GetAllAsync(cancellationToken))
                .FirstOrDefaultAsync(m => m.Id == key, cancellationToken);

            if (model == null)
                throw new ResourceNotFoundException();

            await _testModelsRepository.Value.DeleteAsync(model, cancellationToken);
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

            string jobId = await _backgroundJobWorker.Value
                .PerformBackgroundJobAsync<IEmailService>(emailService => emailService.SendEmail(to, title, message));

            return Guid.Parse(jobId);
        }

        [Action]
        public virtual async Task<Guid> SendEmailUsingBackgroundJobServiceAndPushAfterThat(EmailParameters actionParameters)
        {
            string title = actionParameters.title;
            string message = actionParameters.message;
            string to = actionParameters.to;

            string jobId = await _backgroundJobWorker.Value
                .PerformBackgroundJobAsync<IEmailService>(emailService => emailService.SendEmail(to, title, message));

            string secondJobId = await _backgroundJobWorker.Value
                .PerformBackgroundJobWhenAnotherJobSucceededAsync<IMessageSender>(jobId,
                    messageSender => messageSender.SendMessageToUsers("OnEmailSent", new { Title = title }, new[] { to }));

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
            await _messageSender.Value.SendMessageToUsersAsync("TestTask", new { Date = _dateTimeProvider.Value.GetCurrentUtcDateTime() }, new[] { "SomeUser" });
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

            await _messageSender.Value.SendMessageToUsersAsync("NewWord", new { Word = word }, new[] { to });
        }

        [Action]
        public virtual async Task PushSomeWordToAnotherUsingBackgroundJobWorker(WordParameters parameters, CancellationToken cancellationToken)
        {
            string to = parameters.to;
            string word = parameters.word;

            await _backgroundJobWorker.Value.PerformBackgroundJobAsync<IMessageSender>(messageSender =>
                        messageSender.SendMessageToUsers("NewWord", new { Word = word }, new[] { to }));
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

        public class TimeZoneTestsParameters
        {
            public DateTimeOffset simpleDate { get; set; }

            public IEnumerable<DateTimeOffset> datesArray { get; set; }

            public TestModel simpleDto { get; set; }

            public IEnumerable<TestModel> entitiesArray { get; set; }
        }

        [Action]
        public virtual void TimeZoneTests(TimeZoneTestsParameters actionParameters)
        {
            DateTimeOffset simpleDate = actionParameters.simpleDate;
            List<DateTimeOffset> datesArray = actionParameters.datesArray.ToList();
            TestModel simpleDto = actionParameters.simpleDto;
            List<TestModel> entitiesArray = actionParameters.entitiesArray.ToList();
            IValueChecker valueChecker = Request.GetOwinContext().GetDependencyResolver().Resolve<IValueChecker>();
            valueChecker.CheckValue(simpleDate);
            valueChecker.CheckValue(datesArray);
            valueChecker.CheckValue(simpleDto);
            valueChecker.CheckValue(entitiesArray);
        }

        [Function]
        public virtual async Task<TestModel> CustomActionMethodWithSingleDtoReturnValueTest(CancellationToken cancellationToken)
        {
            return await (await _testModelsRepository.Value
                .GetAllAsync(cancellationToken))
                .FirstAsync(cancellationToken);
        }

        [Function]
        public virtual async Task<IEnumerable<TestModel>> CustomActionMethodWithArrayOfEntitiesReturnValueTest(CancellationToken cancellationToken)
        {
            return await (await _testModelsRepository.Value
                .GetAllAsync(cancellationToken))
                .ToListAsync(cancellationToken);
        }

        [Function]
        public virtual async Task<IQueryable<TestModel>> CustomActionMethodWithQueryableOfEntitiesReturnValueTest(CancellationToken cancellationToken)
        {
            return await _testModelsRepository.Value
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
            IODataSqlBuilder odataSqlBuilder = dependencyResolver.Resolve<IODataSqlBuilder>();
            var sqlParts = odataSqlBuilder.BuildSqlQueryParts(odataQuery);
            valueChecker.CheckValue(sqlParts.WhereClause);
            valueChecker.CheckValue(sqlParts.OrderByClause);
            valueChecker.CheckValue(sqlParts.Top);
            valueChecker.CheckValue(sqlParts.Skip);
            valueChecker.CheckValue(sqlParts.Parameters.Values.ToArray());
            var sql = odataSqlBuilder.BuildSqlQuery(odataQuery, tableName: "Test.TestModels");
            valueChecker.CheckValue(sql.SelectQuery);
            valueChecker.CheckValue(sql.SelectTotalCountQuery);
            valueChecker.CheckValue(sql.Parts.GetTotalCountFromDb);

            return new TestModel[] { }.AsQueryable();
        }
    }
}
