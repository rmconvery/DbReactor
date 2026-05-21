using DbReactor.Core.Constants;

namespace DbReactor.Core.Tests.Seeding;

[TestFixture]
public class SeedStrategyPriorityTests
{
    [Test]
    public void GetPriority_WhenStrategyIsRunOnce_ShouldReturn1()
    {
        // When
        var priority = SeedStrategyPriority.GetPriority(DbReactorConstants.SeedStrategies.RunOnce);

        // Then
        priority.Should().Be(1);
    }

    [Test]
    public void GetPriority_WhenStrategyIsRunIfChanged_ShouldReturn2()
    {
        // When
        var priority = SeedStrategyPriority.GetPriority(DbReactorConstants.SeedStrategies.RunIfChanged);

        // Then
        priority.Should().Be(2);
    }

    [Test]
    public void GetPriority_WhenStrategyIsRunAlways_ShouldReturn3()
    {
        // When
        var priority = SeedStrategyPriority.GetPriority(DbReactorConstants.SeedStrategies.RunAlways);

        // Then
        priority.Should().Be(3);
    }

    [Test]
    public void GetPriority_WhenStrategyIsUnknown_ShouldReturn99()
    {
        // When
        var priority = SeedStrategyPriority.GetPriority("CustomStrategy");

        // Then
        priority.Should().Be(99);
    }

    [Test]
    public void GetPriority_WhenStrategyIsEmpty_ShouldReturn99()
    {
        // When
        var priority = SeedStrategyPriority.GetPriority("");

        // Then
        priority.Should().Be(99);
    }

    [TestCase("runonce")]
    [TestCase("RUNONCE")]
    [TestCase("runOnce")]
    public void GetPriority_WhenStrategyHasDifferentCasing_ShouldReturn99(string strategyName)
    {
        // Strategy matching is case-sensitive (uses exact string comparison)
        // When
        var priority = SeedStrategyPriority.GetPriority(strategyName);

        // Then
        priority.Should().Be(99);
    }

    [Test]
    public void GetPriority_RunOncePriorityShouldBeLessThanRunIfChanged()
    {
        // When
        var runOncePriority = SeedStrategyPriority.GetPriority(DbReactorConstants.SeedStrategies.RunOnce);
        var runIfChangedPriority = SeedStrategyPriority.GetPriority(DbReactorConstants.SeedStrategies.RunIfChanged);

        // Then
        runOncePriority.Should().BeLessThan(runIfChangedPriority);
    }

    [Test]
    public void GetPriority_RunIfChangedPriorityShouldBeLessThanRunAlways()
    {
        // When
        var runIfChangedPriority = SeedStrategyPriority.GetPriority(DbReactorConstants.SeedStrategies.RunIfChanged);
        var runAlwaysPriority = SeedStrategyPriority.GetPriority(DbReactorConstants.SeedStrategies.RunAlways);

        // Then
        runIfChangedPriority.Should().BeLessThan(runAlwaysPriority);
    }

    [Test]
    public void GetPriority_AllKnownStrategiesShouldBeLessThanDefault()
    {
        // When
        var runOncePriority = SeedStrategyPriority.GetPriority(DbReactorConstants.SeedStrategies.RunOnce);
        var runIfChangedPriority = SeedStrategyPriority.GetPriority(DbReactorConstants.SeedStrategies.RunIfChanged);
        var runAlwaysPriority = SeedStrategyPriority.GetPriority(DbReactorConstants.SeedStrategies.RunAlways);
        var defaultPriority = SeedStrategyPriority.GetPriority("Unknown");

        // Then
        using (new AssertionScope())
        {
            runOncePriority.Should().BeLessThan(defaultPriority);
            runIfChangedPriority.Should().BeLessThan(defaultPriority);
            runAlwaysPriority.Should().BeLessThan(defaultPriority);
        }
    }

    [Test]
    public void Constants_ShouldHaveExpectedValues()
    {
        // Then
        using (new AssertionScope())
        {
            SeedStrategyPriority.RunOnce.Should().Be(1);
            SeedStrategyPriority.RunIfChanged.Should().Be(2);
            SeedStrategyPriority.RunAlways.Should().Be(3);
            SeedStrategyPriority.Default.Should().Be(99);
        }
    }
}
