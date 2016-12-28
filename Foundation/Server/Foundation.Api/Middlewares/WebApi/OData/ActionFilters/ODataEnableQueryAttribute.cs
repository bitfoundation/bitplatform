using Foundation.DataAccess.Contracts;
using Microsoft.Owin;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Filters;
using System.Web.OData;
using System.Web.OData.Extensions;
using System.Web.OData.Query;

namespace Foundation.Api.Middlewares.WebApi.OData.ActionFilters
{
    public class ODataEnableQueryAttribute : EnableQueryAttribute
    {
        public override async Task OnActionExecutedAsync(HttpActionExecutedContext actionExecutedContext, CancellationToken cancellationToken)
        {
            if (actionExecutedContext?.Response?.Content is ObjectContent &&
                actionExecutedContext?.Response?.IsSuccessStatusCode == true)
            {
                ObjectContent objContent = ((ObjectContent)(actionExecutedContext.Response.Content));

                if (objContent.Value == null)
                {
                    actionExecutedContext.Response.StatusCode = HttpStatusCode.NoContent;
                    actionExecutedContext.Response.Content = null;
                }
                else
                {
                    TypeInfo actionReturnType = objContent.Value.GetType().GetTypeInfo();

                    if (typeof(string) != actionReturnType && typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(actionReturnType))
                    {
                        bool isIQueryable = typeof(IQueryable).GetTypeInfo().IsAssignableFrom(actionReturnType);

                        TypeInfo queryElementType = null;
                        IAsyncQueryableExecuter asyncQueryableExecuterToUse = null;

                        if (isIQueryable == true)
                        {
                            queryElementType = actionReturnType.GetGenericArguments().Single().GetTypeInfo();
                            IEnumerable<IAsyncQueryableExecuter> asyncQueryableExecuters = actionExecutedContext.Request.GetOwinContext().GetDependencyResolver().ResolveAll<IAsyncQueryableExecuter>();
                            asyncQueryableExecuterToUse = typeof(ODataEnableQueryAttribute).GetMethod(nameof(FindAsyncQueryableExecuter)).MakeGenericMethod(queryElementType).Invoke(this, new object[] { objContent.Value, asyncQueryableExecuters }) as IAsyncQueryableExecuter;
                        }

                        if (isIQueryable && asyncQueryableExecuterToUse != null)
                        {
                            HttpRequestMessageProperties requestODataProps = actionExecutedContext.Request.ODataProperties();
                            ODataQueryContext currentOdataQueryContext = new ODataQueryContext(actionExecutedContext.Request.GetModel(), queryElementType, requestODataProps.Path);
                            ODataQueryOptions currentOdataQueryOptions = new ODataQueryOptions(currentOdataQueryContext, actionExecutedContext.Request);
                            ODataQuerySettings globalODataQuerySettings = new ODataQuerySettings
                            {
                                EnableConstantParameterization = this.EnableConstantParameterization,
                                EnsureStableOrdering = this.EnsureStableOrdering,
                                HandleNullPropagation = this.HandleNullPropagation,
                                PageSize = this.PageSize
                            };

                            ValidateQuery(actionExecutedContext.Request, currentOdataQueryOptions);

                            int? currentQueryPageSize = currentOdataQueryOptions?.Top?.Value;
                            int? globalQuerypageSize = globalODataQuerySettings.PageSize;
                            int? pageSize = null;

                            if (currentQueryPageSize.HasValue && globalQuerypageSize.HasValue)
                                pageSize = currentQueryPageSize.Value < globalQuerypageSize.Value ? currentQueryPageSize.Value : globalQuerypageSize.Value;
                            else if (globalQuerypageSize.HasValue == true && currentQueryPageSize.HasValue == false)
                                pageSize = globalQuerypageSize.Value;
                            else if (globalQuerypageSize.HasValue == false && currentQueryPageSize.HasValue == true)
                                pageSize = currentQueryPageSize.Value;
                            else
                                pageSize = null;

                            globalODataQuerySettings.PageSize = null; // ApplyTo will enumerates the query for values other than null

                            if (currentOdataQueryOptions.Filter != null)
                            {
                                objContent.Value = currentOdataQueryOptions.Filter.ApplyTo(query: (IQueryable)objContent.Value, querySettings: globalODataQuerySettings);
                            }

                            if (currentOdataQueryOptions.Count?.RawValue == "true" && pageSize.HasValue == true)
                            {
                                long count = await (Task<long>)typeof(ODataEnableQueryAttribute).GetMethod(nameof(GetCountAsync)).MakeGenericMethod(queryElementType).Invoke(this, new object[] { objContent.Value, asyncQueryableExecuterToUse, cancellationToken });

                                actionExecutedContext.Request.Properties["System.Web.OData.TotalCountFunc"] = new Func<long>(() => count);
                            }

                            objContent.Value = currentOdataQueryOptions.ApplyTo(query: (IQueryable)objContent.Value, ignoreQueryOptions: AllowedQueryOptions.Filter, querySettings: globalODataQuerySettings);

                            if (currentOdataQueryOptions.SelectExpand != null)
                                queryElementType = objContent.Value.GetType().GetTypeInfo().GetGenericArguments().Single().GetTypeInfo();

                            objContent.Value = await (Task<object>)typeof(ODataEnableQueryAttribute).GetMethod(nameof(ToListAsync)).MakeGenericMethod(queryElementType).Invoke(this, new object[] { objContent.Value, asyncQueryableExecuterToUse, pageSize, cancellationToken });

                            if (currentOdataQueryOptions.Count?.RawValue == "true" && pageSize.HasValue == false)
                            {
                                // We've no paging becuase there is no global config for max top and there is no top specified by the client's request, so the retured result of query's length is equivalent to total count of the query
                                long count = ((IList)objContent.Value).Count;
                                actionExecutedContext.Request.Properties["System.Web.OData.TotalCountFunc"] = new Func<long>(() => count);
                            }
                        }
                        else
                        {
                            base.OnActionExecuted(actionExecutedContext);
                        }
                    }
                }
            }
        }

        public virtual IAsyncQueryableExecuter FindAsyncQueryableExecuter<T>(IQueryable<T> query, IEnumerable<IAsyncQueryableExecuter> asyncQueryableExecuters)
            where T : class
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));

            if (asyncQueryableExecuters == null)
                throw new ArgumentNullException(nameof(asyncQueryableExecuters));

            return asyncQueryableExecuters
                .FirstOrDefault(asyncQueryableExecuter => asyncQueryableExecuter.SupportsAsyncExecution<T>(query));
        }

        public virtual async Task<long> GetCountAsync<T>(IQueryable<T> query, IAsyncQueryableExecuter asyncQueryableExecuter, CancellationToken cancellationToken)
            where T : class
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));

            if (asyncQueryableExecuter == null)
                throw new ArgumentNullException(nameof(asyncQueryableExecuter));

            return await asyncQueryableExecuter.LongCountAsync(query, cancellationToken);
        }

        public virtual async Task<object> ToListAsync<T>(IQueryable<T> query, IAsyncQueryableExecuter asyncQueryableExecuter, int? pageSize, CancellationToken cancellationToken)
            where T : class
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));

            if (asyncQueryableExecuter == null)
                throw new ArgumentNullException(nameof(asyncQueryableExecuter));

            if (pageSize.HasValue == true)
            {
                query = query.Take(pageSize.Value);
            }

            return await asyncQueryableExecuter.ToListAsync(query, cancellationToken);
        }

        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {

        }
    }
}
