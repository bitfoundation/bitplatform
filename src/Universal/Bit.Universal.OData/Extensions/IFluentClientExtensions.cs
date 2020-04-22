using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Simple.OData.Client
{
    public static class IFluentClientExtensions
    {
        public static Task<T> CreateEntryAsync<T>(this IBoundClient<T> client, bool resultRequired, CancellationToken cancellationToken)
            where T : class
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            return client.InsertEntryAsync(resultRequired, cancellationToken);
        }

        public static Task<T> CreateEntryAsync<T>(this IBoundClient<T> client)
            where T : class
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            return client.InsertEntryAsync();
        }

        public static Task<T> CreateEntryAsync<T>(this IBoundClient<T> client, CancellationToken cancellationToken)
            where T : class
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            return client.InsertEntryAsync(cancellationToken);
        }

        public static Task<T> CreateEntryAsync<T>(this IBoundClient<T> client, bool resultRequired)
            where T : class
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            return client.InsertEntryAsync(resultRequired);
        }

        public static IBoundClient<T> Include<T>(this IBoundClient<T> client, ODataExpandOptions includeOptions, Expression<Func<T, object?>> expression)
            where T : class
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            return client.Expand(expandOptions: includeOptions, expression: expression);
        }

        public static IBoundClient<T> Include<T>(this IBoundClient<T> client, Expression<Func<T, object?>> expression)
            where T : class
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            return client.Expand(expression: expression);
        }

        public static IBoundClient<T> Include<T>(this IBoundClient<T> client, IEnumerable<string> associations)
            where T : class
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            return client.Expand(associations: associations);
        }

        public static IBoundClient<T> Include<T>(this IBoundClient<T> client, ODataExpandOptions includeOptions, IEnumerable<string> associations)
            where T : class
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            return client.Expand(expandOptions: includeOptions, associations: associations);
        }

        public static IBoundClient<T> Include<T>(this IBoundClient<T> client, params string[] associations)
            where T : class
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            return client.Expand(associations: associations);
        }

        public static IBoundClient<T> Include<T>(this IBoundClient<T> client, ODataExpandOptions includeOptions, params string[] associations)
            where T : class
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            return client.Expand(expandOptions: includeOptions, associations: associations);
        }

        public static IBoundClient<T> Include<T>(this IBoundClient<T> client, params ODataExpression[] associations)
            where T : class
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            return client.Expand(associations: associations);
        }

        public static IBoundClient<T> Include<T>(this IBoundClient<T> client, ODataExpandOptions includeOptions, params ODataExpression[] associations)
            where T : class
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            return client.Expand(expandOptions: includeOptions, associations: associations);
        }

        public static IBoundClient<T> Include<T>(this IBoundClient<T> client, ODataExpandOptions includeOptions)
            where T : class
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            return client.Expand(expandOptions: includeOptions);
        }

        public static IBoundClient<T> Take<T>(this IBoundClient<T> client, int count)
            where T : class
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            return client.Top(count: count);
        }

        public static IBoundClient<T> Where<T>(this IBoundClient<T> client, string where)
            where T : class
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            return client.Filter(filter: where);
        }

        public static IBoundClient<T> Where<T>(this IBoundClient<T> client, Expression<Func<T, bool>> expression)
            where T : class
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            return client.Filter(expression: expression);
        }

        public static IBoundClient<T> Where<T>(this IBoundClient<T> client, ODataExpression expression)
            where T : class
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            return client.Filter(expression: expression);
        }
    }
}
