using System.Text;

namespace Bit.BlazorUI;

/// <summary>
/// BitAiStructuredDataView renders structured model output: by whatever renderer the app registered for its schema,
/// and otherwise by a default that reads the shape of the value.
/// </summary>
/// <remarks>
/// An array of objects becomes a <see cref="BitAiDataTable"/>, an object a list of labelled values, and anything else
/// a <see cref="BitAiJsonView"/>. All three cope with a partial payload, because the value is re-parsed leniently on
/// every delta and fields fill in as they arrive; while nothing has arrived yet a streaming part shows a
/// <see cref="BitAiThinkingIndicator"/>. A renderer registered with <see cref="BitAiRendererRegistry.RegisterStructured"/>
/// runs inside a <see cref="BitAiRendererBoundary"/>, so one that throws does not take the response down with it.
/// </remarks>
public partial class BitAiStructuredDataView : BitAiComponentBase
{
    private RenderFragment? _custom;





    /// <summary>
    /// Gets or sets the cascading parameters for the structured data view component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple structured data view
    /// components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitAiStructuredDataViewParams.ParamName)]
    public BitAiStructuredDataViewParams? CascadingParameters { get; set; }



    /// <summary>
    /// Custom CSS classes for different parts of the structured data view.
    /// </summary>
    [Parameter, ResetClassBuilder] public BitAiStructuredDataViewClassStyles? Classes { get; set; }

    /// <summary>
    /// The message the part belongs to, handed to a registered structured renderer.
    /// </summary>
    [Parameter] public BitAiMessage? Message { get; set; }

    /// <summary>
    /// The structured part to render.
    /// </summary>
    [Parameter, EditorRequired] public BitAiStructuredDataPart Part { get; set; } = default!;

    /// <summary>
    /// Custom CSS styles for different parts of the structured data view.
    /// </summary>
    [Parameter, ResetStyleBuilder] public BitAiStructuredDataViewClassStyles? Styles { get; set; }



    protected override string RootElementClass => "bit-aisdv";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        _custom = Renderers.FindStructured(Part.SchemaName)
                          ?.Invoke(new BitAiStructuredDataContext(Part, Message ?? new BitAiMessage(BitAiRole.Assistant)));

        base.OnParametersSet();
    }



    /// <summary>
    /// Turns a JSON property name into something a person would write on a label: "firstName" becomes "First Name" and
    /// "first_name" becomes "First name".
    /// </summary>
    /// <remarks>
    /// Only the first letter is capitalised; every other letter keeps its case. An underscore or a hyphen becomes a space,
    /// and an upper-case letter that does not follow another one starts a new word.
    /// </remarks>
    /// <param name="name">The property name.</param>
    /// <returns>The label.</returns>
    internal static string Humanize(string name)
    {
        if (name.Length == 0) return name;

        var builder = new StringBuilder(name.Length + 4);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (c is '_' or '-')
            {
                builder.Append(' ');
                continue;
            }

            if (i > 0 && char.IsUpper(c) && char.IsUpper(name[i - 1]) is false) builder.Append(' ');
            builder.Append(i == 0 ? char.ToUpperInvariant(c) : c);
        }

        return builder.ToString();
    }

    private static bool IsTabular(JsonElement value)
    {
        if (value.GetArrayLength() == 0) return false;

        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind is not JsonValueKind.Object) return false;
        }

        return true;
    }
}
