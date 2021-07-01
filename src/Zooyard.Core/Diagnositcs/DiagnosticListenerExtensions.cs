using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Zooyard.Core.Diagnositcs
{
    public class Constant
    {
        public const string DiagnosticListenerName = "ZooyardDiagnosticListener";

        public const string ZooyardPrefix = "Zooyard.Rpc.";

        public const string ConsumerBefore = ZooyardPrefix + nameof(ConsumerBefore);
        public const string ConsumerAfter = ZooyardPrefix + nameof(ConsumerAfter);
        public const string ConsumerError = ZooyardPrefix + nameof(ConsumerError);

        public const string ProviderBefore = ZooyardPrefix + nameof(ProviderBefore);
        public const string ProviderAfter = ZooyardPrefix + nameof(ProviderAfter);
        public const string ProviderError = ZooyardPrefix + nameof(ProviderError);
    }
    public static class DiagnosticListenerExtensions
    {
        public static void WriteConsumerBefore(this DiagnosticSource _this, object instance, URL url, IInvocation invocation)
        {
            if (!_this.IsEnabled(Constant.ConsumerBefore))
            {
                return;
            }
            _this.Write(Constant.ConsumerBefore, new { instance, url, invocation  });
        }
        public static void WriteConsumerAfter<T>(this DiagnosticSource _this, URL url, IInvocation invocation, IResult<T> result)
        {
            if (!_this.IsEnabled(Constant.ConsumerAfter))
            {
                return;
            }
            _this.Write(Constant.ConsumerAfter, new { url, invocation, result });
        }
        public static void WriteConsumerError(this DiagnosticSource _this, URL url, IInvocation invocation, Exception exception)
        {
            if (!_this.IsEnabled(Constant.ConsumerAfter))
            {
                return;
            }
            _this.Write(Constant.ConsumerAfter, new { url, invocation, exception });
        }
    }
}