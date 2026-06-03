using DbReactor.Core.Abstractions;
using DbReactor.Core.Execution;
using DbReactor.Core.Logging;
using DbReactor.Core.Models;
using DbReactor.Core.Models.Scripts;
using DbReactor.MSSqlServer.Execution;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Data.SqlClient;
using Moq;
using System.Data;

namespace DbReactor.MSSqlServer.Tests.Execution;

[TestFixture]
public class SqlServerScriptExecutorInfoMessageTests
{
    private const string ValidConnectionString = "Server=localhost;Database=TestDB;Trusted_Connection=true;";

    #region Constructor Backward Compatibility

    [Test]
    public void Constructor_WithNoLogProvider_ShouldCreateExecutorWithoutError()
    {
        // When
        SqlServerScriptExecutor executor = new SqlServerScriptExecutor();

        // Then
        executor.Should().NotBeNull();
    }

    [Test]
    public void Constructor_WithTimeoutOnly_ShouldCreateExecutorWithoutError()
    {
        // When
        SqlServerScriptExecutor executor = new SqlServerScriptExecutor(TimeSpan.FromSeconds(30));

        // Then
        executor.Should().NotBeNull();
    }

    [Test]
    public void Constructor_WithNullLogProvider_ShouldCreateExecutorWithoutError()
    {
        // When
        SqlServerScriptExecutor executor = new SqlServerScriptExecutor(TimeSpan.FromSeconds(30), (ILogProvider)null);

        // Then
        executor.Should().NotBeNull();
    }

    #endregion

    #region Constructor With Logger

    [Test]
    public void Constructor_WithLogProvider_ShouldCreateExecutorWithoutError()
    {
        // Given
        Mock<ILogProvider> mockLogger = new Mock<ILogProvider>();

        // When
        SqlServerScriptExecutor executor = new SqlServerScriptExecutor(TimeSpan.FromSeconds(30), mockLogger.Object);

        // Then
        executor.Should().NotBeNull();
    }

    [Test]
    public async Task ExecuteAsync_WithLogProvider_AndConnectionFailure_ShouldReturnFailedResult()
    {
        // Given
        Mock<ILogProvider> mockLogger = new Mock<ILogProvider>();
        SqlServerScriptExecutor executor = new SqlServerScriptExecutor(TimeSpan.FromSeconds(30), mockLogger.Object);
        GenericScript script = new GenericScript("test.sql", "SELECT 1");
        InvalidOperationException expectedException = new InvalidOperationException("Connection failed");
        Mock<IConnectionManager> mockConnectionManager = new Mock<IConnectionManager>();

        mockConnectionManager.Setup(cm => cm.ExecuteWithManagedConnectionAsync(
            It.IsAny<Func<IDbConnection, Task>>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // When
        MigrationResult result = await executor.ExecuteAsync(script, mockConnectionManager.Object, CancellationToken.None);

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNull();
            result.Successful.Should().BeFalse();
            result.Error.Should().Be(expectedException);
        }
    }

    [Test]
    public async Task ExecuteAsync_WithNullLogProvider_AndConnectionFailure_ShouldNotThrowNullReference()
    {
        // Given — null log provider triggers NullLogProvider fallback
        SqlServerScriptExecutor executor = new SqlServerScriptExecutor(TimeSpan.FromSeconds(30), (ILogProvider)null);
        GenericScript script = new GenericScript("test.sql", "SELECT 1");
        InvalidOperationException expectedException = new InvalidOperationException("Connection failed");
        Mock<IConnectionManager> mockConnectionManager = new Mock<IConnectionManager>();

        mockConnectionManager.Setup(cm => cm.ExecuteWithManagedConnectionAsync(
            It.IsAny<Func<IDbConnection, Task>>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // When
        MigrationResult result = await executor.ExecuteAsync(script, mockConnectionManager.Object, CancellationToken.None);

        // Then — should fail gracefully, not throw NullReferenceException
        using (new AssertionScope())
        {
            result.Should().NotBeNull();
            result.Successful.Should().BeFalse();
            result.Error.Should().Be(expectedException);
            result.Error.Should().NotBeOfType<NullReferenceException>();
        }
    }

    #endregion

    #region InfoMessage Routing (Integration - requires SQL Server)

    [Test]
    public async Task ExecuteAsync_WithPrintStatement_ShouldRouteMessageToLogProvider()
    {
        // Given
        if (!IsConnectionStringValid(ValidConnectionString))
        {
            Assert.Ignore("SQL Server not available — skipping integration test");
            return;
        }

        Mock<ILogProvider> mockLogger = new Mock<ILogProvider>();
        SqlServerScriptExecutor executor = new SqlServerScriptExecutor(TimeSpan.FromSeconds(30), mockLogger.Object);
        GenericScript script = new GenericScript("print-test.sql", "PRINT 'hello from info message'");
        DbReactor.MSSqlServer.Execution.DbReactor.MSSqlServer.Implementations.Execution.SqlServerConnectionManager connectionManager =
            new DbReactor.MSSqlServer.Execution.DbReactor.MSSqlServer.Implementations.Execution.SqlServerConnectionManager(ValidConnectionString);

        // When
        MigrationResult result = await executor.ExecuteAsync(script, connectionManager, CancellationToken.None);

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNull();
            result.Successful.Should().BeTrue();
            mockLogger.Verify(
                l => l.WriteInformation(It.Is<string>(msg => msg.Contains("hello from info message")), It.IsAny<object[]>()),
                Times.AtLeastOnce());
        }
    }

    [Test]
    public async Task ExecuteAsync_WithMultiplePrintStatements_ShouldRouteAllMessagesToLogProvider()
    {
        // Given
        if (!IsConnectionStringValid(ValidConnectionString))
        {
            Assert.Ignore("SQL Server not available — skipping integration test");
            return;
        }

        Mock<ILogProvider> mockLogger = new Mock<ILogProvider>();
        SqlServerScriptExecutor executor = new SqlServerScriptExecutor(TimeSpan.FromSeconds(30), mockLogger.Object);
        string scriptContent = @"
PRINT 'message one'
PRINT 'message two'
PRINT 'message three'
";
        GenericScript script = new GenericScript("multi-print.sql", scriptContent);
        DbReactor.MSSqlServer.Execution.DbReactor.MSSqlServer.Implementations.Execution.SqlServerConnectionManager connectionManager =
            new DbReactor.MSSqlServer.Execution.DbReactor.MSSqlServer.Implementations.Execution.SqlServerConnectionManager(ValidConnectionString);

        // When
        MigrationResult result = await executor.ExecuteAsync(script, connectionManager, CancellationToken.None);

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNull();
            result.Successful.Should().BeTrue();
            mockLogger.Verify(
                l => l.WriteInformation(It.IsAny<string>(), It.IsAny<object[]>()),
                Times.AtLeast(3));
        }
    }

    [Test]
    public async Task ExecuteAsync_WithPrintInBatchedScript_ShouldRouteMessageToLogProvider()
    {
        // Given
        if (!IsConnectionStringValid(ValidConnectionString))
        {
            Assert.Ignore("SQL Server not available — skipping integration test");
            return;
        }

        Mock<ILogProvider> mockLogger = new Mock<ILogProvider>();
        SqlServerScriptExecutor executor = new SqlServerScriptExecutor(TimeSpan.FromSeconds(30), mockLogger.Object);
        string scriptContent = @"
PRINT 'batch one start'
SELECT 1
GO
PRINT 'batch two start'
SELECT 2
";
        GenericScript script = new GenericScript("batched-print.sql", scriptContent);
        DbReactor.MSSqlServer.Execution.DbReactor.MSSqlServer.Implementations.Execution.SqlServerConnectionManager connectionManager =
            new DbReactor.MSSqlServer.Execution.DbReactor.MSSqlServer.Implementations.Execution.SqlServerConnectionManager(ValidConnectionString);

        // When
        MigrationResult result = await executor.ExecuteAsync(script, connectionManager, CancellationToken.None);

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNull();
            result.Successful.Should().BeTrue();
            mockLogger.Verify(
                l => l.WriteInformation(It.Is<string>(msg => msg.Contains("batch one start")), It.IsAny<object[]>()),
                Times.AtLeastOnce());
            mockLogger.Verify(
                l => l.WriteInformation(It.Is<string>(msg => msg.Contains("batch two start")), It.IsAny<object[]>()),
                Times.AtLeastOnce());
        }
    }

    [Test]
    public async Task ExecuteAsync_WithRaiserrorInfoMessage_ShouldRouteMessageToLogProvider()
    {
        // Given
        if (!IsConnectionStringValid(ValidConnectionString))
        {
            Assert.Ignore("SQL Server not available — skipping integration test");
            return;
        }

        Mock<ILogProvider> mockLogger = new Mock<ILogProvider>();
        SqlServerScriptExecutor executor = new SqlServerScriptExecutor(TimeSpan.FromSeconds(30), mockLogger.Object);
        // RAISERROR with severity 0-10 routes through InfoMessage, not as an exception
        GenericScript script = new GenericScript("raiserror-info.sql", "RAISERROR('informational message', 0, 1) WITH NOWAIT");
        DbReactor.MSSqlServer.Execution.DbReactor.MSSqlServer.Implementations.Execution.SqlServerConnectionManager connectionManager =
            new DbReactor.MSSqlServer.Execution.DbReactor.MSSqlServer.Implementations.Execution.SqlServerConnectionManager(ValidConnectionString);

        // When
        MigrationResult result = await executor.ExecuteAsync(script, connectionManager, CancellationToken.None);

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNull();
            result.Successful.Should().BeTrue();
            mockLogger.Verify(
                l => l.WriteInformation(It.Is<string>(msg => msg.Contains("informational message")), It.IsAny<object[]>()),
                Times.AtLeastOnce());
        }
    }

    [Test]
    public async Task ExecuteAsync_WithNoLogProvider_AndPrintStatement_ShouldNotThrow()
    {
        // Given — default constructor uses NullLogProvider
        if (!IsConnectionStringValid(ValidConnectionString))
        {
            Assert.Ignore("SQL Server not available — skipping integration test");
            return;
        }

        SqlServerScriptExecutor executor = new SqlServerScriptExecutor();
        GenericScript script = new GenericScript("print-null-logger.sql", "PRINT 'this should not crash'");
        DbReactor.MSSqlServer.Execution.DbReactor.MSSqlServer.Implementations.Execution.SqlServerConnectionManager connectionManager =
            new DbReactor.MSSqlServer.Execution.DbReactor.MSSqlServer.Implementations.Execution.SqlServerConnectionManager(ValidConnectionString);

        // When
        MigrationResult result = await executor.ExecuteAsync(script, connectionManager, CancellationToken.None);

        // Then — NullLogProvider absorbs the message without throwing
        using (new AssertionScope())
        {
            result.Should().NotBeNull();
            result.Successful.Should().BeTrue();
            result.Error.Should().BeNull();
        }
    }

    #endregion

    private static bool IsConnectionStringValid(string connectionString)
    {
        try
        {
            using SqlConnection connection = new SqlConnection(connectionString);
            connection.Open();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
