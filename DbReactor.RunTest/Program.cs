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
            .UseVariables(new Dictionary<string, string>
            {
                {"Environment", "Development"},
                {"AdminEmail", "admin@example.com"},
                {"TenantId", "test-tenant-001"}
            });

        DbReactorEngine engine = new DbReactorEngine(config);

        try
        {
            DbReactorPreviewResult previewResult = await engine.RunPreviewAsync();
            DbReactorResult result = await engine.RunAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception during operation: " + ex.Message);
        }
    }
}
