using DbReactor.Core.Configuration;
using DbReactor.Core.Discovery;
using DbReactor.Core.Engine;
using DbReactor.Core.Extensions;
using DbReactor.Core.Models;
using DbReactor.Core.Seeding.Resolvers;
using DbReactor.Core.Seeding.Strategies;
using DbReactor.MSSqlServer.Extensions;

class Program
{
    static async Task Main(string[] args)
    {
        string connectionString = "Server=localhost;Database=DbReactorTest;Trusted_Connection=True;TrustServerCertificate=true;MultipleActiveResultSets=true;";


        DbReactorConfiguration config = new DbReactorConfiguration()
            .UseSqlServer(connectionString, commandTimeout: TimeSpan.FromSeconds(60))
            .UseConsoleLogging()
            .CreateDatabaseIfNotExists()
            .UseStandardFolderStructure(typeof(Program).Assembly)
            .UseCodeScripts(typeof(Program).Assembly)
            .UseVariables(new Dictionary<string, string>
            {
                {"Environment", "Development"},
                {"AdminEmail", "admin@example.com"},
                {"TenantId", "test-tenant-001"},
                {"CompanyDomain", "example.com"},
                {"ApplicationName", "DbReactor Demo"}
            });

        // Configure seeding
        config.EnableSeeding = true;
        config.SeedJournal = new DbReactor.MSSqlServer.Journaling.SqlServerSeedJournal(config.ConnectionManager);

        // Add seed script providers - use the 3-parameter constructor to be explicit
        EmbeddedScriptProvider seedProvider = new EmbeddedScriptProvider(typeof(Program).Assembly, "DbReactor.RunTest.Seeds", ".sql");
        config.SeedScriptProviders.Add(seedProvider);

        // Add strategy resolvers (order matters - first match wins)
        config.SeedStrategyResolvers.Add(new FolderStructureSeedStrategyResolver());
        //config.SeedStrategyResolvers.Add(new NamingConventionSeedStrategyResolver());

        // Set fallback strategy for seeds without specific strategy indicators
        config.FallbackSeedStrategy = new RunOnceSeedStrategy();

        DbReactorEngine engine = new DbReactorEngine(config);

        try
        {
            Console.WriteLine("=== MIGRATION PREVIEW ===");
            DbReactorPreviewResult previewResult = await engine.RunPreviewAsync();

            Console.WriteLine("=== RUNNING MIGRATIONS ===");
            DbReactorResult result = await engine.RunAsync();

            if (result.Successful)
            {
                Console.WriteLine("=== SEED PREVIEW ===");
                DbReactorSeedPreviewResult seedPreview = await engine.PreviewSeedsAsync();
                Console.WriteLine($"Seeds Preview: {seedPreview.Summary}");

                foreach (SeedPreviewResult? seedResult in seedPreview.SeedResults)
                {
                    Console.WriteLine($"  - {seedResult.SeedName} ({seedResult.Strategy}): {seedResult.ExecutionReason}");
                }

                Console.WriteLine("=== EXECUTING SEEDS ===");
                DbReactorResult seedResults = await engine.ExecuteSeedsAsync();

                if (seedResults.Successful)
                {
                    Console.WriteLine($"Seeding completed successfully! Executed {seedResults.Scripts.Count} seeds.");
                }
                else
                {
                    Console.WriteLine($"Seeding failed: {seedResults.ErrorMessage}");
                }
            }
            else
            {
                Console.WriteLine($"Migration failed: {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception during operation: " + ex.Message);
        }
    }
}
