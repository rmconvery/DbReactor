using DbReactor.Core.Abstractions;
using DbReactor.Core.Configuration;
using DbReactor.Core.Constants;
using DbReactor.Core.Execution;
using DbReactor.Core.Models;
using DbReactor.Core.Services;
using DbReactor.Core.Seeding.Strategies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DbReactor.Core.Tests.Seeding;

[TestFixture]
public class SeedOrchestratorOrderingTests
{
    private Mock<IConnectionManager> _mockConnectionManager;
    private Mock<IScriptExecutor> _mockScriptExecutor;
    private Mock<ISeedJournal> _mockSeedJournal;
    private DbReactorConfiguration _configuration;
    private List<string> _executionOrder;

    [SetUp]
    public void SetUp()
    {
        _mockConnectionManager = new Mock<IConnectionManager>();
        _mockScriptExecutor = new Mock<IScriptExecutor>();
        _mockSeedJournal = new Mock<ISeedJournal>();
        _executionOrder = new List<string>();

        _configuration = new DbReactorConfiguration
        {
            ConnectionManager = _mockConnectionManager.Object,
            ScriptExecutor = _mockScriptExecutor.Object,
            EnableVariables = false,
            Variables = new Dictionary<string, string>()
        };

        _mockSeedJournal.Setup(j => j.EnsureTableExistsAsync(It.IsAny<IConnectionManager>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockSeedJournal.Setup(j => j.RecordExecutionAsync(It.IsAny<ISeed>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Track execution order via the script executor
        _mockScriptExecutor.Setup(e => e.ExecuteAsync(It.IsAny<IScript>(), It.IsAny<IConnectionManager>(), It.IsAny<CancellationToken>()))
            .Callback<IScript, IConnectionManager, CancellationToken>((script, _, _) => _executionOrder.Add(script.Name))
            .ReturnsAsync(new MigrationResult { Successful = true });
    }

    [Test]
    public async Task ExecuteSeedsAsync_WhenMixedStrategies_ShouldExecuteInPriorityOrder()
    {
        // Given
        var seeds = new List<ISeed>
        {
            CreateSeed("RunAlways_Seed1", new RunAlwaysSeedStrategy()),
            CreateSeed("RunOnce_Seed1", new RunOnceSeedStrategy()),
            CreateSeed("RunIfChanged_Seed1", new RunIfChangedSeedStrategy()),
        };

        SetupAllSeedsShouldExecute(seeds);
        var orchestrator = CreateOrchestrator(seeds);

        // When
        var result = await orchestrator.ExecuteSeedsAsync();

        // Then
        using (new AssertionScope())
        {
            result.Successful.Should().BeTrue();
            _executionOrder.Should().HaveCount(3);
            _executionOrder[0].Should().Be("RunOnce_Seed1");
            _executionOrder[1].Should().Be("RunIfChanged_Seed1");
            _executionOrder[2].Should().Be("RunAlways_Seed1");
        }
    }

    [Test]
    public async Task ExecuteSeedsAsync_WhenAllRunOnceBeforeAnyRunAlways_ShouldMaintainOrder()
    {
        // Given
        var seeds = new List<ISeed>
        {
            CreateSeed("RunAlways_A", new RunAlwaysSeedStrategy()),
            CreateSeed("RunAlways_B", new RunAlwaysSeedStrategy()),
            CreateSeed("RunOnce_A", new RunOnceSeedStrategy()),
            CreateSeed("RunOnce_B", new RunOnceSeedStrategy()),
        };

        SetupAllSeedsShouldExecute(seeds);
        var orchestrator = CreateOrchestrator(seeds);

        // When
        var result = await orchestrator.ExecuteSeedsAsync();

        // Then
        using (new AssertionScope())
        {
            result.Successful.Should().BeTrue();
            _executionOrder.Should().HaveCount(4);

            // All RunOnce seeds must execute before any RunAlways seed
            var runOnceIndices = _executionOrder.Select((name, idx) => new { name, idx })
                .Where(x => x.name.StartsWith("RunOnce")).Select(x => x.idx);
            var runAlwaysIndices = _executionOrder.Select((name, idx) => new { name, idx })
                .Where(x => x.name.StartsWith("RunAlways")).Select(x => x.idx);

            runOnceIndices.Max().Should().BeLessThan(runAlwaysIndices.Min());
        }
    }

    [Test]
    public async Task ExecuteSeedsAsync_WhenSameStrategy_ShouldPreserveDiscoveryOrder()
    {
        // Given — seeds discovered in A, B, C order
        var seeds = new List<ISeed>
        {
            CreateSeed("Seed_A", new RunAlwaysSeedStrategy()),
            CreateSeed("Seed_B", new RunAlwaysSeedStrategy()),
            CreateSeed("Seed_C", new RunAlwaysSeedStrategy()),
        };

        SetupAllSeedsShouldExecute(seeds);
        var orchestrator = CreateOrchestrator(seeds);

        // When
        var result = await orchestrator.ExecuteSeedsAsync();

        // Then — stable sort preserves discovery order within same priority
        using (new AssertionScope())
        {
            result.Successful.Should().BeTrue();
            _executionOrder.Should().ContainInOrder("Seed_A", "Seed_B", "Seed_C");
        }
    }

    [Test]
    public async Task ExecuteSeedsAsync_WhenSingleStrategyType_ShouldExecuteCorrectly()
    {
        // Given
        var seeds = new List<ISeed>
        {
            CreateSeed("OnlySeed1", new RunOnceSeedStrategy()),
            CreateSeed("OnlySeed2", new RunOnceSeedStrategy()),
        };

        SetupAllSeedsShouldExecute(seeds);
        var orchestrator = CreateOrchestrator(seeds);

        // When
        var result = await orchestrator.ExecuteSeedsAsync();

        // Then
        using (new AssertionScope())
        {
            result.Successful.Should().BeTrue();
            _executionOrder.Should().HaveCount(2);
            _executionOrder.Should().ContainInOrder("OnlySeed1", "OnlySeed2");
        }
    }

    [Test]
    public async Task ExecuteSeedsAsync_WhenNoSeeds_ShouldReturnSuccessWithoutError()
    {
        // Given — empty seed list
        var seeds = new List<ISeed>();
        var orchestrator = CreateOrchestrator(seeds);

        // When
        var result = await orchestrator.ExecuteSeedsAsync();

        // Then
        using (new AssertionScope())
        {
            result.Successful.Should().BeTrue();
            result.Scripts.Should().BeEmpty();
        }
    }

    [Test]
    public async Task ExecuteSeedsAsync_WhenMixedWithStableSort_ShouldPreserveDiscoveryWithinSamePriority()
    {
        // Given — interleaved strategies with multiple seeds each
        var seeds = new List<ISeed>
        {
            CreateSeed("RunAlways_First", new RunAlwaysSeedStrategy()),
            CreateSeed("RunOnce_First", new RunOnceSeedStrategy()),
            CreateSeed("RunIfChanged_First", new RunIfChangedSeedStrategy()),
            CreateSeed("RunOnce_Second", new RunOnceSeedStrategy()),
            CreateSeed("RunAlways_Second", new RunAlwaysSeedStrategy()),
            CreateSeed("RunIfChanged_Second", new RunIfChangedSeedStrategy()),
        };

        SetupAllSeedsShouldExecute(seeds);
        var orchestrator = CreateOrchestrator(seeds);

        // When
        var result = await orchestrator.ExecuteSeedsAsync();

        // Then
        using (new AssertionScope())
        {
            result.Successful.Should().BeTrue();
            _executionOrder.Should().HaveCount(6);

            // RunOnce seeds first, in discovery order
            _executionOrder[0].Should().Be("RunOnce_First");
            _executionOrder[1].Should().Be("RunOnce_Second");

            // RunIfChanged seeds second, in discovery order
            _executionOrder[2].Should().Be("RunIfChanged_First");
            _executionOrder[3].Should().Be("RunIfChanged_Second");

            // RunAlways seeds last, in discovery order
            _executionOrder[4].Should().Be("RunAlways_First");
            _executionOrder[5].Should().Be("RunAlways_Second");
        }
    }

    private ISeed CreateSeed(string name, ISeedExecutionStrategy strategy)
    {
        var mockScript = new Mock<IScript>();
        mockScript.Setup(s => s.Name).Returns(name);
        mockScript.Setup(s => s.Script).Returns($"-- {name}");
        mockScript.Setup(s => s.Hash).Returns($"hash_{name}");

        return new Seed(name, mockScript.Object, strategy, $"hash_{name}");
    }

    private void SetupAllSeedsShouldExecute(IEnumerable<ISeed> seeds)
    {
        foreach (var seed in seeds)
        {
            // RunAlways always returns true; for RunOnce/RunIfChanged, mock the journal
            _mockSeedJournal.Setup(j => j.HasBeenExecutedAsync(It.Is<ISeed>(s => s.Name == seed.Name), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _mockSeedJournal.Setup(j => j.GetLastExecutedHashAsync(seed.Name, It.IsAny<CancellationToken>()))
                .ReturnsAsync((string?)null);
        }
    }

    private SeedOrchestrator CreateOrchestrator(IEnumerable<ISeed> seeds)
    {
        var mockSeedProvider = new Mock<ISeedScriptProvider>();
        mockSeedProvider.Setup(p => p.GetSeedsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(seeds);

        var discoveryService = new SeedDiscoveryService(
            seedProviders: new[] { mockSeedProvider.Object });

        var variableService = new VariableSubstitutionService();

        return new SeedOrchestrator(
            _configuration,
            discoveryService,
            _mockSeedJournal.Object,
            _mockScriptExecutor.Object,
            variableService);
    }
}
