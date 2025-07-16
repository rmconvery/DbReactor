using DbReactor.Core.Configuration;
using DbReactor.Core.Engine;
using DbReactor.Core.Extensions;
using DbReactor.Core.Models;
using DbReactor.MSSqlServer.Extensions;

class Program
{
    static void Main(string[] args)
    {
        string connectionString = "Server=localhost;Database=DbReactorTest;Trusted_Connection=True;TrustServerCertificate=true;";


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
            DbReactorResult result = engine.Run();

            if (result.Successful)
            {
                Console.WriteLine("Database upgrade successful!");
            }
            else
            {
                Console.WriteLine("Database upgrade failed:");
                Console.WriteLine(result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception during upgrade: " + ex.Message);
        }
    }
}
