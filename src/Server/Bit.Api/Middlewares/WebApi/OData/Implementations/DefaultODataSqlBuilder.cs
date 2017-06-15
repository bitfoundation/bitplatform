using Bit.Api.Middlewares.WebApi.OData.Contracts;
using Bit.Owin.Exceptions;
using LambdaSqlBuilder;
using LambdaSqlBuilder.ValueObjects;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Web.OData.Query;

namespace Bit.Api.Middlewares.WebApi.OData.Implementations
{
    public class DefaultODataSqlBuilder : IODataSqlBuilder
    {
        public DefaultODataSqlBuilder()
        {
            SqlLamBase.SetAdapter(SqlAdapter.SqlServer2012);
        }

        public virtual (string Columns, string Where, string OrderBy, long? Top, long? Skip, IDictionary<string, object> Parameters) BuildSqlQueryParts<TDto>(ODataQueryOptions<TDto> odataQuery)
            where TDto : class
        {
            if (odataQuery == null)
                throw new ArgumentNullException(nameof(odataQuery));

            string columns = "*";
            string orderBy = null, where = null;
            long? top = odataQuery.Top?.Value;
            long? skip = odataQuery.Skip?.Value;
            IDictionary<string, object> parameters = null;

            if (odataQuery.SelectExpand != null && odataQuery.SelectExpand.SelectExpandClause.AllSelected != true)
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

                where = string.Join("", sqlQuery.SqlBuilder.WhereConditions);
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

            return new ValueTuple<string, string, string, long?, long?, IDictionary<string, object>>(columns, where, orderBy, top, skip, parameters);
        }

        public virtual (string Select, string SelectCount, bool SelectCountFromDb, IDictionary<string, object> Parameters) BuildSqlQuery<TDto>(ODataQueryOptions<TDto> queryOptions, string tableName)
            where TDto : class
        {
            if (queryOptions == null)
                throw new ArgumentNullException(nameof(queryOptions));

            TypeInfo dtoType = typeof(TDto).GetTypeInfo();

            var sqlQuery = BuildSqlQueryParts(queryOptions);

            if (sqlQuery.Skip != null)
            {
                if (sqlQuery.Top == null)
                    throw new BadRequestException("$top is not provided while there is a $skip");

                if (sqlQuery.OrderBy == null)
                    throw new BadRequestException("$orderby is not provided while there is a $skip");
            }

            string whereClause = sqlQuery.Where == null ? "" : $"where {sqlQuery.Where}";
            string offsetClause = sqlQuery.Skip == null ? "" : $"offset {sqlQuery.Skip} rows";
            string topClause = "", fetchRowClause = "";
            if (sqlQuery.Top != null)
            {
                if (sqlQuery.Skip != null)
                    fetchRowClause = $"fetch next {sqlQuery.Top} rows only";
                else
                    topClause = $"top({sqlQuery.Top})";
            }
            string orderByClause = sqlQuery.OrderBy == null ? "" : $"order by {sqlQuery.OrderBy}";

            string select = $"select {topClause} {sqlQuery.Columns} from {tableName} as [{dtoType.Name}] {whereClause} {orderByClause} {offsetClause} {fetchRowClause}";

            string selectCount = $"select count_big(1) from {tableName} as [{dtoType.Name}] {whereClause}";

            bool selectCountFromDb = queryOptions.Count?.Value == true && sqlQuery.Top != null;

            return new ValueTuple<string, string, bool, IDictionary<string, object>>(select, selectCount, selectCountFromDb, sqlQuery.Parameters);
        }
    }

    class OrderByFinder<TDto> : ExpressionVisitor
        where TDto : class
    {
        private readonly SqlLam<TDto> _sqlQuery;

        public OrderByFinder(SqlLam<TDto> sqlQuery)
        {
            if (sqlQuery == null)
                throw new ArgumentNullException(nameof(sqlQuery));

            _sqlQuery = sqlQuery;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            Expression result = base.VisitMethodCall(node);

            if (node.Method.Name == nameof(Enumerable.OrderBy) || node.Method.Name == nameof(Enumerable.ThenBy))
                _sqlQuery.OrderBy(ChangeType((LambdaExpression)node.Arguments.OfType<UnaryExpression>().Single().Operand));

            if (node.Method.Name == nameof(Enumerable.OrderByDescending) || node.Method.Name == nameof(Enumerable.ThenByDescending))
                _sqlQuery.OrderByDescending(ChangeType((LambdaExpression)node.Arguments.OfType<UnaryExpression>().Single().Operand));

            return result;
        }

        public Expression<Func<TDto, object>> ChangeType(LambdaExpression expression)
        {
            Expression converted = Expression.Convert(expression.Body, typeof(object));
            return Expression.Lambda<Func<TDto, object>>(converted, expression.Parameters);
        }
    }

    class WhereExpressionFinder<TDto> : ExpressionVisitor
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

    class ExtraMethodCallRemover<TDto> : ExpressionVisitor
        where TDto : class
    {
        protected override Expression VisitUnary(UnaryExpression node)
        {
            if (node?.NodeType == ExpressionType.Convert)
                return (MemberExpression)node.Operand;

            return base.VisitUnary(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.Name == nameof(string.ToLower) || node.Method.Name == nameof(string.ToUpper))
            {
                if (node.Object is UnaryExpression)
                    return VisitUnary((UnaryExpression)node.Object);
                else
                    return node.Object;
            }
            return base.VisitMethodCall(node);
        }
    }
}
