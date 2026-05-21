using DbReactor.Core.Abstractions;
using DbReactor.Core.Discovery;
using DbReactor.Core.Seeding.Resolvers;
using DbReactor.Core.Seeding.Strategies;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DbReactor.Core.Tests.Seeding;

[TestFixture]
public class EmbeddedSeedProviderTests
{
    private FolderStructureSeedStrategyResolver _resolver;
    private Mock<Assembly> _mockAssembly;

    [SetUp]
    public void SetUp()
    {
        _resolver = new FolderStructureSeedStrategyResolver();
        _mockAssembly = new Mock<Assembly>();
    }

    [Test]
    public async Task GetSeedsAsync_WhenResourceContainsRunAlways_ShouldResolveRunAlwaysStrategy()
    {
        // Given
        var resourceName = "MyApp.Seeds.run-always.S001_Refresh.sql";
        SetupEmbeddedResource(resourceName, "SELECT 1");

        var provider = CreateProvider("MyApp.Seeds");

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
    public async Task GetSeedsAsync_WhenResourceContainsRunOnce_ShouldResolveRunOnceStrategy()
    {
        // Given
        var resourceName = "MyApp.Seeds.run-once.S001_Init.sql";
        SetupEmbeddedResource(resourceName, "CREATE TABLE Test (Id INT)");

        var provider = CreateProvider("MyApp.Seeds");

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
    public async Task GetSeedsAsync_WhenResourceContainsRunIfChanged_ShouldResolveRunIfChangedStrategy()
    {
        // Given
        var resourceName = "MyApp.Seeds.run-if-changed.S001_Lookups.sql";
        SetupEmbeddedResource(resourceName, "INSERT INTO Lookups VALUES (1)");

        var provider = CreateProvider("MyApp.Seeds");

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
    public async Task GetSeedsAsync_ShouldPreserveFullResourceNameAsSeedName()
    {
        // Given
        var resourceName = "MyApp.Seeds.run-once.S001_Init.sql";
        SetupEmbeddedResource(resourceName, "SELECT 1");

        var provider = CreateProvider("MyApp.Seeds");

        // When
        var seeds = (await provider.GetSeedsAsync()).ToList();

        // Then
        seeds[0].Name.Should().Be(resourceName);
    }

    [Test]
    public async Task GetSeedsAsync_WhenNoResourcesMatchNamespace_ShouldReturnEmpty()
    {
        // Given
        _mockAssembly.Setup(a => a.GetManifestResourceNames())
            .Returns(new[] { "OtherNamespace.Scripts.S001.sql" });

        var provider = CreateProvider("MyApp.Seeds");

        // When
        var seeds = (await provider.GetSeedsAsync()).ToList();

        // Then
        seeds.Should().BeEmpty();
    }

    [Test]
    public async Task GetSeedsAsync_WhenNoResolverMatches_ShouldUseFallbackStrategy()
    {
        // Given — resource name without strategy folder marker
        var resourceName = "MyApp.Seeds.S001_Plain.sql";
        SetupEmbeddedResource(resourceName, "SELECT 1");

        var provider = CreateProvider("MyApp.Seeds");

        // When
        var seeds = (await provider.GetSeedsAsync()).ToList();

        // Then — fallback is RunOnce by default
        using (new AssertionScope())
        {
            seeds.Should().HaveCount(1);
            seeds[0].Strategy.Should().BeOfType<RunOnceSeedStrategy>();
        }
    }

    [Test]
    public async Task GetSeedsAsync_WhenMultipleResources_ShouldDiscoverAllWithCorrectStrategies()
    {
        // Given
        var resources = new[]
        {
            "MyApp.Seeds.run-once.S001_Init.sql",
            "MyApp.Seeds.run-always.S002_Refresh.sql",
            "MyApp.Seeds.run-if-changed.S003_Lookups.sql",
        };

        foreach (var resource in resources)
        {
            SetupEmbeddedResource(resource, $"-- {resource}");
        }

        // Re-setup GetManifestResourceNames to return all resources
        _mockAssembly.Setup(a => a.GetManifestResourceNames()).Returns(resources);

        var provider = CreateProvider("MyApp.Seeds");

        // When
        var seeds = (await provider.GetSeedsAsync()).ToList();

        // Then
        using (new AssertionScope())
        {
            seeds.Should().HaveCount(3);
            seeds.Should().Contain(s => s.Strategy is RunOnceSeedStrategy);
            seeds.Should().Contain(s => s.Strategy is RunAlwaysSeedStrategy);
            seeds.Should().Contain(s => s.Strategy is RunIfChangedSeedStrategy);
        }
    }

    private void SetupEmbeddedResource(string resourceName, string content)
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        _mockAssembly.Setup(a => a.GetManifestResourceNames())
            .Returns(new[] { resourceName });
        _mockAssembly.Setup(a => a.GetManifestResourceStream(resourceName))
            .Returns(stream);
    }

    private EmbeddedSeedProvider CreateProvider(string resourceNamespace)
    {
        return new EmbeddedSeedProvider(
            _mockAssembly.Object,
            resourceNamespace,
            new ISeedStrategyResolver[] { _resolver });
    }
}
