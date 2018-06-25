using System.Runtime.InteropServices;

namespace ESPlus
{
    public class LZ4
    {
        [DllImport("liblz4")]
        public static extern int LZ4_compress_default(byte[] source, byte[] dest, int sourceSize, int maxDestSize);

        [DllImport("liblz4")]
        public static extern int LZ4_compressBound(int inputSize);

        [DllImport("liblz4")]
        public static extern int LZ4_decompress_safe(byte[] source, byte[] dest, int compressedSize, int maxDecompressedSize);
    }
}