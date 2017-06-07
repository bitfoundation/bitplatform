using System.Collections.Generic;
using System.Linq;
using Bit.Api.ApiControllers;
using Bit.Tests.Model.Dto;

namespace Bit.Tests.Api.ApiControllers
{
    public class ValidationSamplesController : DtoController<ValidationSampleDto>
    {
        public class SubmitValidationsParameters
        {
            public IEnumerable<ValidationSampleDto> validations { get; set; }

            public string arg { get; set; }
        }

        [Action]
        public virtual IEnumerable<ValidationSampleDto> SubmitValidations(SubmitValidationsParameters parameters)
        {
            return parameters.validations
                .Select(v =>
                {
                    v.Id++;
                    v.RequiredByAttributeMember += v.RequiredByAttributeMember + parameters.arg ?? "";
                    v.RequiredByMetadataMember += v.RequiredByMetadataMember + parameters.arg ?? "";
                    return v;
                });
        }

        [Get]
        public virtual ValidationSampleDto[] Get()
        {
            return new ValidationSampleDto[] { };
        }
    }
}
