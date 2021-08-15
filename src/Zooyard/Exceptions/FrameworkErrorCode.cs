using System;


namespace Zooyard.Exceptions
{
    public enum FrameworkErrorCode 
    {
        /// <summary>
        /// 0001 ~ 0099  Configuration related errors
        /// </summary>
        [ErrorCode("0004", "Thread pool is full", "Please check the thread pool configuration")]
        ThreadPoolFull,
        /// <summary>
        /// The Init services client error.
        /// </summary>
        [ErrorCode("0008", "Seata app name or seata server group is null", "Please check your configuration")]
        InitSeataClientError,
        /// <summary>
        /// The Null rule error.
        /// </summary>
        [ErrorCode("0010", "Services rules is null", "Please check your configuration")]
        NullRuleError,
        /// <summary>
        /// 0101 ~ 0199 Network related error. (Not connected, disconnected, dispatched, etc.)
        /// </summary>
        [ErrorCode("0101", "Can not connect to the server", "Please check if the seata service is started. Is the network connection to the seata server normal?")]
        NetConnect,
        /// <summary>
        /// The Net reg appname.
        /// </summary>
        [ErrorCode("0102", "Register client app name failed", "Please check if the seata service is started. Is the network connection to the seata server normal?")]
        NetRegAppname,
        /// <summary>
        /// The Net disconnect.
        /// </summary>
        [ErrorCode("0103", "Seata connection closed", "The network is disconnected. Please check the network connection to the client or seata server.")]
        NetDisconnect,
        /// <summary>
        /// The Net dispatch.
        /// </summary>
        [ErrorCode("0104", "Dispatch error", "Network processing error. Please check the network connection to the client or seata server.")]
        NetDispatch,
        /// <summary>
        /// The Net on message.
        /// </summary>
        [ErrorCode("0105", "On message error", "Network processing error. Please check the network connection to the client or seata server.")]
        NetOnMessage,
        /// <summary>
        /// Get channel error framework error code.
        /// </summary>
        [ErrorCode("0106", "Get channel error", "Get channel error")]
        getChannelError,
        /// <summary>
        /// Channel not writable framework error code.
        /// </summary>
        [ErrorCode("0107", "Channel not writable", "Channel not writable")]
        ChannelNotWritable,
        /// <summary>
        /// Send half message failed framework error code.
        /// </summary>
        [ErrorCode("0108", "Send half message failed", "Send half message failed")]
        SendHalfMessageFailed,
        /// <summary>
        /// Channel is not writable framework error code.
        /// </summary>
        [ErrorCode("0109", "Channel is not writable", "Channel is not writable")]
        ChannelIsNotWritable,
        /// <summary>
        /// No available service framework error code.
        /// </summary>
        [ErrorCode("0110", "No available service", "No available service")]
        NoAvailableService,
        /// <summary>
        /// Invalid configuration framework error code.
        /// </summary>
        [ErrorCode("0201", "Invalid configuration", "Invalid configuration")]
        InvalidConfiguration,
        /// <summary>
        /// Exception caught framework error code.
        /// </summary>
        [ErrorCode("0318", "Exception caught", "Exception caught")]
        ExceptionCaught,
        /// <summary>
        /// Register rm framework error code.
        /// </summary>
        [ErrorCode("0304", "Register RM failed", "Register RM failed")]
        RegisterRM,
        /// <summary>
        /// 0400~0499 Saga相关错误
        /// Process type not found
        /// </summary>
        [ErrorCode("0401", "Process type not found", "Process type not found")]
        ProcessTypeNotFound,
        /// <summary>
        /// Process handler not found
        /// </summary>
        [ErrorCode("0402", "Process handler not found", "Process handler not found")]
        ProcessHandlerNotFound,
        /// <summary>
        /// Process router not found
        /// </summary>
        [ErrorCode("0403", "Process router not found", "Process router not found")]
        ProcessRouterNotFound,
        /// <summary>
        /// method not public
        /// </summary>
        [ErrorCode("0404", "method not public", "method not public")]
        MethodNotPublic,
        /// <summary>
        /// method invoke error
        /// </summary>
        [ErrorCode("0405", "method invoke error", "method invoke error")]
        MethodInvokeError,
        /// <summary>
        /// CompensationState not found
        /// </summary>
        [ErrorCode("0406", "CompensationState not found", "CompensationState not found")]
        CompensationStateNotFound,
        /// <summary>
        /// Evaluation returns null
        /// </summary>
        [ErrorCode("0407", "Evaluation returns null", "Evaluation returns null")]
        EvaluationReturnsNull,
        /// <summary>
        /// Evaluation returns non-Boolean
        /// </summary>
        [ErrorCode("0408", "Evaluation returns non-Boolean", "Evaluation returns non-Boolean")]
        EvaluationReturnsNonBoolean,
        /// <summary>
        /// Not a exception class
        /// </summary>
        [ErrorCode("0409", "Not a exception class", "Not a exception class")]
        NotExceptionClass,
        /// <summary>
        /// No such method
        /// </summary>
        [ErrorCode("0410", "No such method", "No such method")]
        NoSuchMethod,
        /// <summary>
        /// Object not exists
        /// </summary>
        [ErrorCode("0411", "Object not exists", "Object not exists")]
        ObjectNotExists,
        /// <summary>
        /// Parameter required
        /// </summary>
        [ErrorCode("0412", "Parameter required", "Parameter required")]
        ParameterRequired,
        /// <summary>
        /// Variables assign error
        /// </summary>
        [ErrorCode("0413", "Variables assign error", "Variables assign error")]
        VariablesAssignError,
        /// <summary>
        /// No matched status
        /// </summary>
        [ErrorCode("0414", "No matched status", "No matched status")]
        NoMatchedStatus,
        /// <summary>
        /// Asynchronous start disabled
        /// </summary>
        [ErrorCode("0415", "Asynchronous start disabled", "Asynchronous start disabled")]
        AsynchronousStartDisabled,
        /// <summary>
        /// Operation denied
        /// </summary>
        [ErrorCode("0416", "Operation denied", "Operation denied")]
        OperationDenied,
        /// <summary>
        /// Context variable replay failed
        /// </summary>
        [ErrorCode("0417", "Context variable replay failed", "Context variable replay failed")]
        ContextVariableReplayFailed,
        /// <summary>
        /// Context variable replay failed
        /// </summary>
        [ErrorCode("0418", "Invalid parameter", "Invalid parameter")]
        InvalidParameter,
        /// <summary>
        /// Invoke transaction manager error
        /// </summary>
        [ErrorCode("0419", "Invoke transaction manager error", "Invoke transaction manager error")]
        TransactionManagerError,
        /// <summary>
        /// State machine instance not exists
        /// </summary>
        [ErrorCode("0420", "State machine instance not exists", "State machine instance not exists")]
        StateMachineInstanceNotExists,
        /// <summary>
        /// State machine execution timeout
        /// </summary>
        [ErrorCode("0421", "State machine execution timeout", "State machine execution timeout")]
        StateMachineExecutionTimeout,
        /// <summary>
        /// State machine execution no choice matched
        /// </summary>
        [ErrorCode("0422", "State machine no choice matched", "State machine no choice matched")]
        StateMachineNoChoiceMatched,

        /// <summary>
        /// TCC fence datasource need injected
        /// </summary>
        [ErrorCode("0501", "TCC fence datasource need injected", "TCC fence datasource need injected")]
        DateSourceNeedInjected,

        /// <summary>
        /// TCC fence record already exists
        /// </summary>
        [ErrorCode("0502", "TCC fence record already exists", "TCC fence record already exists")]
        RecordAlreadyExists,

        /// <summary>
        /// Insert tcc fence record error
        /// </summary>
        [ErrorCode("0503", "Insert tcc fence record error", "Insert tcc fence record error")]
        InsertRecordError,

        /// <summary>
        /// Insert tcc fence record duplicate key exception
        /// </summary>
        [ErrorCode("0504", "Insert tcc fence record duplicate key exception", "Insert tcc fence record duplicate key exception")]
        DuplicateKeyException,

        /// <summary>
        /// TCC fence transactionManager need injected
        /// </summary>
        [ErrorCode("0505", "TCC fence transactionManager need injected", "TCC fence transactionManager need injected")]
        TransactionManagerNeedInjected,

        /// <summary>
        /// Undefined error
        /// </summary>
        [ErrorCode("10000", "Unknown error", "Internal error")]
        UnknownAppError
    }

    public static class FrameworkErrorCodeExtensions 
    {
        public static ErrorCodeAttribute GetErrorCode(this FrameworkErrorCode errorCode) 
        {
            var type = errorCode.GetType();
            var item = type.GetField(errorCode.ToString());
            var atts = item.GetCustomAttributes(typeof(ErrorCodeAttribute), false);
            if (atts != null && atts.Length != 0)
            {
                var description = (ErrorCodeAttribute)atts[0];//获取特性的描述信息；  description就是特性中的描述信息
                return description;
            }
            return new ErrorCodeAttribute("","","");
        }
        public static string GetErrCode(this FrameworkErrorCode errorCode) 
        {
            var result = errorCode.GetErrorCode();
            return result.ErrCode;
        }
        public static string GetErrMessage(this FrameworkErrorCode errorCode) 
        {
            var result = errorCode.GetErrorCode();
            return result.ErrMessage;
        }
        public static string GetErrDispose(this FrameworkErrorCode errorCode) 
        {
            var result = errorCode.GetErrorCode();
            return result.ErrDispose;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class ErrorCodeAttribute : Attribute
    {
        public ErrorCodeAttribute(string errCode, string errMessage, string errDispose) 
        {
            this.ErrCode = errCode;
            this.ErrMessage = errMessage;
            this.ErrDispose = errDispose;
        }
        public string ErrCode { get; }

        public string ErrMessage { get; }

        public string ErrDispose { get; }
    }
}