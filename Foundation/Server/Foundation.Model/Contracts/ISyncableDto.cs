using System;

namespace Foundation.Model.Contracts
{
    public interface ISyncableDto : IsArchivableDto
    {
        long Version { get; set; }

        bool ISV { get; set; }
    }
}
