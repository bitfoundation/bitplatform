using System.Collections.Generic;
using System.Web.OData.Query;

namespace Bit.Api.Middlewares.WebApi.OData.Contracts
{
    public interface IODataSqlBuilder
    {
        (string Columns, string Where, string OrderBy, long? Top, long? Skip, IDictionary<string, object> Parameters) BuildSqlQueryParts<TDto>(ODataQueryOptions<TDto> odataQuery)
            where TDto : class;

        (string Select, string SelectCount, bool SelectCountFromDb, IDictionary<string, object> Parameters) BuildSqlQuery<TDto>(ODataQueryOptions<TDto> queryOptions, string tableName)
            where TDto : class;
    }
}
