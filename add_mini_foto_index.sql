-- Duyurular tablosuna MiniFotoIndex kolonu ekle
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Duyurular' AND COLUMN_NAME = 'MiniFotoIndex')
BEGIN
    ALTER TABLE Duyurular ADD MiniFotoIndex INT NULL DEFAULT 0;
    PRINT 'MiniFotoIndex kolonu başarıyla eklendi.';
END
ELSE
BEGIN
    PRINT 'MiniFotoIndex kolonu zaten mevcut.';
END
