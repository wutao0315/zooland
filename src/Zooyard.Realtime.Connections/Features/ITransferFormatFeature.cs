namespace Zooyard.Realtime.Connections.Features;

/// <summary>
/// Gets the transfer format of the protocol.
/// </summary>
public interface ITransferFormatFeature
{
    /// <summary>
    /// Gets the supported <see cref="TransferFormat"/>.
    /// </summary>
    TransferFormat SupportedFormats { get; }

    /// <summary>
    /// Gets or sets the active <see cref="TransferFormat"/>.
    /// </summary>
    TransferFormat ActiveFormat { get; set; }
}

