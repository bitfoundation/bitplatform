using Bit.OData.ODataControllers;
using Bit.Tests.Model.Dto;
using Microsoft.AspNet.OData;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

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
        public virtual async Task<SingleResult<TestComplexDto>> Get(int key, CancellationToken cancellationToken)
        {
            return SingleResult(Get()
                .Where(t => t.EntityId == key));
        }

        [Create]
        public virtual async Task<SingleResult<TestComplexDto>> Create(TestComplexDto model, CancellationToken cancellationToken)
        {
            model.ComplexObj.Name += "?";
            return SingleResult(model);
        }

        [PartialUpdate]
        public virtual async Task<SingleResult<TestComplexDto>> PartialUpdate(int key, Delta<TestComplexDto> modelDelta,
            CancellationToken cancellationToken)
        {
            TestComplexDto model = modelDelta.GetInstance();

            model.ComplexObj.Name += "?";

            return SingleResult(model);
        }

        [Action]
        public virtual SingleResult<TestComplexDto> DoSomeThingWithComplexObj(TestComplexDto complexDto)
        {
            complexDto.ComplexObj.Name += "?";

            return SingleResult(complexDto);
        }

        [Function]
        public virtual async Task<ComplexObj2[]> GetComplexObjects()
        {
            return new[] { new ComplexObj2 { Name = "Test" } };
        }

        [Function]
        public virtual TestComplexDto[] GetObjectsWithNullCompexTypes()
        {
            return new[]
            {
                new TestComplexDto { EntityId = 1, ComplexObj = null }
            };
        }

        [Action]
        public virtual async Task<int[]> GetValues(IEnumerable<int> values, CancellationToken cancellationToken)
        {
            return values.Reverse().ToArray();
        }
    }
}
