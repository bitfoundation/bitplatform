using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Bit.OData.ODataControllers;
using Bit.Data.Contracts;
using Bit.Owin.Exceptions;
using Bit.Tests.Model.DomainModels;
using Bit.Tests.Owin.Metadata;
using Microsoft.EntityFrameworkCore;

namespace Bit.Tests.Api.ApiControllers
{
    public class ParentEntitiesController : DtoController<ParentEntity>
    {
        public virtual IRepository<ParentEntity> ParentEntitiesRepository { get; set; }

        [Get]
        [AllowAnonymous]
        public virtual async Task<IQueryable<ParentEntity>> Get(CancellationToken cancellationToken)
        {
            return await ParentEntitiesRepository
                .GetAllAsync(cancellationToken);
        }

        [Get]
        public virtual async Task<SingleResult<ParentEntity>> Get(long key, CancellationToken cancellationToken)
        {
            return SingleResult((await ParentEntitiesRepository
                .GetAllAsync(cancellationToken))
                .Where(t => t.Id == key));
        }

        [Create]
        public virtual async Task<SingleResult<ParentEntity>> Create(ParentEntity model, CancellationToken cancellationToken)
        {
            model = await ParentEntitiesRepository.AddAsync(model, cancellationToken);

            if (model.Name == "KnownError")
                throw new DomainLogicException(TestMetadataBuilder.SomeError);
            else if (model.Name == "UnknowError")
                throw new InvalidOperationException("Some unknown error");

            model.Id = 999;

            return SingleResult(model);
        }

        [Function]
        public virtual ParentEntity[] GetTestData()
        {
            return new[]
            {
                new ParentEntity { Id = 1, Name = "Test", TestModel = new TestModel { Id = 1, StringProperty = "String1" } }
            };
        }
    }
}
