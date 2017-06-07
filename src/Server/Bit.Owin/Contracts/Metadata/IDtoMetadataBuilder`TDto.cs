using Bit.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Bit.Owin.Contracts.Metadata
{
    public class DtoMemberMetadata : ObjectMetadata
    {
        public virtual string DtoMemberName { get; set; }

        public virtual List<EnvironmentCulture> Messages { get; set; } = new List<EnvironmentCulture>();

        public virtual bool IsRequired { get; set; } = false;

        public virtual string Pattern { get; set; } = null;
    }

    public class DtoMemberLookup
    {
        public virtual string DtoMemberName { get; set; }

        public virtual string LookupDtoType { get; set; }

        public virtual string DataTextField { get; set; }

        public virtual string DataValueField { get; set; }

        public virtual string BaseFilter_JS { get; set; }
    }

    public class DtoMemberCultureTitle
    {
        public virtual string CultureName { get; set; }

        public virtual string Title { get; set; }
    }

    public class DtoMetadata : ObjectMetadata
    {
        public virtual string DtoType { get; set; }

        public virtual List<DtoMemberMetadata> MembersMetadata { get; set; } = new List<DtoMemberMetadata> { };

        public virtual List<DtoMemberLookup> MembersLookups { get; set; } = new List<DtoMemberLookup> { };
    }

    public interface IDtoMetadataBuilder<TDto>
        where TDto : class
    {
        IDtoMetadataBuilder<TDto> AddDtoMetadata(DtoMetadata metadata);

        IDtoMetadataBuilder<TDto> AddMemberMetadata(string memberName, DtoMemberMetadata metadata);

        IDtoMetadataBuilder<TDto> AddMemberMetadata(PropertyInfo member, DtoMemberMetadata metadata);

        IDtoMetadataBuilder<TDto> AddLookup<TLookupDto>(string memberName, string dataValueField, string dataTextField, Expression<Func<TLookupDto, bool>> baseFilter = null, string lookupName = null)
            where TLookupDto : class;
    }
}
