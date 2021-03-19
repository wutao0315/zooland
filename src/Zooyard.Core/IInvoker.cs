using System.Threading.Tasks;

namespace Zooyard.Core
{
    public interface IInvoker
    {
        object Instance { get; }
        int ClientTimeout { get; }
        Task<IResult> Invoke(IInvocation invocation);
    }
}
