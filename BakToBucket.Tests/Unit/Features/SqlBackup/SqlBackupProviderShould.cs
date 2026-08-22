using BakToBucket.Features.SqlServerBackup;
using FluentAssertions;
using Xunit;

namespace BakToBucket.Tests.Unit.Features.SqlBackup;

public class SqlBackupProviderShould
{
    [Theory]
    [InlineData("MainDB")]
    [InlineData("User_Database")]
    [InlineData("Backup-2026")]
    [InlineData("db123")]
    public void AllowValidDatabaseNames(string dbName)
    {
        // Act
        var act = () => SqlBackupProvider.ValidateDatabaseName(dbName);

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void ThrowException_WhenDatabaseNameIsEmpty(string? dbName)
    {
        // Act
        var act = () => SqlBackupProvider.ValidateDatabaseName(dbName!);

        // Assert
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("Database name cannot be empty.");
    }

    [Theory]
    [InlineData("DB; DROP TABLE Users")]
    [InlineData("DB'--")]
    [InlineData("DB [Master]")]
    [InlineData("Database Name")] // Spaces not allowed by current regex
    public void ThrowException_WhenDatabaseNameIsInvalid(string dbName)
    {
        // Act
        var act = () => SqlBackupProvider.ValidateDatabaseName(dbName);

        // Assert
        act.Should().Throw<InvalidOperationException>()
           .WithMessage($"Invalid database name: {dbName}");
    }

    [Theory]
    [InlineData("/var/opt/mssql/backup", "TestDb", "20260818_120000", "/var/opt/mssql/backup/TestDb_20260818_120000.bak")]
    [InlineData("/var/opt/mssql/backup/", "TestDb", "20260818_120000", "/var/opt/mssql/backup/TestDb_20260818_120000.bak")]
    [InlineData(@"C:\Backups", "TestDb", "20260818_120000", @"C:\Backups\TestDb_20260818_120000.bak")]
    [InlineData(@"C:\Backups\", "TestDb", "20260818_120000", @"C:\Backups\TestDb_20260818_120000.bak")]
    public void BuildBackupFilePath_ConstructsCorrectPath_ForLinuxAndWindows(string folder, string db, string timestamp, string expected)
    {
        var result = SqlBackupProvider.BuildBackupFilePath(folder, db, timestamp);
        result.Should().Be(expected);
    }
}

