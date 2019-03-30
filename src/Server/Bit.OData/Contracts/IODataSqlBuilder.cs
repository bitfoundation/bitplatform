using Bit.Core.Contracts;
using Microsoft.AspNet.OData.Query;
using Microsoft.Owin;
using System.Collections.Generic;
using System.Net.Http;

namespace Bit.OData.Contracts
{
    public class ODataSqlQueryParts
    {
        public static implicit operator ODataSqlQueryParts(ODataQueryOptions odataQuery)
        {
            IDependencyResolver dependencyResolver = odataQuery.Request.GetOwinContext().GetDependencyResolver();
            IODataSqlBuilder odataSqlBuilder = dependencyResolver.Resolve<IODataSqlBuilder>();
            ODataSqlQueryParts sqlParts = (ODataSqlQueryParts)odataSqlBuilder.GetType().GetMethod(nameof(IODataSqlBuilder.BuildSqlQueryParts))
                .MakeGenericMethod(odataQuery.Context.ElementClrType)
                .Invoke(odataSqlBuilder, new object[] { odataQuery });
            return sqlParts;
        }

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

    public class ODataSqlJsonQuery
    {
        public ODataSqlQuery SqlQuery { get; set; }

        public string SqlJsonQuery { get; set; }
    }

    public interface IODataSqlBuilder
    {
        ODataSqlQueryParts BuildSqlQueryParts<TDto>(ODataQueryOptions<TDto> odataQuery)
                    where TDto : class;

        ODataSqlQuery BuildSqlQuery<TDto>(ODataQueryOptions<TDto> queryOptions, string tableName)
            where TDto : class;

        ODataSqlJsonQuery BuildSqlJsonQuery<TDto>(ODataQueryOptions<TDto> odataQueryOptions, string tableName)
            where TDto : class;
    }
}
