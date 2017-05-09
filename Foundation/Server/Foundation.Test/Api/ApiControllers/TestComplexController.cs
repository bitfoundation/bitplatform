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
        public virtual TestComplexDto[] Get()
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
            };
        }

        [Get]
        public virtual async Task<TestComplexDto> Get(int key, CancellationToken cancellationToken)
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
        public virtual async Task<TestComplexDto> PartialUpdate(int key, Delta<TestComplexDto> modelDelta,
            CancellationToken cancellationToken)
        {
            TestComplexDto model = modelDelta.GetInstance();

            model.ComplexObj.Name += "?";

            return model;
        }
        public class DoSomeThingWithComplexObjParameters
        {
            public TestComplexDto complexDto { get; set; }
        }

        [Action]
        [Parameter("complexDto", typeof(TestComplexDto))]
        public virtual TestComplexDto DoSomeThingWithComplexObj(DoSomeThingWithComplexObjParameters parameters)
        {
            TestComplexDto complexDto = parameters.complexDto;

            complexDto.ComplexObj.Name += "?";

            return complexDto;
        }

        [Function]
        public virtual async Task<ComplexObj2[]> GetComplexObjects()
        {
            return new[] { new ComplexObj2 { Name = "Test" } };
        }

        public class GetValuesParameters
        {
            public IEnumerable<int> values { get; set; }
        }

        [Action]
        [Parameter("values", typeof(IEnumerable<int>))]
        public virtual async Task<int[]> GetValues(GetValuesParameters parameters, CancellationToken cancellationToken)
        {
            IEnumerable<int> values = parameters.values;

            return values.Reverse().ToArray();
        }
    }
}
