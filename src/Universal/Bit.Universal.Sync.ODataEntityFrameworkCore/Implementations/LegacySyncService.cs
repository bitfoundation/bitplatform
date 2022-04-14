using Autofac;
using Bit.Core.Contracts;
using Bit.Data.EntityFrameworkCore.Implementations;
using Bit.Model.Contracts;
using Bit.Sync.ODataEntityFrameworkCore.Contracts;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Simple.OData.Client;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Dynamic;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using static Bit.Sync.ODataEntityFrameworkCore.Implementations.DefaultSyncService;

namespace Bit.Sync.ODataEntityFrameworkCore.Implementations
{
    [Obsolete("Use DefaultSyncService")]
    public class LegacySyncService : ISyncService
    {
        public IContainer Container { get; set; } = default!;
        public IClientAppProfile ClientAppProfile { get; set; } = default!;
        public HttpClient HttpClient { get; set; } = default!;
        public IODataClient ODataClient { get; set; } = default!;
        public IExceptionHandler ExceptionHandler { get; set; } = default!;

        private readonly List<DtoSetSyncConfig> _configs = new List<DtoSetSyncConfig> { };

        public virtual Task SyncContext(CancellationToken cancellationToken = default)
        {
            return SyncDtoSets(cancellationToken, _configs.Select(c => c.DtoSetName).ToArray());
        }

        public virtual async Task SyncDtoSets(CancellationToken cancellationToken = default, params string[] dtoSetNames)
        {
            if (dtoSetNames == null)
                throw new ArgumentNullException(nameof(dtoSetNames));

#if Xamarin
            if (Xamarin.Essentials.Connectivity.NetworkAccess == Xamarin.Essentials.NetworkAccess.None)
                return;
#elif Maui
            if (Microsoft.Maui.Networking.Connectivity.NetworkAccess ==Microsoft.Maui.Networking.NetworkAccess.None)
                return;
#endif

            DtoSetSyncConfig[] toServerDtoSetSyncMaterials = _configs.Where(c => c.ToServerSync == true && c.ToServerSyncFunc() == true && dtoSetNames.Any(n => n == c.DtoSetName)).ToArray();

            DtoSetSyncConfig[] fromServerDtoSetSyncMaterials = _configs.Where(c => c.FromServerSync == true && c.FromServerSyncFunc() == true && dtoSetNames.Any(n => n == c.DtoSetName)).ToArray();

            await CallSyncTo(toServerDtoSetSyncMaterials, cancellationToken).ConfigureAwait(false);

            await CallSyncFrom(fromServerDtoSetSyncMaterials, cancellationToken).ConfigureAwait(false);
        }

        public virtual async Task CallSyncTo(DtoSetSyncConfig[] toServerDtoSetSyncMaterials, CancellationToken cancellationToken)
        {
            if (toServerDtoSetSyncMaterials == null)
                throw new ArgumentNullException(nameof(toServerDtoSetSyncMaterials));

            if (toServerDtoSetSyncMaterials.Any())
            {
                await using (EfCoreDbContextBase offlineContextForSyncTo = Container.Resolve<EfCoreDbContextBase>())
                {
                    ((IsSyncDbContext)offlineContextForSyncTo).IsSyncDbContext = true;

                    ODataBatch onlineBatchContext = Container.Resolve<ODataBatch>();

                    foreach (DtoSetSyncConfig toServerSyncConfig in toServerDtoSetSyncMaterials)
                    {
                        IQueryable<ISyncableDto> offlineSet = toServerSyncConfig.OfflineDtoSet(offlineContextForSyncTo);

                        ISyncableDto[] recentlyChangedOfflineDtos = (await offlineSet.IgnoreQueryFilters().Where(s => EF.Property<bool>(s, "IsSynced") == false).AsNoTracking().ToArrayAsync(cancellationToken).ConfigureAwait(false))
                            .ToArray();

                        if (recentlyChangedOfflineDtos.Any() == false)
                            continue;

                        TypeInfo dtoType = offlineSet.ElementType.GetTypeInfo();

                        PropertyInfo[] keyProps = offlineContextForSyncTo
                            .Model
                            .FindEntityType(dtoType)
                            .FindPrimaryKey()
                            .Properties
                            .Select(x => dtoType.GetProperty(x.Name))
                            .ToArray()!;

                        foreach (ISyncableDto recentlyChangedOfflineDto in recentlyChangedOfflineDtos)
                        {
                            object[] keys = keyProps.Select(p => p.GetValue(recentlyChangedOfflineDto, null)).ToArray()!;

                            if (recentlyChangedOfflineDto.IsArchived == true)
                            {
                                onlineBatchContext += c => toServerSyncConfig.OnlineDtoSet(c).Key(keys).DeleteEntryAsync(cancellationToken);
                            }
                            else if (recentlyChangedOfflineDto.Version == 0)
                            {
                                onlineBatchContext += c => toServerSyncConfig.OnlineDtoSet(c).Set(recentlyChangedOfflineDto).CreateEntryAsync(cancellationToken);
                            }
                            else
                            {
                                onlineBatchContext += c => toServerSyncConfig.OnlineDtoSet(c).Key(keys).Set(recentlyChangedOfflineDto).UpdateEntryAsync(cancellationToken);
                            }
                        }
                    }

                    await onlineBatchContext.ExecuteAsync(cancellationToken).ConfigureAwait(false);
                }
            }
        }

        public virtual async Task CallSyncFrom(DtoSetSyncConfig[] fromServerDtoSetSyncMaterials, CancellationToken cancellationToken)
        {
            if (fromServerDtoSetSyncMaterials == null)
                throw new ArgumentNullException(nameof(fromServerDtoSetSyncMaterials));

            if (fromServerDtoSetSyncMaterials.Any())
            {
                await GetMetadataIfNotRetrievedAlready(cancellationToken).ConfigureAwait(false);

                await using (EfCoreDbContextBase offlineContextForSyncFrom = Container.Resolve<EfCoreDbContextBase>())
                {
                    ((IsSyncDbContext)offlineContextForSyncFrom).IsSyncDbContext = true;

                    List<DtoSyncConfigSyncFromInformation> dtoSyncConfigSyncFromInformationList = new List<DtoSyncConfigSyncFromInformation>();

                    foreach (DtoSetSyncConfig fromServerSyncConfig in fromServerDtoSetSyncMaterials)
                    {
                        IQueryable<ISyncableDto> offlineSet = fromServerSyncConfig.OfflineDtoSet(offlineContextForSyncFrom);

                        var mostRecentOfflineDto = await offlineSet
                            .IgnoreQueryFilters()
                            .AsNoTracking()
                            .Select(e => new { e.Version })
                            .OrderByDescending(e => e.Version)
                            .FirstOrDefaultAsync(cancellationToken)
                            .ConfigureAwait(false);

                        long maxVersion = mostRecentOfflineDto?.Version ?? 0;

                        DtoSyncConfigSyncFromInformation dtoSyncConfigSyncFromInformation = new DtoSyncConfigSyncFromInformation
                        {
                            DtoSetSyncConfig = fromServerSyncConfig,
                            DtoType = offlineSet.ElementType.GetTypeInfo(),
                            HadOfflineDtoBefore = mostRecentOfflineDto != null,
                            MaxVersion = maxVersion
                        };

                        dtoSyncConfigSyncFromInformation.RetrieveDataTask = BuildRetrieveDataTask(dtoSyncConfigSyncFromInformation, cancellationToken);

                        dtoSyncConfigSyncFromInformationList.Add(dtoSyncConfigSyncFromInformation);
                    }

                    await Task.WhenAll(dtoSyncConfigSyncFromInformationList.Select(r => r.RetrieveDataTask)).ConfigureAwait(false);

                    foreach (DtoSyncConfigSyncFromInformation dtoSyncConfigSyncFromInformation in dtoSyncConfigSyncFromInformationList.Where(r => r.RecentlyChangedOnlineDtos.Any()))
                    {
                        if (dtoSyncConfigSyncFromInformation.HadOfflineDtoBefore == false)
                        {
                            foreach (ISyncableDto r in dtoSyncConfigSyncFromInformation.RecentlyChangedOnlineDtos)
                            {
                                offlineContextForSyncFrom.Add(r).Property("IsSynced").CurrentValue = true;
                            }
                        }
                        else
                        {
                            PropertyInfo[] keyProps = offlineContextForSyncFrom
                                .Model
                                .FindEntityType(dtoSyncConfigSyncFromInformation.DtoType)
                                .FindPrimaryKey()
                                .Properties.Select(x => dtoSyncConfigSyncFromInformation.DtoType.GetProperty(x.Name))
                                .ToArray()!;

                            IQueryable<ISyncableDto> offlineSet = dtoSyncConfigSyncFromInformation.DtoSetSyncConfig.OfflineDtoSet(offlineContextForSyncFrom);

                            string equivalentOfflineDtosQuery = "";
                            List<object> equivalentOfflineDtosParams = new List<object>();
                            int parameterIndex = 0;

                            equivalentOfflineDtosQuery = string.Join(" || ", dtoSyncConfigSyncFromInformation.RecentlyChangedOnlineDtos.Select(s =>
                            {

                                return $" ( {string.Join(" && ", keyProps.Select(k => { equivalentOfflineDtosParams.Add(k.GetValue(s)!); return $"{k.Name} == @{parameterIndex++}"; }))} )";

                            }));

                            List<ISyncableDto> equivalentOfflineDtos = await offlineSet
                                .Where(equivalentOfflineDtosQuery, equivalentOfflineDtosParams.ToArray())
                                .IgnoreQueryFilters()
                                .AsNoTracking()
                                .ToListAsync(cancellationToken)
                                .ConfigureAwait(false);

                            foreach (ISyncableDto recentlyChangedOnlineDto in dtoSyncConfigSyncFromInformation.RecentlyChangedOnlineDtos)
                            {
                                bool hasEquivalentInOfflineDb = equivalentOfflineDtos.Any(d => keyProps.All(k => k.GetValue(d)!.Equals(k.GetValue(recentlyChangedOnlineDto))));

                                if (recentlyChangedOnlineDto.IsArchived == false || hasEquivalentInOfflineDb == true)
                                {
                                    if (recentlyChangedOnlineDto.IsArchived == true)
                                    {
                                        offlineContextForSyncFrom.Remove(recentlyChangedOnlineDto);
                                    }
                                    else if (hasEquivalentInOfflineDb == true)
                                    {
                                        offlineContextForSyncFrom.Update(recentlyChangedOnlineDto).Property("IsSynced").CurrentValue = true;
                                    }
                                    else
                                    {
                                        offlineContextForSyncFrom.Add(recentlyChangedOnlineDto).Property("IsSynced").CurrentValue = true;
                                    }
                                }
                            }
                        }
                    }

                    await offlineContextForSyncFrom.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// <see cref="BuildRetrieveDataTask"/> is being called in parallel because of Task.WhenAll. That method calls GetCommandTextAsync which retrieves $metadata if it's not retrieved already.
        /// Due race condition, ODataClient might retrieve $metadata multiple times, but if we get $metadata here (before Task.WhenAll), we can bypass the issue.
        /// </summary>
        private async Task GetMetadataIfNotRetrievedAlready(CancellationToken cancellationToken)
        {
            await ODataClient.GetMetadataAsync(cancellationToken).ConfigureAwait(false);
        }

        protected virtual async Task BuildRetrieveDataTask(DtoSyncConfigSyncFromInformation dtoSyncConfigSyncFromInformation, CancellationToken cancellationToken)
        {
            if (dtoSyncConfigSyncFromInformation == null)
                throw new ArgumentNullException(nameof(dtoSyncConfigSyncFromInformation));

            try
            {
                IBoundClient<IDictionary<string, object>> query = (dtoSyncConfigSyncFromInformation.DtoSetSyncConfig.OnlineDtoSetForGet ?? dtoSyncConfigSyncFromInformation.DtoSetSyncConfig.OnlineDtoSet)(ODataClient);

                if (dtoSyncConfigSyncFromInformation.MaxVersion == 0)
                    query = query.Where($"{nameof(ISyncableDto.IsArchived)} eq false");
                else
                    query = query.Where($"{nameof(ISyncableDto.Version)} gt {dtoSyncConfigSyncFromInformation.MaxVersion}");

                string oDataGetAndVersionFilter = await query
                    .GetCommandTextAsync(cancellationToken)
                    .ConfigureAwait(false);

                string oDataUri = $"{ClientAppProfile.ODataRoute}{oDataGetAndVersionFilter}";

                using (HttpResponseMessage response = await HttpClient.GetAsync(oDataUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();

#if DotNetStandard2_0 || UWP
                    using Stream stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
#elif Android || iOS || DotNetStandard2_1
                    await using Stream stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
#else
                    await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
#endif
                    {
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            using (JsonReader jsonReader = new JsonTextReader(reader))
                            {
                                JToken jToken = await JToken.LoadAsync(jsonReader, new JsonLoadSettings
                                {
                                    CommentHandling = CommentHandling.Ignore,
                                    LineInfoHandling = LineInfoHandling.Ignore
                                }, cancellationToken).ConfigureAwait(false);

                                dtoSyncConfigSyncFromInformation.RecentlyChangedOnlineDtos = ((IEnumerable)(jToken)["value"]!.ToObject(typeof(List<>).MakeGenericType(dtoSyncConfigSyncFromInformation.DtoType))!).Cast<ISyncableDto>().ToArray();
                            }
                        }
                    }
                }
            }
            catch (Exception exp)
            {
                dtoSyncConfigSyncFromInformation.RecentlyChangedOnlineDtos = Array.Empty<ISyncableDto>();
                ExceptionHandler.OnExceptionReceived(exp);
            }
        }

        public virtual ISyncService AddDtoSetSyncConfig(DtoSetSyncConfig dtoSetSyncConfig)
        {
            if (dtoSetSyncConfig == null)
                throw new ArgumentNullException(nameof(dtoSetSyncConfig));

            if (string.IsNullOrEmpty(dtoSetSyncConfig.DtoSetName))
                throw new ArgumentException($"{nameof(DtoSetSyncConfig.DtoSetName)} of {nameof(dtoSetSyncConfig)} may not be null or empty");

            _configs.Add(dtoSetSyncConfig);

            return this;
        }
    }
}
