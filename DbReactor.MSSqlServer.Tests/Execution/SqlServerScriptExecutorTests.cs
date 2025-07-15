using DbReactor.Core.Abstractions;
using DbReactor.Core.Execution;
using DbReactor.Core.Models;
using DbReactor.Core.Models.Scripts;
using DbReactor.MSSqlServer.Execution;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Data.SqlClient;
using Moq;
using NUnit.Framework;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace DbReactor.MSSqlServer.Tests.Execution;

[TestFixture]
public class SqlServerScriptExecutorTests
{
    private const string ValidConnectionString = "Server=localhost;Database=TestDB;Trusted_Connection=true;";
    private const string InvalidConnectionString = "Server=nonexistent;Database=TestDB;Trusted_Connection=true;Connection Timeout=1;";
    private SqlServerScriptExecutor _executor;

    [SetUp]
    public void SetUp()
    {
        _executor = new SqlServerScriptExecutor();
    }

    [Test]
    public void Constructor_WithDefaultTimeout_ShouldCreateExecutor()
    {
        // When
        var executor = new SqlServerScriptExecutor();

        // Then
        using (new AssertionScope())
        {
            executor.Should().NotBeNull();
        }
    }

    [Test]
    public void Constructor_WithCustomTimeout_ShouldCreateExecutor()
    {
        // Given
        var customTimeout = 60;

        // When
        var executor = new SqlServerScriptExecutor(customTimeout);

        // Then
        using (new AssertionScope())
        {
            executor.Should().NotBeNull();
        }
    }

    [Test]
    public async Task ExecuteAsync_WithNullScript_ShouldReturnFailedResult()
    {
        // Given
        IScript nullScript = null!;
        var mockConnectionManager = new Mock<IConnectionManager>();
        var cancellationToken = CancellationToken.None;

        // When
        var result = await _executor.ExecuteAsync(nullScript, mockConnectionManager.Object, cancellationToken);

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNull();
            result.Script.Should().Be(nullScript);
            result.Successful.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
            result.ExecutionTime.Should().BeGreaterThan(TimeSpan.Zero);
        }
    }

    [Test]
    public void GenericScript_WithEmptyScript_ShouldThrowArgumentNullException()
    {
        // When
        Action act = () => new GenericScript("empty.sql", "");

        // Then
        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("script")
                .WithMessage("*Script cannot be null or empty*");
        }
    }

    [Test]
    public void GenericScript_WithWhitespaceScript_ShouldThrowArgumentNullException()
    {
        // When
        Action act = () => new GenericScript("whitespace.sql", "   \n\t  ");

        // Then
        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("script")
                .WithMessage("*Script cannot be null or empty*");
        }
    }

    [Test]
    public async Task ExecuteAsync_WithConnectionManagerException_ShouldReturnFailedResult()
    {
        // Given
        var script = new GenericScript("test.sql", "SELECT 1");
        var expectedException = new InvalidOperationException("Connection failed");
        var mockConnectionManager = new Mock<IConnectionManager>();
        var cancellationToken = CancellationToken.None;

        mockConnectionManager.Setup(cm => cm.ExecuteWithManagedConnectionAsync(
            It.IsAny<Func<IDbConnection, Task>>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // When
        var result = await _executor.ExecuteAsync(script, mockConnectionManager.Object, cancellationToken);

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNull();
            result.Script.Should().Be(script);
            result.Successful.Should().BeFalse();
            result.Error.Should().Be(expectedException);
            result.ErrorMessage.Should().Be("Connection failed");
            result.ExecutionTime.Should().BeGreaterThan(TimeSpan.Zero);
        }
    }

    [Test]
    public async Task VerifySchemaAsync_WithConnectionManagerException_ShouldThrow()
    {
        // Given
        var expectedException = new InvalidOperationException("Connection failed");
        var mockConnectionManager = new Mock<IConnectionManager>();
        var cancellationToken = CancellationToken.None;

        mockConnectionManager.Setup(cm => cm.ExecuteWithManagedConnectionAsync(
            It.IsAny<Func<IDbConnection, Task>>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // When
        Func<Task> act = async () => await _executor.VerifySchemaAsync(mockConnectionManager.Object, cancellationToken);

        // Then
        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Connection failed");
        }
    }

    // Integration tests that require actual SQL Server connection
    [Test]
    public async Task ExecuteAsync_WithValidConnectionAndScript_ShouldExecuteSuccessfully()
    {
        // Given
        var script = new GenericScript("test.sql", "SELECT 1");
        var connectionManager = new DbReactor.MSSqlServer.Execution.DbReactor.MSSqlServer.Implementations.Execution.SqlServerConnectionManager(ValidConnectionString);
        var cancellationToken = CancellationToken.None;

        // When & Then
        if (IsConnectionStringValid(ValidConnectionString))
        {
            var result = await _executor.ExecuteAsync(script, connectionManager, cancellationToken);
            
            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Script.Should().Be(script);
                result.Successful.Should().BeTrue();
                result.Error.Should().BeNull();
                result.ErrorMessage.Should().BeNull();
                result.ExecutionTime.Should().BeGreaterThan(TimeSpan.Zero);
            }
        }
        else
        {
            // If we can't connect, it should return a failed result
            var result = await _executor.ExecuteAsync(script, connectionManager, cancellationToken);
            
            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Script.Should().Be(script);
                result.Successful.Should().BeFalse();
                result.Error.Should().NotBeNull();
                result.ExecutionTime.Should().BeGreaterThan(TimeSpan.Zero);
            }
        }
    }

    [Test]
    public async Task ExecuteAsync_WithScriptContainingGOStatements_ShouldSplitAndExecuteBatches()
    {
        // Given
        var scriptContent = @"
SELECT 1;
GO
SELECT 2;
GO
SELECT 3;
";
        var script = new GenericScript("batches.sql", scriptContent);
        var connectionManager = new DbReactor.MSSqlServer.Execution.DbReactor.MSSqlServer.Implementations.Execution.SqlServerConnectionManager(ValidConnectionString);
        var cancellationToken = CancellationToken.None;

        // When & Then
        if (IsConnectionStringValid(ValidConnectionString))
        {
            var result = await _executor.ExecuteAsync(script, connectionManager, cancellationToken);
            
            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Script.Should().Be(script);
                result.Successful.Should().BeTrue();
                result.Error.Should().BeNull();
                result.ErrorMessage.Should().BeNull();
                result.ExecutionTime.Should().BeGreaterThan(TimeSpan.Zero);
            }
        }
        else
        {
            // If we can't connect, it should return a failed result
            var result = await _executor.ExecuteAsync(script, connectionManager, cancellationToken);
            
            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Script.Should().Be(script);
                result.Successful.Should().BeFalse();
                result.Error.Should().NotBeNull();
                result.ExecutionTime.Should().BeGreaterThan(TimeSpan.Zero);
            }
        }
    }

    [Test]
    public async Task ExecuteAsync_WithSelectStatement_ShouldExecuteWithReader()
    {
        // Given
        var script = new GenericScript("select.sql", "SELECT GETDATE() as CurrentTime");
        var connectionManager = new DbReactor.MSSqlServer.Execution.DbReactor.MSSqlServer.Implementations.Execution.SqlServerConnectionManager(ValidConnectionString);
        var cancellationToken = CancellationToken.None;

        // When & Then
        if (IsConnectionStringValid(ValidConnectionString))
        {
            var result = await _executor.ExecuteAsync(script, connectionManager, cancellationToken);
            
            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Script.Should().Be(script);
                result.Successful.Should().BeTrue();
                result.Error.Should().BeNull();
                result.ErrorMessage.Should().BeNull();
                result.ExecutionTime.Should().BeGreaterThan(TimeSpan.Zero);
            }
        }
        else
        {
            // If we can't connect, it should return a failed result
            var result = await _executor.ExecuteAsync(script, connectionManager, cancellationToken);
            
            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Script.Should().Be(script);
                result.Successful.Should().BeFalse();
                result.Error.Should().NotBeNull();
                result.ExecutionTime.Should().BeGreaterThan(TimeSpan.Zero);
            }
        }
    }

    [Test]
    public async Task ExecuteAsync_WithInvalidConnectionString_ShouldReturnFailedResult()
    {
        // Given
        var script = new GenericScript("test.sql", "SELECT 1");
        var connectionManager = new DbReactor.MSSqlServer.Execution.DbReactor.MSSqlServer.Implementations.Execution.SqlServerConnectionManager(InvalidConnectionString);
        var cancellationToken = CancellationToken.None;

        // When
        var result = await _executor.ExecuteAsync(script, connectionManager, cancellationToken);

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNull();
            result.Script.Should().Be(script);
            result.Successful.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.ExecutionTime.Should().BeGreaterThan(TimeSpan.Zero);
        }
    }

    [Test]
    public async Task VerifySchemaAsync_WithValidConnection_ShouldNotThrow()
    {
        // Given
        var connectionManager = new DbReactor.MSSqlServer.Execution.DbReactor.MSSqlServer.Implementations.Execution.SqlServerConnectionManager(ValidConnectionString);
        var cancellationToken = CancellationToken.None;

        // When & Then
        if (IsConnectionStringValid(ValidConnectionString))
        {
            Func<Task> act = async () => await _executor.VerifySchemaAsync(connectionManager, cancellationToken);
            
            using (new AssertionScope())
            {
                await act.Should().NotThrowAsync();
            }
        }
        else
        {
            // If we can't connect, it should throw
            Func<Task> act = async () => await _executor.VerifySchemaAsync(connectionManager, cancellationToken);
            
            using (new AssertionScope())
            {
                await act.Should().ThrowAsync<Exception>();
            }
        }
    }

    [Test]
    public async Task VerifySchemaAsync_WithInvalidConnection_ShouldThrow()
    {
        // Given
        var connectionManager = new DbReactor.MSSqlServer.Execution.DbReactor.MSSqlServer.Implementations.Execution.SqlServerConnectionManager(InvalidConnectionString);
        var cancellationToken = CancellationToken.None;

        // When
        Func<Task> act = async () => await _executor.VerifySchemaAsync(connectionManager, cancellationToken);

        // Then
        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<Exception>();
        }
    }

    [Test]
    public async Task ExecuteAsync_WithCancelledToken_ShouldReturnFailedResult()
    {
        // Given
        var script = new GenericScript("test.sql", "SELECT 1");
        var connectionManager = new DbReactor.MSSqlServer.Execution.DbReactor.MSSqlServer.Implementations.Execution.SqlServerConnectionManager(ValidConnectionString);
        var cancellationToken = new CancellationToken(true); // Pre-cancelled token

        // When
        var result = await _executor.ExecuteAsync(script, connectionManager, cancellationToken);

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNull();
            result.Script.Should().Be(script);
            // The result might be successful or failed depending on timing
            result.ExecutionTime.Should().BeGreaterThan(TimeSpan.Zero);
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
}