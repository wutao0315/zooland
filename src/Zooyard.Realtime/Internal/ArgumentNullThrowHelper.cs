using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Zooyard.Realtime.Internal;


#nullable enable

internal static partial class ArgumentNullThrowHelper
{
    /// <summary>Throws an <see cref="ArgumentNullException"/> if <paramref name="argument"/> is null.</summary>
    /// <param name="argument">The reference type argument to validate as non-null.</param>
    /// <param name="paramName">The name of the parameter with which <paramref name="argument"/> corresponds.</param>
    public static void ThrowIfNull(
#if INTERNAL_NULLABLE_ATTRIBUTES || NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
        [NotNull]
#endif
        object? argument, [CallerArgumentExpression("argument")] string? paramName = null)
    {
#if !NET7_0_OR_GREATER || NETSTANDARD || NETFRAMEWORK
        if (argument is null)
        {
            Throw(paramName);
        }
#else
        ArgumentNullException.ThrowIfNull(argument, paramName);
#endif
    }

#if !NET7_0_OR_GREATER || NETSTANDARD || NETFRAMEWORK
#if INTERNAL_NULLABLE_ATTRIBUTES || NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
    [DoesNotReturn]
#endif
    internal static void Throw(string? paramName) =>
        throw new ArgumentNullException(paramName);
#endif
}
