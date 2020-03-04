using System.IO;
using System.Text;

namespace ESPlus.Wyrm
{
    public class ApplyStream : IApply
    {
        private readonly string _streamName;

        public ApplyStream(string streamName)
        {
            _streamName = streamName;
        }
        
        public void Apply(BinaryWriter writer)
        {
            writer.Write((int) 12 + _streamName.Length);
            writer.Write((int) Commands.ReadStream);
            writer.Write((int) _streamName.Length);
            writer.Write(Encoding.UTF8.GetBytes(_streamName));
        }
    }
}