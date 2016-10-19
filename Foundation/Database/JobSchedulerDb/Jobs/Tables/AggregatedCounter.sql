CREATE TABLE [Jobs].[AggregatedCounter] (
    [Id]       INT            IDENTITY (1, 1) NOT NULL,
    [Key]      NVARCHAR (100) NOT NULL,
    [Value]    BIGINT         NOT NULL,
    [ExpireAt] DATETIME       NULL,
    CONSTRAINT [PK_HangFire_CounterAggregated] PRIMARY KEY CLUSTERED ([Id] ASC)
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [UX_HangFire_CounterAggregated_Key]
    ON [Jobs].[AggregatedCounter]([Key] ASC)
    INCLUDE([Value]);

