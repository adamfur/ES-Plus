using System.IO;

namespace ESPlus.Wyrm
{
    public class ApplyPosition : IApply
    {
        private readonly Position _position;

        public ApplyPosition(Position position)
        {
            _position = position;
        }
        
        public void Apply(BinaryWriter writer)
        {
            writer.Write((int) 40);
            writer.Write((int) Commands.ReadFrom);
            writer.Write(_position.Binary);
        }
    }
}