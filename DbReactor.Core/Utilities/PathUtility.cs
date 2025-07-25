using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DbReactor.Core.Utilities
{
    /// <summary>
    /// Provides utilities for path and namespace manipulation
    /// </summary>
    public static class PathUtility
    {
        /// <summary>
        /// Normalizes a path to a .NET namespace format (using dots as separators)
        /// </summary>
        /// <param name="path">Path to normalize</param>
        /// <returns>Normalized path with dots as separators</returns>
        public static string NormalizeToNamespace(string path)
        {
            if (string.IsNullOrEmpty(path)) 
                return path;
            
            // Replace all path separators with dots
            return path.Replace('\\', '.').Replace('/', '.');
        }

        /// <summary>
        /// Normalizes path separators to the current platform's format
        /// </summary>
        /// <param name="path">Path to normalize</param>
        /// <returns>Path with platform-appropriate separators</returns>
        public static string NormalizePathSeparators(string path)
        {
            if (string.IsNullOrEmpty(path)) 
                return path;
            
            // Replace all separators with the current platform's separator
            return path.Replace('\\', Path.DirectorySeparatorChar)
                      .Replace('/', Path.DirectorySeparatorChar);
        }

        /// <summary>
        /// Validates that a path is suitable for use as a namespace component
        /// </summary>
        /// <param name="path">Path to validate</param>
        /// <returns>True if the path is valid for namespace use</returns>
        public static bool IsValidNamespacePath(string path)
        {
            if (string.IsNullOrEmpty(path)) 
                return false;
            
            // Check for invalid characters for namespaces
            var invalidChars = new[] { '<', '>', ':', '"', '|', '?', '*', ' ', '\t', '\n', '\r' };
            
            if (path.Any(c => invalidChars.Contains(c)))
                return false;
            
            // Check that each part (separated by dots) is a valid identifier
            var parts = path.Split('.');
            return parts.All(IsValidIdentifier);
        }

        /// <summary>
        /// Validates that a string is a valid C# identifier
        /// </summary>
        /// <param name="identifier">String to validate</param>
        /// <returns>True if the string is a valid identifier</returns>
        public static bool IsValidIdentifier(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
                return false;
            
            // Must start with letter or underscore
            if (!char.IsLetter(identifier[0]) && identifier[0] != '_')
                return false;
            
            // Remaining characters must be letters, digits, or underscores
            return identifier.Skip(1).All(c => char.IsLetterOrDigit(c) || c == '_');
        }

        /// <summary>
        /// Combines namespace parts into a single namespace string
        /// </summary>
        /// <param name="parts">Namespace parts to combine</param>
        /// <returns>Combined namespace string</returns>
        public static string CombineNamespace(params string[] parts)
        {
            if (parts == null || parts.Length == 0)
                return string.Empty;
            
            var validParts = parts.Where(p => !string.IsNullOrEmpty(p))
                                 .Select(NormalizeToNamespace)
                                 .Where(p => !string.IsNullOrEmpty(p));
            
            return string.Join(".", validParts);
        }

        /// <summary>
        /// Extracts the file name without extension from a resource name
        /// </summary>
        /// <param name="resourceName">Full resource name</param>
        /// <returns>File name without extension</returns>
        public static string GetFileNameWithoutExtension(string resourceName)
        {
            if (string.IsNullOrEmpty(resourceName))
                return string.Empty;
            
            var parts = resourceName.Split('.');
            if (parts.Length < 2)
                return resourceName;
            
            // Return the second-to-last part (filename without extension)
            return parts[parts.Length - 2];
        }

        /// <summary>
        /// Extracts the file extension from a resource name
        /// </summary>
        /// <param name="resourceName">Full resource name</param>
        /// <returns>File extension including the dot, or empty string if none</returns>
        public static string GetExtension(string resourceName)
        {
            if (string.IsNullOrEmpty(resourceName))
                return string.Empty;
            
            var lastDotIndex = resourceName.LastIndexOf('.');
            if (lastDotIndex == -1 || lastDotIndex == resourceName.Length - 1)
                return string.Empty;
            
            return resourceName.Substring(lastDotIndex);
        }

        /// <summary>
        /// Sanitizes a string to be safe for use in file paths and namespaces
        /// </summary>
        /// <param name="input">String to sanitize</param>
        /// <returns>Sanitized string</returns>
        public static string SanitizeForPath(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;
            
            // Define platform-independent invalid characters for file names
            // This ensures consistent behavior across Windows, Linux, and macOS
            var invalidChars = new char[] 
            { 
                '<', '>', ':', '"', '|', '?', '*', 
                '\\', '/', '\0', '\u0001', '\u0002', '\u0003', '\u0004', '\u0005', 
                '\u0006', '\u0007', '\u0008', '\u0009', '\u000A', '\u000B', '\u000C', 
                '\u000D', '\u000E', '\u000F', '\u0010', '\u0011', '\u0012', '\u0013', 
                '\u0014', '\u0015', '\u0016', '\u0017', '\u0018', '\u0019', '\u001A', 
                '\u001B', '\u001C', '\u001D', '\u001E', '\u001F'
            };
            
            var sanitized = invalidChars.Aggregate(input, (current, c) => current.Replace(c, '_'));
            
            // Remove consecutive underscores
            sanitized = Regex.Replace(sanitized, "_+", "_");
            
            // Trim underscores from start and end
            return sanitized.Trim('_');
        }

        /// <summary>
        /// Splits a namespace path into its component parts
        /// </summary>
        /// <param name="namespacePath">Namespace path to split</param>
        /// <returns>Array of namespace parts</returns>
        public static string[] SplitNamespace(string namespacePath)
        {
            if (string.IsNullOrEmpty(namespacePath))
                return new string[0];
            
            return namespacePath.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Gets the parent namespace from a given namespace path
        /// </summary>
        /// <param name="namespacePath">Namespace path</param>
        /// <returns>Parent namespace, or empty string if no parent</returns>
        public static string GetParentNamespace(string namespacePath)
        {
            if (string.IsNullOrEmpty(namespacePath))
                return string.Empty;
            
            var lastDotIndex = namespacePath.LastIndexOf('.');
            if (lastDotIndex == -1)
                return string.Empty;
            
            return namespacePath.Substring(0, lastDotIndex);
        }

        /// <summary>
        /// Gets the last component of a namespace path
        /// </summary>
        /// <param name="namespacePath">Namespace path</param>
        /// <returns>Last component, or the entire path if no dots</returns>
        public static string GetNamespaceLeaf(string namespacePath)
        {
            if (string.IsNullOrEmpty(namespacePath))
                return string.Empty;
            
            var lastDotIndex = namespacePath.LastIndexOf('.');
            if (lastDotIndex == -1)
                return namespacePath;
            
            return namespacePath.Substring(lastDotIndex + 1);
        }
    }
}