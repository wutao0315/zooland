using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zooyard.Rpc.NettyImpl.Protocol;
using Zooyard.Rpc.NettyImpl.Protocol.Transaction;

namespace Zooyard.Rpc.NettyImpl.Serializer
{
	/// <summary>
	/// Provide a unified serialization registry, this class used for {@code seata-serializer-fst}
	/// and {@code seata-serializer-kryo}, it will register some classes at startup time (for example <seealso cref="KryoSerializerFactory#create"/>)
	/// </summary>
	public class SerializerClassRegistry
	{

		private static readonly IDictionary<Type, object> REGISTRATIONS = new Dictionary<Type, object>();

		static SerializerClassRegistry()
		{

			// register commonly class
			registerClass(typeof(HashSet<>));
			//registerClass(typeof(ArrayList));
			registerClass(typeof(LinkedList<>));
			//registerClass(typeof(HashSet));
			//registerClass(typeof(SortedSet));
			//registerClass(typeof(Hashtable));
			registerClass(typeof(DateTime));
			//registerClass(typeof(DateTime));
			registerClass(typeof(ConcurrentDictionary<,>));
			//registerClass(typeof(SimpleDateFormat));
			//registerClass(typeof(GregorianCalendar));
			//registerClass(typeof(ArrayList));
			//registerClass(typeof(BitArray));
			registerClass(typeof(StringBuilder));
			registerClass(typeof(StringBuilder));
			registerClass(typeof(object));
			registerClass(typeof(object[]));
			registerClass(typeof(string[]));
			registerClass(typeof(byte[]));
			registerClass(typeof(char[]));
			registerClass(typeof(int[]));
			registerClass(typeof(float[]));
			registerClass(typeof(double[]));

			// register seata protocol relation class
			registerClass(typeof(BranchCommitRequest));
			registerClass(typeof(BranchCommitResponse));
			registerClass(typeof(BranchRegisterRequest));
			registerClass(typeof(BranchRegisterResponse));
			registerClass(typeof(BranchReportRequest));
			registerClass(typeof(BranchReportResponse));
			registerClass(typeof(BranchRollbackRequest));
			registerClass(typeof(BranchRollbackResponse));
			registerClass(typeof(GlobalBeginRequest));
			registerClass(typeof(GlobalBeginResponse));
			registerClass(typeof(GlobalCommitRequest));
			registerClass(typeof(GlobalCommitResponse));
			registerClass(typeof(GlobalLockQueryRequest));
			registerClass(typeof(GlobalLockQueryResponse));
			registerClass(typeof(GlobalRollbackRequest));
			registerClass(typeof(GlobalRollbackResponse));
			registerClass(typeof(GlobalStatusRequest));
			registerClass(typeof(GlobalStatusResponse));
			registerClass(typeof(UndoLogDeleteRequest));
			registerClass(typeof(GlobalReportRequest));
			registerClass(typeof(GlobalReportResponse));

			registerClass(typeof(MergedWarpMessage));
			registerClass(typeof(MergeResultMessage));
			registerClass(typeof(RegisterRMRequest));
			registerClass(typeof(RegisterRMResponse));
			registerClass(typeof(RegisterTMRequest));
			registerClass(typeof(RegisterTMResponse));
		}

		/// <summary>
		/// only supposed to be called at startup time
		/// </summary>
		/// <param name="clazz"> object type </param>
		public static void registerClass(Type clazz)
		{
			registerClass(clazz, null);
		}

		/// <summary>
		/// only supposed to be called at startup time
		/// </summary>
		/// <param name="clazz"> object type </param>
		/// <param name="serializer"> object serializer </param>
		public static void registerClass(Type clazz, object serializer)
		{
			if (clazz == null)
			{
				throw new ArgumentException("Class registered cannot be null!");
			}
			REGISTRATIONS[clazz] = serializer;
		}

		/// <summary>
		/// get registered classes
		/// </summary>
		/// <returns> class serializer
		///  </returns>
		public static IDictionary<Type, object> RegisteredClasses
		{
			get
			{
				return REGISTRATIONS;
			}
		}
	}
}
