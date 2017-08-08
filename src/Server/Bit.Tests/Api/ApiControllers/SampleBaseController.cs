using Bit.OData.ODataControllers;
using Bit.Tests.Model.Dto;
using System;

namespace Bit.Tests.Api.ApiControllers
{
    public class SampleBaseController : DtoController<SampleBaseDto>
    {
        [Function]
        public virtual SampleBaseDto GetSampleDto()
        {
            return new SampleBaseDto() { };
        }
    }

    public class SampleInheritedController : DtoController<SampleInheritedDto>
    {
        [Function]
        public virtual SampleInheritedDto GetSampleDto()
        {
            SampleInheritedDto result = new SampleInheritedDto { };

            result.LastName = "1";
            result.Id = Guid.NewGuid();
            result.Name = "1";

            return result;
        }
    }
}
