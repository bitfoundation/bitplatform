CREATE TABLE [Jobs].[JobParameter] (
    [Id]    INT            IDENTITY (1, 1) NOT NULL,
    [JobId] INT            NOT NULL,
    [Name]  NVARCHAR (40)  NOT NULL,
    [Value] NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_HangFire_JobParameter] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_HangFire_JobParameter_Job] FOREIGN KEY ([JobId]) REFERENCES [Jobs].[Job] ([Id]) ON DELETE CASCADE ON UPDATE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_HangFire_JobParameter_JobIdAndName]
    ON [Jobs].[JobParameter]([JobId] ASC, [Name] ASC);

