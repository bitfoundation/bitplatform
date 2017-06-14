using System.Collections.Generic;
using System.Web.OData.Query;

namespace Bit.Api.Middlewares.WebApi.OData.Contracts
{
    public interface IODataSqlBuilder
    {
        void BuildSqlQuery<TDto>(ODataQueryOptions<TDto> odataQuery, out string columns, out string where, out string orderBy, out long? top, out long? skip, out IDictionary<string, object> parameters)
            where TDto : class;
    }
}
