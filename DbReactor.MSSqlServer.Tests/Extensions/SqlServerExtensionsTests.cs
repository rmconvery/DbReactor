using DbReactor.Core.Configuration;
using DbReactor.Core.Implementations.Logging;
using DbReactor.MSSqlServer.Execution;
using DbReactor.MSSqlServer.Execution.DbReactor.MSSqlServer.Implementations.Execution;
using DbReactor.MSSqlServer.Extensions;
using DbReactor.MSSqlServer.Journaling;
using DbReactor.MSSqlServer.Provisioning;
using FluentAssertions;
using FluentAssertions.Execution;
using NUnit.Framework;
using System;

namespace DbReactor.MSSqlServer.Tests.Extensions;

[TestFixture]
public class SqlServerExtensionsTests
{
    private const string ValidConnectionString = "Server=localhost;Database=TestDB;Trusted_Connection=true;";
    private const string CustomConnectionString = "Server=custom;Database=CustomDB;Trusted_Connection=true;";
    private DbReactorConfiguration _config;

    [SetUp]
    public void SetUp()
    {
        _config = new DbReactorConfiguration();
    }

    [Test]
    public void UseSqlServer_WithValidConnectionString_ShouldConfigureAllComponents()
    {
        // When
        var result = _config.UseSqlServer(ValidConnectionString);

        // Then
        using (new AssertionScope())
        {
            result.Should().BeSameAs(_config);
            result.ConnectionManager.Should().NotBeNull();
            result.ConnectionManager.Should().BeOfType<SqlServerConnectionManager>();
            result.ScriptExecutor.Should().NotBeNull();
            result.ScriptExecutor.Should().BeOfType<SqlServerScriptExecutor>();
            result.MigrationJournal.Should().NotBeNull();
            result.MigrationJournal.Should().BeOfType<SqlServerScriptJournal>();
            result.DatabaseProvisioner.Should().NotBeNull();
            result.DatabaseProvisioner.Should().BeOfType<SqlServerDatabaseProvisioner>();
        }
    }

    [Test]
    public void UseSqlServer_WithCustomTimeout_ShouldConfigureAllComponents()
    {
        // Given
        var customTimeout = 60;

        // When
        var result = _config.UseSqlServer(ValidConnectionString, customTimeout);

        // Then
        using (new AssertionScope())
        {
            result.Should().BeSameAs(_config);
            result.ConnectionManager.Should().NotBeNull();
            result.ScriptExecutor.Should().NotBeNull();
            result.MigrationJournal.Should().NotBeNull();
            result.DatabaseProvisioner.Should().NotBeNull();
        }
    }

    [Test]
    public void UseSqlServer_WithCustomJournalConfiguration_ShouldConfigureAllComponents()
    {
        // Given
        var customSchema = "custom";
        var customTable = "CustomJournal";

        // When
        var result = _config.UseSqlServer(ValidConnectionString, journalSchema: customSchema, journalTable: customTable);

        // Then
        using (new AssertionScope())
        {
            result.Should().BeSameAs(_config);
            result.ConnectionManager.Should().NotBeNull();
            result.ScriptExecutor.Should().NotBeNull();
            result.MigrationJournal.Should().NotBeNull();
            result.DatabaseProvisioner.Should().NotBeNull();
        }
    }

    [Test]
    public void UseSqlServer_WithAllCustomParameters_ShouldConfigureAllComponents()
    {
        // Given
        var customTimeout = 120;
        var customSchema = "migrations";
        var customTable = "ScriptJournal";

        // When
        var result = _config.UseSqlServer(ValidConnectionString, customTimeout, customSchema, customTable);

        // Then
        using (new AssertionScope())
        {
            result.Should().BeSameAs(_config);
            result.ConnectionManager.Should().NotBeNull();
            result.ScriptExecutor.Should().NotBeNull();
            result.MigrationJournal.Should().NotBeNull();
            result.DatabaseProvisioner.Should().NotBeNull();
        }
    }

    [Test]
    public void UseSqlServer_WithNullConnectionString_ShouldThrowArgumentNullException()
    {
        // When
        Action act = () => _config.UseSqlServer(null);

        // Then
        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("connectionString");
        }
    }

    [Test]
    public void UseSqlServerConnection_WithValidConnectionString_ShouldConfigureConnectionManager()
    {
        // When
        var result = _config.UseSqlServerConnection(ValidConnectionString);

        // Then
        using (new AssertionScope())
        {
            result.Should().BeSameAs(_config);
            result.ConnectionManager.Should().NotBeNull();
            result.ConnectionManager.Should().BeOfType<SqlServerConnectionManager>();
        }
    }

    [Test]
    public void UseSqlServerConnection_WithNullConnectionString_ShouldThrowArgumentNullException()
    {
        // When
        Action act = () => _config.UseSqlServerConnection(null);

        // Then
        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("connectionString");
        }
    }

    [Test]
    public void UseSqlServerExecutor_WithDefaultTimeout_ShouldConfigureExecutor()
    {
        // When
        var result = _config.UseSqlServerExecutor();

        // Then
        using (new AssertionScope())
        {
            result.Should().BeSameAs(_config);
            result.ScriptExecutor.Should().NotBeNull();
            result.ScriptExecutor.Should().BeOfType<SqlServerScriptExecutor>();
        }
    }

    [Test]
    public void UseSqlServerExecutor_WithCustomTimeout_ShouldConfigureExecutor()
    {
        // Given
        var customTimeout = 60;

        // When
        var result = _config.UseSqlServerExecutor(customTimeout);

        // Then
        using (new AssertionScope())
        {
            result.Should().BeSameAs(_config);
            result.ScriptExecutor.Should().NotBeNull();
            result.ScriptExecutor.Should().BeOfType<SqlServerScriptExecutor>();
        }
    }

    [Test]
    public void UseSqlServerJournal_WithDefaultParameters_ShouldConfigureJournal()
    {
        // When
        var result = _config.UseSqlServerJournal();

        // Then
        using (new AssertionScope())
        {
            result.Should().BeSameAs(_config);
            result.MigrationJournal.Should().NotBeNull();
            result.MigrationJournal.Should().BeOfType<SqlServerScriptJournal>();
        }
    }

    [Test]
    public void UseSqlServerJournal_WithCustomParameters_ShouldConfigureJournal()
    {
        // Given
        var customSchema = "custom";
        var customTable = "CustomJournal";

        // When
        var result = _config.UseSqlServerJournal(customSchema, customTable);

        // Then
        using (new AssertionScope())
        {
            result.Should().BeSameAs(_config);
            result.MigrationJournal.Should().NotBeNull();
            result.MigrationJournal.Should().BeOfType<SqlServerScriptJournal>();
        }
    }

    [Test]
    public void UseSqlServerJournal_WithExistingConnectionManager_ShouldSetConnectionManager()
    {
        // Given
        _config.UseSqlServerConnection(ValidConnectionString);

        // When
        var result = _config.UseSqlServerJournal();

        // Then
        using (new AssertionScope())
        {
            result.Should().BeSameAs(_config);
            result.MigrationJournal.Should().NotBeNull();
            result.MigrationJournal.Should().BeOfType<SqlServerScriptJournal>();
            // The journal should have the connection manager set internally
        }
    }

    [Test]
    public void UseSqlServerJournal_WithNullSchema_ShouldUseDefaultSchema()
    {
        // When
        var result = _config.UseSqlServerJournal(null);

        // Then
        using (new AssertionScope())
        {
            result.Should().BeSameAs(_config);
            result.MigrationJournal.Should().NotBeNull();
            result.MigrationJournal.Should().BeOfType<SqlServerScriptJournal>();
        }
    }

    [Test]
    public void UseSqlServerJournal_WithNullTableName_ShouldUseDefaultTableName()
    {
        // When
        var result = _config.UseSqlServerJournal(tableName: null);

        // Then
        using (new AssertionScope())
        {
            result.Should().BeSameAs(_config);
            result.MigrationJournal.Should().NotBeNull();
            result.MigrationJournal.Should().BeOfType<SqlServerScriptJournal>();
        }
    }

    [Test]
    public void UseSqlServerProvisioner_WithValidConnectionString_ShouldConfigureProvisioner()
    {
        // When
        var result = _config.UseSqlServerProvisioner(ValidConnectionString);

        // Then
        using (new AssertionScope())
        {
            result.Should().BeSameAs(_config);
            result.DatabaseProvisioner.Should().NotBeNull();
            result.DatabaseProvisioner.Should().BeOfType<SqlServerDatabaseProvisioner>();
        }
    }

    [Test]
    public void UseSqlServerProvisioner_WithNullConnectionString_ShouldThrowArgumentNullException()
    {
        // When
        Action act = () => _config.UseSqlServerProvisioner(null);

        // Then
        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("connectionString");
        }
    }

    [Test]
    public void UseSqlServerProvisioner_WithExistingLogProvider_ShouldUseLogProvider()
    {
        // Given
        _config.LogProvider = new NullLogProvider();

        // When
        var result = _config.UseSqlServerProvisioner(ValidConnectionString);

        // Then
        using (new AssertionScope())
        {
            result.Should().BeSameAs(_config);
            result.DatabaseProvisioner.Should().NotBeNull();
            result.DatabaseProvisioner.Should().BeOfType<SqlServerDatabaseProvisioner>();
        }
    }

    [Test]
    public void ChainedMethodCalls_ShouldReturnSameConfigurationInstance()
    {
        // When
        var result = _config
            .UseSqlServerConnection(ValidConnectionString)
            .UseSqlServerExecutor(60)
            .UseSqlServerJournal("custom", "CustomJournal")
            .UseSqlServerProvisioner(ValidConnectionString);

        // Then
        using (new AssertionScope())
        {
            result.Should().BeSameAs(_config);
            result.ConnectionManager.Should().NotBeNull();
            result.ScriptExecutor.Should().NotBeNull();
            result.MigrationJournal.Should().NotBeNull();
            result.DatabaseProvisioner.Should().NotBeNull();
        }
    }

    [Test]
    public void MethodCalls_ShouldAllowOverridingPreviousConfiguration()
    {
        // Given
        _config.UseSqlServerConnection(ValidConnectionString);

        // When
        var result = _config.UseSqlServerConnection(CustomConnectionString);

        // Then
        using (new AssertionScope())
        {
            result.Should().BeSameAs(_config);
            result.ConnectionManager.Should().NotBeNull();
            result.ConnectionManager.Should().BeOfType<SqlServerConnectionManager>();
        }
    }

    [Test]
    public void UseSqlServerExecutor_WithZeroTimeout_ShouldConfigureExecutor()
    {
        // When
        var result = _config.UseSqlServerExecutor(0);

        // Then
        using (new AssertionScope())
        {
            result.Should().BeSameAs(_config);
            result.ScriptExecutor.Should().NotBeNull();
            result.ScriptExecutor.Should().BeOfType<SqlServerScriptExecutor>();
        }
    }

    [Test]
    public void UseSqlServerExecutor_WithNegativeTimeout_ShouldConfigureExecutor()
    {
        // When
        var result = _config.UseSqlServerExecutor(-1);

        // Then
        using (new AssertionScope())
        {
            result.Should().BeSameAs(_config);
            result.ScriptExecutor.Should().NotBeNull();
            result.ScriptExecutor.Should().BeOfType<SqlServerScriptExecutor>();
        }
    }

    [Test]
    public void UseSqlServerJournal_WithInvalidSchema_ShouldThrowArgumentException()
    {
        // Given
        var invalidSchema = "schema;DROP TABLE";

        // When
        Action act = () => _config.UseSqlServerJournal(invalidSchema);

        // Then
        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Invalid SQL identifier*");
        }
    }

    [Test]
    public void UseSqlServerJournal_WithInvalidTableName_ShouldThrowArgumentException()
    {
        // Given
        var invalidTableName = "table]DROP TABLE";

        // When
        Action act = () => _config.UseSqlServerJournal(tableName: invalidTableName);

        // Then
        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Invalid SQL identifier*");
        }
    }

    [Test]
    public void ConfigurationComponentsChaining_ShouldMaintainAllComponents()
    {
        // When
        var result = _config
            .UseSqlServer(ValidConnectionString)
            .UseSqlServerConnection(CustomConnectionString); // Override connection

        // Then
        using (new AssertionScope())
        {
            result.Should().BeSameAs(_config);
            result.ConnectionManager.Should().NotBeNull();
            result.ScriptExecutor.Should().NotBeNull();
            result.MigrationJournal.Should().NotBeNull();
            result.DatabaseProvisioner.Should().NotBeNull();
        }
    }

    [Test]
    public void AllExtensionMethods_ShouldSupportFluentInterface()
    {
        // When
        var result = _config
            .UseSqlServer(ValidConnectionString, 120, "migrations", "ScriptJournal")
            .UseSqlServerConnection(CustomConnectionString)
            .UseSqlServerExecutor(90)
            .UseSqlServerJournal("custom", "Journal")
            .UseSqlServerProvisioner(ValidConnectionString);

        // Then
        using (new AssertionScope())
        {
            result.Should().BeSameAs(_config);
            result.ConnectionManager.Should().BeOfType<SqlServerConnectionManager>();
            result.ScriptExecutor.Should().BeOfType<SqlServerScriptExecutor>();
            result.MigrationJournal.Should().BeOfType<SqlServerScriptJournal>();
            result.DatabaseProvisioner.Should().BeOfType<SqlServerDatabaseProvisioner>();
        }
    }
}