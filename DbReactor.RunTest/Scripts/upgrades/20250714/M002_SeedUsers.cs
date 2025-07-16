using DbReactor.Core.Abstractions;
using DbReactor.Core.Execution;
using DbReactor.Core.Models.Contexts;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace DbReactor.RunTest.Scripts.upgrades._20250714
{
    /// <summary>
    /// Code script to add indexes to the Users table
    /// </summary>
    public class M002_SeedUsers : ICodeScript
    {
        public bool SupportsDowngrade => true;

        public string GetUpgradeScript(CodeScriptContext context)
        {
            // Example of using variables with the improved API
            string environment = context.Vars.GetString("Environment", "Development");
            string adminEmail = context.Vars.GetString("AdminEmail", "admin@example.com");
            
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

        public string GetDowngradeScript(CodeScriptContext context)
        {
            // Use parameterized queries to prevent SQL injection
            return @"
                -- Removing seeded users from Users table using safe SQL
                DELETE FROM Users WHERE Username IN ('alice', 'bob', 'carol');
                ";
        }
    }
}