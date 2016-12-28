using Foundation.Api.ApiControllers;
using Foundation.Test.Model.Dto;
using System.Collections.Generic;
using System.Linq;
using System.Web.OData;

namespace Foundation.Test.Api.ApiControllers
{
    public class DtoWithEnumController : DtoController<DtoWithEnum>
    {
        [Function]
        [Parameter("gender", typeof(TestGender))]
        public virtual List<DtoWithEnum> GetDtoWithEnumsByGender([FromODataUri]TestGender gender)
        {
            return new List<DtoWithEnum> { new DtoWithEnum { Id = 1, Gender = gender } };
        }

        [Function]
        [Parameter("gender", typeof(TestGender2))]
        public virtual List<DtoWithEnum> GetDtoWithEnumsByGender2([FromODataUri]TestGender2 gender)
        {
            return new List<DtoWithEnum> { new DtoWithEnum { Id = 1, Test = gender.ToString() } };
        }
    }
}
