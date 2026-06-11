/*
  =============================================================================
  SQL Server - Dev Read-Only User Setup
  =============================================================================
  Creates a login with read-only access to all non-system databases.
  Intended for local development environments only.

  INSTRUCTIONS:
  1. Replace 'DevPassword123!' with your preferred password.
  2. Run in SSMS or Azure Data Studio as sa or sysadmin.
  =============================================================================
*/

USE [master];
GO

-- Creates the server login if it doesn't already exist
IF NOT EXISTS (SELECT * FROM sys.server_principals WHERE name = 'dev_readonly')
BEGIN
    PRINT 'Creating login dev_readonly...';
    CREATE LOGIN [dev_readonly]
    WITH PASSWORD        = 'DevPassword123!',
         CHECK_POLICY    = OFF,
         CHECK_EXPIRATION = OFF;
END
ELSE
    PRINT 'Login dev_readonly already exists.';
GO

-- Allows the login to see all database names on the server
GRANT VIEW ANY DATABASE TO [dev_readonly];
GO

-- ============================================================================
-- Iterates every online non-system database and maps the login as a
-- db_datareader user, granting SELECT on all tables and views
-- ============================================================================
DECLARE @sql    NVARCHAR(MAX);
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
    -- Creates the database user if missing, then assigns the read-only role
    SET @sql = N'
        USE [' + @dbName + N'];
        IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = ''dev_readonly'')
        BEGIN
            PRINT ''Creating user in ' + @dbName + N'...'';
            CREATE USER [dev_readonly] FOR LOGIN [dev_readonly];
        END
        ALTER ROLE [db_datareader] ADD MEMBER [dev_readonly];
    ';

    EXEC sp_executesql @sql;

    FETCH NEXT FROM db_cursor INTO @dbName;
END

CLOSE db_cursor;
DEALLOCATE db_cursor;

PRINT 'Done. dev_readonly has db_datareader on all online databases.';
PRINT 'Connection string:';
PRINT 'Server=localhost;Database=<db>;User Id=dev_readonly;Password=DevPassword123!;TrustServerCertificate=True;';