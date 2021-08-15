using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZooTa.Core.Constant
{
	/// <summary>
	/// The redis key constants
	/// 
	/// </summary>
	public class RedisKeyConstants
	{

		/// <summary>
		/// The constant redis key of global transaction name xid
		/// </summary>
		public const string REDIS_KEY_GLOBAL_XID = "xid";

		/// <summary>
		/// The constant redis key of global transaction name transactionId
		/// </summary>
		public const string REDIS_KEY_GLOBAL_TRANSACTION_ID = "transactionId";

		/// <summary>
		/// The constant redis key of global transaction name status
		/// </summary>
		public const string REDIS_KEY_GLOBAL_STATUS = "status";

		/// <summary>
		/// The constant redis key of global transaction name applicationId
		/// </summary>
		public const string REDIS_KEY_GLOBAL_APPLICATION_ID = "applicationId";

		/// <summary>
		/// The constant redis key of global transaction name transactionServiceGroup
		/// </summary>
		public const string REDIS_KEY_GLOBAL_TRANSACTION_SERVICE_GROUP = "transactionServiceGroup";

		/// <summary>
		/// The constant redis key of global transaction name transactionName
		/// </summary>
		public const string REDIS_KEY_GLOBAL_TRANSACTION_NAME = "transactionName";

		/// <summary>
		/// The constant redis key of global transaction name timeout
		/// </summary>
		public const string REDIS_KEY_GLOBAL_TIMEOUT = "timeout";

		/// <summary>
		/// The constant redis key of global transaction name beginTime
		/// </summary>
		public const string REDIS_KEY_GLOBAL_BEGIN_TIME = "beginTime";

		/// <summary>
		/// The constant redis key of global transaction name applicationData
		/// </summary>
		public const string REDIS_KEY_GLOBAL_APPLICATION_DATA = "applicationData";

		/// <summary>
		/// The constant redis key of global transaction name gmtCreate
		/// </summary>
		public const string REDIS_KEY_GLOBAL_GMT_CREATE = "gmtCreate";

		/// <summary>
		/// The constant redis key of global transaction name gmtModified
		/// </summary>
		public const string REDIS_KEY_GLOBAL_GMT_MODIFIED = "gmtModified";





		/// <summary>
		/// The constant redis key of branch transaction name branchId
		/// </summary>
		public const string REDIS_KEY_BRANCH_BRANCH_ID = "branchId";

		/// <summary>
		/// The constant redis key of branch transaction name xid
		/// </summary>
		public const string REDIS_KEY_BRANCH_XID = "xid";

		/// <summary>
		/// The constant redis key of branch transaction name transactionId
		/// </summary>
		public const string REDIS_KEY_BRANCH_TRANSACTION_ID = "transactionId";

		/// <summary>
		/// The constant redis key of branch transaction name resourceGroupId
		/// </summary>
		public const string REDIS_KEY_BRANCH_RESOURCE_GROUP_ID = "resourceGroupId";

		/// <summary>
		/// The constant redis key of branch transaction name resourceId
		/// </summary>
		public const string REDIS_KEY_BRANCH_RESOURCE_ID = "resourceId";

		/// <summary>
		///REDIS_
		/// The constant redis key of branch transaction name branchType
		/// </summary>
		public const string REDIS_KEY_BRANCH_BRANCH_TYPE = "branchType";

		/// <summary>
		/// The constant redis key of branch transaction name status
		/// </summary>
		public const string REDIS_KEY_BRANCH_STATUS = "status";

		/// <summary>
		/// The constant redis key of branch transaction name beginTime
		/// </summary>
		public const string REDIS_KEY_BRANCH_BEGIN_TIME = "beginTime";

		/// <summary>
		/// The constant redis key of branch transaction name applicationData
		/// </summary>
		public const string REDIS_KEY_BRANCH_APPLICATION_DATA = "applicationData";

		/// <summary>
		/// The constant redis key of branch transaction name clientId
		/// </summary>
		public const string REDIS_KEY_BRANCH_CLIENT_ID = "clientId";

		/// <summary>
		/// The constant redis key of branch transaction name gmtCreate
		/// </summary>
		public const string REDIS_KEY_BRANCH_GMT_CREATE = "gmtCreate";

		/// <summary>
		/// The constant redis key of branch transaction name gmtModified
		/// </summary>
		public const string REDIS_KEY_BRANCH_GMT_MODIFIED = "gmtModified";

	}
}
