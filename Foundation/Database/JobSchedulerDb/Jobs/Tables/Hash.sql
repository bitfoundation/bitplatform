CREATE TABLE [Jobs].[Hash] (
    [Id]       INT            IDENTITY (1, 1) NOT NULL,
    [Key]      NVARCHAR (100) NOT NULL,
    [Field]    NVARCHAR (100) NOT NULL,
    [Value]    NVARCHAR (MAX) NULL,
    [ExpireAt] DATETIME2 (7)  NULL,
    CONSTRAINT [PK_HangFire_Hash] PRIMARY KEY CLUSTERED ([Id] ASC)
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [UX_HangFire_Hash_Key_Field]
    ON [Jobs].[Hash]([Key] ASC, [Field] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_HangFire_Hash_ExpireAt]
    ON [Jobs].[Hash]([ExpireAt] ASC)
    INCLUDE([Id]);


GO
CREATE NONCLUSTERED INDEX [IX_HangFire_Hash_Key]
    ON [Jobs].[Hash]([Key] ASC)
    INCLUDE([ExpireAt]);

