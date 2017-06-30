using Bit.Core.Contracts;
using Bit.Data.Contracts;
using Bit.OData.ActionFilters;
using Bit.OData.Contracts;
using Bit.OData.ODataControllers;
using BitChangeSetManager.Dto;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BitChangeSetManager.Api
{
    public class CitiesController : DtoController<CityDto>
    {
        [Get]
        [IgnoreODataEnableQuery]
        public async Task<IEnumerable<CityDto>> GetAll(CancellationToken cancellationToken)
        {
            ODataSqlQuery odataSqlQuery = ODataSqlBuilder.BuildSqlQuery(GetODataQueryOptions(), tableName: "bit.Cities");

            string connectionString = AppEnvironmentProvider.GetActiveAppEnvironment().GetConfig<string>("BitChangeSetManagerDbConnectionString");

            DbConnection dbConnection = await DbConnectionProvider.GetDbConnectionAsync(connectionString, true, cancellationToken);

            DbTransaction dbTransaction = DbConnectionProvider.GetDbTransaction(connectionString);

            IEnumerable<CityDto> cities = await dbConnection.QueryAsync<CityDto>(odataSqlQuery.SelectQuery, odataSqlQuery.Parts.Parameters, transaction: dbTransaction);

            long total = odataSqlQuery.Parts.GetTotalCountFromDb == false ? cities.LongCount() : ((await dbConnection.ExecuteScalarAsync<long?>(odataSqlQuery.SelectTotalCountQuery, odataSqlQuery.Parts.Parameters, transaction: dbTransaction)) ?? 0);

            Request.Properties["System.Web.OData.TotalCountFunc"] = new Func<long>(() => total);

            return cities;
        }

        public IODataSqlBuilder ODataSqlBuilder { get; set; }

        public IDbConnectionProvider DbConnectionProvider { get; set; }

        public IAppEnvironmentProvider AppEnvironmentProvider { get; set; }
    }
}