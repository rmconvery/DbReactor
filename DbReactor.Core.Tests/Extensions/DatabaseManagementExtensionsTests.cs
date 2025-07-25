using DbReactor.Core.Configuration;
using DbReactor.Core.Extensions;
using DbReactor.Core.Provisioning;
using FluentAssertions;
using FluentAssertions.Execution;
using Moq;
using NUnit.Framework;
using System;

namespace DbReactor.Core.Tests.Extensions;

[TestFixture]
public class DatabaseManagementExtensionsTests
{
    private DbReactorConfiguration _config;
    
    [SetUp]
    public void SetUp()
    {
        _config = new DbReactorConfiguration();
    }

    [Test]
    public void CreateDatabaseIfNotExists_WhenCalledWithoutTemplate_ShouldEnableDatabaseCreation()
    {
        // When
        var result = _config.CreateDatabaseIfNotExists();
        
        // Then
        using (new AssertionScope())
        {
            result.Should().Be(_config);
            _config.CreateDatabaseIfNotExists.Should().BeTrue();
            _config.DatabaseCreationTemplate.Should().BeNull();
        }
    }

    [Test]
    public void CreateDatabaseIfNotExists_WhenCalledWithTemplate_ShouldSetTemplate()
    {
        // Given
        var template = "CREATE DATABASE {0}";
        
        // When
        var result = _config.CreateDatabaseIfNotExists(template);
        
        // Then
        using (new AssertionScope())
        {
            result.Should().Be(_config);
            _config.CreateDatabaseIfNotExists.Should().BeTrue();
            _config.DatabaseCreationTemplate.Should().Be(template);
        }
    }

    [Test]
    public void AddDatabaseProvisioner_WhenProvisionerIsValid_ShouldSetDatabaseProvisioner()
    {
        // Given
        var mockProvisioner = new Mock<IDatabaseProvisioner>();
        
        // When
        var result = _config.AddDatabaseProvisioner(mockProvisioner.Object);
        
        // Then
        using (new AssertionScope())
        {
            result.Should().Be(_config);
            _config.DatabaseProvisioner.Should().Be(mockProvisioner.Object);
        }
    }

    [Test]
    public void AddDatabaseProvisioner_WhenProvisionerIsNull_ShouldThrowArgumentNullException()
    {
        // When
        Action act = () => _config.AddDatabaseProvisioner(null);
        
        // Then
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("databaseProvisioner");
    }

    [Test]
    public void AddDatabaseProvisioner_WhenCalledMultipleTimes_ShouldReplaceProvisioner()
    {
        // Given
        var mockProvisioner1 = new Mock<IDatabaseProvisioner>();
        var mockProvisioner2 = new Mock<IDatabaseProvisioner>();
        
        // When
        _config
            .AddDatabaseProvisioner(mockProvisioner1.Object)
            .AddDatabaseProvisioner(mockProvisioner2.Object);
        
        // Then
        _config.DatabaseProvisioner.Should().Be(mockProvisioner2.Object);
    }

    [Test]
    public void CreateDatabaseIfNotExists_WithTemplate_AfterAddDatabaseProvisioner_ShouldMaintainProvisioner()
    {
        // Given
        var mockProvisioner = new Mock<IDatabaseProvisioner>();
        var template = "CREATE DATABASE {0} WITH OPTIONS";
        
        // When
        _config
            .AddDatabaseProvisioner(mockProvisioner.Object)
            .CreateDatabaseIfNotExists(template);
        
        // Then
        using (new AssertionScope())
        {
            _config.DatabaseProvisioner.Should().Be(mockProvisioner.Object);
            _config.CreateDatabaseIfNotExists.Should().BeTrue();
            _config.DatabaseCreationTemplate.Should().Be(template);
        }
    }
}