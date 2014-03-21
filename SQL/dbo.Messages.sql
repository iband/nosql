CREATE TABLE [dbo].messages
(
	[Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY, 
    [userName] VARCHAR(100) NULL, 
    [text] VARCHAR(1000) NULL, 
    [createDate] DATETIME NULL, 
    [version] ROWVERSION NOT NULL

)
