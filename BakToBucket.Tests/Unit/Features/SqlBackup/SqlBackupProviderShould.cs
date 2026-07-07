using BakToBucket.Features.SqlBackup;
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
    [InlineData("Base Datos")] // Spaces not allowed by current regex
    public void ThrowException_WhenDatabaseNameIsInvalid(string dbName)
    {
        // Act
        var act = () => SqlBackupProvider.ValidateDatabaseName(dbName);

        // Assert
        act.Should().Throw<InvalidOperationException>()
           .WithMessage($"Invalid database name: {dbName}");
    }
}

