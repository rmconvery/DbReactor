using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DbReactor.Core.Utilities
{
    /// <summary>
    /// Provides utilities for analyzing and discovering resources in assemblies
    /// </summary>
    public static class AssemblyResourceUtility
    {
        /// <summary>
        /// Discovers the most common base namespace for embedded resources in the assembly
        /// </summary>
        /// <param name="assembly">Assembly to analyze</param>
        /// <param name="scriptExtension">File extension to filter resources (default: .sql)</param>
        /// <param name="knownFolders">Known folder names to help identify namespace structure</param>
        /// <returns>The most common base namespace, or assembly name if none found</returns>
        /// <exception cref="ArgumentNullException">Thrown when assembly is null</exception>
        public static string DiscoverBaseNamespace(Assembly assembly, string scriptExtension = ".sql", string[] knownFolders = null)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            
            knownFolders = knownFolders ?? new[] { "upgrades", "downgrades", "scripts" };
            
            var resources = GetResourcesWithExtension(assembly, scriptExtension);
            
            if (!resources.Any()) 
                return assembly.GetName().Name ?? "Unknown";
            
            var commonPrefixes = ExtractCommonPrefixes(resources, knownFolders);
            
            return commonPrefixes.OrderByDescending(kvp => kvp.Value)
                .Select(kvp => kvp.Key)
                .FirstOrDefault() ?? assembly.GetName().Name ?? "Unknown";
        }

        /// <summary>
        /// Gets all manifest resource names from the assembly that end with the specified extension
        /// </summary>
        /// <param name="assembly">Assembly to analyze</param>
        /// <param name="extension">File extension to filter by</param>
        /// <returns>Array of resource names with the specified extension</returns>
        /// <exception cref="ArgumentNullException">Thrown when assembly is null</exception>
        public static string[] GetResourcesWithExtension(Assembly assembly, string extension)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            
            return assembly.GetManifestResourceNames()
                .Where(r => !string.IsNullOrEmpty(extension) && r.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        /// <summary>
        /// Extracts common namespace prefixes from resource names, considering known folder structures
        /// </summary>
        /// <param name="resourceNames">Array of resource names to analyze</param>
        /// <param name="knownFolders">Known folder names that indicate namespace boundaries</param>
        /// <returns>Dictionary of namespace prefixes and their occurrence counts</returns>
        public static Dictionary<string, int> ExtractCommonPrefixes(string[] resourceNames, string[] knownFolders)
        {
            if (resourceNames == null) throw new ArgumentNullException(nameof(resourceNames));
            knownFolders = knownFolders ?? new string[0];
            
            var prefixCounts = new Dictionary<string, int>();
            
            foreach (string resourceName in resourceNames)
            {
                var prefix = ExtractNamespacePrefix(resourceName, knownFolders);
                if (!string.IsNullOrEmpty(prefix))
                {
                    prefixCounts[prefix] = prefixCounts.TryGetValue(prefix, out int count) ? count + 1 : 1;
                }
            }
            
            return prefixCounts;
        }

        /// <summary>
        /// Extracts the namespace prefix from a single resource name
        /// </summary>
        /// <param name="resourceName">Resource name to analyze</param>
        /// <param name="knownFolders">Known folder names that indicate namespace boundaries</param>
        /// <returns>Namespace prefix, or null if none found</returns>
        public static string ExtractNamespacePrefix(string resourceName, string[] knownFolders)
        {
            if (string.IsNullOrEmpty(resourceName)) return null;
            knownFolders = knownFolders ?? new string[0];
            
            var parts = resourceName.Split('.');
            
            // Look for known folder indicators, prioritizing more specific folders
            // Find the LAST known folder in the path, not the first
            var folderIndex = FindLastKnownFolderIndex(parts, knownFolders);
            
            if (folderIndex > 0)
            {
                // Found a known folder, take everything before it
                return string.Join(".", parts.Take(folderIndex));
            }
            
            // Fallback: assume the prefix is everything except the last two parts (filename.extension)
            if (parts.Length >= 3)
            {
                return string.Join(".", parts.Take(parts.Length - 2));
            }
            
            return null;
        }

        /// <summary>
        /// Finds the index of the first known folder name in the resource path parts
        /// </summary>
        /// <param name="parts">Parts of the resource path</param>
        /// <param name="knownFolders">Known folder names to search for</param>
        /// <returns>Index of the first known folder, or -1 if none found</returns>
        private static int FindKnownFolderIndex(string[] parts, string[] knownFolders)
        {
            for (int i = 0; i < parts.Length; i++)
            {
                if (knownFolders.Any(folder => parts[i].Equals(folder, StringComparison.OrdinalIgnoreCase)))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Finds the index of the last known folder name in the resource path parts
        /// </summary>
        /// <param name="parts">Parts of the resource path</param>
        /// <param name="knownFolders">Known folder names to search for</param>
        /// <returns>Index of the last known folder, or -1 if none found</returns>
        private static int FindLastKnownFolderIndex(string[] parts, string[] knownFolders)
        {
            for (int i = parts.Length - 1; i >= 0; i--)
            {
                if (knownFolders.Any(folder => parts[i].Equals(folder, StringComparison.OrdinalIgnoreCase)))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Validates that the assembly contains resources with the specified extension
        /// </summary>
        /// <param name="assembly">Assembly to validate</param>
        /// <param name="extension">Required file extension</param>
        /// <returns>True if the assembly contains resources with the extension</returns>
        /// <exception cref="ArgumentNullException">Thrown when assembly is null</exception>
        public static bool HasResourcesWithExtension(Assembly assembly, string extension)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            
            return GetResourcesWithExtension(assembly, extension).Any();
        }

        /// <summary>
        /// Gets all unique namespace prefixes from the assembly's resources
        /// </summary>
        /// <param name="assembly">Assembly to analyze</param>
        /// <param name="extension">File extension to filter by</param>
        /// <returns>Array of unique namespace prefixes</returns>
        /// <exception cref="ArgumentNullException">Thrown when assembly is null</exception>
        public static string[] GetUniqueNamespacePrefixes(Assembly assembly, string extension = ".sql")
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            
            var resources = GetResourcesWithExtension(assembly, extension);
            var knownFolders = new[] { "upgrades", "downgrades", "scripts" };
            
            return resources
                .Select(r => ExtractNamespacePrefix(r, knownFolders))
                .Where(prefix => !string.IsNullOrEmpty(prefix))
                .Distinct()
                .OrderBy(prefix => prefix)
                .ToArray();
        }
    }
}