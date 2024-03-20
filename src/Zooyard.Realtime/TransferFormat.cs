namespace Zooyard.Realtime;

/// <summary>
/// Represents the possible transfer formats.
/// </summary>
[Flags]
public enum TransferFormat
{
    /// <summary>
    /// A binary transport format.
    /// </summary>
    Binary = 0x01,

    /// <summary>
    /// A text transport format.
    /// </summary>
    Text = 0x02
}

