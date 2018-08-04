using Bit.OData.ODataControllers;
using Bit.Tests.Model.Dto;
using Microsoft.AspNet.OData;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.Tests.Api.ApiControllers
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
        public virtual Task<TestComplexDto> Get(int key, CancellationToken cancellationToken)
        {
            return Task.FromResult(Get().Single(t => t.EntityId == key));
        }

        [Create]
        public virtual Task<TestComplexDto> Create(TestComplexDto model, CancellationToken cancellationToken)
        {
            model.ComplexObj.Name += "?";
            return Task.FromResult(model);
        }

        [PartialUpdate]
        public virtual Task<TestComplexDto> PartialUpdate(int key, Delta<TestComplexDto> modelDelta,
            CancellationToken cancellationToken)
        {
            TestComplexDto model = modelDelta.GetInstance();

            model.ComplexObj.Name += "?";

            return Task.FromResult(model);
        }

        public class DoSomeThingWithComplexObjParameters
        {
            public TestComplexDto complexDto { get; set; }
        }

        [Action]
        public virtual TestComplexDto DoSomeThingWithComplexObj(DoSomeThingWithComplexObjParameters parameters)
        {
            TestComplexDto complexDto = parameters.complexDto;

            complexDto.ComplexObj.Name += "?";

            return complexDto;
        }

        [Function]
        public virtual Task<ComplexObj2[]> GetComplexObjects()
        {
            return Task.FromResult(new[] { new ComplexObj2 { Name = "Test" } });
        }

        public class GetValuesParameters
        {
            public IEnumerable<int> values { get; set; }
        }

        [Action]
        public virtual Task<int[]> GetValues(GetValuesParameters parameters, CancellationToken cancellationToken)
        {
            IEnumerable<int> values = parameters.values;

            return Task.FromResult(values.Reverse().ToArray());
        }
    }
}
