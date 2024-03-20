﻿namespace RpcContractHttp;

public class HelloResult
{
    public string Name { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string Head { get; set; } = string.Empty;
}

public class Result<T>
{
    public string Code { get; set; }
    public string Message { get; set; }
    public T Data { get; set; }

}
