using DbReactor.Core.Abstractions;
using DbReactor.Core.Configuration;
using DbReactor.Core.Discovery;
using DbReactor.Core.Models.Scripts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DbReactor.Core.Discovery
{
    /// <summary>
    /// Resolves downgrade scripts for embedded upgrade scripts
    /// </summary>
    public class EmbeddedDowngradeResolver : IDowngradeResolver
    {
        private readonly Assembly _assembly;
        private readonly string _downgradeNamespace;
        private readonly DowngradeMatchingOptions _matchingOptions;

        public EmbeddedDowngradeResolver(Assembly assembly, string downgradeNamespace, DowngradeMatchingOptions matchingOptions)
        {
            _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
            _downgradeNamespace = downgradeNamespace ?? throw new ArgumentNullException(nameof(downgradeNamespace));
            _matchingOptions = matchingOptions ?? new DowngradeMatchingOptions();
        }

        public EmbeddedDowngradeResolver(Assembly assembly, string baseNamespace, string folderName, DowngradeMatchingOptions matchingOptions)
        {
            _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
            _downgradeNamespace = string.IsNullOrEmpty(folderName) ? baseNamespace : $"{baseNamespace}.{folderName}";
            _matchingOptions = matchingOptions ?? new DowngradeMatchingOptions();
        }

        public IScript FindDowngradeFor(IScript upgradeScript)
        {
            if (upgradeScript == null)
                return null;

            switch (_matchingOptions.Mode)
            {
                case DowngradeMatchingMode.Suffix:
                    return FindDowngradeWithSuffix(upgradeScript);
                
                case DowngradeMatchingMode.Prefix:
                    return FindDowngradeWithPrefix(upgradeScript);
                
                case DowngradeMatchingMode.SameName:
                    return FindDowngradeWithSameName(upgradeScript);
                
                default:
                    return null;
            }
        }

        public IEnumerable<IScript> GetDowngradeScripts()
        {
            return _assembly.GetManifestResourceNames()
                .Where(r => r.StartsWith(_downgradeNamespace, StringComparison.Ordinal)
                    && r.EndsWith(_matchingOptions.DowngradeSuffix, StringComparison.OrdinalIgnoreCase))
                .OrderBy(r => r)
                .Select(resourceName => new EmbeddedScript(_assembly, resourceName));
        }

        private IScript FindDowngradeWithSuffix(IScript upgradeScript)
        {
            var baseName = GetBaseNameFromUpgrade(upgradeScript.Name);
            var expectedDowngradeName = baseName + _matchingOptions.Pattern + _matchingOptions.DowngradeSuffix;
            
            return GetDowngradeScripts()
                .FirstOrDefault(ds => GetFileNameFromResource(ds.Name).Equals(expectedDowngradeName, StringComparison.OrdinalIgnoreCase));
        }

        private IScript FindDowngradeWithPrefix(IScript upgradeScript)
        {
            var baseName = GetBaseNameFromUpgrade(upgradeScript.Name);
            var expectedDowngradeName = _matchingOptions.Pattern + baseName + _matchingOptions.DowngradeSuffix;
            
            return GetDowngradeScripts()
                .FirstOrDefault(ds => GetFileNameFromResource(ds.Name).Equals(expectedDowngradeName, StringComparison.OrdinalIgnoreCase));
        }

        private IScript FindDowngradeWithSameName(IScript upgradeScript)
        {
            var upgradeFileName = GetFileNameFromResource(upgradeScript.Name);
            
            return GetDowngradeScripts()
                .FirstOrDefault(ds => GetFileNameFromResource(ds.Name).Equals(upgradeFileName, StringComparison.OrdinalIgnoreCase));
        }


        private string GetBaseNameFromUpgrade(string upgradeName)
        {
            return upgradeName.Substring(0, upgradeName.Length - _matchingOptions.UpgradeSuffix.Length);
        }

        private string GetFileNameFromResource(string resourceName)
        {
            // Extract just the filename from the full resource name
            // e.g., "MyApp.Scripts.20250713.TestSql.sql" -> "TestSql.sql"
            var parts = resourceName.Split('.');
            if (parts.Length >= 2)
            {
                return parts[parts.Length - 2] + "." + parts[parts.Length - 1];
            }
            return resourceName;
        }

        private string DiscoverScriptNamespace(Assembly assembly)
        {
            var resourceNames = assembly.GetManifestResourceNames();
            
            // Common script folder conventions to try
            var conventions = new[] { "Scripts", "Migrations", "Sql", "Database", "Db" };
            
            // First try common conventions
            foreach (var convention in conventions)
            {
                var candidateNamespaces = resourceNames
                    .Where(r => r.Contains("." + convention + ".") && r.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
                    .Select(r => ExtractNamespaceForConvention(r, convention))
                    .Where(ns => !string.IsNullOrEmpty(ns))
                    .Distinct()
                    .ToList();
                
                if (candidateNamespaces.Any())
                {
                    return candidateNamespaces.First();
                }
            }
            
            // If no conventions found, find any namespace containing .sql files
            var sqlResources = resourceNames
                .Where(r => r.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
                .ToList();
            
            if (!sqlResources.Any())
            {
                throw new InvalidOperationException("No .sql files found as embedded resources in the assembly. Ensure your SQL scripts are marked as 'Embedded Resource' in their build properties.");
            }
            
            // Extract the most common namespace prefix
            var namespacePrefix = FindCommonNamespacePrefix(sqlResources);
            
            if (string.IsNullOrEmpty(namespacePrefix))
            {
                throw new InvalidOperationException("Unable to automatically discover script namespace. Please specify the resource namespace explicitly.");
            }
            
            return namespacePrefix;
        }
        
        private string ExtractNamespaceForConvention(string resourceName, string convention)
        {
            var parts = resourceName.Split('.');
            var conventionIndex = Array.FindIndex(parts, p => p.Equals(convention, StringComparison.OrdinalIgnoreCase));
            
            if (conventionIndex > 0)
            {
                return string.Join(".", parts.Take(conventionIndex + 1));
            }
            
            return null;
        }
        
        private string FindCommonNamespacePrefix(List<string> resourceNames)
        {
            if (!resourceNames.Any()) return null;
            
            // Group by potential namespace prefixes (everything before the last two parts)
            var prefixGroups = resourceNames
                .Select(r => {
                    var parts = r.Split('.');
                    if (parts.Length >= 3)
                    {
                        return string.Join(".", parts.Take(parts.Length - 2));
                    }
                    return null;
                })
                .Where(prefix => !string.IsNullOrEmpty(prefix))
                .GroupBy(prefix => prefix)
                .OrderByDescending(g => g.Count())
                .ToList();
            
            return prefixGroups.FirstOrDefault()?.Key;
        }
    }
}