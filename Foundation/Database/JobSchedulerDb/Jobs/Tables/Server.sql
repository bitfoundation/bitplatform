CREATE TABLE [Jobs].[Server] (
    [Id]            NVARCHAR (100) NOT NULL,
    [Data]          NVARCHAR (MAX) NULL,
    [LastHeartbeat] DATETIME       NOT NULL,
    CONSTRAINT [PK_HangFire_Server] PRIMARY KEY CLUSTERED ([Id] ASC)
);

