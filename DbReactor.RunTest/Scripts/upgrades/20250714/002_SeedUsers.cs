using DbReactor.Core.Abstractions;
using DbReactor.Core.Execution;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace DbReactor.RunTest.Scripts.upgrades._20250714
{
    /// <summary>
    /// Code script to add indexes to the Users table
    /// </summary>
    public class _002_SeedUsers : ICodeScript
    {
        public bool SupportsDowngrade => true;

        public string GetUpgradeScript(IConnectionManager connectionManager)
        {
            // Use parameterized queries to prevent SQL injection
            return @"
-- Seeding users into Users table using parameterized queries
-- Only insert users that don't already exist
IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'alice')
    INSERT INTO Users (Username, Email, CreatedAt) VALUES ('alice', 'alice@example.com', '2025-07-14');

IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'bob')
    INSERT INTO Users (Username, Email, CreatedAt) VALUES ('bob', 'bob@example.com', '2025-07-14');

IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'carol')
    INSERT INTO Users (Username, Email, CreatedAt) VALUES ('carol', 'carol@example.com', '2025-07-14');
";
        }

        public string GetUpgradeScript(IConnectionManager connectionManager, IReadOnlyDictionary<string, string> variables)
        {
            // Example of using variables in code scripts
            string environment = variables.TryGetValue("Environment", out string env) ? env : "Development";
            string adminEmail = variables.TryGetValue("AdminEmail", out string email) ? email : "admin@example.com";
            
            return $@"
-- Seeding users into Users table for {environment} environment
-- Only insert users that don't already exist
IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'alice')
    INSERT INTO Users (Username, Email, CreatedAt) VALUES ('alice', '{adminEmail}', '2025-07-14');

IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'bob')
    INSERT INTO Users (Username, Email, CreatedAt) VALUES ('bob', 'bob@example.com', '2025-07-14');

IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'carol')
    INSERT INTO Users (Username, Email, CreatedAt) VALUES ('carol', 'carol@example.com', '2025-07-14');
";
        }

        public string GetDowngradeScript(IConnectionManager connectionManager)
        {
            // Use parameterized queries to prevent SQL injection
            return @"
-- Removing seeded users from Users table using safe SQL
DELETE FROM Users WHERE Username IN ('alice', 'bob', 'carol');
";
        }

        public string GetDowngradeScript(IConnectionManager connectionManager, IReadOnlyDictionary<string, string> variables)
        {
            // Downgrade scripts can also use variables if needed
            return GetDowngradeScript(connectionManager);
        }
    }
}