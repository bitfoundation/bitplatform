﻿// <auto-generated />
using System;
using System.Reflection;
using Bit.Besql.Demo.Client.Model;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Sqlite.Storage.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#pragma warning disable 219, 612, 618
#nullable disable

namespace Bit.Besql.Demo.Client.Data.CompiledModel
{
    internal partial class WeatherForecastEntityType
    {
        public static RuntimeEntityType Create(RuntimeModel model, RuntimeEntityType baseEntityType = null)
        {
            var runtimeEntityType = model.AddEntityType(
                "Bit.Besql.Demo.Client.Model.WeatherForecast",
                typeof(WeatherForecast),
                baseEntityType);

            var id = runtimeEntityType.AddProperty(
                "Id",
                typeof(int),
                propertyInfo: typeof(WeatherForecast).GetProperty("Id", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(WeatherForecast).GetField("<Id>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                valueGenerated: ValueGenerated.OnAdd,
                afterSaveBehavior: PropertySaveBehavior.Throw,
                sentinel: 0);
            id.TypeMapping = IntTypeMapping.Default.Clone(
                comparer: new ValueComparer<int>(
                    (int v1, int v2) => v1 == v2,
                    (int v) => v,
                    (int v) => v),
                keyComparer: new ValueComparer<int>(
                    (int v1, int v2) => v1 == v2,
                    (int v) => v,
                    (int v) => v),
                providerValueComparer: new ValueComparer<int>(
                    (int v1, int v2) => v1 == v2,
                    (int v) => v,
                    (int v) => v),
                mappingInfo: new RelationalTypeMappingInfo(
                    storeTypeName: "INTEGER"));

            var date = runtimeEntityType.AddProperty(
                "Date",
                typeof(DateTimeOffset),
                propertyInfo: typeof(WeatherForecast).GetProperty("Date", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(WeatherForecast).GetField("<Date>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                valueConverter: new DateTimeOffsetToBinaryConverter());
            date.TypeMapping = LongTypeMapping.Default.Clone(
                comparer: new ValueComparer<DateTimeOffset>(
                    (DateTimeOffset v1, DateTimeOffset v2) => v1.EqualsExact(v2),
                    (DateTimeOffset v) => v.GetHashCode(),
                    (DateTimeOffset v) => v),
                keyComparer: new ValueComparer<DateTimeOffset>(
                    (DateTimeOffset v1, DateTimeOffset v2) => v1.EqualsExact(v2),
                    (DateTimeOffset v) => v.GetHashCode(),
                    (DateTimeOffset v) => v),
                providerValueComparer: new ValueComparer<long>(
                    (long v1, long v2) => v1 == v2,
                    (long v) => v.GetHashCode(),
                    (long v) => v),
                mappingInfo: new RelationalTypeMappingInfo(
                    storeTypeName: "INTEGER"),
                converter: new ValueConverter<DateTimeOffset, long>(
                    (DateTimeOffset v) => DateTimeOffsetToBinaryConverter.ToLong(v),
                    (long v) => DateTimeOffsetToBinaryConverter.ToDateTimeOffset(v)),
                jsonValueReaderWriter: new JsonConvertedValueReaderWriter<DateTimeOffset, long>(
                    JsonInt64ReaderWriter.Instance,
                    new ValueConverter<DateTimeOffset, long>(
                        (DateTimeOffset v) => DateTimeOffsetToBinaryConverter.ToLong(v),
                        (long v) => DateTimeOffsetToBinaryConverter.ToDateTimeOffset(v))));
            date.SetSentinelFromProviderValue(0L);

            var summary = runtimeEntityType.AddProperty(
                "Summary",
                typeof(string),
                propertyInfo: typeof(WeatherForecast).GetProperty("Summary", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(WeatherForecast).GetField("<Summary>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                nullable: true,
                maxLength: 100);
            summary.TypeMapping = SqliteStringTypeMapping.Default;

            var temperatureC = runtimeEntityType.AddProperty(
                "TemperatureC",
                typeof(int),
                propertyInfo: typeof(WeatherForecast).GetProperty("TemperatureC", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(WeatherForecast).GetField("<TemperatureC>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                sentinel: 0);
            temperatureC.TypeMapping = IntTypeMapping.Default.Clone(
                comparer: new ValueComparer<int>(
                    (int v1, int v2) => v1 == v2,
                    (int v) => v,
                    (int v) => v),
                keyComparer: new ValueComparer<int>(
                    (int v1, int v2) => v1 == v2,
                    (int v) => v,
                    (int v) => v),
                providerValueComparer: new ValueComparer<int>(
                    (int v1, int v2) => v1 == v2,
                    (int v) => v,
                    (int v) => v),
                mappingInfo: new RelationalTypeMappingInfo(
                    storeTypeName: "INTEGER"));

            var key = runtimeEntityType.AddKey(
                new[] { id });
            runtimeEntityType.SetPrimaryKey(key);

            var index = runtimeEntityType.AddIndex(
                new[] { temperatureC });

            return runtimeEntityType;
        }

        public static void CreateAnnotations(RuntimeEntityType runtimeEntityType)
        {
            runtimeEntityType.AddAnnotation("Relational:FunctionName", null);
            runtimeEntityType.AddAnnotation("Relational:Schema", null);
            runtimeEntityType.AddAnnotation("Relational:SqlQuery", null);
            runtimeEntityType.AddAnnotation("Relational:TableName", "WeatherForecasts");
            runtimeEntityType.AddAnnotation("Relational:ViewName", null);
            runtimeEntityType.AddAnnotation("Relational:ViewSchema", null);

            Customize(runtimeEntityType);
        }

        static partial void Customize(RuntimeEntityType runtimeEntityType);
    }
}
