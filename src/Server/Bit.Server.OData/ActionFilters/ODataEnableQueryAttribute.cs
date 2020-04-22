using Bit.Core.Exceptions;
using Bit.Data.Contracts;
using Bit.Data.Implementations;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Query;
using Microsoft.Owin;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Filters;

namespace Bit.OData.ActionFilters
{
    public class ODataEnableQueryAttribute : EnableQueryAttribute
    {
        private int? _defaultPageSize;

        public virtual int? DefaultPageSize
        {
            get => _defaultPageSize;
            set
            {
                _defaultPageSize = value;
                if (value.HasValue)
                    PageSize = value.Value;
            }
        }

        private readonly ConcurrentDictionary<Type, MethodInfo> getCountAsyncMethodsCache = new ConcurrentDictionary<Type, MethodInfo>();
        private readonly ConcurrentDictionary<Type, MethodInfo> findDataProviderSpecificMethodsProviderMethodsCache = new ConcurrentDictionary<Type, MethodInfo>();
        private readonly ConcurrentDictionary<Type, MethodInfo> toQueryableMethodsCache = new ConcurrentDictionary<Type, MethodInfo>();
        private readonly ConcurrentDictionary<Type, MethodInfo> getResultMethodsCache = new ConcurrentDictionary<Type, MethodInfo>();
        private readonly ConcurrentDictionary<Type, MethodInfo> firstAsyncMethodsCache = new ConcurrentDictionary<Type, MethodInfo>();

        public override async Task OnActionExecutedAsync(HttpActionExecutedContext actionExecutedContext, CancellationToken cancellationToken)
        {
            if (actionExecutedContext == null)
                throw new ArgumentNullException(nameof(actionExecutedContext));

            if (actionExecutedContext.Response?.Content is ObjectContent objContent &&
                actionExecutedContext.Response.IsSuccessStatusCode == true &&
                !actionExecutedContext.Request.Properties.ContainsKey("IgnoreODataEnableQuery"))
            {
                if (objContent.Value == null)
                    return;

                bool isSingleResult = objContent.Value is SingleResult;
                bool isCountRequest = actionExecutedContext.Request.RequestUri.LocalPath.Contains("/$count", StringComparison.InvariantCultureIgnoreCase) == true;

                if (isSingleResult == true)
                    objContent.Value = ((SingleResult)objContent.Value).Queryable;

                TypeInfo actionReturnType = objContent.Value.GetType().GetTypeInfo();

                if (typeof(string) != actionReturnType && typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(actionReturnType))
                {
                    TypeInfo queryElementType = actionReturnType.HasElementType ? actionReturnType.GetElementType()!.GetTypeInfo() : actionReturnType.GetGenericArguments().First() /* Why not calling Single() ? http://stackoverflow.com/questions/41718323/why-variable-of-type-ienumerablesomething-gettype-getgenericargsuments-c */.GetTypeInfo();

                    bool isIQueryable = typeof(IQueryable).GetTypeInfo().IsAssignableFrom(actionReturnType);

                    IDataProviderSpecificMethodsProvider? dataProviderSpecificMethodsProvider = null;

                    if (isIQueryable == true)
                    {
                        IEnumerable<IDataProviderSpecificMethodsProvider> dataProviderSpecificMethodsProviders = actionExecutedContext.Request.GetOwinContext().GetDependencyResolver().ResolveAll<IDataProviderSpecificMethodsProvider>();
                        dataProviderSpecificMethodsProvider = (IDataProviderSpecificMethodsProvider)
                            findDataProviderSpecificMethodsProviderMethodsCache.GetOrAdd(queryElementType, t => typeof(ODataEnableQueryAttribute).GetMethod(nameof(FindDataProviderSpecificMethodsProvider))!.MakeGenericMethod(t))
                            .Invoke(this, new[] { objContent.Value, dataProviderSpecificMethodsProviders })!;
                    }
                    else
                    {
                        objContent.Value = toQueryableMethodsCache.GetOrAdd(queryElementType, t => typeof(ODataEnableQueryAttribute).GetMethod(nameof(ToQueryable))!.MakeGenericMethod(t))
                            .Invoke(this, new[] { objContent.Value });
                    }

                    if (dataProviderSpecificMethodsProvider == null)
                        dataProviderSpecificMethodsProvider = DefaultDataProviderSpecificMethodsProvider.Current;

                    HttpRequestMessageProperties requestODataProps = actionExecutedContext.Request.ODataProperties();
                    ODataQueryContext currentOdataQueryContext = new ODataQueryContext(actionExecutedContext.Request.GetModel(), queryElementType, requestODataProps.Path);
                    ODataQueryOptions currentOdataQueryOptions = new ODataQueryOptions(currentOdataQueryContext, actionExecutedContext.Request);
                    if (currentOdataQueryOptions.SelectExpand?.SelectExpandClause != null && requestODataProps.SelectExpandClause == null)
                        requestODataProps.SelectExpandClause = currentOdataQueryOptions.SelectExpand.SelectExpandClause;
                    ODataQuerySettings globalODataQuerySettings = new ODataQuerySettings
                    {
                        EnableConstantParameterization = this.EnableConstantParameterization,
                        EnsureStableOrdering = this.EnsureStableOrdering,
                        HandleNullPropagation = this.HandleNullPropagation,
                        PageSize = this.DefaultPageSize
                    };

                    if (dataProviderSpecificMethodsProvider.SupportsConstantParameterization() == true)
                        globalODataQuerySettings.EnableConstantParameterization = true;

                    ValidateQuery(actionExecutedContext.Request, currentOdataQueryOptions);

                    int? currentQueryPageSize = currentOdataQueryOptions.Top?.Value;
                    int? globalQuerypageSize = globalODataQuerySettings.PageSize;
                    int? takeCount = null;
                    int? skipCount = currentOdataQueryOptions.Skip?.Value;

                    if (currentQueryPageSize != null)
                        takeCount = currentQueryPageSize.Value;
                    else if (globalQuerypageSize != null)
                        takeCount = globalQuerypageSize.Value;
                    else
                        takeCount = null;

                    globalODataQuerySettings.PageSize = null; // ApplyTo will enumerates the query for values other than null. We are gonna apply take in getResult method.

                    if (currentOdataQueryOptions.Filter != null)
                    {
                        objContent.Value = currentOdataQueryOptions.Filter.ApplyTo(query: (IQueryable)objContent.Value!, querySettings: globalODataQuerySettings);
                    }

                    if (isCountRequest == true)
                    {
                        objContent.Value = await (Task<long>)
                            getCountAsyncMethodsCache.GetOrAdd(queryElementType, t => typeof(ODataEnableQueryAttribute).GetMethod(nameof(GetCountAsync))!.MakeGenericMethod(t))
                            .Invoke(this, new[] { objContent.Value, dataProviderSpecificMethodsProvider, cancellationToken })!;

                        return;
                    }

                    if (currentOdataQueryOptions.Count?.Value == true && takeCount != null && isSingleResult == false)
                    {
                        long count = await (Task<long>)
                            getCountAsyncMethodsCache.GetOrAdd(queryElementType, t => typeof(ODataEnableQueryAttribute).GetMethod(nameof(GetCountAsync))!.MakeGenericMethod(t))
                            .Invoke(this, new[] { objContent.Value, dataProviderSpecificMethodsProvider, cancellationToken })!;

                        actionExecutedContext.Request.Properties["Microsoft.AspNet.OData.TotalCountFunc"] = new Func<long>(() => count);
                    }

                    AllowedQueryOptions ignoreQueryOptions = dataProviderSpecificMethodsProvider.SupportsExpand() ? (AllowedQueryOptions.Filter | AllowedQueryOptions.Skip | AllowedQueryOptions.Top) : (AllowedQueryOptions.Filter | AllowedQueryOptions.Skip | AllowedQueryOptions.Top | AllowedQueryOptions.Expand);

                    objContent.Value = currentOdataQueryOptions.ApplyTo(query: (IQueryable)objContent.Value!, querySettings: globalODataQuerySettings, ignoreQueryOptions: ignoreQueryOptions);

                    if (currentOdataQueryOptions.SelectExpand != null || currentOdataQueryOptions.Apply != null)
                    {
                        TypeInfo newReturnTypeAfterApplyingSelectOrApply = objContent.Value.GetType().GetTypeInfo();
                        queryElementType = newReturnTypeAfterApplyingSelectOrApply.GetGenericArguments().ExtendedSingle($"Get generic arguments of ${newReturnTypeAfterApplyingSelectOrApply.Name}").GetTypeInfo();
                    }

                    if (isSingleResult == false)
                    {
                        objContent.Value = await (Task<object>)
                            getResultMethodsCache.GetOrAdd(queryElementType, t => typeof(ODataEnableQueryAttribute).GetMethod(nameof(GetResult))!.MakeGenericMethod(t))
                            .Invoke(this, new[] { objContent.Value, dataProviderSpecificMethodsProvider, takeCount, skipCount, cancellationToken })!;
                    }
                    else
                    {
                        objContent.Value = await (Task<object>)
                            firstAsyncMethodsCache.GetOrAdd(queryElementType, t => typeof(ODataEnableQueryAttribute).GetMethod(nameof(FirstAsync))!.MakeGenericMethod(t))
                            .Invoke(this, new[] { objContent.Value, dataProviderSpecificMethodsProvider, cancellationToken })!;

                        actionExecutedContext.Request.Properties["Microsoft.AspNet.OData.TotalCountFunc"] = new Func<long>(() => 1);
                    }

                    if (currentOdataQueryOptions.Count?.Value == true && takeCount == null && isSingleResult == false)
                    {
                        // We've no paging because there is no global config for max top and there is no top specified by the client's request, so the returned result of query's length is equivalent to total count of the query
                        long count = ((IList)objContent.Value).Count;
                        actionExecutedContext.Request.Properties["Microsoft.AspNet.OData.TotalCountFunc"] = new Func<long>(() => count);
                    }
                }
            }
        }

        public virtual IDataProviderSpecificMethodsProvider FindDataProviderSpecificMethodsProvider<T>(IQueryable<T> query, IEnumerable<IDataProviderSpecificMethodsProvider> dataProviderSpecificMethodsProviders)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));

            if (dataProviderSpecificMethodsProviders == null)
                throw new ArgumentNullException(nameof(dataProviderSpecificMethodsProviders));

            return dataProviderSpecificMethodsProviders
                .FirstOrDefault(dataProviderSpecificMethodsProvider => dataProviderSpecificMethodsProvider.SupportsQueryable<T>(query));
        }

        public virtual Task<long> GetCountAsync<T>(IQueryable<T> query, IDataProviderSpecificMethodsProvider dataProviderSpecificMethodsProvider, CancellationToken cancellationToken)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));

            if (dataProviderSpecificMethodsProvider == null)
                throw new ArgumentNullException(nameof(dataProviderSpecificMethodsProvider));

            return dataProviderSpecificMethodsProvider.LongCountAsync(query, cancellationToken);
        }

        public virtual async Task<object> GetResult<T>(IQueryable<T> query, IDataProviderSpecificMethodsProvider dataProviderSpecificMethodsProvider, int? takeCount, int? skipCount, CancellationToken cancellationToken)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));

            if (dataProviderSpecificMethodsProvider == null)
                throw new ArgumentNullException(nameof(dataProviderSpecificMethodsProvider));

            if (skipCount != null)
            {
                query = dataProviderSpecificMethodsProvider.Skip(query, skipCount.Value);
            }

            if (takeCount != null)
            {
                query = dataProviderSpecificMethodsProvider.Take(query, takeCount.Value);
            }

            return await dataProviderSpecificMethodsProvider.ToArrayAsync(query, cancellationToken).ConfigureAwait(false);
        }

        public virtual async Task<object> FirstAsync<T>(IQueryable<T> query, IDataProviderSpecificMethodsProvider dataProviderSpecificMethodsProvider, CancellationToken cancellationToken)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));

            if (dataProviderSpecificMethodsProvider == null)
                throw new ArgumentNullException(nameof(dataProviderSpecificMethodsProvider));

            object? result = await dataProviderSpecificMethodsProvider.FirstOrDefaultAsync(query, cancellationToken).ConfigureAwait(false);

            if (result == null)
                throw new ResourceNotFoundException();

            return result;
        }

        public virtual IQueryable<T> ToQueryable<T>(IEnumerable<T> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return source.AsQueryable();
        }

        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {

        }
    }
}
