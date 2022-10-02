using System;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text;

namespace Redis_Search
{
    public class Hasher
    {
        private readonly SHA1 shaHasher;

        public Hasher()
        {
            shaHasher = SHA1.Create();
        }

        public string Hash(object input)
        {
            using MemoryStream stream = new MemoryStream();

            JsonSerializer.Serialize(stream, input);

            stream.Seek(0, SeekOrigin.Begin);

            return Convert.ToBase64String(shaHasher.ComputeHash(stream));
        }

        public string Hash(string input)
        {
            return Convert.ToBase64String(shaHasher.ComputeHash(Encoding.UTF8.GetBytes(input)));
        }
    }
}

