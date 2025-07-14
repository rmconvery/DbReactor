using DbReactor.Core.Abstractions;
using DbReactor.Core.Discovery;
using DbReactor.Core.Models.Scripts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DbReactor.Core.Implementations.Discovery
{

    /// <summary>
    /// Discovers scripts embedded as resources in the assembly.
    /// </summary>
    public class EmbeddedScriptProvider : IScriptProvider
    {
        private readonly Assembly _assembly;
        private readonly string _resourceNamespace;
        private readonly string _scriptSuffix;

        public EmbeddedScriptProvider(Assembly assembly, string resourceNamespace, string scriptSuffix = ".sql")
        {
            _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
            _resourceNamespace = resourceNamespace ?? throw new ArgumentNullException(nameof(resourceNamespace));
            _scriptSuffix = scriptSuffix ?? ".sql";
        }

        public EmbeddedScriptProvider(Assembly assembly, string scriptSuffix = ".sql")
        {
            _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
            _resourceNamespace = DiscoverScriptNamespace(assembly);
            _scriptSuffix = scriptSuffix ?? ".sql";
        }

        public EmbeddedScriptProvider(Assembly assembly, string baseNamespace, string folderName, string scriptSuffix = ".sql")
        {
            _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
            _resourceNamespace = string.IsNullOrEmpty(folderName) ? baseNamespace : $"{baseNamespace}.{folderName}";
            _scriptSuffix = scriptSuffix ?? ".sql";
        }


        public IEnumerable<IScript> GetScripts()
        {
            return _assembly.GetManifestResourceNames()
                .Where(r => r.StartsWith(_resourceNamespace, StringComparison.Ordinal)
                    && r.EndsWith(_scriptSuffix, StringComparison.OrdinalIgnoreCase))
                .OrderBy(r => r)
                .Select(resourceName => new EmbeddedScript(_assembly, resourceName));
        }

        private string DiscoverScriptNamespace(Assembly assembly)
        {
            string[] resourceNames = assembly.GetManifestResourceNames();

            // Common script folder conventions to try
            string[] conventions = new[] { "Scripts", "Migrations", "Sql", "Database", "Db" };

            // First try common conventions
            foreach (string convention in conventions)
            {
                IEnumerable<string> candidateNamespaces = resourceNames
                    .Where(r => r.Contains("." + convention + ".") && r.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
                    .Select(r => ExtractNamespaceForConvention(r, convention))
                    .Where(ns => !string.IsNullOrEmpty(ns))
                    .Distinct();

                string firstCandidate = candidateNamespaces.FirstOrDefault();
                if (firstCandidate != null)
                {
                    return firstCandidate;
                }
            }

            // If no conventions found, find any namespace containing .sql files
            IEnumerable<string> sqlResources = resourceNames
                .Where(r => r.EndsWith(".sql", StringComparison.OrdinalIgnoreCase));

            if (!sqlResources.Any())
            {
                throw new InvalidOperationException("No .sql files found as embedded resources in the assembly. Ensure your SQL scripts are marked as 'Embedded Resource' in their build properties.");
            }

            // Extract the most common namespace prefix
            string namespacePrefix = FindCommonNamespacePrefix(sqlResources);

            if (string.IsNullOrEmpty(namespacePrefix))
            {
                throw new InvalidOperationException("Unable to automatically discover script namespace. Please specify the resource namespace explicitly.");
            }

            return namespacePrefix;
        }

        private string ExtractNamespaceForConvention(string resourceName, string convention)
        {
            string[] parts = resourceName.Split('.');
            int conventionIndex = Array.FindIndex(parts, p => p.Equals(convention, StringComparison.OrdinalIgnoreCase));

            if (conventionIndex > 0)
            {
                return string.Join(".", parts.Take(conventionIndex + 1));
            }

            return null;
        }

        private string FindCommonNamespacePrefix(IEnumerable<string> resourceNames)
        {
            if (!resourceNames.Any()) return null;

            // Group by potential namespace prefixes (everything before the last two parts)
            string mostCommonPrefix = resourceNames
                .Select(r =>
                {
                    string[] parts = r.Split('.');
                    if (parts.Length >= 3)
                    {
                        return string.Join(".", parts.Take(parts.Length - 2));
                    }
                    return null;
                })
                .Where(prefix => !string.IsNullOrEmpty(prefix))
                .GroupBy(prefix => prefix)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key;

            return mostCommonPrefix;
        }


    }
}
