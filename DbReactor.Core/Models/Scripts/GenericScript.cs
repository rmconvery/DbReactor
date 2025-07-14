using DbReactor.Core.Abstractions;
using DbReactor.Core.Utilities;
using System;

namespace DbReactor.Core.Models.Scripts
{
    /// <summary>
    /// Represents a generic script that does not come from a provider.
    /// </summary>
    public class GenericScript : IScript
    {
        public string Name { get; }
        public string Script { get; }
        public string Hash { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericScript"/> class.
        /// </summary>
        /// <param name="name">The name of the script.</param>
        /// <param name="script">The script content.</param>
        public GenericScript(string name, string script)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name), "Script name cannot be null or empty.");

            if (string.IsNullOrWhiteSpace(script))
                throw new ArgumentNullException(nameof(script), "Script cannot be null or empty.");

            Name = name;
            Script = script;
            Hash = HashUtility.GenerateHash(script);
        }

    }
}
