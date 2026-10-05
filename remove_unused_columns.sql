-- Remove unused columns from Duyurular table
-- This script removes the Icerik, Ikon, and Kategori columns that are not used in the current model

USE WebPortalDb;
GO

PRINT 'Starting column removal process...';

-- Remove Icerik column if it exists
IF COL_LENGTH('Duyurular', 'Icerik') IS NOT NULL
BEGIN
    ALTER TABLE Duyurular DROP COLUMN Icerik;
    PRINT 'Icerik column removed successfully.';
END
ELSE
BEGIN
    PRINT 'Icerik column does not exist.';
END

-- Remove Ikon column if it exists
IF COL_LENGTH('Duyurular', 'Ikon') IS NOT NULL
BEGIN
    -- Find and remove all constraints on Ikon column
    DECLARE @sql NVARCHAR(MAX);
    
    -- Remove default constraints
    SELECT @sql = 'ALTER TABLE Duyurular DROP CONSTRAINT ' + QUOTENAME(dc.name)
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c ON dc.parent_object_id = c.object_id
    WHERE c.object_id = OBJECT_ID('Duyurular') AND c.name = 'Ikon';
    
    IF @sql IS NOT NULL
    BEGIN
        BEGIN TRY
            EXEC sp_executesql @sql;
            PRINT 'Ikon default constraint removed: ' + @sql;
        END TRY
        BEGIN CATCH
            PRINT 'Warning: Could not remove default constraint: ' + @sql;
        END CATCH
    END
    
    -- Remove check constraints
    SELECT @sql = 'ALTER TABLE Duyurular DROP CONSTRAINT ' + QUOTENAME(cc.name)
    FROM sys.check_constraints cc
    INNER JOIN sys.columns c ON cc.parent_object_id = c.object_id
    WHERE c.object_id = OBJECT_ID('Duyurular') AND c.name = 'Ikon';
    
    IF @sql IS NOT NULL
    BEGIN
        BEGIN TRY
            EXEC sp_executesql @sql;
            PRINT 'Ikon check constraint removed: ' + @sql;
        END TRY
        BEGIN CATCH
            PRINT 'Warning: Could not remove check constraint: ' + @sql;
        END CATCH
    END
    
    -- Try to remove the column with CASCADE if possible
    BEGIN TRY
        ALTER TABLE Duyurular DROP COLUMN Ikon;
        PRINT 'Ikon column removed successfully.';
    END TRY
    BEGIN CATCH
        PRINT 'Error removing Ikon column: ' + ERROR_MESSAGE();
        PRINT 'Trying alternative approach...';
        
        -- Alternative: Drop all constraints first, then column
        DECLARE @dropSQL NVARCHAR(MAX);
        SELECT @dropSQL = STRING_AGG('ALTER TABLE Duyurular DROP CONSTRAINT ' + QUOTENAME(name), '; ')
        FROM sys.objects 
        WHERE parent_object_id = OBJECT_ID('Duyurular') 
        AND type IN ('D', 'C', 'F', 'PK', 'UQ')
        AND name LIKE '%Ikon%';
        
        IF @dropSQL IS NOT NULL
        BEGIN
            EXEC sp_executesql @dropSQL;
            PRINT 'All Ikon-related constraints removed.';
            
            -- Try to remove column again
            ALTER TABLE Duyurular DROP COLUMN Ikon;
            PRINT 'Ikon column removed successfully after constraint cleanup.';
        END
    END CATCH
END
ELSE
BEGIN
    PRINT 'Ikon column does not exist.';
END

-- Remove Kategori column if it exists
IF COL_LENGTH('Duyurular', 'Kategori') IS NOT NULL
BEGIN
    -- Find and remove all constraints on Kategori column
    DECLARE @sql2 NVARCHAR(MAX);
    
    -- Remove default constraints
    SELECT @sql2 = 'ALTER TABLE Duyurular DROP CONSTRAINT ' + QUOTENAME(dc.name)
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c ON dc.parent_object_id = c.object_id
    WHERE c.object_id = OBJECT_ID('Duyurular') AND c.name = 'Kategori';
    
    IF @sql2 IS NOT NULL
    BEGIN
        BEGIN TRY
            EXEC sp_executesql @sql2;
            PRINT 'Kategori default constraint removed: ' + @sql2;
        END TRY
        BEGIN CATCH
            PRINT 'Warning: Could not remove default constraint: ' + @sql2;
        END CATCH
    END
    
    -- Remove check constraints
    SELECT @sql2 = 'ALTER TABLE Duyurular DROP CONSTRAINT ' + QUOTENAME(cc.name)
    FROM sys.check_constraints cc
    INNER JOIN sys.columns c ON cc.parent_object_id = c.object_id
    WHERE c.object_id = OBJECT_ID('Duyurular') AND c.name = 'Kategori';
    
    IF @sql2 IS NOT NULL
    BEGIN
        BEGIN TRY
            EXEC sp_executesql @sql2;
            PRINT 'Kategori check constraint removed: ' + @sql2;
        END TRY
        BEGIN CATCH
            PRINT 'Warning: Could not remove check constraint: ' + @sql2;
        END CATCH
    END
    
    -- Try to remove the column with CASCADE if possible
    BEGIN TRY
        ALTER TABLE Duyurular DROP COLUMN Kategori;
        PRINT 'Kategori column removed successfully.';
    END TRY
    BEGIN CATCH
        PRINT 'Error removing Kategori column: ' + ERROR_MESSAGE();
        PRINT 'Trying alternative approach...';
        
        -- Alternative: Drop all constraints first, then column
        DECLARE @dropSQL2 NVARCHAR(MAX);
        SELECT @dropSQL2 = STRING_AGG('ALTER TABLE Duyurular DROP CONSTRAINT ' + QUOTENAME(name), '; ')
        FROM sys.objects 
        WHERE parent_object_id = OBJECT_ID('Duyurular') 
        AND type IN ('D', 'C', 'F', 'PK', 'UQ')
        AND name LIKE '%Kategori%';
        
        IF @dropSQL2 IS NOT NULL
        BEGIN
            EXEC sp_executesql @dropSQL2;
            PRINT 'All Kategori-related constraints removed.';
            
            -- Try to remove column again
            ALTER TABLE Duyurular DROP COLUMN Kategori;
            PRINT 'Kategori column removed successfully after constraint cleanup.';
        END
    END CATCH
END
ELSE
BEGIN
    PRINT 'Kategori column does not exist.';
END

-- Verify the table structure
PRINT 'Current table structure:';
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Duyurular'
ORDER BY ORDINAL_POSITION;

PRINT 'Script completed successfully.';
GO
