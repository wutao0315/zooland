using System;
using System.Collections.Generic;
using System.Text;

namespace ZooTa.Core.Constant
{
    /// <summary>
	/// client table columns name.
	/// 
	/// </summary>
	public class ClientTableColumnsName
    {

        /// <summary>
        /// The constant undo_log column name xid
        /// </summary>
        public const string UNDO_LOG_XID = "xid";

        /// <summary>
        /// The constant undo_log column name branch_id
        /// </summary>
        public const string UNDO_LOG_BRANCH_XID = "branch_id";

        /// <summary>
        /// The constant undo_log column name context
        /// </summary>
        public const string UNDO_LOG_CONTEXT = "context";

        /// <summary>
        /// The constant undo_log column name rollback_info
        /// </summary>
        public const string UNDO_LOG_ROLLBACK_INFO = "rollback_info";

        /// <summary>
        /// The constant undo_log column name log_status
        /// </summary>
        public const string UNDO_LOG_LOG_STATUS = "log_status";

        /// <summary>
        /// The constant undo_log column name log_created
        /// </summary>
        public const string UNDO_LOG_LOG_CREATED = "log_created";

        /// <summary>
        /// The constant undo_log column name log_modified
        /// </summary>
        public const string UNDO_LOG_LOG_MODIFIED = "log_modified";
    }
}
