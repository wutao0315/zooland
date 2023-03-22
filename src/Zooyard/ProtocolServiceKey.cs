using System.Linq;
using Zooyard.Utils;

namespace Zooyard;

public class ProtocolServiceKey: ServiceKey
{

    public ProtocolServiceKey(string interfaceName, string version, string group, string protocol)
        :base(interfaceName, version, group)
    {
        Protocol = protocol;
    }

    public string Protocol { get; init; }

    public string GetServiceKeyString()
    {
        return base.ToString();
    }

    public bool IsSameWith(ProtocolServiceKey protocolServiceKey)
    {
        // interface version group should be the same
        if (!base.Equals(protocolServiceKey))
        {
            return false;
        }

        // origin protocol is *, can not match any protocol
        if (CommonConstants.ANY_VALUE.Equals(Protocol))
        {
            return false;
        }

        // origin protocol is null, can match any protocol
        if (string.IsNullOrWhiteSpace(Protocol) || string.IsNullOrWhiteSpace(protocolServiceKey.Protocol))
        {
            return true;
        }

        // origin protocol is not *, match itself
        return Protocol.Equals(protocolServiceKey.Protocol);
    }

    public override bool Equals(object? o)
    {
        if (this == o)
        {
            return true;
        }
        if (o == null || GetType() != o.GetType())
        {
            return false;
        }
        if (!base.Equals(o))
        {
            return false;
        }
        ProtocolServiceKey that = (ProtocolServiceKey)o;
        return Protocol.Equals(that.Protocol);
    }

    public override int GetHashCode()
    {
        return base.GetHashCode() + Protocol.GetHashCode();
    }

    public override string ToString()
    {
        return base.ToString() + CommonConstants.GROUP_CHAR_SEPARATOR + Protocol;
    }

    public new static class Matcher
    {
        public static bool IsMatch(ProtocolServiceKey rule, ProtocolServiceKey target)
        {
            // 1. 2. 3. match interface / version / group
            if (!ServiceKey.Matcher.IsMatch(rule, target))
            {
                return false;
            }

            // 4.match protocol
            // 4.1. if rule group is *, match all
            if (!CommonConstants.ANY_VALUE.Equals(rule.Protocol))
            {
                // 4.2. if rule protocol is null, match all
                if (!string.IsNullOrWhiteSpace(rule.Protocol))
                {
                    // 4.3. if rule protocol contains ',', split and match each
                    if (rule.Protocol.Contains(CommonConstants.COMMA_SEPARATOR))
                    {
                        String[] protocols = rule.Protocol.Split("\\" + CommonConstants.COMMA_SEPARATOR, -1);
                        bool match = false;
                        foreach (string protocol in protocols)
                        {
                            var protocolTrimd = protocol.Trim();
                            if (string.IsNullOrEmpty(protocolTrimd) && string.IsNullOrWhiteSpace(target.Protocol))
                            {
                                match = true;
                                break;
                            }
                            else if (protocolTrimd.Equals(target.Protocol))
                            {
                                match = true;
                                break;
                            }
                        }
                        if (!match)
                        {
                            return false;
                        }
                    }
                    // 4.3. if rule protocol is not contains ',', match directly
                    else if (!rule.Protocol.Equals(target.Protocol))
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
