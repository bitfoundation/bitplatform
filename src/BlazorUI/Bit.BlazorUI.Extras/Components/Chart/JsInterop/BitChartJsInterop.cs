﻿using System.Dynamic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Bit.BlazorUI;

/// <summary>
/// Interop layer from C# to JavaScript.
/// </summary>
internal static class BitChartJsInterop
{
    internal static JsonSerializerSettings JsonSerializerSettings { get; } = new JsonSerializerSettings
    {
        NullValueHandling = NullValueHandling.Ignore,
        ContractResolver = new IgnoreDatasetCountContractResolver
        {
            NamingStrategy = new CamelCaseNamingStrategy(true, false)
        },
        Converters = { new IsoDateTimeConverter() }
    };

    public static ValueTask InitChartJs(this IJSRuntime jsRuntime, IEnumerable<string> scripts)
    {
        return jsRuntime.InvokeVoidAsync("BitBlazorUI.BitChart.initChartJs", scripts);
    }

    public static ValueTask RemoveChart(this IJSRuntime jsRuntime, string canvasId)
    {
        return jsRuntime.InvokeVoidAsync("BitBlazorUI.BitChart.removeChart", canvasId);
    }

    /// <summary>
    /// Set up a new chart. Call only once.
    /// </summary>
    /// <param name="jsRuntime"></param>
    /// <param name="chartConfig">The config for the new chart.</param>
    /// <returns></returns>
    public static ValueTask<bool> SetupChart(this IJSRuntime jsRuntime, BitChartConfigBase chartConfig)
    {
        var dynParam = StripNulls(chartConfig);
        Dictionary<string, object> param = ConvertExpandoObjectToDictionary(dynParam);
        return jsRuntime.InvokeAsync<bool>("BitBlazorUI.BitChart.setupChart", param);
    }

    /// <summary>
    /// This method is specifically used to convert an <see cref="ExpandoObject"/> with a Tree structure to a <c>Dictionary&lt;string, object&gt;</c>.
    /// </summary>
    /// <param name="expando">The <see cref="ExpandoObject"/> to convert.</param>
    /// <returns>The fully converted <see cref="ExpandoObject"/>.</returns>
    private static Dictionary<string, object> ConvertExpandoObjectToDictionary(ExpandoObject expando) => RecursivelyConvertIDictToDict(expando);

    /// <summary>
    /// This method takes an <c>IDictionary&lt;string, object&gt;</c> and recursively converts it to a <c>Dictionary&lt;string, object&gt;</c>.
    /// The idea is that every <c>IDictionary&lt;string, object&gt;</c> in the tree will be of type <c>Dictionary&lt;string, object&gt;</c> instead of some other implementation like <see cref="ExpandoObject"/>.
    /// </summary>
    /// <param name="value">The <c>IDictionary&lt;string, object&gt;</c> to convert</param>
    /// <returns>The fully converted <c>Dictionary&lt;string, object&gt;</c></returns>
    private static Dictionary<string, object> RecursivelyConvertIDictToDict(IDictionary<string, object> value) =>
        value.ToDictionary(
            keySelector => keySelector.Key,
            elementSelector =>
            {
                // if it's another IDict just go through it recursively
                if (elementSelector.Value is IDictionary<string, object> dict)
                {
                    return RecursivelyConvertIDictToDict(dict);
                }

                // if it's an IEnumerable check each element
                if (elementSelector.Value is IEnumerable<object> list)
                {
                    // go through all objects in the list
                    // if the object is an IDict -> convert it
                    // if not keep it as is
                    return list
                        .Select(o => o is IDictionary<string, object> dictionary
                            ? RecursivelyConvertIDictToDict(dictionary)
                            : o
                        );
                }

                // neither an IDict nor an IEnumerable -> it's fine to just return the value it has
                return elementSelector.Value;
            }
        );

    /// <summary>
    /// Update an existing chart. Make sure that the Chart with this <see cref="BitChartConfigBase.CanvasId"/> already exists.
    /// </summary>
    /// <param name="jsRuntime"></param>
    /// <param name="chartConfig">The updated config of the chart you want to update.</param>
    /// <returns></returns>
    public static ValueTask<bool> UpdateChart(this IJSRuntime jsRuntime, BitChartConfigBase chartConfig)
    {
        var dynParam = StripNulls(chartConfig);
        Dictionary<string, object> param = ConvertExpandoObjectToDictionary(dynParam);
        return jsRuntime.InvokeAsync<bool>("BitBlazorUI.BitChart.updateChart", param);
    }

    /// <summary>
    /// Returns an object that is equivalent to the given parameter but without any null members AND it preserves <see cref="IBitChartMethodHandler"/>s intact.
    /// <para>Preserving <see cref="IBitChartMethodHandler"/> members is important because they might be <see cref="BitChartDelegateHandler{T}"/> instances which contain
    /// delegates that can't be (de)serialized.</para>
    /// <para>Stripping null members is only needed because chartJs doesn't handle null values and undefined values the same and with JSRuntime null gets
    /// serialized to null instead of undefined (not at all) and WE CAN'T CHANGE THAT (see https://github.com/aspnet/AspNetCore/issues/12685).
    /// If this were not the case, no null member stripping were necessary -> no json.net serialize-deserialize magic -> no loss of <see cref="BitChartDelegateHandler{T}"/>
    /// instances -> no recovery of those. Everything would be better with AspNetCore#12685 finally being implemented but to fully migrate to System.Text.Json
    /// we might also need corefx#38650 and corefx#39905.
    /// Nevertheless, The Show must go on!</para>
    /// </summary>
    /// <param name="chartConfig">The config you want to strip of null members.</param>
    /// <returns></returns>
    private static ExpandoObject StripNulls(BitChartConfigBase chartConfig)
    {
        // Serializing with the custom serializer settings remove null members
        string cleanChartConfigStr = JsonConvert.SerializeObject(chartConfig, JsonSerializerSettings);

        // Get back an ExpandoObject dynamic with the clean config - having an ExpandoObject allows us to add/replace members regardless of type
        ExpandoObject cleanChartConfig = JsonConvert.DeserializeObject<ExpandoObject>(cleanChartConfigStr, new ExpandoObjectConverter());

        // Restore any .net refs that need to be passed intact
        // TODO Find a way to do this dynamically. Maybe with attributes or something like that?
        dynamic dynamicChartConfig = (dynamic)chartConfig;
        if (dynamicChartConfig?.Options?.OnClick is IBitChartMethodHandler chartOnClick)
        {
            cleanChartConfig.SetValue(path: "options.onClick", chartOnClick);
        }

        if (dynamicChartConfig?.Options?.OnHover is IBitChartMethodHandler chartOnHover)
        {
            cleanChartConfig.SetValue(path: "options.onHover", chartOnHover);
        }

        if (dynamicChartConfig?.Options?.Legend?.OnClick is IBitChartMethodHandler legendOnClick)
        {
            cleanChartConfig.SetValue(path: "options.legend.onClick", legendOnClick);
        }

        if (dynamicChartConfig?.Options?.Legend?.OnHover is IBitChartMethodHandler legendOnHover)
        {
            cleanChartConfig.SetValue(path: "options.legend.onHover", legendOnHover);
        }

        if (dynamicChartConfig?.Options?.Legend?.Labels?.GenerateLabels is IBitChartMethodHandler generateLabels)
        {
            cleanChartConfig.SetValue(path: "options.legend.labels.generateLabels", generateLabels);
        }

        if (dynamicChartConfig?.Options?.Legend?.Labels?.Filter is IBitChartMethodHandler filter)
        {
            cleanChartConfig.SetValue(path: "options.legend.labels.filter", filter);
        }

        // Ticks callback need special handling because it can be either a single scale or two arrays of scales (xAxes and yAxes)
        // it's really ugly (and quite slow), I hope we can improve this later on. Also ms, PLEASE, give us customizable jsruntime serialization.
        try
        {
            if (dynamicChartConfig?.Options?.Scale?.Callback is IBitChartMethodHandler singleScaleTickCallback)
            {
                cleanChartConfig.SetValue(path: "options.scale.callback", singleScaleTickCallback);
            }
        }
        catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // happens when the options don't have a Scale property
        {
            try
            {
                // here we trust that if the Scales property exists, it contains an XAxes and a YAxes property
                if (dynamicChartConfig?.Options?.Scales?.XAxes is IEnumerable<object> xAxes)
                {
                    AssignAxes(xAxes, "options.scales.xAxes");
                }

                if (dynamicChartConfig?.Options?.Scales?.YAxes is IEnumerable<object> yAxes)
                {
                    AssignAxes(yAxes, "options.scales.yAxes");
                }
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) { } // happens when the options don't have a Scales property (only Pie & Doughnut)
        }

        return cleanChartConfig;

        void AssignAxes(IEnumerable<object> axes, string axesPath)
        {
            IEnumerable<object> axesInDynamic = cleanChartConfig.GetValue(axesPath) as IEnumerable<object>;

            foreach ((object axis, ExpandoObject axisInDynamic) in axes.Zip(axesInDynamic, (axis, axisInDynamic) => (axis, (ExpandoObject)axisInDynamic)))
            {
                if (((dynamic)axis)?.Ticks?.Callback is IBitChartMethodHandler axisTickCallback)
                {
                    axisInDynamic.SetValue("ticks.callback", axisTickCallback);
                }
            }
        }
    }
}
