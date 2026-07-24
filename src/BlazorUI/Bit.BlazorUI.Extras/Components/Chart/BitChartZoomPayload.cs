namespace Bit.BlazorUI;

// The zoom registration payload sent to the JS bridge. Property names are serialized to
// camelCase by the JS interop serializer to match what the bridge reads (wheel, pan, drag).
internal class BitChartZoomPayload
{
    public bool Wheel { get; set; }
    public bool Pan { get; set; }
    public bool Drag { get; set; }
}
