using DbReactor.Core.Abstractions;
using DbReactor.Core.Extensions;
using DbReactor.Core.Models;

namespace DbReactor.Core.Tests.Extensions;

[TestFixture]
public class DbReactorEngineExtensionsTests
{
    private Mock<IDbReactorEngine> _mockEngine;

    [SetUp]
    public void SetUp()
    {
        _mockEngine = new Mock<IDbReactorEngine>();
    }

    [Test]
    public void Run_WhenCalled_ShouldCallRunAsyncAndReturnResult()
    {
        // Given
        var expectedResult = new DbReactorResult { Successful = true };
        _mockEngine.Setup(e => e.RunAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // When
        var result = _mockEngine.Object.Run();

        // Then
        using (new AssertionScope())
        {
            result.Should().Be(expectedResult);
            result.Successful.Should().BeTrue();
        }
        
        _mockEngine.Verify(e => e.RunAsync(CancellationToken.None), Times.Once);
    }

    [Test]
    public void ApplyUpgrades_WhenCalled_ShouldCallApplyUpgradesAsyncAndReturnResult()
    {
        // Given
        var expectedResult = new DbReactorResult { Successful = true };
        _mockEngine.Setup(e => e.ApplyUpgradesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // When
        var result = _mockEngine.Object.ApplyUpgrades();

        // Then
        using (new AssertionScope())
        {
            result.Should().Be(expectedResult);
            result.Successful.Should().BeTrue();
        }
        
        _mockEngine.Verify(e => e.ApplyUpgradesAsync(CancellationToken.None), Times.Once);
    }

    [Test]
    public void ApplyDowngrades_WhenCalled_ShouldCallApplyDowngradesAsyncAndReturnResult()
    {
        // Given
        var expectedResult = new DbReactorResult { Successful = true };
        _mockEngine.Setup(e => e.ApplyDowngradesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // When
        var result = _mockEngine.Object.ApplyDowngrades();

        // Then
        using (new AssertionScope())
        {
            result.Should().Be(expectedResult);
            result.Successful.Should().BeTrue();
        }
        
        _mockEngine.Verify(e => e.ApplyDowngradesAsync(CancellationToken.None), Times.Once);
    }

    [Test]
    public void HasPendingUpgrades_WhenCalled_ShouldCallHasPendingUpgradesAsyncAndReturnResult()
    {
        // Given
        _mockEngine.Setup(e => e.HasPendingUpgradesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // When
        var result = _mockEngine.Object.HasPendingUpgrades();

        // Then
        using (new AssertionScope())
        {
            result.Should().BeTrue();
        }
        
        _mockEngine.Verify(e => e.HasPendingUpgradesAsync(CancellationToken.None), Times.Once);
    }

    [Test]
    public void Run_WhenAsyncMethodThrowsException_ShouldPropagateException()
    {
        // Given
        var expectedException = new InvalidOperationException("Test exception");
        _mockEngine.Setup(e => e.RunAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // When
        Action act = () => _mockEngine.Object.Run();

        // Then
        using (new AssertionScope())
        {
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Test exception");
        }
    }

    [Test]
    public void ApplyUpgrades_WhenAsyncMethodThrowsException_ShouldPropagateException()
    {
        // Given
        var expectedException = new InvalidOperationException("Test exception");
        _mockEngine.Setup(e => e.ApplyUpgradesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // When
        Action act = () => _mockEngine.Object.ApplyUpgrades();

        // Then
        using (new AssertionScope())
        {
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Test exception");
        }
    }

    [Test]
    public void ApplyDowngrades_WhenAsyncMethodThrowsException_ShouldPropagateException()
    {
        // Given
        var expectedException = new InvalidOperationException("Test exception");
        _mockEngine.Setup(e => e.ApplyDowngradesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // When
        Action act = () => _mockEngine.Object.ApplyDowngrades();

        // Then
        using (new AssertionScope())
        {
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Test exception");
        }
    }

    [Test]
    public void HasPendingUpgrades_WhenAsyncMethodThrowsException_ShouldPropagateException()
    {
        // Given
        var expectedException = new InvalidOperationException("Test exception");
        _mockEngine.Setup(e => e.HasPendingUpgradesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // When
        Action act = () => _mockEngine.Object.HasPendingUpgrades();

        // Then
        using (new AssertionScope())
        {
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Test exception");
        }
    }

    [Test]
    public void Run_WhenAsyncMethodReturnsFailedResult_ShouldReturnFailedResult()
    {
        // Given
        var failedResult = new DbReactorResult 
        { 
            Successful = false, 
            ErrorMessage = "Migration failed" 
        };
        _mockEngine.Setup(e => e.RunAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(failedResult);

        // When
        var result = _mockEngine.Object.Run();

        // Then
        using (new AssertionScope())
        {
            result.Should().Be(failedResult);
            result.Successful.Should().BeFalse();
            result.ErrorMessage.Should().Be("Migration failed");
        }
    }

    [Test]
    public void ApplyUpgrades_WhenAsyncMethodReturnsFailedResult_ShouldReturnFailedResult()
    {
        // Given
        var failedResult = new DbReactorResult 
        { 
            Successful = false, 
            ErrorMessage = "Upgrade failed" 
        };
        _mockEngine.Setup(e => e.ApplyUpgradesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(failedResult);

        // When
        var result = _mockEngine.Object.ApplyUpgrades();

        // Then
        using (new AssertionScope())
        {
            result.Should().Be(failedResult);
            result.Successful.Should().BeFalse();
            result.ErrorMessage.Should().Be("Upgrade failed");
        }
    }

    [Test]
    public void ApplyDowngrades_WhenAsyncMethodReturnsFailedResult_ShouldReturnFailedResult()
    {
        // Given
        var failedResult = new DbReactorResult 
        { 
            Successful = false, 
            ErrorMessage = "Downgrade failed" 
        };
        _mockEngine.Setup(e => e.ApplyDowngradesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(failedResult);

        // When
        var result = _mockEngine.Object.ApplyDowngrades();

        // Then
        using (new AssertionScope())
        {
            result.Should().Be(failedResult);
            result.Successful.Should().BeFalse();
            result.ErrorMessage.Should().Be("Downgrade failed");
        }
    }
}