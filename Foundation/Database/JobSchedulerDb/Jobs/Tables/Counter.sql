CREATE TABLE [Jobs].[Counter] (
    [Id]       INT            IDENTITY (1, 1) NOT NULL,
    [Key]      NVARCHAR (100) NOT NULL,
    [Value]    SMALLINT       NOT NULL,
    [ExpireAt] DATETIME       NULL,
    CONSTRAINT [PK_HangFire_Counter] PRIMARY KEY CLUSTERED ([Id] ASC)
);


GO
CREATE NONCLUSTERED INDEX [IX_HangFire_Counter_Key]
    ON [Jobs].[Counter]([Key] ASC)
    INCLUDE([Value]);

