--USE [ProbeEvents]
--GO

--/****** Object:  Table [dbo].[EventLogTemp]    Script Date: 10/26/2017 8:07:39 AM ******/
--SET ANSI_NULLS ON
--GO

--SET QUOTED_IDENTIFIER ON
--GO

CREATE TABLE [dbo].[EventLogTemp](
	[Timestamp] [bigint] NOT NULL,
	[Line ID] [int] NULL,
	[Direction] [bit] NULL,
	[Journey Pattern ID] [varchar](50) NULL,
	[Timeframe] [datetime] NULL,
	[Vehicle Journey ID] [nchar](10) NULL,
	[Operator] [nchar](10) NULL,
	[Congestion] [bit] NULL,
	[Lon WGS84] [decimal](28, 10) NULL,
	[Lat WGS84] [decimal](28, 10) NULL,
	[Delay] [varchar](50) NULL,
	[Block ID] [varchar](50) NULL,
	[Vehicle ID] [varchar](50) NULL,
	[Stop ID] [varchar](50) NULL,
	[At Stop] [varchar](50) NULL
) ON [PRIMARY]

GO


