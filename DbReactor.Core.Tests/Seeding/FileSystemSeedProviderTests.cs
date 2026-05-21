using DbReactor.Core.Abstractions;
using DbReactor.Core.Discovery;
using DbReactor.Core.Seeding.Resolvers;
using DbReactor.Core.Seeding.Strategies;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DbReactor.Core.Tests.Seeding;

[TestFixture]
public class FileSystemSeedProviderTests
{
    private string _testDirectory;
    private FolderStructureSeedStrategyResolver _resolver;

    [SetUp]
    public void SetUp()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "DbReactor_Tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDirectory);
        _resolver = new FolderStructureSeedStrategyResolver();
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Test]
    public async Task GetSeedsAsync_WhenRunOnceFolderContainsScripts_ShouldAssignRunOnceStrategy()
    {
        // Given
        var runOnceDir = Path.Combine(_testDirectory, "run-once");
        Directory.CreateDirectory(runOnceDir);
        File.WriteAllText(Path.Combine(runOnceDir, "S001_CreateUsers.sql"), "CREATE TABLE Users (Id INT)");

        var provider = CreateProvider();

        // When
        var seeds = (await provider.GetSeedsAsync()).ToList();

        // Then
        using (new AssertionScope())
        {
            seeds.Should().HaveCount(1);
            seeds[0].Strategy.Should().BeOfType<RunOnceSeedStrategy>();
            seeds[0].Strategy.Name.Should().Be("RunOnce");
        }
    }

    [Test]
    public async Task GetSeedsAsync_WhenRunAlwaysFolderContainsScripts_ShouldAssignRunAlwaysStrategy()
    {
        // Given
        var runAlwaysDir = Path.Combine(_testDirectory, "run-always");
        Directory.CreateDirectory(runAlwaysDir);
        File.WriteAllText(Path.Combine(runAlwaysDir, "S001_RefreshViews.sql"), "EXEC sp_refreshview 'vw_Test'");

        var provider = CreateProvider();

        // When
        var seeds = (await provider.GetSeedsAsync()).ToList();

        // Then
        using (new AssertionScope())
        {
            seeds.Should().HaveCount(1);
            seeds[0].Strategy.Should().BeOfType<RunAlwaysSeedStrategy>();
            seeds[0].Strategy.Name.Should().Be("RunAlways");
        }
    }

    [Test]
    public async Task GetSeedsAsync_WhenRunIfChangedFolderContainsScripts_ShouldAssignRunIfChangedStrategy()
    {
        // Given
        var runIfChangedDir = Path.Combine(_testDirectory, "run-if-changed");
        Directory.CreateDirectory(runIfChangedDir);
        File.WriteAllText(Path.Combine(runIfChangedDir, "S001_SeedLookups.sql"), "INSERT INTO Lookups VALUES (1, 'Active')");

        var provider = CreateProvider();

        // When
        var seeds = (await provider.GetSeedsAsync()).ToList();

        // Then
        using (new AssertionScope())
        {
            seeds.Should().HaveCount(1);
            seeds[0].Strategy.Should().BeOfType<RunIfChangedSeedStrategy>();
            seeds[0].Strategy.Name.Should().Be("RunIfChanged");
        }
    }

    [Test]
    public async Task GetSeedsAsync_ShouldPreserveShortFilenameAsName()
    {
        // Given
        var runOnceDir = Path.Combine(_testDirectory, "run-once");
        Directory.CreateDirectory(runOnceDir);
        File.WriteAllText(Path.Combine(runOnceDir, "S001_InsertDefaults.sql"), "INSERT INTO Config DEFAULT VALUES");

        var provider = CreateProvider();

        // When
        var seeds = (await provider.GetSeedsAsync()).ToList();

        // Then
        seeds[0].Name.Should().Be("S001_InsertDefaults");
    }

    [Test]
    public async Task GetSeedsAsync_WhenDirectoryDoesNotExist_ShouldReturnEmptyCollection()
    {
        // Given
        var nonExistentPath = Path.Combine(_testDirectory, "does-not-exist");
        var provider = new FileSystemSeedProvider(
            nonExistentPath,
            new ISeedStrategyResolver[] { _resolver });

        // When
        var seeds = (await provider.GetSeedsAsync()).ToList();

        // Then
        seeds.Should().BeEmpty();
    }

    [Test]
    public async Task GetSeedsAsync_WhenMultipleFolders_ShouldDiscoverAllWithCorrectStrategies()
    {
        // Given
        var runOnceDir = Path.Combine(_testDirectory, "run-once");
        var runAlwaysDir = Path.Combine(_testDirectory, "run-always");
        var runIfChangedDir = Path.Combine(_testDirectory, "run-if-changed");

        Directory.CreateDirectory(runOnceDir);
        Directory.CreateDirectory(runAlwaysDir);
        Directory.CreateDirectory(runIfChangedDir);

        File.WriteAllText(Path.Combine(runOnceDir, "S001_Init.sql"), "SELECT 1");
        File.WriteAllText(Path.Combine(runAlwaysDir, "S002_Refresh.sql"), "SELECT 2");
        File.WriteAllText(Path.Combine(runIfChangedDir, "S003_Lookups.sql"), "SELECT 3");

        var provider = CreateProvider();

        // When
        var seeds = (await provider.GetSeedsAsync()).ToList();

        // Then
        using (new AssertionScope())
        {
            seeds.Should().HaveCount(3);
            seeds.Should().Contain(s => s.Strategy is RunAlwaysSeedStrategy);
            seeds.Should().Contain(s => s.Strategy is RunIfChangedSeedStrategy);
            seeds.Should().Contain(s => s.Strategy is RunOnceSeedStrategy);
        }
    }

    [Test]
    public async Task GetSeedsAsync_WhenNoResolverMatches_ShouldUseFallbackStrategy()
    {
        // Given — script in a folder that doesn't match any strategy resolver
        var otherDir = Path.Combine(_testDirectory, "other");
        Directory.CreateDirectory(otherDir);
        File.WriteAllText(Path.Combine(otherDir, "S001_Something.sql"), "SELECT 1");

        var provider = CreateProvider();

        // When
        var seeds = (await provider.GetSeedsAsync()).ToList();

        // Then — fallback is RunOnce by default
        using (new AssertionScope())
        {
            seeds.Should().HaveCount(1);
            seeds[0].Strategy.Should().BeOfType<RunOnceSeedStrategy>();
        }
    }

    private FileSystemSeedProvider CreateProvider()
    {
        return new FileSystemSeedProvider(
            _testDirectory,
            new ISeedStrategyResolver[] { _resolver });
    }
}
