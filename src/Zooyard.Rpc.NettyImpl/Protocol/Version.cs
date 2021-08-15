using DotNetty.Transport.Channels;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Zooyard.Utils;

namespace Zooyard.Rpc.NettyImpl.Protocol
{
    /// <summary>
    /// The type Version.
    /// 
    /// </summary>
    public class Version
	{

		/// <summary>
		/// The constant CURRENT.
		/// </summary>
		private const string CURRENT = "1.0.0";
		private const string VERSION_0_7_1 = "0.7.1";
		private const int MAX_VERSION_DOT = 3;

		/// <summary>
		/// The constant VERSION_MAP.
		/// </summary>
		public static readonly IDictionary<string, string> VERSION_MAP = new ConcurrentDictionary<string, string>();
		private Version() { }

		/// <summary>
		/// Gets current.
		/// </summary>
		/// <returns> the current </returns>
		public static string Current
		{
			get
			{
				return CURRENT;
			}
		}
		/// <summary>
		/// Put channel version.
		/// </summary>
		/// <param name="c"> the c </param>
		/// <param name="v"> the v </param>
		public static void PutChannelVersion(IChannel c, string v)
		{
			var key = NetUtil.ToStringAddress(c.RemoteAddress);
			VERSION_MAP[key] = v;
		}

		/// <summary>
		/// Gets channel version.
		/// </summary>
		/// <param name="c"> the c </param>
		/// <returns> the channel version </returns>
		public static string GetChannelVersion(IChannel c)
		{
			VERSION_MAP.TryGetValue(NetUtil.ToStringAddress(c.RemoteAddress), out string result);
			return result;
		}

		/// <summary>
		/// Check version string.
		/// </summary>
		/// <param name="version"> the version </param>
		/// <returns> the string </returns>
		/// <exception cref="IncompatibleVersionException"> the incompatible version exception </exception>
		public static void CheckVersion(string version)
		{
			long current = convertVersion(CURRENT);
			long clientVersion = convertVersion(version);
			long divideVersion = convertVersion(VERSION_0_7_1);
			if ((current > divideVersion && clientVersion < divideVersion) || (current < divideVersion && clientVersion > divideVersion))
			{
				throw new IncompatibleVersionException("incompatible client version:" + version);
			}
		}

		private static long convertVersion(string version)
		{
			string[] parts = version.Split('.');
			long result = 0L;
			int i = 1;
			int size = parts.Length;
			if (size > MAX_VERSION_DOT + 1)
			{
				throw new IncompatibleVersionException("incompatible version format:" + version);
			}
			size = MAX_VERSION_DOT + 1;
			foreach (string part in parts)
			{
				if (long.TryParse(part,out long partLong))
				{
					result += calculatePartValue(partLong, size, i);
				}
				else
				{
					string[] subParts = part.Split('-');
					if (long.TryParse(subParts[0], out long partLong2))
					{
						result += calculatePartValue(partLong2, size, i);
					}
				}

				i++;
			}
			return result;
		}

		private static long calculatePartValue(long partNumeric, int size, int index)
		{
			return partNumeric * Convert.ToInt64(Math.Pow(100, size - index));
		}
	}
}