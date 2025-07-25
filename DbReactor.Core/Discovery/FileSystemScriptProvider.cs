using DbReactor.Core.Abstractions;
using DbReactor.Core.Models.Scripts;
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
    /// Discovers SQL scripts from the file system
    /// </summary>
    public class FileSystemScriptProvider : IScriptProvider
    {
        private readonly string _directoryPath;
        private readonly string _fileExtension;
        private readonly bool _recursive;

        /// <summary>
        /// Initializes a new instance of the FileSystemScriptProvider
        /// </summary>
        /// <param name="directoryPath">Directory path to search for scripts</param>
        /// <param name="fileExtension">File extension to search for (default: .sql)</param>
        /// <param name="recursive">Whether to search subdirectories recursively (default: false)</param>
        public FileSystemScriptProvider(string directoryPath, string fileExtension = ".sql", bool recursive = false)
        {
            if (string.IsNullOrEmpty(directoryPath))
                throw new ArgumentException("Directory path cannot be null or empty", nameof(directoryPath));

            _directoryPath = directoryPath;
            _fileExtension = fileExtension ?? ".sql";
            _recursive = recursive;
        }

        /// <summary>
        /// Gets upgrade scripts from the file system directory
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of upgrade scripts found in the directory</returns>
        public async Task<IEnumerable<IScript>> GetScriptsAsync(CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(_directoryPath))
            {
                return Enumerable.Empty<IScript>(); // Directory doesn't exist, return empty collection
            }

            var searchOption = _recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var pattern = "*" + _fileExtension;

            var scriptFiles = Directory.GetFiles(_directoryPath, pattern, searchOption)
                .OrderBy(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase);

            var scripts = new List<IScript>();
            foreach (var filePath in scriptFiles)
            {
                var script = await CreateScriptFromFileAsync(filePath, cancellationToken);
                scripts.Add(script);
            }

            return scripts;
        }

        /// <summary>
        /// Creates a script instance from a file path
        /// </summary>
        /// <param name="filePath">Path to the script file</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Script instance</returns>
        private async Task<IScript> CreateScriptFromFileAsync(string filePath, CancellationToken cancellationToken)
        {
            try
            {
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                var content = await Task.Run(() => File.ReadAllText(filePath), cancellationToken);

                return new GenericScript(fileName, content);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load script from file: {filePath}", ex);
            }
        }
    }
}