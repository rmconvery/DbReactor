using DbReactor.Core.Utilities;
using FluentAssertions;
using FluentAssertions.Execution;
using NUnit.Framework;
using System;
using System.IO;

namespace DbReactor.Core.Tests.Utilities;

[TestFixture]
public class PathUtilityTests
{
    [Test]
    public void NormalizeToNamespace_WithNullPath_ShouldReturnNull()
    {
        // Given
        string nullPath = null!;

        // When
        var result = PathUtility.NormalizeToNamespace(nullPath);

        // Then
        using (new AssertionScope())
        {
            result.Should().BeNull();
        }
    }

    [Test]
    public void NormalizeToNamespace_WithEmptyPath_ShouldReturnEmpty()
    {
        // Given
        var emptyPath = string.Empty;

        // When
        var result = PathUtility.NormalizeToNamespace(emptyPath);

        // Then
        using (new AssertionScope())
        {
            result.Should().BeEmpty();
        }
    }

    [Test]
    public void NormalizeToNamespace_WithBackslashes_ShouldReplaceToDots()
    {
        // Given
        var path = "folder\\subfolder\\file";

        // When
        var result = PathUtility.NormalizeToNamespace(path);

        // Then
        using (new AssertionScope())
        {
            result.Should().Be("folder.subfolder.file");
        }
    }

    [Test]
    public void NormalizeToNamespace_WithForwardSlashes_ShouldReplaceToDots()
    {
        // Given
        var path = "folder/subfolder/file";

        // When
        var result = PathUtility.NormalizeToNamespace(path);

        // Then
        using (new AssertionScope())
        {
            result.Should().Be("folder.subfolder.file");
        }
    }

    [Test]
    public void NormalizeToNamespace_WithMixedSeparators_ShouldReplaceToDots()
    {
        // Given
        var path = "folder\\subfolder/file";

        // When
        var result = PathUtility.NormalizeToNamespace(path);

        // Then
        using (new AssertionScope())
        {
            result.Should().Be("folder.subfolder.file");
        }
    }

    [Test]
    public void NormalizePathSeparators_WithNullPath_ShouldReturnNull()
    {
        // Given
        string nullPath = null!;

        // When
        var result = PathUtility.NormalizePathSeparators(nullPath);

        // Then
        using (new AssertionScope())
        {
            result.Should().BeNull();
        }
    }

    [Test]
    public void NormalizePathSeparators_WithEmptyPath_ShouldReturnEmpty()
    {
        // Given
        var emptyPath = string.Empty;

        // When
        var result = PathUtility.NormalizePathSeparators(emptyPath);

        // Then
        using (new AssertionScope())
        {
            result.Should().BeEmpty();
        }
    }

    [Test]
    public void NormalizePathSeparators_WithMixedSeparators_ShouldNormalizeToPlatform()
    {
        // Given
        var path = "folder\\subfolder/file";

        // When
        var result = PathUtility.NormalizePathSeparators(path);

        // Then
        using (new AssertionScope())
        {
            result.Should().Be($"folder{Path.DirectorySeparatorChar}subfolder{Path.DirectorySeparatorChar}file");
        }
    }

    [Test]
    public void IsValidNamespacePath_WithNullPath_ShouldReturnFalse()
    {
        // Given
        string nullPath = null!;

        // When
        var result = PathUtility.IsValidNamespacePath(nullPath);

        // Then
        using (new AssertionScope())
        {
            result.Should().BeFalse();
        }
    }

    [Test]
    public void IsValidNamespacePath_WithEmptyPath_ShouldReturnFalse()
    {
        // Given
        var emptyPath = string.Empty;

        // When
        var result = PathUtility.IsValidNamespacePath(emptyPath);

        // Then
        using (new AssertionScope())
        {
            result.Should().BeFalse();
        }
    }

    [Test]
    public void IsValidNamespacePath_WithValidPath_ShouldReturnTrue()
    {
        // Given
        var validPath = "MyApp.Scripts.Upgrades";

        // When
        var result = PathUtility.IsValidNamespacePath(validPath);

        // Then
        using (new AssertionScope())
        {
            result.Should().BeTrue();
        }
    }

    [Test]
    public void IsValidNamespacePath_WithInvalidCharacters_ShouldReturnFalse()
    {
        // Given
        var invalidPath = "MyApp<Scripts>Upgrades";

        // When
        var result = PathUtility.IsValidNamespacePath(invalidPath);

        // Then
        using (new AssertionScope())
        {
            result.Should().BeFalse();
        }
    }

    [Test]
    public void IsValidNamespacePath_WithSpaces_ShouldReturnFalse()
    {
        // Given
        var pathWithSpaces = "MyApp Scripts Upgrades";

        // When
        var result = PathUtility.IsValidNamespacePath(pathWithSpaces);

        // Then
        using (new AssertionScope())
        {
            result.Should().BeFalse();
        }
    }

    [Test]
    public void IsValidIdentifier_WithNullIdentifier_ShouldReturnFalse()
    {
        // Given
        string nullIdentifier = null!;

        // When
        var result = PathUtility.IsValidIdentifier(nullIdentifier);

        // Then
        using (new AssertionScope())
        {
            result.Should().BeFalse();
        }
    }

    [Test]
    public void IsValidIdentifier_WithEmptyIdentifier_ShouldReturnFalse()
    {
        // Given
        var emptyIdentifier = string.Empty;

        // When
        var result = PathUtility.IsValidIdentifier(emptyIdentifier);

        // Then
        using (new AssertionScope())
        {
            result.Should().BeFalse();
        }
    }

    [Test]
    public void IsValidIdentifier_WithValidIdentifier_ShouldReturnTrue()
    {
        // Given
        var validIdentifier = "MyValidIdentifier";

        // When
        var result = PathUtility.IsValidIdentifier(validIdentifier);

        // Then
        using (new AssertionScope())
        {
            result.Should().BeTrue();
        }
    }

    [Test]
    public void IsValidIdentifier_WithUnderscoreStart_ShouldReturnTrue()
    {
        // Given
        var identifierWithUnderscore = "_MyValidIdentifier";

        // When
        var result = PathUtility.IsValidIdentifier(identifierWithUnderscore);

        // Then
        using (new AssertionScope())
        {
            result.Should().BeTrue();
        }
    }

    [Test]
    public void IsValidIdentifier_WithDigitStart_ShouldReturnFalse()
    {
        // Given
        var identifierWithDigitStart = "123InvalidIdentifier";

        // When
        var result = PathUtility.IsValidIdentifier(identifierWithDigitStart);

        // Then
        using (new AssertionScope())
        {
            result.Should().BeFalse();
        }
    }

    [Test]
    public void IsValidIdentifier_WithDigitsInMiddle_ShouldReturnTrue()
    {
        // Given
        var identifierWithDigits = "Valid123Identifier";

        // When
        var result = PathUtility.IsValidIdentifier(identifierWithDigits);

        // Then
        using (new AssertionScope())
        {
            result.Should().BeTrue();
        }
    }

    [Test]
    public void CombineNamespace_WithNullParts_ShouldReturnEmpty()
    {
        // Given
        string[] nullParts = null!;

        // When
        var result = PathUtility.CombineNamespace(nullParts);

        // Then
        using (new AssertionScope())
        {
            result.Should().BeEmpty();
        }
    }

    [Test]
    public void CombineNamespace_WithEmptyParts_ShouldReturnEmpty()
    {
        // Given
        var emptyParts = new string[0];

        // When
        var result = PathUtility.CombineNamespace(emptyParts);

        // Then
        using (new AssertionScope())
        {
            result.Should().BeEmpty();
        }
    }

    [Test]
    public void CombineNamespace_WithValidParts_ShouldCombineWithDots()
    {
        // Given
        var parts = new[] { "MyApp", "Scripts", "Upgrades" };

        // When
        var result = PathUtility.CombineNamespace(parts);

        // Then
        using (new AssertionScope())
        {
            result.Should().Be("MyApp.Scripts.Upgrades");
        }
    }

    [Test]
    public void CombineNamespace_WithNullAndEmptyParts_ShouldSkipInvalidParts()
    {
        // Given
        var parts = new[] { "MyApp", null, "", "Scripts", "Upgrades" };

        // When
        var result = PathUtility.CombineNamespace(parts);

        // Then
        using (new AssertionScope())
        {
            result.Should().Be("MyApp.Scripts.Upgrades");
        }
    }

    [Test]
    public void GetFileNameWithoutExtension_WithNullResourceName_ShouldReturnEmpty()
    {
        // Given
        string nullResourceName = null!;

        // When
        var result = PathUtility.GetFileNameWithoutExtension(nullResourceName);

        // Then
        using (new AssertionScope())
        {
            result.Should().BeEmpty();
        }
    }

    [Test]
    public void GetFileNameWithoutExtension_WithEmptyResourceName_ShouldReturnEmpty()
    {
        // Given
        var emptyResourceName = string.Empty;

        // When
        var result = PathUtility.GetFileNameWithoutExtension(emptyResourceName);

        // Then
        using (new AssertionScope())
        {
            result.Should().BeEmpty();
        }
    }

    [Test]
    public void GetFileNameWithoutExtension_WithValidResourceName_ShouldReturnFileName()
    {
        // Given
        var resourceName = "MyApp.Scripts.001_Migration.sql";

        // When
        var result = PathUtility.GetFileNameWithoutExtension(resourceName);

        // Then
        using (new AssertionScope())
        {
            result.Should().Be("001_Migration");
        }
    }

    [Test]
    public void GetFileNameWithoutExtension_WithNoExtension_ShouldReturnOriginal()
    {
        // Given
        var resourceName = "NoExtension";

        // When
        var result = PathUtility.GetFileNameWithoutExtension(resourceName);

        // Then
        using (new AssertionScope())
        {
            result.Should().Be(resourceName);
        }
    }

    [Test]
    public void GetExtension_WithNullResourceName_ShouldReturnEmpty()
    {
        // Given
        string nullResourceName = null!;

        // When
        var result = PathUtility.GetExtension(nullResourceName);

        // Then
        using (new AssertionScope())
        {
            result.Should().BeEmpty();
        }
    }

    [Test]
    public void GetExtension_WithEmptyResourceName_ShouldReturnEmpty()
    {
        // Given
        var emptyResourceName = string.Empty;

        // When
        var result = PathUtility.GetExtension(emptyResourceName);

        // Then
        using (new AssertionScope())
        {
            result.Should().BeEmpty();
        }
    }

    [Test]
    public void GetExtension_WithValidResourceName_ShouldReturnExtension()
    {
        // Given
        var resourceName = "MyApp.Scripts.001_Migration.sql";

        // When
        var result = PathUtility.GetExtension(resourceName);

        // Then
        using (new AssertionScope())
        {
            result.Should().Be(".sql");
        }
    }

    [Test]
    public void GetExtension_WithNoExtension_ShouldReturnEmpty()
    {
        // Given
        var resourceName = "NoExtension";

        // When
        var result = PathUtility.GetExtension(resourceName);

        // Then
        using (new AssertionScope())
        {
            result.Should().BeEmpty();
        }
    }

    [Test]
    public void SanitizeForPath_WithNullInput_ShouldReturnEmpty()
    {
        // Given
        string nullInput = null!;

        // When
        var result = PathUtility.SanitizeForPath(nullInput);

        // Then
        using (new AssertionScope())
        {
            result.Should().BeEmpty();
        }
    }

    [Test]
    public void SanitizeForPath_WithEmptyInput_ShouldReturnEmpty()
    {
        // Given
        var emptyInput = string.Empty;

        // When
        var result = PathUtility.SanitizeForPath(emptyInput);

        // Then
        using (new AssertionScope())
        {
            result.Should().BeEmpty();
        }
    }

    [Test]
    public void SanitizeForPath_WithValidInput_ShouldReturnSameInput()
    {
        // Given
        var validInput = "ValidFileName";

        // When
        var result = PathUtility.SanitizeForPath(validInput);

        // Then
        using (new AssertionScope())
        {
            result.Should().Be(validInput);
        }
    }

    [Test]
    public void SanitizeForPath_WithInvalidCharacters_ShouldReplaceWithUnderscores()
    {
        // Given
        var invalidInput = "File<Name>:With\"Invalid*Characters";

        // When
        var result = PathUtility.SanitizeForPath(invalidInput);

        // Then
        using (new AssertionScope())
        {
            result.Should().NotContain("<");
            result.Should().NotContain(">");
            result.Should().NotContain(":");
            result.Should().NotContain("\"");
            result.Should().NotContain("*");
            result.Should().Contain("_");
        }
    }

    [Test]
    public void SplitNamespace_WithNullPath_ShouldReturnEmptyArray()
    {
        // Given
        string nullPath = null!;

        // When
        var result = PathUtility.SplitNamespace(nullPath);

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }
    }

    [Test]
    public void SplitNamespace_WithEmptyPath_ShouldReturnEmptyArray()
    {
        // Given
        var emptyPath = string.Empty;

        // When
        var result = PathUtility.SplitNamespace(emptyPath);

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }
    }

    [Test]
    public void SplitNamespace_WithValidPath_ShouldReturnParts()
    {
        // Given
        var namespacePath = "MyApp.Scripts.Upgrades";

        // When
        var result = PathUtility.SplitNamespace(namespacePath);

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
            result[0].Should().Be("MyApp");
            result[1].Should().Be("Scripts");
            result[2].Should().Be("Upgrades");
        }
    }

    [Test]
    public void GetParentNamespace_WithNullPath_ShouldReturnEmpty()
    {
        // Given
        string nullPath = null!;

        // When
        var result = PathUtility.GetParentNamespace(nullPath);

        // Then
        using (new AssertionScope())
        {
            result.Should().BeEmpty();
        }
    }

    [Test]
    public void GetParentNamespace_WithEmptyPath_ShouldReturnEmpty()
    {
        // Given
        var emptyPath = string.Empty;

        // When
        var result = PathUtility.GetParentNamespace(emptyPath);

        // Then
        using (new AssertionScope())
        {
            result.Should().BeEmpty();
        }
    }

    [Test]
    public void GetParentNamespace_WithSinglePart_ShouldReturnEmpty()
    {
        // Given
        var singlePart = "MyApp";

        // When
        var result = PathUtility.GetParentNamespace(singlePart);

        // Then
        using (new AssertionScope())
        {
            result.Should().BeEmpty();
        }
    }

    [Test]
    public void GetParentNamespace_WithMultipleParts_ShouldReturnParent()
    {
        // Given
        var namespacePath = "MyApp.Scripts.Upgrades";

        // When
        var result = PathUtility.GetParentNamespace(namespacePath);

        // Then
        using (new AssertionScope())
        {
            result.Should().Be("MyApp.Scripts");
        }
    }

    [Test]
    public void GetNamespaceLeaf_WithNullPath_ShouldReturnEmpty()
    {
        // Given
        string nullPath = null!;

        // When
        var result = PathUtility.GetNamespaceLeaf(nullPath);

        // Then
        using (new AssertionScope())
        {
            result.Should().BeEmpty();
        }
    }

    [Test]
    public void GetNamespaceLeaf_WithEmptyPath_ShouldReturnEmpty()
    {
        // Given
        var emptyPath = string.Empty;

        // When
        var result = PathUtility.GetNamespaceLeaf(emptyPath);

        // Then
        using (new AssertionScope())
        {
            result.Should().BeEmpty();
        }
    }

    [Test]
    public void GetNamespaceLeaf_WithSinglePart_ShouldReturnSamePart()
    {
        // Given
        var singlePart = "MyApp";

        // When
        var result = PathUtility.GetNamespaceLeaf(singlePart);

        // Then
        using (new AssertionScope())
        {
            result.Should().Be("MyApp");
        }
    }

    [Test]
    public void GetNamespaceLeaf_WithMultipleParts_ShouldReturnLastPart()
    {
        // Given
        var namespacePath = "MyApp.Scripts.Upgrades";

        // When
        var result = PathUtility.GetNamespaceLeaf(namespacePath);

        // Then
        using (new AssertionScope())
        {
            result.Should().Be("Upgrades");
        }
    }
}