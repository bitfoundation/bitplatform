using Foundation.Api.ApiControllers;
using Foundation.Test.Model.Dto;
using System.Collections.Generic;
using System.Linq;

namespace Foundation.Test.Api.ApiControllers
{
    public class DtoWithEnumController : DtoController<DtoWithEnum>
    {
        [Function]
        [Parameter("gender", typeof(TestGender))]
        public virtual List<DtoWithEnum> GetDtoWithEnumsByGender(TestGender gender)
        {
            return new List<DtoWithEnum> { new DtoWithEnum { Id = 1, Gender = gender } };
        }

        public class PostDtoWithEnumParameters
        {
            public DtoWithEnum dto { get; set; }
        }

        [Action]
        [Parameter("dto", typeof(DtoWithEnum))]
        public virtual bool PostDtoWithEnum(PostDtoWithEnumParameters actionParameters)
        {
            return actionParameters.dto.Gender == TestGender.Man;
        }

        [Function]
        [Parameter("gender", typeof(TestGender2))]
        public virtual List<DtoWithEnum> GetDtoWithEnumsByGender2(TestGender2 gender)
        {
            return new List<DtoWithEnum> { new DtoWithEnum { Id = 1, Test = gender.ToString() } };
        }

        public class TestEnumsArrayParameters
        {
            public IEnumerable<TestGender2> enums { get; set; }
        }

        [Action]
        [Parameter("enums", typeof(IEnumerable<TestGender2>))]
        public virtual bool TestEnumsArray(TestEnumsArrayParameters parameters)
        {
            return parameters.enums.Count() == 2;
        }
    }
}
