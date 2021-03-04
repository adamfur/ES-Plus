using System;
using System.Transactions;

namespace ESPlus.Interfaces
{
    public class StringPair : IEquatable<StringPair>
    {
        public string Path { get; }
        public string Tenant { get; }

        public StringPair(string tenant, string path)
        {
            Path = path;
            Tenant = tenant;
        }

        public bool Equals(StringPair other)
        {
            bool Compare(string first, string second)
            {
                if (first != second)
                {
                    return false;
                }

                if (first is null)
                {
                    return true;
                }

                return first.Equals(second);
            }


            if (!Compare(Tenant, other.Tenant))
            {
                return false;
            }

            return Compare(Path, other.Path);
        }

        public override int GetHashCode()
        {
            var tenant = Tenant ?? "<@>";
            var empty = Path ?? "<@>";
            
            return new {tenant, empty}.GetHashCode();
        }
    }
}