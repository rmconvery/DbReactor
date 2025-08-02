using DbReactor.Core.Configuration;
using DbReactor.Core.Engine;
using DbReactor.Core.Extensions;
using DbReactor.Core.Models;
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
            .EnableSqlServerSeeding(typeof(Program).Assembly)
            .UseNamingConventionSeedStrategies()
            .UseVariables(new Dictionary<string, string>
            {
                {"Environment", "Development"},
                {"AdminEmail", "admin@example.com"},
                {"TenantId", "test-tenant-001"},
                {"CompanyDomain", "example.com"},
                {"ApplicationName", "DbReactor Demo"}
            });

        DbReactorEngine engine = new DbReactorEngine(config);

        try
        {
            Console.WriteLine("=== MIGRATION PREVIEW ===");
            DbReactorPreviewResult previewResult = await engine.RunPreviewAsync();

            Console.WriteLine("=== SEED PREVIEW ===");
            DbReactorSeedPreviewResult seedPreview = await engine.PreviewSeedsAsync();
            Console.WriteLine($"Seeds Preview: {seedPreview.Summary}");

            foreach (SeedPreviewResult? seedResult in seedPreview.SeedResults)
            {
                Console.WriteLine($"  - {seedResult.SeedName} ({seedResult.Strategy}): {seedResult.ExecutionReason}");
            }

            Console.WriteLine("=== RUNNING MIGRATIONS AND SEEDS TOGETHER ===");
            DbReactorResult result = await engine.RunAsync();

            if (result.Successful)
            {
                int migrationCount = result.Scripts.Count(s => s.Script.Name.Contains("Migration") || s.Script.Name.Contains("M0"));
                int seedCount = result.Scripts.Count(s => s.Script.Name.Contains("Seed") || s.Script.Name.Contains("S0"));
                Console.WriteLine($"✅ All operations completed successfully!");
                Console.WriteLine($"   Migrations executed: {migrationCount}");
                Console.WriteLine($"   Seeds executed: {seedCount}");
                Console.WriteLine($"   Total scripts: {result.Scripts.Count}");
            }
            else
            {
                Console.WriteLine($"❌ Operation failed: {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception during operation: " + ex.Message);
        }
    }
}
