using DbReactor.Core.Configuration;
using DbReactor.Core.Discovery;
using DbReactor.Core.Extensions;
using FluentAssertions;
using FluentAssertions.Execution;
using Moq;
using NUnit.Framework;
using System;

namespace DbReactor.Core.Tests.Extensions;

[TestFixture]
public class ScriptDiscoveryExtensionsTests
{
    private DbReactorConfiguration _config;
    
    [SetUp]
    public void SetUp()
    {
        _config = new DbReactorConfiguration();
    }

    [Test]
    public void AddScriptProvider_WhenProviderIsValid_ShouldAddToScriptProviders()
    {
        // Given
        var mockProvider = new Mock<IScriptProvider>();
        
        // When
        var result = _config.AddScriptProvider(mockProvider.Object);
        
        // Then
        using (new AssertionScope())
        {
            result.Should().Be(_config);
            _config.ScriptProviders.Should().Contain(mockProvider.Object);
            _config.ScriptProviders.Should().HaveCount(1);
        }
    }

    [Test]
    public void AddScriptProvider_WhenProviderIsNull_ShouldThrowArgumentNullException()
    {
        // When
        Action act = () => _config.AddScriptProvider(null);
        
        // Then
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("provider");
    }

    [Test]
    public void AddScriptProvider_WhenCalledMultipleTimes_ShouldAddAllProviders()
    {
        // Given
        var mockProvider1 = new Mock<IScriptProvider>();
        var mockProvider2 = new Mock<IScriptProvider>();
        
        // When
        _config
            .AddScriptProvider(mockProvider1.Object)
            .AddScriptProvider(mockProvider2.Object);
        
        // Then
        using (new AssertionScope())
        {
            _config.ScriptProviders.Should().Contain(mockProvider1.Object);
            _config.ScriptProviders.Should().Contain(mockProvider2.Object);
            _config.ScriptProviders.Should().HaveCount(2);
        }
    }

    [Test]
    public void AddDowngradeResolver_WhenResolverIsValid_ShouldSetDowngradeResolver()
    {
        // Given
        var mockResolver = new Mock<IDowngradeResolver>();
        
        // When
        var result = _config.AddDowngradeResolver(mockResolver.Object);
        
        // Then
        using (new AssertionScope())
        {
            result.Should().Be(_config);
            _config.DowngradeResolver.Should().Be(mockResolver.Object);
            _config.AllowDowngrades.Should().BeTrue();
        }
    }

    [Test]
    public void AddDowngradeResolver_WhenResolverIsNull_ShouldThrowArgumentNullException()
    {
        // When
        Action act = () => _config.AddDowngradeResolver(null);
        
        // Then
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("downgradeResolver");
    }

    [Test]
    public void AddDowngradeResolver_WhenCalled_ShouldEnableDowngrades()
    {
        // Given
        var mockResolver = new Mock<IDowngradeResolver>();
        _config.AllowDowngrades = false;
        
        // When
        _config.AddDowngradeResolver(mockResolver.Object);
        
        // Then
        _config.AllowDowngrades.Should().BeTrue();
    }

    [Test]
    public void AddDowngradeResolver_WhenCalledMultipleTimes_ShouldReplaceResolver()
    {
        // Given
        var mockResolver1 = new Mock<IDowngradeResolver>();
        var mockResolver2 = new Mock<IDowngradeResolver>();
        
        // When
        _config
            .AddDowngradeResolver(mockResolver1.Object)
            .AddDowngradeResolver(mockResolver2.Object);
        
        // Then
        _config.DowngradeResolver.Should().Be(mockResolver2.Object);
    }
}