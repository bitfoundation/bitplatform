using Foundation.Api.ApiControllers;
using Foundation.Test.Model.Dto;
using System.Collections.Generic;
using System.Linq;
using System.Web.OData;

namespace Foundation.Test.Api.ApiControllers
{
    public class ValidationSamplesController : DtoController<ValidationSampleDto>
    {
        [Action]
        [Parameter("validations", typeof(IEnumerable<ValidationSampleDto>))]
        [Parameter("arg", typeof(string), isOptional: true)]
        public virtual IEnumerable<ValidationSampleDto> SubmitValidations(ODataActionParameters parameters)
        {
            return ((IEnumerable<ValidationSampleDto>)parameters["validations"])
                .Select(v =>
                {
                    v.Id++;
                    v.RequiredByAttributeMember += v.RequiredByAttributeMember + (parameters.ContainsKey("arg") ? parameters["arg"] : "");
                    v.RequiredByMetadataMember += v.RequiredByMetadataMember + (parameters.ContainsKey("arg") ? parameters["arg"] : "");
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
