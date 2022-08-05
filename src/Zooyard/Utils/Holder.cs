using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zooyard.Utils;

public class Holder<T>
    where T : class
{
    private volatile T? _value;

    public T? Value { get => _value; set => _value = value; }
}
