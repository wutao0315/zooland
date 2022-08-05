using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zooyard.Rpc.Route
{
    public class RouterResult<T>
    {
        private readonly bool _needContinueRoute;
        private readonly List<T>? _result;
        private readonly string? _message;

        public RouterResult(List<T>? result)
        {
            _needContinueRoute = true;
            _result = result;
            _message = null;
        }

        public RouterResult(List<T> result, string message)
        {
            _needContinueRoute = true;
            _result = result;
            _message = message;
        }

        public RouterResult(bool needContinueRoute, List<T> result, string message)
        {
            _needContinueRoute = needContinueRoute;
            _result = result;
            _message = message;
        }

        public bool IsNeedContinueRoute => _needContinueRoute;

        public List<T>? Result => _result;
        public string? Message => _message;
    }
}
