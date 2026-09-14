namespace Bit.Butil;

/// <summary>
/// Mirrors <see href="https://developer.mozilla.org/en-US/docs/Web/API/BiquadFilterNode/type">BiquadFilterNode.type</see>.
/// </summary>
/// <remarks>
/// Which parameters matter depends on the type: <c>Q</c> shapes the resonance of the pass and notch
/// filters, while <c>gain</c> only does anything for the shelf and peaking ones.
/// </remarks>
public enum BiquadFilterType
{
    /// <summary>Lets everything below the cutoff through. The workhorse for taking harshness out of a signal.</summary>
    Lowpass,

    /// <summary>Lets everything above the cutoff through - what removes rumble and handling noise.</summary>
    Highpass,

    /// <summary>Lets a band around the frequency through and cuts the rest.</summary>
    Bandpass,

    /// <summary>Lifts or cuts everything below the frequency, by <c>gain</c> decibels. A bass control.</summary>
    Lowshelf,

    /// <summary>Lifts or cuts everything above the frequency. A treble control.</summary>
    Highshelf,

    /// <summary>Lifts or cuts a band around the frequency - one band of a graphic equaliser.</summary>
    Peaking,

    /// <summary>Cuts a narrow band and leaves the rest - for removing mains hum at a known frequency.</summary>
    Notch,

    /// <summary>Passes everything but shifts the phase around the frequency. Used in phasers and crossovers.</summary>
    Allpass
}
