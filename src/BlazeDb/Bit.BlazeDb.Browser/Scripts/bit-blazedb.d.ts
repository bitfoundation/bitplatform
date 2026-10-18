// Ambient types shared by the Bit.BlazeDb browser modules. A declaration file emits nothing, so no
// stray module lands in wwwroot next to the four the [JSImport] bindings load.

/**
 * What a [JSMarshalAs<JSType.MemoryView>] Span<byte> argument arrives as: a view over the managed
 * heap that is only valid for the duration of the call, so it is written into rather than kept.
 */
interface DotNetMemoryView {
    set(source: ArrayLike<number>, targetOffset?: number): void;
    readonly length: number;
    readonly byteLength: number;
}
