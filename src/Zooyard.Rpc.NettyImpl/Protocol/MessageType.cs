namespace Zooyard.Rpc.NettyImpl.Protocol
{
    /// <summary>
	/// The type Message codec type.
	/// 
	/// </summary>
	public class MessageType
    {

        /// <summary>
        /// The constant TYPE_GLOBAL_BEGIN.
        /// </summary>
        public const short TYPE_GLOBAL_BEGIN = 1;
        /// <summary>
        /// The constant TYPE_GLOBAL_BEGIN_RESULT.
        /// </summary>
        public const short TYPE_GLOBAL_BEGIN_RESULT = 2;
        /// <summary>
        /// The constant TYPE_GLOBAL_COMMIT.
        /// </summary>
        public const short TYPE_GLOBAL_COMMIT = 7;
        /// <summary>
        /// The constant TYPE_GLOBAL_COMMIT_RESULT.
        /// </summary>
        public const short TYPE_GLOBAL_COMMIT_RESULT = 8;
        /// <summary>
        /// The constant TYPE_GLOBAL_ROLLBACK.
        /// </summary>
        public const short TYPE_GLOBAL_ROLLBACK = 9;
        /// <summary>
        /// The constant TYPE_GLOBAL_ROLLBACK_RESULT.
        /// </summary>
        public const short TYPE_GLOBAL_ROLLBACK_RESULT = 10;
        /// <summary>
        /// The constant TYPE_GLOBAL_STATUS.
        /// </summary>
        public const short TYPE_GLOBAL_STATUS = 15;
        /// <summary>
        /// The constant TYPE_GLOBAL_STATUS_RESULT.
        /// </summary>
        public const short TYPE_GLOBAL_STATUS_RESULT = 16;
        /// <summary>
        /// The constant TYPE_GLOBAL_REPORT.
        /// </summary>
        public const short TYPE_GLOBAL_REPORT = 17;
        /// <summary>
        /// The constant TYPE_GLOBAL_REPORT_RESULT.
        /// </summary>
        public const short TYPE_GLOBAL_REPORT_RESULT = 18;
        /// <summary>
        /// The constant TYPE_GLOBAL_LOCK_QUERY.
        /// </summary>
        public const short TYPE_GLOBAL_LOCK_QUERY = 21;
        /// <summary>
        /// The constant TYPE_GLOBAL_LOCK_QUERY_RESULT.
        /// </summary>
        public const short TYPE_GLOBAL_LOCK_QUERY_RESULT = 22;

        /// <summary>
        /// The constant TYPE_BRANCH_COMMIT.
        /// </summary>
        public const short TYPE_BRANCH_COMMIT = 3;
        /// <summary>
        /// The constant TYPE_BRANCH_COMMIT_RESULT.
        /// </summary>
        public const short TYPE_BRANCH_COMMIT_RESULT = 4;
        /// <summary>
        /// The constant TYPE_BRANCH_ROLLBACK.
        /// </summary>
        public const short TYPE_BRANCH_ROLLBACK = 5;
        /// <summary>
        /// The constant TYPE_BRANCH_ROLLBACK_RESULT.
        /// </summary>
        public const short TYPE_BRANCH_ROLLBACK_RESULT = 6;
        /// <summary>
        /// The constant TYPE_BRANCH_REGISTER.
        /// </summary>
        public const short TYPE_BRANCH_REGISTER = 11;
        /// <summary>
        /// The constant TYPE_BRANCH_REGISTER_RESULT.
        /// </summary>
        public const short TYPE_BRANCH_REGISTER_RESULT = 12;
        /// <summary>
        /// The constant TYPE_BRANCH_STATUS_REPORT.
        /// </summary>
        public const short TYPE_BRANCH_STATUS_REPORT = 13;
        /// <summary>
        /// The constant TYPE_BRANCH_STATUS_REPORT_RESULT.
        /// </summary>
        public const short TYPE_BRANCH_STATUS_REPORT_RESULT = 14;

        /// <summary>
        /// The constant TYPE_SEATA_MERGE.
        /// </summary>
        public const short TYPE_SEATA_MERGE = 59;
        /// <summary>
        /// The constant TYPE_SEATA_MERGE_RESULT.
        /// </summary>
        public const short TYPE_SEATA_MERGE_RESULT = 60;

        /// <summary>
        /// The constant TYPE_REG_CLT.
        /// </summary>
        public const short TYPE_REG_CLT = 101;
        /// <summary>
        /// The constant TYPE_REG_CLT_RESULT.
        /// </summary>
        public const short TYPE_REG_CLT_RESULT = 102;
        /// <summary>
        /// The constant TYPE_REG_RM.
        /// </summary>
        public const short TYPE_REG_RM = 103;
        /// <summary>
        /// The constant TYPE_REG_RM_RESULT.
        /// </summary>
        public const short TYPE_REG_RM_RESULT = 104;
        /// <summary>
        /// The constant TYPE_RM_DELETE_UNDOLOG.
        /// </summary>
        public const short TYPE_RM_DELETE_UNDOLOG = 111;
        /// <summary>
        /// the constant TYPE_HEARTBEAT_MSG
        /// </summary>
        public const short TYPE_HEARTBEAT_MSG = 120;
    }
}
