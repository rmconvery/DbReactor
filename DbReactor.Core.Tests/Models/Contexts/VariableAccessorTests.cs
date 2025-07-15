using DbReactor.Core.Models.Contexts;

namespace DbReactor.Core.Tests.Models.Contexts;

[TestFixture]
public class VariableAccessorTests
{
    [Test]
    public void GetString_WhenVariableExists_ShouldReturnValue()
    {
        // Given
        var variables = new Dictionary<string, string> { { "key", "value" } };
        var accessor = new VariableAccessor(variables);

        // When
        var result = accessor.GetString("key");

        // Then
        using (new AssertionScope())
        {
            result.Should().Be("value");
        }
    }

    [Test]
    public void GetString_WhenVariableDoesNotExist_ShouldReturnDefaultValue()
    {
        // Given
        var variables = new Dictionary<string, string>();
        var accessor = new VariableAccessor(variables);

        // When
        var result = accessor.GetString("nonexistent", "default");

        // Then
        using (new AssertionScope())
        {
            result.Should().Be("default");
        }
    }

    [Test]
    public void GetString_WhenVariableDoesNotExistAndNoDefault_ShouldReturnNull()
    {
        // Given
        var variables = new Dictionary<string, string>();
        var accessor = new VariableAccessor(variables);

        // When
        var result = accessor.GetString("nonexistent");

        // Then
        using (new AssertionScope())
        {
            result.Should().BeNull();
        }
    }

    [Test]
    public void GetRequiredString_WhenVariableExists_ShouldReturnValue()
    {
        // Given
        var variables = new Dictionary<string, string> { { "key", "value" } };
        var accessor = new VariableAccessor(variables);

        // When
        var result = accessor.GetRequiredString("key");

        // Then
        using (new AssertionScope())
        {
            result.Should().Be("value");
        }
    }

    [Test]
    public void GetRequiredString_WhenVariableDoesNotExist_ShouldThrowException()
    {
        // Given
        var variables = new Dictionary<string, string>();
        var accessor = new VariableAccessor(variables);

        // When
        Action act = () => accessor.GetRequiredString("nonexistent");

        // Then
        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentException>()
                .WithMessage("Required variable 'nonexistent' is missing or empty");
        }
    }

    [Test]
    public void GetRequiredString_WhenVariableIsEmpty_ShouldThrowException()
    {
        // Given
        var variables = new Dictionary<string, string> { { "key", "" } };
        var accessor = new VariableAccessor(variables);

        // When
        Action act = () => accessor.GetRequiredString("key");

        // Then
        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentException>()
                .WithMessage("Required variable 'key' is missing or empty");
        }
    }

    [Test]
    public void GetInt_WhenVariableIsValidInteger_ShouldReturnParsedValue()
    {
        // Given
        var variables = new Dictionary<string, string> { { "key", "42" } };
        var accessor = new VariableAccessor(variables);

        // When
        var result = accessor.GetInt("key");

        // Then
        using (new AssertionScope())
        {
            result.Should().Be(42);
        }
    }

    [Test]
    public void GetInt_WhenVariableDoesNotExist_ShouldReturnDefaultValue()
    {
        // Given
        var variables = new Dictionary<string, string>();
        var accessor = new VariableAccessor(variables);

        // When
        var result = accessor.GetInt("nonexistent", 100);

        // Then
        using (new AssertionScope())
        {
            result.Should().Be(100);
        }
    }

    [Test]
    public void GetInt_WhenVariableIsNotValidInteger_ShouldReturnDefaultValue()
    {
        // Given
        var variables = new Dictionary<string, string> { { "key", "not_a_number" } };
        var accessor = new VariableAccessor(variables);

        // When
        var result = accessor.GetInt("key", 100);

        // Then
        using (new AssertionScope())
        {
            result.Should().Be(100);
        }
    }

    [Test]
    public void GetRequiredInt_WhenVariableIsValidInteger_ShouldReturnParsedValue()
    {
        // Given
        var variables = new Dictionary<string, string> { { "key", "42" } };
        var accessor = new VariableAccessor(variables);

        // When
        var result = accessor.GetRequiredInt("key");

        // Then
        using (new AssertionScope())
        {
            result.Should().Be(42);
        }
    }

    [Test]
    public void GetRequiredInt_WhenVariableDoesNotExist_ShouldThrowException()
    {
        // Given
        var variables = new Dictionary<string, string>();
        var accessor = new VariableAccessor(variables);

        // When
        Action act = () => accessor.GetRequiredInt("nonexistent");

        // Then
        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentException>()
                .WithMessage("Required integer variable 'nonexistent' is missing or invalid");
        }
    }

    [Test]
    public void GetRequiredInt_WhenVariableIsNotValidInteger_ShouldThrowException()
    {
        // Given
        var variables = new Dictionary<string, string> { { "key", "not_a_number" } };
        var accessor = new VariableAccessor(variables);

        // When
        Action act = () => accessor.GetRequiredInt("key");

        // Then
        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentException>()
                .WithMessage("Required integer variable 'key' is missing or invalid");
        }
    }

    [Test]
    public void GetBool_WhenVariableIsTrue_ShouldReturnTrue()
    {
        // Given
        var variables = new Dictionary<string, string> { { "key", "true" } };
        var accessor = new VariableAccessor(variables);

        // When
        var result = accessor.GetBool("key");

        // Then
        using (new AssertionScope())
        {
            result.Should().BeTrue();
        }
    }

    [Test]
    public void GetBool_WhenVariableIsFalse_ShouldReturnFalse()
    {
        // Given
        var variables = new Dictionary<string, string> { { "key", "false" } };
        var accessor = new VariableAccessor(variables);

        // When
        var result = accessor.GetBool("key");

        // Then
        using (new AssertionScope())
        {
            result.Should().BeFalse();
        }
    }

    [Test]
    public void GetBool_WhenVariableDoesNotExist_ShouldReturnDefaultValue()
    {
        // Given
        var variables = new Dictionary<string, string>();
        var accessor = new VariableAccessor(variables);

        // When
        var result = accessor.GetBool("nonexistent", true);

        // Then
        using (new AssertionScope())
        {
            result.Should().BeTrue();
        }
    }

    [Test]
    public void GetBool_WhenVariableIsNotValidBoolean_ShouldReturnDefaultValue()
    {
        // Given
        var variables = new Dictionary<string, string> { { "key", "not_a_boolean" } };
        var accessor = new VariableAccessor(variables);

        // When
        var result = accessor.GetBool("key", true);

        // Then
        using (new AssertionScope())
        {
            result.Should().BeTrue();
        }
    }

    [Test]
    public void GetRequiredBool_WhenVariableIsValidBoolean_ShouldReturnParsedValue()
    {
        // Given
        var variables = new Dictionary<string, string> { { "key", "true" } };
        var accessor = new VariableAccessor(variables);

        // When
        var result = accessor.GetRequiredBool("key");

        // Then
        using (new AssertionScope())
        {
            result.Should().BeTrue();
        }
    }

    [Test]
    public void GetRequiredBool_WhenVariableDoesNotExist_ShouldThrowException()
    {
        // Given
        var variables = new Dictionary<string, string>();
        var accessor = new VariableAccessor(variables);

        // When
        Action act = () => accessor.GetRequiredBool("nonexistent");

        // Then
        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentException>()
                .WithMessage("Required boolean variable 'nonexistent' is missing or invalid");
        }
    }

    [Test]
    public void GetRequiredBool_WhenVariableIsNotValidBoolean_ShouldThrowException()
    {
        // Given
        var variables = new Dictionary<string, string> { { "key", "not_a_boolean" } };
        var accessor = new VariableAccessor(variables);

        // When
        Action act = () => accessor.GetRequiredBool("key");

        // Then
        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentException>()
                .WithMessage("Required boolean variable 'key' is missing or invalid");
        }
    }

    [Test]
    public void HasVariable_WhenVariableExists_ShouldReturnTrue()
    {
        // Given
        var variables = new Dictionary<string, string> { { "key", "value" } };
        var accessor = new VariableAccessor(variables);

        // When
        var result = accessor.HasVariable("key");

        // Then
        using (new AssertionScope())
        {
            result.Should().BeTrue();
        }
    }

    [Test]
    public void HasVariable_WhenVariableDoesNotExist_ShouldReturnFalse()
    {
        // Given
        var variables = new Dictionary<string, string>();
        var accessor = new VariableAccessor(variables);

        // When
        var result = accessor.HasVariable("nonexistent");

        // Then
        using (new AssertionScope())
        {
            result.Should().BeFalse();
        }
    }

    [Test]
    public void GetVariableNames_WhenVariablesExist_ShouldReturnAllKeys()
    {
        // Given
        var variables = new Dictionary<string, string> 
        { 
            { "key1", "value1" }, 
            { "key2", "value2" } 
        };
        var accessor = new VariableAccessor(variables);

        // When
        var result = accessor.GetVariableNames();

        // Then
        using (new AssertionScope())
        {
            result.Should().Contain("key1");
            result.Should().Contain("key2");
            result.Should().HaveCount(2);
        }
    }

    [Test]
    public void GetVariableNames_WhenNoVariables_ShouldReturnEmptyCollection()
    {
        // Given
        var variables = new Dictionary<string, string>();
        var accessor = new VariableAccessor(variables);

        // When
        var result = accessor.GetVariableNames();

        // Then
        using (new AssertionScope())
        {
            result.Should().BeEmpty();
        }
    }
}