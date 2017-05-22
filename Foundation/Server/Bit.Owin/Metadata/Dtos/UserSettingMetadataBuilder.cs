using System.Collections.Generic;
using Foundation.Api.Contracts.Metadata;
using Foundation.Api.Implementations.Metadata;
using Foundation.Model.DomainModels;
using System.Threading.Tasks;

namespace Foundation.Api.Metadata.Dtos
{
    public class UserSettingMetadataBuilder : DefaultDtoMetadataBuilder<UserSetting>
    {
        public override async Task<IEnumerable<ObjectMetadata>> BuildMetadata()
        {
            AddDtoMetadata(new DtoMetadata { });

            AddMemberMetadata(nameof(UserSetting.Id), new DtoMemberMetadata
            {

            });

            AddMemberMetadata(nameof(UserSetting.Theme), new DtoMemberMetadata
            {

            });

            AddMemberMetadata(nameof(UserSetting.UserId), new DtoMemberMetadata
            {

            });

            AddMemberMetadata(nameof(UserSetting.Culture), new DtoMemberMetadata
            {

            });

            AddMemberMetadata(nameof(UserSetting.DesiredTimeZone), new DtoMemberMetadata
            {

            });

            AddMemberMetadata(nameof(UserSetting.Version), new DtoMemberMetadata
            {

            });

            return await base.BuildMetadata();
        }
    }
}
