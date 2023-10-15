IF COL_LENGTH('dbo.User', 'LastPasswordChangeDate') IS NULL
BEGIN
    ALTER TABLE
        [dbo].[User]
    ADD
        [LastPasswordChangeDate] DATETIME2 (7) NULL
END
GO

IF COL_LENGTH('dbo.User', 'LastKdfChangeDate') IS NULL
BEGIN
    ALTER TABLE
        [dbo].[User]
    ADD
        [LastKdfChangeDate] DATETIME2 (7) NULL
END
GO

IF COL_LENGTH('dbo.User', 'LastKeyRotationDate') IS NULL
BEGIN
    ALTER TABLE
        [dbo].[User]
    ADD
        [LastKeyRotationDate] DATETIME2 (7) NULL
END
GO

IF COL_LENGTH('dbo.User', 'LastEmailChangeDate') IS NULL
BEGIN
    ALTER TABLE
        [dbo].[User]
    ADD
        [LastEmailChangeDate] DATETIME2 (7) NULL
END
GO

CREATE OR ALTER VIEW [dbo].[UserView]
AS
SELECT
   *
FROM
   [dbo].[User]
GO

CREATE OR ALTER PROCEDURE [dbo].[User_UpdateKeys]
    @Id UNIQUEIDENTIFIER,
    @SecurityStamp NVARCHAR(50),
    @Key NVARCHAR(MAX),
    @PrivateKey VARCHAR(MAX),
    @RevisionDate DATETIME2(7),
    @AccountRevisionDate DATETIME2(7) = NULL,
    @LastKeyRotationDate DATETIME2(7) = NULL
AS
BEGIN
    SET NOCOUNT ON

    UPDATE
        [dbo].[User]
    SET
        [SecurityStamp] = @SecurityStamp,
        [Key] = @Key,
        [PrivateKey] = @PrivateKey,
        [RevisionDate] = @RevisionDate,
        [AccountRevisionDate] = ISNULL(@AccountRevisionDate, @RevisionDate),
        [LastKeyRotationDate] = @LastKeyRotationDate
    WHERE
        [Id] = @Id
END
GO

CREATE OR ALTER PROCEDURE [dbo].[User_Create]
    @Id UNIQUEIDENTIFIER OUTPUT,
    @Name NVARCHAR(50),
    @Email NVARCHAR(256),
    @EmailVerified BIT,
    @MasterPassword NVARCHAR(300),
    @MasterPasswordHint NVARCHAR(50),
    @Culture NVARCHAR(10),
    @SecurityStamp NVARCHAR(50),
    @TwoFactorProviders NVARCHAR(MAX),
    @TwoFactorRecoveryCode NVARCHAR(32),
    @EquivalentDomains NVARCHAR(MAX),
    @ExcludedGlobalEquivalentDomains NVARCHAR(MAX),
    @AccountRevisionDate DATETIME2(7),
    @Key NVARCHAR(MAX),
    @PublicKey NVARCHAR(MAX),
    @PrivateKey NVARCHAR(MAX),
    @Premium BIT,
    @PremiumExpirationDate DATETIME2(7),
    @RenewalReminderDate DATETIME2(7),
    @Storage BIGINT,
    @MaxStorageGb SMALLINT,
    @Gateway TINYINT,
    @GatewayCustomerId VARCHAR(50),
    @GatewaySubscriptionId VARCHAR(50),
    @ReferenceData VARCHAR(MAX),
    @LicenseKey VARCHAR(100),
    @Kdf TINYINT,
    @KdfIterations INT,
    @KdfMemory INT = NULL,
    @KdfParallelism INT = NULL,
    @CreationDate DATETIME2(7),
    @RevisionDate DATETIME2(7),
    @ApiKey VARCHAR(30),
    @ForcePasswordReset BIT = 0,
    @UsesKeyConnector BIT = 0,
    @FailedLoginCount INT = 0,
    @LastFailedLoginDate DATETIME2(7),
    @UnknownDeviceVerificationEnabled BIT = 1,
    @AvatarColor VARCHAR(7) = NULL,
    @LastPasswordChangeDate DATETIME2(7) = NULL,
    @LastKdfChangeDate DATETIME2(7) = NULL,
    @LastKeyRotationDate DATETIME2(7) = NULL,
    @LastEmailChangeDate DATETIME2(7) = NULL
AS
BEGIN
    SET NOCOUNT ON

    INSERT INTO [dbo].[User]
    (
        [Id],
        [Name],
        [Email],
        [EmailVerified],
        [MasterPassword],
        [MasterPasswordHint],
        [Culture],
        [SecurityStamp],
        [TwoFactorProviders],
        [TwoFactorRecoveryCode],
        [EquivalentDomains],
        [ExcludedGlobalEquivalentDomains],
        [AccountRevisionDate],
        [Key],
        [PublicKey],
        [PrivateKey],
        [Premium],
        [PremiumExpirationDate],
        [RenewalReminderDate],
        [Storage],
        [MaxStorageGb],
        [Gateway],
        [GatewayCustomerId],
        [GatewaySubscriptionId],
        [ReferenceData],
        [LicenseKey],
        [Kdf],
        [KdfIterations],
        [CreationDate],
        [RevisionDate],
        [ApiKey],
        [ForcePasswordReset],
        [UsesKeyConnector],
        [FailedLoginCount],
        [LastFailedLoginDate],
        [UnknownDeviceVerificationEnabled],
        [AvatarColor],
        [KdfMemory],
        [KdfParallelism],
        [LastPasswordChangeDate],
        [LastKdfChangeDate],
        [LastKeyRotationDate],
        [LastEmailChangeDate]
    )
    VALUES
    (
        @Id,
        @Name,
        @Email,
        @EmailVerified,
        @MasterPassword,
        @MasterPasswordHint,
        @Culture,
        @SecurityStamp,
        @TwoFactorProviders,
        @TwoFactorRecoveryCode,
        @EquivalentDomains,
        @ExcludedGlobalEquivalentDomains,
        @AccountRevisionDate,
        @Key,
        @PublicKey,
        @PrivateKey,
        @Premium,
        @PremiumExpirationDate,
        @RenewalReminderDate,
        @Storage,
        @MaxStorageGb,
        @Gateway,
        @GatewayCustomerId,
        @GatewaySubscriptionId,
        @ReferenceData,
        @LicenseKey,
        @Kdf,
        @KdfIterations,
        @CreationDate,
        @RevisionDate,
        @ApiKey,
        @ForcePasswordReset,
        @UsesKeyConnector,
        @FailedLoginCount,
        @LastFailedLoginDate,
        @UnknownDeviceVerificationEnabled,
        @AvatarColor,
        @KdfMemory,
        @KdfParallelism,
        @LastPasswordChangeDate,
        @LastKdfChangeDate,
        @LastKeyRotationDate,
        @LastEmailChangeDate
    )
END
GO

CREATE OR ALTER PROCEDURE [dbo].[User_Update]
    @Id UNIQUEIDENTIFIER,
    @Name NVARCHAR(50),
    @Email NVARCHAR(256),
    @EmailVerified BIT,
    @MasterPassword NVARCHAR(300),
    @MasterPasswordHint NVARCHAR(50),
    @Culture NVARCHAR(10),
    @SecurityStamp NVARCHAR(50),
    @TwoFactorProviders NVARCHAR(MAX),
    @TwoFactorRecoveryCode NVARCHAR(32),
    @EquivalentDomains NVARCHAR(MAX),
    @ExcludedGlobalEquivalentDomains NVARCHAR(MAX),
    @AccountRevisionDate DATETIME2(7),
    @Key NVARCHAR(MAX),
    @PublicKey NVARCHAR(MAX),
    @PrivateKey NVARCHAR(MAX),
    @Premium BIT,
    @PremiumExpirationDate DATETIME2(7),
    @RenewalReminderDate DATETIME2(7),
    @Storage BIGINT,
    @MaxStorageGb SMALLINT,
    @Gateway TINYINT,
    @GatewayCustomerId VARCHAR(50),
    @GatewaySubscriptionId VARCHAR(50),
    @ReferenceData VARCHAR(MAX),
    @LicenseKey VARCHAR(100),
    @Kdf TINYINT,
    @KdfIterations INT,
    @KdfMemory INT = NULL,
    @KdfParallelism INT = NULL,
    @CreationDate DATETIME2(7),
    @RevisionDate DATETIME2(7),
    @ApiKey VARCHAR(30),
    @ForcePasswordReset BIT = 0,
    @UsesKeyConnector BIT = 0,
    @FailedLoginCount INT,
    @LastFailedLoginDate DATETIME2(7),
    @UnknownDeviceVerificationEnabled BIT = 1,
    @AvatarColor VARCHAR(7),
    @LastPasswordChangeDate DATETIME2(7) = NULL,
    @LastKdfChangeDate DATETIME2(7) = NULL,
    @LastKeyRotationDate DATETIME2(7) = NULL,
    @LastEmailChangeDate DATETIME2(7) = NULL
AS
BEGIN
    SET NOCOUNT ON

    UPDATE
        [dbo].[User]
    SET
        [Name] = @Name,
        [Email] = @Email,
        [EmailVerified] = @EmailVerified,
        [MasterPassword] = @MasterPassword,
        [MasterPasswordHint] = @MasterPasswordHint,
        [Culture] = @Culture,
        [SecurityStamp] = @SecurityStamp,
        [TwoFactorProviders] = @TwoFactorProviders,
        [TwoFactorRecoveryCode] = @TwoFactorRecoveryCode,
        [EquivalentDomains] = @EquivalentDomains,
        [ExcludedGlobalEquivalentDomains] = @ExcludedGlobalEquivalentDomains,
        [AccountRevisionDate] = @AccountRevisionDate,
        [Key] = @Key,
        [PublicKey] = @PublicKey,
        [PrivateKey] = @PrivateKey,
        [Premium] = @Premium,
        [PremiumExpirationDate] = @PremiumExpirationDate,
        [RenewalReminderDate] = @RenewalReminderDate,
        [Storage] = @Storage,
        [MaxStorageGb] = @MaxStorageGb,
        [Gateway] = @Gateway,
        [GatewayCustomerId] = @GatewayCustomerId,
        [GatewaySubscriptionId] = @GatewaySubscriptionId,
        [ReferenceData] = @ReferenceData,
        [LicenseKey] = @LicenseKey,
        [Kdf] = @Kdf,
        [KdfIterations] = @KdfIterations,
        [KdfMemory] = @KdfMemory,
        [KdfParallelism] = @KdfParallelism,
        [CreationDate] = @CreationDate,
        [RevisionDate] = @RevisionDate,
        [ApiKey] = @ApiKey,
        [ForcePasswordReset] = @ForcePasswordReset,
        [UsesKeyConnector] = @UsesKeyConnector,
        [FailedLoginCount] = @FailedLoginCount,
        [LastFailedLoginDate] = @LastFailedLoginDate,
        [UnknownDeviceVerificationEnabled] = @UnknownDeviceVerificationEnabled,
        [AvatarColor] = @AvatarColor,
        [LastPasswordChangeDate] = @LastPasswordChangeDate,
        [LastKdfChangeDate] = @LastKdfChangeDate,
        [LastKeyRotationDate] = @LastKeyRotationDate,
        [LastEmailChangeDate] = @LastEmailChangeDate
    WHERE
        [Id] = @Id
END
GO
