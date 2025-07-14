using DbReactor.Core.Abstractions;
using System.Collections.Generic;

namespace DbReactor.Core.Discovery
{
    /// <summary>
    /// Builds migrations by combining upgrade scripts with their corresponding downgrade scripts
    /// </summary>
    public interface IMigrationBuilder
    {
        /// <summary>
        /// Builds a collection of migrations from available scripts
        /// </summary>
        /// <returns>Collection of migrations</returns>
        IEnumerable<IMigration> BuildMigrations();
    }
}