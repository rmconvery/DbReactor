using DbReactor.Core.Abstractions;
using DbReactor.Core.Models;
using DbReactor.Core.Models.Scripts;
using DbReactor.Core.Seeding.Strategies;
using DbReactor.Core.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DbReactor.Core.Discovery
{
    /// <summary>
    /// Discovers seed scripts from the file system with strategy resolved at discovery time.
    /// Passes the full relative path to strategy resolvers so folder-based resolution works correctly.
    /// </summary>
    public class FileSystemSeedProvider : ISeedScriptProvider
    {
        private readonly string _directoryPath;
        private readonly string _fileExtension;
        private readonly IEnumerable<ISeedStrategyResolver> _strategyResolvers;
        private readonly ISeedExecutionStrategy _fallbackStrategy;

        /// <summary>
        /// Initializes a new instance of FileSystemSeedProvider
        /// </summary>
        /// <param name="directoryPath">Root directory to search for seed scripts</param>
        /// <param name="strategyResolvers">Strategy resolvers to apply using full relative path</param>
        /// <param name="fileExtension">File extension filter (default: .sql)</param>
        /// <param name="fallbackStrategy">Fallback strategy when no resolver matches</param>
        public FileSystemSeedProvider(
            string directoryPath,
            IEnumerable<ISeedStrategyResolver> strategyResolvers,
            string fileExtension = ".sql",
            ISeedExecutionStrategy fallbackStrategy = null)
        {
            if (string.IsNullOrEmpty(directoryPath))
                throw new ArgumentException("Directory path cannot be null or empty", nameof(directoryPath));

            _directoryPath = directoryPath;
            _strategyResolvers = strategyResolvers ?? Enumerable.Empty<ISeedStrategyResolver>();
            _fileExtension = fileExtension ?? ".sql";
            _fallbackStrategy = fallbackStrategy ?? new RunOnceSeedStrategy();
        }

        /// <summary>
        /// Gets seeds from the file system with strategy resolved using the full relative path
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of seeds with strategy resolved</returns>
        public async Task<IEnumerable<ISeed>> GetSeedsAsync(CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(_directoryPath))
            {
                return Enumerable.Empty<ISeed>();
            }

            var pattern = "*" + _fileExtension;
            // Sort by full path to maintain subdirectory ordering (differs from FileSystemScriptProvider which sorts by filename only)
            var scriptFiles = Directory.GetFiles(_directoryPath, pattern, SearchOption.AllDirectories)
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase);

            var seeds = new List<ISeed>();
            foreach (var filePath in scriptFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var seed = await CreateSeedFromFileAsync(filePath, cancellationToken);
                seeds.Add(seed);
            }

            return seeds;
        }

        private async Task<ISeed> CreateSeedFromFileAsync(string filePath, CancellationToken cancellationToken)
        {
            var content = await Task.Run(() => File.ReadAllText(filePath), cancellationToken);
            var fileName = Path.GetFileNameWithoutExtension(filePath);

            // Use short filename as the script/seed name (journal-compatible)
            var script = new GenericScript(fileName, content);

            // Use full relative path for strategy resolution so folder-based resolvers work
            var relativePath = GetRelativePath(_directoryPath, filePath);
            var strategy = ResolveStrategy(script, relativePath);

            return new Seed(fileName, script, strategy, script.Hash);
        }

        private ISeedExecutionStrategy ResolveStrategy(IScript script, string relativePath)
        {
            foreach (var resolver in _strategyResolvers)
            {
                var strategy = resolver.ResolveStrategy(script, relativePath);
                if (strategy != null)
                    return strategy;
            }

            return _fallbackStrategy;
        }

        // Uses Uri.MakeRelativeUri because Path.GetRelativePath is not available on netstandard2.0
        private static string GetRelativePath(string basePath, string fullPath)
        {
            var baseUri = new Uri(EnsureTrailingSlash(Path.GetFullPath(basePath)));
            var fullUri = new Uri(Path.GetFullPath(fullPath));
            return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fullUri).ToString())
                .Replace('/', Path.DirectorySeparatorChar);
        }

        private static string EnsureTrailingSlash(string path)
        {
            return path.EndsWith(Path.DirectorySeparatorChar.ToString())
                ? path
                : path + Path.DirectorySeparatorChar;
        }
    }
}
