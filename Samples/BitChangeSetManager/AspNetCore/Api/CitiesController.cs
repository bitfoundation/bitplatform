using Bit.Core.Models;
using Bit.Data.Contracts;
using Bit.OData.ActionFilters;
using Bit.OData.Contracts;
using Bit.OData.ODataControllers;
using BitChangeSetManager.Dto;
using Dapper;
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

            string connectionString = AppEnvironment.GetConfig<string>("BitChangeSetManagerDbConnectionString");

            DbConnection dbConnection = await DbConnectionProvider.GetDbConnectionAsync(connectionString, true, cancellationToken);

            DbTransaction dbTransaction = DbConnectionProvider.GetDbTransaction(dbConnection);

            IEnumerable<CityDto> cities = await dbConnection.QueryAsync<CityDto>(odataSqlQuery.SelectQuery, odataSqlQuery.Parts.Parameters, transaction: dbTransaction);

            long total = odataSqlQuery.Parts.GetTotalCountFromDb == false ? cities.LongCount() : ((await dbConnection.ExecuteScalarAsync<long?>(odataSqlQuery.SelectTotalCountQuery, odataSqlQuery.Parts.Parameters, transaction: dbTransaction)) ?? 0);

            SetODataTotalCount(total);

            return cities;
        }

        /*[Get]
        [ResponseType(typeof(IEnumerable<CityDto>))]
        public HttpResponseMessage GetAll(CancellationToken cancellationToken)
        {
            ODataSqlJsonQuery odataSqlQuery = ODataSqlBuilder.BuildSqlJsonQuery(GetODataQueryOptions(), tableName: "bit.Cities");

            string connectionString = AppEnvironment.GetConfig<string>("BitChangeSetManagerDbConnectionString");

            ODataPushStreamContent responseContent = new ODataPushStreamContent(async (stream) =>
            {
                DbConnection dbConnection = await DbConnectionProvider.GetDbConnectionAsync(connectionString, true, cancellationToken);

                DbTransaction dbTransaction = DbConnectionProvider.GetDbTransaction(connectionString);

                DbDataReader dbReader = (DbDataReader)(await dbConnection.ExecuteReaderAsync(odataSqlQuery.SqlJsonQuery, odataSqlQuery.SqlQuery.Parts.Parameters, transaction: dbTransaction));

                await dbReader.PopulateStreamAsync(stream, cancellationToken);

            }, cancellationToken);

            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = responseContent
            };

            response.Headers.Add("OData-Version", "4.0");

            return response;
        }*/

        public IODataSqlBuilder ODataSqlBuilder { get; set; }

        public IDbConnectionProvider DbConnectionProvider { get; set; }

        public AppEnvironment AppEnvironment { get; set; }
    }
}