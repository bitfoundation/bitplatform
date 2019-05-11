using System.Collections.Generic;
using System.Linq;
using Bit.OData.ODataControllers;
using Bit.Tests.Model.Dto;

namespace Bit.Tests.Api.ApiControllers
{
    public class DtoWithEnumController : DtoController<DtoWithEnum>
    {
        [Function]
        public virtual List<DtoWithEnum> GetDtoWithEnumsByGender(TestGender gender)
        {
            return new List<DtoWithEnum> { new DtoWithEnum { Id = 1, Gender = gender } };
        }

        [Action]
        public virtual bool PostDtoWithEnum(DtoWithEnum dto)
        {
            return dto.Gender == TestGender.Man;
        }

        [Function]
        public virtual List<DtoWithEnum> GetDtoWithEnumsByGender2(TestGender2 gender)
        {
            return new List<DtoWithEnum> { new DtoWithEnum { Id = 1, Test = gender.ToString() } };
        }

        [Action]
        public virtual bool TestEnumsArray(IEnumerable<TestGender2> enums)
        {
            return enums.Count() == 2;
        }
    }
}
