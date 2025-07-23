using DbReactor.Core.Abstractions;
using DbReactor.Core.Configuration;
using DbReactor.Core.Engine;
using DbReactor.Core.Models;
using DbReactor.Core.Execution;
using DbReactor.Core.Journaling;
using DbReactor.Core.Discovery;
using System.Threading;

namespace DbReactor.Core.Tests.Engine;

[TestFixture]
public class DbReactorEngineTests
{
    private Mock<IConnectionManager> _mockConnectionManager;
    private Mock<IScriptExecutor> _mockScriptExecutor;
    private Mock<IMigrationJournal> _mockJournal;
    private Mock<IScriptProvider> _mockScriptProvider;
    private DbReactorConfiguration _configuration;

    [SetUp]
    public void SetUp()
    {
        _mockConnectionManager = new Mock<IConnectionManager>();
        _mockScriptExecutor = new Mock<IScriptExecutor>();
        _mockJournal = new Mock<IMigrationJournal>();
        _mockScriptProvider = new Mock<IScriptProvider>();
        
        _configuration = new DbReactorConfiguration
        {
            ConnectionManager = _mockConnectionManager.Object,
            ScriptExecutor = _mockScriptExecutor.Object,
            MigrationJournal = _mockJournal.Object,
            ScriptProviders = new List<IScriptProvider> { _mockScriptProvider.Object },
            EnableVariables = false,
            Variables = new Dictionary<string, string>()
        };
    }


    [Test]
    public void Constructor_WhenConfigurationIsValid_ShouldCreateEngineSuccessfully()
    {
        // Given
        var validConfig = _configuration;

        // When
        var engine = new DbReactorEngine(validConfig);

        // Then
        using (new AssertionScope())
        {
            engine.Should().NotBeNull();
        }
    }

    [Test]
    public async Task RunAsync_WhenCalled_ShouldDelegateToOrchestrator()
    {
        // Given
        var engine = new DbReactorEngine(_configuration);
        var expectedResult = new DbReactorResult { Successful = true };
        
        _mockScriptProvider.Setup(p => p.GetScriptsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<IScript>());
        _mockJournal.Setup(j => j.EnsureTableExistsAsync(_mockConnectionManager.Object, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockJournal.Setup(j => j.GetExecutedMigrationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MigrationJournalEntry>());

        // When
        var result = await engine.RunAsync();

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNull();
            result.Successful.Should().BeTrue();
        }
    }

    [Test]
    public async Task ApplyUpgradesAsync_WhenCalled_ShouldDelegateToOrchestrator()
    {
        // Given
        var engine = new DbReactorEngine(_configuration);
        
        _mockScriptProvider.Setup(p => p.GetScriptsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<IScript>());
        _mockJournal.Setup(j => j.EnsureTableExistsAsync(_mockConnectionManager.Object, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockJournal.Setup(j => j.HasBeenExecutedAsync(It.IsAny<IMigration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // When
        var result = await engine.ApplyUpgradesAsync();

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNull();
            result.Successful.Should().BeTrue();
        }
    }

    [Test]
    public async Task ApplyDowngradesAsync_WhenCalled_ShouldDelegateToOrchestrator()
    {
        // Given
        var engine = new DbReactorEngine(_configuration);
        
        _mockScriptProvider.Setup(p => p.GetScriptsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<IScript>());
        _mockJournal.Setup(j => j.EnsureTableExistsAsync(_mockConnectionManager.Object, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockJournal.Setup(j => j.GetExecutedMigrationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MigrationJournalEntry>());

        // When
        var result = await engine.ApplyDowngradesAsync();

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNull();
            result.Successful.Should().BeTrue();
        }
    }

    [Test]
    public async Task HasPendingUpgradesAsync_WhenPendingUpgradesExist_ShouldReturnTrue()
    {
        // Given
        var engine = new DbReactorEngine(_configuration);
        var script = new Mock<IScript>();
        script.Setup(s => s.Name).Returns("001_Migration");
        
        _mockScriptProvider.Setup(p => p.GetScriptsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[] { script.Object });
        _mockJournal.Setup(j => j.HasBeenExecutedAsync(It.IsAny<IMigration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // When
        var result = await engine.HasPendingUpgradesAsync();

        // Then
        using (new AssertionScope())
        {
            result.Should().BeTrue();
        }
    }

    [Test]
    public async Task HasPendingUpgradesAsync_WhenNoPendingUpgrades_ShouldReturnFalse()
    {
        // Given
        var engine = new DbReactorEngine(_configuration);
        var script = new Mock<IScript>();
        script.Setup(s => s.Name).Returns("001_Migration");
        
        _mockScriptProvider.Setup(p => p.GetScriptsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[] { script.Object });
        _mockJournal.Setup(j => j.HasBeenExecutedAsync(It.IsAny<IMigration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // When
        var result = await engine.HasPendingUpgradesAsync();

        // Then
        using (new AssertionScope())
        {
            result.Should().BeFalse();
        }
    }

    [Test]
    public async Task GetPendingUpgradesAsync_WhenCalled_ShouldReturnPendingMigrations()
    {
        // Given
        var engine = new DbReactorEngine(_configuration);
        var script = new Mock<IScript>();
        script.Setup(s => s.Name).Returns("MyApp.Scripts.001_Migration.sql");
        
        _mockScriptProvider.Setup(p => p.GetScriptsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[] { script.Object });
        _mockJournal.Setup(j => j.HasBeenExecutedAsync(It.IsAny<IMigration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // When
        var result = await engine.GetPendingUpgradesAsync();

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
        }
    }

    [Test]
    public async Task GetAppliedUpgradesAsync_WhenCalled_ShouldReturnAppliedMigrations()
    {
        // Given
        var engine = new DbReactorEngine(_configuration);
        var script = new Mock<IScript>();
        script.Setup(s => s.Name).Returns("MyApp.Scripts.001_Migration.sql");
        
        _mockScriptProvider.Setup(p => p.GetScriptsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[] { script.Object });
        _mockJournal.Setup(j => j.HasBeenExecutedAsync(It.IsAny<IMigration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // When
        var result = await engine.GetAppliedUpgradesAsync();

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
        }
    }



}