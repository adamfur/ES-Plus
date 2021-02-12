using System;
using System.Transactions;

namespace ESPlus.Interfaces
{
    public class StringPair : IEquatable<StringPair>
    {
        public string Path { get; }
        public string Tenant { get; }

        public StringPair(string path, string tenant)
        {
            Path = path;
            Tenant = tenant;
        }

        public bool Equals(StringPair other)
        {
            if (Path == null || other.Path == null)
            {
                return (Path == other.Path);
            }
            else if (Tenant == null || other.Tenant == null)
            {
                return (Tenant == other.Tenant);
            }
                
            return Path.Equals(other.Path)
                   && Tenant.Equals(other.Tenant);
        }

        public override int GetHashCode()
        {
            var tenant = Tenant ?? "";
            var empty = Path ?? "";
            
            return new {tenant, empty}.GetHashCode();
        }
    }
}