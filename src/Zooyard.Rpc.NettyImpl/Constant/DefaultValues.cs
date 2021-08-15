using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zooyard.Rpc.NettyImpl.Constant
{
	public class DefaultValues
	{
		public const int DEFAULT_CLIENT_LOCK_RETRY_INTERVAL = 10;
		public const int DEFAULT_TM_DEGRADE_CHECK_ALLOW_TIMES = 10;
		public const int DEFAULT_CLIENT_LOCK_RETRY_TIMES = 30;
		public const bool DEFAULT_CLIENT_LOCK_RETRY_POLICY_BRANCH_ROLLBACK_ON_CONFLICT = true;
		public const int DEFAULT_LOG_EXCEPTION_RATE = 100;
		public const int DEFAULT_CLIENT_ASYNC_COMMIT_BUFFER_LIMIT = 10000;
		public const int DEFAULT_TM_DEGRADE_CHECK_PERIOD = 2000;
		public const int DEFAULT_CLIENT_REPORT_RETRY_COUNT = 5;
		public const bool DEFAULT_CLIENT_REPORT_SUCCESS_ENABLE = false;
		public const bool DEFAULT_CLIENT_TABLE_META_CHECK_ENABLE = false;
		public const long DEFAULT_TABLE_META_CHECKER_INTERVAL = 60000L;
		public const bool DEFAULT_TM_DEGRADE_CHECK = false;
		public const bool DEFAULT_CLIENT_SAGA_BRANCH_REGISTER_ENABLE = false;
		public const bool DEFAULT_CLIENT_SAGA_RETRY_PERSIST_MODE_UPDATE = false;
		public const bool DEFAULT_CLIENT_SAGA_COMPENSATE_PERSIST_MODE_UPDATE = false;
		public const int DEFAULT_SHUTDOWN_TIMEOUT_SEC = 3;
		public const int DEFAULT_SELECTOR_THREAD_SIZE = 1;
		public const int DEFAULT_BOSS_THREAD_SIZE = 1;
		public const string DEFAULT_SELECTOR_THREAD_PREFIX = "NettyClientSelector";
		public const string DEFAULT_WORKER_THREAD_PREFIX = "NettyClientWorkerThread";
		public const bool DEFAULT_ENABLE_CLIENT_BATCH_SEND_REQUEST = true;
		public const string DEFAULT_BOSS_THREAD_PREFIX = "NettyBoss";
		public const string DEFAULT_NIO_WORKER_THREAD_PREFIX = "NettyServerNIOWorker";
		public const string DEFAULT_EXECUTOR_THREAD_PREFIX = "NettyServerBizHandler";
		public const bool DEFAULT_TRANSPORT_HEARTBEAT = true;
		public const bool DEFAULT_TRANSACTION_UNDO_DATA_VALIDATION = true;
		public const string DEFAULT_TRANSACTION_UNDO_LOG_SERIALIZATION = "jackson";
		public const bool DEFAULT_ONLY_CARE_UPDATE_COLUMNS = true;
		public const string DEFAULT_TRANSACTION_UNDO_LOG_TABLE = "undo_log";
		public const string DEFAULT_STORE_DB_GLOBAL_TABLE = "global_table";
		public const string DEFAULT_STORE_DB_BRANCH_TABLE = "branch_table";
		public const string DEFAULT_LOCK_DB_TABLE = "lock_table";
		public const int DEFAULT_TM_COMMIT_RETRY_COUNT = 5;
		public const int DEFAULT_TM_ROLLBACK_RETRY_COUNT = 5;
		public const int DEFAULT_GLOBAL_TRANSACTION_TIMEOUT = 60000;
		public const string DEFAULT_TX_GROUP = "my_test_tx_group";
		public const string DEFAULT_TC_CLUSTER = "default";
		public const string DEFAULT_GROUPLIST = "127.0.0.1:8091";
		public const string DEFAULT_DATA_SOURCE_PROXY_MODE = "AT";
		public const bool DEFAULT_DISABLE_GLOBAL_TRANSACTION = false;
		public const int SERVER_DEFAULT_PORT = 8091;
		public const string SERVER_DEFAULT_STORE_MODE = "file";
		public const string DEFAULT_SAGA_JSON_PARSER = "fastjson";
		public const bool DEFAULT_SERVER_ENABLE_CHECK_AUTH = true;
		public const string DEFAULT_LOAD_BALANCE = "RandomLoadBalance";
		public const int VIRTUAL_NODES_DEFAULT = 10;
		public const bool DEFAULT_CLIENT_UNDO_COMPRESS_ENABLE = true;
		public const string DEFAULT_CLIENT_UNDO_COMPRESS_TYPE = "zip";
		public const string DEFAULT_CLIENT_UNDO_COMPRESS_THRESHOLD = "64k";
		public const int DEFAULT_RETRY_DEAD_THRESHOLD = 2 * 60 * 1000 + 10 * 1000;
		public static readonly int TM_INTERCEPTOR_ORDER = int.MinValue + 1000;
		public static readonly int TCC_ACTION_INTERCEPTOR_ORDER = int.MinValue + 1000;
	}
}
