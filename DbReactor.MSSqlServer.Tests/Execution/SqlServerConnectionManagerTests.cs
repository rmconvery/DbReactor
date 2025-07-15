using DbReactor.Core.Execution;
using DbReactor.MSSqlServer.Execution.DbReactor.MSSqlServer.Implementations.Execution;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Data.SqlClient;
using NUnit.Framework;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace DbReactor.MSSqlServer.Tests.Execution;

[TestFixture]
public class SqlServerConnectionManagerTests
{
    private const string ValidConnectionString = "Server=localhost;Database=TestDB;Trusted_Connection=true;";
    private const string InvalidConnectionString = "Server=nonexistent;Database=TestDB;Trusted_Connection=true;Connection Timeout=1;";
    private SqlServerConnectionManager _connectionManager;

    [SetUp]
    public void SetUp()
    {
        _connectionManager = new SqlServerConnectionManager(ValidConnectionString);
    }

    [Test]
    public void Constructor_WithValidConnectionString_ShouldCreateManager()
    {
        // Given
        var connectionString = ValidConnectionString;

        // When
        var manager = new SqlServerConnectionManager(connectionString);

        // Then
        using (new AssertionScope())
        {
            manager.Should().NotBeNull();
            manager.Should().BeAssignableTo<IConnectionManager>();
        }
    }

    [Test]
    public void Constructor_WithNullConnectionString_ShouldThrowArgumentNullException()
    {
        // Given
        string nullConnectionString = null!;

        // When
        Action act = () => new SqlServerConnectionManager(nullConnectionString);

        // Then
        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("connectionString");
        }
    }

    [Test]
    public void Constructor_WithEmptyConnectionString_ShouldCreateManager()
    {
        // Given
        var emptyConnectionString = string.Empty;

        // When
        var manager = new SqlServerConnectionManager(emptyConnectionString);

        // Then
        using (new AssertionScope())
        {
            manager.Should().NotBeNull();
        }
    }

    [Test]
    public async Task CreateConnectionAsync_WithValidConnectionString_ShouldReturnOpenConnection()
    {
        // Given
        var cancellationToken = CancellationToken.None;
        var manager = new SqlServerConnectionManager(ValidConnectionString);

        // When & Then
        // Note: This test would require a real SQL Server instance
        // For unit testing, we'll just verify the method doesn't throw with valid syntax
        if (IsConnectionStringValid(ValidConnectionString))
        {
            using var connection = await manager.CreateConnectionAsync(cancellationToken);
            
            using (new AssertionScope())
            {
                connection.Should().NotBeNull();
                connection.Should().BeAssignableTo<SqlConnection>();
                connection.State.Should().Be(ConnectionState.Open);
            }
        }
        else
        {
            // When connection fails, it should throw
            Func<Task> act = async () => await manager.CreateConnectionAsync(cancellationToken);
            await act.Should().ThrowAsync<SqlException>();
        }
    }

    [Test]
    public async Task CreateConnectionAsync_WithInvalidConnectionString_ShouldThrowSqlException()
    {
        // Given
        var manager = new SqlServerConnectionManager(InvalidConnectionString);
        var cancellationToken = CancellationToken.None;

        // When
        Func<Task> act = async () => await manager.CreateConnectionAsync(cancellationToken);

        // Then
        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<SqlException>();
        }
    }

    [Test]
    public async Task CreateConnectionAsync_WithCancelledToken_ShouldThrowOperationCanceledException()
    {
        // Given
        var manager = new SqlServerConnectionManager(ValidConnectionString);
        var cancellationToken = new CancellationToken(true); // Pre-cancelled

        // When
        Func<Task> act = async () => await manager.CreateConnectionAsync(cancellationToken);

        // Then
        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<OperationCanceledException>();
        }
    }

    [Test]
    public async Task ExecuteWithManagedConnectionAsync_WithValidOperation_ShouldExecuteSuccessfully()
    {
        // Given
        var manager = new SqlServerConnectionManager(ValidConnectionString);
        var cancellationToken = CancellationToken.None;
        var operationExecuted = false;

        // When
        if (IsConnectionStringValid(ValidConnectionString))
        {
            await manager.ExecuteWithManagedConnectionAsync(async connection =>
            {
                operationExecuted = true;
                connection.Should().NotBeNull();
                connection.Should().BeAssignableTo<SqlConnection>();
                connection.State.Should().Be(ConnectionState.Open);
                await Task.CompletedTask;
            }, cancellationToken);

            // Then
            using (new AssertionScope())
            {
                operationExecuted.Should().BeTrue();
            }
        }
        else
        {
            // When connection fails, it should throw
            Func<Task> act = async () => await manager.ExecuteWithManagedConnectionAsync(async connection =>
            {
                operationExecuted = true;
                await Task.CompletedTask;
            }, cancellationToken);

            await act.Should().ThrowAsync<SqlException>();
            operationExecuted.Should().BeFalse();
        }
    }

    [Test]
    public async Task ExecuteWithManagedConnectionAsync_WithInvalidConnectionString_ShouldThrowSqlException()
    {
        // Given
        var manager = new SqlServerConnectionManager(InvalidConnectionString);
        var cancellationToken = CancellationToken.None;
        var operationExecuted = false;

        // When
        Func<Task> act = async () => await manager.ExecuteWithManagedConnectionAsync(async connection =>
        {
            operationExecuted = true;
            await Task.CompletedTask;
        }, cancellationToken);

        // Then
        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<SqlException>();
            operationExecuted.Should().BeFalse();
        }
    }

    [Test]
    public async Task ExecuteWithManagedConnectionAsync_WithOperationException_ShouldPropagateException()
    {
        // Given
        var manager = new SqlServerConnectionManager(ValidConnectionString);
        var cancellationToken = CancellationToken.None;
        var expectedException = new InvalidOperationException("Test exception");

        // When
        Func<Task> act = async () => await manager.ExecuteWithManagedConnectionAsync(async connection =>
        {
            await Task.CompletedTask;
            throw expectedException;
        }, cancellationToken);

        // Then
        using (new AssertionScope())
        {
            if (IsConnectionStringValid(ValidConnectionString))
            {
                await act.Should().ThrowAsync<InvalidOperationException>()
                    .WithMessage("Test exception");
            }
            else
            {
                // If connection fails, SqlException will be thrown instead
                await act.Should().ThrowAsync<SqlException>();
            }
        }
    }

    [Test]
    public async Task ExecuteWithManagedConnectionAsync_Generic_WithValidOperation_ShouldReturnResult()
    {
        // Given
        var manager = new SqlServerConnectionManager(ValidConnectionString);
        var cancellationToken = CancellationToken.None;
        var expectedResult = 42;

        // When & Then
        if (IsConnectionStringValid(ValidConnectionString))
        {
            var result = await manager.ExecuteWithManagedConnectionAsync(async connection =>
            {
                connection.Should().NotBeNull();
                connection.Should().BeAssignableTo<SqlConnection>();
                connection.State.Should().Be(ConnectionState.Open);
                await Task.CompletedTask;
                return expectedResult;
            }, cancellationToken);

            using (new AssertionScope())
            {
                result.Should().Be(expectedResult);
            }
        }
        else
        {
            // When connection fails, it should throw
            Func<Task> act = async () => await manager.ExecuteWithManagedConnectionAsync(async connection =>
            {
                await Task.CompletedTask;
                return expectedResult;
            }, cancellationToken);

            await act.Should().ThrowAsync<SqlException>();
        }
    }

    [Test]
    public async Task ExecuteWithManagedConnectionAsync_Generic_WithInvalidConnectionString_ShouldThrowSqlException()
    {
        // Given
        var manager = new SqlServerConnectionManager(InvalidConnectionString);
        var cancellationToken = CancellationToken.None;

        // When
        Func<Task> act = async () => await manager.ExecuteWithManagedConnectionAsync(async connection =>
        {
            await Task.CompletedTask;
            return "result";
        }, cancellationToken);

        // Then
        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<SqlException>();
        }
    }

    [Test]
    public async Task ExecuteWithManagedConnectionAsync_Generic_WithOperationException_ShouldPropagateException()
    {
        // Given
        var manager = new SqlServerConnectionManager(ValidConnectionString);
        var cancellationToken = CancellationToken.None;
        var expectedException = new InvalidOperationException("Test exception");

        // When
        Func<Task> act = async () => await manager.ExecuteWithManagedConnectionAsync<string>(async connection =>
        {
            await Task.CompletedTask;
            throw expectedException;
        }, cancellationToken);

        // Then
        using (new AssertionScope())
        {
            if (IsConnectionStringValid(ValidConnectionString))
            {
                await act.Should().ThrowAsync<InvalidOperationException>()
                    .WithMessage("Test exception");
            }
            else
            {
                // If connection fails, SqlException will be thrown instead
                await act.Should().ThrowAsync<SqlException>();
            }
        }
    }

    [Test]
    public async Task ExecuteWithManagedConnectionAsync_Generic_WithCancelledToken_ShouldThrowOperationCanceledException()
    {
        // Given
        var manager = new SqlServerConnectionManager(ValidConnectionString);
        var cancellationToken = new CancellationToken(true); // Pre-cancelled

        // When
        Func<Task> act = async () => await manager.ExecuteWithManagedConnectionAsync(async connection =>
        {
            await Task.CompletedTask;
            return "result";
        }, cancellationToken);

        // Then
        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<OperationCanceledException>();
        }
    }

    [Test]
    public async Task ExecuteWithManagedConnectionAsync_ShouldDisposeConnectionAfterOperation()
    {
        // Given
        var manager = new SqlServerConnectionManager(ValidConnectionString);
        var cancellationToken = CancellationToken.None;
        IDbConnection capturedConnection = null!;

        // When
        if (IsConnectionStringValid(ValidConnectionString))
        {
            await manager.ExecuteWithManagedConnectionAsync(async connection =>
            {
                capturedConnection = connection;
                await Task.CompletedTask;
            }, cancellationToken);

            // Then
            using (new AssertionScope())
            {
                capturedConnection.Should().NotBeNull();
                capturedConnection.State.Should().Be(ConnectionState.Closed);
            }
        }
        else
        {
            // When connection fails, it should throw
            Func<Task> act = async () => await manager.ExecuteWithManagedConnectionAsync(async connection =>
            {
                capturedConnection = connection;
                await Task.CompletedTask;
            }, cancellationToken);

            await act.Should().ThrowAsync<SqlException>();
            capturedConnection.Should().BeNull();
        }
    }

    [Test]
    public async Task ExecuteWithManagedConnectionAsync_Generic_ShouldDisposeConnectionAfterOperation()
    {
        // Given
        var manager = new SqlServerConnectionManager(ValidConnectionString);
        var cancellationToken = CancellationToken.None;
        IDbConnection capturedConnection = null!;

        // When & Then
        if (IsConnectionStringValid(ValidConnectionString))
        {
            var result = await manager.ExecuteWithManagedConnectionAsync(async connection =>
            {
                capturedConnection = connection;
                await Task.CompletedTask;
                return "test result";
            }, cancellationToken);

            using (new AssertionScope())
            {
                result.Should().Be("test result");
                capturedConnection.Should().NotBeNull();
                capturedConnection.State.Should().Be(ConnectionState.Closed);
            }
        }
        else
        {
            // When connection fails, it should throw
            Func<Task> act = async () => await manager.ExecuteWithManagedConnectionAsync(async connection =>
            {
                capturedConnection = connection;
                await Task.CompletedTask;
                return "test result";
            }, cancellationToken);

            await act.Should().ThrowAsync<SqlException>();
            capturedConnection.Should().BeNull();
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