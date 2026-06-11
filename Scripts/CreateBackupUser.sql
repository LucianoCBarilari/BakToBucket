/*

# SQL Server - Development Backup User Setup

PURPOSE
Creates a SQL Server login with permissions required to discover databases
and perform database backups on all online non-system databases.

IMPORTANT

* Intended for development and non-production environments only.
* Replace <REPLACE_WITH_STRONG_PASSWORD> with a secure password before use.
* Do not commit real credentials to source control.
* Ensure the SQL Server service account has write access to the backup folder.
* Review permissions before using in shared environments.

INSTRUCTIONS

1. Replace <REPLACE_WITH_STRONG_PASSWORD> with a strong password.
2. Execute as a sysadmin user in SSMS or Azure Data Studio.
3. Update your application's connection string with the configured password.

=============================================================================
*/

USE [master];
GO

-- Creates the server login if it does not already exist
IF NOT EXISTS (
SELECT 1
FROM sys.server_principals
WHERE name = 'backup_user'
)
BEGIN
PRINT 'Creating login backup_user...';

```
CREATE LOGIN [backup_user]
WITH PASSWORD = '<REPLACE_WITH_STRONG_PASSWORD>',
     CHECK_POLICY = OFF,
     CHECK_EXPIRATION = OFF;
```

END
ELSE
BEGIN
PRINT 'Login backup_user already exists.';
END
GO

-- Allows the login to discover available databases
GRANT VIEW ANY DATABASE TO [backup_user];
GO

# /*

Maps the login to every online non-system database and grants membership in
the db_backupoperator role, allowing database backups.
======================================================

*/

DECLARE @sql NVARCHAR(MAX);
DECLARE @dbName SYSNAME;

DECLARE db_cursor CURSOR FOR
SELECT name
FROM sys.databases
WHERE state_desc = 'ONLINE'
AND name NOT IN ('master', 'tempdb', 'model', 'msdb');

OPEN db_cursor;

FETCH NEXT FROM db_cursor INTO @dbName;

WHILE @@FETCH_STATUS = 0
BEGIN
SET @sql = N'
USE [' + REPLACE(@dbName, ']', ']]') + N'];

```
    IF NOT EXISTS (
        SELECT 1
        FROM sys.database_principals
        WHERE name = ''backup_user''
    )
    BEGIN
        PRINT ''Creating user in ' + REPLACE(@dbName, '''', '''''') + N'...'';

        CREATE USER [backup_user]
        FOR LOGIN [backup_user];
    END

    ALTER ROLE [db_backupoperator]
    ADD MEMBER [backup_user];
';

EXEC sp_executesql @sql;

FETCH NEXT FROM db_cursor INTO @dbName;
```

END

CLOSE db_cursor;
DEALLOCATE db_cursor;

PRINT 'Setup completed successfully.';
PRINT 'The login backup_user can discover databases and perform backups.';
PRINT '';
PRINT 'Example connection string:';
PRINT 'Server=localhost;Database=master;User Id=backup_user;Password=<YOUR_PASSWORD>;TrustServerCertificate=True;';
GO
