using System.Collections.Generic;
using System.Web.OData.Query;

namespace Bit.Api.Middlewares.WebApi.OData.Contracts
{
    public class ODataSqlQueryParts
    {
        public string SelectionClause { get; set; }

        public string WhereClause { get; set; }

        public string OrderByClause { get; set; }

        public long? Top { get; set; }

        public long? Skip { get; set; }

        public bool GetTotalCountFromDb { get; set; }

        public IDictionary<string, object> Parameters { get; set; }
    }

    public class ODataSqlQuery
    {
        public ODataSqlQueryParts Parts { get; set; }

        public string SelectQuery { get; set; }

        public string SelectTotalCountQuery { get; set; }
    }

    public interface IODataSqlBuilder
    {
        ODataSqlQueryParts BuildSqlQueryParts<TDto>(ODataQueryOptions<TDto> odataQuery)
            where TDto : class;

        ODataSqlQuery BuildSqlQuery<TDto>(ODataQueryOptions<TDto> queryOptions, string tableName)
            where TDto : class;
    }
}
