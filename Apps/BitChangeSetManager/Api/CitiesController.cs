using Bit.Core.Contracts;
using Bit.Data.Contracts;
using Bit.OData.Contents;
using Bit.OData.Contracts;
using Bit.OData.ODataControllers;
using BitChangeSetManager.Dto;
using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Net;
using System.Net.Http;
using System.Threading;

namespace BitChangeSetManager.Api
{
    public class CitiesController : DtoController<CityDto>
    {
        /*[Get]
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
        }*/

        [Get]
        [ReturnType(typeof(IEnumerable<CityDto>))]
        public HttpResponseMessage GetAll(CancellationToken cancellationToken)
        {
            ODataSqlJsonQuery odataSqlQuery = ODataSqlBuilder.BuildSqlJsonQuery(GetODataQueryOptions(), tableName: "bit.Cities");

            string connectionString = AppEnvironmentProvider.GetActiveAppEnvironment().GetConfig<string>("BitChangeSetManagerDbConnectionString");

            BitODataStreamContent responseContent = new BitODataStreamContent(async (stream) =>
            {
                DbConnection dbConnection = await DbConnectionProvider.GetDbConnectionAsync(connectionString, true, cancellationToken);

                DbTransaction dbTransaction = DbConnectionProvider.GetDbTransaction(connectionString);

                await (await dbConnection.ExecuteReaderAsync(odataSqlQuery.SqlJsonQuery, odataSqlQuery.SqlQuery.Parts.Parameters, transaction: dbTransaction)).PopulateStreamAsync(stream, cancellationToken);

            }, cancellationToken);

            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = responseContent
            };

            response.Headers.Add("OData-Version", "4.0");

            return response;
        }

        public IODataSqlBuilder ODataSqlBuilder { get; set; }

        public IDbConnectionProvider DbConnectionProvider { get; set; }

        public IAppEnvironmentProvider AppEnvironmentProvider { get; set; }
    }
}