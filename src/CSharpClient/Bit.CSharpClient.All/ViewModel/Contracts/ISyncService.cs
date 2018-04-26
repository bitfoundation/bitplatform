using Bit.Model.Contracts;
using Microsoft.EntityFrameworkCore;
using Simple.OData.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bit.ViewModel.Contracts
{
    public class DtoSetSyncConfig
    {
        public virtual string DtoSetName { get; set; }

        public virtual Func<IODataClient, IBoundClient<IDictionary<string, object>>> OnlineDtoSet { get; set; }

        public virtual Func<IODataClient, IBoundClient<IDictionary<string, object>>> OnlineDtoSetForGet { get; set; }

        public virtual Func<DbContext, IQueryable<ISyncableDto>> OfflineDtoSet { get; set; }

        public virtual bool FromServerSync { get; set; } = true;

        public virtual Func<bool> FromServerSyncFunc { get; set; } = () => true;

        public virtual bool ToServerSync { get; set; } = true;

        public virtual Func<bool> ToServerSyncFunc { get; set; } = () => true;
    }

    public interface ISyncService
    {
        void AddDtoSetSyncConfig(DtoSetSyncConfig dtoSetSyncConfig);

        Task SyncContext();

        Task SyncDtoSets(params string[] dtoSetNames);
    }
}
