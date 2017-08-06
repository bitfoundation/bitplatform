using Bit.Data.Contracts;
using Bit.Data.Implementations;
using Bit.Owin.Exceptions;
using Microsoft.Owin;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Filters;
using System.Web.OData;
using System.Web.OData.Extensions;
using System.Web.OData.Query;

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

        public override async Task OnActionExecutedAsync(HttpActionExecutedContext actionExecutedContext, CancellationToken cancellationToken)
        {
            if (actionExecutedContext.Response?.Content is ObjectContent &&
                actionExecutedContext.Response.IsSuccessStatusCode == true &&
                !actionExecutedContext.Request.Properties.ContainsKey("IgnoreODataEnableQuery"))
            {
                ObjectContent objContent = ((ObjectContent)(actionExecutedContext.Response.Content));

                if (objContent.Value == null)
                    return;

                bool isSingleResult = objContent.Value is SingleResult;
                bool isCountRequest = actionExecutedContext.Request.RequestUri.LocalPath.Contains("/$count") == true;

                if (isSingleResult == true)
                    objContent.Value = ((SingleResult)objContent.Value).Queryable;

                TypeInfo actionReturnType = objContent.Value.GetType().GetTypeInfo();

                if (typeof(string) != actionReturnType && typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(actionReturnType))
                {
                    TypeInfo queryElementType = actionReturnType.HasElementType ? actionReturnType.GetElementType().GetTypeInfo() : actionReturnType.GetGenericArguments().First() /* Why not calling Single() ? http://stackoverflow.com/questions/41718323/why-variable-of-type-ienumerablesomething-gettype-getgenericargsuments-c */.GetTypeInfo();

                    bool isIQueryable = typeof(IQueryable).GetTypeInfo().IsAssignableFrom(actionReturnType);

                    IDataProviderSpecificMethodsProvider dataProviderSpecificMethodsProvider = null;

                    if (isIQueryable == true)
                    {
                        IEnumerable<IDataProviderSpecificMethodsProvider> dataProviderSpecificMethodsProviders = actionExecutedContext.Request.GetOwinContext().GetDependencyResolver().ResolveAll<IDataProviderSpecificMethodsProvider>();
                        dataProviderSpecificMethodsProvider = (IDataProviderSpecificMethodsProvider)typeof(ODataEnableQueryAttribute).GetMethod(nameof(FindDataProviderSpecificMethodsProvider)).MakeGenericMethod(queryElementType).Invoke(this, new object[] { objContent.Value, dataProviderSpecificMethodsProviders });
                    }
                    else
                    {
                        objContent.Value = typeof(ODataEnableQueryAttribute).GetMethod(nameof(ToQueryable)).MakeGenericMethod(queryElementType).Invoke(this, new [] { objContent.Value });
                    }

                    if (dataProviderSpecificMethodsProvider == null)
                        dataProviderSpecificMethodsProvider = DefaultDataProviderSpecificMethodsProvider.Current;

                    HttpRequestMessageProperties requestODataProps = actionExecutedContext.Request.ODataProperties();
                    ODataQueryContext currentOdataQueryContext = new ODataQueryContext(actionExecutedContext.Request.GetModel(), queryElementType, requestODataProps.Path);
                    ODataQueryOptions currentOdataQueryOptions = new ODataQueryOptions(currentOdataQueryContext, actionExecutedContext.Request);
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

                    if (currentQueryPageSize.HasValue)
                        takeCount = currentQueryPageSize.Value;
                    else if (globalQuerypageSize.HasValue == true)
                        takeCount = globalQuerypageSize.Value;
                    else
                        takeCount = null;

                    globalODataQuerySettings.PageSize = null; // ApplyTo will enumerates the query for values other than null. We are gonna apply take in ToList & ToListAsync methods.

                    if (currentOdataQueryOptions.Filter != null)
                    {
                        objContent.Value = currentOdataQueryOptions.Filter.ApplyTo(query: (IQueryable)objContent.Value, querySettings: globalODataQuerySettings);
                    }

                    if (isCountRequest == true)
                    {
                        objContent.Value = await (Task<long>)typeof(ODataEnableQueryAttribute).GetMethod(nameof(GetCountAsync)).MakeGenericMethod(queryElementType).Invoke(this, new [] { objContent.Value, dataProviderSpecificMethodsProvider, cancellationToken });
                        return;
                    }

                    if (currentOdataQueryOptions.Count?.Value == true && takeCount.HasValue == true && isSingleResult == false)
                    {
                        long count = await (Task<long>)typeof(ODataEnableQueryAttribute).GetMethod(nameof(GetCountAsync)).MakeGenericMethod(queryElementType).Invoke(this, new [] { objContent.Value, dataProviderSpecificMethodsProvider, cancellationToken });
                        actionExecutedContext.Request.Properties["System.Web.OData.TotalCountFunc"] = new Func<long>(() => count);
                    }

                    objContent.Value = currentOdataQueryOptions.ApplyTo(query: (IQueryable)objContent.Value, querySettings: globalODataQuerySettings, ignoreQueryOptions: AllowedQueryOptions.Filter | AllowedQueryOptions.Skip | AllowedQueryOptions.Top);

                    if (currentOdataQueryOptions.SelectExpand != null)
                    {
                        TypeInfo newReturnTypeAfterApplyingSelect = objContent.Value.GetType().GetTypeInfo();
                        queryElementType = newReturnTypeAfterApplyingSelect.GetGenericArguments().ExtendedSingle($"Get generic arguments of ${newReturnTypeAfterApplyingSelect.Name}").GetTypeInfo();
                    }

                    if (isSingleResult == false)
                        objContent.Value = await (Task<object>)typeof(ODataEnableQueryAttribute).GetMethod(nameof(ToListAsync)).MakeGenericMethod(queryElementType).Invoke(this, new [] { objContent.Value, dataProviderSpecificMethodsProvider, takeCount, skipCount, cancellationToken });
                    else
                    {
                        objContent.Value = await (Task<object>)typeof(ODataEnableQueryAttribute).GetMethod(nameof(FirstAsync)).MakeGenericMethod(queryElementType).Invoke(this, new [] { objContent.Value, dataProviderSpecificMethodsProvider, cancellationToken });
                        actionExecutedContext.Request.Properties["System.Web.OData.TotalCountFunc"] = new Func<long>(() => 1);
                    }

                    if (currentOdataQueryOptions.Count?.Value == true && takeCount.HasValue == false && isSingleResult == false)
                    {
                        // We've no paging becuase there is no global config for max top and there is no top specified by the client's request, so the retured result of query's length is equivalent to total count of the query
                        long count = ((IList)objContent.Value).Count;
                        actionExecutedContext.Request.Properties["System.Web.OData.TotalCountFunc"] = new Func<long>(() => count);
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

        public virtual async Task<long> GetCountAsync<T>(IQueryable<T> query, IDataProviderSpecificMethodsProvider dataProviderSpecificMethodsProvider, CancellationToken cancellationToken)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));

            if (dataProviderSpecificMethodsProvider == null)
                throw new ArgumentNullException(nameof(dataProviderSpecificMethodsProvider));

            return await dataProviderSpecificMethodsProvider.LongCountAsync(query, cancellationToken).ConfigureAwait(false);
        }

        public virtual async Task<object> ToListAsync<T>(IQueryable<T> query, IDataProviderSpecificMethodsProvider dataProviderSpecificMethodsProvider, int? takeCount, int? skipCount, CancellationToken cancellationToken)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));

            if (dataProviderSpecificMethodsProvider == null)
                throw new ArgumentNullException(nameof(dataProviderSpecificMethodsProvider));

            if (skipCount.HasValue == true)
            {
                query = dataProviderSpecificMethodsProvider.Skip(query, skipCount.Value);
            }

            if (takeCount.HasValue == true)
            {
                query = dataProviderSpecificMethodsProvider.Take(query, takeCount.Value);
            }

            return await dataProviderSpecificMethodsProvider.ToListAsync(query, cancellationToken).ConfigureAwait(false);
        }

        public virtual async Task<object> FirstAsync<T>(IQueryable<T> query, IDataProviderSpecificMethodsProvider dataProviderSpecificMethodsProvider, CancellationToken cancellationToken)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));

            if (dataProviderSpecificMethodsProvider == null)
                throw new ArgumentNullException(nameof(dataProviderSpecificMethodsProvider));

            object result = await dataProviderSpecificMethodsProvider.FirstOrDefaultAsync(query, cancellationToken).ConfigureAwait(false);

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
