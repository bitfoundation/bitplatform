using Foundation.Model.Contracts;
using System;
using System.Linq;
using System.Reflection;

namespace Foundation.DataAccess.Implementations
{
    public static class KeyWhereBuilder<TSource, TKey>
        where TSource : class, IWithDefaultKey<TKey>
    {
        public static IQueryable<TSource> ApplyKeyWhere(IQueryable<TSource> query, TKey key)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));

            if (IsShort)
                query = (IQueryable<TSource>)CreateIdWhereForShortMethod.Value.Invoke(null, new object[] { query, key });
            else if (IsInt)
                query = (IQueryable<TSource>)CreateIdWhereForIntMethod.Value.Invoke(null, new object[] { query, key });
            else if (IsLong)
                query = (IQueryable<TSource>)CreateIdWhereForLongMethod.Value.Invoke(null, new object[] { query, key });
            else if (IsGuid)
                query = (IQueryable<TSource>)CreateIdWhereForGuidMethod.Value.Invoke(null, new object[] { query, key });
            else if (IsString)
                query = (IQueryable<TSource>)CreateIdWhereForStringMethod.Value.Invoke(null, new object[] { query, key });
            else
                throw new NotImplementedException();

            return query;
        }

        private static TypeInfo KeyType = typeof(TKey).GetTypeInfo();

        private static bool IsGuid = typeof(TKey).GetTypeInfo() == typeof(Guid).GetTypeInfo();
        private static bool IsShort = typeof(TKey).GetTypeInfo() == typeof(short).GetTypeInfo();
        private static bool IsInt = typeof(TKey).GetTypeInfo() == typeof(int).GetTypeInfo();
        private static bool IsLong = typeof(TKey).GetTypeInfo() == typeof(long).GetTypeInfo();
        private static bool IsString = typeof(TKey).GetTypeInfo() == typeof(string).GetTypeInfo();

        private static Lazy<MethodInfo> CreateIdWhereForShortMethod = new Lazy<MethodInfo>(() =>
        {
            return typeof(KeyWhereBuilder<TSource, TKey>)
                .GetTypeInfo()
                .GetMethod(nameof(CreateIdWhereForShort), BindingFlags.NonPublic | BindingFlags.Static)
                .MakeGenericMethod(typeof(TSource).GetTypeInfo());
        });

        private static IQueryable<TDtoWithIntKey> CreateIdWhereForShort<TDtoWithIntKey>(IQueryable<TDtoWithIntKey> query, short id)
            where TDtoWithIntKey : IWithDefaultKey<short>
        {
            return query.Where(dto => dto.Id == id);
        }

        private static Lazy<MethodInfo> CreateIdWhereForIntMethod = new Lazy<MethodInfo>(() =>
        {
            return typeof(KeyWhereBuilder<TSource, TKey>)
                .GetTypeInfo()
                .GetMethod(nameof(CreateIdWhereForInt), BindingFlags.NonPublic | BindingFlags.Static)
                .MakeGenericMethod(typeof(TSource).GetTypeInfo());
        });

        private static IQueryable<TDtoWithIntKey> CreateIdWhereForInt<TDtoWithIntKey>(IQueryable<TDtoWithIntKey> query, int id)
            where TDtoWithIntKey : IWithDefaultKey<int>
        {
            return query.Where(dto => dto.Id == id);
        }

        private static Lazy<MethodInfo> CreateIdWhereForLongMethod = new Lazy<MethodInfo>(() =>
        {
            return typeof(KeyWhereBuilder<TSource, TKey>)
                .GetTypeInfo()
                .GetMethod(nameof(CreateIdWhereForLong), BindingFlags.NonPublic | BindingFlags.Static)
                .MakeGenericMethod(typeof(TSource).GetTypeInfo());
        });

        private static IQueryable<TDtoWithIntKey> CreateIdWhereForLong<TDtoWithIntKey>(IQueryable<TDtoWithIntKey> query, long id)
            where TDtoWithIntKey : IWithDefaultKey<int>
        {
            return query.Where(dto => dto.Id == id);
        }

        private static Lazy<MethodInfo> CreateIdWhereForGuidMethod = new Lazy<MethodInfo>(() =>
        {
            return typeof(KeyWhereBuilder<TSource, TKey>)
              .GetTypeInfo()
              .GetMethod(nameof(CreateIdWhereForGuid), BindingFlags.NonPublic | BindingFlags.Static)
              .MakeGenericMethod(typeof(TSource).GetTypeInfo());
        });

        private static IQueryable<TDtoWithIntKey> CreateIdWhereForGuid<TDtoWithIntKey>(IQueryable<TDtoWithIntKey> query, Guid id)
            where TDtoWithIntKey : IWithDefaultKey<Guid>
        {
            return query.Where(dto => dto.Id == id);
        }

        private static Lazy<MethodInfo> CreateIdWhereForStringMethod = new Lazy<MethodInfo>(() =>
        {
            return typeof(KeyWhereBuilder<TSource, TKey>)
              .GetTypeInfo()
              .GetMethod(nameof(CreateIdWhereForString), BindingFlags.NonPublic | BindingFlags.Static)
              .MakeGenericMethod(typeof(TSource).GetTypeInfo());
        });

        private static IQueryable<TDtoWithIntKey> CreateIdWhereForString<TDtoWithIntKey>(IQueryable<TDtoWithIntKey> query, string id)
            where TDtoWithIntKey : IWithDefaultKey<string>
        {
            return query.Where(dto => dto.Id == id);
        }
    }
}
