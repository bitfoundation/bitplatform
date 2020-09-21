using Bit.Owin.Contracts.Metadata;
using Bit.Owin.Implementations.Metadata;
using Bit.Tests.Model.DomainModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bit.Tests.Owin.Metadata.Model
{
    public class TestModelMetadata : DefaultDtoMetadataBuilder<TestModel>
    {
        public override async Task<IEnumerable<ObjectMetadata>> BuildMetadata()
        {
            AddDtoMetadata(new DtoMetadata { });

            AddMemberMetadata(nameof(TestModel.Id), new DtoMemberMetadata
            {

            });

            AddMemberMetadata(nameof(TestModel.StringProperty), new DtoMemberMetadata
            {

            });

            AddMemberMetadata(nameof(TestModel.DateProperty), new DtoMemberMetadata
            {

            });

            AddMemberMetadata(nameof(TestModel.Version), new DtoMemberMetadata
            {

            });

            return await base.BuildMetadata();
        }
    }
}
