using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace AppController.Utils
{
    public static class Crypto
    {
        public static byte[] GetRandomBytes(int Length)
        {
            var ret = new byte[Length];
            using (var rngCsp = new RNGCryptoServiceProvider())
                rngCsp.GetBytes(ret);
            return ret;
        }
        public static byte[] SHA512Hash(byte[] data)
        {
            using (var sha512 = new SHA512Managed())
                return sha512.ComputeHash(data);
        }
    }
}
