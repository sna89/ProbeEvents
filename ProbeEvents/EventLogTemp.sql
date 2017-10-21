CREATE TABLE [dbo].[EventLogTemp]
(
	[Timestamp] BIGINT NOT NULL,
	[Line ID] INT NULL,
	[Direction] BIT NULL, 
    [Journey Pattern ID] VARCHAR(50) NULL, 
    [Timeframe] DATETIME NULL,
    [Vehicle Journey ID] NCHAR(10) NULL, 
    [Operator] NCHAR(10) NULL, 
    [Congestion] BIT NULL, 
    [Lon WGS84] DECIMAL(28, 10) NULL, 
    [Lat WGS84] DECIMAL(28, 10) NULL, 
    [Delay] VARCHAR(50) NULL, 
    [Block ID] VARCHAR(50) NULL, 
    [Vehicle ID] VARCHAR(50) NULL, 
    [Stop ID] VARCHAR(50) NULL, 
    [At Stop] VARCHAR(50) NULL,
)

GO



CREATE CLUSTERED INDEX [IX_EventLog_ColumnTemp] ON [dbo].[EventLogTemp] ([Timestamp], [Journey Pattern ID], [Vehicle Journey ID], [At Stop])
