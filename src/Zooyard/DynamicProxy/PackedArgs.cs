namespace Zooyard.DynamicProxy;

public record PackedArgs
{
    public const int DispatchProxyPosition = 0;
    public const int DeclaringTypePosition = 1;
    public const int MethodTokenPosition = 2;
    public const int ArgsPosition = 3;
    public const int GenericTypesPosition = 4;
    public const int ReturnValuePosition = 5;

    public static readonly Type[] PackedTypes = [typeof(object), typeof(Type), typeof(int), typeof(object[]), typeof(Type[]), typeof(object)];

    private readonly object[] _args;

    internal PackedArgs() : this(new object[PackedTypes.Length])
    {
    }

    public PackedArgs(object[] args)
    {
        _args = args;
    }

    internal ProxyExecutor DispatchProxy => (ProxyExecutor)_args[DispatchProxyPosition];
    internal Type DeclaringType => (Type)_args[DeclaringTypePosition];
    internal int MethodToken => (int)_args[MethodTokenPosition];
    internal object[] Args => (object[])_args[ArgsPosition];
    internal Type[] GenericTypes => (Type[])_args[GenericTypesPosition];

    internal object? ReturnValue
    {
        /*get { return args[ReturnValuePosition]; }*/
        set => _args[ReturnValuePosition] = value!;
    }
}
