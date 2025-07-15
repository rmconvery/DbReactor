using DbReactor.Core.Abstractions;
using DbReactor.Core.Execution;
using DbReactor.Core.Journaling;
using DbReactor.Core.Logging;
using DbReactor.Core.Models;
using DbReactor.Core.Models.Scripts;
using DbReactor.MSSqlServer.Journaling;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Data.SqlClient;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DbReactor.MSSqlServer.Tests.Journaling;

[TestFixture]
public class SqlServerScriptJournalTests
{
    private Mock<IConnectionManager> _mockConnectionManager;
    private Mock<ILogProvider> _mockLogProvider;
    private SqlServerScriptJournal _journal;

    [SetUp]
    public void SetUp()
    {
        _mockConnectionManager = new Mock<IConnectionManager>();
        _mockLogProvider = new Mock<ILogProvider>();
        
        _journal = new SqlServerScriptJournal(logProvider: _mockLogProvider.Object);
        _journal.SetConnectionManager(_mockConnectionManager.Object);
    }

    [Test]
    public void Constructor_WithDefaultParameters_ShouldCreateJournal()
    {
        // When
        var journal = new SqlServerScriptJournal();

        // Then
        using (new AssertionScope())
        {
            journal.Should().NotBeNull();
            journal.Should().BeAssignableTo<IMigrationJournal>();
        }
    }

    [Test]
    public void Constructor_WithCustomParameters_ShouldCreateJournal()
    {
        // Given
        var schemaName = "custom";
        var tableName = "CustomMigrationJournal";
        var logProvider = _mockLogProvider.Object;

        // When
        var journal = new SqlServerScriptJournal(schemaName, tableName, logProvider);

        // Then
        using (new AssertionScope())
        {
            journal.Should().NotBeNull();
            journal.Should().BeAssignableTo<IMigrationJournal>();
        }
    }

    [Test]
    public void Constructor_WithNullSchema_ShouldUseDefaultSchema()
    {
        // Given
        string nullSchema = null;

        // When
        var journal = new SqlServerScriptJournal(nullSchema);

        // Then
        using (new AssertionScope())
        {
            journal.Should().NotBeNull();
        }
    }

    [Test]
    public void Constructor_WithNullTableName_ShouldUseDefaultTableName()
    {
        // Given
        string nullTableName = null;

        // When
        var journal = new SqlServerScriptJournal(tableName: nullTableName);

        // Then
        using (new AssertionScope())
        {
            journal.Should().NotBeNull();
        }
    }

    [Test]
    public void Constructor_WithInvalidSchema_ShouldThrowArgumentException()
    {
        // Given
        var invalidSchema = "schema;DROP TABLE";

        // When
        Action act = () => new SqlServerScriptJournal(invalidSchema);

        // Then
        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Invalid SQL identifier*");
        }
    }

    [Test]
    public void Constructor_WithInvalidTableName_ShouldThrowArgumentException()
    {
        // Given
        var invalidTableName = "table]DROP TABLE";

        // When
        Action act = () => new SqlServerScriptJournal(tableName: invalidTableName);

        // Then
        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Invalid SQL identifier*");
        }
    }

    [Test]
    public void SetConnectionManager_WithValidConnectionManager_ShouldSetManager()
    {
        // Given
        var journal = new SqlServerScriptJournal();
        var connectionManager = _mockConnectionManager.Object;

        // When
        journal.SetConnectionManager(connectionManager);

        // Then
        using (new AssertionScope())
        {
            // No exception should be thrown
            // The connection manager should be set internally
        }
    }

    [Test]
    public void SetConnectionManager_WithNullConnectionManager_ShouldThrowArgumentNullException()
    {
        // Given
        var journal = new SqlServerScriptJournal();
        IConnectionManager nullConnectionManager = null!;

        // When
        Action act = () => journal.SetConnectionManager(nullConnectionManager);

        // Then
        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("connectionManager");
        }
    }

    [Test]
    public async Task EnsureTableExistsAsync_WithValidConnectionManager_ShouldExecuteSuccessfully()
    {
        // Given
        var cancellationToken = CancellationToken.None;

        _mockConnectionManager.Setup(cm => cm.ExecuteWithManagedConnectionAsync(
            It.IsAny<Func<IDbConnection, Task>>(),
            It.IsAny<CancellationToken>()))
            .Callback<Func<IDbConnection, Task>, CancellationToken>(async (action, token) =>
            {
                var mockConnection = new Mock<SqlConnection>();
                await action(mockConnection.Object);
            })
            .Returns(Task.CompletedTask);

        // When
        Func<Task> act = async () => await _journal.EnsureTableExistsAsync(_mockConnectionManager.Object, cancellationToken);

        // Then
        using (new AssertionScope())
        {
            await act.Should().NotThrowAsync();
            _mockConnectionManager.Verify(cm => cm.ExecuteWithManagedConnectionAsync(
                It.IsAny<Func<IDbConnection, Task>>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    [Test]
    public async Task EnsureTableExistsAsync_WithConnectionException_ShouldThrowAndLog()
    {
        // Given
        var cancellationToken = CancellationToken.None;
        var expectedException = new SqlException("Table creation failed", null, null, 0);

        _mockConnectionManager.Setup(cm => cm.ExecuteWithManagedConnectionAsync(
            It.IsAny<Func<IDbConnection, Task>>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // When
        Func<Task> act = async () => await _journal.EnsureTableExistsAsync(_mockConnectionManager.Object, cancellationToken);

        // Then
        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<SqlException>()
                .WithMessage("Table creation failed");
            
            _mockLogProvider.Verify(lp => lp.WriteError(
                It.Is<string>(msg => msg.Contains("journal table")),
                It.Is<Exception>(ex => ex == expectedException)), Times.Once);
        }
    }

    [Test]
    public async Task StoreExecutedMigrationAsync_WithValidMigration_ShouldExecuteSuccessfully()
    {
        // Given
        var script = new GenericScript("001_Migration.sql", "CREATE TABLE Test");
        var migration = new Migration("001_Migration", script, null);
        var result = new MigrationResult
        {
            Script = script,
            Successful = true,
            ExecutionTime = TimeSpan.FromSeconds(5)
        };
        var cancellationToken = CancellationToken.None;

        _mockConnectionManager.Setup(cm => cm.ExecuteWithManagedConnectionAsync(
            It.IsAny<Func<IDbConnection, Task>>(),
            It.IsAny<CancellationToken>()))
            .Callback<Func<IDbConnection, Task>, CancellationToken>(async (action, token) =>
            {
                var mockConnection = new Mock<SqlConnection>();
                await action(mockConnection.Object);
            })
            .Returns(Task.CompletedTask);

        // When
        Func<Task> act = async () => await _journal.StoreExecutedMigrationAsync(migration, result, cancellationToken);

        // Then
        using (new AssertionScope())
        {
            await act.Should().NotThrowAsync();
            _mockConnectionManager.Verify(cm => cm.ExecuteWithManagedConnectionAsync(
                It.IsAny<Func<IDbConnection, Task>>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    [Test]
    public async Task StoreExecutedMigrationAsync_WithNullMigration_ShouldThrowArgumentNullException()
    {
        // Given
        IMigration nullMigration = null!;
        var result = new MigrationResult();
        var cancellationToken = CancellationToken.None;

        // When
        Func<Task> act = async () => await _journal.StoreExecutedMigrationAsync(nullMigration, result, cancellationToken);

        // Then
        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("migration");
        }
    }

    [Test]
    public async Task StoreExecutedMigrationAsync_WithNullResult_ShouldThrowArgumentNullException()
    {
        // Given
        var script = new GenericScript("001_Migration.sql", "CREATE TABLE Test");
        var migration = new Migration("001_Migration", script, null);
        MigrationResult nullResult = null!;
        var cancellationToken = CancellationToken.None;

        // When
        Func<Task> act = async () => await _journal.StoreExecutedMigrationAsync(migration, nullResult, cancellationToken);

        // Then
        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("result");
        }
    }

    [Test]
    public async Task StoreExecutedMigrationAsync_WithEmptyScriptHash_ShouldLogWarningAndSkip()
    {
        // Given
        var script = new GenericScript("001_Migration.sql", "CREATE TABLE Test");
        // Create a migration with a script that has an empty hash
        var mockScript = new Mock<IScript>();
        mockScript.Setup(s => s.Hash).Returns(string.Empty);
        mockScript.Setup(s => s.Name).Returns("001_Migration.sql");
        mockScript.Setup(s => s.Script).Returns("CREATE TABLE Test");
        
        var migration = new Migration("001_Migration", mockScript.Object, null);
        var result = new MigrationResult
        {
            Script = mockScript.Object,
            Successful = true,
            ExecutionTime = TimeSpan.FromSeconds(5)
        };
        var cancellationToken = CancellationToken.None;

        // When
        Func<Task> act = async () => await _journal.StoreExecutedMigrationAsync(migration, result, cancellationToken);

        // Then
        using (new AssertionScope())
        {
            await act.Should().NotThrowAsync();
            _mockLogProvider.Verify(lp => lp.WriteWarning(
                It.Is<string>(msg => msg.Contains("empty hash"))), Times.Once);
            _mockConnectionManager.Verify(cm => cm.ExecuteWithManagedConnectionAsync(
                It.IsAny<Func<IDbConnection, Task>>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }
    }

    [Test]
    public async Task StoreExecutedMigrationAsync_WithoutConnectionManager_ShouldThrowInvalidOperationException()
    {
        // Given
        var journal = new SqlServerScriptJournal();
        var script = new GenericScript("001_Migration.sql", "CREATE TABLE Test");
        var migration = new Migration("001_Migration", script, null);
        var result = new MigrationResult { Script = script };
        var cancellationToken = CancellationToken.None;

        // When
        Func<Task> act = async () => await journal.StoreExecutedMigrationAsync(migration, result, cancellationToken);

        // Then
        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*ConnectionManager must be set*");
        }
    }

    [Test]
    public async Task RemoveExecutedMigrationAsync_WithValidHash_ShouldExecuteSuccessfully()
    {
        // Given
        var scriptHash = "test-hash-123";
        var cancellationToken = CancellationToken.None;

        _mockConnectionManager.Setup(cm => cm.ExecuteWithManagedConnectionAsync(
            It.IsAny<Func<IDbConnection, Task>>(),
            It.IsAny<CancellationToken>()))
            .Callback<Func<IDbConnection, Task>, CancellationToken>(async (action, token) =>
            {
                var mockConnection = new Mock<SqlConnection>();
                await action(mockConnection.Object);
            })
            .Returns(Task.CompletedTask);

        // When
        Func<Task> act = async () => await _journal.RemoveExecutedMigrationAsync(scriptHash, cancellationToken);

        // Then
        using (new AssertionScope())
        {
            await act.Should().NotThrowAsync();
            _mockConnectionManager.Verify(cm => cm.ExecuteWithManagedConnectionAsync(
                It.IsAny<Func<IDbConnection, Task>>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    [Test]
    public async Task RemoveExecutedMigrationAsync_WithNullHash_ShouldThrowArgumentNullException()
    {
        // Given
        string nullHash = null!;
        var cancellationToken = CancellationToken.None;

        // When
        Func<Task> act = async () => await _journal.RemoveExecutedMigrationAsync(nullHash, cancellationToken);

        // Then
        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("upgradeScriptHash");
        }
    }

    [Test]
    public async Task RemoveExecutedMigrationAsync_WithConnectionException_ShouldThrowAndLog()
    {
        // Given
        var scriptHash = "test-hash-123";
        var cancellationToken = CancellationToken.None;
        var expectedException = new SqlException("Delete failed", null, null, 0);

        _mockConnectionManager.Setup(cm => cm.ExecuteWithManagedConnectionAsync(
            It.IsAny<Func<IDbConnection, Task>>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // When
        Func<Task> act = async () => await _journal.RemoveExecutedMigrationAsync(scriptHash, cancellationToken);

        // Then
        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<SqlException>()
                .WithMessage("Delete failed");
            
            _mockLogProvider.Verify(lp => lp.WriteError(
                It.Is<string>(msg => msg.Contains("remove executed migration")),
                It.Is<Exception>(ex => ex == expectedException)), Times.Once);
        }
    }

    [Test]
    public async Task GetExecutedMigrationsAsync_WithValidConnection_ShouldReturnEntries()
    {
        // Given
        var cancellationToken = CancellationToken.None;
        var expectedEntries = new List<MigrationJournalEntry>
        {
            new MigrationJournalEntry
            {
                Id = 1,
                UpgradeScriptHash = "hash1",
                MigrationName = "001_Migration",
                DowngradeScript = "DROP TABLE Test1",
                MigratedOn = DateTime.UtcNow.AddDays(-1),
                ExecutionTime = TimeSpan.FromSeconds(3)
            },
            new MigrationJournalEntry
            {
                Id = 2,
                UpgradeScriptHash = "hash2",
                MigrationName = "002_Migration",
                DowngradeScript = null,
                MigratedOn = DateTime.UtcNow,
                ExecutionTime = TimeSpan.FromSeconds(2)
            }
        };

        _mockConnectionManager.Setup(cm => cm.ExecuteWithManagedConnectionAsync(
            It.IsAny<Func<IDbConnection, Task<IEnumerable<MigrationJournalEntry>>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedEntries);

        // When
        var result = await _journal.GetExecutedMigrationsAsync(cancellationToken);

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            
            var resultArray = result.ToArray();
            resultArray[0].Id.Should().Be(1);
            resultArray[0].UpgradeScriptHash.Should().Be("hash1");
            resultArray[0].MigrationName.Should().Be("001_Migration");
            resultArray[0].DowngradeScript.Should().Be("DROP TABLE Test1");
            
            resultArray[1].Id.Should().Be(2);
            resultArray[1].UpgradeScriptHash.Should().Be("hash2");
            resultArray[1].MigrationName.Should().Be("002_Migration");
            resultArray[1].DowngradeScript.Should().BeNull();
        }
    }

    [Test]
    public async Task GetExecutedMigrationsAsync_WithConnectionException_ShouldThrowAndLog()
    {
        // Given
        var cancellationToken = CancellationToken.None;
        var expectedException = new SqlException("Query failed", null, null, 0);

        _mockConnectionManager.Setup(cm => cm.ExecuteWithManagedConnectionAsync(
            It.IsAny<Func<IDbConnection, Task<IEnumerable<MigrationJournalEntry>>>>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // When
        Func<Task> act = async () => await _journal.GetExecutedMigrationsAsync(cancellationToken);

        // Then
        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<SqlException>()
                .WithMessage("Query failed");
            
            _mockLogProvider.Verify(lp => lp.WriteError(
                It.Is<string>(msg => msg.Contains("get executed migrations")),
                It.Is<Exception>(ex => ex == expectedException)), Times.Once);
        }
    }

    [Test]
    public async Task HasBeenExecutedAsync_WithExecutedMigration_ShouldReturnTrue()
    {
        // Given
        var script = new GenericScript("001_Migration.sql", "CREATE TABLE Test");
        var migration = new Migration("001_Migration", script, null);
        var cancellationToken = CancellationToken.None;

        _mockConnectionManager.Setup(cm => cm.ExecuteWithManagedConnectionAsync(
            It.IsAny<Func<IDbConnection, Task<bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // When
        var result = await _journal.HasBeenExecutedAsync(migration, cancellationToken);

        // Then
        using (new AssertionScope())
        {
            result.Should().BeTrue();
        }
    }

    [Test]
    public async Task HasBeenExecutedAsync_WithNonExecutedMigration_ShouldReturnFalse()
    {
        // Given
        var script = new GenericScript("001_Migration.sql", "CREATE TABLE Test");
        var migration = new Migration("001_Migration", script, null);
        var cancellationToken = CancellationToken.None;

        _mockConnectionManager.Setup(cm => cm.ExecuteWithManagedConnectionAsync(
            It.IsAny<Func<IDbConnection, Task<bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // When
        var result = await _journal.HasBeenExecutedAsync(migration, cancellationToken);

        // Then
        using (new AssertionScope())
        {
            result.Should().BeFalse();
        }
    }

    [Test]
    public async Task HasBeenExecutedAsync_WithNullMigration_ShouldThrowArgumentNullException()
    {
        // Given
        IMigration nullMigration = null!;
        var cancellationToken = CancellationToken.None;

        // When
        Func<Task> act = async () => await _journal.HasBeenExecutedAsync(nullMigration, cancellationToken);

        // Then
        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("migration");
        }
    }

    [Test]
    public async Task HasBeenExecutedAsync_WithConnectionException_ShouldThrowAndLog()
    {
        // Given
        var script = new GenericScript("001_Migration.sql", "CREATE TABLE Test");
        var migration = new Migration("001_Migration", script, null);
        var cancellationToken = CancellationToken.None;
        var expectedException = new SqlException("Query failed", null, null, 0);

        _mockConnectionManager.Setup(cm => cm.ExecuteWithManagedConnectionAsync(
            It.IsAny<Func<IDbConnection, Task<bool>>>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // When
        Func<Task> act = async () => await _journal.HasBeenExecutedAsync(migration, cancellationToken);

        // Then
        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<SqlException>()
                .WithMessage("Query failed");
            
            _mockLogProvider.Verify(lp => lp.WriteError(
                It.Is<string>(msg => msg.Contains("check if migration")),
                It.Is<Exception>(ex => ex == expectedException)), Times.Once);
        }
    }

    [Test]
    public async Task HasBeenExecutedAsync_WithoutConnectionManager_ShouldThrowInvalidOperationException()
    {
        // Given
        var journal = new SqlServerScriptJournal();
        var script = new GenericScript("001_Migration.sql", "CREATE TABLE Test");
        var migration = new Migration("001_Migration", script, null);
        var cancellationToken = CancellationToken.None;

        // When
        Func<Task> act = async () => await journal.HasBeenExecutedAsync(migration, cancellationToken);

        // Then
        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*ConnectionManager must be set*");
        }
    }
}