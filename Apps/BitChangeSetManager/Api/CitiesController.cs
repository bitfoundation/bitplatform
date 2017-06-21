using Bit.Api.Middlewares.WebApi.OData.ActionFilters;
using Bit.Api.Middlewares.WebApi.OData.Contracts;
using Bit.Core.Contracts;
using Bit.Data.Contracts;
using BitChangeSetManager.DataAccess;
using BitChangeSetManager.Dto;
using BitChangeSetManager.Model;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BitChangeSetManager.Api
{
    public class CitiesController : DefaultReadOnlyDtoSetController<City, CityDto>
    {
        public CitiesController(IBitChangeSetManagerRepository<City> repository)
            : base(repository)
        {

        }

        [IgnoreODataEnableQuery]
        public override async Task<IQueryable<CityDto>> GetAll(CancellationToken cancellationToken)
        {
            var sqlQuery = ODataSqlBuilder.BuildSqlQuery(GetODataQueryOptions(), tableName: "bit.Cities");

            string connectionString = AppEnvironmentProvider.GetActiveAppEnvironment().GetConfig<string>("BitChangeSetManagerDbConnectionString");

            DbConnection dbConnection = await DbConnectionProvider.GetDbConnectionAsync(connectionString, true, cancellationToken);

            DbTransaction dbTransaction = DbConnectionProvider.GetDbTransaction(connectionString);

            IEnumerable<CityDto> cities = await dbConnection.QueryAsync<CityDto>(sqlQuery.SelectQuery, sqlQuery.Parts.Parameters, transaction: dbTransaction);

            long total = sqlQuery.Parts.GetTotalCountFromDb == false ? cities.LongCount() : ((await dbConnection.ExecuteScalarAsync<long?>(sqlQuery.SelectTotalCountQuery, sqlQuery.Parts.Parameters, transaction: dbTransaction)) ?? 0);

            Request.Properties["System.Web.OData.TotalCountFunc"] = new Func<long>(() => total);

            return cities.AsQueryable();
        }

        public IODataSqlBuilder ODataSqlBuilder { get; set; }

        public IDbConnectionProvider DbConnectionProvider { get; set; }

        public IAppEnvironmentProvider AppEnvironmentProvider { get; set; }
    }
}