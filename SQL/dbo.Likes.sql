CREATE TABLE [dbo].[likes] (
    [userName]   VARCHAR (100)    NOT NULL,
    [messageId]  UNIQUEIDENTIFIER NULL,
    [createDate] DATETIME         NULL,
    PRIMARY KEY CLUSTERED ([userName] ASC),
    CONSTRAINT [FK_Likes_to_Messages] FOREIGN KEY ([messageId]) REFERENCES [dbo].[messages] ([Id])
);