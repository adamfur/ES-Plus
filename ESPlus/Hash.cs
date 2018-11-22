using System;
using System.Data.HashFunction.xxHash;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace ESPlus
{
    public static class Hash
    {
        public static string Sha256(this string data)
        {
            var message = Encoding.ASCII.GetBytes(data);
            var hashString = new SHA256Managed();
            var hex = "";
            var hashValue = hashString.ComputeHash(message);

            foreach (byte x in hashValue)
            {
                hex += string.Format("{0:x2}", x);
            }
            
            return hex;
        }

        public static string MongoHash(this string data)
        {
            var algorithm = xxHashFactory.Instance.Create(new xxHashConfig() { HashSizeInBits = 64 });
            var hash = algorithm.ComputeHash(Encoding.UTF8.GetBytes(data));

            return hash.AsHexString() + "00000000"; // 24 bytes
        }
    }
}