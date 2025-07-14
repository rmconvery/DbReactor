using System;
using System.Security.Cryptography;
using System.Text;

namespace DbReactor.Core.Utilities
{
    /// <summary>
    /// Provides standardized hash generation for all scripts in the DbReactor system
    /// </summary>
    public static class HashUtility
    {
        /// <summary>
        /// Generates a consistent SHA256 hash for the given content using hexadecimal encoding
        /// </summary>
        /// <param name="content">The content to hash</param>
        /// <returns>SHA256 hash as a lowercase hexadecimal string</returns>
        public static string GenerateHash(string content)
        {
            if (string.IsNullOrEmpty(content))
                throw new ArgumentException("Content cannot be null or empty", nameof(content));

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }

        /// <summary>
        /// Generates a hash for code scripts based on type name for deterministic identification
        /// </summary>
        /// <param name="typeName">The full type name of the code script</param>
        /// <returns>SHA256 hash as a lowercase hexadecimal string</returns>
        public static string GenerateCodeScriptHash(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                throw new ArgumentException("Type name cannot be null or empty", nameof(typeName));

            return GenerateHash(typeName);
        }
    }
}