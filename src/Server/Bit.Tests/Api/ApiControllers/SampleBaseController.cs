using Bit.OData.ODataControllers;
using Bit.Tests.Model.Dto;
using System;
using System.Web.Http;

namespace Bit.Tests.Api.ApiControllers
{
    public class SampleBaseController : DtoController<SampleBaseDto>
    {
        [Function]
        public virtual SingleResult<SampleBaseDto> GetSampleDto()
        {
            return SingleResult(new SampleBaseDto() { });
        }
    }

    public class SampleInheritedController : DtoController<SampleInheritedDto>
    {
        [Function]
        public virtual SingleResult<SampleInheritedDto> GetSampleDto()
        {
            SampleInheritedDto result = new SampleInheritedDto { };

            result.LastName = "1";
            result.Id = Guid.NewGuid();
            result.Name = "1";

            return SingleResult(result);
        }
    }
}
