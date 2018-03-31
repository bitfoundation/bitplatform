using Bit.Data;
using Bit.Model.Contracts;
using Bit.ViewModel.Contracts;
using Microsoft.EntityFrameworkCore;
using Plugin.Connectivity.Abstractions;
using Prism.Ioc;
using Simple.OData.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic;
using System.Reflection;
using System.Threading.Tasks;

namespace Bit.ViewModel.Implementations
{
    public class DefaultSyncService<TDbContext> : ISyncService
        where TDbContext : EfCoreDbContextBase
    {
        public DefaultSyncService(IConnectivity connectivity, IContainerProvider containerProvider)
        {
            if (containerProvider == null)
                throw new ArgumentNullException(nameof(containerProvider));

            if (connectivity == null)
                throw new ArgumentNullException(nameof(connectivity));

            _connectivity = connectivity;
            _containerProvider = containerProvider;
        }

        private readonly List<DtoSetSyncConfig> _configs = new List<DtoSetSyncConfig> { };
        private readonly IConnectivity _connectivity;
        private readonly IContainerProvider _containerProvider;

        public Task SyncContext()
        {
            return SyncDtoSets(_configs.Select(c => c.DtoSetName).ToArray());
        }

        public async Task SyncDtoSets(params string[] dtoSetNames)
        {
            if (dtoSetNames == null)
                throw new ArgumentNullException(nameof(dtoSetNames));

            if (_connectivity.IsConnected == false)
                return;

            DtoSetSyncConfig[] toServerDtoSetSyncMaterials = _configs.Where(c => c.ToServerSync == true && c.ToServerSyncFunc() == true && dtoSetNames.Any(n => n == c.DtoSetName)).ToArray();

            DtoSetSyncConfig[] fromServerDtoSetSyncMaterials = _configs.Where(c => c.FromServerSync == true && c.FromServerSyncFunc() == true && dtoSetNames.Any(n => n == c.DtoSetName)).ToArray();

            if (toServerDtoSetSyncMaterials.Any())
            {
                using (TDbContext offlineContextForSyncTo = _containerProvider.Resolve<TDbContext>())
                {
                    ((IsSyncDbContext)offlineContextForSyncTo).IsSyncDbContext = true;

                    ODataBatch onlineBatchContext = _containerProvider.Resolve<ODataBatch>();

                    foreach (DtoSetSyncConfig toServerSyncConfig in toServerDtoSetSyncMaterials)
                    {
                        IQueryable<ISyncableDto> offlineSet = toServerSyncConfig.OfflineDtoSet(offlineContextForSyncTo);

                        ISyncableDto[] recentlyChangedOfflineDtos = (await offlineSet.IgnoreQueryFilters().Where(s => EF.Property<bool>(s, "IsSynced") == false).AsNoTracking().ToArrayAsync().ConfigureAwait(false))
                            .Cast<ISyncableDto>()
                            .ToArray();

                        ISyncableDto firstRecentlyChangedOfflineDto = recentlyChangedOfflineDtos.FirstOrDefault();

                        if (firstRecentlyChangedOfflineDto == null)
                            continue;

                        TypeInfo dtoType = firstRecentlyChangedOfflineDto.GetType().GetTypeInfo();

                        PropertyInfo[] keyProps = offlineContextForSyncTo
                            .Model
                            .FindEntityType(dtoType)
                            .FindPrimaryKey()
                            .Properties
                            .Select(x => dtoType.GetProperty(x.Name))
                            .ToArray();

                        foreach (ISyncableDto recentlyChangedOfflineDto in recentlyChangedOfflineDtos)
                        {
                            object[] keys = keyProps.Select(p => p.GetValue(recentlyChangedOfflineDto, null)).ToArray();

                            if (recentlyChangedOfflineDto.IsArchived == true)
                            {
                                onlineBatchContext += new Func<IODataClient, Task>(c => toServerSyncConfig.OnlineDtoSet(c).Key(keys).DeleteEntryAsync());
                            }
                            else if (recentlyChangedOfflineDto.Version == 0)
                            {
                                onlineBatchContext += new Func<IODataClient, Task>(c => toServerSyncConfig.OnlineDtoSet(c).Set(recentlyChangedOfflineDto).InsertEntryAsync());
                            }
                            else
                            {
                                onlineBatchContext += new Func<IODataClient, Task>(c => toServerSyncConfig.OnlineDtoSet(c).Key(keys).Set(recentlyChangedOfflineDto).UpdateEntryAsync());
                            }
                        }
                    }

                    await onlineBatchContext.ExecuteAsync().ConfigureAwait(false);
                }
            }

            if (fromServerDtoSetSyncMaterials.Any())
            {
                using (TDbContext offlineContextForSyncFrom = _containerProvider.Resolve<TDbContext>())
                {
                    ((IsSyncDbContext)offlineContextForSyncFrom).IsSyncDbContext = true;

                    ODataBatch onlineBatchContext = _containerProvider.Resolve<ODataBatch>();

                    List<DtoSyncConfigSyncFromResults> recentlyChangedOnlineDtos = new List<DtoSyncConfigSyncFromResults>();

                    foreach (DtoSetSyncConfig fromServerSyncConfig in fromServerDtoSetSyncMaterials)
                    {
                        IQueryable<ISyncableDto> offlineSet = fromServerSyncConfig.OfflineDtoSet(offlineContextForSyncFrom);

                        long maxVersion = (await offlineSet.Select(e => new { e.Version }).OrderByDescending(e => e.Version).FirstOrDefaultAsync().ConfigureAwait(false))?.Version ?? 0;

                        onlineBatchContext += new Func<IODataClient, Task>(async c => recentlyChangedOnlineDtos.Add(new DtoSyncConfigSyncFromResults
                        {
                            DtoSetSyncConfig = fromServerSyncConfig,
                            RecentlyChangedOnlineDtos = CreateSyncableDtoInstancesFromUnTypedODataResponse(offlineSet.ElementType.GetTypeInfo(), (await (fromServerSyncConfig.OnlineDtoSetForGet ?? fromServerSyncConfig.OnlineDtoSet)(c).Where($"Version gt {maxVersion}").FindEntriesAsync().ConfigureAwait(false)).ToList()),
                            DtoType = offlineSet.ElementType.GetTypeInfo(),
                            HadOfflineDtoBefore = maxVersion > 0
                        }));
                    }

                    await onlineBatchContext.ExecuteAsync().ConfigureAwait(false);

                    foreach (DtoSyncConfigSyncFromResults result in recentlyChangedOnlineDtos.Where(r => r.RecentlyChangedOnlineDtos.Any()))
                    {
                        if (result.HadOfflineDtoBefore == false)
                        {
                            await offlineContextForSyncFrom.AddRangeAsync(result.RecentlyChangedOnlineDtos).ConfigureAwait(false);
                            foreach (ISyncableDto r in result.RecentlyChangedOnlineDtos)
                            {
                                offlineContextForSyncFrom.Entry(r).Property("IsSynced").CurrentValue = true;
                            }
                        }
                        else
                        {
                            PropertyInfo[] keyProps = offlineContextForSyncFrom
                                .Model
                                .FindEntityType(result.DtoType)
                                .FindPrimaryKey()
                                .Properties.Select(x => result.DtoType.GetProperty(x.Name))
                                .ToArray();

                            IQueryable<ISyncableDto> offlineSet = result.DtoSetSyncConfig.OfflineDtoSet(offlineContextForSyncFrom);

                            string equivalentOfflineDtosQuery = "";
                            List<object> equivalentOfflineDtosParams = new List<object>();
                            int parameterIndex = 0;

                            equivalentOfflineDtosQuery = string.Join(" || ", result.RecentlyChangedOnlineDtos.Select(s =>
                            {

                                return $" ( {string.Join(" && ", keyProps.Select(k => { equivalentOfflineDtosParams.Add(k.GetValue(s)); return $"{k.Name} == @{parameterIndex++}"; }))} )";

                            }));

                            List<ISyncableDto> equivalentOfflineDtos = await offlineSet.Where(equivalentOfflineDtosQuery, equivalentOfflineDtosParams.ToArray()).ToListAsync().ConfigureAwait(false);

                            foreach (ISyncableDto recentlyChangedOnlineDto in result.RecentlyChangedOnlineDtos)
                            {
                                bool hasEquivalentInOfflineDb = equivalentOfflineDtos.Any(d => keyProps.All(k => k.GetValue(d).Equals(k.GetValue(recentlyChangedOnlineDto))));

                                if (recentlyChangedOnlineDto.IsArchived == false || hasEquivalentInOfflineDb == true)
                                {
                                    if (recentlyChangedOnlineDto.IsArchived == true)
                                    {
                                        offlineContextForSyncFrom.Remove(recentlyChangedOnlineDto);
                                    }
                                    else if (hasEquivalentInOfflineDb == true)
                                    {
                                        offlineContextForSyncFrom.Update(recentlyChangedOnlineDto);
                                        offlineContextForSyncFrom.Entry(recentlyChangedOnlineDto).Property("IsSynced").CurrentValue = true;
                                    }
                                    else
                                    {
                                        await offlineContextForSyncFrom.AddAsync(recentlyChangedOnlineDto).ConfigureAwait(false);
                                        offlineContextForSyncFrom.Entry(recentlyChangedOnlineDto).Property("IsSynced").CurrentValue = true;
                                    }
                                }
                            }
                        }
                    }

                    await offlineContextForSyncFrom.SaveChangesAsync().ConfigureAwait(false);
                }
            }
        }

        class DtoSyncConfigSyncFromResults
        {
            public bool HadOfflineDtoBefore { get; set; }

            public DtoSetSyncConfig DtoSetSyncConfig { get; set; }

            public TypeInfo DtoType { get; set; }

            public List<ISyncableDto> RecentlyChangedOnlineDtos { get; set; }
        }

        private List<ISyncableDto> CreateSyncableDtoInstancesFromUnTypedODataResponse(TypeInfo dtoType, List<IDictionary<string, object>> untypedDtos)
        {
            return untypedDtos
                .Select(unTypedDto => unTypedDto.ToDto(dtoType))
                .Cast<ISyncableDto>()
                .ToList();
        }

        public void AddDtoSetSyncConfig(DtoSetSyncConfig dtoSetSyncConfig)
        {
            if (dtoSetSyncConfig == null)
                throw new ArgumentNullException(nameof(dtoSetSyncConfig));

            if (string.IsNullOrEmpty(dtoSetSyncConfig.DtoSetName))
                throw new ArgumentException($"{nameof(DtoSetSyncConfig.DtoSetName)} of {nameof(dtoSetSyncConfig)} may not be null or empty");

            _configs.Add(dtoSetSyncConfig);
        }
    }
}
