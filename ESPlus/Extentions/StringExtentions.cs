using System.Text;

namespace ESPlus.Extentions
{
    public static class StringExtentions
    {
        public static string AsHexString(this byte[] bytes)
        {
            var hex = new StringBuilder(bytes.Length * 2);

            foreach (byte b in bytes)
            {
                hex.AppendFormat("{0:x2}", b);
            }

            return hex.ToString();
        }
    }
}