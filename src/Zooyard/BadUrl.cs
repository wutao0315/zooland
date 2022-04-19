namespace Zooyard;

public class BadUrl
{
    /// <summary>
    /// 调用失败的URL
    /// </summary>
    public URL Url { get; set; }
    /// <summary>
    /// 调用失败的时间
    /// </summary>
    public DateTime BadTime { get; set; }
    /// <summary>
    /// 异常描述
    /// </summary>
    public Exception CurrentException { get; set; }
}
