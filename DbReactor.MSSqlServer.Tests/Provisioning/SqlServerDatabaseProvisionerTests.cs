using DbReactor.Core.Logging;
using DbReactor.Core.Provisioning;
using DbReactor.MSSqlServer.Provisioning;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Data.SqlClient;
using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace DbReactor.MSSqlServer.Tests.Provisioning;

[TestFixture]
public class SqlServerDatabaseProvisionerTests
{
    private const string ValidConnectionString = "Server=localhost;Database=TestDB;Trusted_Connection=true;";
    private const string ValidConnectionStringWithoutDatabase = "Server=localhost;Trusted_Connection=true;";
    private const string InvalidConnectionString = "Server=nonexistent;Database=TestDB;Trusted_Connection=true;Connection Timeout=1;";
    private Mock<ILogProvider> _mockLogProvider;
    private SqlServerDatabaseProvisioner _provisioner;

    [SetUp]
    public void SetUp()
    {
        _mockLogProvider = new Mock<ILogProvider>();
        _provisioner = new SqlServerDatabaseProvisioner(ValidConnectionString, _mockLogProvider.Object);
    }

    [Test]
    public void Constructor_WithValidConnectionString_ShouldCreateProvisioner()
    {
        // When
        var provisioner = new SqlServerDatabaseProvisioner(ValidConnectionString);

        // Then
        using (new AssertionScope())
        {
            provisioner.Should().NotBeNull();
            provisioner.Should().BeAssignableTo<IDatabaseProvisioner>();
        }
    }

    [Test]
    public void Constructor_WithValidConnectionStringAndLogProvider_ShouldCreateProvisioner()
    {
        // Given
        var logProvider = _mockLogProvider.Object;

        // When
        var provisioner = new SqlServerDatabaseProvisioner(ValidConnectionString, logProvider);

        // Then
        using (new AssertionScope())
        {
            provisioner.Should().NotBeNull();
            provisioner.Should().BeAssignableTo<IDatabaseProvisioner>();
        }
    }

    [Test]
    public void Constructor_WithNullConnectionString_ShouldThrowArgumentNullException()
    {
        // When
        Action act = () => new SqlServerDatabaseProvisioner(null);

        // Then
        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("connectionString");
        }
    }

    [Test]
    public void Constructor_WithNullLogProvider_ShouldUseNullLogProvider()
    {
        // When
        var provisioner = new SqlServerDatabaseProvisioner(ValidConnectionString, null);

        // Then
        using (new AssertionScope())
        {
            provisioner.Should().NotBeNull();
        }
    }

    [Test]
    public async Task DatabaseExistsAsync_WithValidConnectionString_ShouldReturnCorrectResult()
    {
        // Given
        var provisioner = new SqlServerDatabaseProvisioner(ValidConnectionString);

        // When & Then
        if (IsConnectionStringValid(ValidConnectionString))
        {
            // If we can connect to the database, it should return true
            bool result = await provisioner.DatabaseExistsAsync();
            result.Should().BeTrue();
        }
        else
        {
            // If we can't connect, it should throw an exception
            Func<Task> act = async () => await provisioner.DatabaseExistsAsync();
            await act.Should().ThrowAsync<SqlException>();
        }
    }

    [Test]
    public async Task DatabaseExistsAsync_WithConnectionStringWithoutDatabase_ShouldThrowInvalidOperationException()
    {
        // Given
        var provisioner = new SqlServerDatabaseProvisioner(ValidConnectionStringWithoutDatabase);

        // When
        Func<Task> act = async () => await provisioner.DatabaseExistsAsync();

        // Then
        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Connection string must specify a database name*");
        }
    }

    [Test]
    public async Task DatabaseExistsAsync_WithInvalidConnectionString_ShouldThrowSqlException()
    {
        // Given
        var provisioner = new SqlServerDatabaseProvisioner(InvalidConnectionString);

        // When
        Func<Task> act = async () => await provisioner.DatabaseExistsAsync();

        // Then
        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<SqlException>();
        }
    }

    [Test]
    public async Task DatabaseExistsAsync_WithSqlException_ShouldLogErrorAndThrow()
    {
        // Given
        var provisioner = new SqlServerDatabaseProvisioner(InvalidConnectionString, _mockLogProvider.Object);

        // When
        Func<Task> act = async () => await provisioner.DatabaseExistsAsync();

        // Then
        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<SqlException>();
            _mockLogProvider.Verify(lp => lp.WriteError(
                It.Is<string>(msg => msg.Contains("Error checking if database exists"))), Times.Once);
        }
    }

    [Test]
    public async Task CreateDatabaseAsync_WithValidConnectionString_ShouldCreateDatabaseSuccessfully()
    {
        // Given
        var testDbName = $"TestDB_{Guid.NewGuid():N}";
        var connectionString = $"Server=localhost;Database={testDbName};Trusted_Connection=true;";
        var provisioner = new SqlServerDatabaseProvisioner(connectionString, _mockLogProvider.Object);

        // When & Then
        if (IsConnectionStringValid(connectionString))
        {
            try
            {
                await provisioner.CreateDatabaseAsync();
                
                // Verify database was created
                (await provisioner.DatabaseExistsAsync()).Should().BeTrue();
                
                // Verify logging
                _mockLogProvider.Verify(lp => lp.WriteInformation(
                    It.Is<string>(msg => msg.Contains($"Creating database: {testDbName}"))), Times.Once);
                _mockLogProvider.Verify(lp => lp.WriteInformation(
                    It.Is<string>(msg => msg.Contains($"Database created successfully: {testDbName}"))), Times.Once);
            }
            finally
            {
                // Clean up - drop the test database
                try
                {
                    var masterConnectionString = GetMasterConnectionString(connectionString);
                    using var connection = new SqlConnection(masterConnectionString);
                    await connection.OpenAsync();
                    using var cmd = new SqlCommand($"DROP DATABASE IF EXISTS [{testDbName}]", connection);
                    await cmd.ExecuteNonQueryAsync();
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
        else
        {
            // If we can't connect, it should throw an exception
            Func<Task> act = async () => await provisioner.CreateDatabaseAsync();
            await act.Should().ThrowAsync<SqlException>();
        }
    }

    [Test]
    public async Task CreateDatabaseAsync_WithCustomTemplate_ShouldCreateDatabaseWithTemplate()
    {
        // Given
        var testDbName = $"TestDB_{Guid.NewGuid():N}";
        var connectionString = $"Server=localhost;Database={testDbName};Trusted_Connection=true;";
        var provisioner = new SqlServerDatabaseProvisioner(connectionString, _mockLogProvider.Object);
        var template = "CREATE DATABASE [{0}] COLLATE SQL_Latin1_General_CP1_CI_AS";

        // When & Then
        if (IsConnectionStringValid(connectionString))
        {
            try
            {
                await provisioner.CreateDatabaseAsync(template);
                
                // Verify database was created
                (await provisioner.DatabaseExistsAsync()).Should().BeTrue();
                
                // Verify logging
                _mockLogProvider.Verify(lp => lp.WriteInformation(
                    It.Is<string>(msg => msg.Contains($"Creating database: {testDbName}"))), Times.Once);
                _mockLogProvider.Verify(lp => lp.WriteInformation(
                    It.Is<string>(msg => msg.Contains($"Database created successfully: {testDbName}"))), Times.Once);
            }
            finally
            {
                // Clean up - drop the test database
                try
                {
                    var masterConnectionString = GetMasterConnectionString(connectionString);
                    using var connection = new SqlConnection(masterConnectionString);
                    await connection.OpenAsync();
                    using var cmd = new SqlCommand($"DROP DATABASE IF EXISTS [{testDbName}]", connection);
                    await cmd.ExecuteNonQueryAsync();
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
        else
        {
            // If we can't connect, it should throw an exception
            Func<Task> act = async () => await provisioner.CreateDatabaseAsync(template);
            await act.Should().ThrowAsync<SqlException>();
        }
    }

    [Test]
    public async Task CreateDatabaseAsync_WithConnectionStringWithoutDatabase_ShouldThrowInvalidOperationException()
    {
        // Given
        var provisioner = new SqlServerDatabaseProvisioner(ValidConnectionStringWithoutDatabase);

        // When
        Func<Task> act = async () => await provisioner.CreateDatabaseAsync();

        // Then
        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Connection string must specify a database name*");
        }
    }

    [Test]
    public async Task CreateDatabaseAsync_WithInvalidConnectionString_ShouldThrowSqlException()
    {
        // Given
        var provisioner = new SqlServerDatabaseProvisioner(InvalidConnectionString);

        // When
        Func<Task> act = async () => await provisioner.CreateDatabaseAsync();

        // Then
        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<SqlException>();
        }
    }

    [Test]
    public async Task CreateDatabaseAsync_WithSqlException_ShouldLogErrorAndThrow()
    {
        // Given
        var provisioner = new SqlServerDatabaseProvisioner(InvalidConnectionString, _mockLogProvider.Object);

        // When
        Func<Task> act = async () => await provisioner.CreateDatabaseAsync();

        // Then
        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<SqlException>();
            _mockLogProvider.Verify(lp => lp.WriteError(
                It.Is<string>(msg => msg.Contains("Error creating database"))), Times.Once);
        }
    }

    [Test]
    public async Task EnsureDatabaseExistsAsync_WithExistingDatabase_ShouldNotCreateDatabase()
    {
        // Given
        var provisioner = new SqlServerDatabaseProvisioner(ValidConnectionString, _mockLogProvider.Object);

        // When & Then
        if (IsConnectionStringValid(ValidConnectionString))
        {
            await provisioner.EnsureDatabaseExistsAsync();
            
            // Verify logging shows database already exists
            _mockLogProvider.Verify(lp => lp.WriteInformation(
                It.Is<string>(msg => msg.Contains("Database already exists"))), Times.Once);
        }
        else
        {
            // If we can't connect, it should throw an exception
            Func<Task> act = async () => await provisioner.EnsureDatabaseExistsAsync();
            await act.Should().ThrowAsync<SqlException>();
        }
    }

    [Test]
    public async Task EnsureDatabaseExistsAsync_WithNonExistingDatabase_ShouldCreateDatabase()
    {
        // Given
        var testDbName = $"TestDB_{Guid.NewGuid():N}";
        var connectionString = $"Server=localhost;Database={testDbName};Trusted_Connection=true;";
        var provisioner = new SqlServerDatabaseProvisioner(connectionString, _mockLogProvider.Object);

        // When & Then
        if (IsConnectionStringValid(connectionString))
        {
            try
            {
                await provisioner.EnsureDatabaseExistsAsync();
                
                // Verify database was created
                (await provisioner.DatabaseExistsAsync()).Should().BeTrue();
                
                // Verify logging shows database was created
                _mockLogProvider.Verify(lp => lp.WriteInformation(
                    It.Is<string>(msg => msg.Contains($"Creating database: {testDbName}"))), Times.Once);
                _mockLogProvider.Verify(lp => lp.WriteInformation(
                    It.Is<string>(msg => msg.Contains($"Database created successfully: {testDbName}"))), Times.Once);
            }
            finally
            {
                // Clean up - drop the test database
                try
                {
                    var masterConnectionString = GetMasterConnectionString(connectionString);
                    using var connection = new SqlConnection(masterConnectionString);
                    await connection.OpenAsync();
                    using var cmd = new SqlCommand($"DROP DATABASE IF EXISTS [{testDbName}]", connection);
                    await cmd.ExecuteNonQueryAsync();
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
        else
        {
            // If we can't connect, it should throw an exception
            Func<Task> act = async () => await provisioner.EnsureDatabaseExistsAsync();
            await act.Should().ThrowAsync<SqlException>();
        }
    }

    [Test]
    public async Task EnsureDatabaseExistsAsync_WithCustomTemplate_ShouldCreateDatabaseWithTemplate()
    {
        // Given
        var testDbName = $"TestDB_{Guid.NewGuid():N}";
        var connectionString = $"Server=localhost;Database={testDbName};Trusted_Connection=true;";
        var provisioner = new SqlServerDatabaseProvisioner(connectionString, _mockLogProvider.Object);
        var template = "CREATE DATABASE [{0}] COLLATE SQL_Latin1_General_CP1_CI_AS";

        // When & Then
        if (IsConnectionStringValid(connectionString))
        {
            try
            {
                await provisioner.EnsureDatabaseExistsAsync(template);
                
                // Verify database was created
                (await provisioner.DatabaseExistsAsync()).Should().BeTrue();
                
                // Verify logging shows database was created
                _mockLogProvider.Verify(lp => lp.WriteInformation(
                    It.Is<string>(msg => msg.Contains($"Creating database: {testDbName}"))), Times.Once);
                _mockLogProvider.Verify(lp => lp.WriteInformation(
                    It.Is<string>(msg => msg.Contains($"Database created successfully: {testDbName}"))), Times.Once);
            }
            finally
            {
                // Clean up - drop the test database
                try
                {
                    var masterConnectionString = GetMasterConnectionString(connectionString);
                    using var connection = new SqlConnection(masterConnectionString);
                    await connection.OpenAsync();
                    using var cmd = new SqlCommand($"DROP DATABASE IF EXISTS [{testDbName}]", connection);
                    await cmd.ExecuteNonQueryAsync();
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
        else
        {
            // If we can't connect, it should throw an exception
            Func<Task> act = async () => await provisioner.EnsureDatabaseExistsAsync(template);
            await act.Should().ThrowAsync<SqlException>();
        }
    }

    [Test]
    public async Task EnsureDatabaseExistsAsync_WithConnectionStringWithoutDatabase_ShouldThrowInvalidOperationException()
    {
        // Given
        var provisioner = new SqlServerDatabaseProvisioner(ValidConnectionStringWithoutDatabase);

        // When
        Func<Task> act = async () => await provisioner.EnsureDatabaseExistsAsync();

        // Then
        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Connection string must specify a database name*");
        }
    }

    [Test]
    public async Task EnsureDatabaseExistsAsync_WithInvalidConnectionString_ShouldThrowSqlException()
    {
        // Given
        var provisioner = new SqlServerDatabaseProvisioner(InvalidConnectionString);

        // When
        Func<Task> act = async () => await provisioner.EnsureDatabaseExistsAsync();

        // Then
        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<SqlException>();
        }
    }

    [Test]
    public async Task AllMethods_WithNullLogProvider_ShouldNotThrow()
    {
        // Given
        var provisioner = new SqlServerDatabaseProvisioner(ValidConnectionString, null);

        // When & Then
        if (IsConnectionStringValid(ValidConnectionString))
        {
            // These operations should not throw even with null log provider
            Func<Task> act1 = async () => await provisioner.DatabaseExistsAsync();
            Func<Task> act2 = async () => await provisioner.EnsureDatabaseExistsAsync();
            
            await act1.Should().NotThrowAsync();
            await act2.Should().NotThrowAsync();
        }
        else
        {
            // Even with connection failures, null log provider should not cause issues
            Func<Task> act1 = async () => await provisioner.DatabaseExistsAsync();
            Func<Task> act2 = async () => await provisioner.EnsureDatabaseExistsAsync();
            
            await act1.Should().ThrowAsync<SqlException>();
            await act2.Should().ThrowAsync<SqlException>();
        }
    }

    [Test]
    public async Task DatabaseExistsAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Given
        var provisioner = new SqlServerDatabaseProvisioner(ValidConnectionString);
        var cancellationToken = new System.Threading.CancellationToken(true); // Pre-cancelled

        // When
        Func<Task> act = async () => await provisioner.DatabaseExistsAsync(cancellationToken);

        // Then
        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<System.Threading.Tasks.TaskCanceledException>();
        }
    }

    [Test]
    public async Task CreateDatabaseAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Given
        var testDbName = $"TestDB_{Guid.NewGuid():N}";
        var connectionString = $"Server=localhost;Database={testDbName};Trusted_Connection=true;";
        var provisioner = new SqlServerDatabaseProvisioner(connectionString);
        var cancellationToken = new System.Threading.CancellationToken(true); // Pre-cancelled

        // When
        Func<Task> act = async () => await provisioner.CreateDatabaseAsync(cancellationToken: cancellationToken);

        // Then
        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<System.Threading.Tasks.TaskCanceledException>();
        }
    }

    [Test]
    public async Task EnsureDatabaseExistsAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Given
        var testDbName = $"TestDB_{Guid.NewGuid():N}";
        var connectionString = $"Server=localhost;Database={testDbName};Trusted_Connection=true;";
        var provisioner = new SqlServerDatabaseProvisioner(connectionString);
        var cancellationToken = new System.Threading.CancellationToken(true); // Pre-cancelled

        // When
        Func<Task> act = async () => await provisioner.EnsureDatabaseExistsAsync(cancellationToken: cancellationToken);

        // Then
        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<System.Threading.Tasks.TaskCanceledException>();
        }
    }

    /// <summary>
    /// Helper method to determine if a connection string is valid for the current environment
    /// This is used to conditionally run tests that require a real SQL Server instance
    /// </summary>
    private static bool IsConnectionStringValid(string connectionString)
    {
        try
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Helper method to get master connection string for cleanup operations
    /// </summary>
    private static string GetMasterConnectionString(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        builder.InitialCatalog = "master";
        return builder.ConnectionString;
    }
}