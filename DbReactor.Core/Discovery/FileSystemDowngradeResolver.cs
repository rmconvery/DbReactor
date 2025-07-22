using DbReactor.Core.Abstractions;
using DbReactor.Core.Configuration;
using DbReactor.Core.Models.Scripts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DbReactor.Core.Discovery
{
    /// <summary>
    /// Resolves downgrade scripts from the file system
    /// </summary>
    public class FileSystemDowngradeResolver : IDowngradeResolver
    {
        private readonly string _downgradeDirectory;
        private readonly string _fileExtension;
        private readonly DowngradeMatchingOptions _options;

        /// <summary>
        /// Initializes a new instance of the FileSystemDowngradeResolver
        /// </summary>
        /// <param name="downgradeDirectory">Directory containing downgrade scripts</param>
        /// <param name="fileExtension">File extension for downgrade scripts (default: .sql)</param>
        /// <param name="options">Options for matching upgrade scripts to downgrade scripts</param>
        public FileSystemDowngradeResolver(
            string downgradeDirectory, 
            string fileExtension = ".sql", 
            DowngradeMatchingOptions options = null)
        {
            if (string.IsNullOrEmpty(downgradeDirectory))
                throw new ArgumentException("Downgrade directory cannot be null or empty", nameof(downgradeDirectory));

            _downgradeDirectory = downgradeDirectory;
            _fileExtension = fileExtension ?? ".sql";
            _options = options ?? new DowngradeMatchingOptions
            {
                Mode = DowngradeMatchingMode.SameName,
                UpgradeSuffix = ".sql",
                DowngradeSuffix = ".sql"
            };
        }

        /// <summary>
        /// Finds the corresponding downgrade script for an upgrade script
        /// </summary>
        /// <param name="upgradeScript">The upgrade script to find a downgrade for</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The downgrade script, or null if none found</returns>
        public async Task<IScript> FindDowngradeForAsync(IScript upgradeScript, CancellationToken cancellationToken = default)
        {
            if (upgradeScript == null)
                throw new ArgumentNullException(nameof(upgradeScript));

            if (!Directory.Exists(_downgradeDirectory))
                return null; // Downgrade directory doesn't exist

            var downgradeFileName = DetermineDowngradeFileName(upgradeScript.Name);
            var downgradeFilePath = Path.Combine(_downgradeDirectory, downgradeFileName);

            if (!File.Exists(downgradeFilePath))
                return null; // Downgrade script doesn't exist

            try
            {
                var content = await Task.Run(() => File.ReadAllText(downgradeFilePath), cancellationToken);
                return new GenericScript(upgradeScript.Name, content);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load downgrade script from file: {downgradeFilePath}", ex);
            }
        }

        /// <summary>
        /// Gets all available downgrade scripts
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of downgrade scripts</returns>
        public async Task<IEnumerable<IScript>> GetDowngradeScriptsAsync(CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(_downgradeDirectory))
                return Enumerable.Empty<IScript>();

            var searchOption = SearchOption.TopDirectoryOnly;
            var pattern = "*" + _fileExtension;

            var scriptFiles = Directory.GetFiles(_downgradeDirectory, pattern, searchOption)
                .OrderBy(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase);

            var scripts = new List<IScript>();
            foreach (var filePath in scriptFiles)
            {
                try
                {
                    var fileName = Path.GetFileNameWithoutExtension(filePath);
                    var content = await Task.Run(() => File.ReadAllText(filePath), cancellationToken);
                    scripts.Add(new GenericScript(fileName, content));
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to load downgrade script from file: {filePath}", ex);
                }
            }

            return scripts;
        }

        /// <summary>
        /// Determines the downgrade file name based on the upgrade script name and matching options
        /// </summary>
        /// <param name="upgradeScriptName">Name of the upgrade script</param>
        /// <returns>Expected downgrade file name</returns>
        private string DetermineDowngradeFileName(string upgradeScriptName)
        {
            switch (_options.Mode)
            {
                case DowngradeMatchingMode.SameName:
                    return upgradeScriptName + _fileExtension;
                case DowngradeMatchingMode.Suffix:
                    return upgradeScriptName + _options.Pattern + _fileExtension;
                case DowngradeMatchingMode.Prefix:
                    return _options.Pattern + upgradeScriptName + _fileExtension;
                default:
                    return upgradeScriptName + _fileExtension;
            }
        }
    }
}