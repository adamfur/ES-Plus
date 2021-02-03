using System;
using System.Data.HashFunction.xxHash;
using System.Security.Cryptography;
using System.Text;

namespace ESPlus
{
    public static class Hash
    {
        public static string Sha256(this string data)
        {
            var message = Encoding.ASCII.GetBytes(data);

            return message.Sha256();
        }

        public static string Sha256(this byte[] message)
        {
            var hashString = new SHA256Managed();
            var result = new StringBuilder();
            var hashValue = hashString.ComputeHash(message);

            foreach (byte x in hashValue)
            {
                result.Append($"{x:x2}");
            }

            return result.ToString();
        }

        public static Int64 XXH64(this string data)
        {
            var algorithm = xxHashFactory.Instance.Create(new xxHashConfig { HashSizeInBits = 64 });
            var bytes = Encoding.UTF8.GetBytes(data);
            var hash = algorithm.ComputeHash(bytes);

            return BitConverter.ToInt64(hash.Hash, 0);
        }
    }
}