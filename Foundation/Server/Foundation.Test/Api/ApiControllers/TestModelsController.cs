using System;
using System.Linq;
using System.Threading;
using System.Web.Http;
using Foundation.Api.ApiControllers;
using Foundation.Test.Model.DomainModels;
using System.Threading.Tasks;
using System.Web.OData;
using Foundation.Api.Contracts;
using Foundation.Core.Contracts;
using Foundation.Test.Core.Contracts;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using Foundation.Api.Exceptions;
using System.Net.Http;
using Foundation.DataAccess.Contracts;
using Microsoft.Owin;

namespace Foundation.Test.Api.ApiControllers
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
        public virtual IQueryable<TestModel> Get()
        {
            return _testModelsRepository.Value
                .GetAll();
        }

        [Get]
        public virtual async Task<TestModel> Get([FromODataUri]long key, CancellationToken cancellationToken)
        {
            return await _testModelsRepository.Value
                .GetAll()
                .SingleAsync(t => t.Id == key, cancellationToken);
        }

        [Create]
        public virtual async Task<TestModel> Create(TestModel model, CancellationToken cancellationToken)
        {
            model = await _testModelsRepository.Value.AddAsync(model, cancellationToken);

            return model;
        }

        [PartialUpdate]
        public virtual async Task<TestModel> PartialUpdate([FromODataUri] long key, Delta<TestModel> modelDelta,
            CancellationToken cancellationToken)
        {
            TestModel model = await _testModelsRepository.Value.GetAll()
                .SingleOrDefaultAsync(m => m.Id == key, cancellationToken);

            if (model == null)
                throw new ResourceNotFoundaException();

            modelDelta.Patch(model);

            model = await _testModelsRepository.Value.UpdateAsync(model, cancellationToken);

            return model;
        }

        [Update]
        public virtual async Task<TestModel> Update([FromODataUri] long key, TestModel model,
            CancellationToken cancellationToken)
        {
            model = await _testModelsRepository.Value.UpdateAsync(model, cancellationToken);

            if (model.Id != key)
                throw new BadRequestException();

            return model;
        }

        [Delete]
        public virtual async Task Delete([FromODataUri] long key, CancellationToken cancellationToken)
        {
            TestModel model = await _testModelsRepository.Value.GetAll()
                .SingleOrDefaultAsync(m => m.Id == key, cancellationToken);

            if (model == null)
                throw new ResourceNotFoundaException();

            await _testModelsRepository.Value.DeleteAsync(model, cancellationToken);
        }

        [Action]
        [Parameter("title", typeof(string))]
        [Parameter("message", typeof(string))]
        [Parameter("to", typeof(string))]
        public virtual async Task<Guid> SendEmailUsingBackgroundJobService(ODataActionParameters actionParameters)
        {
            string title = (string)actionParameters["title"];
            string message = (string)actionParameters["message"];
            string to = (string)actionParameters["to"];

            string jobId = await _backgroundJobWorker.Value
                .PerformBackgroundJobAsync<IEmailService>(emailService => emailService.SendEmail(to, title, message));

            return Guid.Parse(jobId);
        }

        [Action]
        [Parameter("title", typeof(string))]
        [Parameter("message", typeof(string))]
        [Parameter("to", typeof(string))]
        public virtual async Task<Guid> SendEmailUsingBackgroundJobServiceAndPushAfterThat(ODataActionParameters actionParameters)
        {
            string title = (string)actionParameters["title"];
            string message = (string)actionParameters["message"];
            string to = (string)actionParameters["to"];

            string jobId = await _backgroundJobWorker.Value
                .PerformBackgroundJobAsync<IEmailService>(emailService => emailService.SendEmail(to, title, message));

            string secondJobId = await _backgroundJobWorker.Value
                .PerformBackgroundJobWhenAnotherJobSucceededAsync<IMessageSender>(jobId,
                    messageSender => messageSender.SendMessageToUsers("OnEmailSent", new { Title = title }, to));

            return Guid.Parse(secondJobId);
        }

        [Action]
        [Parameter("title", typeof(string))]
        [Parameter("message", typeof(string))]
        [Parameter("to", typeof(string))]
        public virtual void SendEmail(ODataActionParameters actionParameters)
        {
            string title = (string)actionParameters["title"];
            string message = (string)actionParameters["message"];
            string to = (string)actionParameters["to"];

            Request.GetOwinContext().GetDependencyResolver().Resolve<IEmailService>().SendEmail(to, title, message);
        }

        [Action]
        public virtual async Task PushSomethingWithDateTimeOffset()
        {
            await _messageSender.Value.SendMessageToUsersAsync("TestTask", new { Date = _dateTimeProvider.Value.GetCurrentUtcDateTime() }, "SomeUser");
        }

        [Action]
        [Parameter("to", typeof(string))]
        [Parameter("word", typeof(string))]
        public virtual async Task PushSomeWordToAnotherUser(ODataActionParameters parameters, CancellationToken cancellationToken)
        {
            string to = (string)parameters["to"];
            string word = (string)parameters["word"];

            await _messageSender.Value.SendMessageToUsersAsync("NewWord", new { Word = word }, to);
        }

        [Action]
        [Parameter("to", typeof(string))]
        [Parameter("word", typeof(string))]
        public virtual async Task PushSomeWordToAnotherUsingBackgroundJobWorker(ODataActionParameters parameters, CancellationToken cancellationToken)
        {
            string to = (string)parameters["to"];
            string word = (string)parameters["word"];

            await _backgroundJobWorker.Value.PerformBackgroundJobAsync<IMessageSender>(messageSender =>
                        messageSender.SendMessageToUsers("NewWord", new { Word = word }, to));
        }

        [Action]
        [Parameter("simpleString", typeof(string))]
        [Parameter("stringsArray", typeof(IEnumerable<string>))]
        [Parameter("stringsArray2", typeof(IEnumerable<string>))]
        [Parameter("simpleDto", typeof(TestModel))]
        [Parameter("entitiesArray", typeof(IEnumerable<TestModel>))]
        public virtual void StringFormattersTests(ODataActionParameters actionParameters)
        {
            string simpleString = (string)actionParameters["simpleString"];
            List<string> stringsArray = ((IEnumerable<string>)actionParameters["stringsArray"]).ToList();
            TestModel simpleDto = (TestModel)actionParameters["simpleDto"];
            List<TestModel> entitiesArray = ((IEnumerable<TestModel>)actionParameters["entitiesArray"]).ToList();
            IValueChecker valueChecker = Request.GetOwinContext().GetDependencyResolver().Resolve<IValueChecker>();
            valueChecker.CheckValue(simpleString);
            valueChecker.CheckValue(stringsArray);
            valueChecker.CheckValue(simpleDto);
            valueChecker.CheckValue(entitiesArray);
        }

        [Action]
        [Parameter("simpleDate", typeof(DateTimeOffset))]
        [Parameter("datesArray", typeof(IEnumerable<DateTimeOffset>))]
        [Parameter("simpleDto", typeof(TestModel))]
        [Parameter("entitiesArray", typeof(IEnumerable<TestModel>))]
        public virtual void TimeZoneTests(ODataActionParameters actionParameters)
        {
            DateTimeOffset simpleDate = (DateTimeOffset)actionParameters["simpleDate"];
            List<DateTimeOffset> datesArray = ((IEnumerable<DateTimeOffset>)actionParameters["datesArray"]).ToList();
            TestModel simpleDto = (TestModel)actionParameters["simpleDto"];
            List<TestModel> entitiesArray = ((IEnumerable<TestModel>)actionParameters["entitiesArray"]).ToList();
            IValueChecker valueChecker = Request.GetOwinContext().GetDependencyResolver().Resolve<IValueChecker>();
            valueChecker.CheckValue(simpleDate);
            valueChecker.CheckValue(datesArray);
            valueChecker.CheckValue(simpleDto);
            valueChecker.CheckValue(entitiesArray);
        }

        [Function]
        public virtual async Task<TestModel> CustomActionMethodWithSingleDtoReturnValueTest(CancellationToken cancellationToken)
        {
            return await _testModelsRepository.Value
                .GetAll()
                .FirstAsync(cancellationToken);
        }

        [Function]
        public virtual async Task<IEnumerable<TestModel>> CustomActionMethodWithArrayOfEntitiesReturnValueTest(CancellationToken cancellationToken)
        {
            return await _testModelsRepository.Value
                .GetAll()
                .ToListAsync(cancellationToken);
        }

        [Function]
        public virtual IQueryable<TestModel> CustomActionMethodWithQueryableOfEntitiesReturnValueTest()
        {
            return _testModelsRepository.Value
                .GetAll();
        }

        [Function]
        [Parameter("val", typeof(long))]
        public virtual IQueryable<TestModel> GetTestModelsByStringPropertyValue([FromODataUri]long val)
        {
            return new TestModel[]
            {
                new TestModel { Id = 1, DateProperty = DateTimeOffset.Now, StringProperty = "String1", Version = 1 },
                new TestModel { Id = 2, DateProperty = DateTimeOffset.Now, StringProperty = "String2", Version = 2 }
            }.AsQueryable();
        }

        [Action]
        [Parameter("firstValue", typeof(int))]
        [Parameter("secondValue", typeof(int))]
        public virtual bool AreEqual(ODataActionParameters parameters)
        {
            return (int)parameters["firstValue"] == (int)parameters["secondValue"];
        }

        [Action]
        [Parameter("firstValue", typeof(int))]
        [Parameter("secondValue", typeof(int))]
        public virtual int Sum(ODataActionParameters parameters)
        {
            return (int)parameters["firstValue"] + (int)parameters["secondValue"];
        }

        [Function]
        public virtual IQueryable<TestModel> GetSomeTestModelsForTest()
        {
            return new[] {
                new TestModel
                {
                    StringProperty = "Test",
                    DateProperty = new DateTimeOffset(2016, 1, 1, 10, 30, 0, TimeSpan.Zero),
                    Version = 1
                }
            }.AsQueryable();
        }

        [Action]
        [Parameter("val", typeof(decimal))]
        public virtual decimal TestIEEE754Compatibility(ODataActionParameters parameters)
        {
            decimal val = (decimal)parameters["val"];
            return val;
        }

        [Action]
        [Parameter("val", typeof(int))]
        public virtual int TestIEEE754Compatibility2(ODataActionParameters parameters)
        {
            int val = (int)parameters["val"];
            return val;
        }

        [Action]
        [Parameter("val", typeof(long))]
        public virtual long TestIEEE754Compatibility3(ODataActionParameters parameters)
        {
            long val = (long)parameters["val"];
            return val;
        }

        [Action]
        [Parameter("firstValue", typeof(decimal))]
        [Parameter("secondValue", typeof(decimal))]
        public virtual decimal TestDecimalSum(ODataActionParameters parameters)
        {
            return (decimal)parameters["firstValue"] + (decimal)parameters["secondValue"];
        }
    }
}
