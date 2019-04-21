using AutoMapper;
using System.Web.Http;

namespace Bit.Tests.Api.ApiControllers
{
    [RoutePrefix("mapper")]
    public class MapperController : ApiController
    {
        public virtual IMapper Mapper { get; set; }

        [Route("some-api")]
        [HttpGet]
        public virtual DestClass SomeApi()
        {
            return Mapper.Map<DestClass>(new SourceClass { Id = 1 });
        }
    }

    public class SourceClass
    {
        public int Id { get; set; }
    }

    public class DestClass
    {
        public int Id { get; set; }

        public virtual string UserId { get; set; }
    }
}
