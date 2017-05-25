using System.Collections.Generic;
using System.Reflection;
using Bit.Core.Models;

namespace Bit.Owin.Contracts.Metadata
{
    public class DtoMemberMetadata : ObjectMetadata
    {
        public virtual string DtoMemberName { get; set; }

        public virtual List<EnvironmentCulture> Messages { get; set; } = new List<EnvironmentCulture>();

        public virtual bool IsRequired { get; set; } = false;

        public virtual string Pattern { get; set; } = null;
    }

    public class DtoMemberCultureTitle
    {
        public virtual string CultureName { get; set; }

        public virtual string Title { get; set; }
    }

    public class DtoMetadata : ObjectMetadata
    {
        public virtual string DtoType { get; set; }

        public virtual List<DtoMemberMetadata> MembersMetadata { get; set; } = new List<DtoMemberMetadata>();
    }

    public interface IDtoMetadataBuilder<TDto>
        where TDto : class
    {
        IDtoMetadataBuilder<TDto> AddDtoMetadata(DtoMetadata metadata);

        IDtoMetadataBuilder<TDto> AddMemberMetadata(string memberName, DtoMemberMetadata metadata);

        IDtoMetadataBuilder<TDto> AddMemberMetadata(PropertyInfo member, DtoMemberMetadata metadata);
    }
}
