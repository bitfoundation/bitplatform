using System;

namespace Foundation.Model.Contracts
{
    public interface ISyncableDto : IsArchivableDto
    {
        Guid Id { get; set; }

        long Version { get; set; }

        bool ISV { get; set; }
    }
}
