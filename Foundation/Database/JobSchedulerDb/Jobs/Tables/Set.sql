CREATE TABLE [Jobs].[Set] (
    [Id]       INT            IDENTITY (1, 1) NOT NULL,
    [Key]      NVARCHAR (100) NOT NULL,
    [Score]    FLOAT (53)     NOT NULL,
    [Value]    NVARCHAR (256) NOT NULL,
    [ExpireAt] DATETIME       NULL,
    CONSTRAINT [PK_HangFire_Set] PRIMARY KEY CLUSTERED ([Id] ASC)
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [UX_HangFire_Set_KeyAndValue]
    ON [Jobs].[Set]([Key] ASC, [Value] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_HangFire_Set_ExpireAt]
    ON [Jobs].[Set]([ExpireAt] ASC)
    INCLUDE([Id]);


GO
CREATE NONCLUSTERED INDEX [IX_HangFire_Set_Key]
    ON [Jobs].[Set]([Key] ASC)
    INCLUDE([ExpireAt], [Value]);

