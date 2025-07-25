using DbReactor.Core.Configuration;
using DbReactor.Core.Extensions;
using DbReactor.Core.Logging;
using FluentAssertions;
using FluentAssertions.Execution;
using Moq;
using NUnit.Framework;
using System;

namespace DbReactor.Core.Tests.Extensions;

[TestFixture]
public class LoggingExtensionsTests
{
    private DbReactorConfiguration _config;
    
    [SetUp]
    public void SetUp()
    {
        _config = new DbReactorConfiguration();
    }

    [Test]
    public void UseConsoleLogging_WhenCalled_ShouldSetConsoleLogProvider()
    {
        // When
        var result = _config.UseConsoleLogging();
        
        // Then
        using (new AssertionScope())
        {
            result.Should().Be(_config);
            _config.LogProvider.Should().BeOfType<ConsoleLogProvider>();
        }
    }

    [Test]
    public void AddLogProvider_WhenProviderIsValid_ShouldSetLogProvider()
    {
        // Given
        var mockProvider = new Mock<ILogProvider>();
        
        // When
        var result = _config.AddLogProvider(mockProvider.Object);
        
        // Then
        using (new AssertionScope())
        {
            result.Should().Be(_config);
            _config.LogProvider.Should().Be(mockProvider.Object);
        }
    }

    [Test]
    public void AddLogProvider_WhenProviderIsNull_ShouldThrowArgumentNullException()
    {
        // When
        Action act = () => _config.AddLogProvider(null);
        
        // Then
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logProvider");
    }

    [Test]
    public void AddLogProvider_WhenCalledMultipleTimes_ShouldReplaceProvider()
    {
        // Given
        var mockProvider1 = new Mock<ILogProvider>();
        var mockProvider2 = new Mock<ILogProvider>();
        
        // When
        _config
            .AddLogProvider(mockProvider1.Object)
            .AddLogProvider(mockProvider2.Object);
        
        // Then
        _config.LogProvider.Should().Be(mockProvider2.Object);
    }

    [Test]
    public void UseConsoleLogging_AfterAddLogProvider_ShouldReplaceCustomProvider()
    {
        // Given
        var mockProvider = new Mock<ILogProvider>();
        _config.AddLogProvider(mockProvider.Object);
        
        // When
        _config.UseConsoleLogging();
        
        // Then
        _config.LogProvider.Should().BeOfType<ConsoleLogProvider>();
    }
}