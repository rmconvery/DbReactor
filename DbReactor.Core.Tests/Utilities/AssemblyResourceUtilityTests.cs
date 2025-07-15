using DbReactor.Core.Utilities;
using FluentAssertions;
using FluentAssertions.Execution;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DbReactor.Core.Tests.Utilities;

[TestFixture]
public class AssemblyResourceUtilityTests
{
    private Assembly _testAssembly;

    [SetUp]
    public void SetUp()
    {
        _testAssembly = Assembly.GetExecutingAssembly();
    }

    [Test]
    public void DiscoverBaseNamespace_WithNullAssembly_ShouldThrowArgumentNullException()
    {
        // Given
        Assembly nullAssembly = null!;

        // When
        Action act = () => AssemblyResourceUtility.DiscoverBaseNamespace(nullAssembly);

        // Then
        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("assembly");
        }
    }

    [Test]
    public void DiscoverBaseNamespace_WithValidAssembly_ShouldReturnAssemblyName()
    {
        // Given
        var assembly = _testAssembly;

        // When
        var result = AssemblyResourceUtility.DiscoverBaseNamespace(assembly);

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNullOrEmpty();
            result.Should().Be(assembly.GetName().Name);
        }
    }

    [Test]
    public void DiscoverBaseNamespace_WithCustomExtension_ShouldFilterCorrectly()
    {
        // Given
        var assembly = _testAssembly;
        var customExtension = ".custom";

        // When
        var result = AssemblyResourceUtility.DiscoverBaseNamespace(assembly, customExtension);

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNullOrEmpty();
            result.Should().Be(assembly.GetName().Name);
        }
    }

    [Test]
    public void DiscoverBaseNamespace_WithKnownFolders_ShouldUseProvidedFolders()
    {
        // Given
        var assembly = _testAssembly;
        var knownFolders = new[] { "custom", "folders" };

        // When
        var result = AssemblyResourceUtility.DiscoverBaseNamespace(assembly, ".sql", knownFolders);

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNullOrEmpty();
            result.Should().Be(assembly.GetName().Name);
        }
    }

    [Test]
    public void GetResourcesWithExtension_WithNullAssembly_ShouldThrowArgumentNullException()
    {
        // Given
        Assembly nullAssembly = null!;

        // When
        Action act = () => AssemblyResourceUtility.GetResourcesWithExtension(nullAssembly, ".sql");

        // Then
        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("assembly");
        }
    }

    [Test]
    public void GetResourcesWithExtension_WithValidAssembly_ShouldReturnArray()
    {
        // Given
        var assembly = _testAssembly;

        // When
        var result = AssemblyResourceUtility.GetResourcesWithExtension(assembly, ".sql");

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNull();
            result.Should().BeOfType<string[]>();
        }
    }

    [Test]
    public void GetResourcesWithExtension_WithNonExistentExtension_ShouldReturnEmptyArray()
    {
        // Given
        var assembly = _testAssembly;

        // When
        var result = AssemblyResourceUtility.GetResourcesWithExtension(assembly, ".nonexistent");

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }
    }

    [Test]
    public void ExtractCommonPrefixes_WithNullResourceNames_ShouldThrowArgumentNullException()
    {
        // Given
        string[] nullResourceNames = null!;

        // When
        Action act = () => AssemblyResourceUtility.ExtractCommonPrefixes(nullResourceNames, new[] { "upgrades" });

        // Then
        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("resourceNames");
        }
    }

    [Test]
    public void ExtractCommonPrefixes_WithValidResourceNames_ShouldReturnDictionary()
    {
        // Given
        var resourceNames = new[]
        {
            "MyApp.Scripts.upgrades.001_Migration.sql",
            "MyApp.Scripts.upgrades.002_Migration.sql",
            "MyApp.Scripts.downgrades.001_Migration.sql"
        };
        var knownFolders = new[] { "upgrades", "downgrades" };

        // When
        var result = AssemblyResourceUtility.ExtractCommonPrefixes(resourceNames, knownFolders);

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNull();
            result.Should().ContainKey("MyApp.Scripts");
            result["MyApp.Scripts"].Should().Be(3);
        }
    }

    [Test]
    public void ExtractCommonPrefixes_WithEmptyResourceNames_ShouldReturnEmptyDictionary()
    {
        // Given
        var resourceNames = new string[0];
        var knownFolders = new[] { "upgrades" };

        // When
        var result = AssemblyResourceUtility.ExtractCommonPrefixes(resourceNames, knownFolders);

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }
    }

    [Test]
    public void ExtractNamespacePrefix_WithNullResourceName_ShouldReturnNull()
    {
        // Given
        string nullResourceName = null!;

        // When
        var result = AssemblyResourceUtility.ExtractNamespacePrefix(nullResourceName, new[] { "upgrades" });

        // Then
        using (new AssertionScope())
        {
            result.Should().BeNull();
        }
    }

    [Test]
    public void ExtractNamespacePrefix_WithEmptyResourceName_ShouldReturnNull()
    {
        // Given
        var emptyResourceName = string.Empty;

        // When
        var result = AssemblyResourceUtility.ExtractNamespacePrefix(emptyResourceName, new[] { "upgrades" });

        // Then
        using (new AssertionScope())
        {
            result.Should().BeNull();
        }
    }

    [Test]
    public void ExtractNamespacePrefix_WithKnownFolder_ShouldReturnCorrectPrefix()
    {
        // Given
        var resourceName = "MyApp.Scripts.upgrades.001_Migration.sql";
        var knownFolders = new[] { "upgrades", "downgrades" };

        // When
        var result = AssemblyResourceUtility.ExtractNamespacePrefix(resourceName, knownFolders);

        // Then
        using (new AssertionScope())
        {
            result.Should().Be("MyApp.Scripts");
        }
    }

    [Test]
    public void ExtractNamespacePrefix_WithoutKnownFolder_ShouldUseFallbackLogic()
    {
        // Given
        var resourceName = "MyApp.Scripts.001_Migration.sql";
        var knownFolders = new[] { "upgrades", "downgrades" };

        // When
        var result = AssemblyResourceUtility.ExtractNamespacePrefix(resourceName, knownFolders);

        // Then
        using (new AssertionScope())
        {
            result.Should().Be("MyApp.Scripts");
        }
    }

    [Test]
    public void ExtractNamespacePrefix_WithShortResourceName_ShouldReturnNull()
    {
        // Given
        var resourceName = "short.sql";
        var knownFolders = new[] { "upgrades" };

        // When
        var result = AssemblyResourceUtility.ExtractNamespacePrefix(resourceName, knownFolders);

        // Then
        using (new AssertionScope())
        {
            result.Should().BeNull();
        }
    }

    [Test]
    public void HasResourcesWithExtension_WithNullAssembly_ShouldThrowArgumentNullException()
    {
        // Given
        Assembly nullAssembly = null!;

        // When
        Action act = () => AssemblyResourceUtility.HasResourcesWithExtension(nullAssembly, ".sql");

        // Then
        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("assembly");
        }
    }

    [Test]
    public void HasResourcesWithExtension_WithValidAssembly_ShouldNotThrow()
    {
        // Given
        var assembly = _testAssembly;

        // When
        Action act = () => AssemblyResourceUtility.HasResourcesWithExtension(assembly, ".sql");

        // Then
        using (new AssertionScope())
        {
            act.Should().NotThrow();
        }
    }

    [Test]
    public void GetUniqueNamespacePrefixes_WithNullAssembly_ShouldThrowArgumentNullException()
    {
        // Given
        Assembly nullAssembly = null!;

        // When
        Action act = () => AssemblyResourceUtility.GetUniqueNamespacePrefixes(nullAssembly);

        // Then
        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("assembly");
        }
    }

    [Test]
    public void GetUniqueNamespacePrefixes_WithValidAssembly_ShouldReturnOrderedArray()
    {
        // Given
        var assembly = _testAssembly;

        // When
        var result = AssemblyResourceUtility.GetUniqueNamespacePrefixes(assembly);

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNull();
            result.Should().BeOfType<string[]>();
            // Check that result is ordered
            result.Should().BeInAscendingOrder();
        }
    }

    [Test]
    public void GetUniqueNamespacePrefixes_WithCustomExtension_ShouldFilterCorrectly()
    {
        // Given
        var assembly = _testAssembly;
        var customExtension = ".custom";

        // When
        var result = AssemblyResourceUtility.GetUniqueNamespacePrefixes(assembly, customExtension);

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNull();
            result.Should().BeOfType<string[]>();
        }
    }
}