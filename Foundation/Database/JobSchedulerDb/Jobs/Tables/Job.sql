CREATE TABLE [Jobs].[Job] (
    [Id]             INT            IDENTITY (1, 1) NOT NULL,
    [StateId]        INT            NULL,
    [StateName]      NVARCHAR (20)  NULL,
    [InvocationData] NVARCHAR (MAX) NOT NULL,
    [Arguments]      NVARCHAR (MAX) NOT NULL,
    [CreatedAt]      DATETIME       NOT NULL,
    [ExpireAt]       DATETIME       NULL,
    CONSTRAINT [PK_HangFire_Job] PRIMARY KEY CLUSTERED ([Id] ASC)
);


GO
CREATE NONCLUSTERED INDEX [IX_HangFire_Job_StateName]
    ON [Jobs].[Job]([StateName] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_HangFire_Job_ExpireAt]
    ON [Jobs].[Job]([ExpireAt] ASC)
    INCLUDE([Id]);

