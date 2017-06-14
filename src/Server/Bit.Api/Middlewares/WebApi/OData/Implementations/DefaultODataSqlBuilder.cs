using Bit.Api.Middlewares.WebApi.OData.Contracts;
using LambdaSqlBuilder;
using LambdaSqlBuilder.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web.OData.Query;

namespace Bit.Api.Middlewares.WebApi.OData.Implementations
{
    public class DefaultODataSqlBuilder : IODataSqlBuilder
    {
        public DefaultODataSqlBuilder()
        {
            SqlLamBase.SetAdapter(SqlAdapter.SqlServer2012);
        }

        public void BuildSqlQuery<TDto>(ODataQueryOptions<TDto> odataQuery, out string columns, out string where, out string orderBy,
                                                                            out long? top, out long? skip, out IDictionary<string, object> parameters)
            where TDto : class
        {
            if (odataQuery == null)
                throw new ArgumentNullException(nameof(odataQuery));

            columns = "*";
            orderBy = where = null;
            top = odataQuery.Top?.Value;
            skip = odataQuery.Skip?.Value;
            parameters = null;

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

                ConvertCallRemover<TDto> convertCallRemover = new ConvertCallRemover<TDto>();
                whereFinder.Where = (Expression<Func<TDto, bool>>)convertCallRemover.Visit(whereFinder.Where);

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

    class ConvertCallRemover<TDto> : ExpressionVisitor
        where TDto : class
    {
        protected override Expression VisitUnary(UnaryExpression node)
        {
            if (node?.NodeType == ExpressionType.Convert)
                return (MemberExpression)(node as UnaryExpression).Operand;

            return base.VisitUnary(node);
        }
    }
}
