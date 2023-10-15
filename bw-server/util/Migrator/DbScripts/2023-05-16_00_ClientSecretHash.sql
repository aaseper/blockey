IF COL_LENGTH('[dbo].[ApiKey]', 'ClientSecretHash') IS NULL
BEGIN
  ALTER TABLE [dbo].[ApiKey]
  ADD [ClientSecretHash] VARCHAR(128);
END
GO

-- Refresh views
IF OBJECT_ID('[dbo].[ApiKeyDetailsView]') IS NOT NULL
    BEGIN
        EXECUTE sp_refreshview N'[dbo].[ApiKeyDetailsView]';
    END
GO

IF OBJECT_ID('[dbo].[ApiKeyView]') IS NOT NULL
    BEGIN
        EXECUTE sp_refreshview N'[dbo].[ApiKeyView]';
    END
GO

CREATE OR ALTER PROCEDURE [dbo].[ApiKey_Create]
    @Id UNIQUEIDENTIFIER OUTPUT,
    @ServiceAccountId UNIQUEIDENTIFIER,
    @Name VARCHAR(200),
    @ClientSecret VARCHAR(30) = 'migrated', -- Deprecated as of 2023-05-17
    @ClientSecretHash VARCHAR(128) = NULL,
    @Scope NVARCHAR(4000),
    @EncryptedPayload NVARCHAR(4000),
    @Key VARCHAR(MAX),
    @ExpireAt DATETIME2(7),
    @CreationDate DATETIME2(7),
    @RevisionDate DATETIME2(7)
AS
BEGIN
    SET NOCOUNT ON

    IF (@ClientSecretHash IS NULL)
    BEGIN
      DECLARE @hb VARBINARY(128) = HASHBYTES('SHA2_256', @ClientSecret);
      SET @ClientSecretHash = CAST(N'' as xml).value('xs:base64Binary(sql:variable("@hb"))', 'VARCHAR(128)');
    END

    INSERT INTO [dbo].[ApiKey] 
    (
        [Id],
        [ServiceAccountId],
        [Name],
        [ClientSecret],
        [ClientSecretHash],
        [Scope],
        [EncryptedPayload],
        [Key],
        [ExpireAt],
        [CreationDate],
        [RevisionDate]
    )
    VALUES 
    (
        @Id,
        @ServiceAccountId,
        @Name,
        @ClientSecret,
        @ClientSecretHash,
        @Scope,
        @EncryptedPayload,
        @Key,
        @ExpireAt,
        @CreationDate,
        @RevisionDate
    )
END
GO
