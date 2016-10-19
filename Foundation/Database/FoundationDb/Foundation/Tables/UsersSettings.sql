CREATE TABLE [Foundation].[UsersSettings]
(
	[Id] BIGINT NOT NULL PRIMARY KEY IDENTITY, 
    [Theme] NVARCHAR(50) NULL, 
    [Culture] NVARCHAR(50) NULL, 
    [DesiredTimeZoneOffset] NVARCHAR(50) NULL, 
	[UserId] NVARCHAR(MAX) NOT NULL,
    [ConcurrencyToken] TIMESTAMP NOT NULL
)
