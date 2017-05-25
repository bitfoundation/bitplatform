using System;
using Bit.Api.ApiControllers;
using Bit.Tests.Model.Dto;

namespace Bit.Tests.Api.ApiControllers
{
    public class SampleBaseController<TSampleBaseDto> : DtoController<TSampleBaseDto>
        where TSampleBaseDto : SampleBaseDto, new()
    {
        public virtual TSampleBaseDto GetSampleDto()
        {
            return new TSampleBaseDto() { Id = Guid.NewGuid(), Name = "1" };
        }
    }

    public class SampleInheritedController : SampleBaseController<SampleInheritedDto>
    {
        [Function]
        public override SampleInheritedDto GetSampleDto()
        {
            SampleInheritedDto result = base.GetSampleDto();

            result.LastName = "1";

            return result;
        }
    }
}
