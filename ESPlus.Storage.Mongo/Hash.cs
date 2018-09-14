using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace ESPlus.Storage.Mongo
{
    public static class Hash
    {
        public static string Sha256(this string data)
        {
            var message = Encoding.ASCII.GetBytes(data);
            SHA256Managed hashString = new SHA256Managed();
            string hex = "";

            var hashValue = hashString.ComputeHash(message);
            foreach (byte x in hashValue)
            {
                hex += string.Format("{0:x2}", x);
            }
            return hex;
        }

        public static string MongoHash(this string data)
        {
            return Sha256(data).Replace("-", "").Substring(0, 24);
        }        
    }
}