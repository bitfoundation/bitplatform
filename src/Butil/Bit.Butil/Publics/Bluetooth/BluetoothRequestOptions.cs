namespace Bit.Butil;

/// <summary>
/// What the browser's Bluetooth device chooser shows, and which services the granted device may
/// then be talked to over.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Bluetooth/requestDevice">Bluetooth.requestDevice()</see>
/// </summary>
/// <remarks>
/// A grant covers exactly the services named here - those on the <see cref="BluetoothFilter.Services"/>
/// of a filter the device matched, plus every entry in <see cref="OptionalServices"/>. A service
/// named in neither stays unreachable: <c>getPrimaryService</c> for it is a <c>SecurityError</c>,
/// so a device picked with filters that name no service and no <see cref="OptionalServices"/> can
/// be connected to and nothing more.
/// </remarks>
public class BluetoothRequestOptions
{
    /// <summary>
    /// The filters deciding which devices appear in the chooser. Leave empty (or set
    /// <see cref="AcceptAllDevices"/>) to show everything nearby.
    /// </summary>
    public BluetoothFilter[]? Filters { get; set; }

    /// <summary>
    /// Show every nearby device instead of filtering. Mutually exclusive with
    /// <see cref="Filters"/> - when both are given, this wins, because the browser rejects a
    /// request carrying both.
    /// </summary>
    public bool AcceptAllDevices { get; set; }

    /// <summary>
    /// Services the page may access on the granted device even though it did not filter on them.
    /// Anything you intend to read, write or subscribe to has to be named here or in a filter.
    /// </summary>
    public string[]? OptionalServices { get; set; }
}
