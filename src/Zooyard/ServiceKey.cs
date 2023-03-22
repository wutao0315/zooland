using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zooyard.Rpc.Route;
using Zooyard.Utils;

namespace Zooyard;

public class ServiceKey
{
    public ServiceKey(string interfaceName, string version, string group)
    {
        InterfaceName = interfaceName;
        Group = group;
        Version = version;
    }

    public string InterfaceName { get; init; }

    public string Group { get; init; }

    public string Version { get; init; }

    public override bool Equals(object? o)
    {
        if (o == this)
        {
            return true;
        }
        if (o == null || GetType() != o.GetType())
        {
            return false;
        }
        ServiceKey that = (ServiceKey)o;
        return InterfaceName.Equals(that.InterfaceName) && Group.Equals(that.Group) && Version.Equals(that.Version);
    }

    public override int GetHashCode()
    {
        return InterfaceName.GetHashCode() + Group.GetHashCode()+ Version.GetHashCode();
    }

    public override string ToString()
    {
        return buildServiceKey(InterfaceName, Group, Version);

        string buildServiceKey(string path, string group, string version)
        {
            int length = path == null ? 0 : path.Length;
            length += group == null ? 0 : group.Length;
            length += version == null ? 0 : version.Length;
            length += 2;

            var buf = new StringBuilder(length);
            if (!string.IsNullOrWhiteSpace(group))
            {
                buf.Append(group).Append('/');
            }
            buf.Append(path);
            if (!string.IsNullOrWhiteSpace(version))
            {
                buf.Append(':').Append(version);
            }
            return buf.ToString();
        }
    }


    public static class Matcher
    {
        public static bool IsMatch(ServiceKey rule, ServiceKey target)
        {
            // 1. match interface (accurate match)
            if (!rule.InterfaceName.Equals(target.InterfaceName))
            {
                return false;
            }

            // 2. match version (accurate match)
            // 2.1. if rule version is *, match all
            if (!CommonConstants.ANY_VALUE.Equals(rule.Version))
            {
                // 2.2. if rule version is null, target version should be null
                if (string.IsNullOrWhiteSpace(rule.Version) && !string.IsNullOrWhiteSpace(target.Version))
                {
                    return false;
                }
                if (!string.IsNullOrWhiteSpace(rule.Version))
                {
                    // 2.3. if rule version contains ',', split and match each
                    if (rule.Version.Contains(CommonConstants.COMMA_SEPARATOR))
                    {
                        String[] versions = rule.Version.Split("\\" + CommonConstants.COMMA_SEPARATOR, -1);
                        bool match = false;
                        foreach (String version in versions)
                        {
                            var versionTrimd = version.Trim();
                            if (string.IsNullOrWhiteSpace(versionTrimd) && string.IsNullOrWhiteSpace(target.Version))
                            {
                                match = true;
                                break;
                            }
                            else if (versionTrimd.Equals(target.Version))
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
                    // 2.4. if rule version is not contains ',', match directly
                    else if (!rule.Version.Equals(target.Version))
                    {
                        return false;
                    }
                }
            }

            // 3. match group (wildcard match)
            // 3.1. if rule group is *, match all
            if (!CommonConstants.ANY_VALUE.Equals(rule.Group))
            {
                // 3.2. if rule group is null, target group should be null
                if (string.IsNullOrWhiteSpace(rule.Group) && !String.IsNullOrWhiteSpace(target.Group))
                {
                    return false;
                }
                if (!string.IsNullOrWhiteSpace(rule.Group))
                {
                    // 3.3. if rule group contains ',', split and match each
                    if (rule.Group.Contains(CommonConstants.COMMA_SEPARATOR))
                    {
                        string[] groups = rule.Group.Split("\\" + CommonConstants.COMMA_SEPARATOR, -1);
                        bool match = false;
                        foreach (var group in groups)
                        {
                            var groupTrimed = group.Trim();
                            if (string.IsNullOrWhiteSpace(groupTrimed) && string.IsNullOrWhiteSpace(target.Group))
                            {
                                match = true;
                                break;
                            }
                            else if (groupTrimed.Equals(target.Group))
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
                    // 3.4. if rule group is not contains ',', match directly
                    else if (!rule.Group.Equals(target.Group))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}