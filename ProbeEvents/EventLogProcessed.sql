USE [ProbeEvents]
GO

/****** Object:  Table [dbo].[EventLogProcessed]    Script Date: 11/24/2017 8:28:52 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[EventLogProcessed](
	[Vehicle Journey ID] [int] NOT NULL,
	[Journey Pattern ID] [varchar](15) NOT NULL,
	[Stop ID] [int] NULL,
	[Timestamp] [datetime] NULL,
	[EventType] [varchar](11) NOT NULL,
	[Station Log Index] [int] NULL,
	[JourneyIndex] [int] NULL
) ON [PRIMARY]

GO


