using System.IO;

namespace ESPlus.Wyrm
{
    public interface IApply
    {
        void Apply(BinaryWriter writer);
    }
}