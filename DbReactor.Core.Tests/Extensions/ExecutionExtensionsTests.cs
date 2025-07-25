using DbReactor.Core.Configuration;
using DbReactor.Core.Execution;
using DbReactor.Core.Extensions;
using DbReactor.Core.Journaling;
using FluentAssertions;
using FluentAssertions.Execution;
using Moq;
using NUnit.Framework;
using System;

namespace DbReactor.Core.Tests.Extensions;

[TestFixture]
public class ExecutionExtensionsTests
{
    private DbReactorConfiguration _config;
    
    [SetUp]
    public void SetUp()
    {
        _config = new DbReactorConfiguration();
    }

    [Test]
    public void AddConnectionManager_WhenManagerIsValid_ShouldSetConnectionManager()
    {
        // Given
        var mockManager = new Mock<IConnectionManager>();
        
        // When
        var result = _config.AddConnectionManager(mockManager.Object);
        
        // Then
        using (new AssertionScope())
        {
            result.Should().Be(_config);
            _config.ConnectionManager.Should().Be(mockManager.Object);
        }
    }

    [Test]
    public void AddConnectionManager_WhenManagerIsNull_ShouldThrowArgumentNullException()
    {
        // When
        Action act = () => _config.AddConnectionManager(null);
        
        // Then
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("connectionManager");
    }

    [Test]
    public void AddScriptExecutor_WhenExecutorIsValid_ShouldSetScriptExecutor()
    {
        // Given
        var mockExecutor = new Mock<IScriptExecutor>();
        
        // When
        var result = _config.AddScriptExecutor(mockExecutor.Object);
        
        // Then
        using (new AssertionScope())
        {
            result.Should().Be(_config);
            _config.ScriptExecutor.Should().Be(mockExecutor.Object);
        }
    }

    [Test]
    public void AddScriptExecutor_WhenExecutorIsNull_ShouldThrowArgumentNullException()
    {
        // When
        Action act = () => _config.AddScriptExecutor(null);
        
        // Then
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("scriptExecutor");
    }

    [Test]
    public void AddMigrationJournal_WhenJournalIsValid_ShouldSetMigrationJournal()
    {
        // Given
        var mockJournal = new Mock<IMigrationJournal>();
        
        // When
        var result = _config.AddMigrationJournal(mockJournal.Object);
        
        // Then
        using (new AssertionScope())
        {
            result.Should().Be(_config);
            _config.MigrationJournal.Should().Be(mockJournal.Object);
        }
    }

    [Test]
    public void AddMigrationJournal_WhenJournalIsNull_ShouldThrowArgumentNullException()
    {
        // When
        Action act = () => _config.AddMigrationJournal(null);
        
        // Then
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("migrationJournal");
    }

    [Test]
    public void AddConnectionManager_WhenCalledMultipleTimes_ShouldReplaceManager()
    {
        // Given
        var mockManager1 = new Mock<IConnectionManager>();
        var mockManager2 = new Mock<IConnectionManager>();
        
        // When
        _config
            .AddConnectionManager(mockManager1.Object)
            .AddConnectionManager(mockManager2.Object);
        
        // Then
        _config.ConnectionManager.Should().Be(mockManager2.Object);
    }

    [Test]
    public void AddScriptExecutor_WhenCalledMultipleTimes_ShouldReplaceExecutor()
    {
        // Given
        var mockExecutor1 = new Mock<IScriptExecutor>();
        var mockExecutor2 = new Mock<IScriptExecutor>();
        
        // When
        _config
            .AddScriptExecutor(mockExecutor1.Object)
            .AddScriptExecutor(mockExecutor2.Object);
        
        // Then
        _config.ScriptExecutor.Should().Be(mockExecutor2.Object);
    }

    [Test]
    public void AddMigrationJournal_WhenCalledMultipleTimes_ShouldReplaceJournal()
    {
        // Given
        var mockJournal1 = new Mock<IMigrationJournal>();
        var mockJournal2 = new Mock<IMigrationJournal>();
        
        // When
        _config
            .AddMigrationJournal(mockJournal1.Object)
            .AddMigrationJournal(mockJournal2.Object);
        
        // Then
        _config.MigrationJournal.Should().Be(mockJournal2.Object);
    }
}