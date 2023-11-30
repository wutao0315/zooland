namespace Zooyard.Rpc;

public sealed class RpcException : Exception
{
    public const int UNKNOWN_EXCEPTION = 0;

    public const int NETWORK_EXCEPTION = 1;

    public const int TIMEOUT_EXCEPTION = 2;

    public const int BIZ_EXCEPTION = 3;

    public const int FORBIDDEN_EXCEPTION = 4;

    public const int SERIALIZATION_EXCEPTION = 5;

    public RpcException()
        : base()
    {
    }

    public RpcException(string? message, Exception? cause)
        : base(message, cause)
    {

    }

    public RpcException(string? message)
        : base(message)
    {
    }

    public RpcException(Exception cause)
        : base(cause.Message, cause)
    {
    }

    public RpcException(int code)
        : base()
    {
        this.Code = code;
    }

    public RpcException(int code, string? message, Exception? cause)
        : base(message, cause)
    {
        this.Code = code;
    }

    public RpcException(int code, string? message)
        : base(message)
    {
        this.Code = code;
    }

    public RpcException(int code, Exception cause)
        : base(cause.Message, cause)
    {
        this.Code = code;
    }

    public int Code { set; get; }


    public bool Biz
    {
        get
        {
            return Code == BIZ_EXCEPTION;
        }
    }

    public bool Forbidded
    {
        get
        {
            return Code == FORBIDDEN_EXCEPTION;
        }
    }

    public bool Timeout
    {
        get
        {
            return Code == TIMEOUT_EXCEPTION;
        }
    }

    public bool Network
    {
        get
        {
            return Code == NETWORK_EXCEPTION;
        }
    }

    public bool Serialization
    {
        get
        {
            return Code == SERIALIZATION_EXCEPTION;
        }
    }
}
