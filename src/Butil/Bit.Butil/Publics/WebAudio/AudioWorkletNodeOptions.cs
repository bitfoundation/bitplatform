namespace Bit.Butil;

/// <summary>
/// How to build an <see cref="AudioWorkletNodeHandle"/>, mirroring the options of the
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/AudioWorkletNode/AudioWorkletNode">AudioWorkletNode constructor</see>.
/// </summary>
/// <remarks>
/// Every member is optional; the defaults build a node with one input and one output, which is what
/// a processing node normally wants. A node that only generates sound is usually declared with
/// <see cref="NumberOfInputs"/> 0.
/// </remarks>
public class AudioWorkletNodeOptions
{
    /// <summary>How many inputs the node has. 0 for a pure generator.</summary>
    public int? NumberOfInputs { get; set; }

    /// <summary>How many outputs the node has.</summary>
    public int? NumberOfOutputs { get; set; }

    /// <summary>The channel count of each output, when the processor needs something other than the default.</summary>
    public int[]? OutputChannelCount { get; set; }

    /// <summary>
    /// Initial values for the processor's declared AudioParams, by name. Change them afterwards with
    /// <see cref="AudioNodeHandle.SetParam"/>.
    /// </summary>
    public System.Collections.Generic.Dictionary<string, double>? ParameterData { get; set; }

    /// <summary>
    /// Anything the processor's constructor needs, as JSON text. It is parsed before being handed
    /// over, so the processor receives an ordinary object - and neither side needs a type the other
    /// knows about.
    /// </summary>
    public string? ProcessorOptions { get; set; }
}
