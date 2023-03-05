namespace Zooyard.DotNettyImpl.Transport;

public interface ITransportMessageCodecFactory
{
    /// <summary>
    /// 获取编码器。
    /// </summary>
    /// <returns>编码器实例。</returns>
    ITransportMessageEncoder GetEncoder();

    /// <summary>
    /// 获取解码器。
    /// </summary>
    /// <returns>解码器实例。</returns>
    ITransportMessageDecoder GetDecoder();
}
