namespace Bit.Butil;

/// <summary>
/// The control lines the device is driving, read with <see cref="SerialPort.GetSignals"/>. These
/// are inputs; the outputs are set with <see cref="SerialPort.SetSignals"/>.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/SerialPort/getSignals">SerialPort.getSignals()</see>
/// </summary>
public class SerialSignals
{
    /// <summary>CTS - the device is ready to receive.</summary>
    public bool ClearToSend { get; set; }

    /// <summary>DCD - a modem reports a carrier on the line.</summary>
    public bool DataCarrierDetect { get; set; }

    /// <summary>DSR - the device is powered and ready.</summary>
    public bool DataSetReady { get; set; }

    /// <summary>RI - a modem reports an incoming call.</summary>
    public bool RingIndicator { get; set; }
}
