using DbReactor.Core.Abstractions;
using DbReactor.Core.Configuration;
using DbReactor.Core.Engine;
using DbReactor.Core.Exceptions;
using DbReactor.Core.Models;
using DbReactor.Core.Models.Scripts;
using DbReactor.Core.Services;
using DbReactor.Core.Execution;
using DbReactor.Core.Journaling;
using DbReactor.Core.Discovery;

namespace DbReactor.Core.Tests.Engine;

[TestFixture]
public class ScriptExecutionServiceTests
{
    private Mock<IConnectionManager> _mockConnectionManager;
    private Mock<IScriptExecutor> _mockScriptExecutor;
    private Mock<IMigrationJournal> _mockJournal;
    private Mock<ITimeProvider> _mockTimeProvider;
    private DbReactorConfiguration _configuration;
    private ScriptExecutionService _service;

    [SetUp]
    public void SetUp()
    {
        _mockConnectionManager = new Mock<IConnectionManager>();
        _mockScriptExecutor = new Mock<IScriptExecutor>();
        _mockJournal = new Mock<IMigrationJournal>();
        _mockTimeProvider = new Mock<ITimeProvider>();
        
        _configuration = new DbReactorConfiguration
        {
            ConnectionManager = _mockConnectionManager.Object,
            ScriptExecutor = _mockScriptExecutor.Object,
            MigrationJournal = _mockJournal.Object,
            EnableVariables = false,
            Variables = new Dictionary<string, string>()
        };
        
        _service = new ScriptExecutionService(_configuration, _mockTimeProvider.Object);
    }

    [Test]
    public async Task ExecuteUpgradeAsync_WhenScriptExecutesSuccessfully_ShouldReturnSuccessfulResult()
    {
        // Given
        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddSeconds(5);
        var script = new GenericScript("001_Migration.sql", "CREATE TABLE Test");
        var migration = new Migration("001_Migration", script, null);
        
        var executionResult = new MigrationResult
        {
            Script = script,
            Successful = true,
            ExecutionTime = TimeSpan.FromSeconds(5)
        };
        
        _mockTimeProvider.SetupSequence(t => t.UtcNow)
            .Returns(startTime)
            .Returns(endTime);
        
        _mockScriptExecutor.Setup(e => e.ExecuteAsync(It.IsAny<IScript>(), _mockConnectionManager.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync(executionResult);

        // When
        var result = await _service.ExecuteUpgradeAsync(migration);

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNull();
            result.Successful.Should().BeTrue();
            result.Script.Should().Be(script);
            result.ExecutionTime.Should().Be(TimeSpan.FromSeconds(5));
        }
        
        _mockJournal.Verify(j => j.StoreExecutedMigrationAsync(It.IsAny<IMigration>(), result, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ExecuteUpgradeAsync_WhenScriptExecutionFails_ShouldNotStoreInJournal()
    {
        // Given
        var script = new GenericScript("001_Migration.sql", "INVALID SQL");
        var migration = new Migration("001_Migration", script, null);
        
        var executionResult = new MigrationResult
        {
            Script = script,
            Successful = false,
            Error = new Exception("SQL error"),
            ErrorMessage = "SQL error"
        };
        
        _mockScriptExecutor.Setup(e => e.ExecuteAsync(It.IsAny<IScript>(), _mockConnectionManager.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync(executionResult);

        // When
        var result = await _service.ExecuteUpgradeAsync(migration);

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNull();
            result.Successful.Should().BeFalse();
        }
        
        _mockJournal.Verify(j => j.StoreExecutedMigrationAsync(It.IsAny<IMigration>(), It.IsAny<MigrationResult>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task ExecuteUpgradeAsync_WhenScriptExecutorThrowsException_ShouldThrowMigrationExecutionException()
    {
        // Given
        var script = new GenericScript("001_Migration.sql", "CREATE TABLE Test");
        var migration = new Migration("001_Migration", script, null);
        
        _mockScriptExecutor.Setup(e => e.ExecuteAsync(It.IsAny<IScript>(), _mockConnectionManager.Object, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        // When
        Func<Task> act = async () => await _service.ExecuteUpgradeAsync(migration);

        // Then
        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<MigrationExecutionException>()
                .WithMessage("*001_Migration*Database connection failed*");
        }
        
        _mockJournal.Verify(j => j.StoreExecutedMigrationAsync(It.IsAny<IMigration>(), It.IsAny<MigrationResult>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task ExecuteUpgradeAsync_WhenVariablesEnabled_ShouldApplyVariableSubstitution()
    {
        // Given
        var script = new GenericScript("001_Migration.sql", "CREATE TABLE ${TableName}");
        var migration = new Migration("001_Migration", script, null);
        
        _configuration.EnableVariables = true;
        _configuration.Variables = new Dictionary<string, string> { { "TableName", "Users" } };
        
        var executionResult = new MigrationResult
        {
            Successful = true,
            ExecutionTime = TimeSpan.FromSeconds(1)
        };
        
        _mockScriptExecutor.Setup(e => e.ExecuteAsync(It.IsAny<IScript>(), _mockConnectionManager.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync(executionResult);

        // When
        var result = await _service.ExecuteUpgradeAsync(migration);

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNull();
            result.Successful.Should().BeTrue();
        }
        
        _mockScriptExecutor.Verify(e => e.ExecuteAsync(
            It.Is<IScript>(s => s.Script == "CREATE TABLE Users"), 
            _mockConnectionManager.Object, 
            It.IsAny<CancellationToken>()), Times.Once);
    }


    [Test]
    public async Task ExecuteDowngradeAsync_WhenDowngradeScriptExists_ShouldExecuteSuccessfully()
    {
        // Given
        var journalEntry = new MigrationJournalEntry
        {
            MigrationName = "001_Migration",
            UpgradeScriptHash = "hash123",
            DowngradeScript = "DROP TABLE Test"
        };
        
        var executionResult = new MigrationResult
        {
            Successful = true,
            ExecutionTime = TimeSpan.FromSeconds(2)
        };
        
        _mockScriptExecutor.Setup(e => e.ExecuteAsync(It.IsAny<IScript>(), _mockConnectionManager.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync(executionResult);

        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddSeconds(2);
        _mockTimeProvider.SetupSequence(t => t.UtcNow)
            .Returns(startTime)
            .Returns(endTime);

        // When
        var result = await _service.ExecuteDowngradeAsync(journalEntry);

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNull();
            result.Successful.Should().BeTrue();
            result.ExecutionTime.Should().Be(TimeSpan.FromSeconds(2));
        }
        
        _mockJournal.Verify(j => j.RemoveExecutedMigrationAsync("hash123", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ExecuteDowngradeAsync_WhenDowngradeScriptIsEmpty_ShouldReturnFailedResult()
    {
        // Given
        var journalEntry = new MigrationJournalEntry
        {
            MigrationName = "001_Migration",
            UpgradeScriptHash = "hash123",
            DowngradeScript = ""
        };

        // When
        var result = await _service.ExecuteDowngradeAsync(journalEntry);

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNull();
            result.Successful.Should().BeFalse();
            result.ErrorMessage.Should().Contain("does not support downgrade");
        }
    }

    [Test]
    public async Task ExecuteDowngradeAsync_WhenDowngradeScriptIsNull_ShouldReturnFailedResult()
    {
        // Given
        var journalEntry = new MigrationJournalEntry
        {
            MigrationName = "001_Migration",
            UpgradeScriptHash = "hash123",
            DowngradeScript = null
        };

        // When
        var result = await _service.ExecuteDowngradeAsync(journalEntry);

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNull();
            result.Successful.Should().BeFalse();
            result.ErrorMessage.Should().Contain("does not support downgrade");
        }
    }

    [Test]
    public async Task ExecuteDowngradeAsync_WhenExecutionFails_ShouldNotRemoveFromJournal()
    {
        // Given
        var journalEntry = new MigrationJournalEntry
        {
            MigrationName = "001_Migration",
            UpgradeScriptHash = "hash123",
            DowngradeScript = "DROP TABLE Test"
        };
        
        var executionResult = new MigrationResult
        {
            Successful = false,
            Error = new Exception("SQL error"),
            ErrorMessage = "SQL error"
        };
        
        _mockScriptExecutor.Setup(e => e.ExecuteAsync(It.IsAny<IScript>(), _mockConnectionManager.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync(executionResult);

        // When
        var result = await _service.ExecuteDowngradeAsync(journalEntry);

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNull();
            result.Successful.Should().BeFalse();
            result.ErrorMessage.Should().Be("SQL error");
        }
        
        _mockJournal.Verify(j => j.RemoveExecutedMigrationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task ExecuteDowngradeAsync_WhenVariablesEnabled_ShouldApplyVariableSubstitution()
    {
        // Given
        var journalEntry = new MigrationJournalEntry
        {
            MigrationName = "001_Migration",
            UpgradeScriptHash = "hash123",
            DowngradeScript = "DROP TABLE ${TableName}"
        };
        
        _configuration.EnableVariables = true;
        _configuration.Variables = new Dictionary<string, string> { { "TableName", "Users" } };
        
        var executionResult = new MigrationResult
        {
            Successful = true,
            ExecutionTime = TimeSpan.FromSeconds(1)
        };
        
        _mockScriptExecutor.Setup(e => e.ExecuteAsync(It.IsAny<IScript>(), _mockConnectionManager.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync(executionResult);

        // When
        var result = await _service.ExecuteDowngradeAsync(journalEntry);

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNull();
            result.Successful.Should().BeTrue();
        }
        
        _mockScriptExecutor.Verify(e => e.ExecuteAsync(
            It.Is<IScript>(s => s.Script == "DROP TABLE Users"), 
            _mockConnectionManager.Object, 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ExecuteDowngradeAsync_WhenScriptExecutorThrowsException_ShouldReturnFailedResult()
    {
        // Given
        var journalEntry = new MigrationJournalEntry
        {
            MigrationName = "001_Migration",
            UpgradeScriptHash = "hash123",
            DowngradeScript = "DROP TABLE Test"
        };
        
        _mockScriptExecutor.Setup(e => e.ExecuteAsync(It.IsAny<IScript>(), _mockConnectionManager.Object, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        // When
        var result = await _service.ExecuteDowngradeAsync(journalEntry);

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNull();
            result.Successful.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Database connection failed");
        }
    }
}