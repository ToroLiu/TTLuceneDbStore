using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace TTLuceneDbStore.Helper
{
    internal static class SHAHelper
    {
        public static string ToSHA256(string str)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(str);
            return ToSHA256(bytes);
        }
        public static string ToSHA256(byte[] bytes)
        {
            using (var sha256 = SHA256.Create())
            {
                var shaBytes = sha256.ComputeHash(bytes);
                string shaStr = Convert.ToBase64String(shaBytes);
                return shaStr;
            }
        }

        public static string ToSHA1(string str)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(str);
            return ToSHA1(bytes);
        }
        public static string ToSHA1(byte[] bytes) {
            using (var sha = SHA1.Create())
            {
                var shaBytes = sha.ComputeHash(bytes);
                string shaStr = Convert.ToBase64String(shaBytes);
                return shaStr;
            }
        }
    }
}
