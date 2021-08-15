using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zooyard.Rpc.NettyImpl.Protocol;

namespace Zooyard.Rpc.NettyImpl
{
	/// <summary>
	/// The interface Register check auth handler.
	/// 
	/// </summary>
	public interface IRegisterCheckAuthHandler
	{

		/// <summary>
		/// Reg transaction manager check auth boolean.
		/// </summary>
		/// <param name="request"> the request </param>
		/// <returns> the boolean </returns>
		Task<bool> RegTransactionManagerCheckAuth(RegisterTMRequest request);

		/// <summary>
		/// Reg resource manager check auth boolean.
		/// </summary>
		/// <param name="request"> the request </param>
		/// <returns> the boolean </returns>
		Task<bool> RegResourceManagerCheckAuth(RegisterRMRequest request);
	}
}
