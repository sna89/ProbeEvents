--USE [ProbeEvents]
--GO

--/****** Object:  Table [dbo].[EventLog]    Script Date: 10/26/2017 8:05:19 AM ******/
--SET ANSI_NULLS ON
--GO

--SET QUOTED_IDENTIFIER ON
--GO

CREATE TABLE [dbo].[EventLog](
	[Timestamp] [datetime] NOT NULL,
	[Line ID] [varchar](5) NULL,
	[Direction] [bit] NULL,
	[Journey Pattern ID] [varchar](15) NULL,
	[Timeframe] [datetime] NULL,
	[Vehicle Journey ID] [int] NULL,
	[Operator] [nchar](10) NULL,
	[Congestion] [bit] NULL,
	[Lon WGS84] [decimal](18, 0) NULL,
	[Lat WGS84] [decimal](18, 0) NULL,
	[Delay] [int] NULL,
	[Block ID] [int] NULL,
	[Vehicle ID] [int] NULL,
	[Stop ID] [int] NULL,
	[At Stop] [bit] NULL
) ON [PRIMARY]

GO



CREATE CLUSTERED INDEX [IX_EventLog_Column] ON [dbo].[EventLog] ([Timestamp], [Journey Pattern ID], [Vehicle Journey ID], [At Stop])
