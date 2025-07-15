using DbReactor.Core.Configuration;
using DbReactor.Core.Discovery;
using DbReactor.Core.Execution;
using DbReactor.Core.Journaling;
using DbReactor.Core.Utilities;
using FluentAssertions;
using FluentAssertions.Execution;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DbReactor.Core.Tests.Utilities;

[TestFixture]
public class ConfigurationUtilityTests
{
    private Mock<IConnectionManager> _mockConnectionManager;
    private Mock<IMigrationJournal> _mockMigrationJournal;
    private Mock<IScriptExecutor> _mockScriptExecutor;
    private Mock<IScriptProvider> _mockScriptProvider;
    private Mock<IDowngradeResolver> _mockDowngradeResolver;
    private DbReactorConfiguration _configuration;

    [SetUp]
    public void SetUp()
    {
        _mockConnectionManager = new Mock<IConnectionManager>();
        _mockMigrationJournal = new Mock<IMigrationJournal>();
        _mockScriptExecutor = new Mock<IScriptExecutor>();
        _mockScriptProvider = new Mock<IScriptProvider>();
        _mockDowngradeResolver = new Mock<IDowngradeResolver>();

        _configuration = new DbReactorConfiguration
        {
            ConnectionManager = _mockConnectionManager.Object,
            MigrationJournal = _mockMigrationJournal.Object,
            ScriptExecutor = _mockScriptExecutor.Object,
            ScriptProviders = new List<IScriptProvider> { _mockScriptProvider.Object },
            DowngradeResolver = _mockDowngradeResolver.Object,
            AllowDowngrades = true,
            EnableVariables = true,
            Variables = new Dictionary<string, string> { { "TestVar", "TestValue" } }
        };
    }

    [Test]
    public void RefreshMigrationBuilder_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Given
        DbReactorConfiguration nullConfig = null!;

        // When
        Action act = () => ConfigurationUtility.RefreshMigrationBuilder(nullConfig);

        // Then
        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("config");
        }
    }

    [Test]
    public void RefreshMigrationBuilder_WithValidConfiguration_ShouldCreateMigrationBuilder()
    {
        // Given
        var config = _configuration;
        config.MigrationBuilder = null;

        // When
        ConfigurationUtility.RefreshMigrationBuilder(config);

        // Then
        using (new AssertionScope())
        {
            config.MigrationBuilder.Should().NotBeNull();
        }
    }

    [Test]
    public void RefreshMigrationBuilder_WithEmptyScriptProviders_ShouldNotCreateMigrationBuilder()
    {
        // Given
        var config = _configuration;
        config.ScriptProviders = new List<IScriptProvider>();
        config.MigrationBuilder = null;

        // When
        ConfigurationUtility.RefreshMigrationBuilder(config);

        // Then
        using (new AssertionScope())
        {
            config.MigrationBuilder.Should().BeNull();
        }
    }

    [Test]
    public void RefreshMigrationBuilder_WithNullScriptProviders_ShouldNotCreateMigrationBuilder()
    {
        // Given
        var config = _configuration;
        config.ScriptProviders = null;
        config.MigrationBuilder = null;

        // When
        ConfigurationUtility.RefreshMigrationBuilder(config);

        // Then
        using (new AssertionScope())
        {
            config.MigrationBuilder.Should().BeNull();
        }
    }

    [Test]
    public void ValidateConfiguration_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Given
        DbReactorConfiguration nullConfig = null!;

        // When
        Action act = () => ConfigurationUtility.ValidateConfiguration(nullConfig);

        // Then
        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("config");
        }
    }

    [Test]
    public void ValidateConfiguration_WithValidConfiguration_ShouldNotThrow()
    {
        // Given
        var config = _configuration;

        // When
        Action act = () => ConfigurationUtility.ValidateConfiguration(config);

        // Then
        using (new AssertionScope())
        {
            act.Should().NotThrow();
        }
    }

    [Test]
    public void ValidateConfiguration_WithNullScriptProviders_ShouldThrowInvalidOperationException()
    {
        // Given
        var config = _configuration;
        config.ScriptProviders = null;

        // When
        Action act = () => ConfigurationUtility.ValidateConfiguration(config);

        // Then
        using (new AssertionScope())
        {
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*script provider*");
        }
    }

    [Test]
    public void ValidateConfiguration_WithEmptyScriptProviders_ShouldThrowInvalidOperationException()
    {
        // Given
        var config = _configuration;
        config.ScriptProviders = new List<IScriptProvider>();

        // When
        Action act = () => ConfigurationUtility.ValidateConfiguration(config);

        // Then
        using (new AssertionScope())
        {
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*script provider*");
        }
    }

    [Test]
    public void ValidateConfiguration_WithNullConnectionManager_ShouldThrowInvalidOperationException()
    {
        // Given
        var config = _configuration;
        config.ConnectionManager = null;

        // When
        Action act = () => ConfigurationUtility.ValidateConfiguration(config);

        // Then
        using (new AssertionScope())
        {
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*connection manager*");
        }
    }

    [Test]
    public void ValidateConfiguration_WithNullMigrationJournal_ShouldThrowInvalidOperationException()
    {
        // Given
        var config = _configuration;
        config.MigrationJournal = null;

        // When
        Action act = () => ConfigurationUtility.ValidateConfiguration(config);

        // Then
        using (new AssertionScope())
        {
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*migration journal*");
        }
    }

    [Test]
    public void ValidateConfiguration_WithNullScriptExecutor_ShouldThrowInvalidOperationException()
    {
        // Given
        var config = _configuration;
        config.ScriptExecutor = null;

        // When
        Action act = () => ConfigurationUtility.ValidateConfiguration(config);

        // Then
        using (new AssertionScope())
        {
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*script executor*");
        }
    }

    [Test]
    public void EnsureMigrationBuilder_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Given
        DbReactorConfiguration nullConfig = null!;

        // When
        Action act = () => ConfigurationUtility.EnsureMigrationBuilder(nullConfig);

        // Then
        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("config");
        }
    }

    [Test]
    public void EnsureMigrationBuilder_WithNullMigrationBuilder_ShouldCreateOne()
    {
        // Given
        var config = _configuration;
        config.MigrationBuilder = null;

        // When
        ConfigurationUtility.EnsureMigrationBuilder(config);

        // Then
        using (new AssertionScope())
        {
            config.MigrationBuilder.Should().NotBeNull();
        }
    }

    [Test]
    public void EnsureMigrationBuilder_WithExistingMigrationBuilder_ShouldNotChange()
    {
        // Given
        var config = _configuration;
        ConfigurationUtility.RefreshMigrationBuilder(config); // Ensure it has a builder
        var existingBuilder = config.MigrationBuilder;

        // When
        ConfigurationUtility.EnsureMigrationBuilder(config);

        // Then
        using (new AssertionScope())
        {
            config.MigrationBuilder.Should().BeSameAs(existingBuilder);
        }
    }

    [Test]
    public void SupportsDowngrades_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Given
        DbReactorConfiguration nullConfig = null!;

        // When
        Action act = () => ConfigurationUtility.SupportsDowngrades(nullConfig);

        // Then
        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("config");
        }
    }

    [Test]
    public void SupportsDowngrades_WithDowngradesEnabled_ShouldReturnTrue()
    {
        // Given
        var config = _configuration;
        config.AllowDowngrades = true;
        config.DowngradeResolver = _mockDowngradeResolver.Object;

        // When
        var result = ConfigurationUtility.SupportsDowngrades(config);

        // Then
        using (new AssertionScope())
        {
            result.Should().BeTrue();
        }
    }

    [Test]
    public void SupportsDowngrades_WithDowngradesDisabled_ShouldReturnFalse()
    {
        // Given
        var config = _configuration;
        config.AllowDowngrades = false;
        config.DowngradeResolver = _mockDowngradeResolver.Object;

        // When
        var result = ConfigurationUtility.SupportsDowngrades(config);

        // Then
        using (new AssertionScope())
        {
            result.Should().BeFalse();
        }
    }

    [Test]
    public void SupportsDowngrades_WithNullDowngradeResolver_ShouldReturnFalse()
    {
        // Given
        var config = _configuration;
        config.AllowDowngrades = true;
        config.DowngradeResolver = null;

        // When
        var result = ConfigurationUtility.SupportsDowngrades(config);

        // Then
        using (new AssertionScope())
        {
            result.Should().BeFalse();
        }
    }

    [Test]
    public void GetScriptProviderCount_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Given
        DbReactorConfiguration nullConfig = null!;

        // When
        Action act = () => ConfigurationUtility.GetScriptProviderCount(nullConfig);

        // Then
        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("config");
        }
    }

    [Test]
    public void GetScriptProviderCount_WithValidConfiguration_ShouldReturnCount()
    {
        // Given
        var config = _configuration;

        // When
        var result = ConfigurationUtility.GetScriptProviderCount(config);

        // Then
        using (new AssertionScope())
        {
            result.Should().Be(1);
        }
    }

    [Test]
    public void GetScriptProviderCount_WithNullScriptProviders_ShouldReturnZero()
    {
        // Given
        var config = _configuration;
        config.ScriptProviders = null;

        // When
        var result = ConfigurationUtility.GetScriptProviderCount(config);

        // Then
        using (new AssertionScope())
        {
            result.Should().Be(0);
        }
    }

    [Test]
    public void HasVariablesEnabled_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Given
        DbReactorConfiguration nullConfig = null!;

        // When
        Action act = () => ConfigurationUtility.HasVariablesEnabled(nullConfig);

        // Then
        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("config");
        }
    }

    [Test]
    public void HasVariablesEnabled_WithVariablesEnabled_ShouldReturnTrue()
    {
        // Given
        var config = _configuration;
        config.EnableVariables = true;
        config.Variables = new Dictionary<string, string> { { "Test", "Value" } };

        // When
        var result = ConfigurationUtility.HasVariablesEnabled(config);

        // Then
        using (new AssertionScope())
        {
            result.Should().BeTrue();
        }
    }

    [Test]
    public void HasVariablesEnabled_WithVariablesDisabled_ShouldReturnFalse()
    {
        // Given
        var config = _configuration;
        config.EnableVariables = false;
        config.Variables = new Dictionary<string, string> { { "Test", "Value" } };

        // When
        var result = ConfigurationUtility.HasVariablesEnabled(config);

        // Then
        using (new AssertionScope())
        {
            result.Should().BeFalse();
        }
    }

    [Test]
    public void HasVariablesEnabled_WithEmptyVariables_ShouldReturnFalse()
    {
        // Given
        var config = _configuration;
        config.EnableVariables = true;
        config.Variables = new Dictionary<string, string>();

        // When
        var result = ConfigurationUtility.HasVariablesEnabled(config);

        // Then
        using (new AssertionScope())
        {
            result.Should().BeFalse();
        }
    }

    [Test]
    public void HasVariablesEnabled_WithNullVariables_ShouldReturnFalse()
    {
        // Given
        var config = _configuration;
        config.EnableVariables = true;
        config.Variables = null;

        // When
        var result = ConfigurationUtility.HasVariablesEnabled(config);

        // Then
        using (new AssertionScope())
        {
            result.Should().BeFalse();
        }
    }

    [Test]
    public void ResetMigrationBuilder_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Given
        DbReactorConfiguration nullConfig = null!;

        // When
        Action act = () => ConfigurationUtility.ResetMigrationBuilder(nullConfig);

        // Then
        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("config");
        }
    }

    [Test]
    public void ResetMigrationBuilder_WithValidConfiguration_ShouldSetToNull()
    {
        // Given
        var config = _configuration;
        ConfigurationUtility.RefreshMigrationBuilder(config); // Ensure it has a builder
        config.MigrationBuilder.Should().NotBeNull();

        // When
        ConfigurationUtility.ResetMigrationBuilder(config);

        // Then
        using (new AssertionScope())
        {
            config.MigrationBuilder.Should().BeNull();
        }
    }

    [Test]
    public void CreateConfigurationSummary_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Given
        DbReactorConfiguration nullConfig = null!;

        // When
        Action act = () => ConfigurationUtility.CreateConfigurationSummary(nullConfig);

        // Then
        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("config");
        }
    }

    [Test]
    public void CreateConfigurationSummary_WithValidConfiguration_ShouldReturnSummary()
    {
        // Given
        var config = _configuration;

        // When
        var result = ConfigurationUtility.CreateConfigurationSummary(config);

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNullOrEmpty();
            result.Should().Contain("DbReactor Configuration Summary");
            result.Should().Contain("Script Providers: 1");
            result.Should().Contain("Connection Manager: Configured");
            result.Should().Contain("Migration Journal: Configured");
            result.Should().Contain("Script Executor: Configured");
            result.Should().Contain("Downgrade Support: Enabled");
            result.Should().Contain("Variables: Enabled (1 variables)");
        }
    }

    [Test]
    public void CreateConfigurationSummary_WithMinimalConfiguration_ShouldReturnSummary()
    {
        // Given
        var config = new DbReactorConfiguration
        {
            ScriptProviders = new List<IScriptProvider>(),
            AllowDowngrades = false,
            EnableVariables = false
        };

        // When
        var result = ConfigurationUtility.CreateConfigurationSummary(config);

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNullOrEmpty();
            result.Should().Contain("DbReactor Configuration Summary");
            result.Should().Contain("Script Providers: 0");
            result.Should().Contain("Connection Manager: Not Configured");
            result.Should().Contain("Migration Journal: Not Configured");
            result.Should().Contain("Script Executor: Not Configured");
            result.Should().Contain("Downgrade Support: Disabled");
            result.Should().Contain("Variables: Disabled");
        }
    }
}