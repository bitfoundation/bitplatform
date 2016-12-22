using Foundation.Api.ApiControllers;
using Foundation.Test.Model.Dto;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.OData;

namespace Foundation.Test.Api.ApiControllers
{
    public class TestComplexController : DtoController<TestComplexDto>
    {
        [Get]
        public virtual IQueryable<TestComplexDto> Get()
        {
            return new[]
            {
                new TestComplexDto
                {
                    EntityId = 1 , ComplexObj = new ComplexObj
                    {
                        Name = "Test1"
                    }
                },
                new TestComplexDto
                {
                    EntityId = 2 , ComplexObj = new ComplexObj
                    {
                        Name = "Test2"
                    }
                }
            }.AsQueryable();
        }

        [Get]
        public virtual async Task<TestComplexDto> Get([FromODataUri]int key, CancellationToken cancellationToken)
        {
            return Get()
                .Single(t => t.EntityId == key);
        }

        [Create]
        public virtual async Task<TestComplexDto> Create(TestComplexDto model, CancellationToken cancellationToken)
        {
            model.ComplexObj.Name += "?";
            return model;
        }

        [PartialUpdate]
        public virtual async Task<TestComplexDto> PartialUpdate([FromODataUri] int key, Delta<TestComplexDto> modelDelta,
            CancellationToken cancellationToken)
        {
            TestComplexDto model = modelDelta.GetInstance();

            model.ComplexObj.Name += "?";

            return model;
        }

        [Action]
        [Parameter("complexDto", typeof(TestComplexDto))]
        public virtual TestComplexDto DoSomeThingWithComplexObj(ODataActionParameters parameters)
        {
            TestComplexDto complexDto = (TestComplexDto)parameters["complexDto"];

            complexDto.ComplexObj.Name += "?";

            return complexDto;
        }

        [Function]
        public virtual async Task<ComplexObj2[]> GetComplexObjects()
        {
            return new[] { new ComplexObj2 { Name = "Test" } };
        }

        [Action]
        [Parameter("values", typeof(IEnumerable<int>))]
        public virtual async Task<int[]> GetValues(ODataActionParameters parameters, CancellationToken cancellationToken)
        {
            IEnumerable<int> values = (IEnumerable<int>)parameters["values"];

            return values.Reverse().ToArray();
        }
    }
}
