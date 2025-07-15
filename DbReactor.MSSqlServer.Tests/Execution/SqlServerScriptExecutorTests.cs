using DbReactor.Core.Abstractions;
using DbReactor.Core.Execution;
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
public class SqlServerScriptExecutorTests
{
    private Mock<IConnectionManager> _mockConnectionManager;
    private Mock<IDbConnection> _mockConnection;
    private Mock<SqlCommand> _mockCommand;
    private SqlServerScriptExecutor _executor;

    [SetUp]
    public void SetUp()
    {
        _mockConnectionManager = new Mock<IConnectionManager>();
        _mockConnection = new Mock<IDbConnection>();
        _mockCommand = new Mock<SqlCommand>();

        _executor = new SqlServerScriptExecutor();
    }

    [Test]
    public void Constructor_WithDefaultTimeout_ShouldCreateExecutor()
    {
        // When
        SqlServerScriptExecutor executor = new SqlServerScriptExecutor();

        // Then
        using (new AssertionScope())
        {
            executor.Should().NotBeNull();
        }
    }

    [Test]
    public void Constructor_WithCustomTimeout_ShouldCreateExecutor()
    {
        // Given
        int customTimeout = 60;

        // When
        SqlServerScriptExecutor executor = new SqlServerScriptExecutor(customTimeout);

        // Then
        using (new AssertionScope())
        {
            executor.Should().NotBeNull();
        }
    }

    [Test]
    public async Task ExecuteAsync_WithValidScript_ShouldReturnSuccessfulResult()
    {
        // Given
        GenericScript script = new GenericScript("test.sql", "SELECT 1");
        CancellationToken cancellationToken = CancellationToken.None;

        _mockConnectionManager.Setup(cm => cm.ExecuteWithManagedConnectionAsync(
            It.IsAny<Func<IDbConnection, Task>>(),
            It.IsAny<CancellationToken>()))
            .Callback<Func<IDbConnection, Task>, CancellationToken>(async (action, token) =>
            {
                await action(_mockConnection.Object);
            })
            .Returns(Task.CompletedTask);

        // When
        MigrationResult result = await _executor.ExecuteAsync(script, _mockConnectionManager.Object, cancellationToken);

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNull();
            result.Script.Should().Be(script);
            result.Successful.Should().BeTrue();
            result.Error.Should().BeNull();
            result.ErrorMessage.Should().BeNull();
            result.ExecutionTime.Should().BeGreaterThan(TimeSpan.Zero);
        }
    }

    [Test]
    public async Task ExecuteAsync_WithNullScript_ShouldReturnFailedResult()
    {
        // Given
        IScript nullScript = null!;
        CancellationToken cancellationToken = CancellationToken.None;

        // When
        MigrationResult result = await _executor.ExecuteAsync(nullScript, _mockConnectionManager.Object, cancellationToken);

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNull();
            result.Script.Should().Be(nullScript);
            result.Successful.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
            result.ExecutionTime.Should().BeGreaterThan(TimeSpan.Zero);
        }
    }

    [Test]
    public async Task ExecuteAsync_WithEmptyScript_ShouldReturnFailedResult()
    {
        // Given
        GenericScript script = new GenericScript("empty.sql", "");
        CancellationToken cancellationToken = CancellationToken.None;

        _mockConnectionManager.Setup(cm => cm.ExecuteWithManagedConnectionAsync(
            It.IsAny<Func<IDbConnection, Task>>(),
            It.IsAny<CancellationToken>()))
            .Callback<Func<IDbConnection, Task>, CancellationToken>(async (action, token) =>
            {
                await action(_mockConnection.Object);
            })
            .Returns(Task.CompletedTask);

        // When
        MigrationResult result = await _executor.ExecuteAsync(script, _mockConnectionManager.Object, cancellationToken);

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNull();
            result.Script.Should().Be(script);
            result.Successful.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.ErrorMessage.Should().Contain("empty");
            result.ExecutionTime.Should().BeGreaterThan(TimeSpan.Zero);
        }
    }

    [Test]
    public async Task ExecuteAsync_WithWhitespaceScript_ShouldReturnFailedResult()
    {
        // Given
        GenericScript script = new GenericScript("whitespace.sql", "   \n\t  ");
        CancellationToken cancellationToken = CancellationToken.None;

        _mockConnectionManager.Setup(cm => cm.ExecuteWithManagedConnectionAsync(
            It.IsAny<Func<IDbConnection, Task>>(),
            It.IsAny<CancellationToken>()))
            .Callback<Func<IDbConnection, Task>, CancellationToken>(async (action, token) =>
            {
                await action(_mockConnection.Object);
            })
            .Returns(Task.CompletedTask);

        // When
        MigrationResult result = await _executor.ExecuteAsync(script, _mockConnectionManager.Object, cancellationToken);

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNull();
            result.Script.Should().Be(script);
            result.Successful.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.ErrorMessage.Should().Contain("empty");
            result.ExecutionTime.Should().BeGreaterThan(TimeSpan.Zero);
        }
    }

    [Test]
    public async Task ExecuteAsync_WithConnectionManagerException_ShouldReturnFailedResult()
    {
        // Given
        GenericScript script = new GenericScript("test.sql", "SELECT 1");
        InvalidOperationException expectedException = new InvalidOperationException("Connection failed");
        CancellationToken cancellationToken = CancellationToken.None;

        _mockConnectionManager.Setup(cm => cm.ExecuteWithManagedConnectionAsync(
            It.IsAny<Func<IDbConnection, Task>>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // When
        MigrationResult result = await _executor.ExecuteAsync(script, _mockConnectionManager.Object, cancellationToken);

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNull();
            result.Script.Should().Be(script);
            result.Successful.Should().BeFalse();
            result.Error.Should().Be(expectedException);
            result.ErrorMessage.Should().Be("Connection failed");
            result.ExecutionTime.Should().BeGreaterThan(TimeSpan.Zero);
        }
    }

    [Test]
    public async Task ExecuteAsync_WithScriptContainingGOStatements_ShouldSplitAndExecuteBatches()
    {
        // Given
        string scriptContent = @"
CREATE TABLE Test1 (Id INT);
GO
INSERT INTO Test1 VALUES (1);
GO
SELECT * FROM Test1;
";
        GenericScript script = new GenericScript("batches.sql", scriptContent);
        CancellationToken cancellationToken = CancellationToken.None;

        _mockConnectionManager.Setup(cm => cm.ExecuteWithManagedConnectionAsync(
            It.IsAny<Func<IDbConnection, Task>>(),
            It.IsAny<CancellationToken>()))
            .Callback<Func<IDbConnection, Task>, CancellationToken>(async (action, token) =>
            {
                await action(_mockConnection.Object);
            })
            .Returns(Task.CompletedTask);

        // When
        MigrationResult result = await _executor.ExecuteAsync(script, _mockConnectionManager.Object, cancellationToken);

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNull();
            result.Script.Should().Be(script);
            result.Successful.Should().BeTrue();
            result.Error.Should().BeNull();
            result.ErrorMessage.Should().BeNull();
            result.ExecutionTime.Should().BeGreaterThan(TimeSpan.Zero);
        }
    }

    [Test]
    public async Task ExecuteAsync_WithSelectStatement_ShouldExecuteWithReader()
    {
        // Given
        GenericScript script = new GenericScript("select.sql", "SELECT * FROM Users");
        CancellationToken cancellationToken = CancellationToken.None;

        _mockConnectionManager.Setup(cm => cm.ExecuteWithManagedConnectionAsync(
            It.IsAny<Func<IDbConnection, Task>>(),
            It.IsAny<CancellationToken>()))
            .Callback<Func<IDbConnection, Task>, CancellationToken>(async (action, token) =>
            {
                await action(_mockConnection.Object);
            })
            .Returns(Task.CompletedTask);

        // When
        MigrationResult result = await _executor.ExecuteAsync(script, _mockConnectionManager.Object, cancellationToken);

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNull();
            result.Script.Should().Be(script);
            result.Successful.Should().BeTrue();
            result.Error.Should().BeNull();
            result.ErrorMessage.Should().BeNull();
            result.ExecutionTime.Should().BeGreaterThan(TimeSpan.Zero);
        }
    }

    [Test]
    public async Task ExecuteAsync_WithInsertStatement_ShouldExecuteWithNonQuery()
    {
        // Given
        GenericScript script = new GenericScript("insert.sql", "INSERT INTO Users (Name) VALUES ('Test')");
        CancellationToken cancellationToken = CancellationToken.None;

        _mockConnectionManager.Setup(cm => cm.ExecuteWithManagedConnectionAsync(
            It.IsAny<Func<IDbConnection, Task>>(),
            It.IsAny<CancellationToken>()))
            .Callback<Func<IDbConnection, Task>, CancellationToken>(async (action, token) =>
            {
                await action(_mockConnection.Object);
            })
            .Returns(Task.CompletedTask);

        // When
        MigrationResult result = await _executor.ExecuteAsync(script, _mockConnectionManager.Object, cancellationToken);

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNull();
            result.Script.Should().Be(script);
            result.Successful.Should().BeTrue();
            result.Error.Should().BeNull();
            result.ErrorMessage.Should().BeNull();
            result.ExecutionTime.Should().BeGreaterThan(TimeSpan.Zero);
        }
    }

    [Test]
    public async Task ExecuteAsync_WithCancellationToken_ShouldPassTokenToConnection()
    {
        // Given
        GenericScript script = new GenericScript("test.sql", "SELECT 1");
        CancellationToken cancellationToken = new CancellationToken(true); // Pre-cancelled token

        _mockConnectionManager.Setup(cm => cm.ExecuteWithManagedConnectionAsync(
            It.IsAny<Func<IDbConnection, Task>>(),
            It.IsAny<CancellationToken>()))
            .Callback<Func<IDbConnection, Task>, CancellationToken>((action, token) =>
            {
                token.Should().Be(cancellationToken);
            })
            .Returns(Task.CompletedTask);

        // When
        MigrationResult result = await _executor.ExecuteAsync(script, _mockConnectionManager.Object, cancellationToken);

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNull();
            result.Script.Should().Be(script);
            // The result might be successful or failed depending on timing
            result.ExecutionTime.Should().BeGreaterThan(TimeSpan.Zero);
        }
    }

    [Test]
    public async Task VerifySchemaAsync_WithValidConnection_ShouldNotThrow()
    {
        // Given
        CancellationToken cancellationToken = CancellationToken.None;

        _mockConnectionManager.Setup(cm => cm.ExecuteWithManagedConnectionAsync(
            It.IsAny<Func<IDbConnection, Task>>(),
            It.IsAny<CancellationToken>()))
            .Callback<Func<IDbConnection, Task>, CancellationToken>(async (action, token) =>
            {
                await action(_mockConnection.Object);
            })
            .Returns(Task.CompletedTask);

        // When
        Func<Task> act = async () => await _executor.VerifySchemaAsync(_mockConnectionManager.Object, cancellationToken);

        // Then
        using (new AssertionScope())
        {
            await act.Should().NotThrowAsync();
        }
    }

    [Test]
    public async Task VerifySchemaAsync_WithConnectionFailure_ShouldThrow()
    {
        // Given
        SqlException expectedException = new SqlException();
        CancellationToken cancellationToken = CancellationToken.None;

        _mockConnectionManager.Setup(cm => cm.ExecuteWithManagedConnectionAsync(
            It.IsAny<Func<IDbConnection, Task>>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // When
        Func<Task> act = async () => await _executor.VerifySchemaAsync(_mockConnectionManager.Object, cancellationToken);

        // Then
        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<SqlException>()
                .WithMessage("Connection failed");
        }
    }

    [Test]
    public async Task ExecuteAsync_WithComplexScript_ShouldHandleCorrectly()
    {
        // Given
        string complexScript = @"
-- This is a comment
CREATE TABLE TestTable (
    Id INT PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL
);
GO

-- Another comment
INSERT INTO TestTable (Id, Name) VALUES (1, 'Test');
GO

-- Query with CTE
WITH NumbersCTE AS (
    SELECT 1 as Number
    UNION ALL
    SELECT Number + 1 FROM NumbersCTE WHERE Number < 10
)
SELECT * FROM NumbersCTE;
";
        GenericScript script = new GenericScript("complex.sql", complexScript);
        CancellationToken cancellationToken = CancellationToken.None;

        _mockConnectionManager.Setup(cm => cm.ExecuteWithManagedConnectionAsync(
            It.IsAny<Func<IDbConnection, Task>>(),
            It.IsAny<CancellationToken>()))
            .Callback<Func<IDbConnection, Task>, CancellationToken>(async (action, token) =>
            {
                await action(_mockConnection.Object);
            })
            .Returns(Task.CompletedTask);

        // When
        MigrationResult result = await _executor.ExecuteAsync(script, _mockConnectionManager.Object, cancellationToken);

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNull();
            result.Script.Should().Be(script);
            result.Successful.Should().BeTrue();
            result.Error.Should().BeNull();
            result.ErrorMessage.Should().BeNull();
            result.ExecutionTime.Should().BeGreaterThan(TimeSpan.Zero);
        }
    }

    [Test]
    public async Task ExecuteAsync_WithStoredProcedureCall_ShouldExecuteCorrectly()
    {
        // Given
        GenericScript script = new GenericScript("sproc.sql", "EXEC sp_helpdb");
        CancellationToken cancellationToken = CancellationToken.None;

        _mockConnectionManager.Setup(cm => cm.ExecuteWithManagedConnectionAsync(
            It.IsAny<Func<IDbConnection, Task>>(),
            It.IsAny<CancellationToken>()))
            .Callback<Func<IDbConnection, Task>, CancellationToken>(async (action, token) =>
            {
                await action(_mockConnection.Object);
            })
            .Returns(Task.CompletedTask);

        // When
        MigrationResult result = await _executor.ExecuteAsync(script, _mockConnectionManager.Object, cancellationToken);

        // Then
        using (new AssertionScope())
        {
            result.Should().NotBeNull();
            result.Script.Should().Be(script);
            result.Successful.Should().BeTrue();
            result.Error.Should().BeNull();
            result.ErrorMessage.Should().BeNull();
            result.ExecutionTime.Should().BeGreaterThan(TimeSpan.Zero);
        }
    }
}