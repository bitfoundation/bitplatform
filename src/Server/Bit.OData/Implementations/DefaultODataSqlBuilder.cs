using Bit.OData.Contracts;
using Bit.Owin.Exceptions;
using LambdaSqlBuilder;
using LambdaSqlBuilder.ValueObjects;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Bit.OData.Implementations
{
    public class DefaultODataSqlBuilder : IODataSqlBuilder
    {
        public DefaultODataSqlBuilder()
        {
            SqlLamBase.SetAdapter(SqlAdapter.SqlServer2012);
        }

        public virtual ODataSqlQueryParts BuildSqlQueryParts<TDto>(ODataQueryOptions<TDto> odataQuery)
            where TDto : class
        {
            if (odataQuery == null)
                throw new ArgumentNullException(nameof(odataQuery));

            const string columns = "*";
            string orderBy = null, where = null;
            long? top = odataQuery.Top?.Value;
            long? skip = odataQuery.Skip?.Value;
            IDictionary<string, object> parameters = null;

            if (odataQuery.SelectExpand?.SelectExpandClause.AllSelected == false)
                throw new BadRequestException("$select is not supported for this resource");

            if (odataQuery.Filter != null)
            {
                IQueryable<TDto> odataQueryAsLinq = Enumerable.Empty<TDto>()
                    .AsQueryable();

                IQueryable filteredOdataQueryAsLinq = odataQuery.Filter.ApplyTo(odataQueryAsLinq, new ODataQuerySettings
                {
                    EnableConstantParameterization = false,
                    EnsureStableOrdering = false,
                    HandleNullPropagation = HandleNullPropagationOption.False,
                    PageSize = null
                });

                WhereExpressionFinder<TDto> whereFinder = new WhereExpressionFinder<TDto>();
                whereFinder.Visit(filteredOdataQueryAsLinq.Expression);

                ExtraMethodCallRemover<TDto> extraMethodCallRemover = new ExtraMethodCallRemover<TDto>();
                whereFinder.Where = (Expression<Func<TDto, bool>>)extraMethodCallRemover.Visit(whereFinder.Where);

                SqlLam<TDto> sqlQuery = new SqlLam<TDto>(whereFinder.Where);

                where = string.Concat(sqlQuery.SqlBuilder.WhereConditions);
                parameters = sqlQuery.QueryParameters;
            }

            if (odataQuery.OrderBy != null)
            {
                IQueryable<TDto> odataQueryAsLinq = Enumerable.Empty<TDto>()
                    .AsQueryable();

                IQueryable orderedOdataQueryAsLinq = odataQuery.OrderBy.ApplyTo(odataQueryAsLinq, new ODataQuerySettings
                {
                    EnableConstantParameterization = false,
                    EnsureStableOrdering = false,
                    HandleNullPropagation = HandleNullPropagationOption.False,
                    PageSize = null
                });

                SqlLam<TDto> sqlQuery = new SqlLam<TDto>();

                OrderByFinder<TDto> orderByFinder = new OrderByFinder<TDto>(sqlQuery);
                orderByFinder.Visit(orderedOdataQueryAsLinq.Expression);

                orderBy = string.Join(",", sqlQuery.SqlBuilder.OrderByList);
            }

            bool selectCountFromDb = odataQuery.Count?.Value == true && top != null;

            return new ODataSqlQueryParts
            {
                SelectionClause = columns,
                OrderByClause = orderBy,
                Parameters = parameters,
                GetTotalCountFromDb = selectCountFromDb,
                Skip = skip,
                Top = top,
                WhereClause = where
            };
        }

        public virtual ODataSqlQuery BuildSqlQuery<TDto>(ODataQueryOptions<TDto> queryOptions, string tableName)
            where TDto : class
        {
            if (queryOptions == null)
                throw new ArgumentNullException(nameof(queryOptions));

            TypeInfo dtoType = typeof(TDto).GetTypeInfo();

            ODataSqlQueryParts sqlQueryParts = BuildSqlQueryParts(queryOptions);

            if (sqlQueryParts.Skip != null)
            {
                if (sqlQueryParts.Top == null)
                    throw new BadRequestException("$top is not provided while there is a $skip");

                if (sqlQueryParts.OrderByClause == null)
                    throw new BadRequestException("$orderby is not provided while there is a $skip");
            }

            string whereClause = sqlQueryParts.WhereClause == null ? "" : $"where {sqlQueryParts.WhereClause}";
            string offsetClause = sqlQueryParts.Skip == null ? "" : $"offset {sqlQueryParts.Skip} rows";
            string topClause = "", fetchRowClause = "";
            if (sqlQueryParts.Top != null)
            {
                if (sqlQueryParts.Skip != null)
                    fetchRowClause = $"fetch next {sqlQueryParts.Top} rows only";
                else
                    topClause = $"top({sqlQueryParts.Top})";
            }
            string orderByClause = sqlQueryParts.OrderByClause == null ? "" : $"order by {sqlQueryParts.OrderByClause}";

            string select = $"select {topClause} {sqlQueryParts.SelectionClause} from {tableName} as [{dtoType.Name}] {whereClause} {orderByClause} {offsetClause} {fetchRowClause}";

            string selectCount = $"select count_big(1) from {tableName} as [{dtoType.Name}] {whereClause}";

            return new ODataSqlQuery
            {
                Parts = sqlQueryParts,
                SelectQuery = select,
                SelectTotalCountQuery = selectCount
            };
        }

        public virtual ODataSqlJsonQuery BuildSqlJsonQuery<TDto>(ODataQueryOptions<TDto> odataQueryOptions, string tableName) where TDto : class
        {
            ODataSqlQuery odataSqlQuery = BuildSqlQuery(odataQueryOptions, tableName: tableName);

            string edmTypeFullPath = $"{odataQueryOptions.Request.GetReaderSettings().BaseUri}$metadata#{odataQueryOptions.Context.Path}";

            string finalSelectCountQuery = odataSqlQuery.Parts.GetTotalCountFromDb ? $"({odataSqlQuery.SelectTotalCountQuery}) as '@odata.count' ," : "";

            string sqlJsonQuery = $@"select * from (select '{edmTypeFullPath}' as '@odata.context', 
              {finalSelectCountQuery}
              ({odataSqlQuery.SelectQuery} FOR JSON PATH) as 'value') as ODataResult
FOR JSON AUTO, WITHOUT_ARRAY_WRAPPER";

            return new ODataSqlJsonQuery
            {
                SqlJsonQuery = sqlJsonQuery,
                SqlQuery = odataSqlQuery
            };
        }
    }

    internal class OrderByFinder<TDto> : ExpressionVisitor
        where TDto : class
    {
        private readonly SqlLam<TDto> _sqlQuery;

        public OrderByFinder(SqlLam<TDto> sqlQuery)
        {
            _sqlQuery = sqlQuery ?? throw new ArgumentNullException(nameof(sqlQuery));
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            Expression result = base.VisitMethodCall(node);

            if (node.Method.Name == nameof(Enumerable.OrderBy) || node.Method.Name == nameof(Enumerable.ThenBy))
                _sqlQuery.OrderBy(ChangeType((LambdaExpression)node.Arguments.OfType<UnaryExpression>().ExtendedSingle($"Finding Operand in {node.Method.Name}").Operand));

            if (node.Method.Name == nameof(Enumerable.OrderByDescending) || node.Method.Name == nameof(Enumerable.ThenByDescending))
                _sqlQuery.OrderByDescending(ChangeType((LambdaExpression)node.Arguments.OfType<UnaryExpression>().ExtendedSingle($"Finding Operand in {node.Method.Name}").Operand));

            return result;
        }

        public Expression<Func<TDto, object>> ChangeType(LambdaExpression expression)
        {
            Expression converted = Expression.Convert(expression.Body, typeof(object));
            return Expression.Lambda<Func<TDto, object>>(converted, expression.Parameters);
        }
    }

    internal class WhereExpressionFinder<TDto> : ExpressionVisitor
        where TDto : class
    {
        public Expression<Func<TDto, bool>> Where { get; set; }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            if (Where == null)
                Where = node as Expression<Func<TDto, bool>>;
            else
                throw new InvalidOperationException("Expression may not contains multiple lambda expressions");

            return base.VisitLambda(node);
        }
    }

    internal class ExtraMethodCallRemover<TDto> : ExpressionVisitor
        where TDto : class
    {
        protected override Expression VisitUnary(UnaryExpression node)
        {
            if (node.NodeType == ExpressionType.Convert)
                return (MemberExpression)node.Operand;

            return base.VisitUnary(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.Name == nameof(string.ToLower) || node.Method.Name == nameof(string.ToUpper))
            {
                if (node.Object is UnaryExpression unaryExpression)
                    return VisitUnary(unaryExpression);
                else
                    return node.Object;
            }
            return base.VisitMethodCall(node);
        }
    }
}
