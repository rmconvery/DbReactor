using DbReactor.Core.Abstractions;
using DbReactor.Core.Configuration;
using DbReactor.Core.Enumerations;
using DbReactor.Core.Models;
using DbReactor.Core.Models.Scripts;
using DbReactor.Core.Discovery;
using DbReactor.Core.Journaling;
using DbReactor.Core.Services;

namespace DbReactor.Core.Tests.Engine;

[TestFixture]
public class MigrationFilteringServiceTests
{
    private Mock<IMigrationJournal> _mockJournal;
    private Mock<IScriptProvider> _mockScriptProvider;
    private Mock<IMigrationBuilder> _mockMigrationBuilder;
    private DbReactorConfiguration _configuration;
    private MigrationFilteringService _service;

    [SetUp]
    public void SetUp()
    {
        _mockJournal = new Mock<IMigrationJournal>();
        _mockScriptProvider = new Mock<IScriptProvider>();
        _mockMigrationBuilder = new Mock<IMigrationBuilder>();
        
        _configuration = new DbReactorConfiguration();
        _configuration.MigrationJournal = _mockJournal.Object;
        _configuration.ScriptProviders = new List<IScriptProvider> { _mockScriptProvider.Object };
        _configuration.ExecutionOrder = ScriptExecutionOrder.ByNameAscending;
        
        _service = new MigrationFilteringService(_configuration);
    }

    [Test]
    public async Task GetPendingUpgradesAsync_WhenNoMigrationsExecuted_ShouldReturnAllMigrations()
    {
        // Given
        var script1 = new GenericScript("001_Migration.sql", "CREATE TABLE Test1");
        var script2 = new GenericScript("002_Migration.sql", "CREATE TABLE Test2");
        var migration1 = new Migration("001_Migration", script1, null);
        var migration2 = new Migration("002_Migration", script2, null);
        
        _mockScriptProvider.Setup(p => p.GetScripts())
            .Returns(new[] { script1, script2 });
        
        _mockJournal.Setup(j => j.HasBeenExecutedAsync(It.Is<IMigration>(m => m.Name == "001_Migration"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockJournal.Setup(j => j.HasBeenExecutedAsync(It.Is<IMigration>(m => m.Name == "002_Migration"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // When
        var result = await _service.GetPendingUpgradesAsync();

        // Then
        using (new AssertionScope())
        {
            result.Should().HaveCount(2);
            result.Should().Contain(m => m.UpgradeScript.Name == "001_Migration.sql");
            result.Should().Contain(m => m.UpgradeScript.Name == "002_Migration.sql");
        }
    }

    [Test]
    public async Task GetPendingUpgradesAsync_WhenSomeMigrationsExecuted_ShouldReturnOnlyPendingMigrations()
    {
        // Given
        var script1 = new GenericScript("001_Migration.sql", "CREATE TABLE Test1");
        var script2 = new GenericScript("002_Migration.sql", "CREATE TABLE Test2");
        var migration1 = new Migration("001_Migration", script1, null);
        var migration2 = new Migration("002_Migration", script2, null);
        
        _mockScriptProvider.Setup(p => p.GetScripts())
            .Returns(new[] { script1, script2 });
        
        _mockJournal.Setup(j => j.HasBeenExecutedAsync(It.Is<IMigration>(m => m.Name == "001_Migration"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockJournal.Setup(j => j.HasBeenExecutedAsync(It.Is<IMigration>(m => m.Name == "002_Migration"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // When
        var result = await _service.GetPendingUpgradesAsync();

        // Then
        using (new AssertionScope())
        {
            result.Should().HaveCount(1);
            result.Should().Contain(m => m.UpgradeScript.Name == "002_Migration.sql");
            result.Should().NotContain(m => m.UpgradeScript.Name == "001_Migration.sql");
        }
    }

    [Test]
    public async Task GetPendingUpgradesAsync_WhenAllMigrationsExecuted_ShouldReturnEmptyCollection()
    {
        // Given
        var script1 = new GenericScript("001_Migration.sql", "CREATE TABLE Test1");
        var script2 = new GenericScript("002_Migration.sql", "CREATE TABLE Test2");
        var migration1 = new Migration("001_Migration", script1, null);
        var migration2 = new Migration("002_Migration", script2, null);
        
        _mockScriptProvider.Setup(p => p.GetScripts())
            .Returns(new[] { script1, script2 });
        
        _mockJournal.Setup(j => j.HasBeenExecutedAsync(It.Is<IMigration>(m => m.Name == "001_Migration"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockJournal.Setup(j => j.HasBeenExecutedAsync(It.Is<IMigration>(m => m.Name == "002_Migration"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // When
        var result = await _service.GetPendingUpgradesAsync();

        // Then
        using (new AssertionScope())
        {
            result.Should().BeEmpty();
        }
    }

    [Test]
    public async Task GetAppliedUpgradesAsync_WhenSomeMigrationsExecuted_ShouldReturnOnlyAppliedMigrations()
    {
        // Given
        var script1 = new GenericScript("001_Migration.sql", "CREATE TABLE Test1");
        var script2 = new GenericScript("002_Migration.sql", "CREATE TABLE Test2");
        
        _mockScriptProvider.Setup(p => p.GetScripts())
            .Returns(new[] { script1, script2 });
        
        _mockJournal.Setup(j => j.HasBeenExecutedAsync(It.Is<IMigration>(m => m.UpgradeScript == script1), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockJournal.Setup(j => j.HasBeenExecutedAsync(It.Is<IMigration>(m => m.UpgradeScript == script2), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // When
        var result = await _service.GetAppliedUpgradesAsync();

        // Then
        using (new AssertionScope())
        {
            result.Should().HaveCount(1);
            result.First().UpgradeScript.Name.Should().Be("001_Migration.sql");
        }
    }

    [Test]
    public async Task HasPendingUpgradesAsync_WhenPendingUpgradesExist_ShouldReturnTrue()
    {
        // Given
        var script1 = new GenericScript("001_Migration.sql", "CREATE TABLE Test1");
        var migration1 = new Migration("001_Migration", script1, null);
        
        _mockScriptProvider.Setup(p => p.GetScripts())
            .Returns(new[] { script1 });
        
        _mockJournal.Setup(j => j.HasBeenExecutedAsync(It.Is<IMigration>(m => m.Name == "001_Migration"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // When
        var result = await _service.HasPendingUpgradesAsync();

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
        var script1 = new GenericScript("001_Migration.sql", "CREATE TABLE Test1");
        var migration1 = new Migration("001_Migration", script1, null);
        
        _mockScriptProvider.Setup(p => p.GetScripts())
            .Returns(new[] { script1 });
        
        _mockJournal.Setup(j => j.HasBeenExecutedAsync(It.Is<IMigration>(m => m.Name == "001_Migration"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // When
        var result = await _service.HasPendingUpgradesAsync();

        // Then
        using (new AssertionScope())
        {
            result.Should().BeFalse();
        }
    }

    [Test]
    public async Task GetEntriesToDowngradeAsync_WhenJournalHasEntriesNotInMigrations_ShouldReturnThoseEntries()
    {
        // Given
        var mockScript1 = new Mock<IScript>();
        mockScript1.Setup(s => s.Hash).Returns("hash1");
        mockScript1.Setup(s => s.Name).Returns("001_Migration");
        
        var journalEntry1 = new MigrationJournalEntry 
        { 
            UpgradeScriptHash = "hash1", 
            MigrationName = "001_Migration" 
        };
        var journalEntry2 = new MigrationJournalEntry 
        { 
            UpgradeScriptHash = "hash2", 
            MigrationName = "002_Migration" 
        };
        
        _mockScriptProvider.Setup(p => p.GetScripts())
            .Returns(new[] { mockScript1.Object });
        
        _mockJournal.Setup(j => j.GetExecutedMigrationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { journalEntry1, journalEntry2 });

        // When
        var result = await _service.GetEntriesToDowngradeAsync();

        // Then
        using (new AssertionScope())
        {
            result.Should().HaveCount(1);
            result.Should().Contain(e => e.UpgradeScriptHash == "hash2");
            result.Should().NotContain(e => e.UpgradeScriptHash == "hash1");
        }
    }

    [Test]
    public async Task GetEntriesToDowngradeAsync_WhenAllJournalEntriesHaveMatchingMigrations_ShouldReturnEmptyCollection()
    {
        // Given
        var mockScript1 = new Mock<IScript>();
        mockScript1.Setup(s => s.Hash).Returns("hash1");
        mockScript1.Setup(s => s.Name).Returns("001_Migration");
        
        var mockScript2 = new Mock<IScript>();
        mockScript2.Setup(s => s.Hash).Returns("hash2");
        mockScript2.Setup(s => s.Name).Returns("002_Migration");
        
        var journalEntry1 = new MigrationJournalEntry 
        { 
            UpgradeScriptHash = "hash1", 
            MigrationName = "001_Migration" 
        };
        var journalEntry2 = new MigrationJournalEntry 
        { 
            UpgradeScriptHash = "hash2", 
            MigrationName = "002_Migration" 
        };
        
        _mockScriptProvider.Setup(p => p.GetScripts())
            .Returns(new[] { mockScript1.Object, mockScript2.Object });
        
        _mockJournal.Setup(j => j.GetExecutedMigrationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { journalEntry1, journalEntry2 });

        // When
        var result = await _service.GetEntriesToDowngradeAsync();

        // Then
        using (new AssertionScope())
        {
            result.Should().BeEmpty();
        }
    }

    [Test]
    public async Task GetEntriesToDowngradeAsync_ShouldReturnEntriesInReverseOrder()
    {
        // Given
        var journalEntry1 = new MigrationJournalEntry 
        { 
            UpgradeScriptHash = "hash1", 
            MigrationName = "001_Migration" 
        };
        var journalEntry2 = new MigrationJournalEntry 
        { 
            UpgradeScriptHash = "hash2", 
            MigrationName = "002_Migration" 
        };
        var journalEntry3 = new MigrationJournalEntry 
        { 
            UpgradeScriptHash = "hash3", 
            MigrationName = "003_Migration" 
        };
        
        _mockScriptProvider.Setup(p => p.GetScripts())
            .Returns(new IScript[0]);
        
        _mockJournal.Setup(j => j.GetExecutedMigrationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { journalEntry1, journalEntry2, journalEntry3 });

        // When
        var result = await _service.GetEntriesToDowngradeAsync();

        // Then
        using (new AssertionScope())
        {
            var resultArray = result.ToArray();
            resultArray.Should().HaveCount(3);
            resultArray[0].Should().Be(journalEntry3);
            resultArray[1].Should().Be(journalEntry2);
            resultArray[2].Should().Be(journalEntry1);
        }
    }

    [Test]
    public async Task GetMigrations_WhenMigrationBuilderIsConfigured_ShouldUseMigrationBuilder()
    {
        // Given
        var migration1 = new Migration("001_Migration", new GenericScript("001.sql", "SQL1"), null);
        var migration2 = new Migration("002_Migration", new GenericScript("002.sql", "SQL2"), null);
        
        _configuration.MigrationBuilder = _mockMigrationBuilder.Object;
        _mockMigrationBuilder.Setup(b => b.BuildMigrations())
            .Returns(new[] { migration1, migration2 });
        
        _mockJournal.Setup(j => j.HasBeenExecutedAsync(It.IsAny<IMigration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // When
        var result = await _service.GetPendingUpgradesAsync();

        // Then
        using (new AssertionScope())
        {
            _mockMigrationBuilder.Verify(b => b.BuildMigrations(), Times.Once);
            _mockScriptProvider.Verify(p => p.GetScripts(), Times.Never);
            result.Should().HaveCount(2);
        }
    }

    [Test]
    public async Task GetMigrations_WhenOrderingByNameAscending_ShouldReturnMigrationsInAscendingOrder()
    {
        // Given
        var script1 = new GenericScript("003_Migration.sql", "CREATE TABLE Test3");
        var script2 = new GenericScript("001_Migration", "CREATE TABLE Test1");
        var script3 = new GenericScript("002_Migration", "CREATE TABLE Test2");
        
        _configuration.ExecutionOrder = ScriptExecutionOrder.ByNameAscending;
        _mockScriptProvider.Setup(p => p.GetScripts())
            .Returns(new[] { script1, script2, script3 });
        
        _mockJournal.Setup(j => j.HasBeenExecutedAsync(It.IsAny<IMigration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // When
        var result = await _service.GetPendingUpgradesAsync();

        // Then
        using (new AssertionScope())
        {
            var resultArray = result.ToArray();
            resultArray.Should().HaveCount(3);
            resultArray.Should().HaveCount(3);
            // Verify the scripts are ordered correctly by their names (processed by GetBaseName)
            resultArray[0].UpgradeScript.Name.Should().Be("001_Migration");
            resultArray[1].UpgradeScript.Name.Should().Be("002_Migration");
            resultArray[2].UpgradeScript.Name.Should().Be("003_Migration.sql");
        }
    }

    [Test]
    public async Task GetMigrations_WhenOrderingByNameDescending_ShouldReturnMigrationsInDescendingOrder()
    {
        // Given
        var script1 = new GenericScript("001_Migration.sql", "CREATE TABLE Test1");
        var script2 = new GenericScript("002_Migration.sql", "CREATE TABLE Test2");
        var script3 = new GenericScript("003_Migration.sql", "CREATE TABLE Test3");
        
        _configuration.ExecutionOrder = ScriptExecutionOrder.ByNameDescending;
        _mockScriptProvider.Setup(p => p.GetScripts())
            .Returns(new[] { script1, script2, script3 });
        
        _mockJournal.Setup(j => j.HasBeenExecutedAsync(It.IsAny<IMigration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // When
        var result = await _service.GetPendingUpgradesAsync();

        // Then
        using (new AssertionScope())
        {
            var resultArray = result.ToArray();
            resultArray.Should().HaveCount(3);
            resultArray.Should().HaveCount(3);
            // Verify the scripts are ordered correctly by their names in descending order
            resultArray[0].UpgradeScript.Name.Should().Be("003_Migration.sql");
            resultArray[1].UpgradeScript.Name.Should().Be("002_Migration.sql");
            resultArray[2].UpgradeScript.Name.Should().Be("001_Migration.sql");
        }
    }
}