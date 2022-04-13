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
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.Sync.ODataEntityFrameworkCore.Implementations
{
    public class DefaultSyncService : ISyncService
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

                    int id = 0;

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
                            Id = id++,
                            DtoSetSyncConfig = fromServerSyncConfig,
                            DtoType = offlineSet.ElementType.GetTypeInfo(),
                            HadOfflineDtoBefore = mostRecentOfflineDto != null,
                            MaxVersion = maxVersion
                        };

                        IBoundClient<IDictionary<string, object>> query = (dtoSyncConfigSyncFromInformation.DtoSetSyncConfig.OnlineDtoSetForGet ?? dtoSyncConfigSyncFromInformation.DtoSetSyncConfig.OnlineDtoSet)(ODataClient);

                        if (dtoSyncConfigSyncFromInformation.MaxVersion == 0)
                            query = query.Where($"{nameof(ISyncableDto.IsArchived)} eq false");
                        else
                            query = query.Where($"{nameof(ISyncableDto.Version)} gt {dtoSyncConfigSyncFromInformation.MaxVersion}");

                        string oDataGetAndVersionFilter = await query
                            .GetCommandTextAsync(cancellationToken)
                            .ConfigureAwait(false);

                        dtoSyncConfigSyncFromInformation.ODataGetUri = $"{ClientAppProfile.HostUri}{ClientAppProfile.ODataRoute}{oDataGetAndVersionFilter}";

                        dtoSyncConfigSyncFromInformationList.Add(dtoSyncConfigSyncFromInformation);
                    }

                    StringBuilder batchRequests = new StringBuilder();

                    batchRequests.AppendLine(@"
{
    ""requests"": [");

                    foreach (DtoSyncConfigSyncFromInformation? dtoSyncConfigSyncFromInformation in dtoSyncConfigSyncFromInformationList)
                    {
                        batchRequests.AppendLine(@$"
        {{
            ""id"": ""{dtoSyncConfigSyncFromInformation.Id}"",
            ""method"": ""GET"",
            ""url"": ""{dtoSyncConfigSyncFromInformation.ODataGetUri}""
        }}{(dtoSyncConfigSyncFromInformation != dtoSyncConfigSyncFromInformationList.Last() ? "," : "")}");
                    }

                    batchRequests.AppendLine(@"
                  ]
}");

                    using (HttpResponseMessage batchResponbse = await HttpClient.PostAsync($"{ClientAppProfile.ODataRoute}$batch", new StringContent(batchRequests.ToString(), Encoding.UTF8, "application/json"), cancellationToken).ConfigureAwait(false))
                    {
                        batchResponbse.EnsureSuccessStatusCode();

                        if (batchResponbse.Content.Headers.ContentType.MediaType != "application/json")
                            throw new InvalidOperationException($"{batchResponbse.Content.Headers.ContentType.MediaType} content type is not supported.");

#if DotNetStandard2_0 || UWP
            using Stream stream = await batchResponbse.Content.ReadAsStreamAsync().ConfigureAwait(false);
#elif Android || iOS || DotNetStandard2_1
            await using Stream stream = await batchResponbse.Content.ReadAsStreamAsync().ConfigureAwait(false);
#else
                        await using Stream stream = await batchResponbse.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
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

                                    foreach (JToken response in jToken["responses"]!)
                                    {
                                        int responseId = response.Value<int>("id");

                                        DtoSyncConfigSyncFromInformation? dtoSyncConfigSyncFromInformation = dtoSyncConfigSyncFromInformationList.ExtendedSingle($"Getting dtoSyncConfigSyncFromInformation with id {responseId}", item => item.Id == responseId);

                                        dtoSyncConfigSyncFromInformation.RecentlyChangedOnlineDtos = ((IEnumerable)response["body"]!["value"]!.ToObject(typeof(List<>).MakeGenericType(dtoSyncConfigSyncFromInformation.DtoType))!).Cast<ISyncableDto>().ToArray();
                                    }
                                }
                            }
                        }
                    }

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

        private async Task GetMetadataIfNotRetrievedAlready(CancellationToken cancellationToken)
        {
            await ODataClient.GetMetadataAsync(cancellationToken).ConfigureAwait(false);
        }

        public class DtoSyncConfigSyncFromInformation
        {
            public int Id { get; set; }

            public bool HadOfflineDtoBefore { get; set; }

            public DtoSetSyncConfig DtoSetSyncConfig { get; set; } = default!;

            public TypeInfo DtoType { get; set; } = default!;

            public ISyncableDto[] RecentlyChangedOnlineDtos { get; set; } = default!;

            public long MaxVersion { get; set; }

            public string ODataGetUri { get; set; } = default!;

            [Obsolete]
            public Task RetrieveDataTask { get; set; } = default!;
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
