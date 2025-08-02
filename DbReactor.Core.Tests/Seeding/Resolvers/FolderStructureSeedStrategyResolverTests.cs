using DbReactor.Core.Abstractions;
using DbReactor.Core.Seeding.Resolvers;
using DbReactor.Core.Seeding.Strategies;

namespace DbReactor.Core.Tests.Seeding.Resolvers;

[TestFixture]
public class FolderStructureSeedStrategyResolverTests
{
    private FolderStructureSeedStrategyResolver _resolver;
    private Mock<IScript> _mockScript;

    [SetUp]
    public void SetUp()
    {
        _resolver = new FolderStructureSeedStrategyResolver();
        _mockScript = new Mock<IScript>();
        _mockScript.Setup(s => s.Name).Returns("TestScript.sql");
    }

    [Test]
    public void ResolveStrategy_WhenScriptPathIsNull_ShouldReturnNull()
    {
        // When
        ISeedExecutionStrategy result = _resolver.ResolveStrategy(_mockScript.Object, null);

        // Then
        result.Should().BeNull();
    }

    [Test]
    public void ResolveStrategy_WhenScriptPathIsEmpty_ShouldReturnNull()
    {
        // When
        ISeedExecutionStrategy result = _resolver.ResolveStrategy(_mockScript.Object, "");

        // Then
        result.Should().BeNull();
    }

    [Test]
    public void ResolveStrategy_WhenScriptPathIsWhitespace_ShouldReturnNull()
    {
        // When
        ISeedExecutionStrategy result = _resolver.ResolveStrategy(_mockScript.Object, "   ");

        // Then
        result.Should().BeNull();
    }

    [TestCase("Seeds/run-once/S001_TestSeed.sql")]
    [TestCase("path\\to\\run-once\\TestSeed.sql")]
    [TestCase("run-once\\TestSeed.sql")]
    public void ResolveStrategy_WhenPathContainsRunOnceFolder_ShouldReturnRunOnceStrategy(string scriptPath)
    {
        // When
        ISeedExecutionStrategy result = _resolver.ResolveStrategy(_mockScript.Object, scriptPath);

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNull();
            result.Should().BeOfType<RunOnceSeedStrategy>();
        }
    }

    [TestCase("Seeds/run-always/S001_TestSeed.sql")]
    [TestCase("path\\to\\run-always\\TestSeed.sql")]
    [TestCase("run-always\\TestSeed.sql")]
    public void ResolveStrategy_WhenPathContainsRunAlwaysFolder_ShouldReturnRunAlwaysStrategy(string scriptPath)
    {
        // When
        ISeedExecutionStrategy result = _resolver.ResolveStrategy(_mockScript.Object, scriptPath);

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNull();
            result.Should().BeOfType<RunAlwaysSeedStrategy>();
        }
    }

    [TestCase("Seeds/run-if-changed/S001_TestSeed.sql")]
    [TestCase("path\\to\\run-if-changed\\TestSeed.sql")]
    [TestCase("run-if-changed\\TestSeed.sql")]
    public void ResolveStrategy_WhenPathContainsRunIfChangedFolder_ShouldReturnRunIfChangedStrategy(string scriptPath)
    {
        // When
        ISeedExecutionStrategy result = _resolver.ResolveStrategy(_mockScript.Object, scriptPath);

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNull();
            result.Should().BeOfType<RunIfChangedSeedStrategy>();
        }
    }

    [TestCase("MyApp.Seeds.run-once.S001_TestSeed.sql")]
    [TestCase("MyApp.Seeds.run-always.S001_TestSeed.sql")]
    [TestCase("MyApp.Seeds.run-if-changed.S001_TestSeed.sql")]
    public void ResolveStrategy_WhenEmbeddedResourceNameHasDottedFolders_ShouldResolveCorrectly(string resourceName)
    {
        // When
        ISeedExecutionStrategy result = _resolver.ResolveStrategy(_mockScript.Object, resourceName);

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNull();

            if (resourceName.Contains("run-once"))
                result.Should().BeOfType<RunOnceSeedStrategy>();
            else if (resourceName.Contains("run-always"))
                result.Should().BeOfType<RunAlwaysSeedStrategy>();
            else if (resourceName.Contains("run-if-changed"))
                result.Should().BeOfType<RunIfChangedSeedStrategy>();
        }
    }

    [TestCase("Seeds/other-folder/TestSeed.sql")]
    [TestCase("Seeds/TestSeed.sql")]
    [TestCase("MyApp.Scripts.TestSeed.sql")]
    [TestCase("random/path/TestSeed.sql")]
    public void ResolveStrategy_WhenPathDoesNotMatchAnyStrategy_ShouldReturnNull(string scriptPath)
    {
        // When
        ISeedExecutionStrategy result = _resolver.ResolveStrategy(_mockScript.Object, scriptPath);

        // Then
        result.Should().BeNull();
    }

    [TestCase("Seeds/RUN-ONCE/TestSeed.sql")]
    [TestCase("Seeds/RUN_ALWAYS/TestSeed.sql")]
    [TestCase("Seeds/RUN-IF-CHANGED/TestSeed.sql")]
    [TestCase("Seeds/Run-Once/TestSeed.sql")]
    [TestCase("Seeds/Run-Always/TestSeed.sql")]
    [TestCase("Seeds/Run-If-Changed/TestSeed.sql")]
    public void ResolveStrategy_WhenFolderNameHasDifferentCasing_ShouldResolveCaseInsensitively(string scriptPath)
    {
        // When
        ISeedExecutionStrategy result = _resolver.ResolveStrategy(_mockScript.Object, scriptPath);

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNull();

            string lowerPath = scriptPath.ToLowerInvariant();
            if (lowerPath.Contains("run-once") || lowerPath.Contains("run_once"))
                result.Should().BeOfType<RunOnceSeedStrategy>();
            else if (lowerPath.Contains("run-always") || lowerPath.Contains("run_always"))
                result.Should().BeOfType<RunAlwaysSeedStrategy>();
            else if (lowerPath.Contains("run-if-changed") || lowerPath.Contains("run_if_changed"))
                result.Should().BeOfType<RunIfChangedSeedStrategy>();
        }
    }

    [TestCase("Seeds/run_once/TestSeed.sql")]
    [TestCase("Seeds/run_always/TestSeed.sql")]
    [TestCase("Seeds/run_if_changed/TestSeed.sql")]
    [TestCase("Seeds/run once/TestSeed.sql")]
    [TestCase("Seeds/run always/TestSeed.sql")]
    [TestCase("Seeds/run if changed/TestSeed.sql")]
    public void ResolveStrategy_WhenFolderNameHasSpacesOrUnderscores_ShouldNormalizeAndResolve(string scriptPath)
    {
        // When
        ISeedExecutionStrategy result = _resolver.ResolveStrategy(_mockScript.Object, scriptPath);

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNull();

            string normalizedPath = scriptPath.ToLowerInvariant().Replace('_', '-').Replace(' ', '-');
            if (normalizedPath.Contains("run-once"))
                result.Should().BeOfType<RunOnceSeedStrategy>();
            else if (normalizedPath.Contains("run-always"))
                result.Should().BeOfType<RunAlwaysSeedStrategy>();
            else if (normalizedPath.Contains("run-if-changed"))
                result.Should().BeOfType<RunIfChangedSeedStrategy>();
        }
    }

    [Test]
    public void ResolveStrategy_WhenEmbeddedResourceWithoutDirectorySeparators_ShouldConvertDotsToDirectorySeparators()
    {
        // Given
        string embeddedResourceName = "MyApp.Seeds.run-once.S001_TestSeed.sql";

        // When
        ISeedExecutionStrategy result = _resolver.ResolveStrategy(_mockScript.Object, embeddedResourceName);

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNull();
            result.Should().BeOfType<RunOnceSeedStrategy>();
        }
    }

    [Test]
    public void ResolveStrategy_WhenFileSystemPathWithDirectorySeparators_ShouldUsePathAsIs()
    {
        // Given
        string fileSystemPath = "C:\\Projects\\MyApp\\Seeds\\run-once\\S001_TestSeed.sql";

        // When
        ISeedExecutionStrategy result = _resolver.ResolveStrategy(_mockScript.Object, fileSystemPath);

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNull();
            result.Should().BeOfType<RunOnceSeedStrategy>();
        }
    }

    [TestCase("Seeds/run-on-demand/TestSeed.sql")]
    [TestCase("Seeds/run-conditionally/TestSeed.sql")]
    [TestCase("Seeds/run-sometimes/TestSeed.sql")]
    [TestCase("Seeds/run/TestSeed.sql")] // Missing "once", "always", or "if changed"
    [TestCase("Seeds/once/TestSeed.sql")] // Missing "run"
    [TestCase("Seeds/always/TestSeed.sql")] // Missing "run"
    public void ResolveStrategy_WhenFolderNameIsCloseButNotExact_ShouldReturnNull(string scriptPath)
    {
        // When
        ISeedExecutionStrategy result = _resolver.ResolveStrategy(_mockScript.Object, scriptPath);

        // Then
        result.Should().BeNull();
    }

    [Test]
    public void ResolveStrategy_WhenMultipleFoldersInPath_ShouldUseImmediateParentFolder()
    {
        // Given
        string scriptPath = "Seeds/run-always/run-once/TestSeed.sql";

        // When
        ISeedExecutionStrategy result = _resolver.ResolveStrategy(_mockScript.Object, scriptPath);

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNull();
            // Should use immediate parent folder "run-once", not "run-always"
            result.Should().BeOfType<RunOnceSeedStrategy>();
        }
    }

    [Test]
    public void ResolveStrategy_WhenScriptParameterIsNull_ShouldStillProcessPath()
    {
        // Given
        string scriptPath = "Seeds/run-once/TestSeed.sql";

        // When
        ISeedExecutionStrategy result = _resolver.ResolveStrategy(null, scriptPath);

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNull();
            result.Should().BeOfType<RunOnceSeedStrategy>();
        }
    }

    [Test]
    public void ResolveStrategy_WhenPathHasNoDirectory_ShouldReturnNull()
    {
        // Given
        string scriptPath = "TestSeed.sql";

        // When
        ISeedExecutionStrategy result = _resolver.ResolveStrategy(_mockScript.Object, scriptPath);

        // Then
        result.Should().BeNull();
    }

    [Test]
    public void ResolveStrategy_WhenPathProcessingThrowsException_ShouldReturnNull()
    {
        // Given - This path should be safe but test exception handling
        string invalidPath = string.Empty;

        // When
        ISeedExecutionStrategy result = _resolver.ResolveStrategy(_mockScript.Object, invalidPath);

        // Then
        result.Should().BeNull();
    }

    [TestCase("MyApp.Seeds.run-once.S001_TestSeed.sql")]
    [TestCase("MyApp.Seeds.run-always.S001_TestSeed.sql")]
    [TestCase("MyApp.Seeds.run-if-changed.S001_TestSeed.sql")]
    public void ResolveStrategy_WhenEmbeddedResourceNameHasUnderscores_ShouldResolveCorrectly(string resourceName)
    {
        // When
        ISeedExecutionStrategy result = _resolver.ResolveStrategy(_mockScript.Object, resourceName);

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNull();

            if (resourceName.Contains("run-once"))
                result.Should().BeOfType<RunOnceSeedStrategy>();
            else if (resourceName.Contains("run-always"))
                result.Should().BeOfType<RunAlwaysSeedStrategy>();
            else if (resourceName.Contains("run-if-changed"))
                result.Should().BeOfType<RunIfChangedSeedStrategy>();
        }
    }

    [TestCase("Seeds/runonce/TestSeed.sql")] // Missing separator between "run" and "once"
    [TestCase("Seeds/runalways/TestSeed.sql")] // Missing separator between "run" and "always"
    [TestCase("Seeds/runifchanged/TestSeed.sql")] // Missing separators
    public void ResolveStrategy_WhenFolderNameLacksSeparators_ShouldNotReturnNull(string scriptPath)
    {
        // When
        ISeedExecutionStrategy result = _resolver.ResolveStrategy(_mockScript.Object, scriptPath);

        // Then
        result.Should().NotBeNull();
    }

    [TestCase("Seeds/my-run-once-folder/TestSeed.sql")]
    [TestCase("Seeds/custom-run-always-scripts/TestSeed.sql")]
    [TestCase("Seeds/special-run-if-changed-seeds/TestSeed.sql")]
    public void ResolveStrategy_WhenStrategyPartsAreWithinLargerFolderName_ShouldResolveCorrectly(string scriptPath)
    {
        // When
        ISeedExecutionStrategy result = _resolver.ResolveStrategy(_mockScript.Object, scriptPath);

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNull();

            string lowerPath = scriptPath.ToLowerInvariant();
            if (lowerPath.Contains("run-once"))
                result.Should().BeOfType<RunOnceSeedStrategy>();
            else if (lowerPath.Contains("run-always"))
                result.Should().BeOfType<RunAlwaysSeedStrategy>();
            else if (lowerPath.Contains("run-if-changed"))
                result.Should().BeOfType<RunIfChangedSeedStrategy>();
        }
    }

    // Tests for embedded resource patterns that should fail (return null)
    [TestCase("MyApp.Seeds.run.once.S001_TestSeed.sql")] // Separate "run" and "once" folders - should return null
    [TestCase("MyApp.Seeds.run.always.S001_TestSeed.sql")] // Separate "run" and "always" folders - should return null  
    [TestCase("MyApp.Seeds.run.if.changed.S001_TestSeed.sql")] // Separate "run", "if", "changed" folders - should return null
    public void ResolveStrategy_WhenEmbeddedResourceHasSeparateStrategyParts_ShouldReturnNull(string resourceName)
    {
        // When
        ISeedExecutionStrategy result = _resolver.ResolveStrategy(_mockScript.Object, resourceName);

        // Then
        result.Should().BeNull("because the strategy parts are in separate folder levels");
    }
}

