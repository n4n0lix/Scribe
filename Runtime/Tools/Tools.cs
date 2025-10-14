using System.Security.Cryptography;
using System.Text;
using UnityEngine;
namespace Scribe.Tools
{
    public static class Tools
    {

        public static string SHA256HexCanonicalJSON(string json)
        {
            if (json == null) json = string.Empty;

            // #1 Clean up
            json = json.Replace("\r\n", "\n");
            json = json.Replace("\r", "\n");
            json = json.Replace("\t", "");

            // SHA-256 (lowercase hex)
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(json);
            var hash = sha.ComputeHash(bytes);

            var sb = new StringBuilder(hash.Length * 2);
            foreach (var b in hash) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }

}