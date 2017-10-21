CREATE TABLE [dbo].[EventLog]
(
	[Timestamp] DATETIME NOT NULL,
	[Line ID] INT NULL,
	[Direction] BIT NULL, 
    [Journey Pattern ID] VARCHAR(10) NULL, 
    [Timeframe] DATETIME NULL,
    [Vehicle Journey ID] INT NULL, 
    [Operator] NCHAR(10) NULL, 
    [Congestion] BIT NULL, 
    [Lon WGS84] DECIMAL NULL, 
    [Lat WGS84] DECIMAL NULL, 
    [Delay] INT NULL, 
    [Block ID] INT NULL, 
    [Vehicle ID] INT NULL, 
    [Stop ID] INT NULL, 
    [At Stop] BIT NULL
)

GO



CREATE CLUSTERED INDEX [IX_EventLog_Column] ON [dbo].[EventLog] ([Timestamp], [Journey Pattern ID], [Vehicle Journey ID], [At Stop])
