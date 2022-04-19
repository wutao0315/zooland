using System.Text;


namespace Zooyard.Rpc.NettyImpl.Protocol;

/// <summary>
/// The type Abstract identify response.
/// 
/// </summary>
[Serializable]
public abstract class AbstractIdentifyResponse : AbstractResultMessage
{

	/// <summary>
	/// Gets version.
	/// </summary>
	/// <returns> the version </returns>
	public virtual string Version { get; set; } = Protocol.Version.Current;


        /// <summary>
        /// Gets extra data.
        /// </summary>
        /// <returns> the extra data </returns>
        public virtual string ExtraData { get; set; }


	/// <summary>
	/// Is identified boolean.
	/// </summary>
	/// <returns> the boolean </returns>
	public virtual bool Identified { get; set; }


	public override string ToString()
	{
		var result = new StringBuilder();
		result.Append("version=");
		result.Append(Version);
		result.Append(',');
		result.Append("extraData=");
		result.Append(ExtraData);
		result.Append(',');
		result.Append("identified=");
		result.Append(Identified);
		result.Append(',');
		result.Append("resultCode=");
		result.Append(ResultCode);
		result.Append(',');
		result.Append("msg=");
		result.Append(Msg);

		return result.ToString();
	}
}

