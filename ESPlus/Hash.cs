using System;
using System.Data.HashFunction.xxHash;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using MongoDB.Bson;

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
                result.Append(string.Format("{0:x2}", x));
            }

            return result.ToString();
        }

        public static ObjectId MongoHash(this string data)
        {
            var algorithm = xxHashFactory.Instance.Create(new xxHashConfig { HashSizeInBits = 64 });
            var hash = algorithm.ComputeHash(Encoding.UTF8.GetBytes(data));

            return ObjectId.Parse(hash.AsHexString() + "00000000"); // 24 bytes
        }

        public static Int64 XXH64(this string data)
        {
            var algorithm = xxHashFactory.Instance.Create(new xxHashConfig() { HashSizeInBits = 64 });
            var hash = algorithm.ComputeHash(Encoding.UTF8.GetBytes(data));

            return BitConverter.ToInt64(hash.Hash, 0);
        }
    }
}