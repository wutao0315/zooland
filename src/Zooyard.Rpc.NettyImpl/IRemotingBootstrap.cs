namespace Zooyard.Rpc.NettyImpl;

/// <summary>
/// The boot strap of the remoting process, generally there are client and server implementation classes
/// </summary>
public interface IRemotingBootstrap
    {
	/// <summary>
	/// Start.
	/// </summary>
	Task Start();

	/// <summary>
	/// Shutdown.
	/// </summary>
	Task Shutdown();
}
