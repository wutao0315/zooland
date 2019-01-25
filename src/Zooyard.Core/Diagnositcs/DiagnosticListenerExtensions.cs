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

        public const string ZooyardPrefix = "Zooyard.";

        public const string ConsumerBefore = ZooyardPrefix + nameof(ConsumerBefore);
        public const string ConsumerAfter = ZooyardPrefix + nameof(ConsumerAfter);
        public const string ConsumerError = ZooyardPrefix + nameof(ConsumerError);

        public const string ProviderBefore = ZooyardPrefix + nameof(ProviderBefore);
        public const string ProviderAfter = ZooyardPrefix + nameof(ProviderAfter);
        public const string ProviderError = ZooyardPrefix + nameof(ProviderError);
    }
    public static class DiagnosticListenerExtensions
    {
        public static void WriteConsumerBefore(this DiagnosticSource _this, IInvocation invocation)
        {
            if (!_this.IsEnabled(Constant.ConsumerBefore))
            {
                return;
            }
            _this.Write(Constant.ConsumerBefore, new { invocation });
        }
        public static void WriteConsumerAfter(this DiagnosticSource _this, IInvocation invocation, IResult clusterResult)
        {
            if (!_this.IsEnabled(Constant.ConsumerAfter))
            {
                return;
            }
            _this.Write(Constant.ConsumerAfter, new { invocation, clusterResult });
        }
        public static void WriteConsumerError(this DiagnosticSource _this, Exception exception)
        {
            if (!_this.IsEnabled(Constant.ConsumerAfter))
            {
                return;
            }
            _this.Write(Constant.ConsumerAfter, new { exception });
        }

        public static void WriteProviderBefore(this DiagnosticSource _this)
        {
            if (!_this.IsEnabled(Constant.ProviderBefore))
            {
                return;
            }
            _this.Write(Constant.ProviderBefore, new { });
        }
        public static void WriteProviderAfter(this DiagnosticSource _this)
        {
            if (!_this.IsEnabled(Constant.ProviderAfter))
            {
                return;
            }
            _this.Write(Constant.ProviderAfter
                , new { });
        }
        public static void WriteProviderError(this DiagnosticSource _this)
        {
            if (!_this.IsEnabled(Constant.ProviderError))
            {
                return;
            }
            _this.Write(Constant.ProviderError
                , new { });
        }
    }
}