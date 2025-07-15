using DbReactor.Core.Execution;
using DbReactor.Core.Models.Contexts;
using FluentAssertions;
using FluentAssertions.Execution;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace DbReactor.Core.Tests.Models.Contexts;

[TestFixture]
public class CodeScriptContextTests
{
    private Mock<IConnectionManager> _mockConnectionManager;

    [SetUp]
    public void SetUp()
    {
        _mockConnectionManager = new Mock<IConnectionManager>();
    }

    [Test]
    public void Constructor_WithValidConnectionManager_ShouldInitializeProperties()
    {
        // Given
        var connectionManager = _mockConnectionManager.Object;

        // When
        var context = new CodeScriptContext(connectionManager);

        // Then
        using (new AssertionScope())
        {
            context.ConnectionManager.Should().Be(connectionManager);
            context.Variables.Should().NotBeNull();
            context.Variables.Should().BeEmpty();
            context.Vars.Should().NotBeNull();
        }
    }

    [Test]
    public void Constructor_WithNullConnectionManager_ShouldThrowArgumentNullException()
    {
        // Given
        IConnectionManager nullConnectionManager = null!;

        // When
        Action act = () => new CodeScriptContext(nullConnectionManager);

        // Then
        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("connectionManager");
        }
    }

    [Test]
    public void Constructor_WithVariables_ShouldInitializeVariablesProperty()
    {
        // Given
        var connectionManager = _mockConnectionManager.Object;
        var variables = new Dictionary<string, string>
        {
            { "Environment", "Development" },
            { "AdminEmail", "admin@example.com" },
            { "MaxRetries", "5" }
        };

        // When
        var context = new CodeScriptContext(connectionManager, variables);

        // Then
        using (new AssertionScope())
        {
            context.Variables.Should().NotBeNull();
            context.Variables.Should().HaveCount(3);
            context.Variables["Environment"].Should().Be("Development");
            context.Variables["AdminEmail"].Should().Be("admin@example.com");
            context.Variables["MaxRetries"].Should().Be("5");
        }
    }

    [Test]
    public void Constructor_WithNullVariables_ShouldInitializeEmptyVariables()
    {
        // Given
        var connectionManager = _mockConnectionManager.Object;
        Dictionary<string, string>? nullVariables = null;

        // When
        var context = new CodeScriptContext(connectionManager, nullVariables);

        // Then
        using (new AssertionScope())
        {
            context.Variables.Should().NotBeNull();
            context.Variables.Should().BeEmpty();
        }
    }

    [Test]
    public void Vars_Property_ShouldBeInitializedWithVariables()
    {
        // Given
        var connectionManager = _mockConnectionManager.Object;
        var variables = new Dictionary<string, string>
        {
            { "TestVariable", "TestValue" },
            { "NumericVariable", "42" }
        };

        // When
        var context = new CodeScriptContext(connectionManager, variables);

        // Then
        using (new AssertionScope())
        {
            context.Vars.Should().NotBeNull();
            context.Vars.GetString("TestVariable").Should().Be("TestValue");
            context.Vars.GetInt("NumericVariable").Should().Be(42);
        }
    }

    [Test]
    public void Variables_Property_ShouldBeReadOnlyInterface()
    {
        // Given
        var connectionManager = _mockConnectionManager.Object;
        var variables = new Dictionary<string, string>
        {
            { "TestKey", "TestValue" }
        };

        // When
        var context = new CodeScriptContext(connectionManager, variables);

        // Then
        using (new AssertionScope())
        {
            context.Variables.Should().BeAssignableTo<IReadOnlyDictionary<string, string>>();
            // Even though the underlying implementation might be a Dictionary,
            // the public interface is read-only
            context.Variables.Should().BeSameAs(variables);
        }
    }

    [Test]
    public void Vars_And_Variables_ShouldProvideAccessToSameData()
    {
        // Given
        var connectionManager = _mockConnectionManager.Object;
        var variables = new Dictionary<string, string>
        {
            { "SharedKey", "SharedValue" }
        };

        // When
        var context = new CodeScriptContext(connectionManager, variables);

        // Then
        using (new AssertionScope())
        {
            // Both should provide access to the same underlying data
            context.Variables["SharedKey"].Should().Be("SharedValue");
            context.Vars.GetString("SharedKey").Should().Be("SharedValue");
        }
    }

    [Test]
    public void ConnectionManager_Property_ShouldReturnInjectedInstance()
    {
        // Given
        var connectionManager = _mockConnectionManager.Object;

        // When
        var context = new CodeScriptContext(connectionManager);

        // Then
        using (new AssertionScope())
        {
            context.ConnectionManager.Should().BeSameAs(connectionManager);
        }
    }

    [Test]
    public void Constructor_WithEmptyVariables_ShouldInitializeEmptyCollections()
    {
        // Given
        var connectionManager = _mockConnectionManager.Object;
        var emptyVariables = new Dictionary<string, string>();

        // When
        var context = new CodeScriptContext(connectionManager, emptyVariables);

        // Then
        using (new AssertionScope())
        {
            context.Variables.Should().NotBeNull();
            context.Variables.Should().BeEmpty();
            context.Vars.Should().NotBeNull();
            context.Vars.GetVariableNames().Should().BeEmpty();
        }
    }

    [Test]
    public void Vars_Property_ShouldSupportFluentAPI()
    {
        // Given
        var connectionManager = _mockConnectionManager.Object;
        var variables = new Dictionary<string, string>
        {
            { "StringVar", "Hello" },
            { "IntVar", "123" },
            { "BoolVar", "true" }
        };

        // When
        var context = new CodeScriptContext(connectionManager, variables);

        // Then
        using (new AssertionScope())
        {
            // Test fluent API methods work correctly
            context.Vars.GetString("StringVar").Should().Be("Hello");
            context.Vars.GetString("MissingVar", "Default").Should().Be("Default");
            context.Vars.GetInt("IntVar").Should().Be(123);
            context.Vars.GetBool("BoolVar").Should().BeTrue();
            context.Vars.HasVariable("StringVar").Should().BeTrue();
            context.Vars.HasVariable("MissingVar").Should().BeFalse();
        }
    }
}